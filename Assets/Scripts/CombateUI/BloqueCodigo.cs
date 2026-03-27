using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class BloqueCodigo : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler {
    [Header("Datos")]
    public string codeText = "Hola Mundo";
    public string commandType = "operador"; // "if", "+", "print"

    [Header("Referencias")]
    public Transform contenedorBloques; // BloquesCodigo
    public BloqueCodigo prefabOriginal; // BloqueCodigoPrefab
    public RectTransform puntoSpawnBloques; // Punto local dentro de BloquesCodigo
    public RectTransform consolaDropZone; // Zona valida para soltar
    public Transform lineasConsolaTarget; // Contenedor de lineas en consola

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

    public static List<BloqueCodigo> consoleLines = new List<BloqueCodigo>();
    [HideInInspector] public int lineNumber = -1;

    void Awake() {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        UpdateBlockText();
        CaptureTemplateRect();

        canvas = GetComponentInParent<Canvas>();
        if (canvas == null) {
            canvas = FindObjectOfType<Canvas>();
        }
    }

    public void Setup(string newText, string newType = "") {
        codeText = newText;
        commandType = newType;
        UpdateBlockText();
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

        bool droppedInConsole = IsPointerInsideConsole(eventData);

        if (droppedInConsole) {
            DropToConsole();
        } else {
            ReturnToOriginalPosition();
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

    void DropToConsole() {
        bool draggedFromPalette = IsFromPalette(originalParent);

        Transform lineasConsola = ResolveLineasConsola();
        if (lineasConsola == null) {
            ReturnToOriginalPosition();
            return;
        }

        transform.SetParent(lineasConsola, false);
        rectTransform.SetAsLastSibling();
        rectTransform.localScale = Vector3.one;

        if (!consoleLines.Contains(this)) {
            consoleLines.Add(this);
        }
        UpdateLineNumbers();

        if (draggedFromPalette && contenedorBloques != null && prefabOriginal != null) {
            BloqueCodigo nuevo = Instantiate(prefabOriginal, contenedorBloques, false);
            nuevo.codeText = this.codeText;
            nuevo.commandType = this.commandType;
            nuevo.UpdateBlockText();

            RectTransform nrt = nuevo.GetComponent<RectTransform>();
            if (nrt != null) {
                ApplyTemplateToRect(nrt);

                if (puntoSpawnBloques != null) {
                    nrt.anchoredPosition = GetSpawnAnchoredPosition();
                } else {
                    nrt.anchoredPosition = originalAnchoredPosition;
                }
                nrt.localScale = Vector3.one;
            }

            var lineNumTxt = nuevo.transform.Find("TextoNumeroLinea")?.GetComponent<TextMeshProUGUI>();
            if (lineNumTxt) lineNumTxt.text = string.Empty;

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


    public static void UpdateLineNumbers() {
        for (int i = consoleLines.Count - 1; i >= 0; i--) {
            if (consoleLines[i] == null) {
                consoleLines.RemoveAt(i);
            }
        }

        for (int i = 0; i < consoleLines.Count; i++) {
            consoleLines[i].lineNumber = i + 1;
            Transform lineNum = consoleLines[i].transform.Find("TextoNumeroLinea");
            if (lineNum) {
                lineNum.GetComponent<TextMeshProUGUI>().text = (i + 1) + ": ";
            }
        }
    }
}
