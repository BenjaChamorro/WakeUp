using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CodeLineTemplateRenderer : MonoBehaviour {
    private const string ContainerName = "LineTemplateContainer";
    private static readonly Regex TokenRegex = new Regex("(operador|operator|_)", RegexOptions.IgnoreCase);

    private RectTransform containerRect;
    private string currentTemplate;

    public void Initialize(string templateText, string commandType) {
        currentTemplate = templateText;

        string normalized = templateText.ToLowerInvariant();
        bool needsTemplate = templateText.Contains("_") || normalized.Contains("operador") || normalized.Contains("operator");
        TextMeshProUGUI mainText = transform.Find("TextoBloqueCodigo")?.GetComponent<TextMeshProUGUI>();

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
        RebuildTemplate();
    }

    private void EnsureContainer() {
        Transform existing = transform.Find(ContainerName);
        if (existing != null) {
            containerRect = existing as RectTransform;
            ConfigureContainerRect(containerRect);
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
        fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
    }

    private void ConfigureContainerRect(RectTransform rect) {
        if (rect == null) return;

        // Mantener el contenedor compacto, anclado a la izquierda, sin estirarlo al ancho completo.
        rect.anchorMin = new Vector2(0f, 0.5f);
        rect.anchorMax = new Vector2(0f, 0.5f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.anchoredPosition = new Vector2(34f, 0f);
        rect.sizeDelta = new Vector2(0f, 32f);
    }

    private void RebuildTemplate() {
        if (containerRect == null) return;

        for (int i = containerRect.childCount - 1; i >= 0; i--) {
            Destroy(containerRect.GetChild(i).gameObject);
        }

        int cursor = 0;
        MatchCollection matches = TokenRegex.Matches(currentTemplate);
        for (int i = 0; i < matches.Count; i++) {
            Match match = matches[i];
            if (match.Index > cursor) {
                string staticText = currentTemplate.Substring(cursor, match.Index - cursor);
                CreateStaticLabel(staticText);
            }

            if (match.Value == "_") {
                CreateInlineInput();
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
    }

    private void CreateStaticLabel(string text) {
        GameObject go = new GameObject("Txt", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(containerRect, false);

        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 26;
        tmp.color = Color.white;
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;

        LayoutElement le = go.AddComponent<LayoutElement>();
        le.preferredWidth = Mathf.Max(8f, tmp.preferredWidth + 2f);
        le.preferredHeight = 30f;
    }

    private void CreateInlineInput() {
        GameObject root = new GameObject("InputSlot", typeof(RectTransform), typeof(Image), typeof(TMP_InputField), typeof(LayoutElement));
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.SetParent(containerRect, false);

        Image img = root.GetComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0.03f);

        Outline border = root.AddComponent<Outline>();
        border.effectColor = new Color(1f, 1f, 1f, 0.35f);
        border.effectDistance = new Vector2(1f, -1f);
        border.useGraphicAlpha = true;

        LayoutElement le = root.GetComponent<LayoutElement>();
        le.minWidth = 48f;
        le.preferredWidth = 64f;
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
        textComp.fontSize = 24;
        textComp.color = Color.white;
        textComp.alignment = TextAlignmentOptions.MidlineLeft;

        GameObject placeholderGO = new GameObject("Placeholder", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform placeholderRect = placeholderGO.GetComponent<RectTransform>();
        placeholderRect.SetParent(rect, false);
        placeholderRect.anchorMin = new Vector2(0f, 0f);
        placeholderRect.anchorMax = new Vector2(1f, 1f);
        placeholderRect.offsetMin = new Vector2(6f, 2f);
        placeholderRect.offsetMax = new Vector2(-6f, -2f);

        TextMeshProUGUI placeholderComp = placeholderGO.GetComponent<TextMeshProUGUI>();
        placeholderComp.text = string.Empty;
        placeholderComp.fontSize = 24;
        placeholderComp.color = new Color(1f, 1f, 1f, 0.4f);
        placeholderComp.alignment = TextAlignmentOptions.MidlineLeft;

        input.textComponent = textComp;
        input.placeholder = placeholderComp;
        input.lineType = TMP_InputField.LineType.SingleLine;

        UpdateInputSlotWidth(input, le, 48f, 180f);
        input.onValueChanged.AddListener(_ => UpdateInputSlotWidth(input, le, 48f, 180f));
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
        text.fontSize = 24;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;

    }

    private void RemoveContainerIfExists() {
        Transform existing = transform.Find(ContainerName);
        if (existing != null) {
            Destroy(existing.gameObject);
        }
    }
}
