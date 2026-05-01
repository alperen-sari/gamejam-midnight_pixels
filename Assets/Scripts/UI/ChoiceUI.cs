using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

/// <summary>
/// Seçim menüsü. Etkileşimli objelerde seçenek sunar.
/// Kendi Canvas/Panel/Butonlarını otomatik oluşturur — elle kurulum gerektirmez.
/// 
/// Sahneye boş bir GameObject ekle, bu scripti ata. O kadar.
/// </summary>
public class ChoiceUI : MonoBehaviour
{
    public static ChoiceUI Instance { get; private set; }

    [Header("Style")]
    [SerializeField] private int fontSize = 26;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color selectedColor = new Color(1f, 0.85f, 0.2f); // Sarı
    [SerializeField] private Color bgColor = new Color(0.1f, 0.1f, 0.15f, 0.9f);
    [SerializeField] private Color buttonBgColor = new Color(0.2f, 0.2f, 0.25f, 0.95f);
    [SerializeField] private Color buttonSelectedBgColor = new Color(0.3f, 0.25f, 0.1f, 0.95f);

    // Runtime oluşturulan UI elemanları
    private Canvas canvas;
    private GameObject panelObj;
    private List<GameObject> buttonObjects = new List<GameObject>();
    private List<Image> buttonBackgrounds = new List<Image>();
    private List<TextMeshProUGUI> buttonTexts = new List<TextMeshProUGUI>();
    private List<TextMeshProUGUI> keyTexts = new List<TextMeshProUGUI>();
    private List<Action> callbacks = new List<Action>();

    private bool isOpen = false;
    private int selectedIndex = 0;

    // Seçim tuşları
    private readonly KeyCode[] choiceKeys = { KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4 };
    private readonly string[] choiceKeyLabels = { "1", "2", "3", "4" };

    public bool IsOpen => isOpen;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        CreateUI();
    }

    // ==================== UI Oluşturma ====================

    private void CreateUI()
    {
        // Canvas
        GameObject canvasObj = new GameObject("ChoiceCanvas");
        canvasObj.transform.SetParent(transform);
        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200; // Prompt'un üstünde

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<GraphicRaycaster>();

        // Ana panel (ekranın alt-ortasında)
        panelObj = new GameObject("ChoicePanel");
        panelObj.transform.SetParent(canvasObj.transform, false);

        Image panelBg = panelObj.AddComponent<Image>();
        panelBg.color = bgColor;

        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.3f);
        panelRect.anchorMax = new Vector2(0.5f, 0.3f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;

        // Vertical Layout
        VerticalLayoutGroup vlg = panelObj.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 8;
        vlg.padding = new RectOffset(16, 16, 16, 16);
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        ContentSizeFitter panelFitter = panelObj.AddComponent<ContentSizeFitter>();
        panelFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        panelFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        panelObj.SetActive(false);
    }

    /// <summary>
    /// Tek bir buton satırı oluşturur: [1] Kahveyi Al ve İç
    /// </summary>
    private void CreateButton(int index, string text)
    {
        // Buton arka planı
        GameObject btnObj = new GameObject($"Choice_{index}");
        btnObj.transform.SetParent(panelObj.transform, false);

        Image btnBg = btnObj.AddComponent<Image>();
        btnBg.color = buttonBgColor;
        buttonBackgrounds.Add(btnBg);

        // Horizontal layout: [tuş] + metin
        HorizontalLayoutGroup hlg = btnObj.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 12;
        hlg.padding = new RectOffset(14, 14, 8, 8);
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = false;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        // Tuş göstergesi: [1]
        GameObject keyObj = new GameObject("Key");
        keyObj.transform.SetParent(btnObj.transform, false);
        TextMeshProUGUI keyTmp = keyObj.AddComponent<TextMeshProUGUI>();
        keyTmp.text = $"[{choiceKeyLabels[index]}]";
        keyTmp.fontSize = fontSize;
        keyTmp.color = selectedColor;
        keyTmp.fontStyle = FontStyles.Bold;
        keyTmp.alignment = TextAlignmentOptions.MidlineLeft;

        RectTransform keyRect = keyObj.GetComponent<RectTransform>();
        keyRect.sizeDelta = new Vector2(50, 40);
        keyTexts.Add(keyTmp);

        // Seçenek metni
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        TextMeshProUGUI btnTmp = textObj.AddComponent<TextMeshProUGUI>();
        btnTmp.text = text;
        btnTmp.fontSize = fontSize;
        btnTmp.color = normalColor;
        btnTmp.alignment = TextAlignmentOptions.MidlineLeft;

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.sizeDelta = new Vector2(350, 40);
        buttonTexts.Add(btnTmp);

        buttonObjects.Add(btnObj);
    }

    // ==================== Menü Kontrolü ====================

    /// <summary>
    /// Seçim menüsünü gösterir.
    /// Kullanım: ChoiceUI.Instance.ShowChoices(("Seçenek A", () => ...), ("Seçenek B", () => ...));
    /// </summary>
    public void ShowChoices(params (string text, Action callback)[] choices)
    {
        if (isOpen) return;

        ClearButtons();
        callbacks.Clear();
        selectedIndex = 0;

        for (int i = 0; i < choices.Length && i < choiceKeys.Length; i++)
        {
            CreateButton(i, choices[i].text);
            callbacks.Add(choices[i].callback);
        }

        panelObj.SetActive(true);
        isOpen = true;
        UpdateVisuals();

        // Oyuncuyu durdur
        Player player = FindFirstObjectByType<Player>();
        if (player != null)
        {
            player.SetCanMove(false);
        }
    }

    void Update()
    {
        if (!isOpen) return;

        // W/Yukarı ile yukarı
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
        {
            selectedIndex = Mathf.Max(0, selectedIndex - 1);
            UpdateVisuals();
        }
        // S/Aşağı ile aşağı
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
        {
            selectedIndex = Mathf.Min(buttonObjects.Count - 1, selectedIndex + 1);
            UpdateVisuals();
        }

        // E veya Enter ile seçili olanı onayla
        if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Return))
        {
            ConfirmChoice(selectedIndex);
            return;
        }

        // Numara tuşlarıyla doğrudan seç (1, 2, 3, 4)
        for (int i = 0; i < callbacks.Count && i < choiceKeys.Length; i++)
        {
            if (Input.GetKeyDown(choiceKeys[i]))
            {
                ConfirmChoice(i);
                return;
            }
        }

        // Escape ile iptal
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Close();
        }
    }

    private void UpdateVisuals()
    {
        for (int i = 0; i < buttonObjects.Count; i++)
        {
            bool isSelected = (i == selectedIndex);

            if (i < buttonBackgrounds.Count)
                buttonBackgrounds[i].color = isSelected ? buttonSelectedBgColor : buttonBgColor;

            if (i < buttonTexts.Count)
                buttonTexts[i].color = isSelected ? selectedColor : normalColor;

            if (i < keyTexts.Count)
                keyTexts[i].color = selectedColor; // Tuş her zaman sarı
        }
    }

    private void ConfirmChoice(int index)
    {
        if (index < 0 || index >= callbacks.Count) return;

        Action callback = callbacks[index];
        Close();
        callback?.Invoke();
    }

    public void Close()
    {
        isOpen = false;
        panelObj.SetActive(false);
        ClearButtons();
        callbacks.Clear();

        // Oyuncuyu serbest bırak
        Player player = FindFirstObjectByType<Player>();
        if (player != null)
        {
            player.SetCanMove(true);
        }
    }

    private void ClearButtons()
    {
        foreach (var btn in buttonObjects)
        {
            if (btn != null) Destroy(btn);
        }
        buttonObjects.Clear();
        buttonBackgrounds.Clear();
        buttonTexts.Clear();
        keyTexts.Clear();
    }
}
