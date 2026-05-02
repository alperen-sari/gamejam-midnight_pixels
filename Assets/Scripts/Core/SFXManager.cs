using UnityEngine;

/// <summary>
/// Ses efekti yöneticisi — tek bir AudioSource kullanır.
/// "One shot audio" spam'i OLUŞTURMAZ.
/// 
/// Kullanım: SFXManager.Play(clip, pos) veya SFXManager.Play2D(clip)
/// Sahneye boş bir GameObject ekle, bu scripti ata.
/// </summary>
public class SFXManager : MonoBehaviour
{
    public static SFXManager Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private float masterVolume = 1f;

    private AudioSource sfxSource;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Tek bir AudioSource oluştur — tüm one-shot sesler bundan çalar
        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
    }

    /// <summary>
    /// Belirtilen pozisyonda ses çalar. One shot — obje oluşturmaz.
    /// </summary>
    public static void Play(AudioClip clip, Vector3 position, float volume = 1f)
    {
        if (clip == null || Instance == null) return;

        float finalVolume = volume * Instance.masterVolume;
        Instance.sfxSource.PlayOneShot(clip, finalVolume);
    }

    /// <summary>
    /// 2D ses çalar (pozisyon fark etmez).
    /// </summary>
    public static void Play2D(AudioClip clip, float volume = 1f)
    {
        if (clip == null || Instance == null) return;

        float finalVolume = volume * Instance.masterVolume;
        Instance.sfxSource.PlayOneShot(clip, finalVolume);
    }
}
