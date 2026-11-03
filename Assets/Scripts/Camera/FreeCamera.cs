using UnityEngine;
using UnityEngine.InputSystem;

// Editor-style fly camera for inspecting the map while the simulation runs.
// Hold the right mouse button to look around; WASD moves, Q/E go down/up, Shift is fast, the wheel changes speed.
public class FreeCamera : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float _moveSpeed = 15f;
    [SerializeField] private float _fastMultiplier = 3f;
    [SerializeField] private float _minSpeed = 1f;
    [SerializeField] private float _maxSpeed = 100f;
    [SerializeField] private float _scrollSpeedStep = 2f;

    [Header("Look")]
    [SerializeField] private float _lookSensitivity = 0.15f;

    private float _yaw;
    private float _pitch;

    private void Start()
    {
        Vector3 angles = transform.eulerAngles;
        _yaw = angles.y;
        _pitch = angles.x > 180f ? angles.x - 360f : angles.x;
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;
        if (keyboard == null || mouse == null) return;

        bool looking = mouse.rightButton.isPressed;
        Cursor.lockState = looking ? CursorLockMode.Locked : CursorLockMode.None;

        if (looking) Look(mouse.delta.ReadValue());

        float scroll = mouse.scroll.ReadValue().y;
        if (scroll != 0f)
            _moveSpeed = Mathf.Clamp(_moveSpeed + Mathf.Sign(scroll) * _scrollSpeedStep, _minSpeed, _maxSpeed);

        Move(keyboard);
    }

    private void Look(Vector2 delta)
    {
        _yaw += delta.x * _lookSensitivity;
        _pitch = Mathf.Clamp(_pitch - delta.y * _lookSensitivity, -89f, 89f);
        transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
    }

    private void Move(Keyboard keyboard)
    {
        float forward = Axis(keyboard.wKey, keyboard.sKey);
        float right = Axis(keyboard.dKey, keyboard.aKey);
        float up = Axis(keyboard.eKey, keyboard.qKey);

        Vector3 direction = transform.forward * forward + transform.right * right + Vector3.up * up;
        if (direction == Vector3.zero) return;

        float speed = _moveSpeed * (keyboard.leftShiftKey.isPressed ? _fastMultiplier : 1f);

        // Unscaled so the camera stays usable if the simulation is paused or slowed.
        transform.position += direction.normalized * (speed * Time.unscaledDeltaTime);
    }

    private static float Axis(UnityEngine.InputSystem.Controls.KeyControl positive, UnityEngine.InputSystem.Controls.KeyControl negative)
    {
        return (positive.isPressed ? 1f : 0f) - (negative.isPressed ? 1f : 0f);
    }

    private void OnDisable()
    {
        Cursor.lockState = CursorLockMode.None;
    }
}
