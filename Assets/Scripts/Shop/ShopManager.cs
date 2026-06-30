using System.Collections.Generic;
using UnityEngine;

namespace StoreSim.Shop
{
    /// <summary>
    /// Хранит список машин магазина (List&lt;CarShopItem&gt;) и выдаёт NPC случайную свободную,
    /// сразу бронируя её, чтобы два покупателя не пошли к одной машине.
    /// Машины регистрируются автоматически (CarShopItem.OnEnable) либо назначаются в инспекторе.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("StoreSim/Shop Manager")]
    public class ShopManager : MonoBehaviour
    {
        public static ShopManager Instance { get; private set; }

        // Машины, успевшие зарегистрироваться до создания менеджера, копятся здесь.
        private static readonly List<CarShopItem> _pending = new();

        [Tooltip("Можно оставить пустым — тогда машины подтянутся со сцены автоматически.")]
        [SerializeField] private List<CarShopItem> _cars = new();

        private readonly List<CarShopItem> _buffer = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            // Подхватываем уже существующие в сцене машины.
            if (_cars.Count == 0)
                _cars.AddRange(FindObjectsByType<CarShopItem>(FindObjectsSortMode.None));

            // И тех, кто зарегистрировался раньше нас.
            for (int i = 0; i < _pending.Count; i++)
            {
                if (_pending[i] != null && !_cars.Contains(_pending[i]))
                    _cars.Add(_pending[i]);
            }
            _pending.Clear();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public static void RegisterCar(CarShopItem car)
        {
            if (car == null)
                return;

            if (Instance != null)
            {
                if (!Instance._cars.Contains(car))
                    Instance._cars.Add(car);
            }
            else if (!_pending.Contains(car))
            {
                _pending.Add(car);
            }
        }

        public static void UnregisterCar(CarShopItem car)
        {
            if (car == null)
                return;

            if (Instance != null)
                Instance._cars.Remove(car);
            _pending.Remove(car);
        }

        /// <summary>Сколько всего машин сейчас в магазине (зарегистрировано и не продано).</summary>
        public int CarCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < _cars.Count; i++)
                {
                    if (_cars[i] != null)
                        count++;
                }
                return count;
            }
        }

        public bool HasAvailableCar()
        {
            for (int i = 0; i < _cars.Count; i++)
            {
                if (_cars[i] != null && _cars[i].IsAvailable)
                    return true;
            }
            return false;
        }

        /// <summary>Возвращает случайную свободную машину, уже забронированную за вызывающим NPC, либо null.</summary>
        public CarShopItem ReserveRandomAvailableCar()
        {
            _buffer.Clear();
            for (int i = 0; i < _cars.Count; i++)
            {
                CarShopItem car = _cars[i];
                if (car != null && car.IsAvailable)
                    _buffer.Add(car);
            }

            while (_buffer.Count > 0)
            {
                int index = Random.Range(0, _buffer.Count);
                CarShopItem candidate = _buffer[index];

                if (candidate != null && candidate.TryReserve())
                    return candidate;

                _buffer.RemoveAt(index);
            }

            return null;
        }
    }
}
