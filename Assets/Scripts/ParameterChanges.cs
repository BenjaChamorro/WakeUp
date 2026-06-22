using System;
using System.Collections.Generic;
using UnityEngine;

public class ParameterChanges : MonoBehaviour
{
    [Serializable]
    public class AnimatorParameterChange
    {
        public string parameterName;
        public AnimatorParameterType parameterType = AnimatorParameterType.Float;
        public float floatValue;
        public int intValue;
        public bool boolValue;
    }

    public enum AnimatorParameterType
    {
        Float,
        Int,
        Bool,
        Trigger
    }

    [Header("Target")]
    [Tooltip("Animator to modify. Leave empty to use the Animator on Target Object or this GameObject.")]
    [SerializeField] private Animator targetAnimator;
    [Tooltip("Optional object whose Animator will be modified. Leave empty to use the Animator on this GameObject.")]
    [SerializeField] private GameObject targetObject;

    [Header("Trigger")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool triggerOnlyOnce = true;

    [Header("Parameter Changes")]
    [SerializeField] private List<AnimatorParameterChange> parameterChanges = new List<AnimatorParameterChange>();

    private bool hasTriggered;
    private Animator resolvedAnimator;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!string.IsNullOrEmpty(playerTag) && !other.CompareTag(playerTag))
        {
            return;
        }

        if (triggerOnlyOnce && hasTriggered)
        {
            return;
        }

        ResolveTargetAnimator();
        ApplyChanges();
        hasTriggered = true;
    }

    public void ApplyChanges()
    {
        ResolveTargetAnimator();

        if (resolvedAnimator == null || parameterChanges == null)
        {
            Debug.LogWarning($"{name}: No Animator found for ParameterChanges.", this);
            return;
        }

        for (int i = 0; i < parameterChanges.Count; i++)
        {
            AnimatorParameterChange change = parameterChanges[i];
            if (change == null || string.IsNullOrEmpty(change.parameterName))
            {
                continue;
            }

            if (!HasParameter(change.parameterName))
            {
                Debug.LogWarning($"{name}: Animator does not contain parameter '{change.parameterName}'.", this);
                continue;
            }

            switch (change.parameterType)
            {
                case AnimatorParameterType.Float:
                    resolvedAnimator.SetFloat(change.parameterName, change.floatValue);
                    break;
                case AnimatorParameterType.Int:
                    resolvedAnimator.SetInteger(change.parameterName, change.intValue);
                    break;
                case AnimatorParameterType.Bool:
                    resolvedAnimator.SetBool(change.parameterName, change.boolValue);
                    break;
                case AnimatorParameterType.Trigger:
                    resolvedAnimator.SetTrigger(change.parameterName);
                    break;
            }
        }
    }

    private bool HasParameter(string parameterName)
    {
        if (resolvedAnimator == null) return false;
        AnimatorControllerParameter[] parameters = resolvedAnimator.parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].name == parameterName)
            {
                return true;
            }
        }

        return false;
    }

    private void ResolveTargetAnimator()
    {
        if (targetObject != null)
        {
            resolvedAnimator = targetObject.GetComponent<Animator>();
            return;
        }

        if (targetAnimator != null)
        {
            resolvedAnimator = targetAnimator;
            return;
        }

        resolvedAnimator = GetComponent<Animator>();
    }
}
