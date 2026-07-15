using System;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PlayerSpawner : MonoBehaviour
{
    [Header("Player Prefabs")]
    [SerializeField] private GameObject wasdPlayerPrefab;
    [SerializeField] private GameObject gamepadPlayerPrefab;

    [Header("Spawn Points")]
    [SerializeField] private Transform firstSpawnPoint;
    [SerializeField] private Transform secondSpawnPoint;

    private bool keyboardHasJoined;
    private bool arrowKeyboardHasJoined;
    private bool gamepadHasJoined;
    private int nextSpawnIndex;

    public bool KeyboardHasJoined => keyboardHasJoined;
    public bool ArrowKeyboardHasJoined => arrowKeyboardHasJoined;
    public bool GamepadHasJoined => gamepadHasJoined;
    public int JoinedPlayerCount => (keyboardHasJoined ? 1 : 0)
                                    + (arrowKeyboardHasJoined ? 1 : 0)
                                    + (gamepadHasJoined ? 1 : 0);
    public bool BothPlayersJoined => JoinedPlayerCount >= 2;
    public event Action<PlayerController> PlayerSpawned;

    private void Update()
    {
        if (BothPlayersJoined)
        {
            return;
        }

        if (!keyboardHasJoined && WasKeyboardJoinPressed())
        {
            keyboardHasJoined = TrySpawn(
                wasdPlayerPrefab,
                "WASD",
                PlayerController.KeyboardControlScheme.WasdSpaceShift);
        }

        if (!BothPlayersJoined && !arrowKeyboardHasJoined && WasArrowKeyboardJoinPressed())
        {
            arrowKeyboardHasJoined = TrySpawn(
                wasdPlayerPrefab,
                "Arrow Keyboard",
                PlayerController.KeyboardControlScheme.ArrowsEnterCtrl);
        }

        if (!BothPlayersJoined && !gamepadHasJoined && WasGamepadJoinPressed())
        {
            gamepadHasJoined = TrySpawn(gamepadPlayerPrefab, "Gamepad", null);
        }
    }

    private bool TrySpawn(
        GameObject playerPrefab,
        string deviceName,
        PlayerController.KeyboardControlScheme? keyboardScheme)
    {
        Transform spawnPoint = nextSpawnIndex == 0 ? firstSpawnPoint : secondSpawnPoint;

        if (playerPrefab == null || spawnPoint == null)
        {
            Debug.LogError(
                $"PlayerSpawner: {deviceName} player prefab or spawn point is not assigned.",
                this);
            return false;
        }

        GameObject playerObject = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);
        PlayerController controller = playerObject.GetComponent<PlayerController>();
        if (controller != null)
        {
            if (keyboardScheme.HasValue)
            {
                controller.SetKeyboardControlScheme(keyboardScheme.Value);
            }

            controller.enabled = false;
        }

        nextSpawnIndex++;
        PlayerSpawned?.Invoke(controller);
        return true;
    }

    private static bool WasKeyboardJoinPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Space);
#endif
    }

    private static bool WasGamepadJoinPressed()
    {
#if ENABLE_INPUT_SYSTEM
        // Xbox A is the south face button.
        return Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame;
#else
        // Unity's legacy input maps Xbox A to joystick button 0.
        return Input.GetKeyDown(KeyCode.JoystickButton0);
#endif
    }

    private static bool WasArrowKeyboardJoinPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null
               && (Keyboard.current.enterKey.wasPressedThisFrame
                   || Keyboard.current.numpadEnterKey.wasPressedThisFrame);
#else
        return Input.GetKeyDown(KeyCode.Return);
#endif
    }
}
