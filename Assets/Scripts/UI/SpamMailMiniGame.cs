using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Spam Mail Mini-Game: Pop-up pencereleri kapat!
/// Gün 1: Normal, kolay
/// Gün 2: X butonları kaçar, silince 2 tane çıkar
/// Gün 3: Durdurulamaz, sahte mavi ekranla biter
/// </summary>
public class SpamMailMiniGame : MonoBehaviour
{
    public static SpamMailMiniGame Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private float gameDuration = 3f;
    [SerializeField] private float spawnInterval = 1.5f;
    [SerializeField] private int maxPopups = 12;

    [Header("Sound")]
    [SerializeField] private AudioClip popupOpenSound;
    [SerializeField] [Range(0f, 1f)] private float popupOpenVol = 0.3f;
    [SerializeField] private AudioClip popupCloseSound;
    [SerializeField] [Range(0f, 1f)] private float popupCloseVol = 0.4f;
    [SerializeField] private AudioClip errorSound;
    [SerializeField] [Range(0f, 1f)] private float errorSoundVol = 0.6f;

    [Header("Style")]
    [SerializeField] private Color popupBg = new Color(0.95f, 0.95f, 0.95f);
    [SerializeField] private Color headerBg = new Color(0.2f, 0.4f, 0.8f);
    [SerializeField] private Color anomalyHeaderBg = new Color(0.6f, 0.1f, 0.1f);

    private GameObject gameCanvas;
    private GameObject panelObj;
    private Image inboxBar;
    private TextMeshProUGUI statusText;
    private TextMeshProUGUI timerText;
    private List<GameObject> activePopups = new List<GameObject>();

    private bool isActive = false;
    private float gameTimer;
    private float spawnTimer;
    private float inboxFill = 0f;
    private int closedCount = 0;
    private System.Action<bool> onCompleteCallback;

    // Gün bazlı mail içerikleri
    private readonly string[] day1Subjects = {
        "Toplantı Notları", "Aylık Rapor", "%50 İndirim!",
        "Merhaba!", "Fatura", "Doğum Günü Kutlaması",
        "Haftalık Özet", "Yeni Politika", "Tatil İzni"
    };
    private readonly string[] day2Subjects = {
        "BİZİ GÖR", "NİYE BURADASIN?", "KAPI AÇIK",
        "arkana bak", "...yardım...", "SİSTEM HATASI",
        "Toplantı N̷o̵t̶l̷a̸r̵ı̶", "ÇIKIŞ YOK", "biliyor musun?"
    };
    private readonly string[] day3Subjects = {
        "■■■■■■", "KAÇAMAZSIN", "HEP BURADAYDIN",
        "S̷İ̸S̴T̵E̶M̷", "0x00FF FATAL", "NULL",
        "UYAN", "son şans", "............"
    };

    public bool IsActive => isActive;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        CreateUI();
    }

    private void CreateUI()
    {
        gameCanvas = new GameObject("SpamMailCanvas");
        gameCanvas.transform.SetParent(transform);
        Canvas c = gameCanvas.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 260;
        CanvasScaler s = gameCanvas.AddComponent<CanvasScaler>();
        s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        s.referenceResolution = new Vector2(1920, 1080);
        gameCanvas.AddComponent<GraphicRaycaster>();

        // Ana panel
        panelObj = new GameObject("BgPanel");
        panelObj.transform.SetParent(gameCanvas.transform, false);
        Image bg = panelObj.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.08f, 0.15f, 0.85f);
        RectTransform bgR = panelObj.GetComponent<RectTransform>();
        bgR.anchorMin = Vector2.zero; bgR.anchorMax = Vector2.one;
        bgR.offsetMin = Vector2.zero; bgR.offsetMax = Vector2.zero;

        // Inbox bar bg
        GameObject barBg = CreateRect(panelObj, "InboxBarBg", new Vector2(0.1f, 0.92f), new Vector2(0.9f, 0.96f));
        barBg.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f);

        GameObject barFill = CreateRect(barBg, "InboxFill", Vector2.zero, Vector2.one);
        inboxBar = barFill.AddComponent<Image>();
        inboxBar.color = new Color(0.3f, 0.7f, 0.3f);

        // Status text
        GameObject stObj = CreateRect(panelObj, "Status", new Vector2(0.1f, 0.87f), new Vector2(0.5f, 0.92f));
        statusText = stObj.AddComponent<TextMeshProUGUI>();
        statusText.fontSize = 18; statusText.color = Color.white;
        statusText.alignment = TextAlignmentOptions.MidlineLeft;

        // Timer
        GameObject tmObj = CreateRect(panelObj, "Timer", new Vector2(0.5f, 0.87f), new Vector2(0.9f, 0.92f));
        timerText = tmObj.AddComponent<TextMeshProUGUI>();
        timerText.fontSize = 18; timerText.color = Color.white;
        timerText.alignment = TextAlignmentOptions.MidlineRight;

        panelObj.SetActive(false);
    }

    private GameObject CreateRect(GameObject parent, string name, Vector2 ancMin, Vector2 ancMax)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent.transform, false);
        RectTransform r = obj.AddComponent<RectTransform>();
        r.anchorMin = ancMin; r.anchorMax = ancMax;
        r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;
        return obj;
    }

    // ==================== Başlat ====================

    public void StartGame(System.Action<bool> onComplete = null)
    {
        if (isActive) return;
        onCompleteCallback = onComplete;
        isActive = true;
        gameTimer = gameDuration;
        spawnTimer = 0f;
        inboxFill = 0f;
        closedCount = 0;

        ClearPopups();
        panelObj.SetActive(true);

        Player p = FindFirstObjectByType<Player>();
        if (p != null) p.SetCanMove(false);

        int day = GameManager.Instance != null ? GameManager.Instance.CurrentDay : 1;
        statusText.text = day >= 3 ? "SİSTEM KRİTİK HATA" : "Gelen kutusu dolmadan temizle!";
    }

    // ==================== Update ====================

    void Update()
    {
        if (!isActive) return;

        int day = GameManager.Instance != null ? GameManager.Instance.CurrentDay : 1;

        gameTimer -= Time.deltaTime;
        timerText.text = $"Süre: {Mathf.CeilToInt(gameTimer)}s  |  Kapatılan: {closedCount}";

        // Spawn timer
        float interval = spawnInterval;
        if (day == 2) interval *= 0.7f;
        if (day >= 3) interval *= 0.4f;

        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0f && activePopups.Count < maxPopups)
        {
            SpawnPopup(day);
            spawnTimer = interval;
        }

        // Inbox doluluk
        inboxFill = (float)activePopups.Count / maxPopups;
        UpdateInboxBar();

        // Gün 3: süre bitince mavi ekran
        if (day >= 3 && gameTimer <= 0f)
        {
            StartCoroutine(BlueScreen());
            return;
        }

        // Normal: süre bitince başarı
        if (gameTimer <= 0f)
        {
            EndGame(true);
            return;
        }

        // Inbox %100 → başarısız
        if (activePopups.Count >= maxPopups)
        {
            SFXManager.Play2D(errorSound, errorSoundVol);
            EndGame(false);
        }
    }

    private void UpdateInboxBar()
    {
        RectTransform r = inboxBar.GetComponent<RectTransform>();
        r.anchorMax = new Vector2(inboxFill, 1f);
        inboxBar.color = Color.Lerp(new Color(0.3f, 0.7f, 0.3f), new Color(0.9f, 0.2f, 0.1f), inboxFill);
    }

    // ==================== Pop-up Oluştur ====================

    private void SpawnPopup(int day)
    {
        SFXManager.Play2D(popupOpenSound, popupOpenVol);

        GameObject popup = new GameObject("Popup");
        popup.transform.SetParent(panelObj.transform, false);

        // Rastgele pozisyon
        float x = Random.Range(0.1f, 0.7f);
        float y = Random.Range(0.1f, 0.75f);
        float w = Random.Range(0.15f, 0.25f);
        float h = Random.Range(0.12f, 0.2f);

        RectTransform pRect = popup.AddComponent<RectTransform>();
        pRect.anchorMin = new Vector2(x, y);
        pRect.anchorMax = new Vector2(x + w, y + h);
        pRect.offsetMin = Vector2.zero; pRect.offsetMax = Vector2.zero;

        // Popup arka plan
        Image popBg = popup.AddComponent<Image>();
        popBg.color = day >= 3 ? new Color(0.15f, 0.02f, 0.02f) : popupBg;

        // Header bar
        GameObject header = CreateRect(popup, "Header", new Vector2(0f, 0.7f), Vector2.one);
        Image hdrImg = header.AddComponent<Image>();
        hdrImg.color = day >= 2 ? anomalyHeaderBg : headerBg;

        // Başlık
        string[] subjects = day >= 3 ? day3Subjects : (day >= 2 ? day2Subjects : day1Subjects);
        GameObject titleObj = CreateRect(header, "Title", new Vector2(0.05f, 0f), new Vector2(0.8f, 1f));
        TextMeshProUGUI title = titleObj.AddComponent<TextMeshProUGUI>();
        title.text = subjects[Random.Range(0, subjects.Length)];
        title.fontSize = 14; title.color = Color.white;
        title.alignment = TextAlignmentOptions.MidlineLeft;

        // X butonu
        GameObject xBtn = CreateRect(header, "CloseBtn", new Vector2(0.82f, 0.1f), new Vector2(0.98f, 0.9f));
        Image xBg = xBtn.AddComponent<Image>();
        xBg.color = new Color(0.8f, 0.2f, 0.2f);
        Button btn = xBtn.AddComponent<Button>();

        TextMeshProUGUI xText = new GameObject("X").AddComponent<TextMeshProUGUI>();
        xText.transform.SetParent(xBtn.transform, false);
        xText.text = "✕"; xText.fontSize = 16; xText.color = Color.white;
        xText.alignment = TextAlignmentOptions.Center;
        RectTransform xTR = xText.GetComponent<RectTransform>();
        xTR.anchorMin = Vector2.zero; xTR.anchorMax = Vector2.one;
        xTR.offsetMin = Vector2.zero; xTR.offsetMax = Vector2.zero;

        // Click event
        btn.onClick.AddListener(() => OnPopupClosed(popup, day));

        // Gün 2: X butonu fareyi kaçırır
        if (day >= 2)
        {
            DodgeButton dodge = xBtn.AddComponent<DodgeButton>();
            dodge.dodgeChance = day >= 3 ? 0.7f : 0.4f;
        }

        activePopups.Add(popup);
    }

    private void OnPopupClosed(GameObject popup, int day)
    {
        if (popup == null) return;

        SFXManager.Play2D(popupCloseSound, popupCloseVol);
        closedCount++;
        activePopups.Remove(popup);
        Destroy(popup);

        // Gün 2+: bir tane silince 2 tane çıkar (%40 şans)
        if (day >= 2 && Random.value < 0.4f && activePopups.Count < maxPopups - 1)
        {
            SpawnPopup(day);
            SpawnPopup(day);
        }
    }

    // ==================== Gün 3: Mavi Ekran ====================

    private IEnumerator BlueScreen()
    {
        isActive = false;
        ClearPopups();

        SFXManager.Play2D(errorSound, errorSoundVol);

        // Mavi ekran
        Image bg = panelObj.GetComponent<Image>();
        bg.color = new Color(0f, 0.2f, 0.8f);

        statusText.text = "";
        timerText.text = "";

        // BSOD text
        GameObject bsod = CreateRect(panelObj, "BSOD", new Vector2(0.1f, 0.2f), new Vector2(0.9f, 0.8f));
        TextMeshProUGUI bsodText = bsod.AddComponent<TextMeshProUGUI>();
        bsodText.fontSize = 20;
        bsodText.color = Color.white;
        bsodText.alignment = TextAlignmentOptions.TopLeft;
        bsodText.text = "";

        string[] lines = {
            "OFFICE_SYSTEM v2.1 — KRİTİK HATA",
            "",
            "Bir sorun oluştu ve sisteminiz kapatılması gerekiyor.",
            "",
            "Hata kodu: 0x0000DEAD",
            "ROUTINE_BREAK_EXCEPTION",
            "",
            "Teknik bilgi:",
            "*** STOP: 0x00000050 (0xFD3094C2, 0x00000001)",
            "*** fracture.sys - Adres 0xFACE0FF at base 0x00000000",
            "",
            "Sistem belleği toplanıyor........",
            "",
            "Fiziksel bellek dökümü başlatılıyor...",
            "Fiziksel bellek dökümü tamamlandı.",
            "",
            "Bilgi toplandı. Serbest bırakılıyor..."
        };

        foreach (string line in lines)
        {
            bsodText.text += line + "\n";
            yield return new WaitForSeconds(0.15f);
        }

        yield return new WaitForSeconds(2f);

        Destroy(bsod);
        bg.color = new Color(0.05f, 0.08f, 0.15f, 0.85f);

        // Kırılma artır
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddFracture(20f);
        }

        EndGame(false);
    }

    // ==================== Bitiş ====================

    private void ClearPopups()
    {
        foreach (var p in activePopups) { if (p != null) Destroy(p); }
        activePopups.Clear();
    }

    private void EndGame(bool success)
    {
        isActive = false;
        ClearPopups();
        panelObj.SetActive(false);

        Player p = FindFirstObjectByType<Player>();
        if (p != null) p.SetCanMove(true);

        onCompleteCallback?.Invoke(success);
        onCompleteCallback = null;
    }
}
