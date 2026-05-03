using UnityEngine;

/// <summary>
/// Müdür NPC'si. Oyuncu konuşunca kısa diyalog → FinalScene başlar.
/// Müdür objesine ekle + Box Collider 2D (Trigger) + Layer: Interactable
/// </summary>
public class BossNPC : MonoBehaviour, IInteractable
{
    [Header("Diyalog")]
    [SerializeField] private string bossName = "Müdür";

    [Header("Sound")]
    [SerializeField] private AudioClip bossCallSound;         // "Gel buraya" sesi
    [SerializeField] [Range(0f, 1f)] private float callSoundVol = 0.5f;

    private bool hasStartedFinal = false;

    public void Interact(Player player)
    {
        if (!CanInteract()) return;
        hasStartedFinal = true;

        // Ses
        if (bossCallSound != null)
            SFXManager.Play(bossCallSound, transform.position, callSoundVol);

        // Güvene göre giriş diyaloğu
        float trust = GameManager.Instance != null ? GameManager.Instance.BossTrust : 50f;
        bool isObedient = trust >= 50f;

        DialogueLine[] lines;

        if (isObedient)
        {
            lines = new DialogueLine[]
            {
                new DialogueLine(bossName, "Otur."),
                new DialogueLine(bossName, "Üç gündür seni izliyordum."),
                new DialogueLine(bossName, "Mükemmel bir çalışan... Tam istediğimiz gibi."),
                new DialogueLine(bossName, "Sana bir teklifim var.")
            };
        }
        else
        {
            lines = new DialogueLine[]
            {
                new DialogueLine(bossName, "Otur dedim."),
                new DialogueLine(bossName, "Üç gündür seni izliyordum."),
                new DialogueLine(bossName, "Kuralları çiğnedin. Düzeni bozdun."),
                new DialogueLine(bossName, "Ama sana bir şans daha veriyorum.")
            };
        }

        // Diyalog bittikten sonra FinalScene başlat
        if (DialogueSystem.Instance != null)
        {
            DialogueSystem.Instance.StartDialogue(lines, () =>
            {
                // Diyalog bitti → Final!
                if (FinalScene.Instance != null)
                {
                    FinalScene.Instance.StartFinal();
                }
            });
        }
        else
        {
            // DialogueSystem yoksa direkt başlat
            if (FinalScene.Instance != null)
                FinalScene.Instance.StartFinal();
        }
    }

    public string GetInteractionPrompt()
    {
        return $"[E] {bossName} ile Konuş";
    }

    public bool CanInteract()
    {
        return !hasStartedFinal;
    }
}
