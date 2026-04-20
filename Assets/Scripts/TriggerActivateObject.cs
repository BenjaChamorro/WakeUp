using UnityEngine;

public class TriggerActivateObject : MonoBehaviour
{
    [SerializeField] private GameObject targetObject;
    [SerializeField] private bool activateOnce = false;
    [SerializeField] private bool stayActive = false;
    [SerializeField] private string playerTag = "Player";

    private bool hasBeenActivated = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        HandleEnter(other.gameObject);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        HandleExit(other.gameObject);
    }

    private void HandleEnter(GameObject otherObject)
    {
        if (!IsPlayer(otherObject))
        {
            return;
        }

        if (activateOnce && hasBeenActivated)
        {
            return;
        }

        SetTargetActive(true);

        if (activateOnce)
        {
            hasBeenActivated = true;
        }
    }

    private void HandleExit(GameObject otherObject)
    {
        if (!IsPlayer(otherObject))
        {
            return;
        }

        if (stayActive)
        {
            return;
        }

        SetTargetActive(false);
    }

    private bool IsPlayer(GameObject otherObject)
    {
        return otherObject.CompareTag(playerTag);
    }

    private void SetTargetActive(bool activeState)
    {
        if (targetObject != null)
        {
            targetObject.SetActive(activeState);
        }
    }
}
