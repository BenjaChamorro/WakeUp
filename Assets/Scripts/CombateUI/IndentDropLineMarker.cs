using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class IndentDropLineMarker : MonoBehaviour {
    private const int IndentSizePixels = 24;

    [HideInInspector] public BloqueCodigo owner;
    [HideInInspector] public int indentLevel = 1;

    public void RefreshVisual() {
        HorizontalLayoutGroup hlg = GetComponent<HorizontalLayoutGroup>();
        if (hlg != null) {
            hlg.padding = new RectOffset(Mathf.Max(0, indentLevel) * IndentSizePixels, 0, 0, 0);
        }

        TextMeshProUGUI text = GetComponentInChildren<TextMeshProUGUI>(true);
        if (text != null) {
            text.text = "[arrastra bloque aqui]";
        }
    }

    void Update() {
        // Si el bloque dueño ya no existe, eliminar el marcador huérfano.
        if (owner == null) {
            Destroy(gameObject);
        }
    }
}
