using StoreSim.Economy;
using TMPro;
using UnityEngine;

namespace StoreSim.UI
{
    /// <summary>
    /// Показывает текущий баланс игрока в TMP-тексте на Canvas.
    /// Перетащи сюда ссылку на текст (TextMeshProUGUI) из своего канваса.
    /// Подписывается на <see cref="EconomyManager.OnBalanceChanged"/> и обновляется автоматически.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("StoreSim/Balance Display")]
    public class BalanceDisplay : MonoBehaviour
    {
        [Tooltip("Ссылка на текст баланса на канвасе (TextMeshPro UGUI).")]
        [SerializeField] private TMP_Text _balanceText;

        [Tooltip("Формат строки. {0} = сумма. Например: \"${0}\" или \"Баланс: {0}\".")]
        [SerializeField] private string _format = "${0}";

        private void Reset() => _balanceText = GetComponent<TMP_Text>();

        private void OnEnable()
        {
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.OnBalanceChanged += UpdateText;
                UpdateText(EconomyManager.Instance.Balance);
            }
        }

        private void Start()
        {
            // На случай, если EconomyManager инициализировался позже этого объекта.
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.OnBalanceChanged -= UpdateText;
                EconomyManager.Instance.OnBalanceChanged += UpdateText;
                UpdateText(EconomyManager.Instance.Balance);
            }
        }

        private void OnDisable()
        {
            if (EconomyManager.Instance != null)
                EconomyManager.Instance.OnBalanceChanged -= UpdateText;
        }

        private void UpdateText(int balance)
        {
            if (_balanceText == null)
                return;

            _balanceText.text = string.IsNullOrEmpty(_format)
                ? balance.ToString()
                : string.Format(_format, balance);
        }
    }
}
