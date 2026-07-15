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
    private bool gamepadHasJoined;
    private int nextSpawnIndex;

    public bool KeyboardHasJoined => keyboardHasJoined;
    public bool GamepadHasJoined => gamepadHasJoined;
    public bool BothPlayersJoined => keyboardHasJoined && gamepadHasJoined;
    public event Action<PlayerController> PlayerSpawned;

    private void Update()
    {
        if (!keyboardHasJoined && WasKeyboardJoinPressed())
        {
            keyboardHasJoined = TrySpawn(wasdPlayerPrefab, "WASD");
        }

        if (!gamepadHasJoined && WasGamepadJoinPressed())
        {
            gamepadHasJoined = TrySpawn(gamepadPlayerPrefab, "Gamepad");
        }
    }

    private bool TrySpawn(GameObject playerPrefab, string deviceName)
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
}
