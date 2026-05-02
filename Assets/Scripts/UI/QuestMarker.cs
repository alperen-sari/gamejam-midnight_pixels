using UnityEngine;

/// <summary>
/// Objenin üstünde soru işareti (?) veya ünlem (!) simgesi gösterir.
/// 
/// Question (?) → NPC'ler: her zaman gösterilir (görev verilmeden önce)
/// Exclamation (!) → Objeler: Gün 1'de NPC ile konuşulana kadar gizli
/// 
/// Görev bitince marker otomatik kaybolur.
/// </summary>
public class QuestMarker : MonoBehaviour
{
    [Header("Marker Settings")]
    [SerializeField] private Sprite questionSprite;
    [SerializeField] private Sprite exclamationSprite;
    [SerializeField] private MarkerType markerType = MarkerType.Question;

    [Header("Position")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 0.7f, 0f);
    [SerializeField] private float bobSpeed = 2f;
    [SerializeField] private float bobAmount = 0.15f;

    [Header("Appearance")]
    [SerializeField] private Color markerColor = Color.yellow;
    [SerializeField] private float markerScale = 0.5f;

    private SpriteRenderer markerRenderer;
    private GameObject markerObj;
    private IInteractable parentInteractable;
    private float baseY;

    // Gün 1'de NPC konuşulduktan sonra obje marker'ları açılır
    public static bool QuestGiven { get; set; } = false;

    public enum MarkerType
    {
        Question,
        Exclamation
    }

    void Start()
    {
        parentInteractable = GetComponent<IInteractable>();
        CreateMarker();

        // Gün değiştiğinde marker'ları resetle
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnDayChanged += (_) => QuestGiven = false;
        }
    }

    private void CreateMarker()
    {
        markerObj = new GameObject("QuestMarker");
        markerObj.transform.SetParent(transform);
        markerObj.transform.localPosition = offset;
        markerObj.transform.localScale = Vector3.one * markerScale;

        markerRenderer = markerObj.AddComponent<SpriteRenderer>();
        markerRenderer.sprite = markerType == MarkerType.Question ? questionSprite : exclamationSprite;
        markerRenderer.color = markerColor;
        markerRenderer.sortingOrder = 100;

        baseY = offset.y;
    }

    void Update()
    {
        if (markerObj == null) return;

        bool shouldShow = ShouldShowMarker();
        markerObj.SetActive(shouldShow);

        // Yukarı aşağı sallanma efekti
        if (shouldShow)
        {
            float newY = baseY + Mathf.Sin(Time.time * bobSpeed) * bobAmount;
            markerObj.transform.localPosition = new Vector3(offset.x, newY, offset.z);
        }
    }

    private bool ShouldShowMarker()
    {
        // Görev bittiyse gizle
        if (parentInteractable != null && !parentInteractable.CanInteract())
        {
            return false;
        }

        // Gün 1'de ünlem marker'ları NPC konuşulana kadar gizli
        if (markerType == MarkerType.Exclamation)
        {
            int day = GameManager.Instance != null ? GameManager.Instance.CurrentDay : 1;
            if (day <= 1 && !QuestGiven)
            {
                return false;
            }
        }

        return true;
    }

    public void SetMarkerType(MarkerType type)
    {
        markerType = type;
        if (markerRenderer != null)
        {
            markerRenderer.sprite = type == MarkerType.Question ? questionSprite : exclamationSprite;
        }
    }

    public void SetVisible(bool visible)
    {
        if (markerObj != null)
        {
            markerObj.SetActive(visible);
        }
    }
}
