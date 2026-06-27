using System;
using UnityEngine;

public class InputSystem : MonoBehaviour
{
    public event Action<Vector2> OnDrag;
    public event Action OnDragEnd;

    private bool _isHolding;

    private void Update()
    {
        bool isMouseDown = Input.GetMouseButton(0);

        if (isMouseDown)
        {
            OnDrag?.Invoke(Input.mousePosition);
        }
        else if (_isHolding)
        {
            OnDragEnd?.Invoke();
        }

        _isHolding = isMouseDown;
    }
}