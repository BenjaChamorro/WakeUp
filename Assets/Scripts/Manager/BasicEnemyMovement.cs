using UnityEngine;

public class BasicEnemyMovement : MonoBehaviour
{
    [SerializeField] private float distanciaDeteccion = 6f;
    [SerializeField] private float velocidadMovimiento = 3f;
    [SerializeField] private Transform objetivo;

    private Rigidbody2D rb;
    private Vector2 direccion;
    private Vector2 velocidadActual;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        // Si no se asigna objetivo manualmente, intentamos encontrar al jugador por tag.
        if (objetivo == null)
        {
            GameObject jugador = GameObject.FindGameObjectWithTag("Player");
            if (jugador != null)
            {
                objetivo = jugador.transform;
            }
        }
    }

    void Update()
    {
        CalcularMovimiento();
    }

    void FixedUpdate()
    {
        Mover();
    }

    void CalcularMovimiento()
    {
        if (objetivo == null)
        {
            direccion = Vector2.zero;
            velocidadActual = Vector2.zero;
            return;
        }

        Vector2 posicionObjetivo = objetivo.position;
        Vector2 vectorHaciaObjetivo = posicionObjetivo - rb.position;
        float distanciaAlObjetivo = vectorHaciaObjetivo.magnitude;

        if (distanciaAlObjetivo <= distanciaDeteccion)
        {
            direccion = vectorHaciaObjetivo.normalized;
            velocidadActual = direccion * velocidadMovimiento;
        }
        else
        {
            direccion = Vector2.zero;
            velocidadActual = Vector2.zero;
        }
    }

    void Mover()
    {
        rb.MovePosition(rb.position + velocidadActual * Time.fixedDeltaTime);
    }
}
