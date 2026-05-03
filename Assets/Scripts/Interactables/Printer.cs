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
    [SerializeField] [Range(0f, 1f)] private float printSoundVol = 0.5f;

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

        // Yazıcı sesi çal
        SFXManager.Play(printSound, transform.position, printSoundVol);

        // Mini-game varsa oyna, yoksa direkt ver
        if (PrinterMiniGame.Instance != null)
        {
            PrinterMiniGame.Instance.StartGame((success) =>
            {
                FinishPrint(player, success);
            });
        }
        else
        {
            FinishPrint(player, true);
        }
    }

    private void FinishPrint(Player player, bool success)
    {
        isUsed = true;

        // İkonu gizle
        HeadIcon icon = GetComponent<HeadIcon>();
        if (icon != null) icon.OnInteracted();

        player.AddItem(itemId);

        Debug.Log("[Printer] Evrak alındı.");

        // Diyalog
        int day = GameManager.Instance != null ? GameManager.Instance.CurrentDay : 1;
        string msg = day >= 3
            ? "*çırr... çırr...* Kağıtta garip şeyler yazıyor..."
            : "*çırr çırr* Evrak hazır.";

        DialogueSystem.Instance?.StartDialogue(new DialogueLine[]
        {
            new DialogueLine("Yazıcı", msg)
        });
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
