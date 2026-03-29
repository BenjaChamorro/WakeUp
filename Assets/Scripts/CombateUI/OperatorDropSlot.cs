using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Text.RegularExpressions;

public class OperatorDropSlot : MonoBehaviour, IDropHandler {
    private static readonly Regex MultiSpaceRegex = new Regex("\\s+");

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

        string cleaned = NormalizeOperatorText(operatorText);
        if (string.IsNullOrWhiteSpace(cleaned) || cleaned == "op") return false;

        CurrentOperator = cleaned;
        EnsureContentRoot();
        BuildOperatorContent(cleaned);
        HidePlaceholderLabel();

        LayoutElement le = GetComponent<LayoutElement>();
        if (le != null) {
            le.preferredWidth = Mathf.Max(64f, 26f + (cleaned.Length * 14f));
        }

        return true;
    }

    private string NormalizeOperatorText(string source) {
        if (string.IsNullOrWhiteSpace(source)) return "op";

        string cleaned = source.Replace("_", string.Empty);
        cleaned = MultiSpaceRegex.Replace(cleaned, " ").Trim();

        return string.IsNullOrWhiteSpace(cleaned) ? "op" : cleaned;
    }

    private void HidePlaceholderLabel() {
        if (slotText == null) {
            Transform directText = transform.Find("Text");
            if (directText != null) {
                slotText = directText.GetComponent<TextMeshProUGUI>();
            }

            if (slotText == null) {
                slotText = GetComponentInChildren<TextMeshProUGUI>(true);
            }
        }

        if (slotText != null) {
            slotText.text = string.Empty;
            slotText.gameObject.SetActive(false);
        }
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

        CreateStaticPart(operatorTemplate);
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

    public void OnDrop(PointerEventData eventData) {
        if (eventData.pointerDrag == null) return;

        BloqueCodigo dragged = eventData.pointerDrag.GetComponent<BloqueCodigo>();
        if (dragged == null || !IsOperatorType(dragged.commandType)) return;

        TrySetOperator(dragged.codeText);
    }

    private bool IsOperatorType(string type) {
        if (string.IsNullOrWhiteSpace(type)) return false;

        string lower = type.Trim().ToLowerInvariant();
        return lower == "operator";
    }
}
