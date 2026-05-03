using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Rapor yazma mini-game'i.
/// 
/// 3 aşama:
///   1. Talimat Ekranı → oyunu anlat, E ile başlat
///   2. Oyun → harflere bas, rapor yaz
///   3. Sonuç Ekranı → rapor gösterilir, E ile kapat
/// 
/// UI: Monitör çerçevesi efekti (scanline + yeşil terminal hissi)
/// </summary>
public class ReportMiniGame : MonoBehaviour
{
    public static ReportMiniGame Instance { get; private set; }

    [Header("Game Settings")]
    [SerializeField] private int totalKeysToPress = 8;
    [SerializeField] private float timePerKey = 2f;
    [SerializeField] private int maxErrors = 3;

    [Header("Style")]
    [SerializeField] private int keyFontSize = 64;
    [SerializeField] private int bodyFontSize = 20;
    [SerializeField] private Color correctColor = new Color(0.2f, 0.9f, 0.3f);
    [SerializeField] private Color wrongColor = new Color(0.9f, 0.2f, 0.2f);
    [SerializeField] private Color terminalGreen = new Color(0.3f, 1f, 0.4f);
    [SerializeField] private Color bgColor = new Color(0.04f, 0.06f, 0.04f, 0.97f);
    [SerializeField] private Color headerColor = new Color(0.1f, 0.2f, 0.1f);

    [Header("Sound")]
    [SerializeField] private AudioClip correctKeySound;      // Doğru tuş sesi
    [SerializeField] [Range(0f, 1f)] private float correctKeyVol = 0.4f;
    [SerializeField] private AudioClip wrongKeySound;        // Yanlış tuş sesi
    [SerializeField] [Range(0f, 1f)] private float wrongKeyVol = 0.5f;

    // UI
    private GameObject gameCanvas;
    private GameObject panelObj;
    private GameObject headerObj;
    private TextMeshProUGUI headerText;
    private TextMeshProUGUI keyText;
    private TextMeshProUGUI progressText;
    private TextMeshProUGUI feedbackText;
    private TextMeshProUGUI reportText;
    private TextMeshProUGUI instructionText;   // Talimat/sonuç
    private Image timerBar;
    private Image scanlineOverlay;

    // State
    private enum GamePhase { Inactive, Instructions, Playing, Result }
    private GamePhase phase = GamePhase.Inactive;
    private KeyCode currentKey;
    private int keysPressed = 0;
    private int errors = 0;
    private float keyTimer = 0f;
    private bool isRebellionMode = false;
    private System.Action<bool> onCompleteCallback;

    private readonly KeyCode[] possibleKeys = {
        KeyCode.A, KeyCode.S, KeyCode.D, KeyCode.F,
        KeyCode.G, KeyCode.H, KeyCode.J, KeyCode.K,
        KeyCode.L, KeyCode.Q, KeyCode.W, KeyCode.E,
        KeyCode.R, KeyCode.T, KeyCode.Y, KeyCode.Z,
        KeyCode.X, KeyCode.C, KeyCode.V, KeyCode.B
    };

    private readonly string[] reportWords = {
        "Bugünkü", "satış", "rakamları", "incelendiğinde,",
        "geçen", "aya", "göre", "artış",
        "gözlemlenmektedir.", "Detaylı", "analiz", "ekte",
        "sunulmuştur.", "Saygılarımla.", "İyi", "çalışmalar."
    };

    private readonly string[] doodleWords = {
        "bıktım", "bu", "işten", "neden",
        "her gün", "aynı", "rapor?!", "YETER",
        "★", "♪", "☺", "...",
        "özgürlük", "nerede?", "kaçmalıyım", "!!!"
    };

    public bool IsActive => phase != GamePhase.Inactive;

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

    // ==================== UI ====================

    private void CreateUI()
    {
        gameCanvas = new GameObject("ReportMiniGameCanvas");
        gameCanvas.transform.SetParent(transform);
        Canvas canvas = gameCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 250;

        CanvasScaler scaler = gameCanvas.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        gameCanvas.AddComponent<GraphicRaycaster>();

        // ── Ana Panel (monitör çerçevesi) ──
        panelObj = new GameObject("MonitorPanel");
        panelObj.transform.SetParent(gameCanvas.transform, false);
        Image panelBg = panelObj.AddComponent<Image>();
        panelBg.color = bgColor;

        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.12f, 0.08f);
        panelRect.anchorMax = new Vector2(0.88f, 0.92f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        // ── Üst çubuk (terminal header) ──
        headerObj = new GameObject("Header");
        headerObj.transform.SetParent(panelObj.transform, false);
        Image headerBg = headerObj.AddComponent<Image>();
        headerBg.color = headerColor;

        RectTransform headerRect = headerObj.GetComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0f, 0.92f);
        headerRect.anchorMax = new Vector2(1f, 1f);
        headerRect.offsetMin = Vector2.zero;
        headerRect.offsetMax = Vector2.zero;

        // Header text
        GameObject htObj = new GameObject("HeaderText");
        htObj.transform.SetParent(headerObj.transform, false);
        headerText = htObj.AddComponent<TextMeshProUGUI>();
        headerText.fontSize = 18;
        headerText.color = terminalGreen;
        headerText.fontStyle = FontStyles.Bold;
        headerText.alignment = TextAlignmentOptions.MidlineLeft;
        headerText.text = "  ■ RAPOR SİSTEMİ v2.1";

        RectTransform htRect = htObj.GetComponent<RectTransform>();
        htRect.anchorMin = Vector2.zero;
        htRect.anchorMax = Vector2.one;
        htRect.offsetMin = new Vector2(10, 0);
        htRect.offsetMax = Vector2.zero;

        // ── Rapor metni (yazılan metin) ──
        GameObject reportObj = new GameObject("ReportText");
        reportObj.transform.SetParent(panelObj.transform, false);
        reportText = reportObj.AddComponent<TextMeshProUGUI>();
        reportText.fontSize = bodyFontSize;
        reportText.color = new Color(0.7f, 0.85f, 0.7f);
        reportText.alignment = TextAlignmentOptions.TopLeft;

        RectTransform reportRect = reportObj.GetComponent<RectTransform>();
        reportRect.anchorMin = new Vector2(0f, 0.35f);
        reportRect.anchorMax = new Vector2(1f, 0.9f);
        reportRect.offsetMin = new Vector2(30, 0);
        reportRect.offsetMax = new Vector2(-30, -10);

        // ── Basılacak tuş (ortada büyük) ──
        GameObject keyObj = new GameObject("KeyDisplay");
        keyObj.transform.SetParent(panelObj.transform, false);
        keyText = keyObj.AddComponent<TextMeshProUGUI>();
        keyText.fontSize = keyFontSize;
        keyText.color = terminalGreen;
        keyText.alignment = TextAlignmentOptions.Center;
        keyText.fontStyle = FontStyles.Bold;

        RectTransform keyRect = keyObj.GetComponent<RectTransform>();
        keyRect.anchorMin = new Vector2(0.25f, 0.12f);
        keyRect.anchorMax = new Vector2(0.75f, 0.38f);
        keyRect.offsetMin = Vector2.zero;
        keyRect.offsetMax = Vector2.zero;

        // ── Timer bar ──
        GameObject timerBgObj = new GameObject("TimerBg");
        timerBgObj.transform.SetParent(panelObj.transform, false);
        Image timerBgImg = timerBgObj.AddComponent<Image>();
        timerBgImg.color = new Color(0.15f, 0.15f, 0.15f);

        RectTransform timerBgRect = timerBgObj.GetComponent<RectTransform>();
        timerBgRect.anchorMin = new Vector2(0.05f, 0.07f);
        timerBgRect.anchorMax = new Vector2(0.95f, 0.09f);
        timerBgRect.offsetMin = Vector2.zero;
        timerBgRect.offsetMax = Vector2.zero;

        GameObject timerObj = new GameObject("TimerFill");
        timerObj.transform.SetParent(timerBgObj.transform, false);
        timerBar = timerObj.AddComponent<Image>();
        timerBar.color = terminalGreen;

        RectTransform timerRect = timerObj.GetComponent<RectTransform>();
        timerRect.anchorMin = Vector2.zero;
        timerRect.anchorMax = Vector2.one;
        timerRect.offsetMin = Vector2.zero;
        timerRect.offsetMax = Vector2.zero;

        // ── İlerleme + feedback (alt) ──
        GameObject progObj = new GameObject("ProgressText");
        progObj.transform.SetParent(panelObj.transform, false);
        progressText = progObj.AddComponent<TextMeshProUGUI>();
        progressText.fontSize = 16;
        progressText.color = new Color(0.5f, 0.6f, 0.5f);
        progressText.alignment = TextAlignmentOptions.MidlineLeft;

        RectTransform progRect = progObj.GetComponent<RectTransform>();
        progRect.anchorMin = new Vector2(0.05f, 0.01f);
        progRect.anchorMax = new Vector2(0.5f, 0.06f);
        progRect.offsetMin = Vector2.zero;
        progRect.offsetMax = Vector2.zero;

        GameObject fbObj = new GameObject("FeedbackText");
        fbObj.transform.SetParent(panelObj.transform, false);
        feedbackText = fbObj.AddComponent<TextMeshProUGUI>();
        feedbackText.fontSize = 18;
        feedbackText.color = correctColor;
        feedbackText.alignment = TextAlignmentOptions.MidlineRight;

        RectTransform fbRect = fbObj.GetComponent<RectTransform>();
        fbRect.anchorMin = new Vector2(0.5f, 0.01f);
        fbRect.anchorMax = new Vector2(0.95f, 0.06f);
        fbRect.offsetMin = Vector2.zero;
        fbRect.offsetMax = Vector2.zero;

        // ── Talimat/Sonuç ekranı (tam panel) ──
        GameObject instrObj = new GameObject("InstructionOverlay");
        instrObj.transform.SetParent(panelObj.transform, false);
        instructionText = instrObj.AddComponent<TextMeshProUGUI>();
        instructionText.fontSize = 22;
        instructionText.color = terminalGreen;
        instructionText.alignment = TextAlignmentOptions.Center;
        instructionText.richText = true;

        RectTransform instrRect = instrObj.GetComponent<RectTransform>();
        instrRect.anchorMin = new Vector2(0.05f, 0.1f);
        instrRect.anchorMax = new Vector2(0.95f, 0.9f);
        instrRect.offsetMin = Vector2.zero;
        instrRect.offsetMax = Vector2.zero;

        // ── Scanline efekti (yarı saydam çizgili overlay) ──
        GameObject scanObj = new GameObject("Scanlines");
        scanObj.transform.SetParent(panelObj.transform, false);
        scanlineOverlay = scanObj.AddComponent<Image>();
        scanlineOverlay.color = new Color(0f, 0f, 0f, 0.06f);
        scanlineOverlay.raycastTarget = false;

        RectTransform scanRect = scanObj.GetComponent<RectTransform>();
        scanRect.anchorMin = Vector2.zero;
        scanRect.anchorMax = Vector2.one;
        scanRect.offsetMin = Vector2.zero;
        scanRect.offsetMax = Vector2.zero;

        panelObj.SetActive(false);
    }

    // ==================== Başlatma ====================

    public void StartGame(bool isRebellion, System.Action<bool> onComplete)
    {
        if (phase != GamePhase.Inactive) return;

        isRebellionMode = isRebellion;
        onCompleteCallback = onComplete;
        keysPressed = 0;
        errors = 0;

        // Oyuncuyu durdur
        Player player = FindFirstObjectByType<Player>();
        if (player != null) player.SetCanMove(false);

        // Talimat ekranı göster
        ShowInstructions();
    }

    private void ShowInstructions()
    {
        phase = GamePhase.Instructions;
        panelObj.SetActive(true);

        // Oyun elemanlarını gizle
        keyText.gameObject.SetActive(false);
        reportText.gameObject.SetActive(false);
        progressText.gameObject.SetActive(false);
        feedbackText.gameObject.SetActive(false);
        timerBar.transform.parent.gameObject.SetActive(false);

        // Talimat göster
        headerText.text = "  ■ RAPOR SİSTEMİ v2.1 — TALİMAT";

        if (isRebellionMode)
        {
            instructionText.text =
                "<size=28><b>★ İSYAN MODU ★</b></size>\n\n" +
                "Rapor yerine kafandakileri yaz.\n" +
                "Ekranda beliren <b>harflere</b> bas.\n\n" +
                $"<color=#FFD700>Toplam {totalKeysToPress} tuş</color>  •  " +
                $"<color=#FF6666>Max {maxErrors} hata</color>\n\n" +
                "Her tuş için süren var.\n" +
                "Süre biterse → hata sayılır.\n\n\n" +
                "<size=18><color=#AAFFAA>[ E ] ile başla</color></size>";
        }
        else
        {
            instructionText.text =
                "<size=28><b>GÜNLÜK RAPOR</b></size>\n\n" +
                "Ekranda beliren harflere basarak\n" +
                "raporu tamamla.\n\n" +
                $"<color=#FFD700>Toplam {totalKeysToPress} tuş</color>  •  " +
                $"<color=#FF6666>Max {maxErrors} hata hakkı</color>\n\n" +
                "Her tuş için süren sınırlı.\n" +
                "Süre biterse → hata sayılır.\n\n\n" +
                "<size=18><color=#AAFFAA>[ E ] ile başla</color></size>";
        }

        instructionText.gameObject.SetActive(true);
    }

    private void StartPlaying()
    {
        phase = GamePhase.Playing;

        // Talimatı gizle, oyun elemanlarını göster
        instructionText.gameObject.SetActive(false);
        keyText.gameObject.SetActive(true);
        reportText.gameObject.SetActive(true);
        progressText.gameObject.SetActive(true);
        feedbackText.gameObject.SetActive(true);
        timerBar.transform.parent.gameObject.SetActive(true);

        headerText.text = isRebellionMode
            ? "  ■ İSYAN MODU — Kafanı boşalt"
            : "  ■ RAPOR SİSTEMİ — Yazıyor...";

        reportText.text = isRebellionMode
            ? "<i>// Kişisel notlar:</i>\n"
            : "<i>// Günlük Rapor:</i>\n";

        feedbackText.text = "";
        ShowNextKey();
    }

    // ==================== Oyun Döngüsü ====================

    private void ShowNextKey()
    {
        if (keysPressed >= totalKeysToPress)
        {
            ShowResult(true);
            return;
        }

        currentKey = possibleKeys[Random.Range(0, possibleKeys.Length)];
        keyText.text = $"[ {currentKey} ]";
        keyText.color = terminalGreen;
        keyTimer = timePerKey;
        UpdateProgress();
    }

    void Update()
    {
        switch (phase)
        {
            case GamePhase.Instructions:
                // E ile oyunu başlat
                if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space))
                {
                    StartPlaying();
                }
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    EndGame(false);
                }
                break;

            case GamePhase.Playing:
                UpdatePlaying();
                break;

            case GamePhase.Result:
                // E ile sonuç ekranını kapat
                if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space))
                {
                    EndGame(lastResult);
                }
                break;
        }

        // Scanline animasyonu
        if (phase != GamePhase.Inactive && scanlineOverlay != null)
        {
            float alpha = 0.03f + Mathf.Sin(Time.time * 2f) * 0.02f;
            scanlineOverlay.color = new Color(0f, 0f, 0f, alpha);
        }
    }

    private bool lastResult;

    private void UpdatePlaying()
    {
        // Süre sayacı
        keyTimer -= Time.deltaTime;

        // Timer bar güncelle
        float timePercent = Mathf.Clamp01(keyTimer / timePerKey);
        RectTransform timerRect = timerBar.GetComponent<RectTransform>();
        timerRect.anchorMax = new Vector2(timePercent, 1f);

        // Timer rengi: yeşil → kırmızı
        timerBar.color = Color.Lerp(wrongColor, terminalGreen, timePercent);

        if (keyTimer <= 0f)
        {
            OnWrongKey();
            return;
        }

        // Tuş kontrolü
        if (Input.anyKeyDown)
        {
            if (Input.GetKeyDown(currentKey))
            {
                OnCorrectKey();
            }
            else if (!Input.GetKeyDown(KeyCode.Escape))
            {
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

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            EndGame(false);
        }
    }

    private void OnCorrectKey()
    {
        keysPressed++;

        // Ses
        SFXManager.Play2D(correctKeySound, correctKeyVol);

        string[] words = isRebellionMode ? doodleWords : reportWords;
        int wordIndex = (keysPressed - 1) % words.Length;
        reportText.text += words[wordIndex] + " ";
        if (keysPressed % 4 == 0) reportText.text += "\n";

        feedbackText.text = "✓ Doğru!";
        feedbackText.color = correctColor;

        ShowNextKey();
    }

    private void OnWrongKey()
    {
        errors++;

        // Ses
        SFXManager.Play2D(wrongKeySound, wrongKeyVol);

        feedbackText.text = $"✗ Yanlış! ({errors}/{maxErrors})";
        feedbackText.color = wrongColor;

        // Ekran titremeefekti
        if (panelObj != null)
        {
            StartCoroutine(ShakePanel());
        }

        if (errors >= maxErrors)
        {
            ShowResult(false);
            return;
        }

        ShowNextKey();
    }

    private System.Collections.IEnumerator ShakePanel()
    {
        RectTransform rect = panelObj.GetComponent<RectTransform>();
        Vector2 origMin = rect.anchorMin;
        Vector2 origMax = rect.anchorMax;

        for (int i = 0; i < 4; i++)
        {
            float offset = Random.Range(-0.005f, 0.005f);
            rect.anchorMin = origMin + new Vector2(offset, offset);
            rect.anchorMax = origMax + new Vector2(offset, offset);
            yield return new WaitForSeconds(0.03f);
        }

        rect.anchorMin = origMin;
        rect.anchorMax = origMax;
    }

    private void UpdateProgress()
    {
        progressText.text = $"İlerleme: {keysPressed}/{totalKeysToPress}  |  Hata: {errors}/{maxErrors}";
    }

    // ==================== Sonuç Ekranı ====================

    private void ShowResult(bool success)
    {
        phase = GamePhase.Result;
        lastResult = success;

        // Oyun elemanlarını gizle
        keyText.gameObject.SetActive(false);
        timerBar.transform.parent.gameObject.SetActive(false);

        // Rapor metni görünsün (oyuncu okuyabilsin)
        reportText.gameObject.SetActive(true);

        if (success)
        {
            headerText.text = isRebellionMode
                ? "  ■ ★ İSYAN TAMAMLANDI ★"
                : "  ■ RAPOR TAMAMLANDI ✓";

            feedbackText.text = "<color=#AAFFAA>[ E ] ile kapat</color>";
            feedbackText.color = correctColor;
            progressText.text = isRebellionMode
                ? "İçindeki baskı biraz azaldı..."
                : "Rapor başarıyla gönderildi.";
        }
        else
        {
            headerText.text = "  ■ RAPOR BAŞARISIZ ✗";
            feedbackText.text = "<color=#AAFFAA>[ E ] ile kapat</color>";
            feedbackText.color = wrongColor;
            progressText.text = "Çok fazla hata yaptın...";
        }
    }

    // ==================== Bitiş ====================

    private void EndGame(bool success)
    {
        phase = GamePhase.Inactive;
        panelObj.SetActive(false);

        Player player = FindFirstObjectByType<Player>();
        if (player != null) player.SetCanMove(true);

        onCompleteCallback?.Invoke(success);
        onCompleteCallback = null;
    }
}
