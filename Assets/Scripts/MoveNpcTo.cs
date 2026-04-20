using System;
using UnityEngine;

public class MoveNpcTo : MonoBehaviour
{
    [SerializeField] private float velocidad = 2f;
    [SerializeField] private Transform[] destinos;
    [SerializeField] private float tiempoEsperaEntreDestinos = 1f;

    private bool moviendo;
    private int indiceDestinoActual;
    private float tiempoEsperaRestante;

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
}
