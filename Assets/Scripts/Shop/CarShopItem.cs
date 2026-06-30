using System;
using UnityEngine;

namespace StoreSim.Shop
{
    /// <summary>
    /// Компонент машины в магазине. Висит на префабе/объекте машины.
    /// Содержит точку подхода NPC (дочерний куб), цену и состояние брони/продажи.
    /// Машина может быть забронирована одним NPC, чтобы другие к ней не шли.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("StoreSim/Car Shop Item")]
    public class CarShopItem : MonoBehaviour
    {
        [Header("Точка подхода NPC (дочерний куб)")]
        [SerializeField] private Transform _approachPoint;

        [Header("Цена")]
        [Tooltip("Если задать фиксированную цену > 0 — будет использоваться она. Иначе берётся случайная из диапазона.")]
        [SerializeField] private int _fixedPrice;
        [SerializeField] private int _minPrice = 100;
        [SerializeField] private int _maxPrice = 500;

        [Header("После продажи")]
        [SerializeField] private bool _despawnOnSold = true;
        [SerializeField] private float _despawnDelay = 0.4f;

        /// <summary>Точка, к которой подходит NPC. Если не задана — сам объект машины.</summary>
        public Transform ApproachPoint => _approachPoint != null ? _approachPoint : transform;

        /// <summary>Машина уже куплена.</summary>
        public bool IsSold { get; private set; }

        /// <summary>Машину уже выбрал (забронировал) какой-то NPC.</summary>
        public bool IsReserved { get; private set; }

        /// <summary>Машина свободна и к ней можно идти.</summary>
        public bool IsAvailable => !IsSold && !IsReserved;

        public int Price { get; private set; }

        /// <summary>Вызывается в момент продажи (до возможного деспавна).</summary>
        public event Action<CarShopItem> OnSold;

        private void Awake()
        {
            Price = _fixedPrice > 0
                ? _fixedPrice
                : UnityEngine.Random.Range(_minPrice, _maxPrice + 1);
        }

        private void OnEnable() => ShopManager.RegisterCar(this);

        private void OnDisable() => ShopManager.UnregisterCar(this);

        /// <summary>Атомарно бронирует машину за NPC. Возвращает false, если уже занята/продана.</summary>
        public bool TryReserve()
        {
            if (!IsAvailable)
                return false;

            IsReserved = true;
            return true;
        }

        /// <summary>Снять бронь (например, если NPC исчез не купив).</summary>
        public void ReleaseReservation()
        {
            if (!IsSold)
                IsReserved = false;
        }

        /// <summary>Пометить как проданную: снимает с витрины и при необходимости деспавнит.</summary>
        public void Sell()
        {
            if (IsSold)
                return;

            IsSold = true;
            IsReserved = false;

            OnSold?.Invoke(this);
            ShopManager.UnregisterCar(this);

            if (_despawnOnSold)
                Destroy(gameObject, _despawnDelay);
        }

        private void OnValidate()
        {
            _minPrice = Mathf.Max(0, _minPrice);
            _maxPrice = Mathf.Max(_minPrice, _maxPrice);
            _despawnDelay = Mathf.Max(0f, _despawnDelay);
        }
    }
}
