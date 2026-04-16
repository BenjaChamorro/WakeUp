using System.Collections;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class DialogBoxController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject dialogRoot;
    [SerializeField] private TMP_Text dialogText;

    [Header("Typing")]
    [SerializeField] private bool useTypewriter = true;
    [SerializeField] private float charactersPerSecond = 40f;
    [SerializeField] private bool allowSkipTyping = true;

    [Header("Input")]
    [SerializeField] private Key continueKey = Key.Space;

    [Header("Auto Advance")]
    [SerializeField] private bool autoAdvanceIfNoInput = true;
    [SerializeField] private float autoAdvanceDelay = 2f;

    [Header("Gameplay Lock")]
    [SerializeField] private MonoBehaviour[] componentsToDisableWhileOpen;

    [Header("Position")]
    [SerializeField] private bool followPlayer = true;
    [SerializeField] private Transform followTarget;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private Vector3 offset = Vector3.zero;

    private Transform playerTransform;

    private Coroutine dialogRoutine;
    private bool isShowing;
    private bool advanceRequested;
    private bool skipTypingRequested;
    private string currentDialogueId = string.Empty;

    public bool IsShowing => isShowing;
    public string CurrentDialogueId => currentDialogueId;
    public event Action<string> DialogueFinished;

    private void Awake()
    {
        SetDialogVisible(false);
        ClearText();

        if (followPlayer && playerTransform == null)
        {
            if (followTarget != null)
            {
                playerTransform = followTarget;
            }
            else
            {
                GameObject playerObject = GameObject.FindWithTag(playerTag);
                if (playerObject != null)
                {
                    playerTransform = playerObject.transform;
                }
            }
        }
    }

    private void Update()
    {
        if (followPlayer && playerTransform != null && isShowing)
        {
            dialogRoot.transform.position = playerTransform.position + offset;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || !isShowing)
        {
            return;
        }

        if (keyboard[continueKey].wasPressedThisFrame)
        {
            advanceRequested = true;
            skipTypingRequested = true;
        }
    }

    public void ShowSingleLine(string line)
    {
        ShowDialogue(new[] { line });
    }

    public void ShowDialogue(string[] lines)
    {
        ShowDialogue(string.Empty, lines);
    }

    public void ShowDialogue(string dialogueId, string[] lines)
    {
        if (lines == null || lines.Length == 0)
        {
            return;
        }

        currentDialogueId = dialogueId ?? string.Empty;

        if (dialogRoutine != null)
        {
            StopCoroutine(dialogRoutine);
        }

        dialogRoutine = StartCoroutine(RunDialogue(lines));
    }

    public void CloseDialogue()
    {
        if (dialogRoutine != null)
        {
            StopCoroutine(dialogRoutine);
            dialogRoutine = null;
        }

        string finishedDialogueId = currentDialogueId;

        isShowing = false;
        advanceRequested = false;
        skipTypingRequested = false;
        currentDialogueId = string.Empty;

        ClearText();
        SetDialogVisible(false);
        SetGameplayEnabled(true);

        if (!string.IsNullOrEmpty(finishedDialogueId))
        {
            DialogueFinished?.Invoke(finishedDialogueId);
        }
    }

    private IEnumerator RunDialogue(string[] lines)
    {
        isShowing = true;
        advanceRequested = false;
        skipTypingRequested = false;

        SetGameplayEnabled(false);
        SetDialogVisible(true);

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i] ?? string.Empty;

            yield return ShowLine(line);
            yield return WaitForAdvance();
        }

        CloseDialogue();
    }

    private IEnumerator ShowLine(string line)
    {
        if (!useTypewriter || charactersPerSecond <= 0f)
        {
            dialogText.text = line;
            yield break;
        }

        skipTypingRequested = false;
        dialogText.text = string.Empty;

        float delay = 1f / charactersPerSecond;

        for (int i = 1; i <= line.Length; i++)
        {
            if (allowSkipTyping && skipTypingRequested)
            {
                dialogText.text = line;
                skipTypingRequested = false;
                yield break;
            }

            dialogText.text = line.Substring(0, i);
            yield return new WaitForSeconds(delay);
        }
    }

    private IEnumerator WaitForAdvance()
    {
        float elapsed = 0f;

        while (true)
        {
            if (advanceRequested)
            {
                break;
            }

            if (autoAdvanceIfNoInput)
            {
                elapsed += Time.deltaTime;
                if (elapsed >= Mathf.Max(0f, autoAdvanceDelay))
                {
                    break;
                }
            }

            yield return null;
        }

        advanceRequested = false;
    }

    private void SetGameplayEnabled(bool enabled)
    {
        if (componentsToDisableWhileOpen == null)
        {
            return;
        }

        for (int i = 0; i < componentsToDisableWhileOpen.Length; i++)
        {
            MonoBehaviour component = componentsToDisableWhileOpen[i];
            if (component != null)
            {
                component.enabled = enabled;
            }
        }
    }

    private void SetDialogVisible(bool visible)
    {
        if (dialogRoot != null)
        {
            dialogRoot.SetActive(visible);
        }
    }

    private void ClearText()
    {
        if (dialogText != null)
        {
            dialogText.text = string.Empty;
        }
    }
}
