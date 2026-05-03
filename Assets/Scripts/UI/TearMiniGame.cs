using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Stardew Valley balık tutma tarzı mini-game.
/// Dikey bar içinde hareketli bir hedef bölge var.
/// Oyuncu SPACE basılı tutarak imleci yukarı kaldırır, bırakınca düşer.
/// İmleç hedef bölgede olduğu sürece ilerleme barı dolar.
/// Bölge dışındaysa ilerleme barı azalır.
/// 
/// Kullanım: TearMiniGame.Instance.StartGame(callback);
/// </summary>
public class TearMiniGame : MonoBehaviour
{
    public static TearMiniGame Instance { get; private set; }

    [Header("Gameplay")]
    [SerializeField] private float cursorSpeed = 3f;         // SPACE basınca yukarı hız
    [SerializeField] private float gravity = 4f;             // Bırakınca düşme hızı
    [SerializeField] private float targetMoveSpeed = 2f;     // Hedef bölge hareket hızı
    [SerializeField] private float targetZoneSize = 0.25f;   // Hedef bölge boyutu (0-1)
    [SerializeField] private float fillSpeed = 0.3f;         // İçindeyken dolma hızı
    [SerializeField] private float drainSpeed = 0.2f;        // Dışındayken boşalma hızı
    [SerializeField] private float winThreshold = 1f;        // Kazanma eşiği

    [Header("Sound")]
    [SerializeField] private AudioClip progressSound;
    [SerializeField] [Range(0f, 1f)] private float progressSoundVol = 0.3f;
    [SerializeField] private AudioClip failSound;
    [SerializeField] [Range(0f, 1f)] private float failSoundVol = 0.5f;
    [SerializeField] private AudioClip winSound;
    [SerializeField] [Range(0f, 1f)] private float winSoundVol = 0.6f;

    [Header("Visuals")]
    [SerializeField] private Color barBgColor = new Color(0.15f, 0.15f, 0.2f);
    [SerializeField] private Color targetZoneColor = new Color(0.2f, 0.7f, 0.3f, 0.4f);
    [SerializeField] private Color cursorColor = new Color(1f, 0.9f, 0.3f);
    [SerializeField] private Color progressColor = new Color(0.3f, 0.8f, 0.4f);

    // UI
    private GameObject gameCanvas;
    private GameObject panelObj;
    private RectTransform barRect;
    private RectTransform targetZoneRect;
    private RectTransform cursorRect;
    private Image progressFill;
    private TextMeshProUGUI instructionText;
    private TextMeshProUGUI titleText;

    // State
    private bool isActive = false;
    private float cursorPos = 0.5f;     // 0 (alt) — 1 (üst)
    private float targetPos = 0.5f;     // Hedef bölgenin merkezi
    private float targetDir = 1f;       // Hedef hareket yönü
    private float progress = 0f;        // 0 — 1 arası ilerleme
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

        // Arka plan (yarı saydam)
        panelObj = new GameObject("BgPanel");
        panelObj.transform.SetParent(gameCanvas.transform, false);
        Image bg = panelObj.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.7f);
        SetFullStretch(panelObj);

        // Başlık
        GameObject titleObj = CreateRect(panelObj, "Title",
            new Vector2(0.2f, 0.85f), new Vector2(0.8f, 0.95f));
        titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "SÖZLEŞMEYI YIRT!";
        titleText.fontSize = 36;
        titleText.color = Color.white;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.Center;

        // Talimat
        GameObject instrObj = CreateRect(panelObj, "Instruction",
            new Vector2(0.2f, 0.78f), new Vector2(0.8f, 0.85f));
        instructionText = instrObj.AddComponent<TextMeshProUGUI>();
        instructionText.text = "SPACE basılı tut — imleci yeşil bölgede tut!";
        instructionText.fontSize = 18;
        instructionText.color = new Color(0.8f, 0.8f, 0.8f);
        instructionText.alignment = TextAlignmentOptions.Center;

        // Ana bar (dikey, ortada)
        GameObject barBg = CreateRect(panelObj, "BarBg",
            new Vector2(0.45f, 0.1f), new Vector2(0.55f, 0.75f));
        barBg.AddComponent<Image>().color = barBgColor;
        barRect = barBg.GetComponent<RectTransform>();

        // Hedef bölge (yeşil)
        GameObject targetObj = new GameObject("TargetZone");
        targetObj.transform.SetParent(barBg.transform, false);
        Image tzImg = targetObj.AddComponent<Image>();
        tzImg.color = targetZoneColor;
        targetZoneRect = targetObj.GetComponent<RectTransform>();
        targetZoneRect.anchorMin = new Vector2(0f, 0.4f);
        targetZoneRect.anchorMax = new Vector2(1f, 0.6f);
        targetZoneRect.offsetMin = Vector2.zero;
        targetZoneRect.offsetMax = Vector2.zero;

        // İmleç (sarı çizgi)
        GameObject cursorObj = new GameObject("Cursor");
        cursorObj.transform.SetParent(barBg.transform, false);
        Image curImg = cursorObj.AddComponent<Image>();
        curImg.color = cursorColor;
        cursorRect = cursorObj.GetComponent<RectTransform>();
        cursorRect.anchorMin = new Vector2(0.05f, 0.49f);
        cursorRect.anchorMax = new Vector2(0.95f, 0.51f);
        cursorRect.offsetMin = Vector2.zero;
        cursorRect.offsetMax = Vector2.zero;

        // İlerleme barı (sağ taraf)
        GameObject progBg = CreateRect(panelObj, "ProgressBg",
            new Vector2(0.58f, 0.1f), new Vector2(0.62f, 0.75f));
        progBg.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f);

        GameObject progFill = CreateRect(progBg, "ProgressFill",
            Vector2.zero, new Vector2(1f, 0f));
        progressFill = progFill.AddComponent<Image>();
        progressFill.color = progressColor;

        panelObj.SetActive(false);
    }

    // ==================== Başlat ====================

    public void StartGame(System.Action<bool> onComplete = null)
    {
        if (isActive) return;
        onCompleteCallback = onComplete;
        isActive = true;
        cursorPos = 0.5f;
        targetPos = 0.5f;
        targetDir = 1f;
        progress = 0f;

        panelObj.SetActive(true);

        Player p = FindFirstObjectByType<Player>();
        if (p != null) p.SetCanMove(false);
    }

    // ==================== Update ====================

    void Update()
    {
        if (!isActive) return;

        // İmleç kontrolü
        if (Input.GetKey(KeyCode.Space))
        {
            cursorPos += cursorSpeed * Time.deltaTime;
        }
        else
        {
            cursorPos -= gravity * Time.deltaTime;
        }
        cursorPos = Mathf.Clamp01(cursorPos);

        // Hedef bölge hareketi (yukarı-aşağı bouncing)
        targetPos += targetDir * targetMoveSpeed * Time.deltaTime;
        if (targetPos >= 1f - targetZoneSize * 0.5f)
        {
            targetDir = -1f;
            targetPos = 1f - targetZoneSize * 0.5f;
        }
        else if (targetPos <= targetZoneSize * 0.5f)
        {
            targetDir = 1f;
            targetPos = targetZoneSize * 0.5f;
        }

        // İmleç hedef bölgede mi?
        float halfZone = targetZoneSize * 0.5f;
        bool inZone = cursorPos >= targetPos - halfZone && cursorPos <= targetPos + halfZone;

        if (inZone)
        {
            progress += fillSpeed * Time.deltaTime;
        }
        else
        {
            progress -= drainSpeed * Time.deltaTime;
        }
        progress = Mathf.Clamp(progress, 0f, winThreshold);

        // UI güncelle
        UpdateUI();

        // Kazandı mı?
        if (progress >= winThreshold)
        {
            SFXManager.Play2D(winSound, winSoundVol);
            EndGame(true);
        }
    }

    private void UpdateUI()
    {
        // İmleç pozisyonu
        cursorRect.anchorMin = new Vector2(0.05f, cursorPos - 0.015f);
        cursorRect.anchorMax = new Vector2(0.95f, cursorPos + 0.015f);

        // Hedef bölge pozisyonu
        float halfZone = targetZoneSize * 0.5f;
        targetZoneRect.anchorMin = new Vector2(0f, targetPos - halfZone);
        targetZoneRect.anchorMax = new Vector2(1f, targetPos + halfZone);

        // İlerleme barı
        RectTransform progRect = progressFill.GetComponent<RectTransform>();
        progRect.anchorMax = new Vector2(1f, progress / winThreshold);
    }

    private void EndGame(bool success)
    {
        isActive = false;
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
