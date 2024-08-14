using UnityEngine;
using Unity.Netcode;

public class MovingPlatform : NetworkBehaviour
{
    public Vector3 pointA;
    public Vector3 pointB;
    public Easing easingType;

    private Vector3 calculatedPosOnServer;

    public float waitTime = 1.0f; // How long the object should wait before lerping again
    private float currentWaitTime = 0f;

    public float lerpDuration = 2.0f; // Duration of the interpolation
    private float lerpTimer = 0.0f;
    private bool isForward = true;

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(pointA, 0.5f);
        Gizmos.DrawSphere(pointB, 0.5f);
        Gizmos.DrawLine(pointA, pointB);
    }

    private void Update()
    {
        if (!IsServer) return;

        currentWaitTime += Time.deltaTime;

        if (currentWaitTime >= waitTime)
        {
            lerpTimer += Time.deltaTime * (isForward ? 1 : -1);
            lerpTimer = Mathf.Clamp(lerpTimer, 0.0f, lerpDuration);
            float t = lerpTimer / lerpDuration;

            float easedValue = EasingFunctions.ApplyEasingFunction(easingType, t);

            // Interpolate between pointA and pointB using the eased value
            calculatedPosOnServer = Vector3.Lerp(pointA, pointB, easedValue);

            UpdatePositionClientRpc(calculatedPosOnServer);

            if (t >= 1.0f)
            {
                isForward = false;
                currentWaitTime = 0.0f;
            }
            else if (t <= 0.0f)
            {
                isForward = true;
                currentWaitTime = 0.0f;
            }
        }
    }

    [ClientRpc]
    private void UpdatePositionClientRpc(Vector3 newPosition)
    {
        transform.position = newPosition;
    }
}
