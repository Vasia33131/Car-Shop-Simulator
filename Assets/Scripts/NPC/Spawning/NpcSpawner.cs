using System.Collections.Generic;
using StoreSim.Shop;
using UnityEngine;

namespace StoreSim.NPC
{
    [DisallowMultipleComponent]
    [AddComponentMenu("AI/NPC Spawner")]
    public class NpcSpawner : MonoBehaviour
    {
        [Header("Spawn Point 1")]
        [SerializeField] private Transform _spawnPoint;
        [SerializeField] private Transform _mandatoryPoint1;
        [SerializeField] private Transform _mandatoryPoint2;

        [Header("Spawn Point 2")]
        [SerializeField] private Transform _spawnPoint2;
        [SerializeField] private Transform _spawn2MandatoryPoint1;
        [SerializeField] private Transform _spawn2MandatoryPoint2;

        [Header("Spawn")]
        [SerializeField] private GameObject[] _npcPrefabs;
        [SerializeField] private bool _spawnOnStart = true;
        [SerializeField] private float _spawnInterval = 5f;
        [Tooltip("Запасной лимит NPC, если в сцене нет ShopManager. Когда ShopManager есть — лимит = числу машин в магазине.")]
        [SerializeField] private int _maxNpcCount = 10;

        [Header("Route")]
        [SerializeField] private Transform[] _randomDestinations;

        [Header("Movement")]
        [SerializeField] private float _moveSpeed = 2f;
        [SerializeField] private float _rotationSpeed = 10f;
        [SerializeField] private float _stoppingDistance = 0.1f;

        [Header("Ground")]
        [SerializeField] private LayerMask _groundMask;
        [SerializeField] private float _groundCheckHeight = 2f;
        [SerializeField] private float _groundCheckDistance = 10f;
        [SerializeField] private float _groundOffset;

        private float _spawnTimer;
        private int _spawnedCount;

        /// <summary>
        /// Сколько NPC можно держать активными одновременно: ровно столько,
        /// сколько сейчас машин в магазине. Если ShopManager нет — запасной _maxNpcCount.
        /// </summary>
        private int MaxNpcCount =>
            ShopManager.Instance != null ? ShopManager.Instance.CarCount : _maxNpcCount;

        private void Start()
        {
            if (_spawnOnStart)
                SpawnNpc();
        }

        private void Update()
        {
            if (_spawnInterval <= 0f || _spawnedCount >= MaxNpcCount)
                return;

            _spawnTimer += Time.deltaTime;
            if (_spawnTimer >= _spawnInterval)
            {
                _spawnTimer = 0f;
                SpawnNpc();
            }
        }

        public void SpawnNpc()
        {
            if (_spawnedCount >= MaxNpcCount)
                return;

            GameObject prefab = TransformUtils.PickRandom(_npcPrefabs);
            if (prefab == null)
            {
                Debug.LogWarning("NpcSpawner: assign at least one NPC prefab.", this);
                return;
            }

            if (!TryGetRandomSpawnSetup(out SpawnSetup setup))
            {
                Debug.LogWarning("NpcSpawner: assign at least one spawn point.", this);
                return;
            }

            GameObject npc = Instantiate(prefab, setup.SpawnPoint.position, setup.SpawnPoint.rotation);

            // Новый цикл покупателя: вход (mandatory-точки) -> машина -> касса -> оплата -> выход.
            if (npc.TryGetComponent(out CustomerController customer))
            {
                customer.ApplyMovementSettings(BuildMovementSettings());
                customer.OnDespawned += HandleCustomerDespawned;
                customer.Initialize(
                    new[] { setup.MandatoryPoint1, setup.MandatoryPoint2 },
                    setup.SpawnPoint);

                _spawnedCount++;
                return;
            }

            // Фолбэк: старое блуждание по случайным точкам.
            if (!npc.TryGetComponent(out NpcRoute route))
                route = npc.AddComponent<NpcRoute>();

            route.Initialize(
                new[] { setup.MandatoryPoint1, setup.MandatoryPoint2 },
                _randomDestinations,
                BuildMovementSettings());

            _spawnedCount++;
        }

        private void HandleCustomerDespawned(CustomerController customer)
        {
            customer.OnDespawned -= HandleCustomerDespawned;
            _spawnedCount = Mathf.Max(0, _spawnedCount - 1);
        }

        private NpcMovementSettings BuildMovementSettings() => new()
        {
            MoveSpeed = _moveSpeed,
            RotationSpeed = _rotationSpeed,
            StoppingDistance = _stoppingDistance,
            GroundMask = _groundMask,
            GroundCheckHeight = _groundCheckHeight,
            GroundCheckDistance = _groundCheckDistance,
            GroundOffset = _groundOffset
        };

        private bool TryGetRandomSpawnSetup(out SpawnSetup setup)
        {
            var options = new List<SpawnSetup>(2);
            if (_spawnPoint != null)
                options.Add(new SpawnSetup(_spawnPoint, _mandatoryPoint1, _mandatoryPoint2));
            if (_spawnPoint2 != null)
                options.Add(new SpawnSetup(_spawnPoint2, _spawn2MandatoryPoint1, _spawn2MandatoryPoint2));

            if (options.Count == 0)
            {
                setup = default;
                return false;
            }

            setup = options[Random.Range(0, options.Count)];
            return true;
        }

        private readonly struct SpawnSetup
        {
            public readonly Transform SpawnPoint;
            public readonly Transform MandatoryPoint1;
            public readonly Transform MandatoryPoint2;

            public SpawnSetup(Transform spawnPoint, Transform mandatoryPoint1, Transform mandatoryPoint2)
            {
                SpawnPoint = spawnPoint;
                MandatoryPoint1 = mandatoryPoint1;
                MandatoryPoint2 = mandatoryPoint2;
            }
        }
    }
}
