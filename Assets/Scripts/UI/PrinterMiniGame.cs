using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Yazıcı Mini-Game: Golf barı tarzı zamanlama oyunu.
/// İbre sağa sola gider, yeşil alanda SPACE'e bas → kağıt alındı!
/// Kaçırırsan → "Kağıt Sıkıştı!" ve baştan dene.
///
/// Gün 2-3'te: yeşil alan küçülür, ibre hızlanır, garip yazılar çıkar.
///
/// Sahneye: Create Empty → "PrinterMiniGame" → Add Component: PrinterMiniGame
/// </summary>
public class PrinterMiniGame : MonoBehaviour
{
    public static PrinterMiniGame Instance { get; private set; }

    [Header("Gameplay")]
    [SerializeField] private float baseSpeed = 2f;           // İbre hızı (Gün 1)
    [SerializeField] private float day2SpeedMultiplier = 1.6f;
    [SerializeField] private float day3SpeedMultiplier = 2.5f;
    [SerializeField] private float baseZoneSize = 0.2f;      // Yeşil alan boyutu (Gün 1)
    [SerializeField] private float day2ZoneSize = 0.12f;
    [SerializeField] private float day3ZoneSize = 0.06f;
    [SerializeField] private int maxAttempts = 5;            // Max deneme (sonra otomatik başarılı)

    [Header("Sound")]
    [SerializeField] private AudioClip successSound;
    [SerializeField] [Range(0f, 1f)] private float successVol = 0.5f;
    [SerializeField] private AudioClip jamSound;             // Kağıt sıkışma sesi
    [SerializeField] [Range(0f, 1f)] private float jamVol = 0.6f;
    [SerializeField] private AudioClip printerHumSound;      // Yazıcı uğultusu
    [SerializeField] [Range(0f, 1f)] private float humVol = 0.2f;

    [Header("Visuals")]
    [SerializeField] private Color barBgColor = new Color(0.2f, 0.2f, 0.25f);
    [SerializeField] private Color safeZoneColor = new Color(0.2f, 0.8f, 0.3f, 0.5f);
    [SerializeField] private Color needleColor = Color.white;
    [SerializeField] private Color dangerColor = new Color(0.9f, 0.2f, 0.15f);

    // UI
    private GameObject gameCanvas;
    private GameObject panelObj;
    private RectTransform needleRect;
    private RectTransform safeZoneRect;
    private TextMeshProUGUI statusText;
    private TextMeshProUGUI titleText;
    private Image needleImage;
    private Image barBgImage;

    // State
    private bool isActive = false;
    private float needlePos = 0f;       // 0-1
    private float needleDir = 1f;       // hareket yönü
    private float currentSpeed;
    private float currentZoneStart;
    private float currentZoneEnd;
    private int attempts = 0;
    private System.Action<bool> onCompleteCallback;

    // Garip yazılar (Gün 3)
    private readonly string[] creepyTexts = {
        "Kağıt sıkıştı... yine...",
        "Bu kağıtta bir şeyler yazıyor...",
        "\"ÇIKIŞ YOK\" yazıyor kağıtta...",
        "Kağıt... ıslak mı?",
        "Yazıcı garip sesler çıkarıyor...",
        "Kağıtta senin ismin yazıyor...",
        "\"TEKRAR DENE\" ... ... ...",
        "Mürekkep... kırmızı mı?",
    };

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        CreateUI();
    }

    private void CreateUI()
    {
        gameCanvas = new GameObject("PrinterMiniGameCanvas");
        gameCanvas.transform.SetParent(transform);
        Canvas c = gameCanvas.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 270;
        CanvasScaler s = gameCanvas.AddComponent<CanvasScaler>();
        s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        s.referenceResolution = new Vector2(1920, 1080);
        gameCanvas.AddComponent<GraphicRaycaster>();

        // Panel arka plan
        panelObj = new GameObject("BgPanel");
        panelObj.transform.SetParent(gameCanvas.transform, false);
        Image bg = panelObj.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.7f);
        SetFullStretch(panelObj);

        // Başlık
        GameObject titleObj = CreateRect(panelObj, "Title",
            new Vector2(0.15f, 0.72f), new Vector2(0.85f, 0.82f));
        titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "YAZICI";
        titleText.fontSize = 40;
        titleText.color = Color.white;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.Center;

        // Status yazısı
        GameObject statusObj = CreateRect(panelObj, "Status",
            new Vector2(0.15f, 0.28f), new Vector2(0.85f, 0.38f));
        statusText = statusObj.AddComponent<TextMeshProUGUI>();
        statusText.text = "SPACE tuşuna tam zamanında bas!";
        statusText.fontSize = 22;
        statusText.color = new Color(0.8f, 0.8f, 0.8f);
        statusText.alignment = TextAlignmentOptions.Center;

        // Bar arka planı
        GameObject barBg = CreateRect(panelObj, "BarBg",
            new Vector2(0.1f, 0.48f), new Vector2(0.9f, 0.6f));
        barBgImage = barBg.AddComponent<Image>();
        barBgImage.color = barBgColor;

        // Outline
        Outline outline = barBg.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 1f, 1f, 0.2f);
        outline.effectDistance = new Vector2(2f, 2f);

        // Güvenli alan (yeşil)
        GameObject safeObj = new GameObject("SafeZone");
        safeObj.transform.SetParent(barBg.transform, false);
        Image safeImg = safeObj.AddComponent<Image>();
        safeImg.color = safeZoneColor;
        safeZoneRect = safeObj.GetComponent<RectTransform>();
        safeZoneRect.anchorMin = new Vector2(0.4f, 0f);
        safeZoneRect.anchorMax = new Vector2(0.6f, 1f);
        safeZoneRect.offsetMin = Vector2.zero;
        safeZoneRect.offsetMax = Vector2.zero;

        // İbre (dikey çizgi)
        GameObject needleObj = new GameObject("Needle");
        needleObj.transform.SetParent(barBg.transform, false);
        needleImage = needleObj.AddComponent<Image>();
        needleImage.color = needleColor;
        needleRect = needleObj.GetComponent<RectTransform>();
        needleRect.anchorMin = new Vector2(0f, 0f);
        needleRect.anchorMax = new Vector2(0.01f, 1f);
        needleRect.offsetMin = Vector2.zero;
        needleRect.offsetMax = Vector2.zero;

        panelObj.SetActive(false);
    }

    // ==================== Başlat ====================

    public void StartGame(System.Action<bool> onComplete = null)
    {
        if (isActive) return;
        onCompleteCallback = onComplete;
        isActive = true;
        attempts = 0;
        needlePos = 0f;
        needleDir = 1f;

        int day = GameManager.Instance != null ? GameManager.Instance.CurrentDay : 1;
        SetupForDay(day);

        panelObj.SetActive(true);

        // Yazıcı sesi
        SFXManager.Play2D(printerHumSound, humVol);

        Player p = FindFirstObjectByType<Player>();
        if (p != null) p.SetCanMove(false);

        titleText.text = day >= 3 ? "Y̸A̵Z̷I̶C̸I̵" : "YAZICI";
        statusText.text = "SPACE tuşuna tam zamanında bas!";
        statusText.color = new Color(0.8f, 0.8f, 0.8f);
    }

    private void SetupForDay(int day)
    {
        float zoneSize;

        switch (day)
        {
            case 1:
                currentSpeed = baseSpeed;
                zoneSize = baseZoneSize;
                break;
            case 2:
                currentSpeed = baseSpeed * day2SpeedMultiplier;
                zoneSize = day2ZoneSize;
                break;
            default: // Gün 3+
                currentSpeed = baseSpeed * day3SpeedMultiplier;
                zoneSize = day3ZoneSize;
                break;
        }

        // Yeşil alanı rastgele konumlandır
        float center = Random.Range(0.2f + zoneSize, 0.8f - zoneSize);
        currentZoneStart = center - zoneSize * 0.5f;
        currentZoneEnd = center + zoneSize * 0.5f;

        safeZoneRect.anchorMin = new Vector2(currentZoneStart, 0f);
        safeZoneRect.anchorMax = new Vector2(currentZoneEnd, 1f);
    }

    // ==================== Update ====================

    void Update()
    {
        if (!isActive) return;

        // İbre hareketi (ping-pong)
        needlePos += needleDir * currentSpeed * Time.deltaTime;
        if (needlePos >= 1f)
        {
            needlePos = 1f;
            needleDir = -1f;
        }
        else if (needlePos <= 0f)
        {
            needlePos = 0f;
            needleDir = 1f;
        }

        // İbre UI güncelle
        needleRect.anchorMin = new Vector2(needlePos - 0.005f, 0f);
        needleRect.anchorMax = new Vector2(needlePos + 0.005f, 1f);

        // İbre yeşil alandayken renk değiştir
        bool inZone = needlePos >= currentZoneStart && needlePos <= currentZoneEnd;
        needleImage.color = inZone ? Color.green : needleColor;

        // SPACE basıldı mı?
        if (Input.GetKeyDown(KeyCode.Space))
        {
            CheckTiming();
        }
    }

    private void CheckTiming()
    {
        bool inZone = needlePos >= currentZoneStart && needlePos <= currentZoneEnd;
        int day = GameManager.Instance != null ? GameManager.Instance.CurrentDay : 1;

        if (inZone)
        {
            // Başarılı!
            SFXManager.Play2D(successSound, successVol);

            if (day >= 3)
            {
                // Gün 3: garip yazı göster, sonra bitir
                string creepy = creepyTexts[Random.Range(0, creepyTexts.Length)];
                statusText.text = creepy;
                statusText.color = dangerColor;
                StartCoroutine(DelayedEnd(1.5f, true));
            }
            else
            {
                statusText.text = "Kağıt alındı!";
                statusText.color = Color.green;
                StartCoroutine(DelayedEnd(0.8f, true));
            }
            isActive = false;
        }
        else
        {
            // Başarısız — kağıt sıkıştı!
            attempts++;
            SFXManager.Play2D(jamSound, jamVol);

            if (attempts >= maxAttempts)
            {
                // Çok denedi — otomatik başarılı
                statusText.text = "Kağıdı zorla çıkardın...";
                statusText.color = Color.yellow;
                isActive = false;
                StartCoroutine(DelayedEnd(1f, true));
                return;
            }

            if (day >= 3)
            {
                string creepy = creepyTexts[Random.Range(0, creepyTexts.Length)];
                statusText.text = creepy;
                statusText.color = dangerColor;
            }
            else if (day >= 2)
            {
                statusText.text = $"Kağıt sıkıştı! ({attempts}/{maxAttempts}) Tekrar dene...";
                statusText.color = Color.yellow;
            }
            else
            {
                statusText.text = "Kağıt sıkıştı! Tekrar dene!";
                statusText.color = dangerColor;
            }

            // Bar titresi
            StartCoroutine(BarShake());

            // Gün 2+: her denemede alan küçülür
            if (day >= 2)
            {
                float shrink = 0.02f;
                currentZoneStart += shrink * 0.5f;
                currentZoneEnd -= shrink * 0.5f;
                if (currentZoneEnd - currentZoneStart < 0.03f)
                {
                    currentZoneEnd = currentZoneStart + 0.03f;
                }
                safeZoneRect.anchorMin = new Vector2(currentZoneStart, 0f);
                safeZoneRect.anchorMax = new Vector2(currentZoneEnd, 1f);
            }
        }
    }

    private System.Collections.IEnumerator BarShake()
    {
        RectTransform barParent = barBgImage.GetComponent<RectTransform>();
        Vector2 orig = barParent.anchoredPosition;

        for (int i = 0; i < 5; i++)
        {
            barParent.anchoredPosition = orig + new Vector2(
                Random.Range(-8f, 8f), Random.Range(-4f, 4f));
            yield return new WaitForSeconds(0.04f);
        }
        barParent.anchoredPosition = orig;
    }

    private System.Collections.IEnumerator DelayedEnd(float delay, bool success)
    {
        yield return new WaitForSeconds(delay);
        EndGame(success);
    }

    private void EndGame(bool success)
    {
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
