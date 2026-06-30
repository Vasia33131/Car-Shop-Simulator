using UnityEngine;

namespace StoreSim.NPC
{
    public class NpcMover
    {
        // Общий буфер для всех NPC: касты выполняются синхронно в главном потоке,
        // результат потребляется сразу, поэтому переиспользование безопасно и не аллоцирует.
        private static readonly RaycastHit[] _castBuffer = new RaycastHit[16];

        private readonly Transform _transform;
        private readonly Collider _bodyCollider;

        private float _moveSpeed;
        private LayerMask _groundMask;
        private LayerMask _obstacleMask;
        private float _groundCheckHeight;
        private float _groundCheckDistance;
        private float _groundOffset;
        private float _collisionPadding;
        private float _fallbackHeight;
        private float _fallbackRadius;

        public bool IsMoving { get; private set; }

        public NpcMover(Transform transform, Collider bodyCollider)
        {
            _transform = transform;
            _bodyCollider = bodyCollider;
        }

        public void Configure(
            NpcMovementSettings movement,
            LayerMask obstacleMask,
            float collisionPadding,
            float fallbackHeight,
            float fallbackRadius)
        {
            _moveSpeed = movement.MoveSpeed;
            _groundMask = movement.GroundMask;
            _groundCheckHeight = movement.GroundCheckHeight;
            _groundCheckDistance = movement.GroundCheckDistance;
            _groundOffset = movement.GroundOffset;
            _obstacleMask = obstacleMask & ~movement.GroundMask;
            _collisionPadding = collisionPadding;
            _fallbackHeight = fallbackHeight;
            _fallbackRadius = fallbackRadius;
        }

        public void MoveTowards(Vector3 targetPosition)
        {
            Vector3 current = _transform.position;
            Vector3 flatCurrent = new(current.x, 0f, current.z);
            Vector3 flatTarget = new(targetPosition.x, 0f, targetPosition.z);

            Vector3 nextFlat = Vector3.MoveTowards(flatCurrent, flatTarget, _moveSpeed * Time.deltaTime);
            Vector3 wanted = new(nextFlat.x, current.y, nextFlat.z);
            Vector3 resolved = ResolveWallCollision(current, wanted);
            _transform.position = resolved;

            Vector3 delta = resolved - current;
            delta.y = 0f;
            IsMoving = delta.sqrMagnitude > 0.00001f;

            SnapToGround();
        }

        public void SnapToGround()
        {
            Vector3 origin = _transform.position + Vector3.up * _groundCheckHeight;
            float maxDistance = _groundCheckHeight + _groundCheckDistance;

            if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit, maxDistance, _groundMask, QueryTriggerInteraction.Ignore))
                return;

            Vector3 position = _transform.position;
            position.y = hit.point.y + _groundOffset;
            _transform.position = position;
        }

        private Vector3 ResolveWallCollision(Vector3 current, Vector3 wanted)
        {
            Vector3 move = wanted - current;
            move.y = 0f;

            float distance = move.magnitude;
            if (distance < 0.0001f)
                return current;

            Vector3 direction = move / distance;

            if (!TryCapsuleCast(current, direction, distance, out RaycastHit hit))
                return wanted;

            float directStep = Mathf.Max(hit.distance - _collisionPadding, 0f);
            Vector3 stopBeforeWall = current + direction * Mathf.Min(directStep, distance);

            Vector3 slideDirection = Vector3.ProjectOnPlane(direction, hit.normal);
            slideDirection.y = 0f;
            if (slideDirection.sqrMagnitude < 0.0001f)
                return stopBeforeWall;

            slideDirection.Normalize();
            float remaining = Mathf.Max(distance - directStep, 0f);
            if (remaining <= 0f)
                return stopBeforeWall;

            if (!TryCapsuleCast(stopBeforeWall, slideDirection, remaining, out RaycastHit slideHit))
                return stopBeforeWall + slideDirection * remaining;

            float slideStep = Mathf.Max(slideHit.distance - _collisionPadding, 0f);
            return stopBeforeWall + slideDirection * Mathf.Min(slideStep, remaining);
        }

        private bool TryCapsuleCast(Vector3 position, Vector3 direction, float distance, out RaycastHit bestHit)
        {
            BuildCapsule(position, out Vector3 point1, out Vector3 point2, out float radius);
            int hitCount = Physics.CapsuleCastNonAlloc(
                point1, point2, radius, direction, _castBuffer, distance + _collisionPadding,
                _obstacleMask, QueryTriggerInteraction.Ignore);

            bestHit = default;
            float closest = float.MaxValue;
            bool found = false;

            for (int i = 0; i < hitCount; i++)
            {
                Collider hitCollider = _castBuffer[i].collider;
                if (hitCollider == null)
                    continue;

                if (_bodyCollider != null && (hitCollider == _bodyCollider || hitCollider.transform.IsChildOf(_transform)))
                    continue;

                if (_castBuffer[i].distance < closest)
                {
                    closest = _castBuffer[i].distance;
                    bestHit = _castBuffer[i];
                    found = true;
                }
            }

            return found;
        }

        private void BuildCapsule(Vector3 position, out Vector3 point1, out Vector3 point2, out float radius)
        {
            if (_bodyCollider != null)
            {
                Bounds bounds = _bodyCollider.bounds;
                radius = Mathf.Max(0.05f, Mathf.Min(bounds.extents.x, bounds.extents.z));

                float bottom = bounds.min.y + radius;
                float top = bounds.max.y - radius;
                if (top < bottom)
                    top = bottom;

                point1 = new Vector3(position.x, bottom, position.z);
                point2 = new Vector3(position.x, top, position.z);
                return;
            }

            radius = Mathf.Max(0.05f, _fallbackRadius);
            float height = Mathf.Max(_fallbackHeight, radius * 2f + 0.01f);
            point1 = position + Vector3.up * radius;
            point2 = position + Vector3.up * (height - radius);
        }
    }
}
