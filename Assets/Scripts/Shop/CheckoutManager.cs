using System.Collections;
using System.Collections.Generic;
using StoreSim.Economy;
using StoreSim.NPC;
using StoreSim.UI;
using UnityEngine;
using UnityEngine.UI;

namespace StoreSim.Shop
{
    /// <summary>
    /// Касса: управляет очередью покупателей, обслуживает первого в очереди,
    /// показывает круговой Slider из канваса над головой NPC и по окончании
    /// начисляет цену машины игроку (через <see cref="EconomyManager"/>).
    ///
    /// Очередь — это упорядоченный список. Индекс 0 стоит у кассы (точка обслуживания),
    /// остальные — на точках ожидания (либо назначенных, либо вычисленных за предыдущим).
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("StoreSim/Checkout Manager")]
    public class CheckoutManager : MonoBehaviour
    {
        public static CheckoutManager Instance { get; private set; }

        [Header("Точки")]
        [Tooltip("Точка подхода к кассе (дочерний куб у кассы) — место первого в очереди.")]
        [SerializeField] private Transform _cashierPoint;

        [Tooltip("Необязательно: фиксированные точки ожидания. Индекс 0 — сразу за кассой.")]
        [SerializeField] private List<Transform> _queuePoints = new();

        [Header("Если точки ожидания не заданы — считаем позицию за предыдущим")]
        [Tooltip("Направление роста очереди в мировых координатах (обычно от кассы 'назад').")]
        [SerializeField] private Vector3 _queueDirection = Vector3.back;
        [SerializeField] private float _queueSpacing = 1.2f;

        [Header("Обслуживание")]
        [Tooltip("Круговой слайдер из канваса (объект со Slider). Включается над головой NPC на время оплаты.")]
        [SerializeField] private GameObject _sliderPrefab;
        [Tooltip("Смещение над головой (в мировых координатах) перед переводом в экранные.")]
        [SerializeField] private Vector3 _sliderOffset = new(0f, 2.2f, 0f);
        [SerializeField] private float _serviceDuration = 3f;

        [Header("Деньги")]
        [Tooltip("Формат всплывающего текста начисления. {0} = сумма. Например: \"+${0}\".")]
        [SerializeField] private string _moneyTextFormat = "+${0}";

        private readonly List<CustomerController> _line = new();
        private bool _serving;

        private Slider _slider;
        private RectTransform _sliderRect;
        private Canvas _sliderCanvas;

        public Transform CashierPoint => _cashierPoint;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            CacheSlider();
            HideSlider();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void CacheSlider()
        {
            if (_sliderPrefab == null)
                return;

            _slider = _sliderPrefab.GetComponentInChildren<Slider>(true);
            _sliderRect = _sliderPrefab.GetComponent<RectTransform>();
            _sliderCanvas = _sliderPrefab.GetComponentInParent<Canvas>(true);
        }

        /// <summary>NPC встаёт в очередь. Возвращает позицию его слота (куда идти).</summary>
        public Vector3 JoinQueue(CustomerController customer)
        {
            if (customer == null)
                return _cashierPoint != null ? _cashierPoint.position : transform.position;

            if (!_line.Contains(customer))
                _line.Add(customer);

            return GetSlotPosition(_line.IndexOf(customer));
        }

        /// <summary>True, если данный NPC сейчас первый в очереди.</summary>
        public bool IsFront(CustomerController customer)
        {
            return _line.Count > 0 && _line[0] == customer;
        }

        /// <summary>NPC сообщает, что дошёл до кассы. Если он первый и касса свободна — начинаем обслуживание.</summary>
        public void NotifyArrivedAtCashier(CustomerController customer)
        {
            if (!_serving && IsFront(customer))
                StartCoroutine(ServeRoutine(customer));
        }

        /// <summary>NPC покинул очередь досрочно (исчез до обслуживания).</summary>
        public void LeaveQueue(CustomerController customer)
        {
            if (_line.Remove(customer))
                AdvanceLine();
        }

        private IEnumerator ServeRoutine(CustomerController customer)
        {
            _serving = true;

            Transform headAnchor = customer.HeadAnchor;

            ShowSlider();
            PositionSliderOverHead(headAnchor);
            SetSliderProgress(0f);

            float elapsed = 0f;
            while (elapsed < _serviceDuration)
            {
                elapsed += Time.deltaTime;
                SetSliderProgress(Mathf.Clamp01(elapsed / _serviceDuration));
                PositionSliderOverHead(headAnchor);
                yield return null;
            }

            SetSliderProgress(1f);
            HideSlider();

            // Начисляем цену машины игроку.
            int amount = customer.PendingCar != null ? customer.PendingCar.Price : 0;
            if (amount > 0)
            {
                if (EconomyManager.Instance != null)
                    EconomyManager.Instance.AddMoney(amount);

                ShowMoneyPopup(headAnchor, amount);
            }

            // Помечаем машину проданной / деспавним.
            if (customer.PendingCar != null)
                customer.PendingCar.Sell();

            _line.Remove(customer);
            customer.OnServiceComplete();

            _serving = false;
            AdvanceLine();
        }

        private void ShowMoneyPopup(Transform head, int amount)
        {
            if (FloatingTextManager.Instance == null || head == null || string.IsNullOrEmpty(_moneyTextFormat))
                return;

            FloatingTextManager.Instance.ShowFloatingText(
                head.position + _sliderOffset,
                string.Format(_moneyTextFormat, amount));
        }

        private void ShowSlider()
        {
            if (_sliderPrefab != null && !_sliderPrefab.activeSelf)
                _sliderPrefab.SetActive(true);
        }

        private void HideSlider()
        {
            if (_sliderPrefab != null && _sliderPrefab.activeSelf)
                _sliderPrefab.SetActive(false);
        }

        private void SetSliderProgress(float value01)
        {
            if (_slider == null)
                return;

            _slider.minValue = 0f;
            _slider.maxValue = 1f;
            _slider.value = value01;
        }

        /// <summary>Ставит слайдер над головой NPC (корректно для Screen Space - Overlay и - Camera).</summary>
        private void PositionSliderOverHead(Transform head)
        {
            if (_sliderRect == null || head == null)
                return;

            Camera worldCam = _sliderCanvas != null && _sliderCanvas.worldCamera != null
                ? _sliderCanvas.worldCamera
                : Camera.main;
            if (worldCam == null)
                return;

            Vector3 screen = worldCam.WorldToScreenPoint(head.position + _sliderOffset);

            // Точка за камерой — прячем слайдер, чтобы он не «прыгал» по экрану.
            if (screen.z < 0f)
            {
                HideSlider();
                return;
            }

            if (!_sliderPrefab.activeSelf)
                _sliderPrefab.SetActive(true);

            if (!(_sliderRect.parent is RectTransform parentRect))
            {
                _sliderRect.position = screen;
                return;
            }

            Camera uiCam = _sliderCanvas != null && _sliderCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? _sliderCanvas.worldCamera
                : null;

            // Переводим экранную точку в мировую на плоскости канваса — корректно при любых якорях/пивоте.
            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(parentRect, screen, uiCam, out Vector3 world))
                _sliderRect.position = world;
        }

        /// <summary>Пересчитать слоты и подвинуть всех в очереди вперёд.</summary>
        private void AdvanceLine()
        {
            for (int i = 0; i < _line.Count; i++)
            {
                if (_line[i] != null)
                    _line[i].SetQueueTarget(GetSlotPosition(i), i == 0);
            }
        }

        private Vector3 GetSlotPosition(int index)
        {
            Vector3 cashier = _cashierPoint != null ? _cashierPoint.position : transform.position;

            if (index <= 0)
                return cashier;

            int waitIndex = index - 1;
            if (_queuePoints != null && waitIndex < _queuePoints.Count && _queuePoints[waitIndex] != null)
                return _queuePoints[waitIndex].position;

            Vector3 dir = _queueDirection.sqrMagnitude > 0.0001f ? _queueDirection.normalized : Vector3.back;
            return cashier + dir * (_queueSpacing * index);
        }

        private void OnValidate()
        {
            _queueSpacing = Mathf.Max(0.1f, _queueSpacing);
            _serviceDuration = Mathf.Max(0.1f, _serviceDuration);
        }

        private void OnDrawGizmosSelected()
        {
            if (_cashierPoint == null)
                return;

            Gizmos.color = Color.cyan;
            Vector3 prev = _cashierPoint.position;
            Gizmos.DrawWireCube(prev, Vector3.one * 0.3f);

            int slots = Mathf.Max(_queuePoints.Count, 4);
            for (int i = 1; i <= slots; i++)
            {
                Vector3 p = GetSlotPosition(i);
                Gizmos.DrawWireSphere(p, 0.25f);
                Gizmos.DrawLine(prev, p);
                prev = p;
            }
        }
    }
}
