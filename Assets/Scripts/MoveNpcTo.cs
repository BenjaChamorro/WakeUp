using System;
using UnityEngine;

public class MoveNpcTo : MonoBehaviour
{
    [SerializeField] private float velocidad = 2f;
    [SerializeField] private Transform[] destinos;
    [SerializeField] private float tiempoEsperaEntreDestinos = 1f;
    [SerializeField] private float fuerzaRechazoAlJugador = 0.8f;
    [Header("Optional Animation")]
    [Tooltip("Si se asigna, estos NPCSpriteControllers se actualizarán con la dirección del movimiento. Déjalo vacío para detectarlos automáticamente en los hijos.")]
    [SerializeField] private NPCSpriteController[] spriteControllers;

    private bool moviendo;
    private int indiceDestinoActual;
    private float tiempoEsperaRestante;
    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

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

        Vector3 posicionActual = rb != null ? rb.position : transform.position;
        Vector3 nuevaPosicion = Vector3.MoveTowards(posicionActual, destinoActual.position, velocidad * Time.deltaTime);

        if (rb != null)
        {
            rb.MovePosition(new Vector2(nuevaPosicion.x, nuevaPosicion.y));
        }
        else
        {
            transform.position = nuevaPosicion;
        }

        Vector3 delta = destinoActual.position - (rb != null ? rb.position : transform.position);
        Vector2 dir = new Vector2(delta.x, delta.y);
        SetSpriteDirectionFromVelocity(dir * velocidad);

        if (Vector3.Distance(rb != null ? rb.position : transform.position, destinoActual.position) <= 0.001f)
        {
            if (rb != null)
            {
                rb.position = new Vector2(destinoActual.position.x, destinoActual.position.y);
            }
            else
            {
                transform.position = destinoActual.position;
            }

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

    private void OnCollisionStay2D(Collision2D collision)
    {
        ManejarContactoConJugador(collision.gameObject, collision.rigidbody);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        ManejarContactoConJugador(other.gameObject, other.attachedRigidbody);
    }

    private void ManejarContactoConJugador(GameObject otherObject, Rigidbody2D otherRigidbody)
    {
        if (!moviendo || otherObject == null || otherRigidbody == null)
            return;

        if (!otherObject.TryGetComponent<MainMovement>(out _))
            return;

        Vector2 direccionRechazo = (otherObject.transform.position - transform.position).normalized;
        if (direccionRechazo.sqrMagnitude <= 0.001f)
            return;

        otherRigidbody.MovePosition(otherRigidbody.position + direccionRechazo * fuerzaRechazoAlJugador * Time.fixedDeltaTime);
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
