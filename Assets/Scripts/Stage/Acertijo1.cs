using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class Acertijo1 : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private GameObject Arboles;

    [Header("Input")]
    [SerializeField] private Key confirmKey = Key.E;

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        if (keyboard[confirmKey].wasPressedThisFrame)
        {
            VerificarRespuesta();
        }
    }

    private void VerificarRespuesta()
    {
        if (inputField == null)
        {
            Debug.LogWarning("InputField no asignado.");
            return;
        }

        string respuesta = inputField.text.Trim();

        if (respuesta == "0")
        {
            DesactivarArboles();
        }
    }

    private void DesactivarArboles()
    {
        if (Arboles == null)
        {
            Debug.LogWarning("GameObject Arboles no asignado.");
            return;
        }

        Arboles.SetActive(false);
    }
}
