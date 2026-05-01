using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Ekranın sağ üstünde "Müdür Güveni" barı gösterir.
/// Kendi UI'ını otomatik oluşturur — boş bir GameObject'e ekle, çalışır.
/// 
/// Güven düştükçe bar kısalır ve renk yeşilden kırmızıya geçer.
/// </summary>
public class BossTrustUI : MonoBehaviour
{
    [Header("Style")]
    [SerializeField] private float barWidth = 200f;
    [SerializeField] private float barHeight = 22f;
    [SerializeField] private Color highTrustColor = new Color(0.2f, 0.8f, 0.3f);     // Yeşil
    [SerializeField] private Color mediumTrustColor = new Color(0.9f, 0.7f, 0.1f);   // Sarı
    [SerializeField] private Color lowTrustColor = new Color(0.9f, 0.2f, 0.2f);      // Kırmızı
    [SerializeField] private Color bgBarColor = new Color(0.15f, 0.15f, 0.2f, 0.8f);

    private Image fillBar;
    private TextMeshProUGUI labelText;
    private float displayedTrust = 100f;
    private float targetTrust = 100f;

    void Start()
    {
        CreateUI();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnBossTrustChanged += OnTrustChanged;
            targetTrust = GameManager.Instance.BossTrust;
            displayedTrust = targetTrust;
            UpdateBar();
        }
    }

    private void CreateUI()
    {
        // Canvas
        GameObject canvasObj = new GameObject("BossTrustCanvas");
        canvasObj.transform.SetParent(transform);
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<GraphicRaycaster>();

        // Container (sağ üst)
        GameObject container = new GameObject("TrustContainer");
        container.transform.SetParent(canvasObj.transform, false);
        RectTransform contRect = container.AddComponent<RectTransform>();
        contRect.anchorMin = new Vector2(1f, 1f);
        contRect.anchorMax = new Vector2(1f, 1f);
        contRect.pivot = new Vector2(1f, 1f);
        contRect.anchoredPosition = new Vector2(-20f, -20f);
        contRect.sizeDelta = new Vector2(barWidth + 10, 50f);

        // Label: "Müdür Güveni"
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(container.transform, false);
        labelText = labelObj.AddComponent<TextMeshProUGUI>();
        labelText.text = "Müdür Güveni";
        labelText.fontSize = 16;
        labelText.color = Color.white;
        labelText.alignment = TextAlignmentOptions.BottomRight;

        RectTransform labelRect = labelObj.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 0.5f);
        labelRect.anchorMax = new Vector2(1f, 1f);
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        // Bar arka planı
        GameObject bgObj = new GameObject("BarBackground");
        bgObj.transform.SetParent(container.transform, false);
        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.color = bgBarColor;

        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0f, 0f);
        bgRect.anchorMax = new Vector2(1f, 0.45f);
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        // Bar doluluk (fill)
        GameObject fillObj = new GameObject("BarFill");
        fillObj.transform.SetParent(bgObj.transform, false);
        fillBar = fillObj.AddComponent<Image>();
        fillBar.color = highTrustColor;

        RectTransform fillRect = fillObj.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(1f, 1f);
        fillRect.pivot = new Vector2(0f, 0.5f);
        fillRect.offsetMin = new Vector2(2, 2);
        fillRect.offsetMax = new Vector2(-2, -2);
    }

    private void OnTrustChanged(float newTrust)
    {
        targetTrust = newTrust;
    }

    void Update()
    {
        // Smooth animasyon
        if (!Mathf.Approximately(displayedTrust, targetTrust))
        {
            displayedTrust = Mathf.Lerp(displayedTrust, targetTrust, Time.deltaTime * 5f);
            UpdateBar();
        }
    }

    private void UpdateBar()
    {
        if (fillBar == null) return;

        float maxTrust = GameManager.Instance != null ? 100f : 100f;
        float percent = displayedTrust / maxTrust;

        // Bar genişliğini ayarla
        RectTransform fillRect = fillBar.GetComponent<RectTransform>();
        fillRect.anchorMax = new Vector2(percent, 1f);

        // Renge göre renk geçişi
        if (percent > 0.6f)
        {
            fillBar.color = Color.Lerp(mediumTrustColor, highTrustColor, (percent - 0.6f) / 0.4f);
        }
        else if (percent > 0.3f)
        {
            fillBar.color = Color.Lerp(lowTrustColor, mediumTrustColor, (percent - 0.3f) / 0.3f);
        }
        else
        {
            fillBar.color = lowTrustColor;
        }
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnBossTrustChanged -= OnTrustChanged;
        }
    }
}
