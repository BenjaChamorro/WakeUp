using UnityEngine;

public class TriggerActivateObject : MonoBehaviour
{
    [SerializeField] private GameObject targetObject;
    [SerializeField] private string playerTag = "Player";

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
        if (IsPlayer(otherObject))
        {
            SetTargetActive(true);
        }
    }

    private void HandleExit(GameObject otherObject)
    {
        if (IsPlayer(otherObject))
        {
            SetTargetActive(false);
        }
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
