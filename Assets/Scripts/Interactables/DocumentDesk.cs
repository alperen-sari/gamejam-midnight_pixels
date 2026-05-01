using UnityEngine;

/// <summary>
/// Evrak teslim masası. Yazıcıdan alınan evrakı buraya teslim edersin.
/// Evrak atıldıysa (çöp kutusu) → sitemli tepki.
/// Evrak teslim edildiyse → teşekkür.
/// Evrak henüz alınmadıysa → "Evrakları bekliyorum."
/// </summary>
public class DocumentDesk : MonoBehaviour, IInteractable
{
    [Header("Settings")]
    [SerializeField] private string coworkerName = "Mehmet";
    [SerializeField] private string requiredItem = "evrak";
    [SerializeField] private string taskId = "evrak_teslim";

    [Header("Trust Impact")]
    [SerializeField] private float trustGainOnDeliver = 5f;
    [SerializeField] private float trustLossOnTrash = 15f;
    [SerializeField] private float fractureTrashAmount = 12f;

    private bool isDelivered = false;
    private bool hasReacted = false;

    void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnDayChanged += (_) => { isDelivered = false; hasReacted = false; };
        }
    }

    public void Interact(Player player)
    {
        if (!CanInteract()) return;
        if (DialogueSystem.Instance == null) return;

        bool hasItem = player.HasItem(requiredItem);

        // Yazıcı kullanılmış mı kontrol et
        Printer printer = FindFirstObjectByType<Printer>();
        bool printerUsed = printer != null && !printer.CanInteract();

        if (hasItem)
        {
            HandleDelivery(player);
        }
        else if (printerUsed && !isDelivered)
        {
            // Yazıcı kullanılmış, evrak elde yok → çöpe atmış!
            HandleTrashed(player);
        }
        else
        {
            HandleAskForDocument(player);
        }
    }

    public string GetInteractionPrompt()
    {
        Player player = FindFirstObjectByType<Player>();
        if (player != null && player.HasItem(requiredItem))
        {
            return $"[E] {coworkerName}'e Evrak Ver";
        }
        return $"[E] {coworkerName} ile Konuş";
    }

    public bool CanInteract()
    {
        return !isDelivered || !hasReacted;
    }

    // ==================== Durumlar ====================

    private void HandleAskForDocument(Player player)
    {
        int day = GameManager.Instance != null ? GameManager.Instance.CurrentDay : 1;

        DialogueLine[] lines;
        switch (day)
        {
            case 1:
                lines = new DialogueLine[]
                {
                    new DialogueLine(coworkerName, "Selam! Yazıcıdan evraklar gelmiş olmalı."),
                    new DialogueLine(coworkerName, "Alıp getirir misin? Acil lazım.")
                };
                break;
            case 2:
                lines = new DialogueLine[]
                {
                    new DialogueLine(coworkerName, "Evraklar? Yazıcıda bekliyordur."),
                    new DialogueLine(coworkerName, "...Her gün aynı iş, değil mi?")
                };
                break;
            default:
                lines = new DialogueLine[]
                {
                    new DialogueLine(coworkerName, "Evraklar..."),
                    new DialogueLine(coworkerName, "...önemli mi gerçekten?")
                };
                break;
        }

        DialogueSystem.Instance.StartDialogue(lines);
    }

    private void HandleDelivery(Player player)
    {
        if (ChoiceUI.Instance != null)
        {
            ChoiceUI.Instance.ShowChoices(
                ("Evrakı Teslim Et", () => DeliverDocument(player)),
                ("Vazgeç", () => { })
            );
        }
        else
        {
            DeliverDocument(player);
        }
    }

    private void DeliverDocument(Player player)
    {
        player.RemoveItem(requiredItem);
        isDelivered = true;

        int day = GameManager.Instance != null ? GameManager.Instance.CurrentDay : 1;

        DialogueLine[] lines;
        switch (day)
        {
            case 1:
                lines = new DialogueLine[]
                {
                    new DialogueLine(coworkerName, "Süper, teşekkürler!"),
                    new DialogueLine(coworkerName, "Tam zamanında geldi.")
                };
                break;
            case 2:
                lines = new DialogueLine[]
                {
                    new DialogueLine(coworkerName, "Hmm, tamam."),
                    new DialogueLine(coworkerName, "Ne kadar verimli bir çalışansın... tabii ki.")
                };
                break;
            default:
                lines = new DialogueLine[]
                {
                    new DialogueLine(coworkerName, "..."),
                    new DialogueLine(coworkerName, "Aferin. İyi robot.")
                };
                break;
        }

        DialogueSystem.Instance.StartDialogue(lines, () =>
        {
            if (TaskManager.Instance != null)
                TaskManager.Instance.CompleteTask(taskId);
            if (GameManager.Instance != null)
                GameManager.Instance.AddBossTrust(trustGainOnDeliver);
        });
    }

    private void HandleTrashed(Player player)
    {
        hasReacted = true;

        int day = GameManager.Instance != null ? GameManager.Instance.CurrentDay : 1;

        DialogueLine[] lines;
        switch (day)
        {
            case 1:
                lines = new DialogueLine[]
                {
                    new DialogueLine(coworkerName, "Bir dakika... Evraklar nerede?"),
                    new DialogueLine(coworkerName, "Yazıcıdan aldın ama... bana getirmedin?"),
                    new DialogueLine(coworkerName, "Yok artık. ATTIN MI?!"),
                    new DialogueLine(coworkerName, "Müdür duymasın sakın...")
                };
                break;
            case 2:
                lines = new DialogueLine[]
                {
                    new DialogueLine(coworkerName, "EVRAKLARı NAPTIIN?!"),
                    new DialogueLine(coworkerName, "Bu ikinci kez!! Önemli belgelerdi!!"),
                    new DialogueLine(coworkerName, "Müdüre söylemek zorundayım bu sefer.")
                };
                break;
            default:
                lines = new DialogueLine[]
                {
                    new DialogueLine(coworkerName, "...biliyordum."),
                    new DialogueLine(coworkerName, "Zaten hiçbir şeyin anlamı kalmadı."),
                    new DialogueLine(coworkerName, "...belki de kağıtlar haklıydı. Çöpe ait.")
                };
                break;
        }

        DialogueSystem.Instance.StartDialogue(lines, () =>
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ReduceBossTrust(trustLossOnTrash);
                GameManager.Instance.AddFracture(fractureTrashAmount);
            }
        });
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnDayChanged -= (_) => { };
        }
    }
}
