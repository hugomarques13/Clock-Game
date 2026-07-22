using UnityEngine;

/// <summary>
/// Smoothly keeps the camera centred on a target. Put this on Main Camera
/// and drag the Player into the Target slot.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Tooltip("Usually the Player.")]
    public Transform target;

    [Tooltip("Higher = camera catches up faster. 0 = instant snap.")]
    public float smoothTime = 0.15f;

    Vector3 velocity;

    void LateUpdate()
    {
        if (target == null) return;

        // Keep the camera's own Z (-10) so 2D sprites stay visible.
        Vector3 goal = new Vector3(target.position.x, target.position.y, transform.position.z);

        transform.position = smoothTime <= 0f
            ? goal
            : Vector3.SmoothDamp(transform.position, goal, ref velocity, smoothTime);
    }
}
