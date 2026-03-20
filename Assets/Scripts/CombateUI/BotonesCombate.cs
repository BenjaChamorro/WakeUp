using UnityEngine;
using UnityEngine.UI;

public class BotonesCombate : MonoBehaviour
{
    public Button BotonEscape;
    public Button BotonObjeto;
    public Button BotonScript;

    void Start() {
        BotonEscape.onClick.AddListener(OnEscape);
        BotonObjeto.onClick.AddListener(OnObjeto);
        BotonScript.onClick.AddListener(OnScript);
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
        // Programar menú scripts
        Debug.Log("Script presionado");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
