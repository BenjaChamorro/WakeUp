using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Text.RegularExpressions;

public class OperatorDropSlot : MonoBehaviour, IDropHandler {
    private static readonly Regex PlaceholderRegex = new Regex("(_)");

    [SerializeField] private TextMeshProUGUI slotText;
    [SerializeField] private RectTransform dynamicContentRoot;

    public string CurrentOperator { get; private set; } = string.Empty;

    void Awake() {
        if (slotText == null) {
            slotText = GetComponentInChildren<TextMeshProUGUI>();
        }

        if (slotText != null && string.IsNullOrWhiteSpace(slotText.text)) {
            slotText.text = "op";
        }
    }

    public bool TrySetOperator(string operatorText) {
        if (string.IsNullOrWhiteSpace(operatorText)) return false;

        CurrentOperator = operatorText;
        EnsureContentRoot();
        BuildOperatorContent(operatorText);

        if (slotText != null) {
            slotText.gameObject.SetActive(false);
        }

        LayoutElement le = GetComponent<LayoutElement>();
        if (le != null) {
            // Dar espacio adicional cuando se inserta operador con placeholders.
            if (operatorText.Contains("_")) {
                le.preferredWidth = 188f;
            } else {
                le.preferredWidth = Mathf.Max(64f, 26f + (operatorText.Length * 14f));
            }
        }

        return true;
    }

    private void EnsureContentRoot() {
        if (dynamicContentRoot != null) {
            ConfigureContentRootLayout(dynamicContentRoot);
            return;
        }

        Transform existing = transform.Find("OperatorContent");
        if (existing != null) {
            dynamicContentRoot = existing as RectTransform;
            ConfigureContentRootLayout(dynamicContentRoot);
            return;
        }

        GameObject root = new GameObject("OperatorContent", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
        dynamicContentRoot = root.GetComponent<RectTransform>();
        dynamicContentRoot.SetParent(transform, false);
        dynamicContentRoot.anchorMin = new Vector2(0f, 0f);
        dynamicContentRoot.anchorMax = new Vector2(1f, 1f);
        dynamicContentRoot.offsetMin = new Vector2(4f, 2f);
        dynamicContentRoot.offsetMax = new Vector2(-4f, -2f);

        ConfigureContentRootLayout(dynamicContentRoot);
    }

    private void ConfigureContentRootLayout(RectTransform rootRect) {
        if (rootRect == null) return;

        HorizontalLayoutGroup hlg = rootRect.GetComponent<HorizontalLayoutGroup>();
        if (hlg == null) {
            hlg = rootRect.gameObject.AddComponent<HorizontalLayoutGroup>();
        }
        hlg.childAlignment = TextAnchor.MiddleCenter;
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

    private void BuildOperatorContent(string operatorTemplate) {
        if (dynamicContentRoot == null) return;

        for (int i = dynamicContentRoot.childCount - 1; i >= 0; i--) {
            Destroy(dynamicContentRoot.GetChild(i).gameObject);
        }

        int cursor = 0;
        MatchCollection matches = PlaceholderRegex.Matches(operatorTemplate);
        for (int i = 0; i < matches.Count; i++) {
            Match match = matches[i];

            if (match.Index > cursor) {
                string staticPart = operatorTemplate.Substring(cursor, match.Index - cursor);
                CreateStaticPart(staticPart);
            }

            CreateInlineInput();
            cursor = match.Index + match.Length;
        }

        if (cursor < operatorTemplate.Length) {
            string tail = operatorTemplate.Substring(cursor);
            CreateStaticPart(tail);
        }

        if (matches.Count == 0) {
            CreateStaticPart(operatorTemplate);
        }
    }

    private void CreateStaticPart(string text) {
        GameObject go = new GameObject("Txt", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(dynamicContentRoot, false);

        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 22f;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;

        LayoutElement le = go.GetComponent<LayoutElement>();
        le.preferredWidth = Mathf.Max(16f, tmp.preferredWidth + 2f);
        le.preferredHeight = 26f;
    }

    private void CreateInlineInput() {
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
        le.minWidth = 34f;
        le.preferredWidth = 42f;
        le.preferredHeight = 24f;

        TMP_InputField input = root.GetComponent<TMP_InputField>();
        input.lineType = TMP_InputField.LineType.SingleLine;

        GameObject textGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.SetParent(rect, false);
        textRect.anchorMin = new Vector2(0f, 0f);
        textRect.anchorMax = new Vector2(1f, 1f);
        textRect.offsetMin = new Vector2(4f, 1f);
        textRect.offsetMax = new Vector2(-4f, -1f);

        TextMeshProUGUI textComp = textGO.GetComponent<TextMeshProUGUI>();
        textComp.text = string.Empty;
        textComp.fontSize = 19f;
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
        phText.fontSize = 19f;
        phText.color = new Color(1f, 1f, 1f, 0.4f);
        phText.alignment = TextAlignmentOptions.Center;

        input.textComponent = textComp;
        input.placeholder = phText;

        UpdateInputSlotWidth(input, le, 34f, 120f);
        input.onValueChanged.AddListener(_ => UpdateInputSlotWidth(input, le, 34f, 120f));
    }

    private void UpdateInputSlotWidth(TMP_InputField input, LayoutElement le, float minWidth, float maxWidth) {
        if (input == null || le == null || input.textComponent == null) return;

        string value = string.IsNullOrEmpty(input.text) ? "..." : input.text;
        float textWidth = input.textComponent.GetPreferredValues(value).x;
        float target = Mathf.Clamp(textWidth + 12f, minWidth, maxWidth);

        le.preferredWidth = target;
    }

    public void OnDrop(PointerEventData eventData) {
        if (eventData.pointerDrag == null) return;

        BloqueCodigo dragged = eventData.pointerDrag.GetComponent<BloqueCodigo>();
        if (dragged == null || dragged.commandType != "operator") return;

        TrySetOperator(dragged.codeText);
    }
}
