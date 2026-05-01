using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Mail okuma sistemi. Gün bazlı mailler gösterir.
/// Kendi UI'ını otomatik oluşturur.
/// 
/// Mailler okunabilir veya silinebilir (isyan).
/// </summary>
public class MailSystem : MonoBehaviour
{
    public static MailSystem Instance { get; private set; }

    [Header("Style")]
    [SerializeField] private Color bgColor = new Color(0.12f, 0.12f, 0.18f, 0.95f);
    [SerializeField] private Color headerColor = new Color(0.2f, 0.4f, 0.8f);
    [SerializeField] private Color textColor = Color.white;
    [SerializeField] private Color deleteColor = new Color(0.9f, 0.25f, 0.25f);

    private GameObject mailCanvas;
    private GameObject panelObj;
    private TextMeshProUGUI fromText;
    private TextMeshProUGUI subjectText;
    private TextMeshProUGUI bodyText;
    private TextMeshProUGUI instructionText;

    private List<MailData> currentMails = new List<MailData>();
    private int currentMailIndex = 0;
    private bool isOpen = false;
    private System.Action onCloseCallback;

    public bool IsOpen => isOpen;

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
        mailCanvas = new GameObject("MailCanvas");
        mailCanvas.transform.SetParent(transform);
        Canvas canvas = mailCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 220;

        CanvasScaler scaler = mailCanvas.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        mailCanvas.AddComponent<GraphicRaycaster>();

        // Panel
        panelObj = new GameObject("MailPanel");
        panelObj.transform.SetParent(mailCanvas.transform, false);
        Image panelBg = panelObj.AddComponent<Image>();
        panelBg.color = bgColor;

        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.15f, 0.1f);
        panelRect.anchorMax = new Vector2(0.85f, 0.9f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        // Header bar
        GameObject headerObj = new GameObject("Header");
        headerObj.transform.SetParent(panelObj.transform, false);
        Image headerBg = headerObj.AddComponent<Image>();
        headerBg.color = headerColor;

        RectTransform headerRect = headerObj.GetComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0f, 0.88f);
        headerRect.anchorMax = new Vector2(1f, 1f);
        headerRect.offsetMin = Vector2.zero;
        headerRect.offsetMax = Vector2.zero;

        // "MAIL" title
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(headerObj.transform, false);
        TextMeshProUGUI titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
        titleTmp.text = "📧 MAIL";
        titleTmp.fontSize = 24;
        titleTmp.color = Color.white;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.alignment = TextAlignmentOptions.MidlineLeft;

        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = Vector2.zero;
        titleRect.anchorMax = Vector2.one;
        titleRect.offsetMin = new Vector2(20, 0);
        titleRect.offsetMax = Vector2.zero;

        // Kimden
        GameObject fromObj = new GameObject("From");
        fromObj.transform.SetParent(panelObj.transform, false);
        fromText = fromObj.AddComponent<TextMeshProUGUI>();
        fromText.fontSize = 18;
        fromText.color = new Color(0.6f, 0.6f, 0.7f);
        fromText.alignment = TextAlignmentOptions.TopLeft;

        RectTransform fromRect = fromObj.GetComponent<RectTransform>();
        fromRect.anchorMin = new Vector2(0f, 0.78f);
        fromRect.anchorMax = new Vector2(1f, 0.87f);
        fromRect.offsetMin = new Vector2(25, 0);
        fromRect.offsetMax = new Vector2(-25, 0);

        // Konu
        GameObject subObj = new GameObject("Subject");
        subObj.transform.SetParent(panelObj.transform, false);
        subjectText = subObj.AddComponent<TextMeshProUGUI>();
        subjectText.fontSize = 22;
        subjectText.color = Color.white;
        subjectText.fontStyle = FontStyles.Bold;
        subjectText.alignment = TextAlignmentOptions.TopLeft;

        RectTransform subRect = subObj.GetComponent<RectTransform>();
        subRect.anchorMin = new Vector2(0f, 0.70f);
        subRect.anchorMax = new Vector2(1f, 0.78f);
        subRect.offsetMin = new Vector2(25, 0);
        subRect.offsetMax = new Vector2(-25, 0);

        // İçerik
        GameObject bodyObj = new GameObject("Body");
        bodyObj.transform.SetParent(panelObj.transform, false);
        bodyText = bodyObj.AddComponent<TextMeshProUGUI>();
        bodyText.fontSize = 18;
        bodyText.color = textColor;
        bodyText.alignment = TextAlignmentOptions.TopLeft;

        RectTransform bodyRect = bodyObj.GetComponent<RectTransform>();
        bodyRect.anchorMin = new Vector2(0f, 0.15f);
        bodyRect.anchorMax = new Vector2(1f, 0.68f);
        bodyRect.offsetMin = new Vector2(25, 0);
        bodyRect.offsetMax = new Vector2(-25, 0);

        // Alt bilgi
        GameObject instrObj = new GameObject("Instructions");
        instrObj.transform.SetParent(panelObj.transform, false);
        instructionText = instrObj.AddComponent<TextMeshProUGUI>();
        instructionText.fontSize = 16;
        instructionText.color = new Color(0.5f, 0.5f, 0.6f);
        instructionText.alignment = TextAlignmentOptions.Center;

        RectTransform instrRect = instrObj.GetComponent<RectTransform>();
        instrRect.anchorMin = new Vector2(0f, 0.02f);
        instrRect.anchorMax = new Vector2(1f, 0.12f);
        instrRect.offsetMin = Vector2.zero;
        instrRect.offsetMax = Vector2.zero;

        panelObj.SetActive(false);
    }

    // ==================== Mail Açma ====================

    public void OpenMails(System.Action onClose = null)
    {
        if (isOpen) return;

        onCloseCallback = onClose;
        currentMails = GetMailsForDay();
        currentMailIndex = 0;
        isOpen = true;

        Player player = FindFirstObjectByType<Player>();
        if (player != null) player.SetCanMove(false);

        panelObj.SetActive(true);
        ShowCurrentMail();
    }

    private void ShowCurrentMail()
    {
        if (currentMailIndex >= currentMails.Count)
        {
            CloseMails();
            return;
        }

        MailData mail = currentMails[currentMailIndex];
        fromText.text = $"Kimden: {mail.From}";
        subjectText.text = mail.Subject;
        bodyText.text = mail.Body;
        instructionText.text = $"Mail {currentMailIndex + 1}/{currentMails.Count}  |  [E] Sonraki  [Backspace] Sil  [Esc] Kapat";
    }

    void Update()
    {
        if (!isOpen) return;

        if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space))
        {
            // Sonraki mail
            currentMailIndex++;
            ShowCurrentMail();
        }
        else if (Input.GetKeyDown(KeyCode.Backspace) || Input.GetKeyDown(KeyCode.Delete))
        {
            // Maili sil (isyan!)
            DeleteCurrentMail();
        }
        else if (Input.GetKeyDown(KeyCode.Escape))
        {
            CloseMails();
        }
    }

    private void DeleteCurrentMail()
    {
        MailData mail = currentMails[currentMailIndex];
        Debug.Log($"[MailSystem] Mail silindi: {mail.Subject}");

        // Kırılma artır
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddFracture(5f);
            GameManager.Instance.ReduceBossTrust(3f);
        }

        // Silinen mailin yerine glitch metin koy
        bodyText.text = "<color=#FF3333>[ M A İ L   S İ L İ N D İ ]</color>\n\n...geri alamazsın.";
        subjectText.text = "<s>" + mail.Subject + "</s>";

        // Kısa bekleme sonrası sonraki maile geç
        StartCoroutine(WaitAndNext());
    }

    private System.Collections.IEnumerator WaitAndNext()
    {
        yield return new WaitForSeconds(1f);
        currentMailIndex++;
        ShowCurrentMail();
    }

    private void CloseMails()
    {
        isOpen = false;
        panelObj.SetActive(false);

        Player player = FindFirstObjectByType<Player>();
        if (player != null) player.SetCanMove(true);

        onCloseCallback?.Invoke();
        onCloseCallback = null;
    }

    // ==================== Gün Bazlı Mailler ====================

    private List<MailData> GetMailsForDay()
    {
        int day = GameManager.Instance != null ? GameManager.Instance.CurrentDay : 1;
        List<MailData> mails = new List<MailData>();

        switch (day)
        {
            case 1:
                mails.Add(new MailData(
                    "Müdür Bey <mudur@sirket.com>",
                    "Günlük Hatırlatma",
                    "Merhaba,\n\nBugünkü görevlerinizi tamamlamayı unutmayın.\nKahve siparişi, evrak teslimi ve günlük rapor.\n\nBaşarılar,\nMüdür"
                ));
                mails.Add(new MailData(
                    "İK Departmanı <ik@sirket.com>",
                    "Hoşgeldiniz!",
                    "Şirketimize hoş geldiniz!\n\nÇalışma saatleriniz 09:00-18:00'dir.\nKurallarımıza uymanızı rica ederiz.\n\nİyi çalışmalar!"
                ));
                break;

            case 2:
                mails.Add(new MailData(
                    "Müdür Bey <mudur@sirket.com>",
                    "RE: Günlük Hatırlatma",
                    "Dünkü performansını değerlendirdim.\n\n...devam et.\n\nMüdür"
                ));
                mails.Add(new MailData(
                    "sistem@sirket.com",
                    "Otomatik Bildirim",
                    "Bu mail otomatik olarak gönderilmiştir.\nBu mail otomatik olarak gönderilmiştir.\nBu mail otomatik olarak gönderilmiştir.\nBu mail otomatik olarak gönderilmiştir.\n..."
                ));
                mails.Add(new MailData(
                    "??? <bilinmeyen@???.???>",
                    "(konu yok)",
                    "Bunu okuyan biri var mı?\n\n...bence de yok."
                ));
                break;

            case 3:
            default:
                mails.Add(new MailData(
                    "M̷ü̷d̷ü̷r̷",
                    ".",
                    "...\n\n\n\n\n\n                    bak."
                ));
                mails.Add(new MailData(
                    "Sen <sen@sen.sen>",
                    "Kendine Not",
                    "Bu maili sen yazdın.\nAma hatırlamıyorsun.\n\nÇünkü her gün aynı.\nHer gün aynı.\nHer gün aynı.\n\n...değil mi?"
                ));
                break;
        }

        return mails;
    }
}

[System.Serializable]
public class MailData
{
    public string From;
    public string Subject;
    public string Body;

    public MailData(string from, string subject, string body)
    {
        From = from;
        Subject = subject;
        Body = body;
    }
}
