using UnityEngine;

/// <summary>
/// Kamerayı oyuncuya yumuşak şekilde takip ettirir.
/// Main Camera'ya ekle.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;           // Player objesi

    [Header("Settings")]
    [SerializeField] private float smoothSpeed = 5f;     // Takip yumuşaklığı
    [SerializeField] private Vector3 offset = new Vector3(0f, 0f, -10f); // Kamera Z offset

    void Start()
    {
        // Target atanmadıysa Player'ı otomatik bul
        if (target == null)
        {
            Player player = FindFirstObjectByType<Player>();
            if (player != null)
            {
                target = player.transform;
            }
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 targetPos = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, targetPos, smoothSpeed * Time.deltaTime);
    }
}
