using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CodeLineTemplateRenderer : MonoBehaviour {
    private const string ContainerName = "LineTemplateContainer";
    private static readonly Regex TokenRegex = new Regex("(operador|_)");

    private RectTransform containerRect;
    private string currentTemplate;

    public void Initialize(string templateText, string commandType) {
        currentTemplate = templateText;

        bool needsTemplate = templateText.Contains("_") || templateText.Contains("operador");
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
            return;
        }

        GameObject go = new GameObject(ContainerName, typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
        containerRect = go.GetComponent<RectTransform>();
        containerRect.SetParent(transform, false);
        containerRect.anchorMin = new Vector2(0f, 0f);
        containerRect.anchorMax = new Vector2(1f, 1f);
        containerRect.offsetMin = new Vector2(34f, 4f);
        containerRect.offsetMax = new Vector2(-8f, -4f);

        HorizontalLayoutGroup hlg = go.GetComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = false;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        hlg.spacing = 4f;

        ContentSizeFitter fitter = go.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
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
    }

    private void CreateStaticLabel(string text) {
        GameObject go = new GameObject("Txt", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(containerRect, false);

        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 26;
        tmp.color = Color.white;
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
        img.color = new Color(0f, 0f, 0f, 0.15f);

        LayoutElement le = root.GetComponent<LayoutElement>();
        le.preferredWidth = 70f;
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
        placeholderComp.text = "...";
        placeholderComp.fontSize = 24;
        placeholderComp.color = new Color(1f, 1f, 1f, 0.4f);
        placeholderComp.alignment = TextAlignmentOptions.MidlineLeft;

        input.textComponent = textComp;
        input.placeholder = placeholderComp;
        input.lineType = TMP_InputField.LineType.SingleLine;
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
