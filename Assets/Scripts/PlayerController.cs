using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PlayerModel))]
public class PlayerController : MonoBehaviour
{
    private enum ControlScheme
    {
        WasdAndSpace,
        ArrowsAndEnter
    }

    [Header("Input")]
    [SerializeField] private ControlScheme controlScheme = ControlScheme.WasdAndSpace;

    [Header("Movement")]
    [SerializeField, Min(0f)] private float moveSpeed = 5f;
    [SerializeField, Min(0.1f)] private float turnRadius = 1.5f;
    [SerializeField, Range(0f, 0.95f)] private float stickDeadZone = 0.15f;

    [Header("Dash")]
    [SerializeField, Min(0f)] private float dashDistance = 3f;
    [SerializeField, Min(0.01f)] private float dashDuration = 0.15f;
    [SerializeField, Min(0f)] private float dashCooldown = 5f;

    [Header("Ball Chain")]
    [SerializeField, Min(0f)] private float ballGap = 0f;
    [SerializeField, Min(0.01f)] private float trailSampleDistance = 0.1f;
    [SerializeField, Min(1f)] private float ballFollowSpeed = 20f;

    [Header("Ball Shooting")]
    [SerializeField, Min(0f)] private float ballLaunchSpeed = 15f;
    [SerializeField, Min(0f)] private float launchClearance = 0.1f;

    private Rigidbody body;
    private PlayerModel playerModel;
    private Vector3 desiredDirection;
    private float movementAmount;
    private float playerRadius = 0.5f;
    private bool launchRequested;
    private bool dashRequested;
    private float dashRemainingDistance;
    private float nextDashTime;
    private Vector3 dashDirection;
    private readonly System.Collections.Generic.List<Vector3> trail =
        new System.Collections.Generic.List<Vector3>();

    public float DashCooldownRemaining => Mathf.Max(0f, nextDashTime - Time.time);
    public bool IsDashing => dashRemainingDistance > 0f;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        playerModel = GetComponent<PlayerModel>();
        playerRadius = GetHorizontalRadius();

        // Movement is constrained to the horizontal XZ plane.
        desiredDirection = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        if (desiredDirection.sqrMagnitude < 0.001f)
        {
            desiredDirection = Vector3.forward;
        }

        trail.Add(body.position);
    }

    private void Update()
    {
        Vector2 input = ReadMoveInput();

        if (WasLaunchPressed())
        {
            launchRequested = true;
        }

        if (WasDashPressed() && Time.time >= nextDashTime && dashRemainingDistance <= 0f)
        {
            dashRequested = true;
        }

        // Move only while the keyboard or left stick is being operated.
        if (input.sqrMagnitude > stickDeadZone * stickDeadZone)
        {
            desiredDirection = new Vector3(input.x, 0f, input.y).normalized;
            movementAmount = Mathf.InverseLerp(stickDeadZone, 1f, input.magnitude);
        }
        else
        {
            movementAmount = 0f;
        }
    }

    private void FixedUpdate()
    {
        if (dashRequested)
        {
            BeginDash();
            dashRequested = false;
        }

        if (launchRequested)
        {
            LaunchFirstBall();
            launchRequested = false;
        }

        if (dashRemainingDistance > 0f)
        {
            UpdateDash();
        }
        else if (movementAmount > 0f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(desiredDirection, Vector3.up);
            float currentSpeed = moveSpeed * movementAmount;
            float angularSpeed = currentSpeed / Mathf.Max(turnRadius, 0.1f) * Mathf.Rad2Deg;
            Quaternion nextRotation = Quaternion.RotateTowards(
                body.rotation,
                targetRotation,
                angularSpeed * Time.fixedDeltaTime);

            body.MoveRotation(nextRotation);
            Vector3 forward = nextRotation * Vector3.forward;
            Vector3 nextPosition = body.position
                                   + forward * (currentSpeed * Time.fixedDeltaTime);
            body.MovePosition(nextPosition);
            RecordTrailPoint(nextPosition);
        }

        UpdateBallChain();
    }

    private void BeginDash()
    {
        dashDirection = body.rotation * Vector3.forward;
        dashDirection.y = 0f;
        if (dashDirection.sqrMagnitude < 0.001f)
        {
            dashDirection = desiredDirection;
        }

        dashDirection.Normalize();
        dashRemainingDistance = dashDistance;
        nextDashTime = Time.time + dashCooldown;
    }

    private void UpdateDash()
    {
        float dashSpeed = dashDistance / Mathf.Max(dashDuration, 0.01f);
        float step = Mathf.Min(dashRemainingDistance, dashSpeed * Time.fixedDeltaTime);
        Vector3 nextPosition = body.position + dashDirection * step;

        body.MoveRotation(Quaternion.LookRotation(dashDirection, Vector3.up));
        body.MovePosition(nextPosition);
        RecordTrailPoint(nextPosition);
        dashRemainingDistance -= step;
    }

    private void LaunchFirstBall()
    {
        CollectibleBall ball = playerModel.RemoveFirstBall();
        if (ball == null)
        {
            return;
        }

        Vector3 launchDirection = body.rotation * Vector3.forward;
        launchDirection.y = 0f;
        launchDirection.Normalize();

        Vector3 launchPosition = body.position
                                 + launchDirection * (playerRadius + ball.WorldRadius + launchClearance);
        launchPosition.y = ball.transform.position.y;

        ball.Launch(launchPosition, launchDirection, ballLaunchSpeed, playerModel);
    }

    private void OnTriggerEnter(Collider other)
    {
        TryCollectBall(other);
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryCollectBall(collision.collider);
    }

    private void TryCollectBall(Collider other)
    {
        CollectibleBall ball = other.GetComponentInParent<CollectibleBall>();
        if (ball == null || !ball.TryCollect())
        {
            return;
        }

        playerModel.AddBall(ball);
        ball.RefreshWorldRadius();
        ball.transform.SetParent(null, true);
    }

    private void RecordTrailPoint(Vector3 position)
    {
        if (trail.Count == 0 || Vector3.Distance(trail[0], position) >= trailSampleDistance)
        {
            trail.Insert(0, position);
        }

        float requiredLength = GetRequiredTrailLength() + 2f;
        float length = 0f;
        for (int i = 1; i < trail.Count; i++)
        {
            length += Vector3.Distance(trail[i - 1], trail[i]);
            if (length > requiredLength)
            {
                trail.RemoveRange(i + 1, trail.Count - i - 1);
                break;
            }
        }
    }

    private void UpdateBallChain()
    {
        float distanceBehind = 0f;
        float previousRadius = playerRadius;

        for (int i = 0; i < playerModel.CollectedBalls.Count; i++)
        {
            CollectibleBall ball = playerModel.CollectedBalls[i].Ball;
            if (ball == null)
            {
                continue;
            }

            distanceBehind += previousRadius + ball.WorldRadius + ballGap;

            Vector3 target = GetTrailPosition(distanceBehind);
            ball.transform.position = Vector3.MoveTowards(
                ball.transform.position,
                target,
                ballFollowSpeed * Time.fixedDeltaTime);

            previousRadius = ball.WorldRadius;
        }
    }

    private float GetRequiredTrailLength()
    {
        float length = 0f;
        float previousRadius = playerRadius;

        for (int i = 0; i < playerModel.CollectedBalls.Count; i++)
        {
            CollectibleBall ball = playerModel.CollectedBalls[i].Ball;
            if (ball == null)
            {
                continue;
            }

            length += previousRadius + ball.WorldRadius + ballGap;
            previousRadius = ball.WorldRadius;
        }

        return length;
    }

    private float GetHorizontalRadius()
    {
        Collider playerCollider = GetComponent<Collider>();
        if (playerCollider != null)
        {
            return Mathf.Max(playerCollider.bounds.extents.x, playerCollider.bounds.extents.z);
        }

        Vector3 scale = transform.lossyScale;
        return Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z)) * 0.5f;
    }

    private Vector3 GetTrailPosition(float distanceBehind)
    {
        if (trail.Count == 0)
        {
            return body.position;
        }

        float travelled = 0f;
        for (int i = 1; i < trail.Count; i++)
        {
            float segmentLength = Vector3.Distance(trail[i - 1], trail[i]);
            if (travelled + segmentLength >= distanceBehind)
            {
                float t = (distanceBehind - travelled) / Mathf.Max(segmentLength, 0.0001f);
                return Vector3.Lerp(trail[i - 1], trail[i], t);
            }

            travelled += segmentLength;
        }

        return trail[trail.Count - 1];
    }

    private Vector2 ReadMoveInput()
    {
#if ENABLE_INPUT_SYSTEM
        Vector2 input = Vector2.zero;

        if (Keyboard.current != null)
        {
            if (controlScheme == ControlScheme.WasdAndSpace)
            {
                input.x = (Keyboard.current.dKey.isPressed ? 1f : 0f)
                        - (Keyboard.current.aKey.isPressed ? 1f : 0f);
                input.y = (Keyboard.current.wKey.isPressed ? 1f : 0f)
                        - (Keyboard.current.sKey.isPressed ? 1f : 0f);
            }
            else
            {
                input.x = (Keyboard.current.rightArrowKey.isPressed ? 1f : 0f)
                        - (Keyboard.current.leftArrowKey.isPressed ? 1f : 0f);
                input.y = (Keyboard.current.upArrowKey.isPressed ? 1f : 0f)
                        - (Keyboard.current.downArrowKey.isPressed ? 1f : 0f);
            }
        }

        return Vector2.ClampMagnitude(input, 1f);
#else
        KeyCode left = controlScheme == ControlScheme.WasdAndSpace ? KeyCode.A : KeyCode.LeftArrow;
        KeyCode right = controlScheme == ControlScheme.WasdAndSpace ? KeyCode.D : KeyCode.RightArrow;
        KeyCode down = controlScheme == ControlScheme.WasdAndSpace ? KeyCode.S : KeyCode.DownArrow;
        KeyCode up = controlScheme == ControlScheme.WasdAndSpace ? KeyCode.W : KeyCode.UpArrow;

        Vector2 input = new Vector2(
            (Input.GetKey(right) ? 1f : 0f) - (Input.GetKey(left) ? 1f : 0f),
            (Input.GetKey(up) ? 1f : 0f) - (Input.GetKey(down) ? 1f : 0f));
        return Vector2.ClampMagnitude(input, 1f);
#endif
    }

    private bool WasLaunchPressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current == null)
        {
            return false;
        }

        return controlScheme == ControlScheme.WasdAndSpace
            ? Keyboard.current.spaceKey.wasPressedThisFrame
            : Keyboard.current.enterKey.wasPressedThisFrame
              || Keyboard.current.numpadEnterKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(
            controlScheme == ControlScheme.WasdAndSpace ? KeyCode.Space : KeyCode.Return);
#endif
    }

    private bool WasDashPressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current == null)
        {
            return false;
        }

        return controlScheme == ControlScheme.WasdAndSpace
            ? Keyboard.current.leftShiftKey.wasPressedThisFrame
              || Keyboard.current.rightShiftKey.wasPressedThisFrame
            : Keyboard.current.leftCtrlKey.wasPressedThisFrame
              || Keyboard.current.rightCtrlKey.wasPressedThisFrame;
#else
        return controlScheme == ControlScheme.WasdAndSpace
            ? Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift)
            : Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.RightControl);
#endif
    }
}
