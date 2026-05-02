using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Oyunun genel durumunu yöneten singleton.
/// Gün takibi, kırılma seviyesi (gizli), müdür güveni (görünür).
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game State")]
    [SerializeField] private int currentDay = 1;
    [SerializeField] private int maxDays = 3;

    [Header("Fracture System (Gizli — arka planda)")]
    [SerializeField] private float fractureLevel = 0f;      // 0 - 100 arası
    [SerializeField] private float maxFracture = 100f;

    [Header("Boss Trust (Görünür — ekranda bar)")]
    [SerializeField] private float bossTrust = 100f;         // 0 - 100 arası
    [SerializeField] private float maxBossTrust = 100f;

    // Events
    public System.Action<int> OnDayChanged;
    public System.Action<float> OnFractureLevelChanged;
    public System.Action<FractureStage> OnFractureStageChanged;
    public System.Action<float> OnBossTrustChanged;          // Müdür güveni değiştiğinde
    public System.Action OnGameEnded;

    public int CurrentDay => currentDay;
    public float FractureLevel => fractureLevel;
    public float FracturePercent => fractureLevel / maxFracture;
    public float BossTrust => bossTrust;
    public float BossTrustPercent => bossTrust / maxBossTrust;

    public FractureStage CurrentFractureStage
    {
        get
        {
            float percent = FracturePercent;
            if (percent < 0.3f) return FractureStage.Subtle;
            if (percent < 0.65f) return FractureStage.Noticeable;
            return FractureStage.Breaking;
        }
    }

    private FractureStage lastStage = FractureStage.Subtle;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ==================== Kırılma (Gizli) ====================

    private int rebellionCount = 0;  // Bu gün kaç rutin kırıldı
    public int RebellionCount => rebellionCount;

    /// <summary>
    /// Kırılma seviyesini artırır. Rutin kırıldığında çağrılır.
    /// Anında anomali tetikler!
    /// </summary>
    public void AddFracture(float amount)
    {
        float oldLevel = fractureLevel;
        fractureLevel = Mathf.Clamp(fractureLevel + amount, 0f, maxFracture);
        rebellionCount++;

        if (!Mathf.Approximately(oldLevel, fractureLevel))
        {
            OnFractureLevelChanged?.Invoke(fractureLevel);

            FractureStage newStage = CurrentFractureStage;
            if (newStage != lastStage)
            {
                lastStage = newStage;
                OnFractureStageChanged?.Invoke(newStage);
                Debug.Log($"[GameManager] Kırılma aşaması değişti: {newStage}");
            }

            // Rutin kırılınca ANINDA anomali tetikle
            if (FractureSystem.Instance != null)
            {
                FractureSystem.Instance.TriggerImmediateAnomaly(rebellionCount);
            }
        }
    }

    // ==================== Müdür Güveni (Görünür) ====================

    /// <summary>
    /// Müdür güvenini azaltır. İsyan yapınca düşer.
    /// Ekranda bar olarak gösterilir.
    /// </summary>
    public void ReduceBossTrust(float amount)
    {
        float oldTrust = bossTrust;
        bossTrust = Mathf.Clamp(bossTrust - amount, 0f, maxBossTrust);

        if (!Mathf.Approximately(oldTrust, bossTrust))
        {
            OnBossTrustChanged?.Invoke(bossTrust);
            Debug.Log($"[GameManager] Müdür güveni: {bossTrust:F0}/{maxBossTrust:F0}");
        }
    }

    /// <summary>
    /// Müdür güvenini artırır. Rutine uyunca artar.
    /// </summary>
    public void AddBossTrust(float amount)
    {
        float oldTrust = bossTrust;
        bossTrust = Mathf.Clamp(bossTrust + amount, 0f, maxBossTrust);

        if (!Mathf.Approximately(oldTrust, bossTrust))
        {
            OnBossTrustChanged?.Invoke(bossTrust);
        }
    }

    // ==================== Gün Sistemi ====================

    public void AdvanceDay()
    {
        if (currentDay >= maxDays)
        {
            EndGame();
            return;
        }

        currentDay++;
        rebellionCount = 0;
        Debug.Log($"[GameManager] Gün {currentDay}. Kırılma: %{FracturePercent * 100:F0} | Güven: {bossTrust:F0}");
        OnDayChanged?.Invoke(currentDay);
    }

    private void EndGame()
    {
        Debug.Log($"[GameManager] Oyun bitti! Kırılma: %{FracturePercent * 100:F0} | Güven: {bossTrust:F0}");
        OnGameEnded?.Invoke();
    }

    public void ResetGame()
    {
        currentDay = 1;
        fractureLevel = 0f;
        bossTrust = maxBossTrust;
        lastStage = FractureStage.Subtle;
        OnDayChanged?.Invoke(currentDay);
        OnFractureLevelChanged?.Invoke(fractureLevel);
        OnBossTrustChanged?.Invoke(bossTrust);
    }
}

public enum FractureStage
{
    Subtle,      // %0-30:  Hafif glitchler
    Noticeable,  // %30-65: Objeler garip davranır
    Breaking     // %65-100: Gerçeklik kırılır
}
