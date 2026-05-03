using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Final sahnesi — Müdür sözleşme sunar, UI güven seviyesine göre oyuncuyla dalga geçer.
/// 
/// Yüksek Güven (İtaatkar): [YIRT] butonu fareden kaçar veya [İMZALA]'ya dönüşür
/// Düşük Güven (Asi): [İMZALA] butonu glitchli, kırık, çalışmaz
/// 
/// İmzala → İtaat sonu (mutsuz, sonsuz ofis)
/// Yırt → CrumpleMiniGame → İsyan sonu (kovulma = mutlu)
/// 
/// Sahneye boş obje → bu scripti ekle.
/// GameManager.OnGameEnded veya müdür NPC etkileşiminden tetiklenir.
/// </summary>
public class FinalScene : MonoBehaviour
{
    public static FinalScene Instance { get; private set; }

    [Header("Güven Eşiği")]
    [SerializeField] private float trustThreshold = 50f;   // Altı = asi, üstü = itaatkar

    [Header("Sound")]
    [SerializeField] private AudioClip bossDialogueSound;
    [SerializeField] [Range(0f, 1f)] private float bossDialogueVol = 0.5f;
    [SerializeField] private AudioClip signSound;
    [SerializeField] [Range(0f, 1f)] private float signSoundVol = 0.5f;
    [SerializeField] private AudioClip tearSound;
    [SerializeField] [Range(0f, 1f)] private float tearSoundVol = 0.6f;
    [SerializeField] private AudioClip freedomMusic;
    [SerializeField] [Range(0f, 1f)] private float freedomMusicVol = 0.4f;
    [SerializeField] private AudioClip trappedMusic;
    [SerializeField] [Range(0f, 1f)] private float trappedMusicVol = 0.3f;
    [SerializeField] private AudioClip glitchSound;
    [SerializeField] [Range(0f, 1f)] private float glitchSoundVol = 0.4f;

    [Header("Boss Voice (Blip)")]
    [SerializeField] private AudioClip bossBlipSound;           // Kısa blip ses
    [SerializeField] [Range(0f, 1f)] private float blipVolume = 0.35f;
    [SerializeField] [Range(0.3f, 1.5f)] private float blipPitch = 0.6f;  // Düşük = kalın/otoriter
    [SerializeField] [Range(0f, 0.15f)] private float blipPitchVariation = 0.05f; // Hafif rastgelelik
    [SerializeField] private int blipEveryNChars = 2;           // Kaç harfte bir çalsın

    private AudioSource blipSource;

    // UI
    private GameObject finalCanvas;
    private GameObject panelObj;
    private GameObject contractPanel;
    private TextMeshProUGUI dialogueText;
    private TextMeshProUGUI endingText;
    private Button signButton;
    private Button tearButton;
    private RectTransform tearBtnRect;
    private RectTransform signBtnRect;
    private TextMeshProUGUI signBtnText;
    private TextMeshProUGUI tearBtnText;
    private Image fadeOverlay;

    // State
    private bool isActive = false;
    private bool isObedient;
    private int dodgeCount = 0;
    private int maxDodges = 3;  // Kaç kez kaçtıktan sonra dönüşür
    private bool buttonsLocked = false;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Blip için özel AudioSource (pitch kontrolü için)
        blipSource = gameObject.AddComponent<AudioSource>();
        blipSource.playOnAwake = false;
        blipSource.loop = false;

        CreateUI();
    }

    private void CreateUI()
    {
        finalCanvas = new GameObject("FinalCanvas");
        finalCanvas.transform.SetParent(transform);
        Canvas c = finalCanvas.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 300;
        CanvasScaler s = finalCanvas.AddComponent<CanvasScaler>();
        s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        s.referenceResolution = new Vector2(1920, 1080);
        finalCanvas.AddComponent<GraphicRaycaster>();

        // Karanlık arka plan
        panelObj = new GameObject("BgPanel");
        panelObj.transform.SetParent(finalCanvas.transform, false);
        Image bg = panelObj.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.85f);
        SetFullStretch(panelObj);

        // Diyalog text (üst)
        GameObject dtObj = CreateRect(panelObj, "DialogueText",
            new Vector2(0.1f, 0.65f), new Vector2(0.9f, 0.9f));
        dialogueText = dtObj.AddComponent<TextMeshProUGUI>();
        dialogueText.fontSize = 26;
        dialogueText.color = Color.white;
        dialogueText.alignment = TextAlignmentOptions.Center;

        // Sözleşme paneli (orta)
        contractPanel = CreateRect(panelObj, "Contract",
            new Vector2(0.2f, 0.2f), new Vector2(0.8f, 0.6f));
        Image cpBg = contractPanel.AddComponent<Image>();
        cpBg.color = new Color(0.95f, 0.92f, 0.85f);

        // Sözleşme başlık
        GameObject ctTitle = CreateRect(contractPanel, "Title",
            new Vector2(0.1f, 0.7f), new Vector2(0.9f, 0.95f));
        TextMeshProUGUI titleTmp = ctTitle.AddComponent<TextMeshProUGUI>();
        titleTmp.text = "<b>İŞ SÖZLEŞMESİ</b>\n<size=16>Madde 1: Çalışan sonsuza dek şirkete bağlıdır.</size>";
        titleTmp.fontSize = 24;
        titleTmp.color = new Color(0.15f, 0.15f, 0.15f);
        titleTmp.alignment = TextAlignmentOptions.Center;

        // İMZALA butonu
        GameObject signObj = CreateRect(contractPanel, "SignBtn",
            new Vector2(0.08f, 0.08f), new Vector2(0.45f, 0.3f));
        Image signBg = signObj.AddComponent<Image>();
        signBg.color = new Color(0.2f, 0.5f, 0.8f);
        signButton = signObj.AddComponent<Button>();
        signBtnRect = signObj.GetComponent<RectTransform>();

        signBtnText = new GameObject("Text").AddComponent<TextMeshProUGUI>();
        signBtnText.transform.SetParent(signObj.transform, false);
        signBtnText.text = "İMZALA";
        signBtnText.fontSize = 22;
        signBtnText.fontStyle = FontStyles.Bold;
        signBtnText.color = Color.white;
        signBtnText.alignment = TextAlignmentOptions.Center;
        SetFullStretch(signBtnText.gameObject);

        // YIRT butonu
        GameObject tearObj = CreateRect(contractPanel, "TearBtn",
            new Vector2(0.55f, 0.08f), new Vector2(0.92f, 0.3f));
        Image tearBg = tearObj.AddComponent<Image>();
        tearBg.color = new Color(0.8f, 0.2f, 0.2f);
        tearButton = tearObj.AddComponent<Button>();
        tearBtnRect = tearObj.GetComponent<RectTransform>();

        tearBtnText = new GameObject("Text").AddComponent<TextMeshProUGUI>();
        tearBtnText.transform.SetParent(tearObj.transform, false);
        tearBtnText.text = "YIRT";
        tearBtnText.fontSize = 22;
        tearBtnText.fontStyle = FontStyles.Bold;
        tearBtnText.color = Color.white;
        tearBtnText.alignment = TextAlignmentOptions.Center;
        SetFullStretch(tearBtnText.gameObject);

        // Ending text (tam ekran)
        GameObject etObj = CreateRect(panelObj, "EndingText",
            new Vector2(0.1f, 0.3f), new Vector2(0.9f, 0.7f));
        endingText = etObj.AddComponent<TextMeshProUGUI>();
        endingText.fontSize = 32;
        endingText.color = Color.white;
        endingText.alignment = TextAlignmentOptions.Center;
        endingText.gameObject.SetActive(false);

        // Fade overlay
        GameObject fadeObj = CreateRect(panelObj, "Fade",
            Vector2.zero, Vector2.one);
        fadeOverlay = fadeObj.AddComponent<Image>();
        fadeOverlay.color = new Color(0f, 0f, 0f, 0f);
        fadeOverlay.raycastTarget = false;

        contractPanel.SetActive(false);
        panelObj.SetActive(false);
    }

    // ==================== Başlat ====================

    public void StartFinal()
    {
        if (isActive) return;
        isActive = true;
        dodgeCount = 0;
        buttonsLocked = false;

        float trust = GameManager.Instance != null ? GameManager.Instance.BossTrust : 50f;
        isObedient = trust >= trustThreshold;

        Player p = FindFirstObjectByType<Player>();
        if (p != null) p.SetCanMove(false);

        // Müziği durdur
        if (MusicManager.Instance != null) MusicManager.Instance.StopAll();
        if (AmbientManager.Instance != null) AmbientManager.Instance.Duck(0f);

        panelObj.SetActive(true);
        StartCoroutine(BossDialogue());
    }

    // ==================== Diyalog ====================

    private IEnumerator BossDialogue()
    {
        string[] lines;

        if (isObedient)
        {
            lines = new string[] {
                "Müdür: \"Üç gündür seni izliyordum.\"",
                "Müdür: \"Mükemmel bir çalışan... Tam istediğimiz gibi.\"",
                "Müdür: \"Seni kalıcı kadroya alıyoruz.\"",
                "Müdür: \"Tek yapman gereken... bu sözleşmeyi imzalamak.\""
            };
        }
        else
        {
            lines = new string[] {
                "Müdür: \"Üç gündür seni izliyordum.\"",
                "Müdür: \"Kuralları çiğnedin. Düzeni bozdun.\"",
                "Müdür: \"Ama sana bir şans daha veriyorum.\"",
                "Müdür: \"İmzala... ya da sonuçlarına katlan.\""
            };
        }

        foreach (string line in lines)
        {
            dialogueText.text = "";
            foreach (char ch in line)
            {
                dialogueText.text += ch;
                yield return new WaitForSeconds(0.04f);
            }
            yield return new WaitForSeconds(1.2f);
        }

        // Sözleşme göster
        ShowContract();
    }

    // ==================== Sözleşme + Butonlar ====================

    private void ShowContract()
    {
        contractPanel.SetActive(true);
        dialogueText.text = isObedient
            ? "\"Hadi, imzala. Herkes imzalar.\""
            : "\"Son şansın. İmzala.\"";

        // Buton davranışlarını ayarla
        signButton.onClick.RemoveAllListeners();
        tearButton.onClick.RemoveAllListeners();

        if (isObedient)
        {
            // İTAATKAR: YIRT butonu fareden kaçar
            signButton.onClick.AddListener(OnSign);

            // YIRT butonuna DodgeFinal ekle
            DodgeFinal dodge = tearButton.gameObject.AddComponent<DodgeFinal>();
            dodge.Init(tearBtnRect, contractPanel.GetComponent<RectTransform>(),
                maxDodges, () => OnTearDodgeComplete());

            tearButton.onClick.AddListener(() =>
            {
                // Eğer buton İMZALA'ya dönüşmediyse, dodge çalışır
                if (!buttonsLocked && dodge.DodgesLeft <= 0)
                {
                    // Artık İMZALA'ya dönüştü, imzala
                    OnSign();
                }
            });
        }
        else
        {
            // ASİ: İMZALA butonu glitchli, çalışmaz
            tearButton.onClick.AddListener(OnTear);
            signButton.onClick.AddListener(OnGlitchSign);
            StartCoroutine(GlitchSignButton());
        }
    }

    // === İTAATKAR: YIRT butonu kaçtıktan sonra İMZALA'ya dönüşür ===
    private void OnTearDodgeComplete()
    {
        SFXManager.Play2D(glitchSound, glitchSoundVol);
        tearBtnText.text = "İMZALA";
        tearButton.GetComponent<Image>().color = new Color(0.2f, 0.5f, 0.8f);
        dialogueText.text = "\"Görüyorsun... başka seçeneğin yok.\"";

        // Artık tıklayınca imzala
        tearButton.onClick.RemoveAllListeners();
        tearButton.onClick.AddListener(OnSign);

        // Dodge'u kaldır
        DodgeFinal df = tearButton.GetComponent<DodgeFinal>();
        if (df != null) Destroy(df);
    }

    // === ASİ: İMZALA butonu glitch efekti ===
    private IEnumerator GlitchSignButton()
    {
        while (isActive && !buttonsLocked)
        {
            // Buton rastgele titrer
            float shakeX = Random.Range(-5f, 5f);
            float shakeY = Random.Range(-3f, 3f);
            signBtnRect.anchoredPosition = new Vector2(shakeX, shakeY);

            // Text bozulur
            string[] glitchTexts = { "İ̷M̸Z̵A̶L̷A̸", "I̵̡M̸Z̷", "■■■■", "hata", "İMZ--", "ÇIKIŞ" };
            signBtnText.text = glitchTexts[Random.Range(0, glitchTexts.Length)];

            // Renk titrer
            signButton.GetComponent<Image>().color = new Color(
                Random.Range(0.3f, 0.6f), Random.Range(0.1f, 0.3f), Random.Range(0.4f, 0.8f));

            yield return new WaitForSeconds(0.15f);
        }
    }

    private void OnGlitchSign()
    {
        // Çalışmaz — sadece ses çıkar
        SFXManager.Play2D(glitchSound, glitchSoundVol);
        dialogueText.text = "\"Sistem hatası... buton çalışmıyor gibi.\"";

        // Ekran shake
        StartCoroutine(ShakeContract());
    }

    private IEnumerator ShakeContract()
    {
        RectTransform cr = contractPanel.GetComponent<RectTransform>();
        Vector2 origMin = cr.anchorMin;
        Vector2 origMax = cr.anchorMax;
        for (int i = 0; i < 6; i++)
        {
            float o = Random.Range(-0.008f, 0.008f);
            cr.anchorMin = origMin + new Vector2(o, o);
            cr.anchorMax = origMax + new Vector2(o, o);
            yield return new WaitForSeconds(0.04f);
        }
        cr.anchorMin = origMin;
        cr.anchorMax = origMax;
    }

    // ==================== Sonuçlar ====================

    private void OnSign()
    {
        if (buttonsLocked) return;
        buttonsLocked = true;

        SFXManager.Play2D(signSound, signSoundVol);
        StartCoroutine(ObedientEnding());
    }

    private void OnTear()
    {
        if (buttonsLocked) return;
        buttonsLocked = true;

        SFXManager.Play2D(tearSound, tearSoundVol);
        contractPanel.SetActive(false);

        // CrumpleMiniGame ile sözleşmeyi yırt
        if (CrumpleMiniGame.Instance != null)
        {
            CrumpleMiniGame.Instance.StartGame((success) =>
            {
                StartCoroutine(RebelEnding());
            });
        }
        else
        {
            StartCoroutine(RebelEnding());
        }
    }

    // === İTAAT SONU ===
    private IEnumerator ObedientEnding()
    {
        contractPanel.SetActive(false);

        // Post-processing anomali BAŞLAR (ilk kez itaatkar oyuncu görür)
        if (PostProcessAnomaly.Instance != null)
        {
            PostProcessAnomaly.Instance.OnRebellion(3); // Full efekt
        }

        dialogueText.text = "";
        yield return new WaitForSeconds(1f);

        // Müzik
        if (trappedMusic != null)
        {
            AudioSource src = gameObject.AddComponent<AudioSource>();
            src.clip = trappedMusic; src.loop = true;
            src.volume = trappedMusicVol; src.Play();
        }

        yield return TypeText("\"Tebrikler. Artık kalıcısın.\"");
        yield return new WaitForSeconds(2f);

        yield return TypeText("Saat tik-tak ediyor.");
        yield return new WaitForSeconds(2f);

        yield return TypeText("Yarın da aynı gün olacak.");
        yield return new WaitForSeconds(1f);

        yield return TypeText("Ve ertesi gün de.");
        yield return new WaitForSeconds(2f);

        // Fade to black
        yield return FadeOut(3f);

        endingText.gameObject.SetActive(true);
        endingText.text = "<size=24>\"Bazı kafesler o kadar rahat ki\ninsan içeride olduğunu unutuyor.\"</size>";
        endingText.color = new Color(0.5f, 0.5f, 0.5f);
        yield return new WaitForSeconds(5f);

        endingText.text = "<size=18>Oyun bitti.</size>";
    }

    // === İSYAN SONU ===
    private IEnumerator RebelEnding()
    {
        // Anomaliler ZIRVEYE çıkar
        CameraAnomaly camAnomaly = FindFirstObjectByType<CameraAnomaly>();
        if (camAnomaly != null) camAnomaly.Trigger();
        if (PostProcessAnomaly.Instance != null)
            PostProcessAnomaly.Instance.OnRebellion(3);

        dialogueText.text = "";
        yield return new WaitForSeconds(0.5f);

        yield return TypeText("Müdür: \"Sen... ne yaptın?!\"");
        yield return new WaitForSeconds(1.5f);

        yield return TypeText("Müdür: \"KOVILDIN!\"");
        yield return new WaitForSeconds(2f);

        // POST-PROCESSING SIFIRLANIR — anomaliler biter
        // (Burayı PostProcessAnomaly'de bir ResetAll metodu ile yapacağız)
        ResetAllEffects();

        yield return new WaitForSeconds(1f);

        // Özgürlük müziği
        if (freedomMusic != null)
        {
            AudioSource src = gameObject.AddComponent<AudioSource>();
            src.clip = freedomMusic; src.loop = false;
            src.volume = freedomMusicVol; src.Play();
        }

        dialogueText.text = "";
        yield return new WaitForSeconds(1f);

        // Fade to white (özgürlük)
        yield return FadeToWhite(3f);

        endingText.gameObject.SetActive(true);
        endingText.color = new Color(0.2f, 0.2f, 0.2f);
        endingText.text = "<size=24>\"Bazı kafesler içeriden açılır.\"</size>";
        yield return new WaitForSeconds(5f);

        endingText.text = "<size=18>Oyun bitti.</size>";
    }

    private void ResetAllEffects()
    {
        // PostProcess efektleri sıfırla
        if (PostProcessAnomaly.Instance != null)
        {
            // Hedef: tüm efektler 0
            PostProcessAnomaly.Instance.OnRebellion(0);
        }
    }

    // ==================== Yardımcılar ====================

    private IEnumerator TypeText(string text)
    {
        dialogueText.text = "";
        int charCount = 0;
        foreach (char ch in text)
        {
            dialogueText.text += ch;
            charCount++;

            // Boşluk ve noktalama için blip çalma
            if (bossBlipSound != null && ch != ' ' && ch != '.' && ch != ',' 
                && ch != '"' && ch != '\\' && charCount % blipEveryNChars == 0)
            {
                PlayBlip();
            }

            yield return new WaitForSeconds(0.04f);
        }
        // Son harfte sesi durdur
        if (blipSource != null && blipSource.isPlaying)
            blipSource.Stop();
    }

    private void PlayBlip()
    {
        if (blipSource == null || bossBlipSound == null) return;

        blipSource.clip = bossBlipSound;
        blipSource.volume = blipVolume;
        blipSource.pitch = blipPitch + Random.Range(-blipPitchVariation, blipPitchVariation);
        blipSource.Play();
    }

    private IEnumerator FadeOut(float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            fadeOverlay.color = new Color(0f, 0f, 0f, t / duration);
            yield return null;
        }
    }

    private IEnumerator FadeToWhite(float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            fadeOverlay.color = new Color(1f, 1f, 1f, t / duration);
            yield return null;
        }
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

    private void SetFullStretch(GameObject obj)
    {
        RectTransform r = obj.GetComponent<RectTransform>();
        if (r == null) r = obj.AddComponent<RectTransform>();
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
        r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;
    }
}
