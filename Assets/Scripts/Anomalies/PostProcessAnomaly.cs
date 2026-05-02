using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// URP Post-Processing efektlerini kırılma/isyan durumuna göre kontrol eder.
/// Global Volume objesine ekle.
/// 
/// 6 efekt kontrol eder:
///   - Lens Distortion    → Ekran bükülmesi
///   - Chromatic Aberration → RGB kayması (VHS efekti)
///   - Film Grain          → Karıncalanma / noise
///   - Vignette            → Ekran kenarları kararır
///   - Color Adjustments   → Renk solması (desatürasyon)
///   - Bloom               → Parlama (azalır = umutsuzluk)
/// </summary>
public class PostProcessAnomaly : MonoBehaviour
{
    public static PostProcessAnomaly Instance { get; private set; }

    [Header("Transition")]
    [SerializeField] private float transitionSpeed = 2f;
    [SerializeField] private float slamSpeed = 8f;           // İsyan anı hızlı geçiş

    // ==================== LENS DISTORTION ====================
    [Header("Lens Distortion")]
    [SerializeField] private float ldRebellion1 = -0.4f;
    [SerializeField] private float ldRebellion2 = -0.55f;
    [SerializeField] private float ldDay3Start = -0.35f;
    [SerializeField] private float ldFull = -0.8f;

    // ==================== CHROMATIC ABERRATION ====================
    [Header("Chromatic Aberration")]
    [SerializeField] private float caRebellion2 = 0.5f;
    [SerializeField] private float caDay3Start = 0.5f;
    [SerializeField] private float caFull = 1f;

    // ==================== FILM GRAIN ====================
    [Header("Film Grain")]
    [SerializeField] private float fgRebellion2 = 0.4f;
    [SerializeField] private float fgDay3Start = 0.4f;
    [SerializeField] private float fgFull = 1f;

    // ==================== VIGNETTE ====================
    [Header("Vignette")]
    [SerializeField] private float vigDay2 = 0.15f;          // Gün 2 hafif
    [SerializeField] private float vigDay3Start = 0.3f;      // Gün 3 başlangıç
    [SerializeField] private float vigFull = 0.55f;          // Full — boğucu
    [SerializeField] private Color vigColor = Color.black;

    // ==================== COLOR ADJUSTMENTS ====================
    [Header("Color Adjustments (Saturation)")]
    [SerializeField] private float satNormal = 0f;           // Normal (0 = değişiklik yok)
    [SerializeField] private float satDay2 = -15f;           // Gün 2 hafif soluk
    [SerializeField] private float satDay3Start = -30f;      // Gün 3 belirgin soluk
    [SerializeField] private float satFull = -60f;           // Full — neredeyse gri

    // ==================== BLOOM ====================
    [Header("Bloom")]
    [SerializeField] private float bloomNormal = 1.5f;       // Gün 1 parlak
    [SerializeField] private float bloomDay2 = 1f;           // Gün 2 azalır
    [SerializeField] private float bloomDay3 = 0.5f;         // Gün 3 soluk
    [SerializeField] private float bloomFull = 0.2f;         // Full — kasvetli

    // URP Volume referansları
    private Volume volume;
    private LensDistortion lensDistortion;
    private ChromaticAberration chromaticAberration;
    private FilmGrain filmGrain;
    private Vignette vignette;
    private ColorAdjustments colorAdjustments;
    private Bloom bloom;

    // Hedef değerler
    private float targetLD = 0f;
    private float targetCA = 0f;
    private float targetFG = 0f;
    private float targetVig = 0f;
    private float targetSat = 0f;
    private float targetBloom = 1.5f;

    private float currentSpeed;

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
        volume = GetComponent<Volume>();
        if (volume == null)
        {
            volume = FindFirstObjectByType<Volume>();
        }

        if (volume == null || volume.profile == null)
        {
            Debug.LogWarning("[PostProcessAnomaly] Volume veya Profile bulunamadı!");
            return;
        }

        // Efekt referanslarını al ve override'ları aktif et
        SetupEffect(out lensDistortion);
        SetupEffect(out chromaticAberration);
        SetupEffect(out filmGrain);
        SetupEffect(out vignette);
        SetupEffect(out colorAdjustments);
        SetupEffect(out bloom);

        // Başlangıç değerleri
        if (lensDistortion != null) { lensDistortion.intensity.overrideState = true; lensDistortion.intensity.value = 0f; }
        if (chromaticAberration != null) { chromaticAberration.intensity.overrideState = true; chromaticAberration.intensity.value = 0f; }
        if (filmGrain != null) { filmGrain.intensity.overrideState = true; filmGrain.intensity.value = 0f; }
        if (vignette != null) { vignette.intensity.overrideState = true; vignette.intensity.value = 0f; vignette.color.overrideState = true; vignette.color.value = vigColor; }
        if (colorAdjustments != null) { colorAdjustments.saturation.overrideState = true; colorAdjustments.saturation.value = 0f; }
        if (bloom != null) { bloom.intensity.overrideState = true; bloom.intensity.value = bloomNormal; }

        currentSpeed = transitionSpeed;

        // Event'lere bağlan
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnDayChanged += OnDayChanged;
            GameManager.Instance.OnFractureLevelChanged += OnFractureChanged;
        }

        UpdateTargetsForCurrentState();
    }

    private void SetupEffect<T>(out T effect) where T : VolumeComponent
    {
        if (volume.profile.TryGet(out effect))
        {
            effect.active = true;
        }
    }

    void Update()
    {
        float speed = currentSpeed * Time.deltaTime;

        if (lensDistortion != null)
            lensDistortion.intensity.value = Mathf.Lerp(lensDistortion.intensity.value, targetLD, speed);

        if (chromaticAberration != null)
            chromaticAberration.intensity.value = Mathf.Lerp(chromaticAberration.intensity.value, targetCA, speed);

        if (filmGrain != null)
            filmGrain.intensity.value = Mathf.Lerp(filmGrain.intensity.value, targetFG, speed);

        if (vignette != null)
            vignette.intensity.value = Mathf.Lerp(vignette.intensity.value, targetVig, speed);

        if (colorAdjustments != null)
            colorAdjustments.saturation.value = Mathf.Lerp(colorAdjustments.saturation.value, targetSat, speed);

        if (bloom != null)
            bloom.intensity.value = Mathf.Lerp(bloom.intensity.value, targetBloom, speed);

        // Hızı yavaşça normale döndür
        currentSpeed = Mathf.Lerp(currentSpeed, transitionSpeed, Time.deltaTime);
    }

    // ==================== Event Handlers ====================

    private void OnDayChanged(int newDay)
    {
        UpdateTargetsForCurrentState();
    }

    private void OnFractureChanged(float newLevel)
    {
        UpdateTargetsForCurrentState();
    }

    /// <summary>
    /// İsyan anında çağrılır — efektler hızla güncellenir.
    /// </summary>
    public void OnRebellion(int rebellionNumber)
    {
        // Hızlı geçiş modu
        currentSpeed = slamSpeed;
        UpdateTargetsForCurrentState();

        // İlk isyanda lens distortion ani sıçrama
        if (rebellionNumber == 1 && lensDistortion != null)
        {
            lensDistortion.intensity.value = targetLD * 1.5f;
        }
    }

    // ==================== Hedef Hesaplama ====================

    private void UpdateTargetsForCurrentState()
    {
        if (GameManager.Instance == null) return;

        int day = GameManager.Instance.CurrentDay;
        int rebellions = GameManager.Instance.RebellionCount;

        switch (day)
        {
            case 1:
                // Gün 1: Tamamen temiz
                targetLD = 0f;
                targetCA = 0f;
                targetFG = 0f;
                targetVig = 0f;
                targetSat = satNormal;
                targetBloom = bloomNormal;
                break;

            case 2:
                if (rebellions >= 2)
                {
                    // 2. isyan: Tam paket
                    targetLD = ldRebellion2;
                    targetCA = caRebellion2;
                    targetFG = fgRebellion2;
                    targetVig = vigDay2;
                    targetSat = satDay2;
                    targetBloom = bloomDay2;
                }
                else if (rebellions >= 1)
                {
                    // 1. isyan: Sadece lens + hafif vignette
                    targetLD = ldRebellion1;
                    targetCA = 0f;
                    targetFG = 0f;
                    targetVig = vigDay2 * 0.5f;
                    targetSat = satDay2 * 0.3f;
                    targetBloom = bloomNormal;
                }
                else
                {
                    // İsyan yok
                    targetLD = 0f;
                    targetCA = 0f;
                    targetFG = 0f;
                    targetVig = 0f;
                    targetSat = satNormal;
                    targetBloom = bloomNormal;
                }
                break;

            case 3:
            default:
                if (rebellions >= 1)
                {
                    // Gün 3 + isyan → FULL CHAOS
                    targetLD = ldFull;
                    targetCA = caFull;
                    targetFG = fgFull;
                    targetVig = vigFull;
                    targetSat = satFull;
                    targetBloom = bloomFull;
                }
                else
                {
                    // Gün 3 başlangıç — bozulma zaten var
                    targetLD = ldDay3Start;
                    targetCA = caDay3Start;
                    targetFG = fgDay3Start;
                    targetVig = vigDay3Start;
                    targetSat = satDay3Start;
                    targetBloom = bloomDay3;
                }
                break;
        }
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnDayChanged -= OnDayChanged;
            GameManager.Instance.OnFractureLevelChanged -= OnFractureChanged;
        }
    }
}
