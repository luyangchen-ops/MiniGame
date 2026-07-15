using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    [Header("Targets")]
    [SerializeField] private PlayerController playerOne;
    [SerializeField] private PlayerController playerTwo;

    [Header("Map Bounds")]
    [SerializeField] private Collider mapBounds;
    [SerializeField, Min(0f)] private float boundsPadding = 0.1f;

    [Header("Follow")]
    [SerializeField, Min(0.01f)] private float followSmoothTime = 0.25f;
    [SerializeField] private Vector2 centerOffset;

    [Header("Dynamic Zoom")]
    [SerializeField, Min(0.1f)] private float minimumHeight = 7f;
    [SerializeField, Min(0.1f)] private float maximumHeight = 18f;
    [SerializeField, Min(0f)] private float heightPerDistance = 0.8f;
    [SerializeField, Min(0f)] private float distancePadding = 2f;
    [SerializeField, Min(0.01f)] private float zoomSmoothTime = 0.3f;

    [Header("Elimination Shake")]
    [SerializeField, Min(0f)] private float baseShakeStrength = 0.08f;
    [SerializeField, Min(0f)] private float strengthPerExtraBall = 0.05f;
    [SerializeField, Min(0f)] private float baseShakeDuration = 0.12f;
    [SerializeField, Min(0f)] private float durationPerExtraBall = 0.025f;
    [SerializeField, Min(0f)] private float maximumShakeStrength = 0.4f;

    private Camera controlledCamera;
    private Vector3 followVelocity;
    private float zoomVelocity;
    private float shakeRemainingTime;
    private float shakeDuration;
    private float shakeStrength;
    private Vector3 previousShakeOffset;

    private void Awake()
    {
        controlledCamera = GetComponent<Camera>();
    }

    private void LateUpdate()
    {
        // Remove last frame's additive shake before calculating camera follow.
        transform.position -= previousShakeOffset;
        previousShakeOffset = Vector3.zero;

        FindMissingPlayers();
        if (playerOne == null || playerTwo == null)
        {
            ApplyShake();
            return;
        }

        Vector3 firstPosition = playerOne.transform.position;
        Vector3 secondPosition = playerTwo.transform.position;
        Vector3 center = (firstPosition + secondPosition) * 0.5f;
        center.x += centerOffset.x;
        center.z += centerOffset.y;

        Vector2 firstHorizontal = new Vector2(firstPosition.x, firstPosition.z);
        Vector2 secondHorizontal = new Vector2(secondPosition.x, secondPosition.z);
        float playerDistance = Vector2.Distance(firstHorizontal, secondHorizontal);
        float targetZoom = Mathf.Clamp(
            minimumHeight + Mathf.Max(0f, playerDistance - distancePadding) * heightPerDistance,
            minimumHeight,
            maximumHeight);
        targetZoom = Mathf.Min(targetZoom, GetMaximumZoomInsideMap());
        ClampCenterToMap(ref center, targetZoom);

        if (controlledCamera.orthographic)
        {
            controlledCamera.orthographicSize = Mathf.SmoothDamp(
                controlledCamera.orthographicSize,
                targetZoom,
                ref zoomVelocity,
                zoomSmoothTime);

            Vector3 targetPosition = new Vector3(center.x, transform.position.y, center.z);
            transform.position = Vector3.SmoothDamp(
                transform.position,
                targetPosition,
                ref followVelocity,
                followSmoothTime);
            ApplyShake();
            return;
        }

        Vector3 perspectiveTarget = new Vector3(center.x, transform.position.y, center.z);
        Vector3 nextPosition = Vector3.SmoothDamp(
            transform.position,
            perspectiveTarget,
            ref followVelocity,
            followSmoothTime);
        nextPosition.y = Mathf.SmoothDamp(
            transform.position.y,
            targetZoom,
            ref zoomVelocity,
            zoomSmoothTime);
        transform.position = nextPosition;
        ApplyShake();
    }

    public void ShakeForElimination(int eliminatedBallCount)
    {
        if (eliminatedBallCount <= 0)
        {
            return;
        }

        int extraBallCount = Mathf.Max(0, eliminatedBallCount - 3);
        float requestedStrength = Mathf.Min(
            baseShakeStrength + extraBallCount * strengthPerExtraBall,
            maximumShakeStrength);
        float requestedDuration = baseShakeDuration + extraBallCount * durationPerExtraBall;

        // A stronger overlapping elimination should not be weakened by an active shake.
        shakeStrength = Mathf.Max(shakeStrength, requestedStrength);
        shakeDuration = Mathf.Max(shakeDuration, requestedDuration);
        shakeRemainingTime = Mathf.Max(shakeRemainingTime, requestedDuration);
    }

    private void ApplyShake()
    {
        if (shakeRemainingTime <= 0f || shakeDuration <= 0f)
        {
            shakeRemainingTime = 0f;
            shakeDuration = 0f;
            shakeStrength = 0f;
            return;
        }

        shakeRemainingTime = Mathf.Max(0f, shakeRemainingTime - Time.unscaledDeltaTime);
        float fade = shakeRemainingTime / shakeDuration;
        Vector2 randomOffset = Random.insideUnitCircle * shakeStrength * fade;
        previousShakeOffset = new Vector3(randomOffset.x, 0f, randomOffset.y);
        transform.position += previousShakeOffset;
    }

    private void FindMissingPlayers()
    {
        if (playerOne != null && playerTwo != null)
        {
            return;
        }

        PlayerController[] foundPlayers = FindObjectsOfType<PlayerController>();
        foreach (PlayerController player in foundPlayers)
        {
            if (player == null || player == playerOne || player == playerTwo)
            {
                continue;
            }

            if (playerOne == null)
            {
                playerOne = player;
            }
            else
            {
                playerTwo = player;
                break;
            }
        }
    }

    private void ClampCenterToMap(ref Vector3 center, float targetZoom)
    {
        if (mapBounds == null)
        {
            return;
        }

        Bounds bounds = mapBounds.bounds;
        float halfViewZ;
        float halfViewX;

        if (controlledCamera.orthographic)
        {
            halfViewZ = targetZoom;
            halfViewX = halfViewZ * controlledCamera.aspect;
        }
        else
        {
            float heightAboveMap = Mathf.Max(0.01f, targetZoom - bounds.max.y);
            halfViewZ = heightAboveMap
                        * Mathf.Tan(controlledCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            halfViewX = halfViewZ * controlledCamera.aspect;
        }

        center.x = ClampAxisToBounds(
            center.x,
            bounds.min.x + boundsPadding,
            bounds.max.x - boundsPadding,
            halfViewX);
        center.z = ClampAxisToBounds(
            center.z,
            bounds.min.z + boundsPadding,
            bounds.max.z - boundsPadding,
            halfViewZ);
    }

    private float GetMaximumZoomInsideMap()
    {
        if (mapBounds == null)
        {
            return maximumHeight;
        }

        Bounds bounds = mapBounds.bounds;
        float availableWidth = Mathf.Max(0.01f, bounds.size.x - boundsPadding * 2f);
        float availableDepth = Mathf.Max(0.01f, bounds.size.z - boundsPadding * 2f);

        if (controlledCamera.orthographic)
        {
            return Mathf.Min(availableDepth * 0.5f, availableWidth * 0.5f / controlledCamera.aspect);
        }

        float tangent = Mathf.Tan(controlledCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float heightForDepth = availableDepth * 0.5f / tangent;
        float heightForWidth = availableWidth * 0.5f / (tangent * controlledCamera.aspect);
        return bounds.max.y + Mathf.Min(heightForDepth, heightForWidth);
    }

    private static float ClampAxisToBounds(float value, float minimum, float maximum, float halfViewSize)
    {
        float allowedMinimum = minimum + halfViewSize;
        float allowedMaximum = maximum - halfViewSize;
        if (allowedMinimum > allowedMaximum)
        {
            return (minimum + maximum) * 0.5f;
        }

        return Mathf.Clamp(value, allowedMinimum, allowedMaximum);
    }
}
