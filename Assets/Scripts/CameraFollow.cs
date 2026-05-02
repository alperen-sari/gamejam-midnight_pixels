using UnityEngine;

/// <summary>
/// Kamerayı oyuncuya yumuşak şekilde takip ettirir.
/// Main Camera'ya ekle.
/// 
/// Anomali offset'i destekler — CameraAnomaly shake efektleri
/// bu scriptteki anomalyOffset üzerinden çalışır.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    public static CameraFollow Instance { get; private set; }

    [Header("Target")]
    [SerializeField] private Transform target;           // Player objesi

    [Header("Settings")]
    [SerializeField] private float smoothSpeed = 5f;     // Takip yumuşaklığı
    [SerializeField] private Vector3 offset = new Vector3(0f, 0f, -10f); // Kamera Z offset

    // Anomali efektleri için offset — CameraAnomaly tarafından set edilir
    [HideInInspector] public Vector3 anomalyOffset = Vector3.zero;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
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

        Vector3 targetPos = target.position + offset + anomalyOffset;
        transform.position = Vector3.Lerp(transform.position, targetPos, smoothSpeed * Time.deltaTime);
    }
}
