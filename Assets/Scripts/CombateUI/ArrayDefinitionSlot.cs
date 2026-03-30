using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ArrayDefinitionSlot : MonoBehaviour {
    [SerializeField] private RectTransform contentRoot;
    [SerializeField] private Button addButton;

    private readonly List<GameObject> itemSlots = new List<GameObject>();
    private const float ItemMinWidth = 50f;
    private const float ItemMaxWidth = 120f;

    public void Initialize() {
        EnsureMainLayout();
        EnsureContentRoot();

        CreateStaticText("[", transform);

        AddItemSlot();
        AddItemSlot();
        AddAddButton();

        CreateStaticText("]", transform);
        EnsureTrailingOrder();
    }

    private void EnsureMainLayout() {
        HorizontalLayoutGroup hlg = GetComponent<HorizontalLayoutGroup>();
        if (hlg == null) {
            hlg = gameObject.AddComponent<HorizontalLayoutGroup>();
        }

        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        hlg.spacing = 3f;

        ContentSizeFitter fitter = GetComponent<ContentSizeFitter>();
        if (fitter == null) {
            fitter = gameObject.AddComponent<ContentSizeFitter>();
        }

        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    private void EnsureContentRoot() {
        if (contentRoot != null) return;

        Transform existing = transform.Find("ArrayContent");
        if (existing != null) {
            contentRoot = existing as RectTransform;
            ConfigureContentRootLayout(contentRoot);
            return;
        }

        GameObject go = new GameObject("ArrayContent", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
        contentRoot = go.GetComponent<RectTransform>();
        contentRoot.SetParent(transform, false);
        contentRoot.anchorMin = new Vector2(0f, 0.5f);
        contentRoot.anchorMax = new Vector2(0f, 0.5f);
        contentRoot.pivot = new Vector2(0f, 0.5f);
        contentRoot.anchoredPosition = Vector2.zero;

        ConfigureContentRootLayout(contentRoot);
    }

    private void ConfigureContentRootLayout(RectTransform rect) {
        if (rect == null) return;

        HorizontalLayoutGroup hlg = rect.GetComponent<HorizontalLayoutGroup>();
        if (hlg == null) {
            hlg = rect.gameObject.AddComponent<HorizontalLayoutGroup>();
        }

        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        hlg.spacing = 3f;

        ContentSizeFitter fitter = rect.GetComponent<ContentSizeFitter>();
        if (fitter == null) {
            fitter = rect.gameObject.AddComponent<ContentSizeFitter>();
        }

        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
    }

    private void AddItemSlot() {
        if (itemSlots.Count > 0) {
            CreateStaticText(",", contentRoot);
        }

        GameObject root = new GameObject("ItemSlot", typeof(RectTransform), typeof(Image), typeof(TMP_InputField), typeof(LayoutElement));
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.SetParent(contentRoot, false);
        SetBeforeAddButton(rect);

        Image bg = root.GetComponent<Image>();
        bg.color = new Color(1f, 1f, 1f, 0.03f);

        Outline outline = root.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 1f, 1f, 0.35f);
        outline.effectDistance = new Vector2(1f, -1f);
        outline.useGraphicAlpha = true;

        LayoutElement le = root.GetComponent<LayoutElement>();
        le.minWidth = ItemMinWidth;
        le.preferredWidth = ItemMinWidth + 8f;
        le.preferredHeight = 24f;

        TMP_InputField input = root.GetComponent<TMP_InputField>();
        input.lineType = TMP_InputField.LineType.SingleLine;
        input.contentType = TMP_InputField.ContentType.Standard;

        GameObject textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform textRect = textGo.GetComponent<RectTransform>();
        textRect.SetParent(rect, false);
        textRect.anchorMin = new Vector2(0f, 0f);
        textRect.anchorMax = new Vector2(1f, 1f);
        textRect.offsetMin = new Vector2(4f, 1f);
        textRect.offsetMax = new Vector2(-4f, -1f);

        TextMeshProUGUI text = textGo.GetComponent<TextMeshProUGUI>();
        text.text = string.Empty;
        text.fontSize = 20f;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;

        GameObject phGo = new GameObject("Placeholder", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform phRect = phGo.GetComponent<RectTransform>();
        phRect.SetParent(rect, false);
        phRect.anchorMin = new Vector2(0f, 0f);
        phRect.anchorMax = new Vector2(1f, 1f);
        phRect.offsetMin = new Vector2(4f, 1f);
        phRect.offsetMax = new Vector2(-4f, -1f);

        TextMeshProUGUI ph = phGo.GetComponent<TextMeshProUGUI>();
        ph.text = string.Empty;
        ph.fontSize = 20f;
        ph.color = new Color(1f, 1f, 1f, 0.4f);
        ph.alignment = TextAlignmentOptions.Center;

        input.textComponent = text;
        input.placeholder = ph;

        input.onValueChanged.AddListener(_ => UpdateInputSlotWidth(input, le));
        UpdateInputSlotWidth(input, le);

        itemSlots.Add(root);
    }

    private void AddAddButton() {
        GameObject go = new GameObject("AddButton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(contentRoot, false);

        Image img = go.GetComponent<Image>();
        img.color = new Color32(78, 37, 192, 255);

        LayoutElement le = go.GetComponent<LayoutElement>();
        le.preferredWidth = 22f;
        le.preferredHeight = 24f;

        addButton = go.GetComponent<Button>();
        addButton.onClick.AddListener(OnAddClicked);

        GameObject txtGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform txtRect = txtGo.GetComponent<RectTransform>();
        txtRect.SetParent(rect, false);
        txtRect.anchorMin = Vector2.zero;
        txtRect.anchorMax = Vector2.one;
        txtRect.offsetMin = Vector2.zero;
        txtRect.offsetMax = Vector2.zero;

        TextMeshProUGUI txt = txtGo.GetComponent<TextMeshProUGUI>();
        txt.text = "+";
        txt.fontSize = 18f;
        txt.color = Color.white;
        txt.alignment = TextAlignmentOptions.Center;
    }

    private void OnAddClicked() {
        AddItemSlot();
        EnsureTrailingOrder();
    }

    private void SetBeforeAddButton(Transform element) {
        if (element == null || addButton == null) return;
        element.SetSiblingIndex(addButton.transform.GetSiblingIndex());
    }

    private void EnsureTrailingOrder() {
        if (contentRoot != null) {
            contentRoot.SetAsLastSibling();
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(transform as RectTransform);
    }

    private void UpdateInputSlotWidth(TMP_InputField input, LayoutElement le) {
        if (input == null || le == null || input.textComponent == null) return;

        string value = string.IsNullOrEmpty(input.text) ? "..." : input.text;
        float w = input.textComponent.GetPreferredValues(value).x;
        le.preferredWidth = Mathf.Clamp(w + 12f, ItemMinWidth, ItemMaxWidth);
    }

    private void CreateStaticText(string text, Transform parent) {
        GameObject go = new GameObject("Txt", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);

        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 20f;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;

        LayoutElement le = go.GetComponent<LayoutElement>();
        le.preferredWidth = Mathf.Max(12f, tmp.preferredWidth + 1f);
        le.preferredHeight = 24f;
    }
}
