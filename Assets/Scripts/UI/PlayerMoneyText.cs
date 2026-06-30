using TMPro;
using UnityEngine;

namespace StoreSim.UI
{
    /// <summary>
    /// Простой счётчик денег: хранит сумму и пишет её в TMP-текст на канвасе.
    /// Касса вызывает <see cref="Add"/> с ценой машины — значение прибавляется и текст обновляется.
    /// Никакого EconomyManager/ScriptableObject — только этот текст.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("StoreSim/Player Money Text")]
    public class PlayerMoneyText : MonoBehaviour
    {
        [Tooltip("Текст баланса на канвасе (TextMeshPro UGUI).")]
        [SerializeField] private TMP_Text _text;

        [Tooltip("Стартовая сумма.")]
        [SerializeField] private int _startAmount;

        [Tooltip("Формат строки. {0} = сумма. Например: \"${0}\" или \"Баланс: {0}\".")]
        [SerializeField] private string _format = "${0}";

        public int Amount { get; private set; }

        private void Reset() => _text = GetComponent<TMP_Text>();

        private void Awake()
        {
            Amount = _startAmount;
            Refresh();
        }

        /// <summary>Прибавить значение (например цену машины) и обновить текст.</summary>
        public void Add(int value)
        {
            Amount += value;
            Refresh();
        }

        public void SetAmount(int value)
        {
            Amount = value;
            Refresh();
        }

        private void Refresh()
        {
            if (_text == null)
                return;

            _text.text = string.IsNullOrEmpty(_format)
                ? Amount.ToString()
                : string.Format(_format, Amount);
        }
    }
}
