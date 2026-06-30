using System;
using UnityEngine;

namespace StoreSim.NPC
{
    [DisallowMultipleComponent]
    public class NpcRoute : NpcActorBase
    {
        [SerializeField] private float _moveSpeed = 2f;
        [SerializeField] private float _rotationSpeed = 10f;
        [SerializeField] private float _stoppingDistance = 0.1f;

        [SerializeField] private LayerMask _groundMask;
        [SerializeField] private float _groundCheckHeight = 2f;
        [SerializeField] private float _groundCheckDistance = 10f;
        [SerializeField] private float _groundOffset;

        [Header("Walls")]
        [SerializeField] private LayerMask _obstacleMask = ~0;
        [SerializeField] private float _collisionPadding = 0.02f;
        [SerializeField] private float _fallbackHeight = 1.8f;
        [SerializeField] private float _fallbackRadius = 0.25f;

        [Header("Visual")]
        [SerializeField] private float _facingYawOffset = 180f;
        [SerializeField] private float _swayAngle = 3f;
        [SerializeField] private float _swaySpeed = 8f;

        private Transform[] _mandatoryPoints = Array.Empty<Transform>();
        private Transform[] _randomDestinations = Array.Empty<Transform>();
        private Transform _currentTarget;
        private int _mandatoryIndex;
        private bool _mandatoryPhaseComplete = true;
        private bool _routeFinished;
        private bool _initialized;

        private NpcMover _mover;
        private Quaternion _baseFacing = Quaternion.identity;
        private float _swayPhase;

        public void Initialize(Transform[] mandatoryPoints, Transform[] randomDestinations, NpcMovementSettings movement)
        {
            _moveSpeed = movement.MoveSpeed;
            _rotationSpeed = movement.RotationSpeed;
            _stoppingDistance = movement.StoppingDistance;
            _groundMask = movement.GroundMask;
            _groundCheckHeight = movement.GroundCheckHeight;
            _groundCheckDistance = movement.GroundCheckDistance;
            _groundOffset = movement.GroundOffset;

            _mover = new NpcMover(transform, GetComponent<Collider>());
            _mover.Configure(movement, _obstacleMask, _collisionPadding, _fallbackHeight, _fallbackRadius);

            _mandatoryPoints = TransformUtils.FilterNull(mandatoryPoints);
            _randomDestinations = TransformUtils.FilterNull(randomDestinations);
            _mandatoryIndex = 0;
            _mandatoryPhaseComplete = _mandatoryPoints.Length == 0;
            _routeFinished = false;
            _initialized = true;
            _currentTarget = _mandatoryPhaseComplete ? PickRandomDestination() : _mandatoryPoints[0];

            _mover.SnapToGround();
        }

        private void Update()
        {
            if (!_initialized || _routeFinished || _currentTarget == null)
                return;

            Vector3 toTarget = _currentTarget.position - transform.position;
            toTarget.y = 0f;

            if (toTarget.sqrMagnitude <= _stoppingDistance * _stoppingDistance)
            {
                OnReachedCurrentPoint();
                return;
            }

            RotateTowards(toTarget);
            _mover.MoveTowards(_currentTarget.position);
            ApplyWalkSway();
        }

        private void RotateTowards(Vector3 direction)
        {
            if (direction.sqrMagnitude < 0.0001f)
                return;

            _baseFacing = Quaternion.LookRotation(direction) * Quaternion.Euler(0f, _facingYawOffset, 0f);
            transform.rotation = Quaternion.Slerp(transform.rotation, _baseFacing, _rotationSpeed * Time.deltaTime);
        }

        private void ApplyWalkSway()
        {
            if (_swayAngle <= 0f)
                return;

            if (_mover.IsMoving)
                _swayPhase += _swaySpeed * Time.deltaTime;
            else
                _swayPhase = Mathf.MoveTowards(_swayPhase, 0f, _swaySpeed * Time.deltaTime);

            float sway = Mathf.Sin(_swayPhase) * (_mover.IsMoving ? _swayAngle : 0f);
            transform.rotation = _baseFacing * Quaternion.Euler(0f, sway, sway * 0.35f);
        }

        private void OnReachedCurrentPoint()
        {
            if (!_mandatoryPhaseComplete)
            {
                _mandatoryIndex++;
                _currentTarget = _mandatoryIndex >= _mandatoryPoints.Length
                    ? PickRandomDestination()
                    : _mandatoryPoints[_mandatoryIndex];

                if (_mandatoryIndex >= _mandatoryPoints.Length)
                    _mandatoryPhaseComplete = true;

                if (_currentTarget == null)
                    _routeFinished = true;

                return;
            }

            _routeFinished = true;
            _currentTarget = null;
        }

        private Transform PickRandomDestination() => TransformUtils.PickRandom(_randomDestinations);

        private void OnValidate()
        {
            _swayAngle = Mathf.Max(0f, _swayAngle);
            _swaySpeed = Mathf.Max(0f, _swaySpeed);
        }
    }
}
