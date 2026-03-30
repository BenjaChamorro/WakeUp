using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CodeLineTemplateRenderer : MonoBehaviour {
    private const string ContainerName = "LineTemplateContainer";
    private static readonly Regex TokenRegex = new Regex("(operador|operator|_)", RegexOptions.IgnoreCase);
    private static readonly Regex IdentifierInvalidCharsRegex = new Regex("[^a-zA-Z_]");

    private RectTransform containerRect;
    private string currentTemplate;
    private string currentCommandType;
    private string currentBlockId;
    private TMP_FontAsset referenceFont;
    private Material referenceFontMaterial;
    private float referenceFontSize = 20f;
    private Color referenceColor = Color.white;

    public void Initialize(string templateText, string commandType, string blockId = "") {
        currentTemplate = templateText;
        currentCommandType = string.IsNullOrWhiteSpace(commandType) ? string.Empty : commandType.Trim().ToLowerInvariant();
        currentBlockId = string.IsNullOrWhiteSpace(blockId) ? string.Empty : blockId.Trim().ToLowerInvariant();

        string normalized = templateText.ToLowerInvariant();
        bool needsTemplate = templateText.Contains("_") || normalized.Contains("operador") || normalized.Contains("operator");
        TextMeshProUGUI mainText = transform.Find("TextoBloqueCodigo")?.GetComponent<TextMeshProUGUI>();

        CacheReferenceStyle(mainText);

        if (!needsTemplate) {
            if (mainText != null) {
                mainText.gameObject.SetActive(true);
                mainText.text = templateText;
            }
            RemoveContainerIfExists();
            return;
        }

        if (mainText != null) {
            mainText.gameObject.SetActive(false);
        }

        EnsureContainer();
        if (IsArrayDefinitionMode()) {
            BuildArrayDefinitionTemplate();
            return;
        }

        RebuildTemplate();
    }

    private bool IsArrayDefinitionMode() {
        return currentCommandType == "definition" && currentBlockId == "array";
    }

    private void BuildArrayDefinitionTemplate() {
        if (containerRect == null) return;

        for (int i = containerRect.childCount - 1; i >= 0; i--) {
            Destroy(containerRect.GetChild(i).gameObject);
        }

        GameObject arrayRoot = new GameObject("ArrayDefinition", typeof(RectTransform), typeof(ArrayDefinitionSlot), typeof(LayoutElement));
        RectTransform arrayRect = arrayRoot.GetComponent<RectTransform>();
        arrayRect.SetParent(containerRect, false);

        LayoutElement le = arrayRoot.GetComponent<LayoutElement>();
        le.minWidth = 120f;
        le.preferredWidth = -1f;
        le.preferredHeight = 30f;
        le.flexibleWidth = 0f;
        le.flexibleHeight = 0f;

        ArrayDefinitionSlot slot = arrayRoot.GetComponent<ArrayDefinitionSlot>();
        slot.Initialize();

        LayoutRebuilder.ForceRebuildLayoutImmediate(containerRect);
        RefreshOwnerLineHeight();
    }

    private void CacheReferenceStyle(TextMeshProUGUI source) {
        if (source == null) return;

        if (source.font != null) {
            referenceFont = source.font;
        }

        if (source.fontSharedMaterial != null) {
            referenceFontMaterial = source.fontSharedMaterial;
        }

        referenceFontSize = source.fontSize > 0f ? source.fontSize : 20f;
        referenceColor = source.color;
    }

    private void ApplyReferenceStyle(TextMeshProUGUI target, float? fontSizeOverride = null, TextAlignmentOptions? alignOverride = null) {
        if (target == null) return;

        if (referenceFont != null) {
            target.font = referenceFont;
        }

        if (referenceFontMaterial != null) {
            target.fontSharedMaterial = referenceFontMaterial;
        }

        target.fontSize = fontSizeOverride ?? referenceFontSize;
        target.color = referenceColor;

        if (alignOverride.HasValue) {
            target.alignment = alignOverride.Value;
        }
    }

    private void EnsureContainer() {
        Transform existing = transform.Find(ContainerName);
        if (existing != null) {
            containerRect = existing as RectTransform;
            ConfigureContainerLayout(containerRect);
            return;
        }

        GameObject go = new GameObject(ContainerName, typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
        containerRect = go.GetComponent<RectTransform>();
        containerRect.SetParent(transform, false);
        ConfigureContainerRect(containerRect);
        ConfigureContainerLayout(containerRect);

        HorizontalLayoutGroup hlg = go.GetComponent<HorizontalLayoutGroup>();
        ContentSizeFitter fitter = go.GetComponent<ContentSizeFitter>();
        if (hlg == null || fitter == null) {
            Debug.LogWarning("CodeLineTemplateRenderer: faltan componentes de layout en LineTemplateContainer.");
        }
    }

    private void ConfigureContainerLayout(RectTransform rect) {
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
        hlg.spacing = 4f;

        ContentSizeFitter fitter = rect.GetComponent<ContentSizeFitter>();
        if (fitter == null) {
            fitter = rect.gameObject.AddComponent<ContentSizeFitter>();
        }
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    private void ConfigureContainerRect(RectTransform rect) {
        if (rect == null) return;

        // Valores iniciales solo para contenedores creados por codigo.
        rect.anchorMin = new Vector2(0f, 0.5f);
        rect.anchorMax = new Vector2(0f, 0.5f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, 0f);
        rect.sizeDelta = new Vector2(0f, 32f);
    }

    private void RebuildTemplate() {
        if (containerRect == null) return;

        for (int i = containerRect.childCount - 1; i >= 0; i--) {
            Destroy(containerRect.GetChild(i).gameObject);
        }

        int cursor = 0;
        int placeholderIndex = 0;
        MatchCollection matches = TokenRegex.Matches(currentTemplate);
        for (int i = 0; i < matches.Count; i++) {
            Match match = matches[i];
            if (match.Index > cursor) {
                string staticText = currentTemplate.Substring(cursor, match.Index - cursor);
                CreateStaticLabel(staticText);
            }

            if (match.Value == "_") {
                CreatePlaceholderForCurrentCommand(placeholderIndex);
                placeholderIndex++;
            } else {
                CreateOperatorSlot();
            }

            cursor = match.Index + match.Length;
        }

        if (cursor < currentTemplate.Length) {
            string tail = currentTemplate.Substring(cursor);
            CreateStaticLabel(tail);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(containerRect);
        RefreshOwnerLineHeight();
    }

    public void RefreshOwnerLineHeight() {
        if (containerRect == null) return;

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(containerRect);

        RectTransform ownerRect = transform as RectTransform;
        if (ownerRect == null) return;

        LayoutElement ownerLayout = GetComponent<LayoutElement>();
        if (ownerLayout == null) {
            ownerLayout = gameObject.AddComponent<LayoutElement>();
        }

        float templateHeight = Mathf.Max(32f, containerRect.rect.height + 4f);
        ownerLayout.minHeight = templateHeight;
        ownerLayout.preferredHeight = templateHeight;
        ownerLayout.flexibleHeight = 0f;

        LayoutRebuilder.ForceRebuildLayoutImmediate(ownerRect);
    }

    private bool IsAssignmentMode() {
        return currentCommandType == "assignment";
    }

    private void CreatePlaceholderForCurrentCommand(int placeholderIndex) {
        if (IsAssignmentMode()) {
            if (placeholderIndex == 0) {
                CreateInlineInput(56f, 190f, true, false);
                return;
            }

            if (placeholderIndex == 1) {
                GameObject valueRoot;
                TMP_InputField valueInput = CreateInlineInput(56f, 190f, false, true, out valueRoot);
                if (valueRoot != null) {
                    AssignmentValueSlot slot = valueRoot.GetComponent<AssignmentValueSlot>();
                    if (slot == null) {
                        slot = valueRoot.AddComponent<AssignmentValueSlot>();
                    }
                    slot.Initialize(valueInput);
                }
                return;
            }
        }

        CreateInlineInput();
    }

    private void CreateStaticLabel(string text) {
        GameObject go = new GameObject("Txt", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(containerRect, false);

        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        ApplyReferenceStyle(tmp, referenceFontSize, TextAlignmentOptions.MidlineLeft);
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Overflow;

        LayoutElement le = go.AddComponent<LayoutElement>();
        le.preferredWidth = Mathf.Max(8f, tmp.preferredWidth + 2f);
        le.preferredHeight = 30f;
    }

    private void CreateInlineInput() {
        CreateInlineInput(48f, 180f, false, false);
    }

    private TMP_InputField CreateInlineInput(float minWidth, float maxWidth, bool identifierOnly, bool numericOnly) {
        GameObject root;
        return CreateInlineInput(minWidth, maxWidth, identifierOnly, numericOnly, out root);
    }

    private TMP_InputField CreateInlineInput(float minWidth, float maxWidth, bool identifierOnly, bool numericOnly, out GameObject root) {
        root = new GameObject("InputSlot", typeof(RectTransform), typeof(Image), typeof(TMP_InputField), typeof(LayoutElement));
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.SetParent(containerRect, false);

        Image img = root.GetComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0.03f);

        Outline border = root.AddComponent<Outline>();
        border.effectColor = new Color(1f, 1f, 1f, 0.35f);
        border.effectDistance = new Vector2(1f, -1f);
        border.useGraphicAlpha = true;

        LayoutElement le = root.GetComponent<LayoutElement>();
        le.minWidth = minWidth;
        le.preferredWidth = Mathf.Max(minWidth, 64f);
        le.preferredHeight = 30f;

        TMP_InputField input = root.GetComponent<TMP_InputField>();

        GameObject textGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.SetParent(rect, false);
        textRect.anchorMin = new Vector2(0f, 0f);
        textRect.anchorMax = new Vector2(1f, 1f);
        textRect.offsetMin = new Vector2(6f, 2f);
        textRect.offsetMax = new Vector2(-6f, -2f);

        TextMeshProUGUI textComp = textGO.GetComponent<TextMeshProUGUI>();
        textComp.text = string.Empty;
        ApplyReferenceStyle(textComp, referenceFontSize, TextAlignmentOptions.MidlineLeft);

        GameObject placeholderGO = new GameObject("Placeholder", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform placeholderRect = placeholderGO.GetComponent<RectTransform>();
        placeholderRect.SetParent(rect, false);
        placeholderRect.anchorMin = new Vector2(0f, 0f);
        placeholderRect.anchorMax = new Vector2(1f, 1f);
        placeholderRect.offsetMin = new Vector2(6f, 2f);
        placeholderRect.offsetMax = new Vector2(-6f, -2f);

        TextMeshProUGUI placeholderComp = placeholderGO.GetComponent<TextMeshProUGUI>();
        placeholderComp.text = string.Empty;
        ApplyReferenceStyle(placeholderComp, referenceFontSize, TextAlignmentOptions.MidlineLeft);
        placeholderComp.color = new Color(referenceColor.r, referenceColor.g, referenceColor.b, 0.4f);

        input.textComponent = textComp;
        input.placeholder = placeholderComp;
        input.textViewport = rect;
        input.lineType = TMP_InputField.LineType.SingleLine;

        if (numericOnly) {
            input.contentType = TMP_InputField.ContentType.DecimalNumber;
        }

        if (identifierOnly) {
            input.contentType = TMP_InputField.ContentType.Standard;
            input.onValueChanged.AddListener(v => {
                string sanitized = IdentifierInvalidCharsRegex.Replace(v, string.Empty);
                if (!string.Equals(sanitized, v)) {
                    input.SetTextWithoutNotify(sanitized);
                }
            });
        }

        UpdateInputSlotWidth(input, le, minWidth, maxWidth);
        input.onValueChanged.AddListener(_ => UpdateInputSlotWidth(input, le, minWidth, maxWidth));

        return input;
    }

    private void UpdateInputSlotWidth(TMP_InputField input, LayoutElement le, float minWidth, float maxWidth) {
        if (input == null || le == null || input.textComponent == null) return;

        string value = string.IsNullOrEmpty(input.text) ? "..." : input.text;
        float textWidth = input.textComponent.GetPreferredValues(value).x;
        float target = Mathf.Clamp(textWidth + 14f, minWidth, maxWidth);

        le.preferredWidth = target;
    }

    private void CreateOperatorSlot() {
        GameObject root = new GameObject("OperatorSlot", typeof(RectTransform), typeof(Image), typeof(LayoutElement), typeof(OperatorDropSlot));
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.SetParent(containerRect, false);

        Image img = root.GetComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0.12f);

        LayoutElement le = root.GetComponent<LayoutElement>();
        le.preferredWidth = 64f;
        le.preferredHeight = 30f;

        GameObject textGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.SetParent(rect, false);
        textRect.anchorMin = new Vector2(0f, 0f);
        textRect.anchorMax = new Vector2(1f, 1f);
        textRect.offsetMin = new Vector2(4f, 0f);
        textRect.offsetMax = new Vector2(-4f, 0f);

        TextMeshProUGUI text = textGO.GetComponent<TextMeshProUGUI>();
        text.text = "op";
        ApplyReferenceStyle(text, referenceFontSize, TextAlignmentOptions.Center);

    }

    private void RemoveContainerIfExists() {
        Transform existing = transform.Find(ContainerName);
        if (existing != null) {
            Destroy(existing.gameObject);
        }
    }
}
