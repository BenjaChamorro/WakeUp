using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class FlatTextInputHandler : MonoBehaviour, IDropHandler {
    private TMP_InputField inputField;

    private void Awake() {
        inputField = GetComponent<TMP_InputField>();
    }

    public void OnDrop(PointerEventData eventData) {
        if (eventData.pointerDrag == null) return;
        if (inputField == null) return;

        BloqueCodigo dragged = eventData.pointerDrag.GetComponent<BloqueCodigo>();
        if (dragged == null) return;

        string draggedType = dragged.commandType != null ? dragged.commandType.Trim().ToLowerInvariant() : string.Empty;
        
        // Los bloques flatText se insertan directamente en el input
        if (draggedType == "flattext") {
            inputField.text = dragged.codeText;
            inputField.caretPosition = inputField.text.Length;
        }
    }
}
