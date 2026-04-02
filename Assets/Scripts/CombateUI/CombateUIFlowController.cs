using UnityEngine;
using UnityEngine.UI;

public class CombateUIFlowController : MonoBehaviour {
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
        Debug.Log("Ejecutar presionado");
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
