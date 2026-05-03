using UnityEngine;

/// <summary>
/// Ofisteki objeleri rastgele hareket ettirir, döndürür veya titretir.
/// Kırılma seviyesine göre yoğunluk artar.
/// </summary>
public class ObjectAnomaly : AnomalyBase
{
    public enum AnomalyType
    {
        Shake,       // Obje titrer
        Float,       // Obje havaya kalkar
        Rotate,      // Obje döner
        Teleport     // Obje başka yere ışınlanır
    }

    [Header("Object Anomaly Settings")]
    [SerializeField] private AnomalyType type = AnomalyType.Shake;
    [SerializeField] private float duration = 2f;
    [SerializeField] private float intensity = 0.3f;

    [Header("Sound")]
    [SerializeField] private AudioClip glitchSound;        // Bozulma anında çalan ses
    [SerializeField] [Range(0f, 1f)] private float glitchSoundVol = 0.7f;

    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private bool isActive = false;

    protected override void Start()
    {
        base.Start();  // AnomalyBase kayıt işlemi

        originalPosition = transform.localPosition;
        originalRotation = transform.localRotation;
    }

    protected override void OnTrigger()
    {
        if (isActive) return;

        // Bozulma sesi
        SFXManager.Play(glitchSound, transform.position, glitchSoundVol);

        // Kırılma seviyesine göre yoğunluğu ayarla
        float fractureMultiplier = 1f;
        if (GameManager.Instance != null)
        {
            fractureMultiplier = 1f + GameManager.Instance.FracturePercent * 2f;
        }

        switch (type)
        {
            case AnomalyType.Shake:
                StartCoroutine(ShakeRoutine(duration, intensity * fractureMultiplier));
                break;
            case AnomalyType.Float:
                StartCoroutine(FloatRoutine(duration, intensity * fractureMultiplier));
                break;
            case AnomalyType.Rotate:
                StartCoroutine(RotateRoutine(duration, intensity * fractureMultiplier));
                break;
            case AnomalyType.Teleport:
                TeleportNearby(intensity * fractureMultiplier);
                break;
        }
    }

    private System.Collections.IEnumerator ShakeRoutine(float dur, float inten)
    {
        isActive = true;
        float elapsed = 0f;

        while (elapsed < dur)
        {
            Vector3 offset = new Vector3(
                Random.Range(-inten, inten),
                Random.Range(-inten, inten),
                0f
            );
            transform.localPosition = originalPosition + offset;
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = originalPosition;
        isActive = false;
    }

    private System.Collections.IEnumerator FloatRoutine(float dur, float inten)
    {
        isActive = true;
        float elapsed = 0f;

        while (elapsed < dur)
        {
            float yOffset = Mathf.Sin(elapsed * 3f) * inten;
            transform.localPosition = originalPosition + new Vector3(0f, yOffset, 0f);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = originalPosition;
        isActive = false;
    }

    private System.Collections.IEnumerator RotateRoutine(float dur, float inten)
    {
        isActive = true;
        float elapsed = 0f;
        float rotationSpeed = inten * 360f;

        while (elapsed < dur)
        {
            transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localRotation = originalRotation;
        isActive = false;
    }

    private void TeleportNearby(float range)
    {
        Vector2 randomOffset = Random.insideUnitCircle * range;
        transform.localPosition = originalPosition + new Vector3(randomOffset.x, randomOffset.y, 0f);

        // Bir süre sonra geri dön
        StartCoroutine(ReturnToOriginal(3f));
    }

    private System.Collections.IEnumerator ReturnToOriginal(float delay)
    {
        yield return new WaitForSeconds(delay);
        transform.localPosition = originalPosition;
    }
}
