using UnityEngine;

namespace StoreSim.NPC
{
    /// <summary>
    /// Спавнер покупателей нового цикла. Создаёт префаб с <see cref="CustomerController"/>
    /// и запускает цикл через Initialize(entry, exit). Движение у покупателя кастомное (NpcMover).
    /// Держит лимит одновременно активных покупателей.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("StoreSim/Customer Spawner")]
    public class CustomerSpawner : MonoBehaviour
    {
        [Header("Префабы")]
        [SerializeField] private GameObject[] _customerPrefabs;

        [Header("Точки")]
        [SerializeField] private Transform _spawnPoint;
        [Tooltip("Точка входа в магазин (за дверью). Может быть пустой.")]
        [SerializeField] private Transform _entryPoint;
        [Tooltip("Точка выхода для деспавна. Может быть пустой.")]
        [SerializeField] private Transform _exitPoint;

        [Header("Параметры спавна")]
        [SerializeField] private bool _spawnOnStart = true;
        [SerializeField] private float _spawnInterval = 5f;
        [SerializeField] private int _maxActive = 10;

        private int _activeCount;
        private float _timer;

        private void Start()
        {
            if (_spawnOnStart)
                TrySpawn();
        }

        private void Update()
        {
            if (_spawnInterval <= 0f || _activeCount >= _maxActive)
                return;

            _timer += Time.deltaTime;
            if (_timer >= _spawnInterval)
            {
                _timer = 0f;
                TrySpawn();
            }
        }

        public void TrySpawn()
        {
            if (_activeCount >= _maxActive)
                return;

            if (_customerPrefabs == null || _customerPrefabs.Length == 0)
            {
                Debug.LogWarning("CustomerSpawner: назначь хотя бы один префаб покупателя.", this);
                return;
            }

            if (_spawnPoint == null)
            {
                Debug.LogWarning("CustomerSpawner: не задана точка спавна.", this);
                return;
            }

            GameObject prefab = _customerPrefabs[Random.Range(0, _customerPrefabs.Length)];
            if (prefab == null)
                return;

            GameObject go = Instantiate(prefab, _spawnPoint.position, _spawnPoint.rotation);

            if (!go.TryGetComponent(out CustomerController customer))
                customer = go.AddComponent<CustomerController>();

            customer.OnDespawned += HandleDespawn;
            _activeCount++;

            customer.Initialize(_entryPoint, _exitPoint);
        }

        private void HandleDespawn(CustomerController customer)
        {
            customer.OnDespawned -= HandleDespawn;
            _activeCount = Mathf.Max(0, _activeCount - 1);
        }
    }
}
