using UnityEngine;

namespace StoreSim.CameraSystem
{
    public class CameraMovement : MonoBehaviour
    {
        [SerializeField] private float _moveSpeed = 10f;
        [SerializeField] private float _sensitivity = 1f;
        [SerializeField] private InputSystem _inputSystem;

        private Vector3 _lastMousePosition;
        private bool _isDragging;

        private void OnEnable()
        {
            if (_inputSystem == null)
                return;

            _inputSystem.OnDrag += MoveCamera;
            _inputSystem.OnDragEnd += StopDrag;
        }

        private void OnDisable()
        {
            if (_inputSystem == null)
                return;

            _inputSystem.OnDrag -= MoveCamera;
            _inputSystem.OnDragEnd -= StopDrag;
        }

        private void MoveCamera(Vector2 mousePosition)
        {
            if (!_isDragging)
            {
                _isDragging = true;
                _lastMousePosition = mousePosition;
                return;
            }

            float deltaY = mousePosition.y - _lastMousePosition.y;
            transform.Translate(0f, 0f, -deltaY * _moveSpeed * _sensitivity * Time.deltaTime, Space.World);
            _lastMousePosition = mousePosition;
        }

        private void StopDrag() => _isDragging = false;
    }
}
