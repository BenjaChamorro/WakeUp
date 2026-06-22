using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 5f;
    public float rotationSpeed = 180f;

    void Update()
    {
        transform.position += Vector3.down * speed * Time.deltaTime;

        transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);

        if (transform.position.y < -6f)
            Destroy(gameObject);
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        if (col.gameObject.CompareTag("Player"))
        {
            col.gameObject.GetComponent<PlayerHealth>()?.TakeDamage(10);
            Destroy(gameObject);
        }

        if (col.gameObject.CompareTag("Floor"))
            Destroy(gameObject);
    }
}