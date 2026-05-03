using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Sözleşme yırtma mini-game: SPACE'e hızlı bas, bar dolsun!
/// Bar dolunca sözleşme yırtılır.
/// Basmazsan bar yavaşça azalır.
///
/// Sahneye: Create Empty → "TearMiniGame" → Add Component: TearMiniGame
/// </summary>
public class TearMiniGame : MonoBehaviour
{
    public static TearMiniGame Instance { get; private set; }

    [Header("Gameplay")]
    [SerializeField] private float fillPerPress = 0.04f;     // Her basışta ne kadar dolar
    [SerializeField] private float drainSpeed = 0.15f;       // Basmazsan ne kadar azalır/sn
    [SerializeField] private float shakeIntensity = 5f;      // Bar doldukça titreme

    [Header("Sound")]
    [SerializeField] private AudioClip mashSound;            // Her basışta ses
    [SerializeField] [Range(0f, 1f)] private float mashSoundVol = 0.3f;
    [SerializeField] private AudioClip winSound;
    [SerializeField] [Range(0f, 1f)] private float winSoundVol = 0.6f;

    [Header("Visuals")]
    [SerializeField] private Color barBgColor = new Color(0.15f, 0.15f, 0.2f);
    [SerializeField] private Color barFillColor = new Color(0.9f, 0.3f, 0.2f);
    [SerializeField] private Color barFullColor = new Color(1f, 0.1f, 0.05f);

    // UI
    private GameObject gameCanvas;
    private GameObject panelObj;
    private Image barFill;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI instructionText;
    private RectTransform barRect;

    // State
    private bool isActive = false;
    private float progress = 0f;
    private System.Action<bool> onCompleteCallback;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        CreateUI();
    }

    private void CreateUI()
    {
        gameCanvas = new GameObject("TearMiniGameCanvas");
        gameCanvas.transform.SetParent(transform);
        Canvas c = gameCanvas.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 280;
        CanvasScaler s = gameCanvas.AddComponent<CanvasScaler>();
        s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        s.referenceResolution = new Vector2(1920, 1080);
        gameCanvas.AddComponent<GraphicRaycaster>();

        // Arka plan
        panelObj = new GameObject("BgPanel");
        panelObj.transform.SetParent(gameCanvas.transform, false);
        Image bg = panelObj.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.75f);
        SetFullStretch(panelObj);

        // Başlık
        GameObject titleObj = CreateRect(panelObj, "Title",
            new Vector2(0.15f, 0.72f), new Vector2(0.85f, 0.85f));
        titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "SÖZLEŞMEYİ YIRT!";
        titleText.fontSize = 48;
        titleText.color = Color.white;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.Center;

        // Talimat
        GameObject instrObj = CreateRect(panelObj, "Instruction",
            new Vector2(0.2f, 0.62f), new Vector2(0.8f, 0.72f));
        instructionText = instrObj.AddComponent<TextMeshProUGUI>();
        instructionText.text = "[ SPACE ] tuşuna bas! Hızlı bas!";
        instructionText.fontSize = 24;
        instructionText.color = new Color(0.9f, 0.9f, 0.6f);
        instructionText.alignment = TextAlignmentOptions.Center;

        // Bar arka planı
        GameObject barBg = CreateRect(panelObj, "BarBg",
            new Vector2(0.2f, 0.4f), new Vector2(0.8f, 0.55f));
        Image barBgImg = barBg.AddComponent<Image>();
        barBgImg.color = barBgColor;
        barRect = barBg.GetComponent<RectTransform>();

        // Outline efekti
        Outline outline = barBg.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 1f, 1f, 0.3f);
        outline.effectDistance = new Vector2(2f, 2f);

        // Bar dolgu
        GameObject fillObj = new GameObject("BarFill");
        fillObj.transform.SetParent(barBg.transform, false);
        barFill = fillObj.AddComponent<Image>();
        barFill.color = barFillColor;
        RectTransform fillRect = fillObj.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = new Vector2(0f, 1f);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        panelObj.SetActive(false);
    }

    // ==================== Başlat ====================

    public void StartGame(System.Action<bool> onComplete = null)
    {
        if (isActive) return;
        onCompleteCallback = onComplete;
        isActive = true;
        progress = 0f;

        panelObj.SetActive(true);

        Player p = FindFirstObjectByType<Player>();
        if (p != null) p.SetCanMove(false);
    }

    // ==================== Update ====================

    void Update()
    {
        if (!isActive) return;

        // SPACE basıldı mı?
        if (Input.GetKeyDown(KeyCode.Space))
        {
            progress += fillPerPress;
            SFXManager.Play2D(mashSound, mashSoundVol);
        }

        // Basmazsan azalır
        progress -= drainSpeed * Time.deltaTime;
        progress = Mathf.Clamp01(progress);

        // UI güncelle
        UpdateUI();

        // Kazandı mı?
        if (progress >= 1f)
        {
            SFXManager.Play2D(winSound, winSoundVol);
            EndGame(true);
        }
    }

    private void UpdateUI()
    {
        // Bar dolgusu
        RectTransform fillRect = barFill.GetComponent<RectTransform>();
        fillRect.anchorMax = new Vector2(progress, 1f);

        // Renk geçişi (kırmızıya döner)
        barFill.color = Color.Lerp(barFillColor, barFullColor, progress);

        // Titreme efekti (bar doldukça artar)
        if (progress > 0.3f)
        {
            float shake = shakeIntensity * progress;
            float x = Random.Range(-shake, shake);
            float y = Random.Range(-shake, shake);
            barRect.anchoredPosition = new Vector2(x, y);
        }
        else
        {
            barRect.anchoredPosition = Vector2.zero;
        }

        // Yazı değişimi
        if (progress > 0.8f)
            instructionText.text = "BIRAKMA! AZ KALDI!";
        else if (progress > 0.5f)
            instructionText.text = "DEVAM ET! BAS BAS BAS!";
        else
            instructionText.text = "[ SPACE ] tuşuna bas! Hızlı bas!";
    }

    private void EndGame(bool success)
    {
        isActive = false;
        barRect.anchoredPosition = Vector2.zero;
        panelObj.SetActive(false);

        Player p = FindFirstObjectByType<Player>();
        if (p != null) p.SetCanMove(true);

        onCompleteCallback?.Invoke(success);
        onCompleteCallback = null;
    }

    // ==================== Yardımcılar ====================

    private GameObject CreateRect(GameObject parent, string name, Vector2 ancMin, Vector2 ancMax)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent.transform, false);
        RectTransform r = obj.AddComponent<RectTransform>();
        r.anchorMin = ancMin; r.anchorMax = ancMax;
        r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;
        return obj;
    }

    private void SetFullStretch(GameObject obj)
    {
        RectTransform r = obj.GetComponent<RectTransform>();
        if (r == null) r = obj.AddComponent<RectTransform>();
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
        r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;
    }
}
