using UnityEngine;

/// <summary>
/// Kamerada anomali efektleri: titreme, zoom bozulması, kayma.
/// Kamera objesine eklenir.
/// </summary>
public class CameraAnomaly : AnomalyBase
{
    [Header("Camera Anomaly Settings")]
    [SerializeField] private float shakeDuration = 0.5f;
    [SerializeField] private float shakeIntensity = 0.15f;
    [SerializeField] private float zoomGlitchAmount = 0.5f;

    private Camera cam;
    private Vector3 originalPosition;
    private float originalSize;
    private bool isShaking = false;

    void Start()
    {
        cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;
        
        if (cam != null)
        {
            originalSize = cam.orthographicSize;
        }
    }

    protected override void OnTrigger()
    {
        if (isShaking || cam == null) return;

        // Kırılma seviyesine göre farklı efekt
        FractureStage stage = GameManager.Instance != null 
            ? GameManager.Instance.CurrentFractureStage 
            : FractureStage.Subtle;

        switch (stage)
        {
            case FractureStage.Subtle:
                StartCoroutine(CameraShake(shakeDuration, shakeIntensity * 0.5f));
                break;
            case FractureStage.Noticeable:
                StartCoroutine(CameraShake(shakeDuration * 1.5f, shakeIntensity));
                StartCoroutine(ZoomGlitch());
                break;
            case FractureStage.Breaking:
                StartCoroutine(CameraShake(shakeDuration * 2f, shakeIntensity * 2f));
                StartCoroutine(ZoomGlitch());
                StartCoroutine(CameraDrift());
                break;
        }
    }

    private System.Collections.IEnumerator CameraShake(float duration, float intensity)
    {
        isShaking = true;
        originalPosition = transform.localPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float x = Random.Range(-intensity, intensity);
            float y = Random.Range(-intensity, intensity);
            transform.localPosition = originalPosition + new Vector3(x, y, 0f);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = originalPosition;
        isShaking = false;
    }

    private System.Collections.IEnumerator ZoomGlitch()
    {
        float targetSize = originalSize + Random.Range(-zoomGlitchAmount, zoomGlitchAmount);
        float duration = 0.3f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetSize, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Geri dön
        elapsed = 0f;
        while (elapsed < duration)
        {
            cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, originalSize, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        cam.orthographicSize = originalSize;
    }

    private System.Collections.IEnumerator CameraDrift()
    {
        originalPosition = transform.localPosition;
        float driftDuration = 2f;
        float elapsed = 0f;
        Vector3 driftTarget = originalPosition + new Vector3(
            Random.Range(-0.5f, 0.5f),
            Random.Range(-0.5f, 0.5f),
            0f
        );

        // Yavaşça kayır
        while (elapsed < driftDuration)
        {
            transform.localPosition = Vector3.Lerp(originalPosition, driftTarget, elapsed / driftDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Geri dön
        elapsed = 0f;
        while (elapsed < driftDuration * 0.5f)
        {
            transform.localPosition = Vector3.Lerp(driftTarget, originalPosition, elapsed / (driftDuration * 0.5f));
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = originalPosition;
    }
}
