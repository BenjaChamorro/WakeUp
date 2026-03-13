using UnityEngine;
using UnityEngine.InputSystem;

public class MainMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    
    private Animator animator;
    private Rigidbody2D rb;
    private Vector2 moveInput;
    
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        Move();
    }

    public void Move()
    {
        // Calcular movimiento horizontal
        if (Keyboard.current.aKey.isPressed)
        {
            moveInput.x = -1f;
        }
        else if (Keyboard.current.dKey.isPressed)
        {
            moveInput.x = 1f;
        }
        else
        {
            moveInput.x = 0f;
        }

        // Calcular movimiento vertical
        if (Keyboard.current.wKey.isPressed)
        {
            moveInput.y = 1f;
        }
        else if (Keyboard.current.sKey.isPressed)
        {
            moveInput.y = -1f;
        }
        else
        {
            moveInput.y = 0f;
        }

        // Actualizar parámetros del Animator
        animator.SetFloat("Horizontal", moveInput.x);
        animator.SetFloat("Vertical", moveInput.y);

        transform.Translate(moveInput * moveSpeed * Time.deltaTime);
    }
}
