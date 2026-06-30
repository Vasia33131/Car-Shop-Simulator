using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace StoreSim.NPC
{
    public class AiMoveToPoint : NpcActorBase
    {
        [SerializeField] private NavMeshAgent _meshAgent;
        [SerializeField] private List<Transform> _wayPoints;
        [SerializeField] private float _stopTime = 2f;
        [SerializeField] private float _stoppingDistance = 0.5f;

        private int _waypointIndex;
        private float _waitTimer;
        private bool _isWaiting;

        private void Start()
        {
            if (_wayPoints != null && _wayPoints.Count > 0)
                _meshAgent.SetDestination(_wayPoints[0].position);
        }

        private void Update()
        {
            if (_wayPoints == null || _wayPoints.Count == 0 || !_meshAgent.isActiveAndEnabled)
                return;

            if (_isWaiting)
            {
                _waitTimer -= Time.deltaTime;
                if (_waitTimer <= 0f)
                {
                    _isWaiting = false;
                    MoveToNextPoint();
                }

                return;
            }

            if (!_meshAgent.pathPending && _meshAgent.remainingDistance <= _stoppingDistance)
            {
                _isWaiting = true;
                _waitTimer = _stopTime;
            }
        }

        private void MoveToNextPoint()
        {
            _waypointIndex = (_waypointIndex + 1) % _wayPoints.Count;
            _meshAgent.SetDestination(_wayPoints[_waypointIndex].position);
        }
    }
}
