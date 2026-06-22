using TMPro;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CodePaletteBuilder : MonoBehaviour {
    [Header("Referencias")]
    [SerializeField] private PlayerCodeInventory inventory;
    [SerializeField] private Transform contenedorBloques;
    [SerializeField] private BloqueCodigo bloquePrefab;
    [SerializeField] private RectTransform consolaDropZone;
    [SerializeField] private Transform lineasConsolaTarget;
    
    private RectTransform bloquesCodigo; // Referencia al padre BloquesCodigo

    [Header("Comportamiento")]
    [SerializeField] private bool refreshOnEnable = true;

    private readonly List<CodeBlockData> combatExclusiveBlocks = new List<CodeBlockData>();

    void Awake() {
        AutoAssignReferences();
        if (inventory == null) {
            inventory = FindObjectOfType<PlayerCodeInventory>();
        }
    }

    void OnEnable() {
        if (refreshOnEnable) {
            RefreshPalette();
        }
    }

    public void RefreshPalette() {
        if (!EnsurePaletteLayout()) {
            return;
        }

        if (inventory == null || contenedorBloques == null || bloquePrefab == null) {
            Debug.LogWarning("CodePaletteBuilder: faltan referencias para construir la paleta (inventory/contenedorBloques/bloquePrefab).");
            return;
        }

        HideTemplateBlockIfNeeded();

        ClearPalette();

        List<CodeBlockData> paletteBlocks = BuildPaletteBlockList();
        for (int i = 0; i < paletteBlocks.Count; i++) {
            CodeBlockData data = paletteBlocks[i];
            if (data == null) continue;

            BloqueCodigo block = Instantiate(bloquePrefab, contenedorBloques, false);
            block.gameObject.SetActive(true);
            block.Setup(data.displayText, data.commandType, data.blockId);
            block.contenedorBloques = contenedorBloques;
            block.prefabOriginal = bloquePrefab;
            block.consolaDropZone = consolaDropZone;
            block.lineasConsolaTarget = lineasConsolaTarget;

            RectTransform blockRect = block.GetComponent<RectTransform>();
            if (blockRect != null) {
                blockRect.anchoredPosition = Vector2.zero;
                blockRect.localScale = Vector3.one;
            }

            LayoutElement le = block.GetComponent<LayoutElement>();
            if (le != null) {
                le.ignoreLayout = false;
            }

            var lineNumTxt = block.transform.Find("TextoNumeroLinea")?.GetComponent<TextMeshProUGUI>();
            if (lineNumTxt != null) {
                lineNumTxt.text = string.Empty;
                lineNumTxt.gameObject.SetActive(false);
            }
        }

        RectTransform contentRect = contenedorBloques as RectTransform;
        if (contentRect != null) {
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
        }
    }

    public void SetCombatExclusiveBlocks(IReadOnlyList<CodeBlockData> temporaryBlocks) {
        combatExclusiveBlocks.Clear();

        if (temporaryBlocks != null) {
            for (int i = 0; i < temporaryBlocks.Count; i++) {
                CodeBlockData block = temporaryBlocks[i];
                if (block == null || string.IsNullOrWhiteSpace(block.blockId)) continue;
                combatExclusiveBlocks.Add(block);
            }
        }

        RefreshPalette();
    }

    public BloqueCodigo GetBlockPrefab() {
        return bloquePrefab;
    }

    public Transform GetConsoleLineTarget() {
        return lineasConsolaTarget;
    }

    public void UnlockAndRefresh(CodeBlockData block) {
        if (inventory == null || block == null) return;

        if (inventory.UnlockBlock(block)) {
            RefreshPalette();
        }
    }

    public void UnlockByIdAndRefresh(string blockId) {
        if (inventory == null) return;

        if (inventory.UnlockBlock(blockId)) {
            RefreshPalette();
        }
    }

    private void ClearPalette() {
        for (int i = contenedorBloques.childCount - 1; i >= 0; i--) {
            Transform child = contenedorBloques.GetChild(i);
            if (bloquePrefab != null && child == bloquePrefab.transform) {
                continue;
            }
            Destroy(child.gameObject);
        }
    }

    private void HideTemplateBlockIfNeeded() {
        if (bloquePrefab == null) return;

        LayoutElement le = bloquePrefab.GetComponent<LayoutElement>();
        if (le != null) {
            le.ignoreLayout = true;
        }

        if (bloquePrefab.gameObject.activeSelf) {
            bloquePrefab.gameObject.SetActive(false);
        }
    }

    private void AutoAssignReferences() {
        // Buscar BloquesCodigo padre
        if (bloquesCodigo == null) {
            GameObject go = GameObject.Find("BloquesCodigo");
            if (go != null) bloquesCodigo = go.GetComponent<RectTransform>();
        }

        // Si la referencia actual no esta en BloquesCodigo > Viewport, se corrige.
        if (contenedorBloques != null) {
            bool isValid = contenedorBloques.parent != null
                           && contenedorBloques.parent.name == "Viewport"
                           && contenedorBloques.parent.parent != null
                           && contenedorBloques.parent.parent.name == "BloquesCodigo";
            if (!isValid) {
                contenedorBloques = null;
            }
        }

        // Buscar ContenedorBloques dentro de BloquesCodigo > Viewport
        if (contenedorBloques == null && bloquesCodigo != null) {
            Transform viewport = bloquesCodigo.Find("Viewport");
            if (viewport != null) {
                Transform contenedor = viewport.Find("ContenedorBloques");
                if (contenedor != null) {
                    contenedorBloques = contenedor;
                }
            }
        }

        if (lineasConsolaTarget == null) {
            GameObject root = GameObject.Find("CombateUI-Consola");
            if (root != null) {
                Transform consola = root.transform.Find("Consola");
                if (consola != null) {
                    Transform direct = consola.Find("LineasConsola");
                    if (direct != null) {
                        lineasConsolaTarget = direct;
                    } else {
                        Transform viewport = consola.Find("Viewport");
                        if (viewport != null) {
                            lineasConsolaTarget = viewport.Find("LineasConsola");
                        }
                    }

                    if (consolaDropZone == null) {
                        consolaDropZone = consola as RectTransform;
                    }
                }
            }
        }

        if (bloquePrefab == null) {
            BloqueCodigo[] blocks = FindObjectsOfType<BloqueCodigo>(true);
            foreach (BloqueCodigo b in blocks) {
                if (b.name.Contains("BloqueCodigoPrefab")) {
                    bloquePrefab = b;
                    break;
                }
            }

            if (bloquePrefab == null && blocks.Length > 0) {
                bloquePrefab = blocks[0];
            }
        }
    }

    private bool EnsurePaletteLayout() {
        AutoAssignReferences();

        // La estructura se crea manualmente en Unity.
        // Aqui solo validamos/configuramos componentes.
        if (contenedorBloques == null) {
            Debug.LogError("CodePaletteBuilder: ContenedorBloques no está asignado. " +
                           "Crea la estructura manualmente:\n" +
                           "BloquesCodigo > Viewport > ContenedorBloques");
            return false;
        }

        RectTransform contentRect = contenedorBloques as RectTransform;
        if (contentRect == null) {
            Debug.LogError("CodePaletteBuilder: ContenedorBloques debe ser RectTransform.");
            return false;
        }

        Transform viewport = contenedorBloques.parent;
        RectTransform viewportRect = viewport as RectTransform;
        if (viewportRect == null || viewport.name != "Viewport") {
            Debug.LogError("CodePaletteBuilder: ContenedorBloques debe estar dentro de Viewport.");
            return false;
        }

        RectMask2D mask = viewport.GetComponent<RectMask2D>();
        if (mask == null) {
            viewport.gameObject.AddComponent<RectMask2D>();
        }

        Transform parent = viewport.parent;
        RectTransform bloquesRect = parent as RectTransform;
        if (bloquesRect == null || parent.name != "BloquesCodigo") {
            Debug.LogError("CodePaletteBuilder: Viewport debe estar dentro de BloquesCodigo.");
            return false;
        }

        ScrollRect scroll = bloquesRect.GetComponent<ScrollRect>();
        if (scroll == null) {
            Debug.LogError("CodePaletteBuilder: BloquesCodigo necesita componente ScrollRect.");
            return false;
        }

        scroll.viewport = viewportRect;
        scroll.content = contentRect;
        scroll.vertical = true;
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;

        VerticalLayoutGroup vlg = contenedorBloques.GetComponent<VerticalLayoutGroup>();
        bool createdVlg = false;
        if (vlg == null) {
            vlg = contenedorBloques.gameObject.AddComponent<VerticalLayoutGroup>();
            createdVlg = true;
        }

        // Respetar valores del Inspector si el componente ya existia.
        if (createdVlg) {
            vlg.childAlignment = TextAnchor.UpperLeft;
            vlg.spacing = 6f;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
        }

        ContentSizeFitter fitter = contenedorBloques.GetComponent<ContentSizeFitter>();
        if (fitter == null) {
            fitter = contenedorBloques.gameObject.AddComponent<ContentSizeFitter>();
        }
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;

        return true;
    }

    private List<CodeBlockData> BuildPaletteBlockList() {
        List<CodeBlockData> merged = new List<CodeBlockData>();
        HashSet<string> usedIds = new HashSet<string>();

        for (int i = 0; i < combatExclusiveBlocks.Count; i++) {
            CodeBlockData data = combatExclusiveBlocks[i];
            if (data == null || string.IsNullOrWhiteSpace(data.blockId)) continue;

            if (usedIds.Add(data.blockId)) {
                merged.Add(data);
            }
        }

        if (inventory != null) {
            var unlocked = inventory.GetUnlockedBlocks();
            for (int i = 0; i < unlocked.Count; i++) {
                CodeBlockData data = unlocked[i];
                if (data == null || string.IsNullOrWhiteSpace(data.blockId)) continue;

                if (usedIds.Add(data.blockId)) {
                    merged.Add(data);
                }
            }
        }

        return merged;
    }
}
