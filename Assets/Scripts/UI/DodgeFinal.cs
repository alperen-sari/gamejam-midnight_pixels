using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Final sahnesi: YIRT butonu fareyi kaçırır.
/// maxDodges kez kaçtıktan sonra İMZALA'ya dönüşür.
/// </summary>
public class DodgeFinal : MonoBehaviour, IPointerEnterHandler
{
    private RectTransform btnRect;
    private RectTransform containerRect;
    private int dodgesLeft;
    private System.Action onDodgesComplete;

    public int DodgesLeft => dodgesLeft;

    public void Init(RectTransform button, RectTransform container, int maxDodges, System.Action onComplete)
    {
        btnRect = button;
        containerRect = container;
        dodgesLeft = maxDodges;
        onDodgesComplete = onComplete;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (dodgesLeft <= 0) return;

        dodgesLeft--;

        // Rastgele pozisyona kaç
        float newX = Random.Range(0.05f, 0.6f);
        float newY = Random.Range(0.05f, 0.6f);
        float w = btnRect.anchorMax.x - btnRect.anchorMin.x;
        float h = btnRect.anchorMax.y - btnRect.anchorMin.y;

        btnRect.anchorMin = new Vector2(newX, newY);
        btnRect.anchorMax = new Vector2(newX + w, newY + h);

        SFXManager.Play2D(null); // Ses FinalScene'den gelir

        if (dodgesLeft <= 0)
        {
            onDodgesComplete?.Invoke();
        }
    }
}
