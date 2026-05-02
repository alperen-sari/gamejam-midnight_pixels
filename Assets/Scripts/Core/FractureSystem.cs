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
    [SerializeField] private float anomalyCheckInterval = 3f;   // Kaç saniyede bir anomali kontrolü
    [SerializeField] private float baseAnomalyChance = 0.25f;   // Temel anomali şansı (%25)

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
    /// Rutin kırıldığında ANINDA anomali tetikler.
    /// rebellionNumber: bu gün kaçıncı isyan.
    /// 1. isyan = sadece kamera shake
    /// 2.+ isyan = kamera shake + obje anomalisi
    /// </summary>
    public void TriggerImmediateAnomaly(int rebellionNumber)
    {
        Debug.Log($"[FractureSystem] ANLIK anomali! İsyan #{rebellionNumber}");

        // Kamera anomalisi bul ve tetikle
        CameraAnomaly camAnomaly = null;
        ObjectAnomaly objAnomaly = null;

        foreach (var anomaly in registeredAnomalies)
        {
            if (anomaly is CameraAnomaly ca && camAnomaly == null)
                camAnomaly = ca;
            if (anomaly is ObjectAnomaly oa && objAnomaly == null)
                objAnomaly = oa;
        }

        // 1. isyan: kamera shake
        if (camAnomaly != null)
        {
            camAnomaly.Trigger();
        }

        // 2.+ isyan: kamera + obje
        if (rebellionNumber >= 2 && objAnomaly != null)
        {
            objAnomaly.Trigger();
        }

        // Post-Processing efektlerini güncelle
        if (PostProcessAnomaly.Instance != null)
        {
            PostProcessAnomaly.Instance.OnRebellion(rebellionNumber);
        }
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
