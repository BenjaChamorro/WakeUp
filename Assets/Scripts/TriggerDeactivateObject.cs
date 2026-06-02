using UnityEngine;
using System.Collections;

public class TriggerDeactivateObject : MonoBehaviour
{
    [SerializeField] private GameObject targetObject;
    [SerializeField] private bool deactivateOnce = false;
    [SerializeField] private bool stayInactive = false;
    [SerializeField] private string playerTag = "Player";

    [Header("Event Filter")]
    [SerializeField] private bool useEventFilter = false;
    [SerializeField] private string requiredEventId = "";
    [SerializeField] private bool requireEventCompleted = false;
    [SerializeField] private bool requireEventIncomplete = false;

    private bool hasBeenDeactivated = false;

    private void Awake()
    {
    }

    private void Start()
    {
        StartCoroutine(ApplyInitialStateDeferred());
    }

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

        if (useEventFilter && !string.IsNullOrWhiteSpace(requiredEventId) && SaveManager.Instance != null)
        {
            if (!ShouldDeactivateBasedOnEventFilter())
            {
                return;
            }
        }

        if (deactivateOnce && hasBeenDeactivated)
        {
            return;
        }

        SetTargetActive(false);

        if (deactivateOnce)
        {
            hasBeenDeactivated = true;
        }
    }

    private void ApplyInitialState()
    {
        if (!ShouldDeactivateBasedOnEventFilter())
        {
            return;
        }

        if (deactivateOnce && hasBeenDeactivated)
        {
            return;
        }

        SetTargetActive(false);

        if (deactivateOnce)
        {
            hasBeenDeactivated = true;
        }
    }

    private IEnumerator ApplyInitialStateDeferred()
    {
        yield return null;
        ApplyInitialState();
    }

    private bool ShouldDeactivateBasedOnEventFilter()
    {
        if (!useEventFilter || string.IsNullOrWhiteSpace(requiredEventId) || SaveManager.Instance == null)
        {
            return false;
        }

        bool completed = SaveManager.Instance.WasEventTriggered(requiredEventId);

        if (requireEventIncomplete)
        {
            return !completed && !SaveManager.Instance.IsEventPending(requiredEventId);
        }

        if (requireEventCompleted)
        {
            return completed;
        }

        return !completed;
    }

    private void HandleExit(GameObject otherObject)
    {
        if (!IsPlayer(otherObject))
        {
            return;
        }

        if (stayInactive)
        {
            return;
        }

        if (ShouldDeactivateBasedOnEventFilter())
        {
            return;
        }

        SetTargetActive(true);
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
