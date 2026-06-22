using UnityEngine;
using System.Collections;

public class TriggerDeactivateObject : MonoBehaviour
{
    [SerializeField] private GameObject targetObject;
    [SerializeField] private bool deactivateOnce = false;
    [SerializeField] private bool stayInactive = false;
    [SerializeField] private string requiredTag = "Player";

    [Header("Event Filter")]
    [SerializeField] private bool useEventFilter = false;
    [SerializeField] private string requiredEventId = "";
    [SerializeField] private bool requireEventCompleted = false;
    [SerializeField] private bool requireEventIncomplete = false; // Evento no completado (independiente de si esta pendiente)

    private bool hasBeenDeactivated = false;
    private bool isPlayerInsideTrigger = false;
    private Collider2D triggerCollider;

    private void Awake()
    {
        triggerCollider = GetComponent<Collider2D>();
    }

    private void Start()
    {
        StartCoroutine(ApplyInitialStateDeferred());
    }

    private void Update()
    {
        SyncPresenceAndApply();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        HandleEnter(other.gameObject);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        HandleStay(other.gameObject);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        HandleExit(other.gameObject);
    }

    private void HandleEnter(GameObject otherObject)
    {
        if (!MatchesRequiredTag(otherObject))
        {
            return;
        }

        isPlayerInsideTrigger = true;

        ApplyIfAllowed();
    }

    private void HandleStay(GameObject otherObject)
    {
        if (!MatchesRequiredTag(otherObject))
        {
            return;
        }

        isPlayerInsideTrigger = true;
        ApplyIfAllowed();
    }

    private void ApplyInitialState()
    {
        if (!isPlayerInsideTrigger)
        {
            return;
        }

        ApplyIfAllowed();
    }

    private IEnumerator ApplyInitialStateDeferred()
    {
        yield return new WaitForFixedUpdate();
        RefreshPlayerInsideState();
        ApplyInitialState();
    }

    private void RefreshPlayerInsideState()
    {
        isPlayerInsideTrigger = IsPlayerCurrentlyInsideTrigger();
    }

    private void SyncPresenceAndApply()
    {
        bool wasInside = isPlayerInsideTrigger;
        bool isInsideNow = IsPlayerCurrentlyInsideTrigger();

        isPlayerInsideTrigger = isInsideNow;

        if (isInsideNow)
        {
            ApplyIfAllowed();
            return;
        }

        if (wasInside && !stayInactive)
        {
            SetTargetActive(true);
        }
    }

    private void ApplyIfAllowed()
    {
        if (!isPlayerInsideTrigger)
        {
            return;
        }

        if (!ShouldApplyByEventFilter())
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

    private bool ShouldApplyByEventFilter()
    {
        if (!useEventFilter)
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(requiredEventId) || SaveManager.Instance == null)
        {
            return false;
        }

        bool completed = SaveManager.Instance.WasEventTriggered(requiredEventId);

        if (requireEventCompleted && requireEventIncomplete)
        {
            return false;
        }

        if (requireEventIncomplete)
        {
            return !completed;
        }

        if (requireEventCompleted)
        {
            return completed;
        }

        return !completed;
    }

    private bool IsPlayerCurrentlyInsideTrigger()
    {
        if (triggerCollider == null)
        {
            return false;
        }

        GameObject playerObject = GameObject.FindGameObjectWithTag(requiredTag);
        if (playerObject == null)
        {
            return false;
        }

        Collider2D playerCollider = playerObject.GetComponentInChildren<Collider2D>();
        if (playerCollider == null)
        {
            return false;
        }

        return triggerCollider.bounds.Intersects(playerCollider.bounds);
    }

    private void HandleExit(GameObject otherObject)
    {
        if (!MatchesRequiredTag(otherObject))
        {
            return;
        }

        isPlayerInsideTrigger = false;

        if (stayInactive)
        {
            return;
        }

        if (ShouldApplyByEventFilter())
        {
            return;
        }

        SetTargetActive(true);
    }

    private bool MatchesRequiredTag(GameObject otherObject)
    {
        return otherObject.CompareTag(requiredTag);
    }

    private void SetTargetActive(bool activeState)
    {
        if (targetObject != null)
        {
            targetObject.SetActive(activeState);
        }
    }
}
