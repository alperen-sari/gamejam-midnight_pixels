using UnityEngine;

/// <summary>
/// Çöp kutusu. Evrak varsa atabilirsin (rutin kırma).
/// Animasyon yok, direkt çalışır.
/// Güven düşer, kırılma artar.
/// </summary>
public class TrashCan : MonoBehaviour, IInteractable
{
    [Header("Settings")]
    [SerializeField] private string targetItem = "evrak";       // Atılacak eşya

    [Header("Impact")]
    [SerializeField] private float trustLoss = 15f;
    [SerializeField] private float fractureGain = 12f;

    [Header("Sound")]
    [SerializeField] private AudioClip trashSound;       // Buruşturma sesi

    public void Interact(Player player)
    {
        if (!CanInteract()) return;

        if (player.HasItem(targetItem))
        {
            // Seçenek sun
            if (ChoiceUI.Instance != null)
            {
                ChoiceUI.Instance.ShowChoices(
                    ("Evrakı Çöpe At", () => ThrowAway(player)),
                    ("Vazgeç", () => { })
                );
            }
            else
            {
                ThrowAway(player);
            }
        }
        else
        {
            Debug.Log("[TrashCan] Atacak bir şeyin yok.");
        }
    }

    public string GetInteractionPrompt()
    {
        Player player = FindFirstObjectByType<Player>();
        if (player != null && player.HasItem(targetItem))
        {
            return "[E] Çöpe At";
        }
        return ""; // Elinde eşya yoksa prompt gösterme
    }

    public bool CanInteract()
    {
        // Gün 1'de çöp kutusu kilitli (rutin kıramazsın)
        int day = GameManager.Instance != null ? GameManager.Instance.CurrentDay : 1;
        if (day <= 1) return false;

        Player player = FindFirstObjectByType<Player>();
        return player != null && player.HasItem(targetItem);
    }

    private void ThrowAway(Player player)
    {
        player.RemoveItem(targetItem);

        // Buruşturma sesi
        SFXManager.Play(trashSound, transform.position);

        Debug.Log($"[TrashCan] {targetItem} çöpe atıldı!");

        // Güven düşür + kırılma artır
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ReduceBossTrust(trustLoss);
            GameManager.Instance.AddFracture(fractureGain);
        }

        // Diyalog
        if (DialogueSystem.Instance != null)
        {
            DialogueSystem.Instance.StartDialogue(new DialogueLine[]
            {
                new DialogueLine("", "Evrakı buruşturup çöpe attın."),
                new DialogueLine("", "...İçinden bir rahatlama hissettin.")
            });
        }
    }
}
