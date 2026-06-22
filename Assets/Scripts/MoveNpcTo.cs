using System;
using UnityEngine;

public class MoveNpcTo : MonoBehaviour
{
    [SerializeField] private float velocidad = 2f;
    [SerializeField] private Transform[] destinos;
    [SerializeField] private float tiempoEsperaEntreDestinos = 1f;
    [Header("Optional Animation")]
    [Tooltip("Si se asigna, estos NPCSpriteControllers se actualizarán con la dirección del movimiento. Déjalo vacío para detectarlos automáticamente en los hijos.")]
    [SerializeField] private NPCSpriteController[] spriteControllers;

    private bool moviendo;
    private int indiceDestinoActual;
    private float tiempoEsperaRestante;

    private void Awake()
    {
        if (spriteControllers == null || spriteControllers.Length == 0)
        {
            spriteControllers = GetComponentsInChildren<NPCSpriteController>(true);
        }
    }

    public bool EstaMoviendo => moviendo;
    public event Action MovimientoFinalizado;

    public void IniciarMovimiento()
    {
        if (destinos == null || destinos.Length == 0)
        {
            FinalizarMovimiento();
            return;
        }

        indiceDestinoActual = 0;
        tiempoEsperaRestante = 0f;
        moviendo = true;
    }

    public void DetenerMovimiento()
    {
        FinalizarMovimiento();
    }

    private void Update()
    {
        if (!moviendo || destinos == null || destinos.Length == 0)
        {
            SetSpriteDirection(Vector2.zero);
            return;
        }

        if (tiempoEsperaRestante > 0f)
        {
            tiempoEsperaRestante -= Time.deltaTime;
            return;
        }

        Transform destinoActual = destinos[indiceDestinoActual];
        if (destinoActual == null)
        {
            AvanzarAlSiguienteDestino();
            return;
        }

        transform.position = Vector3.MoveTowards(
            transform.position,
            destinoActual.position,
            velocidad * Time.deltaTime);

        Vector3 delta = destinoActual.position - transform.position;
        Vector2 dir = new Vector2(delta.x, delta.y);
        SetSpriteDirectionFromVelocity(dir * velocidad);

        if (Vector3.Distance(transform.position, destinoActual.position) <= 0.001f)
        {
            transform.position = destinoActual.position;
            AvanzarAlSiguienteDestino();
        }
    }

    private void AvanzarAlSiguienteDestino()
    {
        indiceDestinoActual++;

        if (indiceDestinoActual >= destinos.Length)
        {
            FinalizarMovimiento();
            return;
        }

        tiempoEsperaRestante = tiempoEsperaEntreDestinos;
    }

    private void FinalizarMovimiento()
    {
        bool estabaMoviendo = moviendo;
        moviendo = false;
        tiempoEsperaRestante = 0f;

        if (estabaMoviendo)
        {
            MovimientoFinalizado?.Invoke();
        }
    }

    private void SetSpriteDirection(Vector2 direction)
    {
        if (spriteControllers == null)
            return;

        for (int i = 0; i < spriteControllers.Length; i++)
        {
            if (spriteControllers[i] != null)
                spriteControllers[i].SetDirection(direction);
        }
    }

    private void SetSpriteDirectionFromVelocity(Vector2 velocity)
    {
        if (spriteControllers == null)
            return;

        for (int i = 0; i < spriteControllers.Length; i++)
        {
            if (spriteControllers[i] != null)
                spriteControllers[i].SetDirectionFromVelocity(velocity);
        }
    }
}
