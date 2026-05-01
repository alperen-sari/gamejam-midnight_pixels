using UnityEngine;

/// <summary>
/// Oyuncunun etkileşime girebileceği tüm objeler bu interface'i kullanır.
/// Bilgisayar, yazıcı, kahve makinesi, çöp kutusu vs.
/// </summary>
public interface IInteractable
{
    /// <summary>
    /// Etkileşim gerçekleştiğinde çağrılır.
    /// </summary>
    void Interact(Player player);

    /// <summary>
    /// Etkileşim için UI'da gösterilecek metin. Örn: "[E] Bilgisayarı Kullan"
    /// </summary>
    string GetInteractionPrompt();

    /// <summary>
    /// Bu objeyle etkileşim şu an mümkün mü?
    /// </summary>
    bool CanInteract();
}
