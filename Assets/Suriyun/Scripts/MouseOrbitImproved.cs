using UnityEngine;
using UnityEngine.InputSystem;

[AddComponentMenu("Camera-Control/Mouse Orbit with zoom (New Input System)")]
public class MouseOrbitImproved : MonoBehaviour
{
    public Transform target;
    public float distance = 5.0f;
    public float xSpeed = 120.0f;
    public float ySpeed = 120.0f;

    public float yMinLimit = -20f;
    public float yMaxLimit = 80f;

    public float distanceMin = .5f;
    public float distanceMax = 15f;

    private Rigidbody rb;

    float x = 0.0f;
    float y = 0.0f;

    void Start()
    {
        Vector3 angles = transform.eulerAngles;
        x = angles.y;
        y = angles.x;

        rb = GetComponent<Rigidbody>();
        if (rb != null)
            rb.freezeRotation = true;
    }

    void LateUpdate()
    {
        if (!target) return;

        // ===== New Input System =====
        Vector2 mouseDelta = Vector2.zero;
        float scroll = 0f;

        if (Mouse.current != null)
        {
            mouseDelta = Mouse.current.delta.ReadValue();
            scroll = Mouse.current.scroll.ReadValue().y;
        }

        // Rotate
        x += mouseDelta.x * xSpeed * distance * 0.02f;
        y -= mouseDelta.y * ySpeed * 0.02f;
        y = ClampAngle(y, yMinLimit, yMaxLimit);

        Quaternion rotation = Quaternion.Euler(y, x, 0);

        // Zoom
        distance = Mathf.Clamp(
            distance - scroll * 0.01f * 5f,
            distanceMin,
            distanceMax
        );

        // Collision
        if (Physics.Linecast(target.position, transform.position, out RaycastHit hit))
        {
            distance -= hit.distance;
        }

        Vector3 negDistance = new Vector3(0.0f, 0.0f, -distance);
        Vector3 position = rotation * negDistance + target.position;

        transform.rotation = rotation;
        transform.position = position;
    }

    public static float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360f) angle += 360f;
        if (angle > 360f) angle -= 360f;
        return Mathf.Clamp(angle, min, max);
    }
}
