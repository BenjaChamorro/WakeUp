using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CodeLineTemplateRenderer : MonoBehaviour {
    private const string ContainerName = "LineTemplateContainer";
    private static readonly Regex TokenRegex = new Regex("(operador|operator|_)", RegexOptions.IgnoreCase);
    private static readonly Regex IdentifierInvalidCharsRegex = new Regex("[^a-zA-Z_]");
    private const float InlineInputMinWidth = 48f;
    private const float InlineInputMaxWidth = 640f;
    private const float AssignmentKeyMinWidth = 56f;
    private const float AssignmentKeyMaxWidth = 320f;
    private const float AssignmentValueMinWidth = 56f;
    private const float AssignmentValueMaxWidth = 640f;

    private RectTransform containerRect;
    private string currentTemplate;
    private string currentCommandType;
    private string currentBlockId;
    private TMP_FontAsset referenceFont;
    private Material referenceFontMaterial;
    private float referenceFontSize = 32f;
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

    public bool ApplyCombatPrefill(string prefilledBlockSpec) {
        if (containerRect == null || string.IsNullOrWhiteSpace(prefilledBlockSpec)) {
            return false;
        }

        if (!TryParseCombatPrefill(prefilledBlockSpec, out bool lockWholeBlock, out List<PrefillSlotValue> slotValues)) {
            return false;
        }

        BloqueCodigo owner = GetComponent<BloqueCodigo>();
        if (owner != null) {
            owner.SetCombatPresetLocked(lockWholeBlock);
        }

        ApplyPrefillValues(slotValues);
        return true;
    }

    public bool RenderPrefilledCombatLine(string blockId, string prefilledBlockSpec) {
        if (string.IsNullOrWhiteSpace(blockId) || string.IsNullOrWhiteSpace(prefilledBlockSpec)) {
            return false;
        }

        if (!TryParseCombatPrefill(prefilledBlockSpec, out bool lockWholeBlock, out List<PrefillSlotValue> slotValues)) {
            return false;
        }

        string normalizedBlockId = blockId.Trim().ToLowerInvariant();
        TextMeshProUGUI mainText = transform.Find("TextoBloqueCodigo")?.GetComponent<TextMeshProUGUI>();
        CacheReferenceStyle(mainText);

        EnsureContainer();
        ClearContainer();

        if (mainText != null) {
            mainText.gameObject.SetActive(false);
        }

        BuildPrefilledLineLayout(normalizedBlockId);
        ApplyPrefillValues(slotValues);

        BloqueCodigo owner = GetComponent<BloqueCodigo>();
        if (owner != null) {
            owner.SetCombatPresetLocked(true);
        }

        RefreshOwnerLineHeight();
        return true;
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
            CodeConsoleTypography.CaptureDefaultFont(referenceFont);
        }

        if (source.fontSharedMaterial != null) {
            referenceFontMaterial = source.fontSharedMaterial;
        }

        referenceFontSize = source.fontSize > 0f ? source.fontSize : 32f;
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

        CodeConsoleTypography.Apply(target, fontSizeOverride ?? referenceFontSize, alignOverride);
        target.color = referenceColor;
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

    private void ClearContainer() {
        if (containerRect == null) return;

        for (int i = containerRect.childCount - 1; i >= 0; i--) {
            Destroy(containerRect.GetChild(i).gameObject);
        }
    }

    private void BuildPrefilledLineLayout(string blockId) {
        if (containerRect == null) return;

        string lower = blockId != null ? blockId.Trim().ToLowerInvariant() : string.Empty;

        switch (lower) {
            case "equal":
            case "assignment":
                BuildAssignmentPrefillLine();
                break;
            case "if":
                BuildIfPrefillLine();
                break;
            case "print":
                BuildPrintPrefillLine();
                break;
            default:
                BuildFallbackPrefillLine(blockId);
                break;
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(containerRect);
    }

    private void BuildAssignmentPrefillLine() {
        CreateInlineInput(AssignmentKeyMinWidth, AssignmentKeyMaxWidth, true, false);
        CreateStaticLabel(" = ");
        CreateInlineInput(AssignmentValueMinWidth, AssignmentValueMaxWidth, false, false);
    }

    private void BuildIfPrefillLine() {
        CreateStaticLabel("if (");
        CreateInlineInput(InlineInputMinWidth, InlineInputMaxWidth, true, false);
        CreateStaticLabel(" ");
        CreateOperatorSlot();
        CreateStaticLabel(" ");
        CreateInlineInput(InlineInputMinWidth, InlineInputMaxWidth, false, false);
        CreateStaticLabel("):");
    }

    private void BuildPrintPrefillLine() {
        CreateStaticLabel("print(");
        CreateInlineInput(InlineInputMinWidth, InlineInputMaxWidth, false, false);
        CreateStaticLabel(")");
    }

    private void BuildFallbackPrefillLine(string blockId) {
        if (!string.IsNullOrWhiteSpace(blockId)) {
            CreateStaticLabel(blockId);
        } else {
            CreateStaticLabel("line");
        }
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
                CreateInlineInput(AssignmentKeyMinWidth, AssignmentKeyMaxWidth, true, false);
                return;
            }

            if (placeholderIndex == 1) {
                GameObject valueRoot;
                TMP_InputField valueInput = CreateInlineInput(AssignmentValueMinWidth, AssignmentValueMaxWidth, false, true, out valueRoot);
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
        CreateInlineInput(InlineInputMinWidth, InlineInputMaxWidth, false, false);
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

        // Agregar handler para bloques flatText
        FlatTextInputHandler flatTextHandler = root.AddComponent<FlatTextInputHandler>();

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
        OperatorDropSlot slot = root.GetComponent<OperatorDropSlot>();
        if (slot != null) {
            slot.SetLocked(false);
        }
    }

    private void ApplyPrefillValues(List<PrefillSlotValue> slotValues) {
        if (slotValues == null || slotValues.Count == 0) {
            return;
        }

        List<TMP_InputField> inputSlots = new List<TMP_InputField>();
        List<OperatorDropSlot> operatorSlots = new List<OperatorDropSlot>();
        CollectSlotComponents(containerRect, inputSlots, operatorSlots);

        for (int i = 0; i < slotValues.Count; i++) {
            PrefillSlotValue value = slotValues[i];
            if (value == null) continue;

            if (value.Kind == PrefillSlotKind.Input) {
                int index = Mathf.Max(0, value.Index - 1);
                if (index >= inputSlots.Count) continue;

                TMP_InputField input = inputSlots[index];
                if (input == null) continue;

                input.SetTextWithoutNotify(value.Value);
                input.ForceLabelUpdate();

                LayoutElement inputLayout = input.GetComponent<LayoutElement>();
                if (inputLayout != null) {
                    float minWidth = Mathf.Max(48f, inputLayout.minWidth);
                    float maxWidth = Mathf.Max(minWidth, 640f);
                    UpdateInputSlotWidth(input, inputLayout, minWidth, maxWidth);
                }

                if (value.Locked) {
                    input.readOnly = true;
                    input.interactable = false;
                }
            } else if (value.Kind == PrefillSlotKind.Operator) {
                int index = Mathf.Max(0, value.Index - 1);
                if (index >= operatorSlots.Count) continue;

                OperatorDropSlot slot = operatorSlots[index];
                if (slot == null) continue;

                if (!string.IsNullOrWhiteSpace(value.Value)) {
                    slot.TrySetOperator(value.Value);
                }

                if (value.Locked) {
                    slot.SetLocked(true);
                }
            }
        }

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(containerRect);
        RefreshOwnerLineHeight();
    }

    private void CollectSlotComponents(Transform node, List<TMP_InputField> inputs, List<OperatorDropSlot> operators) {
        if (node == null) return;

        TMP_InputField input = node.GetComponent<TMP_InputField>();
        if (input != null) {
            inputs.Add(input);
        }

        OperatorDropSlot operatorSlot = node.GetComponent<OperatorDropSlot>();
        if (operatorSlot != null) {
            operators.Add(operatorSlot);
        }

        for (int i = 0; i < node.childCount; i++) {
            CollectSlotComponents(node.GetChild(i), inputs, operators);
        }
    }

    private bool TryParseCombatPrefill(string prefilledBlockSpec, out bool lockWholeBlock, out List<PrefillSlotValue> slotValues) {
        lockWholeBlock = false;
        slotValues = new List<PrefillSlotValue>();

        string normalized = prefilledBlockSpec.Trim();
        if (string.IsNullOrWhiteSpace(normalized)) {
            return false;
        }

        int separatorIndex = normalized.IndexOf(" - ", System.StringComparison.Ordinal);
        string headerPart;
        string bodyPart;

        if (separatorIndex >= 0) {
            headerPart = normalized.Substring(0, separatorIndex).Trim();
            bodyPart = normalized.Substring(separatorIndex + 3).Trim();
        } else {
            headerPart = normalized;
            bodyPart = string.Empty;
        }

        if (string.IsNullOrWhiteSpace(headerPart)) {
            return false;
        }

        lockWholeBlock = headerPart.EndsWith("*", System.StringComparison.Ordinal);

        if (string.IsNullOrWhiteSpace(bodyPart)) {
            return true;
        }

        if (bodyPart.StartsWith("-")) {
            bodyPart = bodyPart.Substring(1).TrimStart();
        }

        List<string> tokens = SplitPrefillTokens(bodyPart);
        for (int i = 0; i < tokens.Count; i++) {
            string token = tokens[i];
            if (string.IsNullOrWhiteSpace(token)) continue;

            int equalsIndex = token.IndexOf('=');
            string keyPart = equalsIndex >= 0 ? token.Substring(0, equalsIndex).Trim() : token.Trim();
            string valuePart = equalsIndex >= 0 ? token.Substring(equalsIndex + 1).Trim() : string.Empty;

            bool locked = keyPart.EndsWith("*", System.StringComparison.Ordinal);
            keyPart = keyPart.TrimEnd('*').Trim();

            if (string.IsNullOrWhiteSpace(keyPart)) continue;

            if (!TryParseSlotKey(keyPart, out PrefillSlotKind kind, out int index)) {
                continue;
            }

            valuePart = UnwrapLiteralValue(valuePart);

            slotValues.Add(new PrefillSlotValue {
                Kind = kind,
                Index = index,
                Value = valuePart,
                Locked = locked
            });
        }

        return true;
    }

    private List<string> SplitPrefillTokens(string bodyPart) {
        List<string> tokens = new List<string>();
        StringBuilder current = new StringBuilder();
        int braceDepth = 0;

        for (int i = 0; i < bodyPart.Length; i++) {
            char c = bodyPart[i];

            if (char.IsWhiteSpace(c) && braceDepth == 0) {
                if (current.Length > 0) {
                    tokens.Add(current.ToString());
                    current.Length = 0;
                }
                continue;
            }

            if (c == '{') {
                braceDepth++;
            } else if (c == '}' && braceDepth > 0) {
                braceDepth--;
            }

            current.Append(c);
        }

        if (current.Length > 0) {
            tokens.Add(current.ToString());
        }

        return tokens;
    }

    private bool TryParseSlotKey(string keyPart, out PrefillSlotKind kind, out int index) {
        kind = PrefillSlotKind.Input;
        index = 1;

        string lower = keyPart.ToLowerInvariant();
        if (lower.StartsWith("input")) {
            string numericPart = lower.Substring(5);
            if (string.IsNullOrWhiteSpace(numericPart)) {
                return true;
            }

            if (int.TryParse(numericPart, out int parsedIndex) && parsedIndex > 0) {
                index = parsedIndex;
                return true;
            }

            return false;
        }

        if (lower.StartsWith("operator")) {
            kind = PrefillSlotKind.Operator;
            string numericPart = lower.Substring(8);
            if (string.IsNullOrWhiteSpace(numericPart)) {
                return true;
            }

            if (int.TryParse(numericPart, out int parsedIndex) && parsedIndex > 0) {
                index = parsedIndex;
                return true;
            }

            return false;
        }

        return false;
    }

    private string UnwrapLiteralValue(string valuePart) {
        if (string.IsNullOrWhiteSpace(valuePart)) {
            return string.Empty;
        }

        string trimmed = valuePart.Trim();
        if (trimmed.Length >= 2 && trimmed[0] == '{' && trimmed[trimmed.Length - 1] == '}') {
            trimmed = trimmed.Substring(1, trimmed.Length - 2);
        }

        return trimmed;
    }

    private enum PrefillSlotKind {
        Input,
        Operator
    }

    private class PrefillSlotValue {
        public PrefillSlotKind Kind;
        public int Index;
        public string Value;
        public bool Locked;
    }

    private void RemoveContainerIfExists() {
        Transform existing = transform.Find(ContainerName);
        if (existing != null) {
            Destroy(existing.gameObject);
        }
    }
}
