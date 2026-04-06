using UnityEngine;
using UnityEngine.InputSystem;

public class PcActivate : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject AcertijoMecanismo;
    [SerializeField] private GameObject PressE;
    [SerializeField] private Animator pressEAnimator;

    [Header("Filter")]
    [SerializeField] private bool requireTag = true;
    [SerializeField] private string requiredTag = "Player";

    [Header("Input")]
    [SerializeField] private Key interactionKey = Key.E;
    [SerializeField] private float pressedAnimationTime = 0.12f;

    private static readonly int IsPressedHash = Animator.StringToHash("IsPressed");
    private bool targetInside;
    private Coroutine pressAnimationRoutine;

    private void Awake()
    {
        if (pressEAnimator == null && PressE != null)
        {
            pressEAnimator = PressE.GetComponent<Animator>();
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
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (EsObjetivoValido(other.gameObject))
        {
            targetInside = true;
            MostrarMecanismo(true);
            MostrarPressE(true);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (EsObjetivoValido(other.gameObject))
        {
            targetInside = false;
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
}
