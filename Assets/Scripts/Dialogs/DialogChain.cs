using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;

[Serializable]
public class DialogLineAction
{
    [Tooltip("0-based line index in the dialogue to trigger this action.")]
    public int lineIndex = 0;
    [Tooltip("ParameterChanges components to call when this line is reached.")]
    public ParameterChanges[] parameterTargets;
    [Tooltip("UnityEvent to invoke when this line is reached. Useful for calling arbitrary methods.")]
    public UnityEvent onLine;
    [Tooltip("If true the action will be fired only once per dialogue show.")]
    public bool onlyOnce = true;
    [NonSerialized] public bool fired;
}

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

    [Header("Per-line Actions")]
    [SerializeField] private List<DialogLineAction> lineActions = new List<DialogLineAction>();

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
        if (selfDialogController != null)
        {
            selfDialogController.LineShown += OnLineShown;
        }
    }

    private void OnDisable()
    {
        if (previousDialogController != null)
        {
            previousDialogController.DialogueFinished -= OnDialogueFinished;
        }
        if (selfDialogController != null)
        {
            selfDialogController.LineShown -= OnLineShown;
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

        // reset per-line fired flags
        if (lineActions != null)
        {
            for (int i = 0; i < lineActions.Count; i++)
            {
                lineActions[i].fired = false;
            }
        }

        selfDialogController.ShowDialogue(selfDialogueId, lines);
    }

    private void OnLineShown(int lineIndex)
    {
        if (lineActions == null || lineActions.Count == 0) return;

        for (int i = 0; i < lineActions.Count; i++)
        {
            DialogLineAction action = lineActions[i];
            if (action == null) continue;
            if (action.lineIndex != lineIndex) continue;
            if (action.onlyOnce && action.fired) continue;

            if (action.parameterTargets != null)
            {
                for (int j = 0; j < action.parameterTargets.Length; j++)
                {
                    ParameterChanges pc = action.parameterTargets[j];
                    if (pc != null)
                    {
                        pc.ApplyChanges();
                    }
                }
            }

            if (action.onLine != null)
            {
                action.onLine.Invoke();
            }

            action.fired = true;
        }
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
