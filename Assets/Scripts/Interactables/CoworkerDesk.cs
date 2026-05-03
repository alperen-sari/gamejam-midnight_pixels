using UnityEngine;

/// <summary>
/// Çalışan masası — kahve teslim noktası.
/// 
/// Duruma göre farklı diyaloglar:
/// - Oyuncu kahveyi içtiyse → sitemli "KAHVEMİ??" tepkisi
/// - Oyuncu kahveyi getirdiyse → "Teşekkürler kölem" 
/// - Oyuncu henüz kahve almadıysa → "Kahvemi bekliyorum"
/// 
/// Gün bazlı diyalog varyasyonları var (deneme için sırayla gelir).
/// </summary>
public class CoworkerDesk : MonoBehaviour, IInteractable
{
    [Header("Settings")]
    [SerializeField] private string coworkerName = "Ahmet";
    [SerializeField] private string requiredItem = "kahve";
    [SerializeField] private string taskId = "kahve_al";

    [Header("Trust Impact")]
    [SerializeField] private float trustGainOnDeliver = 5f;      // Kahve teslim → güven artar
    [SerializeField] private float trustLossOnDrink = 15f;       // Kahveyi içtin → güven düşer
    [SerializeField] private float fractureDrinkAmount = 10f;    // Kahveyi içtin → kırılma artar

    private bool isDelivered = false;
    private bool hasReacted = false;
    private bool taskCompleted = false;  // Görev tamamen bitti mi?

    // Deneme için gün simülasyonu (her konuşmada artır)
    [Header("Debug")]
    [SerializeField] private int debugDaySimulation = 1;

    void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnDayChanged += OnDayChanged;
        }
    }

    private void OnDayChanged(int newDay)
    {
        isDelivered = false;
        hasReacted = false;
        taskCompleted = false;
    }

    // ==================== IInteractable ====================

    public void Interact(Player player)
    {
        if (!CanInteract()) return;

        if (DialogueSystem.Instance == null)
        {
            Debug.LogWarning("[CoworkerDesk] DialogueSystem bulunamadi!");
            return;
        }

        // Zaten tepki verdiyse tekrar konusma
        if (hasReacted || isDelivered)
        {
            return;
        }

        bool hasCoffee = player.HasItem(requiredItem);

        if (hasCoffee)
        {
            // Oyuncu kahveyi getirmis
            HandleCoffeeDelivery(player);
        }
        else
        {
            // Kahve makinesi kullanilmis mi?
            CoffeeMaker coffeeMaker = FindFirstObjectByType<CoffeeMaker>();
            bool coffeeMachineUsed = coffeeMaker != null && !coffeeMaker.CanInteract();

            if (coffeeMachineUsed)
            {
                // Makine kullanildi ama elde kahve yok = icmis
                HandleCoffeeDrunk(player);
            }
            else
            {
                // Henuz kahve almamis
                HandleAskForCoffee(player);
            }
        }
    }

    public string GetInteractionPrompt()
    {
        Player player = FindFirstObjectByType<Player>();
        if (player != null && player.HasItem(requiredItem))
        {
            return $"[E] {coworkerName}'e Kahve Ver";
        }
        return $"[E] {coworkerName} ile Konuş";
    }

    public bool CanInteract()
    {
        return !taskCompleted;
    }

    // ==================== Diyalog Durumları ====================

    /// <summary>
    /// Kahve henüz alınmamış — NPC kahve istiyor.
    /// Gün bazlı farklı diyaloglar.
    /// </summary>
    private void HandleAskForCoffee(Player player)
    {
        int day = GameManager.Instance != null ? GameManager.Instance.CurrentDay : debugDaySimulation;

        DialogueLine[] lines;
        switch (day)
        {
            case 1:
                lines = new DialogueLine[]
                {
                    new DialogueLine(coworkerName, "Hey, günaydın!"),
                    new DialogueLine(coworkerName, "Bana bir kahve getirir misin? Sütlü olsun."),
                    new DialogueLine(coworkerName, "Kahve makinesi koridorun sonunda.")
                };
                break;
            case 2:
                lines = new DialogueLine[]
                {
                    new DialogueLine(coworkerName, "Yine sen... Kahvemi getir."),
                    new DialogueLine(coworkerName, "Her gün aynı şey değil mi?"),
                    new DialogueLine(coworkerName, "...Neyse, hadi çabuk ol.")
                };
                break;
            case 3:
            default:
                lines = new DialogueLine[]
                {
                    new DialogueLine(coworkerName, "..."),
                    new DialogueLine(coworkerName, "Kahve. Biliyorsun."),
                    new DialogueLine(coworkerName, "Yoksa... bilmiyor musun?")
                };
                break;
        }

        DialogueSystem.Instance.StartDialogue(lines, () =>
        {
            // NPC konuştu → obje marker'ları artık görünsün
            QuestMarker.QuestGiven = true;
        });
    }

    /// <summary>
    /// Oyuncu kahveyi getirmiş — teslim seçeneği.
    /// </summary>
    private void HandleCoffeeDelivery(Player player)
    {
        int day = GameManager.Instance != null ? GameManager.Instance.CurrentDay : debugDaySimulation;

        if (ChoiceUI.Instance != null)
        {
            ChoiceUI.Instance.ShowChoices(
                ("Kahveyi Teslim Et", () => DeliverCoffee(player, day)),
                ("Vazgeç", () => { })
            );
        }
        else
        {
            DeliverCoffee(player, day);
        }
    }

    private void DeliverCoffee(Player player, int day)
    {
        player.RemoveItem(requiredItem);
        isDelivered = true;

        DialogueLine[] lines;
        switch (day)
        {
            case 1:
                lines = new DialogueLine[]
                {
                    new DialogueLine(coworkerName, "Oh, sonunda! Teşekkürler."),
                    new DialogueLine(coworkerName, "Aferin, iyi çalışan.")
                };
                break;
            case 2:
                lines = new DialogueLine[]
                {
                    new DialogueLine(coworkerName, "Hmm, tamam."),
                    new DialogueLine(coworkerName, "Teşekkürler kölem. Haha, şaka şaka..."),
                    new DialogueLine(coworkerName, "...ya da değil.")
                };
                break;
            case 3:
            default:
                lines = new DialogueLine[]
                {
                    new DialogueLine(coworkerName, "..."),
                    new DialogueLine(coworkerName, "İyi köle."),
                };
                break;
        }

        DialogueSystem.Instance.StartDialogue(lines, () =>
        {
            taskCompleted = true;

            // Teslim onay sesi
            DialogueSystem.Instance?.PlayDeliverySound();

            if (TaskManager.Instance != null)
            {
                TaskManager.Instance.CompleteTask(taskId);
            }
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddBossTrust(trustGainOnDeliver);
            }
        });
    }

    /// <summary>
    /// Oyuncu kahveyi içmiş — sitemli tepki!
    /// Güven düşer, kırılma artar.
    /// </summary>
    private void HandleCoffeeDrunk(Player player)
    {
        hasReacted = true;

        int day = GameManager.Instance != null ? GameManager.Instance.CurrentDay : debugDaySimulation;

        DialogueLine[] lines;
        switch (day)
        {
            case 1:
                lines = new DialogueLine[]
                {
                    new DialogueLine(coworkerName, "Bir dakika..."),
                    new DialogueLine(coworkerName, "O kahve... BENİM kahvemdi??"),
                    new DialogueLine(coworkerName, "İnanamıyorum. Gerçekten içtin mi?!"),
                    new DialogueLine(coworkerName, "...Müdüre söylemeyeceğim. Bu seferlik.")
                };
                break;
            case 2:
                lines = new DialogueLine[]
                {
                    new DialogueLine(coworkerName, "KAHVE NEREDE?!"),
                    new DialogueLine(coworkerName, "Yine mi içtin?! Bu ikinci kez!!"),
                    new DialogueLine(coworkerName, "Müdür bunu duyarsa..."),
                    new DialogueLine(coworkerName, "Benden duymuş olma ama güveni kaybediyorsun.")
                };
                break;
            case 3:
            default:
                lines = new DialogueLine[]
                {
                    new DialogueLine(coworkerName, "..."),
                    new DialogueLine(coworkerName, "Biliyordum."),
                    new DialogueLine(coworkerName, "Zaten hiçbir şeyin önemi yok artık, değil mi?"),
                    new DialogueLine(coworkerName, "...belki de haklısın.")
                };
                break;
        }

        DialogueSystem.Instance.StartDialogue(lines, () =>
        {
            taskCompleted = true;  // Artik tekrar konusulamaz

            if (TaskManager.Instance != null)
            {
                TaskManager.Instance.RebelTask(taskId, fractureDrinkAmount);
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.ReduceBossTrust(trustLossOnDrink);
            }
        });
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnDayChanged -= OnDayChanged;
        }
    }
}
