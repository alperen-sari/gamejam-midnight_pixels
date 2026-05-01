using UnityEngine;

/// <summary>
/// Kahve makinesi etkileşimli objesi.
/// E'ye basınca 2 seçenek sunar:
///   1) Kahveyi Al ve Götür → sadece görevi tamamlar
///   2) Kahveyi Al ve İç   → drinking_coffee animasyonu oynatılır
/// </summary>
public class CoffeeMaker : MonoBehaviour, IInteractable
{
    [Header("Settings")]
    [SerializeField] private string taskId = "kahve_al";     // TaskManager'daki görev ID'si
    [SerializeField] private float drinkDuration = 2f;       // İçme animasyonu süresi

    [Header("Rebellion")]
    [SerializeField] private bool isDrinkingRebellion = true;   // İçmek rutin kırma mı?
    [SerializeField] private float rebellionFracture = 10f;     // Kırılma miktarı

    private bool isUsed = false; // Bu gün kullanıldı mı?

    void Start()
    {
        // Gün değiştiğinde resetle
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnDayChanged += (_) => isUsed = false;
        }
    }

    // ==================== IInteractable ====================

    public void Interact(Player player)
    {
        if (!CanInteract()) return;

        // Seçim menüsünü göster
        if (ChoiceUI.Instance != null)
        {
            ChoiceUI.Instance.ShowChoices(
                ("Kahveyi Al ve Götür", () => TakeCoffee(player)),
                ("Kahveyi Al ve İç", () => DrinkCoffee(player))
            );
        }
        else
        {
            // ChoiceUI yoksa direkt al
            Debug.LogWarning("[CoffeeMaker] ChoiceUI bulunamadı! Kahve direkt alındı.");
            TakeCoffee(player);
        }
    }

    public string GetInteractionPrompt()
    {
        return "[E] Kahve Makinesi";
    }

    public bool CanInteract()
    {
        return !isUsed;
    }

    // ==================== Seçenekler ====================

    /// <summary>
    /// Kahveyi al ve götür — rutin davranış.
    /// Şu anlık sadece görevi tamamlar.
    /// </summary>
    private void TakeCoffee(Player player)
    {
        isUsed = true;

        // Kahveyi oyuncunun envanterine ekle
        player.AddItem("kahve");

        Debug.Log("[CoffeeMaker] Kahve alındı, teslim edilmeyi bekliyor.");
    }

    /// <summary>
    /// Kahveyi al ve iç — drinking_coffee animasyonu oynar.
    /// Bu bir rutin kırma eylemi olabilir (isDrinkingRebellion).
    /// </summary>
    private void DrinkCoffee(Player player)
    {
        isUsed = true;

        Debug.Log("[CoffeeMaker] Kahve içiliyor...");

        // Oyuncuyu durdur ve animasyonu oynat
        player.SetCanMove(false);
        player.PlayAction("drinking_coffee", drinkDuration, () =>
        {
            player.SetCanMove(true);

            Debug.Log("[CoffeeMaker] Kahve içildi! (Başkasının kahvesiydi...)");

            // Kırılma + güven etkisi
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddFracture(rebellionFracture);
                GameManager.Instance.ReduceBossTrust(10f);
            }
        });
    }
}
