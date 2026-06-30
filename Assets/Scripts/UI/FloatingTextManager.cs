using System.Collections.Generic;
using UnityEngine;

namespace StoreSim.UI
{
    /// <summary>
    /// Менеджер всплывающего текста. Метод <see cref="ShowFloatingText"/> спавнит готовый префаб
    /// (с компонентом <see cref="FloatingText"/>) в мировой позиции. Использует простой пул объектов
    /// при включённом <see cref="_usePooling"/>, иначе просто Instantiate/Destroy.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("StoreSim/Floating Text Manager")]
    public class FloatingTextManager : MonoBehaviour
    {
        public static FloatingTextManager Instance { get; private set; }

        [Tooltip("Префаб всплывающего текста с компонентом FloatingText.")]
        [SerializeField] private FloatingText _prefab;

        [Tooltip("Необязательный родитель для заспавненных текстов.")]
        [SerializeField] private Transform _parent;

        [SerializeField] private bool _usePooling;
        [SerializeField] private int _prewarm = 8;

        private readonly Queue<FloatingText> _pool = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (_usePooling && _prefab != null)
            {
                for (int i = 0; i < _prewarm; i++)
                {
                    FloatingText item = Instantiate(_prefab, _parent);
                    item.gameObject.SetActive(false);
                    _pool.Enqueue(item);
                }
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        /// <summary>Показать всплывающий текст в мировой позиции.</summary>
        public void ShowFloatingText(Vector3 worldPosition, string text)
        {
            if (_prefab == null)
            {
                Debug.LogWarning("FloatingTextManager: не назначен префаб текста.", this);
                return;
            }

            FloatingText instance;
            if (_usePooling && _pool.Count > 0)
            {
                instance = _pool.Dequeue();
                instance.transform.SetPositionAndRotation(worldPosition, Quaternion.identity);
                instance.gameObject.SetActive(true);
            }
            else
            {
                instance = Instantiate(_prefab, worldPosition, Quaternion.identity, _parent);
            }

            instance.Show(text);
        }
    }
}
