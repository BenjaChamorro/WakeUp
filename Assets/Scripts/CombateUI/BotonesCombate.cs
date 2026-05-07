using UnityEngine;
using UnityEngine.UI;

public class BotonesCombate : MonoBehaviour
{
    public Button BotonEscape;
    public Button BotonObjeto;
    public Button BotonScript;
    public CombateUIFlowController uiFlowController;

    void Awake() {
        AutoAssignReferences();
    }

    void Start() {
        if (uiFlowController == null) {
            uiFlowController = FindObjectOfType<CombateUIFlowController>();
        }

        if (BotonEscape != null) BotonEscape.onClick.AddListener(OnEscape);
        if (BotonObjeto != null) BotonObjeto.onClick.AddListener(OnObjeto);
        if (BotonScript != null) BotonScript.onClick.AddListener(OnScript);
    }

    void OnDestroy() {
        if (BotonEscape != null) BotonEscape.onClick.RemoveListener(OnEscape);
        if (BotonObjeto != null) BotonObjeto.onClick.RemoveListener(OnObjeto);
        if (BotonScript != null) BotonScript.onClick.RemoveListener(OnScript);
    }

    void OnEscape() {
        // Programar lógica de escape
        Debug.Log("Escapar presionado");
    }

    void OnObjeto() {
        // Programar menú de objetos
        Debug.Log("Objeto presionado");
    }

    void OnScript() {
        Debug.Log("Script presionado");
        if (uiFlowController != null) {
            uiFlowController.OpenScriptConsole();
        }
    }

    void AutoAssignReferences() {
        if (BotonEscape == null) BotonEscape = FindButtonByName("BotonEscape");
        if (BotonObjeto == null) BotonObjeto = FindButtonByName("BotonObjeto");
        if (BotonScript == null) BotonScript = FindButtonByName("BotonScript");
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
