using UnityEngine;

/// <summary>
/// Yazıcı etkileşimli objesi.
/// E'ye basınca evrakı alır, envantere "evrak" ekler.
/// </summary>
public class Printer : MonoBehaviour, IInteractable
{
    [Header("Settings")]
    [SerializeField] private string taskId = "evrak_teslim";
    [SerializeField] private string itemId = "evrak";

    [Header("Sound")]
    [SerializeField] private AudioClip printSound;       // Yazıcı sesi

    private bool isUsed = false;

    void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnDayChanged += (_) => isUsed = false;
        }
    }

    public void Interact(Player player)
    {
        if (!CanInteract()) return;

        isUsed = true;
        player.AddItem(itemId);

        // Yazıcı sesi çal
        SFXManager.Play(printSound, transform.position);

        Debug.Log("[Printer] Evrak alındı.");

        // Kısa diyalog
        if (DialogueSystem.Instance != null)
        {
            DialogueSystem.Instance.StartDialogue(new DialogueLine[]
            {
                new DialogueLine("Yazıcı", "*çırr çırr* Evrak hazır.")
            });
        }
    }

    public string GetInteractionPrompt()
    {
        return "[E] Evrakı Al";
    }

    public bool CanInteract()
    {
        return !isUsed;
    }
}
