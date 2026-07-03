using UnityEngine;

// Moneda recolectable individual. El CoinSpawner la instancia y se asigna a sí mismo
// como dueño para que Coin le avise cuando el jugador la recoge.
[RequireComponent(typeof(Collider2D))]
public class Coin : MonoBehaviour {
    private CoinSpawner owner;
    private SpriteRenderer spriteRenderer;

    void Awake() {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null) {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }
    }

    // Lo llama el CoinSpawner al instanciar, para aplicar la variante activa del minijuego.
    public void Configure(CoinSpawner spawner, Sprite sprite, Vector2 scale) {
        owner = spawner;

        if (sprite != null && spriteRenderer != null) {
            spriteRenderer.sprite = sprite;
        }

        // (0,0) = conservar la escala del prefab; cualquier otro valor la sobreescribe.
        if (scale.x > 0f && scale.y > 0f) {
            transform.localScale = new Vector3(scale.x, scale.y, transform.localScale.z);
        }
    }

    void OnTriggerEnter2D(Collider2D other) {
        if (!other.CompareTag("MiniGamePlayer")) {
            return;
        }

        owner?.NotifyCoinCollected();
        Destroy(gameObject);
    }
}
