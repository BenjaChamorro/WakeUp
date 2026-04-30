using System;
using UnityEngine;

public class DialogChain : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Controlador que emite el dialogo anterior y al que este chain se suscribe.")]
    [SerializeField] private DialogBoxController previousDialogController;
    [Tooltip("Controlador que reproducira este dialogo. Si se deja vacio, se usara el mismo de arriba.")]
    [SerializeField] private DialogBoxController selfDialogController;

    [Header("Filter")]
    [Tooltip("Id del dialogo que debe terminar para ejecutar este eslabon.")]
    [SerializeField] private string previousDialogueId;
    [SerializeField] private bool ignoreCase = false;

    [Header("Dialogue")]
    [Tooltip("Id propio del dialogo que este componente va a mostrar.")]
    [SerializeField] private string selfDialogueId;
    [TextArea(2, 6)]
    [SerializeField] private string[] lines;

    [Header("Target")]
    [SerializeField] private GameObject targetObject;
    [SerializeField] private bool setActiveState = true;

    [Header("Behavior")]
    [SerializeField] private bool triggerOnlyOnce = true;

    private bool hasTriggered;

    private void Awake()
    {
        if (previousDialogController == null)
        {
            previousDialogController = FindAnyObjectByType<DialogBoxController>();
        }

        if (selfDialogController == null)
        {
            selfDialogController = previousDialogController;
        }
    }

    private void OnEnable()
    {
        if (previousDialogController != null)
        {
            previousDialogController.DialogueFinished += OnDialogueFinished;
        }
    }

    private void OnDisable()
    {
        if (previousDialogController != null)
        {
            previousDialogController.DialogueFinished -= OnDialogueFinished;
        }
    }

    private void OnDialogueFinished(string finishedDialogueId)
    {
        if (hasTriggered && triggerOnlyOnce)
        {
            return;
        }

        if (!IsDialogueMatch(finishedDialogueId))
        {
            return;
        }

        if (targetObject != null)
        {
            targetObject.SetActive(setActiveState);
        }

        hasTriggered = true;
        StartOwnDialogue();
    }

    public void StartOwnDialogue()
    {
        if (selfDialogController == null)
        {
            Debug.LogWarning("No se encontro DialogBoxController de reproduccion en DialogChain.");
            return;
        }

        if (lines == null || lines.Length == 0)
        {
            Debug.LogWarning("DialogChain no tiene lineas para mostrar.");
            return;
        }

        if (selfDialogController.IsShowing)
        {
            return;
        }

        selfDialogController.ShowDialogue(selfDialogueId, lines);
    }

    private bool IsDialogueMatch(string finishedDialogueId)
    {
        if (string.IsNullOrEmpty(previousDialogueId))
        {
            return false;
        }

        StringComparison comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        return string.Equals(finishedDialogueId, previousDialogueId, comparison);
    }
}
