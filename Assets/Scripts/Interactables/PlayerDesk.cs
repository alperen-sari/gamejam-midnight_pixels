using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Oyuncunun kendi masası. Tüm görevler bitince "Günü Bitir" seçeneği çıkar.
/// Gün geçiş efektini (fade) de yönetir.
/// 
/// Masaya bu scripti ekle + Box Collider 2D + Layer: Interactable
/// </summary>
public class PlayerDesk : MonoBehaviour, IInteractable
{
    [Header("Fade Settings")]
    [SerializeField] private float fadeDuration = 1.5f;
    [SerializeField] private float dayTextDuration = 2f;

    // Fade UI (runtime oluşturulur)
    private GameObject fadeCanvas;
    private Image fadePanel;
    private TextMeshProUGUI dayText;

    void Start()
    {
        CreateFadeUI();
    }

    // ==================== IInteractable ====================

    public void Interact(Player player)
    {
        if (!CanInteract()) return;

        if (AreAllTasksDone())
        {
            // Tüm görevler bitti → gün bitir seçeneği
            ChoiceUI.Instance?.ShowChoices(
                ("Günü Bitir", () => EndDay()),
                ("Biraz Daha Kal", () => { })
            );
        }
        else
        {
            // Henüz görevler bitmedi
            DialogueSystem.Instance?.StartDialogue(new DialogueLine[]
            {
                new DialogueLine("", "Henüz yapılacak işlerim var..."),
                new DialogueLine("", "Bitmeden gidemem.")
            });
        }
    }

    public string GetInteractionPrompt()
    {
        if (AreAllTasksDone())
        {
            return "[E] Masana Otur (Günü Bitir)";
        }
        return "[E] Masana Otur";
    }

    public bool CanInteract()
    {
        return true; // Her zaman etkileşime açık
    }

    // ==================== Görev Kontrolü ====================

    /// <summary>
    /// Sahnedeki tüm ana görevlerin bitip bitmediğini kontrol eder.
    /// Her interactable'ın CanInteract() == false olması = görev tamam.
    /// </summary>
    private bool AreAllTasksDone()
    {
        // Kahve makinesi kullanıldı mı?
        CoffeeMaker coffee = FindFirstObjectByType<CoffeeMaker>();
        if (coffee != null && coffee.CanInteract()) return false;

        // Kahve teslim/tepki verildi mi?
        CoworkerDesk coworker = FindFirstObjectByType<CoworkerDesk>();
        if (coworker != null && coworker.CanInteract()) return false;

        // Yazıcı kullanıldı mı?
        Printer printer = FindFirstObjectByType<Printer>();
        if (printer != null && printer.CanInteract()) return false;

        // Evrak teslim/tepki verildi mi?
        DocumentDesk docDesk = FindFirstObjectByType<DocumentDesk>();
        if (docDesk != null && docDesk.CanInteract()) return false;

        // Bilgisayar (rapor + mail) tamamlandı mı?
        Computer computer = FindFirstObjectByType<Computer>();
        if (computer != null && computer.CanInteract()) return false;

        return true;
    }

    // ==================== Gün Geçişi ====================

    private void EndDay()
    {
        StartCoroutine(DayTransitionRoutine());
    }

    private System.Collections.IEnumerator DayTransitionRoutine()
    {
        // Oyuncuyu durdur
        Player player = FindFirstObjectByType<Player>();
        if (player != null) player.SetCanMove(false);

        // Fade out (siyaha)
        yield return StartCoroutine(Fade(0f, 1f, fadeDuration));

        // Gün geçir
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AdvanceDay();
        }

        // Gün yazısı göster
        int newDay = GameManager.Instance != null ? GameManager.Instance.CurrentDay : 1;
        dayText.text = $"G Ü N  {newDay}";
        dayText.gameObject.SetActive(true);

        yield return new WaitForSeconds(dayTextDuration);

        dayText.gameObject.SetActive(false);

        // Fade in (açıl)
        yield return StartCoroutine(Fade(1f, 0f, fadeDuration));

        // Oyuncuyu serbest bırak
        if (player != null) player.SetCanMove(true);
    }

    // ==================== Fade UI ====================

    private void CreateFadeUI()
    {
        fadeCanvas = new GameObject("FadeCanvas");
        Canvas canvas = fadeCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999; // En üstte

        CanvasScaler scaler = fadeCanvas.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        // Siyah panel (tam ekran)
        GameObject panelObj = new GameObject("FadePanel");
        panelObj.transform.SetParent(fadeCanvas.transform, false);
        fadePanel = panelObj.AddComponent<Image>();
        fadePanel.color = new Color(0f, 0f, 0f, 0f); // Başta şeffaf

        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        // Gün yazısı
        GameObject textObj = new GameObject("DayText");
        textObj.transform.SetParent(fadeCanvas.transform, false);
        dayText = textObj.AddComponent<TextMeshProUGUI>();
        dayText.fontSize = 72;
        dayText.color = Color.white;
        dayText.alignment = TextAlignmentOptions.Center;
        dayText.fontStyle = FontStyles.Bold;
        dayText.gameObject.SetActive(false);

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.2f, 0.35f);
        textRect.anchorMax = new Vector2(0.8f, 0.65f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        // Raycast'i engellemesin
        fadePanel.raycastTarget = false;
    }

    private System.Collections.IEnumerator Fade(float from, float to, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float alpha = Mathf.Lerp(from, to, t);
            fadePanel.color = new Color(0f, 0f, 0f, alpha);
            yield return null;
        }

        fadePanel.color = new Color(0f, 0f, 0f, to);
    }
}
