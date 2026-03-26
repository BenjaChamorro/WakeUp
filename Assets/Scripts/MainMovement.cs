using UnityEngine;
using UnityEngine.InputSystem;

public class MainMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;

    private Animator animator;
    private Rigidbody2D rb;

    private Vector2 rawInput;
    private Vector2 moveInput;

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
        rawInput = Vector2.zero;

        if (Keyboard.current.aKey.isPressed) rawInput.x = -1;
        else if (Keyboard.current.dKey.isPressed) rawInput.x = 1;

        if (Keyboard.current.wKey.isPressed) rawInput.y = 1;
        else if (Keyboard.current.sKey.isPressed) rawInput.y = -1;

        // Solo el movimiento va normalizado para evitar que el personaje se mueva más rápido en diagonal
        moveInput = rawInput.normalized;
    }

    void Move()
    {
        rb.MovePosition(rb.position + moveInput * moveSpeed * Time.fixedDeltaTime);
    }

    void UpdateAnimator()
    {
        animator.SetFloat("Horizontal", rawInput.x);
        animator.SetFloat("Vertical", rawInput.y);
    }
}
