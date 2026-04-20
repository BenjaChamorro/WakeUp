using UnityEngine;
using UnityEngine.InputSystem;

public class OnPressDeactivate : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private Key activationKey = Key.E;

    [Header("Trigger Filter")]
    [SerializeField] private bool requireTag = true;
    [SerializeField] private string requiredTag = "Player";

    [Header("Targets")]
    [SerializeField] private Behaviour[] componentsToDisable;
    [SerializeField] private GameObject[] objectsToDeactivate;

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
            ExecuteDeactivation();
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

    private void ExecuteDeactivation()
    {
        if (componentsToDisable != null)
        {
            foreach (Behaviour component in componentsToDisable)
            {
                if (component != null)
                {
                    component.enabled = false;
                }
            }
        }

        if (objectsToDeactivate != null)
        {
            foreach (GameObject targetObject in objectsToDeactivate)
            {
                if (targetObject != null)
                {
                    targetObject.SetActive(false);
                }
            }
        }

        hasExecuted = true;
    }
}
