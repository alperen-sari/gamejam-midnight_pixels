using UnityEngine;

/// <summary>
/// Ses efekti yöneticisi — tek bir AudioSource kullanır.
/// "One shot audio" spam'i OLUŞTURMAZ.
/// 
/// Instance yoksa fallback olarak AudioSource.PlayClipAtPoint kullanır.
/// Sahneye boş bir GameObject ekle, bu scripti ata.
/// </summary>
public class SFXManager : MonoBehaviour
{
    public static SFXManager Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] [Range(0f, 1f)] private float masterVolume = 1f;

    private AudioSource sfxSource;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
    }

    /// <summary>
    /// Ses çalar. Instance yoksa fallback kullanır.
    /// </summary>
    public static void Play(AudioClip clip, Vector3 position, float volume = 1f)
    {
        if (clip == null) return;

        if (Instance != null && Instance.sfxSource != null)
        {
            float finalVolume = volume * Instance.masterVolume;
            Instance.sfxSource.PlayOneShot(clip, finalVolume);
        }
        else
        {
            // Fallback — Instance yoksa bile çal
            Debug.LogWarning("[SFXManager] Instance yok! Fallback kullanılıyor.");
            AudioSource.PlayClipAtPoint(clip, position, volume);
        }
    }

    /// <summary>
    /// 2D ses çalar (pozisyon fark etmez).
    /// </summary>
    public static void Play2D(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;

        if (Instance != null && Instance.sfxSource != null)
        {
            float finalVolume = volume * Instance.masterVolume;
            Instance.sfxSource.PlayOneShot(clip, finalVolume);
        }
        else
        {
            Debug.LogWarning("[SFXManager] Instance yok! Fallback kullanılıyor.");
            if (Camera.main != null)
            {
                AudioSource.PlayClipAtPoint(clip, Camera.main.transform.position, volume);
            }
        }
    }
}
