using UnityEngine;
using UnityEngine.UI;

// Botón "Guía" del combate: vuelve a mostrar el diálogo de Clippy del enemigo actual.
// EnemyCombatRuntime lo muestra solo en los combates cuyo enemigo tiene 'dialogoClippy'.
[RequireComponent(typeof(Button))]
public class ClippyGuideButton : MonoBehaviour {
    void Awake() {
        GetComponent<Button>().onClick.AddListener(ShowGuide);
    }

    public void SetAvailable(bool available) {
        gameObject.SetActive(available);
    }

    public void ShowGuide() {
        EnemyCombatRuntime enemyRuntime = FindObjectOfType<EnemyCombatRuntime>(true);
        if (enemyRuntime != null) {
            enemyRuntime.ReplayClippyDialogue();
        }
    }
}
