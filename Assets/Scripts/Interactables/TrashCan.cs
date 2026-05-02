using UnityEngine;

/// <summary>
/// Çöp kutusu. Evrak varsa buruşturma mini-game'i tetikler.
/// Mini-game başarılı → çöpe gider, güven düşer, kırılma artar.
/// Gün 1'de kilitli (rutin kırılamaz).
/// </summary>
public class TrashCan : MonoBehaviour, IInteractable
{
    [Header("Settings")]
    [SerializeField] private string targetItem = "evrak";

    [Header("Impact")]
    [SerializeField] private float trustLoss = 15f;
    [SerializeField] private float fractureGain = 12f;

    [Header("Sound")]
    [SerializeField] private AudioClip trashSound;
    [SerializeField] [Range(0f, 1f)] private float trashSoundVol = 0.6f;

    public void Interact(Player player)
    {
        if (!CanInteract()) return;

        if (player.HasItem(targetItem))
        {
            if (ChoiceUI.Instance != null)
            {
                ChoiceUI.Instance.ShowChoices(
                    ("Evrakı Buruştur ve At", () => StartCrumple(player)),
                    ("Vazgeç", () => { })
                );
            }
            else
            {
                StartCrumple(player);
            }
        }
    }

    public string GetInteractionPrompt()
    {
        Player player = FindFirstObjectByType<Player>();
        if (player != null && player.HasItem(targetItem))
        {
            return "[E] Çöpe At";
        }
        return "";
    }

    public bool CanInteract()
    {
        int day = GameManager.Instance != null ? GameManager.Instance.CurrentDay : 1;
        if (day <= 1) return false;

        Player player = FindFirstObjectByType<Player>();
        return player != null && player.HasItem(targetItem);
    }

    private void StartCrumple(Player player)
    {
        // CrumpleMiniGame varsa mini-game başlat
        if (CrumpleMiniGame.Instance != null)
        {
            CrumpleMiniGame.Instance.StartGame((success) =>
            {
                if (success)
                {
                    FinishThrowAway(player);
                }
                else
                {
                    // İptal — hiçbir şey olmuyor
                    Debug.Log("[TrashCan] Buruşturma iptal edildi.");
                }
            });
        }
        else
        {
            // Mini-game yoksa direkt at
            FinishThrowAway(player);
        }
    }

    private void FinishThrowAway(Player player)
    {
        player.RemoveItem(targetItem);
        SFXManager.Play(trashSound, transform.position, trashSoundVol);

        Debug.Log($"[TrashCan] {targetItem} çöpe atıldı!");

        if (GameManager.Instance != null)
        {
            GameManager.Instance.ReduceBossTrust(trustLoss);
            GameManager.Instance.AddFracture(fractureGain);
        }

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
