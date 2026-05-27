using UnityEngine;
using UnityEngine.InputSystem;

public class MainMovement : MonoBehaviour
{
    [SerializeField] private float velocidadMovimiento = 5f;

    private Animator animator;
    private Rigidbody2D rb;

    private Vector2 direccion;
    private Vector2 velocidadActual;

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

    void UpdateAnimator()
    {
        animator.SetFloat("Horizontal", direccion.x);
        animator.SetFloat("Vertical", direccion.y);
        animator.SetBool("IsWalking", direccion.sqrMagnitude > 0f);
    }
}
