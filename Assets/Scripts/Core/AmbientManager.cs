using UnityEngine;

/// <summary>
/// Katmanlı ofis ortam sesleri yöneticisi.
/// 
/// Gün 1: Rutin ofis atmosferi (saat, klavye, fotokopi — ritimli, sakin)
/// Gün 2: Sesler hafif bozulmaya başlar (pitch/volume değişimleri)
/// Gün 3: Ofis sesleri neredeyse kaybolur, anomali devralır
/// 
/// Her ses katmanı kendi AudioSource'unda loop olarak çalar.
/// Inspector'dan her katmanın volume'ü ayrı ayarlanır.
/// 
/// Kullanım: Sahneye boş GameObject → bu scripti ekle.
/// </summary>
public class AmbientManager : MonoBehaviour
{
    public static AmbientManager Instance { get; private set; }

    [Header("=== SAAT TİK-TAK ===")]
    [SerializeField] private AudioClip clockTickClip;
    [SerializeField] [Range(0f, 1f)] private float clockVolume = 0.25f;

    [Header("=== KLAVYERİTMİ ===")]
    [SerializeField] private AudioClip keyboardClip;
    [SerializeField] [Range(0f, 1f)] private float keyboardVolume = 0.15f;

    [Header("=== FOTOKOPİ / YAZICI GÜRÜLTÜSÜ ===")]
    [SerializeField] private AudioClip copierClip;
    [SerializeField] [Range(0f, 1f)] private float copierVolume = 0.1f;

    [Header("=== OFİS UĞULTUSU (genel muhabbet) ===")]
    [SerializeField] private AudioClip officeMurmurClip;
    [SerializeField] [Range(0f, 1f)] private float murmurVolume = 0.08f;

    [Header("=== GENEL AYARLAR ===")]
    [SerializeField] [Range(0f, 1f)] private float masterAmbientVolume = 1f;
    [SerializeField] private float dayTransitionSpeed = 1f;

    // AudioSource'lar (her katman ayrı)
    private AudioSource clockSource;
    private AudioSource keyboardSource;
    private AudioSource copierSource;
    private AudioSource murmurSource;

    // Hedef volume'ler (gün bazlı)
    private float targetClock, targetKeyboard, targetCopier, targetMurmur;
    // Hedef pitch'ler (anomali)
    private float targetClockPitch = 1f, targetKeyboardPitch = 1f;
    private float targetCopierPitch = 1f, targetMurmurPitch = 1f;

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
        // Her katman için AudioSource oluştur
        clockSource = CreateLayer("Clock", clockTickClip, clockVolume);
        keyboardSource = CreateLayer("Keyboard", keyboardClip, keyboardVolume);
        copierSource = CreateLayer("Copier", copierClip, copierVolume);
        murmurSource = CreateLayer("Murmur", officeMurmurClip, murmurVolume);

        // Event'lere bağlan
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnDayChanged += OnDayChanged;
            GameManager.Instance.OnFractureLevelChanged += OnFractureChanged;
        }

        // Mevcut durumu uygula
        ApplyDaySettings(GameManager.Instance != null ? GameManager.Instance.CurrentDay : 1);
    }

    private AudioSource CreateLayer(string layerName, AudioClip clip, float volume)
    {
        if (clip == null) return null;

        GameObject obj = new GameObject($"Ambient_{layerName}");
        obj.transform.SetParent(transform);

        AudioSource source = obj.AddComponent<AudioSource>();
        source.clip = clip;
        source.loop = true;
        source.playOnAwake = false;
        source.volume = volume * masterAmbientVolume;
        source.spatialBlend = 0f;  // Tamamen 2D
        source.Play();

        return source;
    }

    void Update()
    {
        float speed = dayTransitionSpeed * Time.deltaTime;

        // Volume smooth geçiş
        SmoothVolume(clockSource, targetClock, speed);
        SmoothVolume(keyboardSource, targetKeyboard, speed);
        SmoothVolume(copierSource, targetCopier, speed);
        SmoothVolume(murmurSource, targetMurmur, speed);

        // Pitch smooth geçiş
        SmoothPitch(clockSource, targetClockPitch, speed);
        SmoothPitch(keyboardSource, targetKeyboardPitch, speed);
        SmoothPitch(copierSource, targetCopierPitch, speed);
        SmoothPitch(murmurSource, targetMurmurPitch, speed);
    }

    private void SmoothVolume(AudioSource source, float target, float speed)
    {
        if (source == null) return;
        source.volume = Mathf.Lerp(source.volume, target * masterAmbientVolume, speed);
    }

    private void SmoothPitch(AudioSource source, float target, float speed)
    {
        if (source == null) return;
        source.pitch = Mathf.Lerp(source.pitch, target, speed);
    }

    // ==================== Gün Bazlı Ayarlar ====================

    private void OnDayChanged(int day)
    {
        ApplyDaySettings(day);
    }

    private void OnFractureChanged(float fracture)
    {
        // Kırılma arttıkça sesler bozulur
        if (GameManager.Instance == null) return;
        float percent = GameManager.Instance.FracturePercent;

        // Pitch hafif bozulma (0% = normal, 100% = çok bozuk)
        float pitchDistort = 1f - (percent * 0.15f);  // Max %15 yavaşlama
        targetClockPitch = pitchDistort;
        targetKeyboardPitch = Mathf.Lerp(1f, 0.8f, percent);
        targetCopierPitch = Mathf.Lerp(1f, 0.7f, percent);
    }

    private void ApplyDaySettings(int day)
    {
        switch (day)
        {
            case 1:
                // TAM OFİS ATMOSFERİ — rutin, sakin, ritimli
                targetClock = clockVolume;
                targetKeyboard = keyboardVolume;
                targetCopier = copierVolume;
                targetMurmur = murmurVolume;

                targetClockPitch = 1f;
                targetKeyboardPitch = 1f;
                targetCopierPitch = 1f;
                targetMurmurPitch = 1f;
                break;

            case 2:
                // OFİS BOZULMAYA BAŞLIYOR — bazı sesler kısılır/yavaşlar
                targetClock = clockVolume * 0.7f;
                targetKeyboard = keyboardVolume * 0.5f;
                targetCopier = copierVolume * 0.3f;
                targetMurmur = murmurVolume * 0.4f;

                targetClockPitch = 0.95f;
                targetKeyboardPitch = 0.9f;
                targetCopierPitch = 0.85f;
                targetMurmurPitch = 0.9f;
                break;

            case 3:
            default:
                // OFİS NEREDEYSE ÖLMÜŞ — sadece saat hafifçe tıkırdıyor
                targetClock = clockVolume * 0.3f;
                targetKeyboard = 0f;
                targetCopier = 0f;
                targetMurmur = 0f;

                targetClockPitch = 0.8f;  // Yavaşlamış saat
                break;
        }

        Debug.Log($"[AmbientManager] Gün {day} ambient ayarları uygulandı.");
    }

    // ==================== Dış Kontrol ====================

    /// <summary>
    /// Tüm ambient sesleri kıs (diyalog vs. için).
    /// </summary>
    public void Duck(float factor = 0.3f)
    {
        masterAmbientVolume = factor;
    }

    /// <summary>
    /// Ambient sesleri normale döndür.
    /// </summary>
    public void Restore()
    {
        masterAmbientVolume = 1f;
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
