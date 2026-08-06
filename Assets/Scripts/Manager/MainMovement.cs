using UnityEngine;
using UnityEngine.InputSystem;

public class MainMovement : MonoBehaviour
{
    [SerializeField] private float velocidadMovimiento = 5f;
    [SerializeField] private float fuerzaRechazo = 0.35f;

    private Animator animator;
    private Rigidbody2D rb;

    private Vector2 direccion;
    private Vector2 velocidadActual;
    private bool bloqueadoPorNpc;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        ReadInput();
        UpdateAnimator();
    }

    void FixedUpdate()
    {
        if (bloqueadoPorNpc)
        {
            velocidadActual = Vector2.zero;
            return;
        }

        Move();
    }

    void ReadInput()
    {
        direccion = Vector2.zero;

        if (Keyboard.current.aKey.isPressed) direccion.x = -1;
        else if (Keyboard.current.dKey.isPressed) direccion.x = 1;

        if (Keyboard.current.wKey.isPressed) direccion.y = 1;
        else if (Keyboard.current.sKey.isPressed) direccion.y = -1;

        // Convertimos direccion en velocidad para mantener una rapidez constante en diagonal.
        velocidadActual = direccion.normalized * velocidadMovimiento;
    }

    void Move()
    {
        rb.MovePosition(rb.position + velocidadActual * Time.fixedDeltaTime);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        ManejarContactoConNpc(collision.collider);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        ManejarContactoConNpc(other);
    }

    private void ManejarContactoConNpc(Collider2D other)
    {
        if (other == null || !other.TryGetComponent<MoveNpcTo>(out var movimientoNpc) || !movimientoNpc.EstaMoviendo)
        {
            bloqueadoPorNpc = false;
            return;
        }

        bloqueadoPorNpc = true;
        Vector2 direccionRechazo = (transform.position - other.transform.position).normalized;
        if (direccionRechazo.sqrMagnitude > 0.001f)
        {
            rb.MovePosition(rb.position + direccionRechazo * fuerzaRechazo);
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.collider != null && collision.collider.TryGetComponent<MoveNpcTo>(out _))
        {
            bloqueadoPorNpc = false;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other != null && other.TryGetComponent<MoveNpcTo>(out _))
        {
            bloqueadoPorNpc = false;
        }
    }

    void UpdateAnimator()
    {
        animator.SetFloat("Horizontal", direccion.x);
        animator.SetFloat("Vertical", direccion.y);
        animator.SetBool("IsWalking", direccion.sqrMagnitude > 0f);
    }
}
