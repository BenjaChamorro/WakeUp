using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 5f;
    public float rotationSpeed = 180f;
    public int damage = 10;

    private Vector2 moveDirection = Vector2.down;
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private float defaultGravityScale;

    void Awake()
    {
        ResolveSpriteRenderer();
        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
            defaultGravityScale = rb.gravityScale;
    }

    // Lo llama el ProjectileSpawner al instanciar, para aplicar la variante activa del minijuego.
    // 'horizontalGravityScale' solo se usa si el movimiento es horizontal: 0 = línea recta, >0 = cae en arco.
    public void Configure(Sprite sprite, Vector2 scale, float fallSpeed, float spin, int hitDamage, Vector2 direction = default, float horizontalGravityScale = 0f)
    {
        ResolveSpriteRenderer();

        if (sprite != null && spriteRenderer != null)
            spriteRenderer.sprite = sprite;

        // (0,0) = conservar la escala del prefab; cualquier otro valor la sobreescribe.
        if (scale.x > 0f && scale.y > 0f)
            transform.localScale = new Vector3(scale.x, scale.y, transform.localScale.z);

        speed = fallSpeed;
        rotationSpeed = spin;
        damage = hitDamage;
        moveDirection = direction.sqrMagnitude > 0f ? direction.normalized : Vector2.down;

        // Movimiento puramente horizontal: por defecto la gravedad del Rigidbody2D no lo arrastra hacia abajo,
        // salvo que la variante pida que caiga (la gravedad se suma al avance horizontal y genera un arco).
        if (rb != null)
            rb.gravityScale = Mathf.Approximately(moveDirection.y, 0f) ? Mathf.Max(0f, horizontalGravityScale) : defaultGravityScale;
    }

    private void ResolveSpriteRenderer()
    {
        if (spriteRenderer != null)
            return;

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    void Update()
    {
        transform.position += (Vector3)(moveDirection * speed * Time.deltaTime);

        transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);

        if (transform.position.y < -6f || Mathf.Abs(transform.position.x) > 12f)
            Destroy(gameObject);
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        if (col.gameObject.CompareTag("MiniGamePlayer"))
        {
            col.gameObject.GetComponent<PlayerHealth>()?.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        if (col.gameObject.CompareTag("Floor"))
        {
            Destroy(gameObject);
            return;
        }

        DestroyIfBlocked(col);
    }

    void OnCollisionStay2D(Collision2D col)
    {
        DestroyIfBlocked(col);
    }

    // Si choca de frente con algo que le impide avanzar (p. ej. los bordes Edge_Left/Edge_Right del área de juego),
    // se destruye. Como se mueve por transform, sin esto se quedaría atascado empujando contra la pared para siempre.
    private void DestroyIfBlocked(Collision2D col)
    {
        // Los choques entre proyectiles no cuentan: no deben destruirse unos a otros.
        if (col.gameObject.GetComponent<Projectile>() != null)
            return;

        for (int i = 0; i < col.contactCount; i++)
        {
            // La normal apunta desde el otro collider hacia este proyectil: si va contra el avance, lo está frenando.
            if (Vector2.Dot(col.GetContact(i).normal, moveDirection) < -0.5f)
            {
                Destroy(gameObject);
                return;
            }
        }
    }
}
