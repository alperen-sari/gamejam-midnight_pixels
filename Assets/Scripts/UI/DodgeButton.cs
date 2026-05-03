using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Fare yaklaşınca buton kaçar. SpamMailMiniGame Gün 2+ anomali efekti.
/// </summary>
public class DodgeButton : MonoBehaviour, IPointerEnterHandler
{
    [HideInInspector] public float dodgeChance = 0.4f;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (Random.value > dodgeChance) return;

        RectTransform parent = transform.parent?.GetComponent<RectTransform>();
        if (parent == null) return;

        // Popup'ı rastgele pozisyona taşı
        RectTransform popupRect = parent.parent?.GetComponent<RectTransform>();
        if (popupRect == null) return;

        float x = Random.Range(0.1f, 0.7f);
        float y = Random.Range(0.1f, 0.75f);
        float w = popupRect.anchorMax.x - popupRect.anchorMin.x;
        float h = popupRect.anchorMax.y - popupRect.anchorMin.y;

        popupRect.anchorMin = new Vector2(x, y);
        popupRect.anchorMax = new Vector2(x + w, y + h);
    }
}
