using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ArrayDefinitionSlot : MonoBehaviour {
    [SerializeField] private RectTransform contentRoot;
    [SerializeField] private Button addButton;
    [SerializeField] private RectTransform openingBracket;
    [SerializeField] private RectTransform closingBracket;

    private readonly List<GameObject> itemSlots = new List<GameObject>();
    private readonly List<RectTransform> commaTokens = new List<RectTransform>();
    private const float ItemMinWidth = 50f;
    private const float ItemMaxWidth = 480f;

    public void Initialize() {
        EnsureMainLayout();
        EnsureContentRoot();

        openingBracket = CreateStaticText("[", contentRoot);

        AddItemSlot();
        AddItemSlot();
        AddAddButton();

        closingBracket = CreateStaticText("]", contentRoot);
        RebuildInlineFlow();
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
        GameObject root = new GameObject("ItemSlot", typeof(RectTransform), typeof(Image), typeof(TMP_InputField), typeof(LayoutElement));
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.SetParent(contentRoot, false);

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

        // Agregar handler para bloques flatText
        FlatTextInputHandler flatTextHandler = root.AddComponent<FlatTextInputHandler>();

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
        CodeConsoleTypography.Apply(text, 32f, TextAlignmentOptions.Center);
        text.color = Color.white;

        GameObject phGo = new GameObject("Placeholder", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform phRect = phGo.GetComponent<RectTransform>();
        phRect.SetParent(rect, false);
        phRect.anchorMin = new Vector2(0f, 0f);
        phRect.anchorMax = new Vector2(1f, 1f);
        phRect.offsetMin = new Vector2(4f, 1f);
        phRect.offsetMax = new Vector2(-4f, -1f);

        TextMeshProUGUI ph = phGo.GetComponent<TextMeshProUGUI>();
        ph.text = string.Empty;
        CodeConsoleTypography.Apply(ph, 32f, TextAlignmentOptions.Center);
        ph.color = new Color(1f, 1f, 1f, 0.4f);

        input.textComponent = text;
        input.placeholder = ph;
        input.textViewport = rect;

        input.onValueChanged.AddListener(_ => UpdateInputSlotWidth(input, le));
        UpdateInputSlotWidth(input, le);

        itemSlots.Add(root);

        // Mantener orden de comas/boton/corchete al editar anchos.
        input.onValueChanged.AddListener(_ => {
            RebuildInlineFlow();
            EnsureTrailingOrder();
        });
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
        CodeConsoleTypography.Apply(txt, 29f, TextAlignmentOptions.Center);
        txt.color = Color.white;
    }

    private void OnAddClicked() {
        AddItemSlot();
        RebuildInlineFlow();
        
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRoot as RectTransform);
        EnsureTrailingOrder();
        NotifyParentLineHeightChanged();
    }

    private void RebuildInlineFlow() {
        if (contentRoot == null || addButton == null) return;

        for (int i = 0; i < commaTokens.Count; i++) {
            if (commaTokens[i] != null) {
                Destroy(commaTokens[i].gameObject);
            }
        }
        commaTokens.Clear();

        int index = 0;
        if (openingBracket != null) {
            openingBracket.SetSiblingIndex(index++);
        }

        for (int i = 0; i < itemSlots.Count; i++) {
            if (itemSlots[i] == null) continue;

            itemSlots[i].transform.SetSiblingIndex(index++);
            if (i < itemSlots.Count - 1) {
                RectTransform comma = CreateStaticText(",", contentRoot);
                comma.SetSiblingIndex(index++);
                commaTokens.Add(comma);
            }
        }

        if (addButton != null) {
            addButton.transform.SetSiblingIndex(index++);
        }

        if (closingBracket != null) {
            closingBracket.SetSiblingIndex(index);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRoot);
        NotifyParentLineHeightChanged();
    }

    private void EnsureTrailingOrder() {
        LayoutRebuilder.ForceRebuildLayoutImmediate(transform as RectTransform);
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRoot);
    }

    private void NotifyParentLineHeightChanged() {
        CodeLineTemplateRenderer renderer = GetComponentInParent<CodeLineTemplateRenderer>();
        if (renderer != null) {
            renderer.RefreshOwnerLineHeight();
        }
    }

    private void UpdateInputSlotWidth(TMP_InputField input, LayoutElement le) {
        if (input == null || le == null || input.textComponent == null) return;

        string value = string.IsNullOrEmpty(input.text) ? "..." : input.text;
        float w = input.textComponent.GetPreferredValues(value).x;
        le.preferredWidth = Mathf.Clamp(w + 12f, ItemMinWidth, ItemMaxWidth);
    }

    private RectTransform CreateStaticText(string text, Transform parent) {
        GameObject go = new GameObject("Txt", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);

        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        CodeConsoleTypography.Apply(tmp, 32f, TextAlignmentOptions.Center);
        tmp.color = Color.white;

        LayoutElement le = go.GetComponent<LayoutElement>();
        le.preferredWidth = Mathf.Max(12f, tmp.preferredWidth + 1f);
        le.preferredHeight = 24f;

        return rect;
    }
}
