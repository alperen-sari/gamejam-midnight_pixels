using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Rapor yazma mini-game'i. Ekranda rastgele harfler belirir,
/// doğru tuşa basarsan ilerlenir. Yanlış basarsan hata artar.
/// 
/// Kendi UI'ını otomatik oluşturur.
/// Bilgisayar masasına (Computer interactable) bağlanır.
/// </summary>
public class ReportMiniGame : MonoBehaviour
{
    public static ReportMiniGame Instance { get; private set; }

    [Header("Game Settings")]
    [SerializeField] private int totalKeysToPress = 8;       // Toplam basılacak tuş
    [SerializeField] private float timePerKey = 2f;          // Her tuş için süre
    [SerializeField] private int maxErrors = 3;              // Max yanlış basma hakkı

    [Header("Style")]
    [SerializeField] private int keyFontSize = 64;
    [SerializeField] private int progressFontSize = 20;
    [SerializeField] private Color correctColor = new Color(0.2f, 0.9f, 0.3f);
    [SerializeField] private Color wrongColor = new Color(0.9f, 0.2f, 0.2f);
    [SerializeField] private Color keyColor = Color.white;
    [SerializeField] private Color bgColor = new Color(0.08f, 0.08f, 0.12f, 0.95f);

    // UI (runtime oluşturulur)
    private GameObject gameCanvas;
    private GameObject panelObj;
    private TextMeshProUGUI keyText;
    private TextMeshProUGUI progressText;
    private TextMeshProUGUI feedbackText;
    private TextMeshProUGUI reportText;        // Yazılan rapor metni

    // Game State
    private bool isActive = false;
    private KeyCode currentKey;
    private int keysPressed = 0;
    private int errors = 0;
    private float keyTimer = 0f;
    private System.Action<bool> onCompleteCallback;  // true = başarılı, false = başarısız

    // Kullanılacak tuşlar
    private readonly KeyCode[] possibleKeys = {
        KeyCode.A, KeyCode.S, KeyCode.D, KeyCode.F,
        KeyCode.G, KeyCode.H, KeyCode.J, KeyCode.K,
        KeyCode.L, KeyCode.Q, KeyCode.W, KeyCode.E,
        KeyCode.R, KeyCode.T, KeyCode.Y, KeyCode.Z,
        KeyCode.X, KeyCode.C, KeyCode.V, KeyCode.B
    };

    // Raporda yazılan "kelimeler" (her doğru tuşta bir kelime eklenir)
    private readonly string[] reportWords = {
        "Bugünkü", "satış", "rakamları", "incelendiğinde,",
        "geçen", "aya", "göre", "artış",
        "gözlemlenmektedir.", "Detaylı", "analiz", "ekte",
        "sunulmuştur.", "Saygılarımla.", "İyi", "çalışmalar."
    };

    // İsyankar versiyonda yazılan kelimeler
    private readonly string[] doodleWords = {
        "bıktım", "bu", "işten", "neden",
        "her gün", "aynı", "rapor?!", "YETER",
        "★", "♪", "☺", "...",
        "özgürlük", "nerede?", "kaçmalıyım", "!!!"
    };

    public bool IsActive => isActive;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        CreateUI();
    }

    private void CreateUI()
    {
        // Canvas
        gameCanvas = new GameObject("ReportMiniGameCanvas");
        gameCanvas.transform.SetParent(transform);
        Canvas canvas = gameCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 250;

        CanvasScaler scaler = gameCanvas.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        gameCanvas.AddComponent<GraphicRaycaster>();

        // Ana panel (ekranın ortası)
        panelObj = new GameObject("MiniGamePanel");
        panelObj.transform.SetParent(gameCanvas.transform, false);

        Image panelBg = panelObj.AddComponent<Image>();
        panelBg.color = bgColor;

        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.2f, 0.15f);
        panelRect.anchorMax = new Vector2(0.8f, 0.85f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        // Rapor metni (yazılan metin üstte)
        GameObject reportObj = new GameObject("ReportText");
        reportObj.transform.SetParent(panelObj.transform, false);
        reportText = reportObj.AddComponent<TextMeshProUGUI>();
        reportText.fontSize = progressFontSize;
        reportText.color = new Color(0.7f, 0.7f, 0.7f);
        reportText.alignment = TextAlignmentOptions.TopLeft;
        reportText.text = "";

        RectTransform reportRect = reportObj.GetComponent<RectTransform>();
        reportRect.anchorMin = new Vector2(0f, 0.4f);
        reportRect.anchorMax = new Vector2(1f, 0.9f);
        reportRect.offsetMin = new Vector2(30, 0);
        reportRect.offsetMax = new Vector2(-30, -20);

        // Basılacak tuş (ortada büyük)
        GameObject keyObj = new GameObject("KeyDisplay");
        keyObj.transform.SetParent(panelObj.transform, false);
        keyText = keyObj.AddComponent<TextMeshProUGUI>();
        keyText.fontSize = keyFontSize;
        keyText.color = keyColor;
        keyText.alignment = TextAlignmentOptions.Center;
        keyText.fontStyle = FontStyles.Bold;

        RectTransform keyRect = keyObj.GetComponent<RectTransform>();
        keyRect.anchorMin = new Vector2(0.2f, 0.15f);
        keyRect.anchorMax = new Vector2(0.8f, 0.45f);
        keyRect.offsetMin = Vector2.zero;
        keyRect.offsetMax = Vector2.zero;

        // İlerleme (alt)
        GameObject progObj = new GameObject("ProgressText");
        progObj.transform.SetParent(panelObj.transform, false);
        progressText = progObj.AddComponent<TextMeshProUGUI>();
        progressText.fontSize = progressFontSize;
        progressText.color = Color.white;
        progressText.alignment = TextAlignmentOptions.Center;

        RectTransform progRect = progObj.GetComponent<RectTransform>();
        progRect.anchorMin = new Vector2(0f, 0.02f);
        progRect.anchorMax = new Vector2(0.5f, 0.12f);
        progRect.offsetMin = new Vector2(20, 0);
        progRect.offsetMax = Vector2.zero;

        // Geri bildirim (sağ alt — "Doğru!" / "Yanlış!")
        GameObject fbObj = new GameObject("FeedbackText");
        fbObj.transform.SetParent(panelObj.transform, false);
        feedbackText = fbObj.AddComponent<TextMeshProUGUI>();
        feedbackText.fontSize = progressFontSize + 4;
        feedbackText.color = correctColor;
        feedbackText.alignment = TextAlignmentOptions.Center;
        feedbackText.text = "";

        RectTransform fbRect = fbObj.GetComponent<RectTransform>();
        fbRect.anchorMin = new Vector2(0.5f, 0.02f);
        fbRect.anchorMax = new Vector2(1f, 0.12f);
        fbRect.offsetMin = Vector2.zero;
        fbRect.offsetMax = new Vector2(-20, 0);

        panelObj.SetActive(false);
    }

    // ==================== Oyun Kontrolü ====================

    /// <summary>
    /// Mini-game'i başlatır.
    /// </summary>
    /// <param name="isRebellion">True ise isyankar rapor yazar (doodle)</param>
    /// <param name="onComplete">Bitince callback (true=başarılı)</param>
    public void StartGame(bool isRebellion, System.Action<bool> onComplete)
    {
        if (isActive) return;

        isActive = true;
        keysPressed = 0;
        errors = 0;
        onCompleteCallback = onComplete;

        reportText.text = isRebellion ? "<i>// Kişisel notlar:</i>\n" : "<i>// Günlük Rapor:</i>\n";
        panelObj.SetActive(true);

        // Oyuncuyu durdur
        Player player = FindFirstObjectByType<Player>();
        if (player != null) player.SetCanMove(false);

        ShowNextKey();
    }

    private void ShowNextKey()
    {
        if (keysPressed >= totalKeysToPress)
        {
            EndGame(true);
            return;
        }

        currentKey = possibleKeys[Random.Range(0, possibleKeys.Length)];
        keyText.text = currentKey.ToString();
        keyText.color = keyColor;
        keyTimer = timePerKey;

        UpdateProgress();
    }

    void Update()
    {
        if (!isActive) return;

        // Süre sayacı
        keyTimer -= Time.deltaTime;
        if (keyTimer <= 0f)
        {
            // Süre doldu → hata
            OnWrongKey();
            return;
        }

        // Süre azaldıkça tuş rengi kırmızılaşır
        float timePercent = keyTimer / timePerKey;
        if (timePercent < 0.3f)
        {
            keyText.color = Color.Lerp(wrongColor, keyColor, timePercent / 0.3f);
        }

        // Tuş kontrolü
        if (Input.anyKeyDown)
        {
            // Doğru tuşa mı basıldı?
            if (Input.GetKeyDown(currentKey))
            {
                OnCorrectKey();
            }
            else if (!Input.GetKeyDown(KeyCode.Escape))
            {
                // Yanlış tuş (escape hariç)
                foreach (var key in possibleKeys)
                {
                    if (Input.GetKeyDown(key))
                    {
                        OnWrongKey();
                        break;
                    }
                }
            }
        }

        // Escape ile iptal
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            EndGame(false);
        }
    }

    private void OnCorrectKey()
    {
        keysPressed++;

        // Rapor metnine kelime ekle
        bool isRebellion = reportText.text.Contains("notlar");
        string[] words = isRebellion ? doodleWords : reportWords;
        int wordIndex = (keysPressed - 1) % words.Length;
        reportText.text += words[wordIndex] + " ";

        // Her 4 kelimede satır atla
        if (keysPressed % 4 == 0) reportText.text += "\n";

        // Feedback
        feedbackText.text = "✓ Doğru!";
        feedbackText.color = correctColor;

        ShowNextKey();
    }

    private void OnWrongKey()
    {
        errors++;

        feedbackText.text = $"✗ Yanlış! ({errors}/{maxErrors})";
        feedbackText.color = wrongColor;

        if (errors >= maxErrors)
        {
            EndGame(false);
            return;
        }

        // Yeni tuş göster
        ShowNextKey();
    }

    private void UpdateProgress()
    {
        progressText.text = $"İlerleme: {keysPressed}/{totalKeysToPress}  |  Hata: {errors}/{maxErrors}";
    }

    private void EndGame(bool success)
    {
        isActive = false;
        panelObj.SetActive(false);

        // Oyuncuyu serbest bırak
        Player player = FindFirstObjectByType<Player>();
        if (player != null) player.SetCanMove(true);

        Debug.Log($"[ReportMiniGame] Oyun bitti. Başarılı: {success}");

        onCompleteCallback?.Invoke(success);
        onCompleteCallback = null;
    }
}
