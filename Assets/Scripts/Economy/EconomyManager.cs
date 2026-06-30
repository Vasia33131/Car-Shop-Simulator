using System;
using UnityEngine;

namespace StoreSim.Economy
{
    /// <summary>
    /// Singleton-доступ к балансу игрока. UI подписывается на <see cref="OnBalanceChanged"/>.
    /// Если назначен <see cref="PlayerBalance"/> ассет — баланс хранится в нём,
    /// иначе используется внутреннее runtime-поле.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("StoreSim/Economy Manager")]
    public class EconomyManager : MonoBehaviour
    {
        public static EconomyManager Instance { get; private set; }

        [Tooltip("Необязательно. Если назначить ScriptableObject, баланс будет храниться в нём.")]
        [SerializeField] private PlayerBalance _balanceAsset;
        [Tooltip("Стартовый баланс, если ассет PlayerBalance не назначен.")]
        [SerializeField] private int _startingBalance;

        private int _runtimeBalance;

        /// <summary>Вызывается при любом изменении баланса (текущее значение).</summary>
        public event Action<int> OnBalanceChanged;

        public int Balance => _balanceAsset != null ? _balanceAsset.Amount : _runtimeBalance;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (_balanceAsset != null)
                _balanceAsset.OnChanged += HandleAssetChanged;
            else
                _runtimeBalance = _startingBalance;
        }

        private void Start()
        {
            // Дать подписчикам (UI) актуальное значение на старте.
            OnBalanceChanged?.Invoke(Balance);
        }

        private void OnDestroy()
        {
            if (_balanceAsset != null)
                _balanceAsset.OnChanged -= HandleAssetChanged;

            if (Instance == this)
                Instance = null;
        }

        public void AddMoney(int amount)
        {
            if (amount == 0)
                return;

            if (_balanceAsset != null)
            {
                _balanceAsset.Add(amount);
                return;
            }

            _runtimeBalance += amount;
            OnBalanceChanged?.Invoke(_runtimeBalance);
        }

        public bool TrySpend(int amount)
        {
            if (_balanceAsset != null)
                return _balanceAsset.TrySpend(amount);

            if (amount < 0 || _runtimeBalance < amount)
                return false;

            _runtimeBalance -= amount;
            OnBalanceChanged?.Invoke(_runtimeBalance);
            return true;
        }

        private void HandleAssetChanged(int value) => OnBalanceChanged?.Invoke(value);
    }
}
