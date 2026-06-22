using UnityEngine;

public class TriggerScriptActivate : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool triggerOnlyOnce = true;

    [Header("Event Phase Control")]
    [SerializeField] private EventPhaseState eventPhase;
    [SerializeField] private bool isFirstTriggerInSequence = false;
    [SerializeField] private bool isLastTriggerInSequence = false;
    
    [Header("Event Filter")]
    [SerializeField] private bool useEventFilter = false;
    [SerializeField] private string requiredEventId = "";
    [SerializeField] private bool requireEventCompleted = false; // if true -> trigger only when event is completed; if false -> trigger only when event is NOT completed

    [Header("NPC Movement")]
    [SerializeField] private MoveNpcTo npcMovement;
    [SerializeField] private MonoBehaviour[] playerScriptsToBlockWhileNpcMoves;


    [Header("Scripts to Enable")]
    [SerializeField] private MonoBehaviour[] scriptsToEnable;

    [Header("Scripts to Disable")]
    [SerializeField] private MonoBehaviour[] scriptsToDisable;
    [SerializeField] private MonoBehaviour[] scriptsToEnablePostMovement;

    private bool hasTriggered;

    private void Awake()
    {
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag))
        {
            return;
        }

        // Optional event-based filter
        if (useEventFilter && !string.IsNullOrWhiteSpace(requiredEventId) && SaveManager.Instance != null)
        {
            bool completed = SaveManager.Instance.WasEventTriggered(requiredEventId);
            if (requireEventCompleted && !completed)
            {
                return; // requires event completed but it's not
            }
            if (!requireEventCompleted && completed)
            {
                return; // requires event NOT completed but it is
            }
        }

        if (triggerOnlyOnce && hasTriggered)
        {
            return;
        }

        // Iniciar fase del evento si es el primer trigger
        if (isFirstTriggerInSequence && eventPhase != null)
        {
            eventPhase.BeginPhase();
        }

        EnableScripts();
        DisableScripts();

        StartNpcMovement();

        hasTriggered = true;

        // Completar fase del evento si es el último trigger
        if (isLastTriggerInSequence && eventPhase != null)
        {
            eventPhase.CompletePhase();
        }
    }

    private void OnDisable()
    {
        if (npcMovement != null)
        {
            npcMovement.MovimientoFinalizado -= OnNpcMovementFinished;
        }
    }

    private void StartNpcMovement()
    {
        if (npcMovement == null)
        {
            return;
        }

        npcMovement.MovimientoFinalizado -= OnNpcMovementFinished;
        npcMovement.MovimientoFinalizado += OnNpcMovementFinished;

        SetPlayerScriptsBlocked(true);
        npcMovement.IniciarMovimiento();

        if (!npcMovement.EstaMoviendo)
        {
            SetPlayerScriptsBlocked(false);
            EnablePostMovementScripts();
            npcMovement.MovimientoFinalizado -= OnNpcMovementFinished;
        }
    }

    private void OnNpcMovementFinished()
    {
        SetPlayerScriptsBlocked(false);
        EnablePostMovementScripts();

        if (npcMovement != null)
        {
            npcMovement.MovimientoFinalizado -= OnNpcMovementFinished;
        }
    }

    private void SetPlayerScriptsBlocked(bool blocked)
    {
        if (playerScriptsToBlockWhileNpcMoves == null)
        {
            return;
        }

        for (int i = 0; i < playerScriptsToBlockWhileNpcMoves.Length; i++)
        {
            MonoBehaviour script = playerScriptsToBlockWhileNpcMoves[i];
            if (script != null)
            {
                script.enabled = !blocked;
            }
        }
    }

    private void EnableScripts()
    {
        if (scriptsToEnable == null)
        {
            return;
        }

        for (int i = 0; i < scriptsToEnable.Length; i++)
        {
            MonoBehaviour script = scriptsToEnable[i];
            if (script != null)
            {
                script.enabled = true;
            }
        }
    }

    private void DisableScripts()
    {
        if (scriptsToDisable == null)
        {
            return;
        }

        for (int i = 0; i < scriptsToDisable.Length; i++)
        {
            MonoBehaviour script = scriptsToDisable[i];
            if (script != null)
            {
                script.enabled = false;
            }
        }
    }

    private void EnablePostMovementScripts()
    {
        if (scriptsToEnablePostMovement == null)
        {
            return;
        }

        for (int i = 0; i < scriptsToEnablePostMovement.Length; i++)
        {
            MonoBehaviour script = scriptsToEnablePostMovement[i];
            if (script != null)
            {
                script.enabled = true;
            }
        }
    }

}
