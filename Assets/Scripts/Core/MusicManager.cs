using UnityEngine;

/// <summary>
/// Gün bazlı arka plan müziği yöneticisi.
/// Crossfade ile gün geçişi. Inspector'dan volume ayarı.
/// null clip = o gün müzik yok.
/// 
/// Diyalog sırasında müziği kısar (DuckVolume), bitince normale döner.
/// </summary>
public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    [Header("=== GÜN 1 MÜZİĞİ ===")]
    [SerializeField] private AudioClip day1Music;
    [SerializeField] [Range(0f, 1f)] private float day1Volume = 0.35f;

    [Header("=== GÜN 2 MÜZİĞİ ===")]
    [SerializeField] private AudioClip day2Music;
    [SerializeField] [Range(0f, 1f)] private float day2Volume = 0.35f;

    [Header("=== GÜN 3 MÜZİĞİ ===")]
    [SerializeField] private AudioClip day3Music;
    [SerializeField] [Range(0f, 1f)] private float day3Volume = 0.35f;

    [Header("=== GENEL AYARLAR ===")]
    [SerializeField] [Range(0f, 1f)] private float masterMusicVolume = 1f;
    [SerializeField] private float fadeSpeed = 1.5f;

    private AudioSource musicSourceA;
    private AudioSource musicSourceB;
    private bool usingA = true;
    private float targetVolumeA = 0f;
    private float targetVolumeB = 0f;
    private float currentDayVolume = 0.35f;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        musicSourceA = gameObject.AddComponent<AudioSource>();
        musicSourceA.playOnAwake = false;
        musicSourceA.loop = true;
        musicSourceA.volume = 0f;

        musicSourceB = gameObject.AddComponent<AudioSource>();
        musicSourceB.playOnAwake = false;
        musicSourceB.loop = true;
        musicSourceB.volume = 0f;
    }

    void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnDayChanged += OnDayChanged;
        }
        PlayMusicForDay(GameManager.Instance != null ? GameManager.Instance.CurrentDay : 1);
    }

    void Update()
    {
        musicSourceA.volume = Mathf.MoveTowards(musicSourceA.volume, targetVolumeA * masterMusicVolume, fadeSpeed * Time.deltaTime);
        musicSourceB.volume = Mathf.MoveTowards(musicSourceB.volume, targetVolumeB * masterMusicVolume, fadeSpeed * Time.deltaTime);

        if (musicSourceA.volume <= 0.01f && musicSourceA.isPlaying && targetVolumeA == 0f)
            musicSourceA.Stop();
        if (musicSourceB.volume <= 0.01f && musicSourceB.isPlaying && targetVolumeB == 0f)
            musicSourceB.Stop();
    }

    private void OnDayChanged(int newDay)
    {
        PlayMusicForDay(newDay);
    }

    private void PlayMusicForDay(int day)
    {
        AudioClip clip = null;
        float vol = 0.35f;

        switch (day)
        {
            case 1: clip = day1Music; vol = day1Volume; break;
            case 2: clip = day2Music; vol = day2Volume; break;
            case 3: clip = day3Music; vol = day3Volume; break;
            default: clip = day3Music; vol = day3Volume; break;
        }

        currentDayVolume = vol;

        if (clip == null)
        {
            targetVolumeA = 0f;
            targetVolumeB = 0f;
            return;
        }

        if (usingA)
        {
            musicSourceB.clip = clip;
            musicSourceB.Play();
            targetVolumeB = vol;
            targetVolumeA = 0f;
        }
        else
        {
            musicSourceA.clip = clip;
            musicSourceA.Play();
            targetVolumeA = vol;
            targetVolumeB = 0f;
        }

        usingA = !usingA;
    }

    /// <summary>
    /// Müzik kıs (diyalog vs. için).
    /// </summary>
    public void DuckVolume(float duckFactor = 0.4f)
    {
        float ducked = currentDayVolume * duckFactor;
        if (usingA) targetVolumeB = ducked;
        else targetVolumeA = ducked;

        // Ambient'ı da kıs
        if (AmbientManager.Instance != null) AmbientManager.Instance.Duck(duckFactor);
    }

    /// <summary>
    /// Müzik normale dön.
    /// </summary>
    public void RestoreVolume()
    {
        if (usingA) targetVolumeB = currentDayVolume;
        else targetVolumeA = currentDayVolume;

        if (AmbientManager.Instance != null) AmbientManager.Instance.Restore();
    }

    public void StopAll()
    {
        targetVolumeA = 0f;
        targetVolumeB = 0f;
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnDayChanged -= OnDayChanged;
        }
    }
}
