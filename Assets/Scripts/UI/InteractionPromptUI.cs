using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Etkileşim promptu. Player yakınında IInteractable varken
/// ekranda "[E] Kahve Makinesi" gibi bir metin gösterir.
/// 
/// Player objesine ekle. Kendi UI elemanlarını otomatik oluşturur,
/// Canvas'ta elle bir şey kurmana gerek yok.
/// </summary>
public class InteractionPromptUI : MonoBehaviour
{
    [Header("Prompt Settings")]
    [SerializeField] private Vector2 screenOffset = new Vector2(0f, -200f); // Ekran merkezine göre offset
    [SerializeField] private int fontSize = 28;
    [SerializeField] private Color textColor = Color.white;
    [SerializeField] private Color backgroundColor = new Color(0f, 0f, 0f, 0.7f);

    private Player player;
    private GameObject promptCanvas;
    private TextMeshProUGUI promptText;
    private Image backgroundImage;

    void Start()
    {
        player = GetComponent<Player>();
        CreatePromptUI();
    }

    /// <summary>
    /// Prompt UI'ı kod ile oluşturur. Canvas'ta elle kurmana gerek yok.
    /// </summary>
    private void CreatePromptUI()
    {
        // Canvas oluştur
        promptCanvas = new GameObject("InteractionPromptCanvas");
        Canvas canvas = promptCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100; // Diğer UI'ların üstünde
        promptCanvas.AddComponent<CanvasScaler>();
        promptCanvas.AddComponent<GraphicRaycaster>();

        // Arka plan paneli
        GameObject bgObj = new GameObject("PromptBackground");
        bgObj.transform.SetParent(promptCanvas.transform, false);
        backgroundImage = bgObj.AddComponent<Image>();
        backgroundImage.color = backgroundColor;

        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0.5f, 0.5f);
        bgRect.anchorMax = new Vector2(0.5f, 0.5f);
        bgRect.pivot = new Vector2(0.5f, 0.5f);
        bgRect.anchoredPosition = screenOffset;

        // Horizontal Layout veya Content Size Fitter ile otomatik boyut
        ContentSizeFitter bgFitter = bgObj.AddComponent<ContentSizeFitter>();
        bgFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        bgFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        HorizontalLayoutGroup layout = bgObj.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(20, 20, 10, 10);
        layout.childAlignment = TextAnchor.MiddleCenter;

        // Metin
        GameObject textObj = new GameObject("PromptText");
        textObj.transform.SetParent(bgObj.transform, false);
        promptText = textObj.AddComponent<TextMeshProUGUI>();
        promptText.fontSize = fontSize;
        promptText.color = textColor;
        promptText.alignment = TextAlignmentOptions.Center;
        promptText.text = "";

        // Başlangıçta gizle
        promptCanvas.SetActive(false);
    }

    void Update()
    {
        if (player == null) return;

        IInteractable target = player.CurrentInteractable;

        if (target != null)
        {
            // Prompt göster
            promptCanvas.SetActive(true);
            promptText.text = target.GetInteractionPrompt();
        }
        else
        {
            // Prompt gizle
            promptCanvas.SetActive(false);
        }
    }

    void OnDestroy()
    {
        if (promptCanvas != null)
        {
            Destroy(promptCanvas);
        }
    }
}
