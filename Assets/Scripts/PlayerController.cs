using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PlayerModel))]
public class PlayerController : MonoBehaviour
{
    public enum KeyboardControlScheme
    {
        WasdSpaceShift,
        ArrowsEnterCtrl
    }

    [Header("Keyboard Input")]
    [SerializeField] private KeyboardControlScheme keyboardControlScheme =
        KeyboardControlScheme.WasdSpaceShift;

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
    [SerializeField, Min(0f)] private float attackInterval = 0.3f;

    [Header("Special Effect Visuals")]
    [SerializeField] private Color aimGuideColor = new Color(1f, 1f, 1f, 0.8f);
    [SerializeField, Min(0.001f)] private float aimGuideWidth = 0.04f;
    [SerializeField, Min(0f)] private float aimGuideHeightOffset = 0.15f;

    private Rigidbody body;
    private PlayerModel playerModel;
    private Vector3 desiredDirection;
    private Quaternion prefabRotationOffset = Quaternion.identity;
    private float movementAmount;
    private float playerRadius = 0.5f;
    private bool launchRequested;
    private bool dashRequested;
    private float dashRemainingDistance;
    private float nextDashTime;
    private float nextAttackTime;
    private Vector3 dashDirection;
    private float speedBoostMultiplier = 1f;
    private float speedBoostEndTime;
    private float slowMultiplier = 1f;
    private float slowEndTime;
    private float suctionRadius;
    private float suctionEndTime;
    private float aimGuideEndTime;
    private float aimGuideLength;
    private bool explosiveBallArmed;
    private float explosiveBallEndTime;
    private CollectibleBall explosiveGlowBall;
    private ExplosiveBallGlow explosiveGlow;
    private LineRenderer aimGuide;
    private readonly System.Collections.Generic.List<Vector3> trail =
        new System.Collections.Generic.List<Vector3>();

    public float DashCooldownRemaining => Mathf.Max(0f, nextDashTime - Time.time);
    public float DashCooldownProgress => dashCooldown <= 0f
        ? 1f
        : 1f - Mathf.Clamp01(DashCooldownRemaining / dashCooldown);
    public bool IsDashing => dashRemainingDistance > 0f;

    /// <summary>把玩家安全地移动到新区域，并重置移动与球链轨迹状态。</summary>
    public void TeleportTo(Transform destination)
    {
        if (destination == null)
        {
            return;
        }

        if (body == null)
        {
            body = GetComponent<Rigidbody>();
        }

        body.velocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        body.position = destination.position;
        body.rotation = destination.rotation;
        transform.SetPositionAndRotation(destination.position, destination.rotation);

        desiredDirection = destination.forward;
        desiredDirection.y = 0f;
        if (desiredDirection.sqrMagnitude < 0.001f)
        {
            desiredDirection = Vector3.forward;
        }
        else
        {
            desiredDirection.Normalize();
        }

        movementAmount = 0f;
        launchRequested = false;
        dashRequested = false;
        dashRemainingDistance = 0f;
        trail.Clear();
        trail.Add(destination.position);
    }

    public void SetKeyboardControlScheme(KeyboardControlScheme scheme)
    {
        keyboardControlScheme = scheme;
    }

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

        Quaternion initialFacing = Quaternion.LookRotation(desiredDirection, Vector3.up);
        prefabRotationOffset = Quaternion.Inverse(initialFacing) * body.rotation;

        trail.Add(body.position);
    }

    private void Update()
    {
        UpdateTimedEffects();
        Vector2 input = ReadMoveInput();

        if (WasLaunchPressed() && Time.time >= nextAttackTime)
        {
            launchRequested = true;
        }

        if (WasDashPressed() && Time.time >= nextDashTime && dashRemainingDistance <= 0f)
        {
            dashRequested = true;
        }

        // Move only while the selected movement control is being operated.
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
            if (LaunchFirstBall())
            {
                nextAttackTime = Time.time + attackInterval;
            }

            launchRequested = false;
        }

        if (dashRemainingDistance > 0f)
        {
            UpdateDash();
        }
        else if (movementAmount > 0f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(desiredDirection, Vector3.up)
                                        * prefabRotationOffset;
            float currentSpeed = moveSpeed * GetSpeedMultiplier() * movementAmount;
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

        body.MoveRotation(Quaternion.LookRotation(dashDirection, Vector3.up)
                          * prefabRotationOffset);
        body.MovePosition(nextPosition);
        RecordTrailPoint(nextPosition);
        dashRemainingDistance -= step;
    }

    private bool LaunchFirstBall()
    {
        CollectibleBall ball = playerModel.RemoveFirstBall();
        if (ball == null)
        {
            return false;
        }

        if (explosiveBallArmed)
        {
            ball.SetExplosive(true);
            explosiveBallArmed = false;
            explosiveBallEndTime = 0f;
            ClearExplosiveBallGlow();
        }

        Vector3 launchDirection = body.rotation * Vector3.forward;
        launchDirection.y = 0f;
        launchDirection.Normalize();

        Vector3 launchPosition = body.position
                                 + launchDirection * (playerRadius + ball.WorldRadius + launchClearance);
        launchPosition.y = ball.transform.position.y;

        ball.Launch(launchPosition, launchDirection, ballLaunchSpeed, playerModel);
        return true;
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

    public void ApplySpeedBoost(float multiplier, float duration)
    {
        speedBoostMultiplier = Mathf.Max(1f, multiplier);
        speedBoostEndTime = Mathf.Max(speedBoostEndTime, Time.time + duration);
    }

    public void ApplySlow(float multiplier, float duration)
    {
        slowMultiplier = Mathf.Clamp(multiplier, 0.01f, 1f);
        slowEndTime = Mathf.Max(slowEndTime, Time.time + duration);
    }

    public void ApplySuction(float radius, float duration)
    {
        suctionRadius = Mathf.Max(suctionRadius, radius);
        suctionEndTime = Mathf.Max(suctionEndTime, Time.time + duration);
    }

    public void ApplyAimGuide(float duration, float length)
    {
        aimGuideLength = Mathf.Max(0.1f, length);
        aimGuideEndTime = Mathf.Max(aimGuideEndTime, Time.time + duration);
        EnsureAimGuide();
    }

    public void ArmExplosiveBall(float duration = 0f)
    {
        explosiveBallArmed = true;
        explosiveBallEndTime = duration > 0f ? Time.time + duration : 0f;
        RefreshExplosiveBallGlow();
    }

    private float GetSpeedMultiplier()
    {
        float boost = Time.time < speedBoostEndTime ? speedBoostMultiplier : 1f;
        float slow = Time.time < slowEndTime ? slowMultiplier : 1f;
        return boost * slow;
    }

    private void UpdateTimedEffects()
    {
        if (explosiveBallArmed && explosiveBallEndTime > 0f
            && Time.time >= explosiveBallEndTime)
        {
            explosiveBallArmed = false;
            explosiveBallEndTime = 0f;
            ClearExplosiveBallGlow();
        }
        else
        {
            RefreshExplosiveBallGlow();
        }

        if (Time.time < suctionEndTime)
        {
            Collider[] nearby = Physics.OverlapSphere(transform.position, suctionRadius);
            foreach (Collider nearbyCollider in nearby)
            {
                TryCollectBall(nearbyCollider);
            }
        }
        else
        {
            suctionRadius = 0f;
        }

        bool showAimGuide = Time.time < aimGuideEndTime;
        if (aimGuide != null)
        {
            aimGuide.enabled = showAimGuide;
            if (showAimGuide)
            {
                Vector3 start = body.position + Vector3.up * aimGuideHeightOffset;
                CollectibleBall firstBall = playerModel != null
                                            && playerModel.CollectedBalls.Count > 0
                    ? playerModel.CollectedBalls[0].Ball
                    : null;
                if (firstBall != null)
                {
                    start.y = firstBall.transform.position.y;
                }

                Vector3 forward = body.rotation * Vector3.forward;
                forward.y = 0f;
                forward.Normalize();
                aimGuide.SetPosition(0, start);
                aimGuide.SetPosition(1, start + forward * aimGuideLength);
            }
        }
    }

    private void RefreshExplosiveBallGlow()
    {
        CollectibleBall firstBall = explosiveBallArmed && playerModel != null
                                    && playerModel.CollectedBalls.Count > 0
            ? playerModel.CollectedBalls[0].Ball
            : null;

        if (firstBall == explosiveGlowBall)
        {
            return;
        }

        ClearExplosiveBallGlow();
        if (firstBall == null)
        {
            return;
        }

        explosiveGlowBall = firstBall;
        explosiveGlow = firstBall.gameObject.AddComponent<ExplosiveBallGlow>();
    }

    private void ClearExplosiveBallGlow()
    {
        if (explosiveGlow != null)
        {
            Destroy(explosiveGlow);
        }

        explosiveGlow = null;
        explosiveGlowBall = null;
    }

    private void EnsureAimGuide()
    {
        if (aimGuide != null)
        {
            aimGuide.enabled = true;
            return;
        }

        GameObject guideObject = new GameObject("AimGuide");
        guideObject.transform.SetParent(transform, false);
        aimGuide = guideObject.AddComponent<LineRenderer>();
        aimGuide.useWorldSpace = true;
        aimGuide.positionCount = 2;
        aimGuide.startWidth = aimGuideWidth;
        aimGuide.endWidth = aimGuideWidth;
        aimGuide.startColor = aimGuideColor;
        aimGuide.endColor = aimGuideColor;
        aimGuide.material = new Material(Shader.Find("Sprites/Default"));
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

            if (i == 0)
            {
                Vector3 forward = body.rotation * Vector3.forward;
                forward.y = 0f;
                forward.Normalize();
                Vector3 launchReadyPosition = body.position
                                              + forward * (playerRadius
                                                           + ball.WorldRadius
                                                           + launchClearance);
                launchReadyPosition.y = ball.transform.position.y;
                ball.transform.position = Vector3.MoveTowards(
                    ball.transform.position,
                    launchReadyPosition,
                    ballFollowSpeed * Time.fixedDeltaTime);
                continue;
            }

            distanceBehind += previousRadius + ball.WorldRadius + ballGap;

            Vector3 target = GetTrailPosition(distanceBehind);
            // Follow the player's trail only on the XZ plane. Keeping the ball's
            // own Y position prevents collected balls from being lifted to, or
            // sunk toward, the player's Rigidbody center height.
            target.y = ball.transform.position.y;
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

        // The first ball is held in front of the player and does not consume trail length.
        for (int i = 1; i < playerModel.CollectedBalls.Count; i++)
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

    protected virtual Vector2 ReadMoveInput()
    {
#if ENABLE_INPUT_SYSTEM
        Vector2 input = Vector2.zero;
        if (Keyboard.current != null)
        {
            if (keyboardControlScheme == KeyboardControlScheme.WasdSpaceShift)
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
        KeyCode left = keyboardControlScheme == KeyboardControlScheme.WasdSpaceShift
            ? KeyCode.A : KeyCode.LeftArrow;
        KeyCode right = keyboardControlScheme == KeyboardControlScheme.WasdSpaceShift
            ? KeyCode.D : KeyCode.RightArrow;
        KeyCode down = keyboardControlScheme == KeyboardControlScheme.WasdSpaceShift
            ? KeyCode.S : KeyCode.DownArrow;
        KeyCode up = keyboardControlScheme == KeyboardControlScheme.WasdSpaceShift
            ? KeyCode.W : KeyCode.UpArrow;
        Vector2 input = new Vector2(
            (Input.GetKey(right) ? 1f : 0f) - (Input.GetKey(left) ? 1f : 0f),
            (Input.GetKey(up) ? 1f : 0f) - (Input.GetKey(down) ? 1f : 0f));
        return Vector2.ClampMagnitude(input, 1f);
#endif
    }

    protected virtual bool WasLaunchPressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current == null)
        {
            return false;
        }

        return keyboardControlScheme == KeyboardControlScheme.WasdSpaceShift
            ? Keyboard.current.spaceKey.wasPressedThisFrame
            : Keyboard.current.enterKey.wasPressedThisFrame
              || Keyboard.current.numpadEnterKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(
            keyboardControlScheme == KeyboardControlScheme.WasdSpaceShift
                ? KeyCode.Space
                : KeyCode.Return);
#endif
    }

    protected virtual bool WasDashPressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current == null)
        {
            return false;
        }

        return keyboardControlScheme == KeyboardControlScheme.WasdSpaceShift
            ? Keyboard.current.leftShiftKey.wasPressedThisFrame
              || Keyboard.current.rightShiftKey.wasPressedThisFrame
            : Keyboard.current.leftCtrlKey.wasPressedThisFrame
              || Keyboard.current.rightCtrlKey.wasPressedThisFrame;
#else
        return keyboardControlScheme == KeyboardControlScheme.WasdSpaceShift
            ? Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift)
            : Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.RightControl);
#endif
    }
}
