using UnityEngine;
using UnityEngine.InputSystem;

public class TiltGravityController : MonoBehaviour
{
    [SerializeField] private float gravityStrength = 15f;
    [SerializeField] private float sensitivity = 2.5f;
    [SerializeField] private float deadZone = 0.05f;

    private Vector2 tiltInput;

    private void OnEnable()
    {
        if (Accelerometer.current != null)
        {
            InputSystem.EnableDevice(Accelerometer.current);
        }
    }

    private void OnDisable()
    {
        if (Accelerometer.current != null)
        {
            InputSystem.DisableDevice(Accelerometer.current);
        }
    }

    private void Update()
    {
#if UNITY_EDITOR
        float x = 0f;
        float y = -1f;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) x = -1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) x = 1f;
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) y = 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) y = -1f;
        }

        tiltInput = new Vector2(x, y);
#else
        if (Accelerometer.current != null)
        {
            Vector3 acc = Accelerometer.current.acceleration.ReadValue();

            tiltInput = new Vector2(
                acc.x * sensitivity,
                acc.y * sensitivity
            );

            tiltInput = Vector2.ClampMagnitude(tiltInput, 1f);
        }
#endif

        if (Mathf.Abs(tiltInput.x) < deadZone) tiltInput.x = 0f;
        if (Mathf.Abs(tiltInput.y) < deadZone) tiltInput.y = 0f;
    }

    private void FixedUpdate()
    {
        Physics2D.gravity = tiltInput * gravityStrength;
    }
}