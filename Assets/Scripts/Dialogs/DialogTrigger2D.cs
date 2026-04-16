using UnityEngine;
using UnityEngine.InputSystem;

public class DialogTrigger2D : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DialogBoxController dialogController;

    [Header("Lines")]
    [TextArea(2, 6)]
    [SerializeField] private string[] lines;
    [SerializeField] private string dialogueId;

    [Header("Filter")]
    [SerializeField] private bool requireTag = true;
    [SerializeField] private string requiredTag = "Player";

    [Header("Behavior")]
    [SerializeField] private bool triggerOnEnter = true;
    [SerializeField] private bool requireInteractionKey = false;
    [SerializeField] private Key interactionKey = Key.Enter;
    [SerializeField] private bool triggerOnlyOnce = true;

    private bool hasTriggered;
    private bool targetInside;

    private void Awake()
    {
        if (dialogController == null)
        {
            dialogController = FindAnyObjectByType<DialogBoxController>();
        }
    }

    private void Update()
    {
        if (!requireInteractionKey || !targetInside)
        {
            return;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        if (keyboard[interactionKey].wasPressedThisFrame)
        {
            TryStartDialogue();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        HandleEnter(other.gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        HandleEnter(other.gameObject);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (IsTargetValid(other.gameObject))
        {
            targetInside = false;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (IsTargetValid(other.gameObject))
        {
            targetInside = false;
        }
    }

    private void HandleEnter(GameObject otherObject)
    {
        if (!IsTargetValid(otherObject))
        {
            return;
        }

        targetInside = true;

        if (triggerOnEnter && !requireInteractionKey)
        {
            TryStartDialogue();
        }
    }

    private bool IsTargetValid(GameObject otherObject)
    {
        if (!requireTag)
        {
            return true;
        }

        return otherObject.CompareTag(requiredTag);
    }

    private void TryStartDialogue()
    {
        if (hasTriggered && triggerOnlyOnce)
        {
            return;
        }

        if (dialogController == null)
        {
            Debug.LogWarning("No se encontro DialogBoxController en la escena.");
            return;
        }

        if (dialogController.IsShowing)
        {
            return;
        }

        dialogController.ShowDialogue(dialogueId, lines);
        hasTriggered = true;
    }

    public void TriggerDialogueFromChain()
    {
        TryStartDialogue();
    }
}
