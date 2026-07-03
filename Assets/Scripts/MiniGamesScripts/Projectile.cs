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
    public void Configure(Sprite sprite, Vector2 scale, float fallSpeed, float spin, int hitDamage, Vector2 direction = default)
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

        // Movimiento puramente horizontal: la gravedad del Rigidbody2D no debe arrastrarlo hacia abajo.
        if (rb != null)
            rb.gravityScale = Mathf.Approximately(moveDirection.y, 0f) ? 0f : defaultGravityScale;
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
        }

        if (col.gameObject.CompareTag("Floor"))
            Destroy(gameObject);
    }
}
