using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Diyalog sistemi. Kendi UI'ını otomatik oluşturur — elle Canvas kurmana gerek yok.
/// Boş bir GameObject'e ekle, çalışır.
/// 
/// Kırılma arttıkça metin otomatik bozulur (glitch efekti).
/// </summary>
public class DialogueSystem : MonoBehaviour
{
    public static DialogueSystem Instance { get; private set; }

    [Header("Style")]
    [SerializeField] private int fontSize = 24;
    [SerializeField] private float typingSpeed = 0.03f;
    [SerializeField] private Color speakerColor = new Color(1f, 0.85f, 0.2f);
    [SerializeField] private Color textColor = Color.white;
    [SerializeField] private Color bgColor = new Color(0.05f, 0.05f, 0.1f, 0.92f);

    [Header("Sound")]
    [SerializeField] private AudioClip talkSound;          // Normal konuşma sesi
    [SerializeField] private AudioClip anomalyTalkSound;   // Gün 3 / yüksek kırılmada konuşma sesi
    [SerializeField] private AudioClip deliverySound;      // Teslim onay sesi
    [SerializeField] [Range(0f, 1f)] private float talkVolume = 0.4f;

    // Events
    public System.Action<DialogueLine> OnDialogueStarted;
    public System.Action OnDialogueEnded;

    // UI elemanları (runtime'da oluşturulur)
    private GameObject dialogueCanvas;
    private GameObject panelObj;
    private TextMeshProUGUI speakerText;
    private TextMeshProUGUI dialogueText;
    private TextMeshProUGUI continueText;

    private Queue<DialogueLine> dialogueQueue = new Queue<DialogueLine>();
    private bool isDialogueActive = false;
    private bool isTyping = false;
    private string fullLineText = "";
    private Coroutine typingCoroutine;
    private AudioSource talkSource;   // Konuşma sesi için tek AudioSource

    // Diyalog bitince çağrılacak callback
    private System.Action onDialogueCompleteCallback;

    public bool IsDialogueActive => isDialogueActive;

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
        // Canvas
        dialogueCanvas = new GameObject("DialogueCanvas");
        dialogueCanvas.transform.SetParent(transform);
        Canvas canvas = dialogueCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 150;

        CanvasScaler scaler = dialogueCanvas.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        dialogueCanvas.AddComponent<GraphicRaycaster>();

        // Ana panel — ekranın altında
        panelObj = new GameObject("DialoguePanel");
        panelObj.transform.SetParent(dialogueCanvas.transform, false);

        Image panelBg = panelObj.AddComponent<Image>();
        panelBg.color = bgColor;

        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.1f, 0.02f);
        panelRect.anchorMax = new Vector2(0.9f, 0.22f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        // Konuşmacı adı — sol üst
        GameObject speakerObj = new GameObject("SpeakerName");
        speakerObj.transform.SetParent(panelObj.transform, false);
        speakerText = speakerObj.AddComponent<TextMeshProUGUI>();
        speakerText.fontSize = fontSize + 4;
        speakerText.color = speakerColor;
        speakerText.fontStyle = FontStyles.Bold;
        speakerText.alignment = TextAlignmentOptions.TopLeft;

        RectTransform speakerRect = speakerObj.GetComponent<RectTransform>();
        speakerRect.anchorMin = new Vector2(0f, 0.7f);
        speakerRect.anchorMax = new Vector2(0.5f, 1f);
        speakerRect.offsetMin = new Vector2(20, 0);
        speakerRect.offsetMax = new Vector2(0, -8);

        // Diyalog metni — ortada
        GameObject textObj = new GameObject("DialogueText");
        textObj.transform.SetParent(panelObj.transform, false);
        dialogueText = textObj.AddComponent<TextMeshProUGUI>();
        dialogueText.fontSize = fontSize;
        dialogueText.color = textColor;
        dialogueText.alignment = TextAlignmentOptions.TopLeft;

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0f, 0f);
        textRect.anchorMax = new Vector2(1f, 0.7f);
        textRect.offsetMin = new Vector2(20, 10);
        textRect.offsetMax = new Vector2(-20, -5);

        // "E ile devam" — sağ alt
        GameObject continueObj = new GameObject("ContinueIndicator");
        continueObj.transform.SetParent(panelObj.transform, false);
        continueText = continueObj.AddComponent<TextMeshProUGUI>();
        continueText.fontSize = fontSize - 6;
        continueText.color = new Color(1f, 1f, 1f, 0.5f);
        continueText.text = "▼ E";
        continueText.alignment = TextAlignmentOptions.BottomRight;
        continueText.gameObject.SetActive(false);

        RectTransform contRect = continueObj.GetComponent<RectTransform>();
        contRect.anchorMin = new Vector2(0.8f, 0f);
        contRect.anchorMax = new Vector2(1f, 0.3f);
        contRect.offsetMin = new Vector2(0, 5);
        contRect.offsetMax = new Vector2(-15, 0);

        panelObj.SetActive(false);
    }

    // ==================== Diyalog Kontrolü ====================

    /// <summary>
    /// Diyalog sekansı başlatır.
    /// </summary>
    public void StartDialogue(DialogueLine[] lines, System.Action onComplete = null)
    {
        if (isDialogueActive) return;

        dialogueQueue.Clear();
        foreach (var line in lines)
        {
            dialogueQueue.Enqueue(line);
        }

        onDialogueCompleteCallback = onComplete;
        isDialogueActive = true;
        panelObj.SetActive(true);

        // Oyuncuyu durdur
        Player player = FindFirstObjectByType<Player>();
        if (player != null)
        {
            player.SetCanMove(false);
        }

        ShowNextLine();
    }

    public void ShowNextLine()
    {
        if (dialogueQueue.Count == 0)
        {
            EndDialogue();
            return;
        }

        DialogueLine line = dialogueQueue.Dequeue();

        // Kırılma seviyesine göre boz
        if (GameManager.Instance != null)
        {
            line = ApplyFractureDistortion(line);
        }

        speakerText.text = line.SpeakerName;
        fullLineText = line.Text;
        continueText.gameObject.SetActive(false);

        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeText(fullLineText));

        OnDialogueStarted?.Invoke(line);
    }

    public void EndDialogue()
    {
        isDialogueActive = false;
        panelObj.SetActive(false);

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        // Konuşma sesini durdur
        StopTalkAudio();

        Player player = FindFirstObjectByType<Player>();
        if (player != null)
        {
            player.SetCanMove(true);
        }

        OnDialogueEnded?.Invoke();

        // Callback
        onDialogueCompleteCallback?.Invoke();
        onDialogueCompleteCallback = null;
    }

    // ==================== Typewriter ====================

    private System.Collections.IEnumerator TypeText(string text)
    {
        isTyping = true;
        dialogueText.text = "";

        // Hangi ses çalınacak? Gün 3 veya yüksek kırılmada anomali sesi
        AudioClip activeClip = GetActiveTalkSound();

        // Konuşma sesini başlat (loop)
        StartTalkAudio(activeClip);

        foreach (char c in text)
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }

        // Yazım bitti — sesi durdur
        StopTalkAudio();

        isTyping = false;
        continueText.gameObject.SetActive(true);
    }

    private AudioClip GetActiveTalkSound()
    {
        if (anomalyTalkSound != null && GameManager.Instance != null)
        {
            // Gün 3+ veya kırılma %50+ ise anomali sesi
            if (GameManager.Instance.CurrentDay >= 3 || GameManager.Instance.FracturePercent >= 0.5f)
            {
                return anomalyTalkSound;
            }
        }
        return talkSound;
    }

    private void StartTalkAudio(AudioClip clip)
    {
        if (clip == null) return;

        if (talkSource == null)
        {
            talkSource = gameObject.AddComponent<AudioSource>();
            talkSource.playOnAwake = false;
        }

        talkSource.clip = clip;
        talkSource.loop = true;
        talkSource.volume = talkVolume;
        talkSource.Play();
    }

    private void StopTalkAudio()
    {
        if (talkSource != null && talkSource.isPlaying)
        {
            talkSource.Stop();
        }
    }

    /// <summary>
    /// Teslim onay sesini çalar. Dışarıdan çağrılabilir.
    /// </summary>
    public void PlayDeliverySound()
    {
        if (deliverySound != null)
        {
            if (talkSource == null)
            {
                talkSource = gameObject.AddComponent<AudioSource>();
                talkSource.playOnAwake = false;
            }
            talkSource.PlayOneShot(deliverySound, talkVolume);
        }
    }

    void Update()
    {
        if (!isDialogueActive) return;

        if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space))
        {
            if (isTyping)
            {
                // Yazım devam ediyorsa → hemen bitir + sesi durdur
                if (typingCoroutine != null) StopCoroutine(typingCoroutine);
                StopTalkAudio();
                dialogueText.text = fullLineText;
                isTyping = false;
                continueText.gameObject.SetActive(true);
            }
            else
            {
                // Yazım bitmişse → sonraki satır (TypeText sesi tekrar başlatır)
                ShowNextLine();
            }
        }
    }

    // ==================== Kırılma Efekti ====================

    private DialogueLine ApplyFractureDistortion(DialogueLine line)
    {
        FractureStage stage = GameManager.Instance.CurrentFractureStage;

        switch (stage)
        {
            case FractureStage.Subtle:
                return line;

            case FractureStage.Noticeable:
                return new DialogueLine(line.SpeakerName, AddHesitation(line.Text));

            case FractureStage.Breaking:
                return new DialogueLine(GlitchText(line.SpeakerName), GlitchText(line.Text));

            default:
                return line;
        }
    }

    private string AddHesitation(string text)
    {
        string[] words = text.Split(' ');
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (int i = 0; i < words.Length; i++)
        {
            sb.Append(words[i]);
            if (Random.value < 0.25f) sb.Append("...");
            if (i < words.Length - 1) sb.Append(' ');
        }
        return sb.ToString();
    }

    private string GlitchText(string text)
    {
        char[] glitchChars = { '̷', '̶', '̸', '̵', '̴' };
        char[] chars = text.ToCharArray();
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (int i = 0; i < chars.Length; i++)
        {
            sb.Append(chars[i]);
            if (Random.value < 0.3f && !char.IsWhiteSpace(chars[i]))
                sb.Append(glitchChars[Random.Range(0, glitchChars.Length)]);
        }
        return sb.ToString();
    }
}

[System.Serializable]
public class DialogueLine
{
    public string SpeakerName;
    public string Text;

    public DialogueLine(string speaker, string text)
    {
        SpeakerName = speaker;
        Text = text;
    }
}
