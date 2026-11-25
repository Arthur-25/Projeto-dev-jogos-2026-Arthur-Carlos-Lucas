using UnityEngine;

public class ParallaxBackground : MonoBehaviour
{
    private Transform cam;
    private Vector3 startPos;
    private SpriteRenderer sr;

    [Header("Velocidades do Parallax (0 a 1)")]
    [Range(0f, 1f)] public float parallaxSpeedX = 0.5f;
    [Range(0f, 1f)] public float parallaxSpeedY = 0.0f;

    [Header("Travar Movimento Vertical")]
    public bool lockVertical = true;

    void Start()
    {
        if (Camera.main != null)
        {
            cam = Camera.main.transform;
        }
        startPos = transform.position;

        sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.enabled = true;
        }
    }

    void LateUpdate()
    {
        if (cam == null)
        {
            if (Camera.main != null) cam = Camera.main.transform;
            else return;
        }

        if (sr != null && !sr.enabled)
        {
            sr.enabled = true;
        }

        float distX = (cam.position.x - startPos.x) * parallaxSpeedX;
        float distY = lockVertical ? 0f : (cam.position.y - startPos.y) * parallaxSpeedY;

        transform.position = new Vector3(startPos.x + distX, startPos.y + distY, startPos.z);
    }
}
