using UnityEngine;

/// <summary>
/// Tüm anomalilerin base class'ı. Her anomali bunu extend eder.
/// FractureSystem tarafından yönetilir.
/// </summary>
public abstract class AnomalyBase : MonoBehaviour
{
    [Header("Anomaly Base Settings")]
    [SerializeField] private string anomalyName = "Unnamed Anomaly";
    [SerializeField] private FractureStage minimumStage = FractureStage.Subtle;
    [SerializeField] private float cooldown = 10f;         // Tekrar tetiklenme bekleme süresi

    private float lastTriggerTime = -999f;

    public string AnomalyName => anomalyName;
    public FractureStage MinimumStage => minimumStage;

    /// <summary>
    /// Anomali şu an tetiklenebilir mi?
    /// </summary>
    public virtual bool CanTrigger()
    {
        return Time.time - lastTriggerTime >= cooldown;
    }

    /// <summary>
    /// Anomaliyi tetikler.
    /// </summary>
    public void Trigger()
    {
        if (!CanTrigger()) return;

        lastTriggerTime = Time.time;
        OnTrigger();
    }

    /// <summary>
    /// Alt sınıflar bu metodu override ederek kendi anomali efektlerini uygular.
    /// </summary>
    protected abstract void OnTrigger();

    /// <summary>
    /// Anomali kendini FractureSystem'e kaydeder.
    /// </summary>
    protected virtual void OnEnable()
    {
        if (FractureSystem.Instance != null)
        {
            FractureSystem.Instance.RegisterAnomaly(this);
        }
    }

    /// <summary>
    /// OnEnable'da Instance henüz null olabilir — Start'ta tekrar dene.
    /// </summary>
    protected virtual void Start()
    {
        if (FractureSystem.Instance != null)
        {
            FractureSystem.Instance.RegisterAnomaly(this);
            Debug.Log($"[AnomalyBase] '{anomalyName}' kayıt edildi. MinStage: {minimumStage}");
        }
        else
        {
            Debug.LogWarning($"[AnomalyBase] '{anomalyName}' kayıt EDİLEMEDİ — FractureSystem bulunamadı!");
        }
    }

    protected virtual void OnDisable()
    {
        if (FractureSystem.Instance != null)
        {
            FractureSystem.Instance.UnregisterAnomaly(this);
        }
    }
}
