using UnityEngine;
using System.Collections;

public class TriggerActivateObject : MonoBehaviour
{
    [SerializeField] private GameObject targetObject;
    [SerializeField] private bool activateOnce = false;
    [SerializeField] private bool stayActive = false;
    [SerializeField] private string requiredTag = "Player";

    [Header("Event Filter")]
    [SerializeField] private bool useEventFilter = false;
    [SerializeField] private string requiredEventId = "";
    [SerializeField] private bool requireEventCompleted = false;
    [SerializeField] private bool requireEventIncomplete = false; // Evento no completado (independiente de si esta pendiente)

    private bool hasBeenActivated = false;
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

        if (wasInside && !stayActive)
        {
            SetTargetActive(false);
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

        if (stayActive)
        {
            return;
        }

        SetTargetActive(false);
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
