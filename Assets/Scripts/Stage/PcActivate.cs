using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class PcActivate : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject AcertijoMecanismo;
    [SerializeField] private GameObject PressE;
    [SerializeField] private TMP_InputField linkedInputField;
    private Animator pcAnimator;
    private Animator pressEAnimator;
    private AudioSource pressESound;
    private AudioSource activationSound;

    [Header("Filter")]
    [SerializeField] private bool requireTag = true;
    [SerializeField] private string requiredTag = "Player";

    [Header("Input")]
    [SerializeField] private Key interactionKey = Key.E;
    [SerializeField] private float pressedAnimationTime = 0.12f;

    private static readonly int IsOnHash = Animator.StringToHash("IsOn");
    private static readonly int IsPressedHash = Animator.StringToHash("IsPressed");
    private static readonly int InputHash = Animator.StringToHash("Input");
    private bool targetInside;
    private Coroutine pressAnimationRoutine;
    private int cachedInputValue;

    private void Awake()
    {
        pcAnimator = GetComponent<Animator>();
        if (PressE != null)
        {
            pressEAnimator = PressE.GetComponent<Animator>();
            pressESound = PressE.GetComponent<AudioSource>();
        }

        activationSound = GetComponent<AudioSource>();

        if (linkedInputField != null)
        {
            linkedInputField.onValueChanged.AddListener(OnInputFieldValueChanged);
            OnInputFieldValueChanged(linkedInputField.text);
        }
    }

    private void OnDestroy()
    {
        if (linkedInputField != null)
        {
            linkedInputField.onValueChanged.RemoveListener(OnInputFieldValueChanged);
        }
    }

    private void Update()
    {
        if (!targetInside)
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
            ReproducirAnimacionPresionado();

            if (cachedInputValue != 0)
            {
                ReproducirSonidoPressE();
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (EsObjetivoValido(other.gameObject))
        {
            targetInside = true;
            SetOnParameter(true);
            MostrarMecanismo(true);
            MostrarPressE(true);
            if (activationSound != null)
            {
                activationSound.Play();
            }
            ApplyCachedInputToAnimator();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (EsObjetivoValido(other.gameObject))
        {
            targetInside = false;
            SetOnParameter(false);
            MostrarMecanismo(false);
            MostrarPressE(false);
            SetPressedParameter(false);
        }
    }

    private bool EsObjetivoValido(GameObject otherObject)
    {
        if (!requireTag)
        {
            return true;
        }

        return otherObject.CompareTag(requiredTag);
    }

    private void MostrarMecanismo(bool visible)
    {
        if (AcertijoMecanismo == null)
        {
            return;
        }

        AcertijoMecanismo.SetActive(visible);
    }

    private void MostrarPressE(bool visible)
    {
        if (PressE == null)
        {
            return;
        }

        PressE.SetActive(visible);
    }

    private void ReproducirAnimacionPresionado()
    {
        if (pressAnimationRoutine != null)
        {
            StopCoroutine(pressAnimationRoutine);
        }

        pressAnimationRoutine = StartCoroutine(PulseIsPressed());
    }

    private System.Collections.IEnumerator PulseIsPressed()
    {
        SetPressedParameter(true);
        yield return new WaitForSeconds(Mathf.Max(0.01f, pressedAnimationTime));
        SetPressedParameter(false);
        pressAnimationRoutine = null;
    }

    private void SetPressedParameter(bool value)
    {
        if (pressEAnimator == null)
        {
            return;
        }

        pressEAnimator.SetBool(IsPressedHash, value);
    }

    private void OnInputFieldValueChanged(string value)
    {
        int parsedValue;
        if (!int.TryParse(value, out parsedValue))
        {
            parsedValue = 0;
        }

        cachedInputValue = parsedValue;
        ApplyCachedInputToAnimator();
    }

    private void ApplyCachedInputToAnimator()
    {
        if (pressEAnimator == null)
        {
            return;
        }

        if (pressEAnimator.runtimeAnimatorController == null)
        {
            return;
        }

        if (!pressEAnimator.isActiveAndEnabled)
        {
            return;
        }

        pressEAnimator.SetInteger(InputHash, cachedInputValue);
    }

    private void ReproducirSonidoPressE()
    {
        if (pressESound == null)
        {
            return;
        }

        pressESound.Play();
    }

    private void SetOnParameter(bool value)
    {
        if (pcAnimator == null)
        {
            return;
        }

        pcAnimator.SetBool(IsOnHash, value);
    }
}
