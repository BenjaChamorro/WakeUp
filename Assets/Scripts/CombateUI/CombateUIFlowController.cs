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
        Debug.Log("Borrar presionado");
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
}
