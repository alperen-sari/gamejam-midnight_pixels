using UnityEngine;

/// <summary>
/// Basit ses efekti yöneticisi.
/// Herhangi bir yerden SFXManager.Play(clip, position) ile ses çalabilirsin.
/// 
/// Sahneye boş bir GameObject ekle, bu scripti ata.
/// </summary>
public class SFXManager : MonoBehaviour
{
    public static SFXManager Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private float masterVolume = 1f;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// Belirtilen pozisyonda ses çalar.
    /// Herhangi bir script'ten çağrılabilir.
    /// </summary>
    public static void Play(AudioClip clip, Vector3 position, float volume = 1f)
    {
        if (clip == null) return;

        float finalVolume = volume;
        if (Instance != null) finalVolume *= Instance.masterVolume;

        AudioSource.PlayClipAtPoint(clip, position, finalVolume);
    }

    /// <summary>
    /// 2D ses çalar (pozisyon fark etmez).
    /// </summary>
    public static void Play2D(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;

        float finalVolume = volume;
        if (Instance != null) finalVolume *= Instance.masterVolume;

        // Kameranın pozisyonunda çal (2D efekti)
        if (Camera.main != null)
        {
            AudioSource.PlayClipAtPoint(clip, Camera.main.transform.position, finalVolume);
        }
    }
}
