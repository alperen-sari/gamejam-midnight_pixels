using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Kırılma seviyesine göre anomalileri tetikleyen sistem.
/// GameManager'daki kırılma değerine bağlı olarak çalışır.
/// </summary>
public class FractureSystem : MonoBehaviour
{
    public static FractureSystem Instance { get; private set; }

    [Header("Anomaly Settings")]
    [SerializeField] private float anomalyCheckInterval = 5f;   // Kaç saniyede bir anomali kontrolü
    [SerializeField] private float baseAnomalyChance = 0.1f;    // Temel anomali şansı

    [Header("References")]
    [SerializeField] private List<AnomalyBase> registeredAnomalies = new List<AnomalyBase>();

    private float anomalyTimer;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnFractureStageChanged += OnStageChanged;
        }
    }

    void Update()
    {
        if (GameManager.Instance == null) return;
        if (GameManager.Instance.FractureLevel <= 0f) return;

        anomalyTimer += Time.deltaTime;
        if (anomalyTimer >= anomalyCheckInterval)
        {
            anomalyTimer = 0f;
            TryTriggerRandomAnomaly();
        }
    }

    /// <summary>
    /// Kırılma seviyesine göre rastgele bir anomali tetiklemeye çalışır.
    /// </summary>
    private void TryTriggerRandomAnomaly()
    {
        float fracturePercent = GameManager.Instance.FracturePercent;
        float chance = baseAnomalyChance + (fracturePercent * 0.5f); // Kırılma arttıkça şans artar

        if (Random.value > chance) return;

        // Mevcut aşamaya uygun anomalileri filtrele
        FractureStage currentStage = GameManager.Instance.CurrentFractureStage;
        List<AnomalyBase> validAnomalies = new List<AnomalyBase>();

        foreach (var anomaly in registeredAnomalies)
        {
            if (anomaly != null && anomaly.CanTrigger() && anomaly.MinimumStage <= currentStage)
            {
                validAnomalies.Add(anomaly);
            }
        }

        if (validAnomalies.Count == 0) return;

        // Rastgele birini seç ve tetikle
        AnomalyBase selected = validAnomalies[Random.Range(0, validAnomalies.Count)];
        selected.Trigger();

        Debug.Log($"[FractureSystem] Anomali tetiklendi: {selected.AnomalyName} (Kırılma: %{fracturePercent * 100:F0})");
    }

    /// <summary>
    /// Bir anomaliyi sisteme kaydeder.
    /// </summary>
    public void RegisterAnomaly(AnomalyBase anomaly)
    {
        if (!registeredAnomalies.Contains(anomaly))
        {
            registeredAnomalies.Add(anomaly);
        }
    }

    /// <summary>
    /// Bir anomaliyi sistemden çıkarır.
    /// </summary>
    public void UnregisterAnomaly(AnomalyBase anomaly)
    {
        registeredAnomalies.Remove(anomaly);
    }

    private void OnStageChanged(FractureStage newStage)
    {
        Debug.Log($"[FractureSystem] Yeni aşama: {newStage}. Anomali sıklığı artıyor.");

        // Aşama yükseldikçe anomali kontrolü daha sık olsun
        switch (newStage)
        {
            case FractureStage.Subtle:
                anomalyCheckInterval = 5f;
                break;
            case FractureStage.Noticeable:
                anomalyCheckInterval = 3f;
                break;
            case FractureStage.Breaking:
                anomalyCheckInterval = 1.5f;
                break;
        }
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnFractureStageChanged -= OnStageChanged;
        }
    }
}
