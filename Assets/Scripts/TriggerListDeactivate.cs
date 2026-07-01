using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TriggerListDeactivate : MonoBehaviour
{
    [SerializeField] private GameObject targetObject;
    [SerializeField] private bool deactivateOnce = false;
    [SerializeField] private bool stayInactive = false;
    [SerializeField] private string requiredTag = "Player";

    [Header("Event Filter")]
    [SerializeField] private bool useEventFilter = false;
    [SerializeField] private List<EventRequirement> requiredEvents = new();
    public enum EventCheckMode
    {
        All,
        Any
    }
    [SerializeField] private EventCheckMode checkMode = EventCheckMode.All;

    private bool hasBeenDeactivated = false;
    private bool isPlayerInsideTrigger = false;
    private Collider2D triggerCollider;

    [System.Serializable]
    public class EventRequirement
    {
        public string eventId;

        [Header("Condition")]
        public bool requireCompleted;
        public bool requireIncomplete;
    }

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
            return true;

        if (SaveManager.Instance == null)
            return false;

        if (requiredEvents == null || requiredEvents.Count == 0)
            return false;

        if (checkMode == EventCheckMode.All)
        {
            foreach (var requirement in requiredEvents)
            {
                if (!EvaluateRequirement(requirement))
                    return false;
            }

            return true;
        }
        else
        {
            foreach (var requirement in requiredEvents)
            {
                if (EvaluateRequirement(requirement))
                    return true;
            }

            return false;
        }
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

    private bool EvaluateRequirement(EventRequirement requirement)
    {
        if (string.IsNullOrWhiteSpace(requirement.eventId))
            return false;

        bool completed = SaveManager.Instance.WasEventTriggered(requirement.eventId);

        if (requirement.requireCompleted && requirement.requireIncomplete)
            return false;

        if (requirement.requireCompleted)
            return completed;

        if (requirement.requireIncomplete)
            return !completed;

        // Si no se marca ninguna opción, por defecto exige que NO esté completado
        return !completed;
    }
}
