using UnityEngine;

public class ProjectileSpawner : MonoBehaviour
{
    public GameObject projectilePrefab;
    public float spawnInterval = 1.5f;
    public float minX = -8f;
    public float maxX = 8f;

    void Start()
    {
        InvokeRepeating(nameof(SpawnProjectile), 0f, spawnInterval);
    }

    void SpawnProjectile()
    {
        float randomX = Random.Range(minX, maxX);
        Vector3 spawnPos = new Vector3(randomX, transform.position.y, 0f);
        Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
    }
}
