using System;
using System.Collections;
using StoreSim.Shop;
using UnityEngine;

namespace StoreSim.NPC
{
    /// <summary>
    /// Полный цикл покупателя на КАСТОМНОМ движении (NpcMover, без NavMesh):
    /// вход в магазин -> подход к случайной свободной машине -> осмотр (задержка) ->
    /// очередь к кассе -> оплата (анимация слайдера) -> уход и деспавн.
    ///
    /// Наследуется от <see cref="NpcActorBase"/>, чтобы двери (SlidingDoorByNpc) его видели.
    /// Движение/повороты/покачивание повторяют логику NpcRoute.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("StoreSim/Customer Controller")]
    public class CustomerController : NpcActorBase
    {
        public enum State
        {
            EnteringStore,
            GoingToCar,
            InspectingCar,
            GoingToCashier,
            WaitingInQueue,
            Paying,
            Leaving,
            Done
        }

        [Header("Движение")]
        [SerializeField] private float _moveSpeed = 1.5f;
        [SerializeField] private float _rotationSpeed = 10f;
        [Tooltip("Дистанция, на которой считаем, что NPC дошёл до точки.")]
        [SerializeField] private float _stoppingDistance = 0.5f;

        [Header("Земля")]
        [SerializeField] private LayerMask _groundMask;
        [SerializeField] private float _groundCheckHeight = 2f;
        [SerializeField] private float _groundCheckDistance = 10f;
        [SerializeField] private float _groundOffset;

        [Header("Стены")]
        [SerializeField] private LayerMask _obstacleMask = ~0;
        [SerializeField] private float _collisionPadding = 0.02f;
        [SerializeField] private float _fallbackHeight = 1.8f;
        [SerializeField] private float _fallbackRadius = 0.25f;

        [Header("Визуал")]
        [SerializeField] private float _facingYawOffset = 180f;
        [SerializeField] private float _swayAngle = 3f;
        [SerializeField] private float _swaySpeed = 8f;

        [Header("Ссылки")]
        [Tooltip("Якорь над головой для слайдера/текста. Если пусто — используется сам объект.")]
        [SerializeField] private Transform _headAnchor;

        [Header("Поведение")]
        [Tooltip("Диапазон времени осмотра машины перед походом к кассе (сек).")]
        [SerializeField] private Vector2 _inspectDuration = new(2f, 3f);

        [Tooltip("Через сколько секунд после оплаты гарантированно удалить покупателя, даже если он не дошёл до выхода.")]
        [SerializeField] private float _leaveTimeout = 8f;
        [Tooltip("Удалять покупателя сразу после оплаты, не идя к выходу.")]
        [SerializeField] private bool _despawnImmediatelyAfterPay;

        public State Current { get; private set; } = State.Done;
        public CarShopItem PendingCar { get; private set; }
        public Transform HeadAnchor => _headAnchor != null ? _headAnchor : transform;

        /// <summary>Вызывается при деспавне (для спавнера/счётчиков).</summary>
        public event Action<CustomerController> OnDespawned;

        private NpcMover _mover;
        private Quaternion _baseFacing = Quaternion.identity;
        private float _swayPhase;

        private Transform[] _entryPoints = Array.Empty<Transform>();
        private int _entryIndex;
        private Transform _exitPoint;

        private Vector3 _destination;
        private bool _hasDestination;
        private bool _initialized;

        private bool _isFrontInQueue;
        private bool _notifiedCashier;
        private float _leaveTimer;

        /// <summary>Запустить цикл покупателя с одной точкой входа.</summary>
        public void Initialize(Transform entryPoint, Transform exitPoint)
            => Initialize(entryPoint != null ? new[] { entryPoint } : Array.Empty<Transform>(), exitPoint);

        /// <summary>
        /// Запустить цикл покупателя. <paramref name="entryPoints"/> — путь внутрь магазина
        /// (например через дверь), который NPC проходит ДО выбора машины.
        /// </summary>
        public void Initialize(Transform[] entryPoints, Transform exitPoint)
        {
            _entryPoints = TransformUtils.FilterNull(entryPoints);
            _entryIndex = 0;
            _exitPoint = exitPoint;

            BuildMover();
            _mover.SnapToGround();

            _initialized = true;
            EnterEnteringStore();
        }

        /// <summary>Альтернатива: задать настройки движения извне (например из спавнера).</summary>
        public void ApplyMovementSettings(NpcMovementSettings movement)
        {
            _moveSpeed = movement.MoveSpeed;
            _rotationSpeed = movement.RotationSpeed;
            _stoppingDistance = movement.StoppingDistance;
            _groundMask = movement.GroundMask;
            _groundCheckHeight = movement.GroundCheckHeight;
            _groundCheckDistance = movement.GroundCheckDistance;
            _groundOffset = movement.GroundOffset;

            BuildMover();
        }

        private void BuildMover()
        {
            _mover = new NpcMover(transform, GetComponent<Collider>());
            _mover.Configure(BuildMovementSettings(), _obstacleMask, _collisionPadding, _fallbackHeight, _fallbackRadius);
        }

        private NpcMovementSettings BuildMovementSettings() => new()
        {
            MoveSpeed = _moveSpeed,
            RotationSpeed = _rotationSpeed,
            StoppingDistance = _stoppingDistance,
            GroundMask = _groundMask,
            GroundCheckHeight = _groundCheckHeight,
            GroundCheckDistance = _groundCheckDistance,
            GroundOffset = _groundOffset
        };

        private void Update()
        {
            if (!_initialized)
                return;

            UpdateStateMachine();
            UpdateMovement();
        }

        // ---------- Стейт-машина ----------

        private void UpdateStateMachine()
        {
            switch (Current)
            {
                case State.EnteringStore:
                    if (HasArrived())
                        AdvanceEntry();
                    break;

                case State.GoingToCar:
                    if (HasArrived())
                        BeginInspectCar();
                    break;

                case State.WaitingInQueue:
                    if (_isFrontInQueue && !_notifiedCashier && HasArrived())
                    {
                        _notifiedCashier = true;
                        Current = State.Paying;
                        StopMoving();
                        CheckoutManager.Instance.NotifyArrivedAtCashier(this);
                    }
                    break;

                case State.Leaving:
                    _leaveTimer += Time.deltaTime;
                    if (_exitPoint == null || HasArrived() || _leaveTimer >= _leaveTimeout)
                        Despawn();
                    break;

                // InspectingCar и Paying управляются корутиной/коллбэком.
            }
        }

        private void EnterEnteringStore()
        {
            Current = State.EnteringStore;
            _entryIndex = 0;

            if (_entryPoints.Length > 0)
                GoTo(_entryPoints[0].position);
            else
                BeginGoToCar();
        }

        /// <summary>Идём по точкам входа по очереди, затем — к машине.</summary>
        private void AdvanceEntry()
        {
            _entryIndex++;
            if (_entryIndex < _entryPoints.Length)
                GoTo(_entryPoints[_entryIndex].position);
            else
                BeginGoToCar();
        }

        private void BeginGoToCar()
        {
            PendingCar = ShopManager.Instance != null
                ? ShopManager.Instance.ReserveRandomAvailableCar()
                : null;

            if (PendingCar == null)
            {
                ToLeaving();
                return;
            }

            Current = State.GoingToCar;
            GoTo(PendingCar.ApproachPoint.position);
        }

        private void BeginInspectCar()
        {
            Current = State.InspectingCar;
            StopMoving();
            StartCoroutine(InspectRoutine());
        }

        private IEnumerator InspectRoutine()
        {
            float wait = UnityEngine.Random.Range(_inspectDuration.x, _inspectDuration.y);
            yield return new WaitForSeconds(wait);
            JoinCashierQueue();
        }

        private void JoinCashierQueue()
        {
            if (CheckoutManager.Instance == null)
            {
                ToLeaving();
                return;
            }

            _notifiedCashier = false;
            Current = State.WaitingInQueue;

            Vector3 slot = CheckoutManager.Instance.JoinQueue(this);
            _isFrontInQueue = CheckoutManager.Instance.IsFront(this);
            GoTo(slot);
        }

        /// <summary>Вызывается кассой при пересчёте очереди (подвинуться вперёд / стать первым).</summary>
        public void SetQueueTarget(Vector3 position, bool isFront)
        {
            _isFrontInQueue = isFront;
            if (Current == State.WaitingInQueue)
                GoTo(position);
        }

        /// <summary>Вызывается кассой после начисления денег — можно уходить.</summary>
        public void OnServiceComplete()
        {
            if (_despawnImmediatelyAfterPay)
            {
                Despawn();
                return;
            }

            ToLeaving();
        }

        private void ToLeaving()
        {
            Current = State.Leaving;
            _leaveTimer = 0f;
            if (_exitPoint != null)
                GoTo(_exitPoint.position);
            else
                StopMoving();
        }

        private void Despawn()
        {
            Current = State.Done;
            OnDespawned?.Invoke(this);
            Destroy(gameObject);
        }

        // ---------- Кастомное движение ----------

        private void GoTo(Vector3 position)
        {
            _destination = position;
            _hasDestination = true;
        }

        private void StopMoving() => _hasDestination = false;

        private bool HasArrived()
        {
            if (!_hasDestination)
                return true;

            Vector3 toTarget = _destination - transform.position;
            toTarget.y = 0f;
            return toTarget.sqrMagnitude <= _stoppingDistance * _stoppingDistance;
        }

        private void UpdateMovement()
        {
            if (_mover == null)
                return;

            if (!_hasDestination || HasArrived())
            {
                ApplyWalkSway(false);
                _mover.SnapToGround();
                return;
            }

            Vector3 toTarget = _destination - transform.position;
            toTarget.y = 0f;

            RotateTowards(toTarget);
            _mover.MoveTowards(_destination);
            ApplyWalkSway(true);
        }

        private void RotateTowards(Vector3 direction)
        {
            if (direction.sqrMagnitude < 0.0001f)
                return;

            _baseFacing = Quaternion.LookRotation(direction) * Quaternion.Euler(0f, _facingYawOffset, 0f);
            transform.rotation = Quaternion.Slerp(transform.rotation, _baseFacing, _rotationSpeed * Time.deltaTime);
        }

        private void ApplyWalkSway(bool moving)
        {
            if (_swayAngle <= 0f)
                return;

            bool isMoving = moving && _mover.IsMoving;

            if (isMoving)
                _swayPhase += _swaySpeed * Time.deltaTime;
            else
                _swayPhase = Mathf.MoveTowards(_swayPhase, 0f, _swaySpeed * Time.deltaTime);

            float sway = Mathf.Sin(_swayPhase) * (isMoving ? _swayAngle : 0f);
            transform.rotation = _baseFacing * Quaternion.Euler(0f, sway, sway * 0.35f);
        }

        private void OnDestroy()
        {
            if (PendingCar != null && !PendingCar.IsSold)
                PendingCar.ReleaseReservation();

            if (CheckoutManager.Instance != null)
                CheckoutManager.Instance.LeaveQueue(this);
        }

        private void OnValidate()
        {
            _inspectDuration.x = Mathf.Max(0f, _inspectDuration.x);
            _inspectDuration.y = Mathf.Max(_inspectDuration.x, _inspectDuration.y);
            _swayAngle = Mathf.Max(0f, _swayAngle);
            _swaySpeed = Mathf.Max(0f, _swaySpeed);
        }
    }
}
