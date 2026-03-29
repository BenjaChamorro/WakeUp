using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class OperatorDropSlot : MonoBehaviour, IDropHandler {
    [SerializeField] private TextMeshProUGUI slotText;

    public string CurrentOperator { get; private set; } = string.Empty;

    void Awake() {
        if (slotText == null) {
            slotText = GetComponentInChildren<TextMeshProUGUI>();
        }

        if (slotText != null && string.IsNullOrWhiteSpace(slotText.text)) {
            slotText.text = "op";
        }
    }

    public bool TrySetOperator(string operatorText) {
        if (string.IsNullOrWhiteSpace(operatorText)) return false;

        CurrentOperator = operatorText;
        if (slotText != null) {
            slotText.text = operatorText;
        }

        return true;
    }

    public void OnDrop(PointerEventData eventData) {
        if (eventData.pointerDrag == null) return;

        BloqueCodigo dragged = eventData.pointerDrag.GetComponent<BloqueCodigo>();
        if (dragged == null || dragged.commandType != "operator") return;

        TrySetOperator(dragged.codeText);
    }
}
