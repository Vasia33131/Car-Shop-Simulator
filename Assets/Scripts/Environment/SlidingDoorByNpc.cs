using StoreSim.NPC;
using UnityEngine;

namespace StoreSim.Environment
{
    [DisallowMultipleComponent]
    public class SlidingDoorByNpc : MonoBehaviour
    {
        [Header("Door Leaves")]
        [SerializeField] private Transform _leftDoor;
        [SerializeField] private Transform _rightDoor;
        [SerializeField] private Vector3 _slideAxis = Vector3.right;
        [SerializeField] private float _slideDistance = 1.5f;
        [SerializeField] private float _slideSpeed = 2.5f;

        [Header("NPC Detection")]
        [SerializeField] private Transform _detectionCenter;
        [SerializeField] private float _detectionRadius = 2.5f;
        [SerializeField] private bool _ignoreHeight = true;

        private Vector3 _leftClosedLocalPos;
        private Vector3 _rightClosedLocalPos;
        private Vector3 _leftOpenLocalPos;
        private Vector3 _rightOpenLocalPos;

        private void Awake() => CacheDoorPositions();

        private void Update()
        {
            if (_leftDoor == null || _rightDoor == null)
                return;

            bool open = IsAnyNpcNear();
            float step = _slideSpeed * Time.deltaTime;

            _leftDoor.localPosition = Vector3.MoveTowards(
                _leftDoor.localPosition, open ? _leftOpenLocalPos : _leftClosedLocalPos, step);
            _rightDoor.localPosition = Vector3.MoveTowards(
                _rightDoor.localPosition, open ? _rightOpenLocalPos : _rightClosedLocalPos, step);
        }

        private bool IsAnyNpcNear()
        {
            Vector3 center = _detectionCenter != null ? _detectionCenter.position : transform.position;
            float radiusSqr = _detectionRadius * _detectionRadius;
            var npcs = NpcActorBase.Active;

            for (int i = 0; i < npcs.Count; i++)
            {
                NpcActorBase npc = npcs[i];
                if (npc == null)
                    continue;

                Vector3 delta = npc.transform.position - center;
                if (_ignoreHeight)
                    delta.y = 0f;

                if (delta.sqrMagnitude <= radiusSqr)
                    return true;
            }

            return false;
        }

        private void CacheDoorPositions()
        {
            if (_leftDoor == null || _rightDoor == null)
                return;

            _leftClosedLocalPos = _leftDoor.localPosition;
            _rightClosedLocalPos = _rightDoor.localPosition;

            Vector3 axis = _slideAxis.sqrMagnitude > 0.0001f ? _slideAxis.normalized : Vector3.right;
            Vector3 shift = axis * _slideDistance;

            _leftOpenLocalPos = _leftClosedLocalPos + shift;
            _rightOpenLocalPos = _rightClosedLocalPos - shift;
        }

        private void OnValidate()
        {
            _slideDistance = Mathf.Max(0f, _slideDistance);
            _slideSpeed = Mathf.Max(0f, _slideSpeed);
            _detectionRadius = Mathf.Max(0f, _detectionRadius);
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 center = _detectionCenter != null ? _detectionCenter.position : transform.position;
            Gizmos.color = new Color(0f, 1f, 0f, 0.35f);
            Gizmos.DrawWireSphere(center, _detectionRadius);
        }
    }
}
