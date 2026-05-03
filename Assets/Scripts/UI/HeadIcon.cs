using UnityEngine;

/// <summary>
/// NPC veya objenin kafasında ünlem (!) / soru işareti (?) gösterir.
/// NPC'ye veya interactable objeye ekle.
/// 
/// Sprite otomatik oluşturulur (Unity'nin built-in beyaz kare sprite'ı + TextMesh).
/// Inspector'dan iconOffset ile yüksekliği ayarla.
/// </summary>
public class HeadIcon : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private Sprite iconSprite;              // Inspector'dan sprite ata!
    [SerializeField] private string iconText = "!";          // Sprite yoksa metin kullanır
    [SerializeField] private Color iconColor = Color.yellow;
    [SerializeField] private Vector2 iconOffset = new Vector2(0f, 1.2f);
    [SerializeField] private float bobSpeed = 2f;
    [SerializeField] private float bobAmount = 0.1f;
    [SerializeField] private float iconScale = 0.5f;        // Sprite boyutu

    [Header("Visibility")]
    [SerializeField] private bool showOnStart = true;
    [SerializeField] private bool hideAfterInteract = true;

    private GameObject iconObj;
    private bool isVisible = false;

    void Start()
    {
        CreateIcon();

        if (showOnStart)
            Show();
        else
            Hide();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnDayChanged += (_) => Show();
        }
    }

    private void CreateIcon()
    {
        iconObj = new GameObject("HeadIcon");
        iconObj.transform.SetParent(transform);
        iconObj.transform.localPosition = (Vector3)iconOffset;
        iconObj.transform.localScale = Vector3.one * iconScale;

        if (iconSprite != null)
        {
            // Sprite modu — Inspector'dan atanan sprite
            SpriteRenderer sr = iconObj.AddComponent<SpriteRenderer>();
            sr.sprite = iconSprite;
            sr.color = iconColor;
            sr.sortingOrder = 100;
        }
        else
        {
            // TextMesh fallback — sprite yoksa "!" yazar
            TextMesh tm = iconObj.AddComponent<TextMesh>();
            tm.text = iconText;
            tm.fontSize = 32;
            tm.characterSize = 0.5f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = iconColor;
            tm.fontStyle = FontStyle.Bold;
            MeshRenderer mr = iconObj.GetComponent<MeshRenderer>();
            if (mr != null) mr.sortingOrder = 100;
        }
    }

    void Update()
    {
        if (!isVisible || iconObj == null) return;

        // Yukarı-aşağı sallanma efekti
        Vector3 basePos = (Vector3)iconOffset;
        float bob = Mathf.Sin(Time.time * bobSpeed) * bobAmount;
        iconObj.transform.localPosition = basePos + new Vector3(0f, bob, 0f);
    }

    public void Show()
    {
        isVisible = true;
        if (iconObj != null) iconObj.SetActive(true);
    }

    public void Hide()
    {
        isVisible = false;
        if (iconObj != null) iconObj.SetActive(false);
    }

    /// <summary>
    /// Etkileşim sonrası çağır — hideAfterInteract açıksa gizler.
    /// </summary>
    public void OnInteracted()
    {
        if (hideAfterInteract) Hide();
    }

    /// <summary>
    /// İkon metnini değiştir (örn: "!" → "?" → "✓")
    /// </summary>
    public void SetIcon(string text, Color color)
    {
        iconText = text;
        iconColor = color;
        if (textMesh != null)
        {
            textMesh.text = text;
            textMesh.color = color;
        }
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnDayChanged -= (_) => Show();
        }
    }
}
