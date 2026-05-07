using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class BloqueCodigo : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler {
    private const float ConsoleLineSpacing = 3f;
    private const float ConsoleLineNumberWidth = 28f;
    private const int ConsoleLeftPadding = 6;
    private const int IndentSizePixels = 24;
    private const float ConsoleMinHorizontalScrollWidth = 3000f;

    [Header("Datos")]
    public string blockId = "";
    public string codeText = "Hola Mundo";
    public string commandType = "operador"; // "if", "+", "print"

    [Header("Referencias")]
    public Transform contenedorBloques; // BloquesCodigo
    public BloqueCodigo prefabOriginal; // BloqueCodigoPrefab
    public RectTransform puntoSpawnBloques; // Punto local dentro de BloquesCodigo
    public RectTransform consolaDropZone; // Zona valida para soltar
    public Transform lineasConsolaTarget; // Contenedor de lineas en consola

    [Header("Ajuste visual paleta")]
    [SerializeField] private float paletteTextHorizontalNudge = -20f;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Canvas canvas;
    private Transform originalParent;
    private int originalSiblingIndex;
    private Vector2 originalAnchoredPosition;
    private Vector2 templateAnchorMin;
    private Vector2 templateAnchorMax;
    private Vector2 templatePivot;
    private Vector2 templateSizeDelta;
    private Vector2 templateOffsetMin;
    private Vector2 templateOffsetMax;
    private bool hasTemplateRect;
    private LayoutElement lineNumberLayout;
    private bool hasLineNumberLayoutTemplate;
    private float lineNumberPreferredWidth;
    private float lineNumberMinWidth;
    private float lineNumberFlexibleWidth;
    private RectTransform codeTextRect;
    private bool hasCodeTextAnchoredTemplate;
    private Vector2 codeTextAnchoredTemplate;
    private int indentLevel;

    public static List<BloqueCodigo> consoleLines = new List<BloqueCodigo>();
    [HideInInspector] public int lineNumber = -1;

    void Awake() {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        UpdateBlockText();
        CaptureTemplateRect();
        CacheLineNumberLayoutTemplate();
        CacheCodeTextTemplate();

        canvas = GetComponentInParent<Canvas>();
        if (canvas == null) {
            canvas = FindObjectOfType<Canvas>();
        }
    }

    public void Setup(string newText, string newType = "", string newBlockId = "") {
        blockId = newBlockId;
        codeText = newText;
        commandType = newType;
        UpdateBlockText();

        // En paleta no se muestra el numero de linea para evitar espacio vacio a la izquierda.
        SetLineNumberVisible(false);
    }

    public void OnBeginDrag(PointerEventData eventData) {
        originalParent = transform.parent;
        originalSiblingIndex = transform.GetSiblingIndex();
        originalAnchoredPosition = rectTransform.anchoredPosition;

        if (IsFromPalette(originalParent)) {
            CaptureTemplateRect();
        }

        canvasGroup.blocksRaycasts = false;
        transform.SetParent(canvas.transform, true);
        canvasGroup.alpha = 0.6f;
    }

    public void OnDrag(PointerEventData eventData) {
        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData) {
        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1f;

        OperatorDropSlot operatorSlot = FindParentWithComponent<OperatorDropSlot>(eventData.pointerEnter != null ? eventData.pointerEnter.transform : null);
        if (operatorSlot != null && IsOperatorType(commandType)) {
            HandleOperatorSlotDrop(operatorSlot);
            return;
        }

        AssignmentValueSlot assignmentSlot = FindParentWithComponent<AssignmentValueSlot>(eventData.pointerEnter != null ? eventData.pointerEnter.transform : null);
        if (assignmentSlot != null && (IsMathOperatorType(commandType) || IsDefinitionType(commandType))) {
            HandleAssignmentValueSlotDrop(assignmentSlot);
            return;
        }

        bool droppedInConsole = IsPointerInsideConsole(eventData);

        if (droppedInConsole) {
            DropToConsole(eventData);
        } else {
            // Si estaba en la consola y se arrastra fuera, destruirlo; si no, devolverlo
            HandleOutOfConsoleRelease();
        }
    }

    private bool IsPointerInsideConsole(PointerEventData eventData) {
        if (eventData.pointerEnter != null) {
            Transform current = eventData.pointerEnter.transform;
            while (current != null) {
                if (current.GetComponent<SoltarEnConsola>() != null) {
                    return true;
                }
                current = current.parent;
            }
        }

        RectTransform target = consolaDropZone;
        if (target == null) {
            GameObject root = GameObject.Find("CombateUI-Consola");
            if (root == null) return false;

            Transform consola = root.transform.Find("Consola");
            if (consola == null) return false;

            target = consola as RectTransform;
        }

        Camera uiCamera = eventData.pressEventCamera != null ? eventData.pressEventCamera : eventData.enterEventCamera;
        return RectTransformUtility.RectangleContainsScreenPoint(target, eventData.position, uiCamera);
    }

    private void ReturnToOriginalPosition() {
        if (originalParent == null) return;

        transform.SetParent(originalParent, false);
        transform.SetSiblingIndex(originalSiblingIndex);
        rectTransform.anchoredPosition = originalAnchoredPosition;
        rectTransform.localScale = Vector3.one;
    }

    private void HandleOutOfConsoleRelease() {
        Transform lineasConsola = ResolveLineasConsola();
        
        // Si el bloque estaba en la consola, destruirlo
        if (originalParent != null && lineasConsola != null && originalParent == lineasConsola) {
            RemoveOwnedIndentPlaceholder(lineasConsola);
            consoleLines.Remove(this);
            RefreshLineNumbersFromHierarchy(lineasConsola);
            Destroy(gameObject);
            return;
        }

        // Si no estaba en la consola, devolverlo a su posición original (paleta)
        ReturnToOriginalPosition();
    }

    private void HandleOperatorSlotDrop(OperatorDropSlot slot) {
        if (slot == null) {
            ReturnToOriginalPosition();
            return;
        }

        bool accepted = slot.TrySetOperator(codeText);
        if (!accepted) {
            ReturnToOriginalPosition();
            return;
        }

        if (IsFromPalette(originalParent)) {
            ReturnToOriginalPosition();
            return;
        }

        Transform lineasConsola = ResolveLineasConsola();
        if (lineasConsola != null) {
            RemoveOwnedIndentPlaceholder(lineasConsola);
        }
        consoleLines.Remove(this);
        UpdateLineNumbers();
        Destroy(gameObject);
    }

    private bool IsOperatorType(string type) {
        if (string.IsNullOrWhiteSpace(type)) return false;

        string lower = type.Trim().ToLowerInvariant();
        return lower == "operator";
    }

    private bool IsMathOperatorType(string type) {
        if (string.IsNullOrWhiteSpace(type)) return false;

        string lower = type.Trim().ToLowerInvariant();
        return lower == "mathoperator";
    }

    private bool IsDefinitionType(string type) {
        if (string.IsNullOrWhiteSpace(type)) return false;

        string lower = type.Trim().ToLowerInvariant();
        return lower == "definition";
    }

    private void HandleAssignmentValueSlotDrop(AssignmentValueSlot slot) {
        if (slot == null) {
            ReturnToOriginalPosition();
            return;
        }

        bool accepted = false;
        if (IsMathOperatorType(commandType)) {
            accepted = slot.TrySetOperator(codeText);
        } else if (IsDefinitionType(commandType)) {
            accepted = slot.TrySetDefinition(blockId, codeText);
        }

        if (!accepted) {
            ReturnToOriginalPosition();
            return;
        }

        if (IsFromPalette(originalParent)) {
            ReturnToOriginalPosition();
            return;
        }

        Transform lineasConsola = ResolveLineasConsola();
        if (lineasConsola != null) {
            RemoveOwnedIndentPlaceholder(lineasConsola);
        }
        consoleLines.Remove(this);
        UpdateLineNumbers();
        Destroy(gameObject);
    }

    private T FindParentWithComponent<T>(Transform start) where T : Component {
        Transform current = start;
        while (current != null) {
            T component = current.GetComponent<T>();
            if (component != null) return component;
            current = current.parent;
        }

        return null;
    }

    void DropToConsole(PointerEventData eventData) {
        bool draggedFromPalette = IsFromPalette(originalParent);

        Transform lineasConsola = ResolveLineasConsola();
        if (lineasConsola == null) {
            ReturnToOriginalPosition();
            return;
        }

        EnsureConsoleLinesLayout(lineasConsola);

        transform.SetParent(lineasConsola, false);
        IndentDropLineMarker dropMarker = FindParentWithComponent<IndentDropLineMarker>(eventData.pointerEnter != null ? eventData.pointerEnter.transform : null);
        if (dropMarker != null && dropMarker.transform.parent == lineasConsola) {
            int markerIndex = dropMarker.transform.GetSiblingIndex();
            transform.SetSiblingIndex(markerIndex);
            indentLevel = Mathf.Max(1, dropMarker.indentLevel);
        } else {
            rectTransform.SetAsLastSibling();
            indentLevel = 0;
        }
        rectTransform.localScale = Vector3.one;

        // Debe participar siempre en el VerticalLayoutGroup de la consola.
        LayoutElement selfLayout = GetComponent<LayoutElement>();
        if (selfLayout == null) {
            selfLayout = gameObject.AddComponent<LayoutElement>();
        }
        selfLayout.ignoreLayout = false;

        ContentSizeFitter rowFitter = GetComponent<ContentSizeFitter>();
        if (rowFitter == null) {
            rowFitter = gameObject.AddComponent<ContentSizeFitter>();
        }
        rowFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        rowFitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

        HorizontalLayoutGroup rowLayout = GetComponent<HorizontalLayoutGroup>();
        if (rowLayout == null) {
            rowLayout = gameObject.AddComponent<HorizontalLayoutGroup>();
        }
        rowLayout.childAlignment = TextAnchor.MiddleLeft;
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = true;
        rowLayout.childForceExpandWidth = false;
        rowLayout.childForceExpandHeight = false;
        rowLayout.spacing = 4f;
        rowLayout.padding = new RectOffset(indentLevel * IndentSizePixels, 0, 0, 0);

        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(0f, 1f);
        rectTransform.pivot = new Vector2(0f, 1f);
        rectTransform.anchoredPosition = Vector2.zero;

        // En consola si se muestra el numero de linea.
        SetLineNumberVisible(true);

        LayoutElement ownerLayout = GetComponent<LayoutElement>();
        if (ownerLayout == null) {
            ownerLayout = gameObject.AddComponent<LayoutElement>();
        }
        if (ownerLayout.preferredHeight <= 0f) {
            ownerLayout.minHeight = 32f;
            ownerLayout.preferredHeight = 32f;
            ownerLayout.flexibleHeight = 0f;
        }

        CodeLineTemplateRenderer templateRenderer = GetComponent<CodeLineTemplateRenderer>();
        if (templateRenderer == null) {
            templateRenderer = gameObject.AddComponent<CodeLineTemplateRenderer>();
        }
        templateRenderer.Initialize(codeText, commandType, blockId);

        if (!consoleLines.Contains(this)) {
            consoleLines.Add(this);
        }

        if (RequiresIndentedBody()) {
            EnsureIndentPlaceholderExists(lineasConsola);
        }

        RefreshLineNumbersFromHierarchy(lineasConsola);
        RefreshConsoleLayout(lineasConsola);

        if (draggedFromPalette && contenedorBloques != null && prefabOriginal != null) {
            BloqueCodigo nuevo = Instantiate(prefabOriginal, contenedorBloques, false);
            nuevo.gameObject.SetActive(true);
            nuevo.blockId = this.blockId;
            nuevo.codeText = this.codeText;
            nuevo.commandType = this.commandType;
            nuevo.UpdateBlockText();

            int targetIndex = Mathf.Clamp(originalSiblingIndex, 0, Mathf.Max(0, contenedorBloques.childCount - 1));
            nuevo.transform.SetSiblingIndex(targetIndex);

            RectTransform nrt = nuevo.GetComponent<RectTransform>();
            if (nrt != null) {
                ApplyTemplateToRect(nrt);

                // En paleta con VerticalLayoutGroup, el layout decide la posicion.
                nrt.anchoredPosition = Vector2.zero;
                nrt.localScale = Vector3.one;
            }

            LayoutGroup paletteLayout = contenedorBloques.GetComponent<LayoutGroup>();
            if (paletteLayout != null) {
                LayoutElement le = nuevo.GetComponent<LayoutElement>();
                if (le != null) {
                    le.ignoreLayout = false;
                }

                RectTransform paletteRect = contenedorBloques as RectTransform;
                if (paletteRect != null) {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(paletteRect);
                }
            }

            var lineNumTxt = nuevo.transform.Find("TextoNumeroLinea")?.GetComponent<TextMeshProUGUI>();
            if (lineNumTxt) lineNumTxt.text = string.Empty;
            nuevo.SetLineNumberVisible(false);

            nuevo.contenedorBloques = contenedorBloques;
            nuevo.prefabOriginal = prefabOriginal;
            nuevo.puntoSpawnBloques = puntoSpawnBloques;
            nuevo.consolaDropZone = consolaDropZone;
            nuevo.lineasConsolaTarget = lineasConsolaTarget;
            nuevo.CopyTemplateFrom(this);
        }
    }

    private void CaptureTemplateRect() {
        templateAnchorMin = rectTransform.anchorMin;
        templateAnchorMax = rectTransform.anchorMax;
        templatePivot = rectTransform.pivot;
        templateSizeDelta = rectTransform.sizeDelta;
        templateOffsetMin = rectTransform.offsetMin;
        templateOffsetMax = rectTransform.offsetMax;
        hasTemplateRect = true;
    }

    private void CopyTemplateFrom(BloqueCodigo source) {
        if (source == null || !source.hasTemplateRect) return;

        templateAnchorMin = source.templateAnchorMin;
        templateAnchorMax = source.templateAnchorMax;
        templatePivot = source.templatePivot;
        templateSizeDelta = source.templateSizeDelta;
        templateOffsetMin = source.templateOffsetMin;
        templateOffsetMax = source.templateOffsetMax;
        hasTemplateRect = true;
    }

    private void ApplyTemplateToRect(RectTransform target) {
        if (target == null) return;

        if (!hasTemplateRect) {
            CaptureTemplateRect();
        }

        target.anchorMin = templateAnchorMin;
        target.anchorMax = templateAnchorMax;
        target.pivot = templatePivot;
        target.sizeDelta = templateSizeDelta;
        target.offsetMin = templateOffsetMin;
        target.offsetMax = templateOffsetMax;
    }

    private Vector2 GetSpawnAnchoredPosition() {
        if (puntoSpawnBloques == null) {
            return originalAnchoredPosition;
        }

        RectTransform paletteRect = contenedorBloques as RectTransform;
        if (paletteRect == null) {
            return puntoSpawnBloques.anchoredPosition;
        }

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            paletteRect,
            RectTransformUtility.WorldToScreenPoint(canvas != null ? canvas.worldCamera : null, puntoSpawnBloques.position),
            canvas != null ? canvas.worldCamera : null,
            out localPoint
        );

        return localPoint;
    }

    private bool IsFromPalette(Transform sourceParent) {
        if (sourceParent == null) return false;

        if (contenedorBloques != null && sourceParent.IsChildOf(contenedorBloques)) {
            return true;
        }

        return !consoleLines.Contains(this);
    }

    private Transform ResolveLineasConsola() {
        if (lineasConsolaTarget != null) {
            return lineasConsolaTarget;
        }

        GameObject root = GameObject.Find("CombateUI-Consola");
        if (root == null) return null;

        Transform consola = root.transform.Find("Consola");
        if (consola == null) return null;

        Transform direct = consola.Find("LineasConsola");
        if (direct != null) return direct;

        Transform viewport = consola.Find("Viewport");
        if (viewport != null) {
            Transform underViewport = viewport.Find("LineasConsola");
            if (underViewport != null) return underViewport;
        }

        foreach (Transform child in consola.GetComponentsInChildren<Transform>(true)) {
            if (child.name == "LineasConsola") {
                return child;
            }
        }

        return null;
    }

    private void UpdateBlockText() {
        TextMeshProUGUI txt = transform.Find("TextoBloqueCodigo")?.GetComponent<TextMeshProUGUI>();
        if (txt) txt.text = codeText;
    }

    private bool RequiresIndentedBody() {
        if (string.IsNullOrWhiteSpace(blockId)) return false;

        string id = blockId.Trim().ToLowerInvariant();
        return id == "for" || id == "if" || id == "while";
    }

    private void EnsureIndentPlaceholderExists(Transform lineasConsola) {
        if (lineasConsola == null) return;

        foreach (Transform child in lineasConsola) {
            IndentDropLineMarker marker = child.GetComponent<IndentDropLineMarker>();
            if (marker != null && marker.owner == this) {
                marker.indentLevel = indentLevel + 1;
                int ownIndex = transform.GetSiblingIndex();
                marker.transform.SetSiblingIndex(Mathf.Min(ownIndex + 1, lineasConsola.childCount - 1));
                marker.RefreshVisual();
                return;
            }
        }

        GameObject placeholder = new GameObject("IndentDropLine", typeof(RectTransform), typeof(LayoutElement), typeof(HorizontalLayoutGroup), typeof(Image), typeof(IndentDropLineMarker));
        RectTransform rect = placeholder.GetComponent<RectTransform>();
        rect.SetParent(lineasConsola, false);
        rect.localScale = Vector3.one;

        int ownerSiblingIndex = transform.GetSiblingIndex();
        rect.SetSiblingIndex(Mathf.Min(ownerSiblingIndex + 1, lineasConsola.childCount - 1));

        LayoutElement le = placeholder.GetComponent<LayoutElement>();
        le.minHeight = 30f;
        le.preferredHeight = 30f;
        le.flexibleHeight = 0f;
        le.minWidth = 200f;

        HorizontalLayoutGroup hlg = placeholder.GetComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        hlg.padding = new RectOffset((indentLevel + 1) * IndentSizePixels, 0, 0, 0);

        Image bg = placeholder.GetComponent<Image>();
        bg.color = new Color(1f, 1f, 1f, 0.04f);

        GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.SetParent(rect, false);

        TextMeshProUGUI text = textObj.GetComponent<TextMeshProUGUI>();
        text.text = "[arrastra bloque aqui]";
        text.fontSize = 18f;
        text.color = new Color(1f, 1f, 1f, 0.45f);
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.enableWordWrapping = false;

        LayoutElement textLE = textObj.AddComponent<LayoutElement>();
        textLE.preferredHeight = 26f;

        IndentDropLineMarker dropMarker = placeholder.GetComponent<IndentDropLineMarker>();
        dropMarker.owner = this;
        dropMarker.indentLevel = indentLevel + 1;
        dropMarker.RefreshVisual();
    }

    private void RemoveOwnedIndentPlaceholder(Transform lineasConsola) {
        if (lineasConsola == null) return;

        for (int i = lineasConsola.childCount - 1; i >= 0; i--) {
            Transform child = lineasConsola.GetChild(i);
            IndentDropLineMarker marker = child.GetComponent<IndentDropLineMarker>();
            if (marker != null && marker.owner == this) {
                Destroy(child.gameObject);
            }
        }
    }

    public void ApplyIndentVisual() {
        HorizontalLayoutGroup rowLayout = GetComponent<HorizontalLayoutGroup>();
        if (rowLayout != null) {
            rowLayout.padding = new RectOffset(indentLevel * IndentSizePixels, 0, 0, 0);
        }
    }

    private void CacheLineNumberLayoutTemplate() {
        Transform lineNum = transform.Find("TextoNumeroLinea");
        if (lineNum == null) return;

        lineNumberLayout = lineNum.GetComponent<LayoutElement>();
        if (lineNumberLayout == null) {
            lineNumberLayout = lineNum.gameObject.AddComponent<LayoutElement>();
        }

        lineNumberPreferredWidth = lineNumberLayout.preferredWidth;
        lineNumberMinWidth = lineNumberLayout.minWidth;
        lineNumberFlexibleWidth = lineNumberLayout.flexibleWidth;
        hasLineNumberLayoutTemplate = true;
    }

    private void CacheCodeTextTemplate() {
        if (codeTextRect == null) {
            codeTextRect = transform.Find("TextoBloqueCodigo") as RectTransform;
        }

        if (codeTextRect == null) return;

        codeTextAnchoredTemplate = codeTextRect.anchoredPosition;
        hasCodeTextAnchoredTemplate = true;
    }

    private void SetLineNumberVisible(bool visible) {
        Transform lineNum = transform.Find("TextoNumeroLinea");
        if (lineNum == null) return;

        if (!hasLineNumberLayoutTemplate || lineNumberLayout == null) {
            CacheLineNumberLayoutTemplate();
        }

        if (lineNum.gameObject.activeSelf != visible) {
            lineNum.gameObject.SetActive(visible);
        }

        if (lineNumberLayout != null) {
            if (visible) {
                // En consola usar ancho compacto fijo para no empujar el contenido hacia el centro.
                lineNumberLayout.preferredWidth = ConsoleLineNumberWidth;
                lineNumberLayout.minWidth = ConsoleLineNumberWidth;
                lineNumberLayout.flexibleWidth = 0f;
            } else {
                lineNumberLayout.preferredWidth = 0f;
                lineNumberLayout.minWidth = 0f;
                lineNumberLayout.flexibleWidth = 0f;
            }
        }

        if (visible) {
            RectTransform lineNumRect = lineNum as RectTransform;
            if (lineNumRect != null) {
                lineNumRect.anchorMin = new Vector2(0f, 0.5f);
                lineNumRect.anchorMax = new Vector2(0f, 0.5f);
                lineNumRect.pivot = new Vector2(0f, 0.5f);
                lineNumRect.anchoredPosition = new Vector2(3f, 0f);
            }
        }

        // En paleta desplazamos el texto del bloque hacia la izquierda.
        if (!hasCodeTextAnchoredTemplate || codeTextRect == null) {
            CacheCodeTextTemplate();
        }

        if (codeTextRect != null && hasCodeTextAnchoredTemplate) {
            if (visible) {
                codeTextRect.anchoredPosition = codeTextAnchoredTemplate;
            } else {
                Vector2 nudgePos = codeTextAnchoredTemplate;
                nudgePos.x += paletteTextHorizontalNudge;
                codeTextRect.anchoredPosition = nudgePos;
            }
        }

        if (!visible) {
            TextMeshProUGUI txt = lineNum.GetComponent<TextMeshProUGUI>();
            if (txt != null) {
                txt.text = string.Empty;
            }
        }
    }


    public static void UpdateLineNumbers() {
        for (int i = consoleLines.Count - 1; i >= 0; i--) {
            if (consoleLines[i] == null) {
                consoleLines.RemoveAt(i);
            }
        }

        for (int i = 0; i < consoleLines.Count; i++) {
            consoleLines[i].lineNumber = i + 1;
            consoleLines[i].SetLineNumberVisible(true);
            consoleLines[i].ApplyIndentVisual();
            Transform lineNum = consoleLines[i].transform.Find("TextoNumeroLinea");
            if (lineNum) {
                TextMeshProUGUI numTxt = lineNum.GetComponent<TextMeshProUGUI>();
                if (numTxt != null) {
                    numTxt.text = (i + 1) + ": ";
                }
            }
        }

        if (consoleLines.Count > 0 && consoleLines[0] != null && consoleLines[0].transform.parent != null) {
            consoleLines[0].RefreshConsoleLayout(consoleLines[0].transform.parent);
        }
    }

    public static void RefreshLineNumbersFromHierarchy(Transform lineasConsola) {
        // Reconstruir consoleLines basándose en el orden actual en la jerarquía del UI
        if (lineasConsola == null) return;

        // Limpiar referencias nulas
        for (int i = consoleLines.Count - 1; i >= 0; i--) {
            if (consoleLines[i] == null) {
                consoleLines.RemoveAt(i);
            }
        }

        // Obtener la lista de bloques que están actualmente bajo LineasConsola, en orden
        List<BloqueCodigo> orderedLines = new List<BloqueCodigo>();
        for (int i = 0; i < lineasConsola.childCount; i++) {
            BloqueCodigo codigo = lineasConsola.GetChild(i).GetComponent<BloqueCodigo>();
            if (codigo != null) {
                orderedLines.Add(codigo);
            }
        }

        // Actualizar consoleLines solo si el contenido realmente cambió
        if (!ListsAreEqual(consoleLines, orderedLines)) {
            consoleLines = orderedLines;
        }

        // Actualizar números de línea
        for (int i = 0; i < consoleLines.Count; i++) {
            consoleLines[i].lineNumber = i + 1;
            consoleLines[i].SetLineNumberVisible(true);
            Transform lineNum = consoleLines[i].transform.Find("TextoNumeroLinea");
            if (lineNum) {
                TextMeshProUGUI numTxt = lineNum.GetComponent<TextMeshProUGUI>();
                if (numTxt != null) {
                    numTxt.text = (i + 1) + ": ";
                }
            }
        }

        if (consoleLines.Count > 0 && consoleLines[0] != null && consoleLines[0].transform.parent != null) {
            consoleLines[0].RefreshConsoleLayout(consoleLines[0].transform.parent);
        }
    }

    private static bool ListsAreEqual(List<BloqueCodigo> list1, List<BloqueCodigo> list2) {
        if (list1.Count != list2.Count) return false;
        for (int i = 0; i < list1.Count; i++) {
            if (list1[i] != list2[i]) return false;
        }
        return true;
    }

    private static void EnsureConsoleLinesLayout(Transform lineasConsola) {
        if (lineasConsola == null) return;

        RectTransform contentRect = lineasConsola as RectTransform;
        if (contentRect != null) {
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(0f, 1f);
            contentRect.pivot = new Vector2(0f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
        }

        VerticalLayoutGroup vlg = lineasConsola.GetComponent<VerticalLayoutGroup>();
        if (vlg == null) {
            vlg = lineasConsola.gameObject.AddComponent<VerticalLayoutGroup>();
        }

        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = false;
        vlg.childForceExpandHeight = false;
        vlg.spacing = ConsoleLineSpacing;
        vlg.padding = new RectOffset(ConsoleLeftPadding, 0, 0, 0);

        ContentSizeFitter fitter = lineasConsola.GetComponent<ContentSizeFitter>();
        if (fitter == null) {
            fitter = lineasConsola.gameObject.AddComponent<ContentSizeFitter>();
        }
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        LayoutElement contentLayout = lineasConsola.GetComponent<LayoutElement>();
        if (contentLayout == null) {
            contentLayout = lineasConsola.gameObject.AddComponent<LayoutElement>();
        }
        // Forzar un ancho mínimo grande para que el scroll horizontal siempre esté disponible.
        contentLayout.minWidth = ConsoleMinHorizontalScrollWidth;
        contentLayout.preferredWidth = Mathf.Max(contentLayout.preferredWidth, ConsoleMinHorizontalScrollWidth);

        ScrollRect scroll = lineasConsola.GetComponentInParent<ScrollRect>();
        if (scroll != null) {
            scroll.horizontal = true;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.inertia = false;
            scroll.decelerationRate = 0f;

            RectTransform linesRect = lineasConsola as RectTransform;
            if (linesRect != null) {
                scroll.content = linesRect;
            }

            if (scroll.viewport == null) {
                Transform vp = lineasConsola.parent;
                if (vp != null && vp.name == "Viewport") {
                    scroll.viewport = vp as RectTransform;
                }
            }
        }
    }

    private void RefreshConsoleLayout(Transform lineasConsola) {
        if (lineasConsola == null) return;

        EnsureConsoleLinesLayout(lineasConsola);

        RectTransform linesRect = lineasConsola as RectTransform;
        if (linesRect != null) {
            LayoutRebuilder.ForceRebuildLayoutImmediate(linesRect);
        }

        RectTransform parentRect = lineasConsola.parent as RectTransform;
        if (parentRect != null) {
            LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
        }

        Canvas.ForceUpdateCanvases();
    }
}
