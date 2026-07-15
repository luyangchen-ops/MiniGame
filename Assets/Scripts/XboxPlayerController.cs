using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class XboxPlayerController : PlayerController
{
    protected override Vector2 ReadMoveInput()
    {
#if ENABLE_INPUT_SYSTEM
        return Gamepad.current != null
            ? Vector2.ClampMagnitude(Gamepad.current.leftStick.ReadValue(), 1f)
            : Vector2.zero;
#else
        return Vector2.ClampMagnitude(
            new Vector2(Input.GetAxisRaw("XboxHorizontal"), Input.GetAxisRaw("XboxVertical")),
            1f);
#endif
    }

    protected override bool WasLaunchPressed()
    {
#if ENABLE_INPUT_SYSTEM
        // Xbox A is the south face button.
        return Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.JoystickButton0);
#endif
    }

    protected override bool WasDashPressed()
    {
#if ENABLE_INPUT_SYSTEM
        // Xbox B is the east face button.
        return Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.JoystickButton1);
#endif
    }
}
