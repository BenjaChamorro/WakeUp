using UnityEngine;

public class HorizontalFlipNPC : MonoBehaviour
{
    [SerializeField] private float umbralMovimiento = 0.001f;

    private Quaternion rotacionNormal;
    private float posicionXAnterior;

    void Awake()
    {
        rotacionNormal = transform.localRotation;
        posicionXAnterior = transform.position.x;
    }

    void LateUpdate()
    {
        float desplazamientoX = transform.position.x - posicionXAnterior;

        if (desplazamientoX < -umbralMovimiento)
        {
            transform.localRotation = rotacionNormal * Quaternion.Euler(0f, 180f, 0f);
        }
        else if (desplazamientoX > umbralMovimiento)
        {
            transform.localRotation = rotacionNormal;
        }

        posicionXAnterior = transform.position.x;
    }
}
