using System;
using System.Collections.Generic;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PlayerSpawner : MonoBehaviour
{
    public enum PlayerJoinType
    {
        Wasd,
        Arrows,
        Gamepad
    }

    [Header("Battle Player Prefabs")]
    [SerializeField] private GameObject playerOneKeyboardPrefab;
    [SerializeField] private GameObject playerTwoKeyboardPrefab;
    [SerializeField] private GameObject playerOneGamepadPrefab;
    [SerializeField] private GameObject playerTwoGamepadPrefab;

    [Header("Join Preview Models")]
    [Tooltip("Join 页面中预先放置的 P1 模型，不需要挂控制脚本")]
    [SerializeField] private GameObject playerOneJoinModel;
    [Tooltip("Join 页面中预先放置的 P2 模型，不需要挂控制脚本")]
    [SerializeField] private GameObject playerTwoJoinModel;

    private bool keyboardHasJoined;
    private bool arrowKeyboardHasJoined;
    private bool gamepadHasJoined;
    private readonly List<PlayerJoinType> joinOrder = new List<PlayerJoinType>(2);
    private bool battlePlayersSpawned;

    public bool KeyboardHasJoined => keyboardHasJoined;
    public bool ArrowKeyboardHasJoined => arrowKeyboardHasJoined;
    public bool GamepadHasJoined => gamepadHasJoined;
    public int JoinedPlayerCount => (keyboardHasJoined ? 1 : 0)
                                    + (arrowKeyboardHasJoined ? 1 : 0)
                                    + (gamepadHasJoined ? 1 : 0);
    public bool BothPlayersJoined => JoinedPlayerCount >= 2;
    public IReadOnlyList<PlayerJoinType> JoinOrder => joinOrder;
    public event Action<PlayerController> PlayerSpawned;

    private void Update()
    {
        if (BothPlayersJoined)
        {
            return;
        }

        if (!keyboardHasJoined && WasKeyboardJoinPressed())
        {
            keyboardHasJoined = TryJoin(PlayerJoinType.Wasd);
        }

        if (!BothPlayersJoined && !arrowKeyboardHasJoined && WasArrowKeyboardJoinPressed())
        {
            arrowKeyboardHasJoined = TryJoin(PlayerJoinType.Arrows);
        }

        if (!BothPlayersJoined && !gamepadHasJoined && WasGamepadJoinPressed())
        {
            gamepadHasJoined = TryJoin(PlayerJoinType.Gamepad);
        }
    }

    private bool TryJoin(PlayerJoinType joinType)
    {
        if (joinOrder.Count >= 2)
        {
            return false;
        }

        GameObject joinModel = joinOrder.Count == 0 ? playerOneJoinModel : playerTwoJoinModel;
        if (joinModel != null)
        {
            joinModel.SetActive(true);
        }

        joinOrder.Add(joinType);
        return true;
    }

    public bool SpawnBattlePlayers(Transform playerOnePosition, Transform playerTwoPosition)
    {
        if (battlePlayersSpawned)
        {
            return true;
        }

        if (!BothPlayersJoined || playerOnePosition == null || playerTwoPosition == null)
        {
            Debug.LogError("PlayerSpawner: two joined players and both battle positions are required.", this);
            return false;
        }

        for (int i = 0; i < 2; i++)
        {
            GameObject battlePrefab = GetBattlePrefab(i, joinOrder[i]);
            if (battlePrefab == null)
            {
                Debug.LogError($"PlayerSpawner: battle prefab for P{i + 1} ({joinOrder[i]}) is not assigned.", this);
                return false;
            }

            if (battlePrefab.GetComponentInChildren<PlayerController>(true) == null)
            {
                Debug.LogError($"PlayerSpawner: P{i + 1} battle prefab has no PlayerController.", this);
                return false;
            }
        }

        SetJoinModelsVisible(false);
        for (int i = 0; i < 2; i++)
        {
            Transform battlePosition = i == 0 ? playerOnePosition : playerTwoPosition;
            PlayerJoinType joinType = joinOrder[i];
            GameObject playerObject = Instantiate(
                GetBattlePrefab(i, joinType),
                battlePosition.position,
                battlePosition.rotation);
            PlayerController controller = playerObject.GetComponent<PlayerController>();
            if (controller == null)
            {
                controller = playerObject.GetComponentInChildren<PlayerController>();
            }

            if (controller != null)
            {
                if (joinType == PlayerJoinType.Wasd)
                {
                    controller.SetKeyboardControlScheme(
                        PlayerController.KeyboardControlScheme.WasdSpaceShift);
                }
                else if (joinType == PlayerJoinType.Arrows)
                {
                    controller.SetKeyboardControlScheme(
                        PlayerController.KeyboardControlScheme.ArrowsEnterCtrl);
                }

                controller.enabled = true;
            }

            PlayerSpawned?.Invoke(controller);
        }

        battlePlayersSpawned = true;
        return true;
    }

    private GameObject GetBattlePrefab(int playerIndex, PlayerJoinType joinType)
    {
        bool usesGamepad = joinType == PlayerJoinType.Gamepad;
        if (playerIndex == 0)
        {
            return usesGamepad ? playerOneGamepadPrefab : playerOneKeyboardPrefab;
        }

        return usesGamepad ? playerTwoGamepadPrefab : playerTwoKeyboardPrefab;
    }

    private void SetJoinModelsVisible(bool visible)
    {
        if (playerOneJoinModel != null) playerOneJoinModel.SetActive(visible);
        if (playerTwoJoinModel != null) playerTwoJoinModel.SetActive(visible);
    }

    public void SpawnSavedPlayers(IReadOnlyList<PlayerJoinType> savedJoinOrder)
    {
        if (savedJoinOrder == null)
        {
            return;
        }

        foreach (PlayerJoinType joinType in savedJoinOrder)
        {
            if (BothPlayersJoined)
            {
                break;
            }

            switch (joinType)
            {
                case PlayerJoinType.Wasd:
                    keyboardHasJoined = TryJoin(joinType);
                    break;
                case PlayerJoinType.Arrows:
                    arrowKeyboardHasJoined = TryJoin(joinType);
                    break;
                case PlayerJoinType.Gamepad:
                    gamepadHasJoined = TryJoin(joinType);
                    break;
            }
        }
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
