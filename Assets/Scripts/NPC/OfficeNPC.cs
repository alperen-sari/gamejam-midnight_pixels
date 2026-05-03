using UnityEngine;

/// <summary>
/// Ofisteki NPC'lerin temel davranışı.
/// Günlük rutin, diyalog, ve kırılma bazlı davranış değişimleri.
/// </summary>
public class OfficeNPC : MonoBehaviour, IInteractable
{
    [Header("NPC Info")]
    [SerializeField] private string npcName = "Çalışan";

    [Header("Dialogue Per Day")]
    [SerializeField] private NPCDayDialogue[] dayDialogues;

    [Header("Görev Objeleri (Konuştuktan sonra ünlem çıkar)")]
    [SerializeField] private GameObject[] taskTargetObjects;  // Kahve makinesi, yazıcı vs.

    [Header("Anomaly Behavior")]
    [SerializeField] private bool canWalkBackwards = true;
    [SerializeField] private bool canStareAtPlayer = true;

    private SpriteRenderer spriteRenderer;
    private bool hasInteractedToday = false;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnDayChanged += OnDayChanged;
            GameManager.Instance.OnFractureStageChanged += OnFractureStageChanged;
        }
    }

    void Update()
    {
        // Kırılma yüksekken garip davranışlar
        if (GameManager.Instance != null && 
            GameManager.Instance.CurrentFractureStage == FractureStage.Breaking)
        {
            if (canStareAtPlayer)
            {
                LookAtPlayer();
            }
        }
    }

    // ==================== IInteractable ====================

    public void Interact(Player player)
    {
        if (!CanInteract()) return;

        DialogueLine[] lines = GetCurrentDialogue();
        if (lines != null && lines.Length > 0)
        {
            // Diyalog bitince görev objelerinde ünlem göster
            DialogueSystem.Instance?.StartDialogue(lines, () =>
            {
                ShowTaskIcons();
            });
            hasInteractedToday = true;

            // Kafadaki ünlemi gizle
            HeadIcon icon = GetComponent<HeadIcon>();
            if (icon != null) icon.OnInteracted();
        }
    }

    /// <summary>
    /// Diyalog bitince görev objelerindeki HeadIcon'ları açar.
    /// </summary>
    private void ShowTaskIcons()
    {
        if (taskTargetObjects == null) return;

        foreach (GameObject obj in taskTargetObjects)
        {
            if (obj == null) continue;
            HeadIcon icon = obj.GetComponent<HeadIcon>();
            if (icon != null) icon.Show();
        }
    }

    public string GetInteractionPrompt()
    {
        return $"[E] {npcName} ile Konuş";
    }

    public bool CanInteract()
    {
        return !hasInteractedToday || 
               (GameManager.Instance != null && 
                GameManager.Instance.CurrentFractureStage >= FractureStage.Noticeable);
    }

    // ==================== Dialogue ====================

    private DialogueLine[] GetCurrentDialogue()
    {
        if (dayDialogues == null || GameManager.Instance == null) return null;

        int day = GameManager.Instance.CurrentDay;

        foreach (var dd in dayDialogues)
        {
            if (dd.Day == day)
            {
                return dd.Lines;
            }
        }

        return null;
    }

    // ==================== Anomaly Behaviors ====================

    private void LookAtPlayer()
    {
        Player player = FindFirstObjectByType<Player>();
        if (player == null) return;

        // NPC sürekli oyuncuya bakıyor (creepy)
        Vector2 direction = (player.transform.position - transform.position).normalized;
        
        // Sprite flip
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = direction.x < 0;
        }
    }

    private void OnDayChanged(int newDay)
    {
        hasInteractedToday = false;
    }

    private void OnFractureStageChanged(FractureStage stage)
    {
        // Kırılma aşamasına göre NPC rengi değişir
        if (spriteRenderer != null)
        {
            switch (stage)
            {
                case FractureStage.Subtle:
                    spriteRenderer.color = Color.white;
                    break;
                case FractureStage.Noticeable:
                    spriteRenderer.color = new Color(0.9f, 0.9f, 0.95f); // Hafif soğuk ton
                    break;
                case FractureStage.Breaking:
                    spriteRenderer.color = new Color(0.8f, 0.7f, 0.9f);  // Mor-ish, garip
                    break;
            }
        }
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnDayChanged -= OnDayChanged;
            GameManager.Instance.OnFractureStageChanged -= OnFractureStageChanged;
        }
    }
}

/// <summary>
/// NPC'nin belirli bir gün için diyalog satırları.
/// </summary>
[System.Serializable]
public class NPCDayDialogue
{
    public int Day;
    public DialogueLine[] Lines;
}
