using UnityEngine;

public class TriggerScriptActivate : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool triggerOnlyOnce = true;

    [Header("NPC Movement")]
    [SerializeField] private MoveNpcTo npcMovement;
    [SerializeField] private MonoBehaviour[] playerScriptsToBlockWhileNpcMoves;


    [Header("Scripts to Enable")]
    [SerializeField] private MonoBehaviour[] scriptsToEnable;

    [Header("Scripts to Disable")]
    [SerializeField] private MonoBehaviour[] scriptsToDisable;
    [SerializeField] private MonoBehaviour[] scriptsToEnablePostMovement;

    private bool hasTriggered;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag))
        {
            return;
        }

        if (triggerOnlyOnce && hasTriggered)
        {
            return;
        }

        EnableScripts();
        DisableScripts();

        StartNpcMovement();

        hasTriggered = true;
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
