using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Kağıt Buruşturma Mini-Game'i.
/// Fare sallayarak kağıdı buruştur, çöpe at.
/// 
/// 3 sprite: Düz → Hafif buruşuk → Tam buruşmuş
/// Fare hızı "buruşma sayacı"nı doldurur.
/// 
/// Gün bazlı davranış:
///   Gün 1: Normal buruşturma (rutin öğretme)
///   Gün 2: Kağıt direnir, garip sesler çıkar
///   Gün 3: Kağıt kendini geri açar, "DUR" yazar
/// 
/// Kullanım: Sahnede boş obje → bu script.
/// TrashCan veya başka yerden CrumpleMiniGame.Instance.StartGame() ile çağır.
/// </summary>
public class CrumpleMiniGame : MonoBehaviour
{
    public static CrumpleMiniGame Instance { get; private set; }

    [Header("Paper Sprites (Inspector'dan ata)")]
    [SerializeField] private Sprite paperFlat;            // Düz kağıt
    [SerializeField] private Sprite paperHalfCrumpled;    // Hafif buruşuk
    [SerializeField] private Sprite paperFullCrumpled;    // Tam buruşmuş

    [Header("Settings")]
    [SerializeField] private float crumpleSpeedMultiplier = 1f;    // Buruşma hızı çarpanı
    [SerializeField] private float day2ResistMultiplier = 0.6f;    // Gün 2'de direnç (yavaşlatır)
    [SerializeField] private float day3ResistMultiplier = 0.4f;    // Gün 3'te daha fazla direnç
    [SerializeField] private float uncrumpleSpeed = 15f;           // Gün 3: kağıt kendini açma hızı

    [Header("Sound")]
    [SerializeField] private AudioClip crumpleSound1;     // %33 hışırtı
    [SerializeField] [Range(0f, 1f)] private float crumpleSound1Vol = 0.6f;
    [SerializeField] private AudioClip crumpleSound2;     // %66 hışırtı
    [SerializeField] [Range(0f, 1f)] private float crumpleSound2Vol = 0.7f;
    [SerializeField] private AudioClip crumpleSound3;     // %100 final buruşma
    [SerializeField] [Range(0f, 1f)] private float crumpleSound3Vol = 0.8f;
    [SerializeField] private AudioClip anomalySound;      // Gün 2+: kemik kırılma / garip ses
    [SerializeField] [Range(0f, 1f)] private float anomalySoundVol = 0.5f;
    [SerializeField] private AudioClip uncrumpleSound;    // Gün 3: kağıt kendini açma sesi
    [SerializeField] [Range(0f, 1f)] private float uncrumpleSoundVol = 0.5f;

    // UI (runtime oluşturulur)
    private GameObject miniGameCanvas;
    private GameObject panelObj;
    private Image paperImage;
    private Image progressBar;
    private Image progressBarBg;
    private TextMeshProUGUI instructionText;
    private TextMeshProUGUI paperText;        // Kağıt üstündeki yazı (anomali)

    // State
    private bool isActive = false;
    private float crumpleProgress = 0f;       // 0-100
    private int currentStage = 0;             // 0=düz, 1=hafif, 2=tam
    private bool isHolding = false;
    private float lastMouseX;
    private System.Action<bool> onCompleteCallback;

    // Gün 3 anomali
    private bool isUncrumpling = false;
    private float uncrumpleTimer = 0f;
    private int anomalyTriggerCount = 0;

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

    // ==================== UI Oluşturma ====================

    private void CreateUI()
    {
        miniGameCanvas = new GameObject("CrumpleCanvas");
        miniGameCanvas.transform.SetParent(transform);
        Canvas canvas = miniGameCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 250;

        CanvasScaler scaler = miniGameCanvas.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        miniGameCanvas.AddComponent<GraphicRaycaster>();

        // Karanlık arka plan
        panelObj = new GameObject("BgPanel");
        panelObj.transform.SetParent(miniGameCanvas.transform, false);
        Image bgImg = panelObj.AddComponent<Image>();
        bgImg.color = new Color(0f, 0f, 0f, 0.7f);
        RectTransform bgRect = panelObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        // Kağıt resmi (ekranın ortası)
        GameObject paperObj = new GameObject("PaperImage");
        paperObj.transform.SetParent(panelObj.transform, false);
        paperImage = paperObj.AddComponent<Image>();
        paperImage.preserveAspect = true;

        RectTransform paperRect = paperObj.GetComponent<RectTransform>();
        paperRect.anchorMin = new Vector2(0.3f, 0.2f);
        paperRect.anchorMax = new Vector2(0.7f, 0.8f);
        paperRect.offsetMin = Vector2.zero;
        paperRect.offsetMax = Vector2.zero;

        // Kağıt üstü yazı (anomali için)
        GameObject textOnPaper = new GameObject("PaperText");
        textOnPaper.transform.SetParent(paperObj.transform, false);
        paperText = textOnPaper.AddComponent<TextMeshProUGUI>();
        paperText.fontSize = 28;
        paperText.color = new Color(0.2f, 0.2f, 0.2f);
        paperText.alignment = TextAlignmentOptions.Center;
        paperText.text = "";
        paperText.raycastTarget = false;

        RectTransform ptRect = textOnPaper.GetComponent<RectTransform>();
        ptRect.anchorMin = new Vector2(0.1f, 0.1f);
        ptRect.anchorMax = new Vector2(0.9f, 0.9f);
        ptRect.offsetMin = Vector2.zero;
        ptRect.offsetMax = Vector2.zero;

        // Progress bar arka planı
        GameObject barBgObj = new GameObject("ProgressBg");
        barBgObj.transform.SetParent(panelObj.transform, false);
        progressBarBg = barBgObj.AddComponent<Image>();
        progressBarBg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

        RectTransform barBgRect = barBgObj.GetComponent<RectTransform>();
        barBgRect.anchorMin = new Vector2(0.3f, 0.12f);
        barBgRect.anchorMax = new Vector2(0.7f, 0.15f);
        barBgRect.offsetMin = Vector2.zero;
        barBgRect.offsetMax = Vector2.zero;

        // Progress bar dolgu
        GameObject barObj = new GameObject("ProgressFill");
        barObj.transform.SetParent(barBgObj.transform, false);
        progressBar = barObj.AddComponent<Image>();
        progressBar.color = new Color(0.9f, 0.6f, 0.1f);

        RectTransform barRect = barObj.GetComponent<RectTransform>();
        barRect.anchorMin = Vector2.zero;
        barRect.anchorMax = new Vector2(0f, 1f);  // Genişlik 0'dan başlar
        barRect.offsetMin = Vector2.zero;
        barRect.offsetMax = Vector2.zero;

        // Talimat yazısı
        GameObject instrObj = new GameObject("Instructions");
        instrObj.transform.SetParent(panelObj.transform, false);
        instructionText = instrObj.AddComponent<TextMeshProUGUI>();
        instructionText.fontSize = 22;
        instructionText.color = Color.white;
        instructionText.alignment = TextAlignmentOptions.Center;
        instructionText.text = "";

        RectTransform instrRect = instrObj.GetComponent<RectTransform>();
        instrRect.anchorMin = new Vector2(0.2f, 0.02f);
        instrRect.anchorMax = new Vector2(0.8f, 0.1f);
        instrRect.offsetMin = Vector2.zero;
        instrRect.offsetMax = Vector2.zero;

        panelObj.SetActive(false);
    }

    // ==================== Mini-Game Başlat ====================

    /// <summary>
    /// Mini-game'i başlatır.
    /// onComplete: true = başarıyla buruşturuldu, false = iptal/başarısız
    /// </summary>
    public void StartGame(System.Action<bool> onComplete = null)
    {
        if (isActive) return;

        onCompleteCallback = onComplete;
        isActive = true;
        crumpleProgress = 0f;
        currentStage = 0;
        isUncrumpling = false;
        anomalyTriggerCount = 0;
        uncrumpleTimer = 0f;

        // Kağıdı düz sprite'a ayarla
        if (paperFlat != null) paperImage.sprite = paperFlat;
        paperImage.color = Color.white;
        paperText.text = "";

        // Talimat
        int day = GameManager.Instance != null ? GameManager.Instance.CurrentDay : 1;
        if (day <= 1)
        {
            instructionText.text = "Sol tık basılı tut + Fareyi sağa sola salla!";
        }
        else
        {
            instructionText.text = "Kağıdı buruştur... eğer yapabilirsen.";
        }

        // Progress bar sıfırla
        UpdateProgressBar(0f);

        // Oyuncuyu durdur
        Player player = FindFirstObjectByType<Player>();
        if (player != null) player.SetCanMove(false);

        panelObj.SetActive(true);
    }

    // ==================== Update ====================

    void Update()
    {
        if (!isActive) return;

        int day = GameManager.Instance != null ? GameManager.Instance.CurrentDay : 1;

        // Sol tık basılı mı?
        if (Input.GetMouseButtonDown(0))
        {
            isHolding = true;
            lastMouseX = Input.mousePosition.x;
        }
        if (Input.GetMouseButtonUp(0))
        {
            isHolding = false;
        }

        // ESC ile iptal
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            EndGame(false);
            return;
        }

        if (isHolding)
        {
            // Fare hızını hesapla
            float currentMouseX = Input.mousePosition.x;
            float mouseSpeed = Mathf.Abs(currentMouseX - lastMouseX);
            lastMouseX = currentMouseX;

            // Direnç çarpanı (gün bazlı)
            float resist = 1f;
            if (day == 2) resist = day2ResistMultiplier;
            else if (day >= 3) resist = day3ResistMultiplier;

            // Buruşma ilerlemesi
            float addAmount = mouseSpeed * crumpleSpeedMultiplier * resist * Time.deltaTime;
            crumpleProgress = Mathf.Clamp(crumpleProgress + addAmount, 0f, 100f);

            // Kağıt titremesi (fare sallanırken)
            if (mouseSpeed > 5f)
            {
                float shake = Mathf.Sin(Time.time * 30f) * (mouseSpeed * 0.01f);
                paperImage.transform.localRotation = Quaternion.Euler(0f, 0f, shake);
            }
        }
        else
        {
            // Basılı değilken kağıt rotasyonu sıfırla
            paperImage.transform.localRotation = Quaternion.identity;
        }

        // === Gün 3 Anomali: Kağıt kendini açar ===
        if (day >= 3 && crumpleProgress > 10f && !isHolding)
        {
            uncrumpleTimer += Time.deltaTime;
            if (uncrumpleTimer > 1.5f)
            {
                isUncrumpling = true;
                crumpleProgress -= uncrumpleSpeed * Time.deltaTime;
                crumpleProgress = Mathf.Max(crumpleProgress, 0f);

                // Kağıt üstü garip yazılar
                if (anomalyTriggerCount == 0)
                {
                    anomalyTriggerCount++;
                    paperText.text = "...dur.";
                    SFXManager.Play2D(uncrumpleSound, uncrumpleSoundVol);
                }
            }
        }
        else
        {
            uncrumpleTimer = 0f;
            isUncrumpling = false;
        }

        // Stage güncellemesi
        UpdateStage(day);

        // Progress bar
        UpdateProgressBar(crumpleProgress / 100f);

        // Tamamlandı mı?
        if (crumpleProgress >= 100f)
        {
            EndGame(true);
        }
    }

    // ==================== Stage / Sprite Değişimi ====================

    private void UpdateStage(int day)
    {
        int newStage = 0;
        if (crumpleProgress >= 66f) newStage = 2;
        else if (crumpleProgress >= 33f) newStage = 1;

        if (newStage != currentStage)
        {
            int oldStage = currentStage;
            currentStage = newStage;

            // Geri açılıyorsa sprite düşür ama ses çalma
            if (newStage < oldStage)
            {
                UpdatePaperSprite();
                return;
            }

            // İleri gidiyorsa
            UpdatePaperSprite();

            // Ses efekti
            switch (currentStage)
            {
                case 1:
                    SFXManager.Play2D(crumpleSound1, crumpleSound1Vol);
                    // Gün 2+: garip ses
                    if (day >= 2)
                    {
                        SFXManager.Play2D(anomalySound, anomalySoundVol * 0.8f);
                        instructionText.text = "...bir şey yanlış.";
                    }
                    break;
                case 2:
                    SFXManager.Play2D(crumpleSound2, crumpleSound2Vol);
                    if (day >= 2)
                    {
                        SFXManager.Play2D(anomalySound, anomalySoundVol);
                        instructionText.text = "...kağıt direniyor.";
                        // Kağıt üstü yazı
                        if (day >= 3)
                        {
                            paperText.text = "<color=#CC0000>D U R</color>";
                            paperText.fontSize = 40;
                        }
                    }
                    break;
            }
        }
    }

    private void UpdatePaperSprite()
    {
        switch (currentStage)
        {
            case 0:
                if (paperFlat != null) paperImage.sprite = paperFlat;
                break;
            case 1:
                if (paperHalfCrumpled != null) paperImage.sprite = paperHalfCrumpled;
                break;
            case 2:
                if (paperFullCrumpled != null) paperImage.sprite = paperFullCrumpled;
                break;
        }
    }

    // ==================== Progress Bar ====================

    private void UpdateProgressBar(float fillAmount)
    {
        if (progressBar == null) return;

        RectTransform barRect = progressBar.GetComponent<RectTransform>();
        barRect.anchorMax = new Vector2(fillAmount, 1f);

        // Renk: sarı → kırmızı
        progressBar.color = Color.Lerp(
            new Color(0.9f, 0.6f, 0.1f),   // Sarı
            new Color(0.9f, 0.2f, 0.1f),   // Kırmızı
            fillAmount
        );
    }

    // ==================== Bitiş ====================

    private void EndGame(bool success)
    {
        isActive = false;
        panelObj.SetActive(false);

        // Final sesi
        if (success)
        {
            SFXManager.Play2D(crumpleSound3, crumpleSound3Vol);
        }

        // Oyuncuyu serbest bırak
        Player player = FindFirstObjectByType<Player>();
        if (player != null) player.SetCanMove(true);

        onCompleteCallback?.Invoke(success);
        onCompleteCallback = null;
    }
}
