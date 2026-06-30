using System;
using UnityEngine;

namespace StoreSim.Economy
{
    /// <summary>
    /// Опциональное хранилище баланса в виде ScriptableObject.
    /// Если такой ассет назначен в <see cref="EconomyManager"/>, баланс живёт в нём,
    /// иначе менеджер использует своё runtime-поле. Это закрывает оба требования:
    /// Singleton-доступ и хранение баланса в ScriptableObject.
    /// </summary>
    [CreateAssetMenu(menuName = "StoreSim/Player Balance", fileName = "PlayerBalance")]
    public class PlayerBalance : ScriptableObject
    {
        [SerializeField] private int _startingAmount;
        [Tooltip("Сбрасывать баланс к стартовому при входе в Play Mode (ассет не сохраняет прогресс между сессиями).")]
        [SerializeField] private bool _resetOnEnable = true;

        [NonSerialized] private int _amount;
        [NonSerialized] private bool _initialized;

        public event Action<int> OnChanged;

        public int Amount
        {
            get
            {
                EnsureInitialized();
                return _amount;
            }
        }

        private void OnEnable()
        {
            _initialized = false;
            if (_resetOnEnable)
            {
                _amount = _startingAmount;
                _initialized = true;
            }
        }

        public void Add(int value)
        {
            EnsureInitialized();
            SetAmount(_amount + value);
        }

        public bool TrySpend(int value)
        {
            EnsureInitialized();
            if (value < 0 || _amount < value)
                return false;

            SetAmount(_amount - value);
            return true;
        }

        public void SetAmount(int value)
        {
            EnsureInitialized();
            if (_amount == value)
                return;

            _amount = value;
            OnChanged?.Invoke(_amount);
        }

        private void EnsureInitialized()
        {
            if (_initialized)
                return;

            _amount = _startingAmount;
            _initialized = true;
        }
    }
}
