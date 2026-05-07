using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class AssignmentValueSlot : MonoBehaviour, IDropHandler {
    private static readonly Regex MultiSpaceRegex = new Regex("\\s+");
    private const float OperatorMinWidth = 150f;
    private const float OperatorMaxWidth = 640f;
    private const float StringInputMinWidth = 56f;
    private const float StringInputMaxWidth = 640f;
    private const float MathInputMinWidth = 38f;
    private const float MathInputMaxWidth = 320f;

    [SerializeField] private TMP_InputField baseInput;
    [SerializeField] private RectTransform dynamicContentRoot;

    public string CurrentOperator { get; private set; } = string.Empty;

    public void Initialize(TMP_InputField initialInput) {
        baseInput = initialInput;
        SetBaseInputVisible(true);

        if (dynamicContentRoot != null) {
            dynamicContentRoot.gameObject.SetActive(false);
        }
    }

    public bool TrySetOperator(string operatorText) {
        string cleaned = NormalizeOperatorText(operatorText);
        if (string.IsNullOrWhiteSpace(cleaned) || cleaned == "op") return false;

        CurrentOperator = cleaned;
        SetBaseInputVisible(false);

        EnsureContentRoot();
        BuildMathExpression(cleaned);

        LayoutElement le = GetComponent<LayoutElement>();
        if (le != null) {
            le.preferredWidth = Mathf.Clamp(OperatorMinWidth + (cleaned.Length * 8f), OperatorMinWidth, OperatorMaxWidth);
            le.minWidth = 120f;
        }

        return true;
    }

    public bool TrySetDefinition(string definitionBlockId, string definitionText) {
        if (string.IsNullOrWhiteSpace(definitionBlockId)) return false;

        string normalizedId = definitionBlockId.Trim().ToLowerInvariant();
        if (normalizedId == "string") {
            return TrySetStringDefinition();
        }

        if (normalizedId == "array") {
            return TrySetArrayDefinition();
        }

        return false;
    }

    public void OnDrop(PointerEventData eventData) {
        if (eventData.pointerDrag == null) return;

        BloqueCodigo dragged = eventData.pointerDrag.GetComponent<BloqueCodigo>();
        if (dragged == null) return;

        string draggedType = dragged.commandType != null ? dragged.commandType.Trim().ToLowerInvariant() : string.Empty;
        if (draggedType == "mathoperator") {
            TrySetOperator(dragged.codeText);
            return;
        }

        if (draggedType == "definition") {
            TrySetDefinition(dragged.blockId, dragged.codeText);
        }
    }

    private bool TrySetStringDefinition() {
        SetBaseInputVisible(false);
        EnsureContentRoot();
        ClearContentRoot();

        CreateStaticText("\"");
        CreateTextInput(StringInputMinWidth, StringInputMaxWidth, 20f, false);
        CreateStaticText("\"");

        LayoutElement le = GetComponent<LayoutElement>();
        if (le != null) {
            le.preferredWidth = 320f;
            le.minWidth = 120f;
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(dynamicContentRoot);
        return true;
    }

    private bool TrySetArrayDefinition() {
        SetBaseInputVisible(false);
        EnsureContentRoot();
        ClearContentRoot();

        GameObject arrayGo = new GameObject("ArrayDefinition", typeof(RectTransform), typeof(ArrayDefinitionSlot), typeof(LayoutElement));
        RectTransform rect = arrayGo.GetComponent<RectTransform>();
        rect.SetParent(dynamicContentRoot, false);

        ArrayDefinitionSlot arraySlot = arrayGo.GetComponent<ArrayDefinitionSlot>();
        arraySlot.Initialize();

        LayoutElement arrayLe = arrayGo.GetComponent<LayoutElement>();
        arrayLe.minWidth = 130f;
        arrayLe.preferredWidth = 170f;
        arrayLe.preferredHeight = 24f;

        LayoutElement le = GetComponent<LayoutElement>();
        if (le != null) {
            le.preferredWidth = 220f;
            le.minWidth = 160f;
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(dynamicContentRoot);
        return true;
    }

    private void SetBaseInputVisible(bool visible) {
        if (baseInput == null) return;

        baseInput.enabled = visible;
        baseInput.readOnly = !visible;
        baseInput.interactable = visible;

        Image bg = baseInput.GetComponent<Image>();
        if (bg != null) {
            bg.enabled = visible;
        }

        if (baseInput.textComponent != null) {
            baseInput.textComponent.gameObject.SetActive(visible);
        }

        if (baseInput.placeholder != null) {
            ((Component)baseInput.placeholder).gameObject.SetActive(visible);
        }

        LayoutElement le = baseInput.GetComponent<LayoutElement>();
        if (le != null) {
            // El slot SIEMPRE debe participar en layout para no desalinear el '='.
            le.ignoreLayout = false;

            if (visible) {
                le.minWidth = Mathf.Max(le.minWidth, 56f);
                le.preferredWidth = Mathf.Max(le.preferredWidth, 64f);
                le.preferredHeight = Mathf.Max(le.preferredHeight, 30f);
            }
        }

        Outline outline = baseInput.GetComponent<Outline>();
        if (outline != null) {
            outline.enabled = visible;
        }

        if (dynamicContentRoot != null) {
            dynamicContentRoot.gameObject.SetActive(!visible);
        }
    }

    private string NormalizeOperatorText(string source) {
        if (string.IsNullOrWhiteSpace(source)) return "op";

        string cleaned = source.Replace("_", string.Empty);
        cleaned = MultiSpaceRegex.Replace(cleaned, " ").Trim();
        return string.IsNullOrWhiteSpace(cleaned) ? "op" : cleaned;
    }

    private void EnsureContentRoot() {
        if (dynamicContentRoot != null) {
            ConfigureContentRootLayout(dynamicContentRoot);
            return;
        }

        Transform existing = transform.Find("AssignmentContent");
        if (existing != null) {
            dynamicContentRoot = existing as RectTransform;
            ConfigureContentRootLayout(dynamicContentRoot);
            return;
        }

        GameObject root = new GameObject("AssignmentContent", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
        dynamicContentRoot = root.GetComponent<RectTransform>();
        dynamicContentRoot.SetParent(transform, false);
        dynamicContentRoot.anchorMin = new Vector2(0f, 0.5f);
        dynamicContentRoot.anchorMax = new Vector2(0f, 0.5f);
        dynamicContentRoot.pivot = new Vector2(0f, 0.5f);
        dynamicContentRoot.anchoredPosition = new Vector2(4f, 0f);
        dynamicContentRoot.sizeDelta = new Vector2(0f, 24f);
        dynamicContentRoot.gameObject.SetActive(false);

        ConfigureContentRootLayout(dynamicContentRoot);
    }

    private void ConfigureContentRootLayout(RectTransform rootRect) {
        if (rootRect == null) return;

        HorizontalLayoutGroup hlg = rootRect.GetComponent<HorizontalLayoutGroup>();
        if (hlg == null) {
            hlg = rootRect.gameObject.AddComponent<HorizontalLayoutGroup>();
        }

        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        hlg.spacing = 4f;

        ContentSizeFitter fitter = rootRect.GetComponent<ContentSizeFitter>();
        if (fitter == null) {
            fitter = rootRect.gameObject.AddComponent<ContentSizeFitter>();
        }

        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
    }

    private void BuildMathExpression(string opText) {
        if (dynamicContentRoot == null) return;

        ClearContentRoot();
        CreateTextInput(MathInputMinWidth, MathInputMaxWidth, 20f, false);
        CreateStaticText(opText);
        CreateTextInput(MathInputMinWidth, MathInputMaxWidth, 20f, false);

        LayoutRebuilder.ForceRebuildLayoutImmediate(dynamicContentRoot);
    }

    private void ClearContentRoot() {
        if (dynamicContentRoot == null) return;

        for (int i = dynamicContentRoot.childCount - 1; i >= 0; i--) {
            Destroy(dynamicContentRoot.GetChild(i).gameObject);
        }
    }

    private void CreateStaticText(string text) {
        GameObject go = new GameObject("Txt", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(dynamicContentRoot, false);

        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 20f;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;

        LayoutElement le = go.GetComponent<LayoutElement>();
        le.preferredWidth = Mathf.Max(14f, tmp.preferredWidth + 2f);
        le.preferredHeight = 24f;
    }

    private TMP_InputField CreateNumericInput(float minWidth, float maxWidth, float fontSize) {
        return CreateTextInput(minWidth, maxWidth, fontSize, true);
    }

    private TMP_InputField CreateTextInput(float minWidth, float maxWidth, float fontSize, bool numericOnly) {
        GameObject root = new GameObject("Input", typeof(RectTransform), typeof(Image), typeof(TMP_InputField), typeof(LayoutElement));
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.SetParent(dynamicContentRoot, false);

        Image bg = root.GetComponent<Image>();
        bg.color = new Color(1f, 1f, 1f, 0.03f);

        Outline border = root.AddComponent<Outline>();
        border.effectColor = new Color(1f, 1f, 1f, 0.35f);
        border.effectDistance = new Vector2(1f, -1f);
        border.useGraphicAlpha = true;

        LayoutElement le = root.GetComponent<LayoutElement>();
        le.minWidth = minWidth;
        le.preferredWidth = minWidth + 10f;
        le.preferredHeight = 24f;

        TMP_InputField input = root.GetComponent<TMP_InputField>();
        input.lineType = TMP_InputField.LineType.SingleLine;
        input.contentType = numericOnly ? TMP_InputField.ContentType.DecimalNumber : TMP_InputField.ContentType.Standard;

        GameObject textGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.SetParent(rect, false);
        textRect.anchorMin = new Vector2(0f, 0f);
        textRect.anchorMax = new Vector2(1f, 1f);
        textRect.offsetMin = new Vector2(4f, 1f);
        textRect.offsetMax = new Vector2(-4f, -1f);

        TextMeshProUGUI textComp = textGO.GetComponent<TextMeshProUGUI>();
        textComp.text = string.Empty;
        textComp.fontSize = fontSize;
        textComp.color = Color.white;
        textComp.alignment = TextAlignmentOptions.Center;

        GameObject phGO = new GameObject("Placeholder", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform phRect = phGO.GetComponent<RectTransform>();
        phRect.SetParent(rect, false);
        phRect.anchorMin = new Vector2(0f, 0f);
        phRect.anchorMax = new Vector2(1f, 1f);
        phRect.offsetMin = new Vector2(4f, 1f);
        phRect.offsetMax = new Vector2(-4f, -1f);

        TextMeshProUGUI phText = phGO.GetComponent<TextMeshProUGUI>();
        phText.text = string.Empty;
        phText.fontSize = fontSize;
        phText.color = new Color(1f, 1f, 1f, 0.4f);
        phText.alignment = TextAlignmentOptions.Center;

        input.textComponent = textComp;
        input.placeholder = phText;
        input.textViewport = rect;

        input.onValueChanged.AddListener(_ => UpdateInputSlotWidth(input, le, minWidth, maxWidth));
        UpdateInputSlotWidth(input, le, minWidth, maxWidth);

        return input;
    }

    private void UpdateInputSlotWidth(TMP_InputField input, LayoutElement le, float minWidth, float maxWidth) {
        if (input == null || le == null || input.textComponent == null) return;

        string value = string.IsNullOrEmpty(input.text) ? "..." : input.text;
        float textWidth = input.textComponent.GetPreferredValues(value).x;
        le.preferredWidth = Mathf.Clamp(textWidth + 12f, minWidth, maxWidth);
    }
}
