using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text;
using System.Collections;

public class CombateUIFlowController : MonoBehaviour {
    private const int IndentSizePixels = 24;
    private const int SpacesPerIndent = 4;
    private const float VictoryMessageDelaySeconds = 4f;

    [Header("Paneles")]
    public GameObject combateUI;
    public GameObject combateUIConsola;

    [Header("Botones (opcional)")]
    public Button botonScript;
    public Button botonVolver;
    public Button botonBorrar;
    public Button botonEjecutar;

    [Header("Referencias de Consola")]
    [SerializeField] private Transform lineasConsola;

    private bool victorySequenceStarted;

    void Awake() {
        AutoAssignReferences();
        SetInitialState();
    }

    void Start() {
        if (botonScript != null) {
            botonScript.onClick.AddListener(OpenScriptConsole);
        }

        if (botonVolver != null) {
            botonVolver.onClick.AddListener(BackToCombatUI);
        }

        if (botonBorrar != null) {
            botonBorrar.onClick.AddListener(OnBorrar);
        }

        if (botonEjecutar != null) {
            botonEjecutar.onClick.AddListener(OnEjecutar);
        }
    }

    void OnDestroy() {
        if (botonScript != null) {
            botonScript.onClick.RemoveListener(OpenScriptConsole);
        }

        if (botonVolver != null) {
            botonVolver.onClick.RemoveListener(BackToCombatUI);
        }

        if (botonBorrar != null) {
            botonBorrar.onClick.RemoveListener(OnBorrar);
        }

        if (botonEjecutar != null) {
            botonEjecutar.onClick.RemoveListener(OnEjecutar);
        }
    }

    public void SetInitialState() {
        if (combateUI != null) combateUI.SetActive(true);
        if (combateUIConsola != null) combateUIConsola.SetActive(false);
    }

    public void OpenScriptConsole() {
        if (combateUI != null) combateUI.SetActive(false);
        if (combateUIConsola != null) combateUIConsola.SetActive(true);

        CodePaletteBuilder paletteBuilder = FindObjectOfType<CodePaletteBuilder>(true);
        if (paletteBuilder != null) {
            paletteBuilder.RefreshPalette();
        }
    }

    public void BackToCombatUI() {
        if (combateUI != null) combateUI.SetActive(true);
        if (combateUIConsola != null) combateUIConsola.SetActive(false);
    }

    public void OnBorrar() {
        Transform consolaLineas = ResolveLineasConsola();
        if (consolaLineas == null) {
            Debug.LogWarning("No se encontró LineasConsola para limpiar");
            return;
        }

        // Destruir todos los bloques de código
        int childCount = consolaLineas.childCount;
        for (int i = childCount - 1; i >= 0; i--) {
            Destroy(consolaLineas.GetChild(i).gameObject);
        }
    }

    public void OnEjecutar() {
        if (victorySequenceStarted) {
            return;
        }

        string code = ReadConsoleCode();
        Debug.Log("[CodeRunner] Código leído:\n" + code);

        EnemyCombatRuntime enemyRuntime = FindObjectOfType<EnemyCombatRuntime>(true);
        if (enemyRuntime == null) {
            Debug.LogWarning("[CodeRunner] No se encontró EnemyCombatRuntime para evaluar la victoria.");
            return;
        }

        if (enemyRuntime.TryEvaluateVictory(code, out string victoryReason)) {
            Debug.Log("[CodeRunner] Victoria lograda: " + victoryReason);
            StartCoroutine(HandleVictorySequence(enemyRuntime));
        } else {
            Debug.Log("[CodeRunner] Aún no cumple la condición de victoria: " + victoryReason);
        }
    }

    IEnumerator HandleVictorySequence(EnemyCombatRuntime enemyRuntime) {
        victorySequenceStarted = true;

        // Mantener misma navegación que el botón Volver y luego bloquear interacción.
        BackToCombatUI();
        SetAllButtonsInteractable(false);

        if (enemyRuntime != null) {
            enemyRuntime.ShowDefeatDialogue();
            enemyRuntime.PlayDefeatAnimation();
        }

        yield return new WaitForSeconds(VictoryMessageDelaySeconds);

        if (enemyRuntime != null) {
            enemyRuntime.ShowEnemyDefeatedMessage();
        }
    }

    void SetAllButtonsInteractable(bool interactable) {
        Button[] allButtons = FindObjectsOfType<Button>(true);
        for (int i = 0; i < allButtons.Length; i++) {
            if (allButtons[i] != null) {
                allButtons[i].interactable = interactable;
            }
        }
    }

    string ReadConsoleCode() {
        Transform consolaLineas = ResolveLineasConsola();
        if (consolaLineas == null) {
            return string.Empty;
        }

        StringBuilder code = new StringBuilder();
        bool firstLine = true;

        for (int i = 0; i < consolaLineas.childCount; i++) {
            Transform child = consolaLineas.GetChild(i);
            BloqueCodigo bloque = child.GetComponent<BloqueCodigo>();
            if (bloque == null) {
                continue;
            }

            string lineText = ReadLineText(bloque.transform).TrimEnd();
            int indentLevel = GetLineIndentLevel(bloque.transform);
            string indent = new string(' ', indentLevel * SpacesPerIndent);
            if (!firstLine) {
                code.Append('\n');
            }
            code.Append(indent);
            code.Append(lineText);
            firstLine = false;
        }

        return code.ToString();
    }

    string ReadLineText(Transform bloqueRoot) {
        if (bloqueRoot == null) return string.Empty;

        StringBuilder line = new StringBuilder();

        // Si hay template inline, se reconstruye desde su contenedor; si no, se usa el texto principal.
        Transform template = bloqueRoot.Find("LineTemplateContainer");
        if (template != null && template.gameObject.activeInHierarchy) {
            for (int i = 0; i < template.childCount; i++) {
                AppendNodeText(template.GetChild(i), line);
            }
        } else {
            TextMeshProUGUI mainText = bloqueRoot.Find("TextoBloqueCodigo")?.GetComponent<TextMeshProUGUI>();
            if (mainText != null) {
                line.Append(mainText.text);
            }
        }

        return line.ToString();
    }

    void AppendNodeText(Transform node, StringBuilder output) {
        if (node == null || output == null) return;

        // El botón '+' de arrays es solo UI para agregar elementos, no parte del código.
        if (node.GetComponent<Button>() != null || node.name == "AddButton") {
            return;
        }

        AssignmentValueSlot assignmentSlot = node.GetComponent<AssignmentValueSlot>();
        if (assignmentSlot != null) {
            AppendAssignmentValueSlot(node, output);
            return;
        }

        TMP_InputField input = node.GetComponent<TMP_InputField>();
        if (input != null) {
            output.Append(string.IsNullOrEmpty(input.text) ? "_" : input.text);
            return;
        }

        TextMeshProUGUI text = node.GetComponent<TextMeshProUGUI>();
        if (text != null && node.name != "TextoNumeroLinea" && node.name != "Placeholder" && text.gameObject.activeInHierarchy) {
            output.Append(text.text);
            return;
        }

        for (int i = 0; i < node.childCount; i++) {
            AppendNodeText(node.GetChild(i), output);
        }
    }

    void AppendAssignmentValueSlot(Transform slotNode, StringBuilder output) {
        if (slotNode == null || output == null) return;

        Transform dynamicContent = slotNode.Find("AssignmentContent");
        if (dynamicContent != null && dynamicContent.gameObject.activeInHierarchy) {
            for (int i = 0; i < dynamicContent.childCount; i++) {
                AppendNodeText(dynamicContent.GetChild(i), output);
            }
            return;
        }

        TMP_InputField baseInput = slotNode.GetComponent<TMP_InputField>();
        if (baseInput != null) {
            output.Append(string.IsNullOrEmpty(baseInput.text) ? "_" : baseInput.text);
        }
    }

    int GetLineIndentLevel(Transform lineRoot) {
        if (lineRoot == null) return 0;

        HorizontalLayoutGroup rowLayout = lineRoot.GetComponent<HorizontalLayoutGroup>();
        if (rowLayout == null || rowLayout.padding == null) return 0;

        int paddingLeft = Mathf.Max(0, rowLayout.padding.left);
        if (paddingLeft <= 0) return 0;

        return Mathf.Max(0, Mathf.RoundToInt((float)paddingLeft / IndentSizePixels));
    }

    void AutoAssignReferences() {
        if (combateUI == null) {
            GameObject go = GameObject.Find("CombateUI");
            if (go != null) combateUI = go;
        }

        if (combateUIConsola == null) {
            GameObject go = GameObject.Find("CombateUI-Consola");
            if (go != null) combateUIConsola = go;
        }

        if (botonScript == null) botonScript = FindButtonByName("BotonScript");
        if (botonVolver == null) botonVolver = FindButtonByName("BotonVolver");
        if (botonBorrar == null) botonBorrar = FindButtonByName("BotonBorrar");
        if (botonEjecutar == null) botonEjecutar = FindButtonByName("BotonEjecutar");

        if (lineasConsola == null) {
            lineasConsola = FindLineasConsola();
        }
    }

    Button FindButtonByName(string buttonName) {
        Button[] buttons = FindObjectsOfType<Button>(true);
        foreach (Button b in buttons) {
            if (b.name == buttonName) {
                return b;
            }
        }

        return null;
    }

    Transform ResolveLineasConsola() {
        if (lineasConsola != null) {
            return lineasConsola;
        }

        Transform foundLineas = FindLineasConsola();
        if (foundLineas != null) {
            lineasConsola = foundLineas;
        }
        return foundLineas;
    }

    Transform FindLineasConsola() {
        // Buscar bajo CombateUI-Consola
        GameObject consoleGO = combateUIConsola ?? GameObject.Find("CombateUI-Consola");
        if (consoleGO == null) return null;

        Transform consola = consoleGO.transform;

        // Buscar directamente como hijo
        Transform direct = consola.Find("LineasConsola");
        if (direct != null) return direct;

        // Buscar bajo viewport (ScrollRect)
        Transform viewport = consola.Find("viewport");
        if (viewport != null) {
            Transform underViewport = viewport.Find("LineasConsola");
            if (underViewport != null) return underViewport;
        }

        // Búsqueda recursiva por nombre
        foreach (Transform child in consola.GetComponentsInChildren<Transform>()) {
            if (child.name == "LineasConsola") {
                return child;
            }
        }

        return null;
    }
}
