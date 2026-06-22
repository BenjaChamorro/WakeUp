using UnityEngine;
using UnityEngine.Events;

public class TriggerFunction : MonoBehaviour
{
    [Header("Trigger Filter")]
    [SerializeField] private bool requireTag = true;
    [SerializeField] private string requiredTag = "Player";

    [Header("Action")]
    [SerializeField] private UnityEvent onTriggered;

    [Header("Options")]
    [SerializeField] private bool triggerOnlyOnce = true;

    private bool hasTriggered;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggerOnlyOnce && hasTriggered)
        {
            return;
        }

        if (!IsValidTriggerSource(other))
        {
            return;
        }

        onTriggered?.Invoke();
        hasTriggered = true;
    }

    private bool IsValidTriggerSource(Collider2D other)
    {
        if (!requireTag)
        {
            return true;
        }

        return other.CompareTag(requiredTag);
    }
}
