using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class OnPressActivate : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private Key activationKey = Key.E;

    [Header("Events")]
    [SerializeField] private UnityEvent onPressed;

    [Header("Trigger Filter")]
    [SerializeField] private bool requireTag = true;
    [SerializeField] private string requiredTag = "Player";

    [Header("Targets")]
    [SerializeField] private Behaviour[] componentsToEnable;
    [SerializeField] private GameObject[] objectsToActivate;

    [Header("Options")]
    [SerializeField] private bool oneShot = true;

    private int validOverlaps = 0;
    private bool hasExecuted = false;

    private void Update()
    {
        if (oneShot && hasExecuted)
        {
            return;
        }

        if (validOverlaps <= 0)
        {
            return;
        }

        if (Keyboard.current != null && Keyboard.current[activationKey].wasPressedThisFrame)
        {
            ExecuteActivation();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsValidTriggerSource(other))
        {
            return;
        }

        validOverlaps++;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!IsValidTriggerSource(other))
        {
            return;
        }

        validOverlaps = Mathf.Max(0, validOverlaps - 1);
    }

    private bool IsValidTriggerSource(Collider2D other)
    {
        if (!requireTag)
        {
            return true;
        }

        return other.CompareTag(requiredTag);
    }

    private void ExecuteActivation()
    {
        onPressed?.Invoke();

        if (componentsToEnable != null)
        {
            foreach (Behaviour component in componentsToEnable)
            {
                if (component != null)
                {
                    component.enabled = true;
                }
            }
        }

        if (objectsToActivate != null)
        {
            foreach (GameObject targetObject in objectsToActivate)
            {
                if (targetObject != null)
                {
                    targetObject.SetActive(true);
                }
            }
        }

        hasExecuted = true;
    }
}