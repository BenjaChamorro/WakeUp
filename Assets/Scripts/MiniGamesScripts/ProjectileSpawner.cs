using System.Collections;
using UnityEngine;

public class ProjectileSpawner : MonoBehaviour
{
    public GameObject projectilePrefab;
    public float spawnInterval = 1.5f;
    public float minX = -8f;
    public float maxX = 8f;

    // Configuración aplicada por MiniGameRuntime. Si nadie configura el spawner
    // (escena suelta), se usan los valores por defecto de arriba.
    private Sprite projectileSprite;
    private Vector2 projectileScale = new Vector2(4f, 4f);
    private float projectileFallSpeed = 5f;
    private float projectileRotationSpeed = 180f;
    private int projectileDamage = 10;
    private MiniGameData.SpawnPattern spawnPattern = MiniGameData.SpawnPattern.RandomRain;
    private int columnCount = 4;
    private int projectilesPerWave = 3;
    private bool overrideSpawnHeight = false;
    private float spawnHeight;

    private MiniGameData.MovementDirection movementDirection = MiniGameData.MovementDirection.Down;
    private float minY = -3f;
    private float maxY = 2f;
    private float horizontalSpawnEdgeX = 9f;

    private int nextColumn;
    private Coroutine spawnRoutine;
    private bool configured;

    void Start()
    {
        // Espera un frame: da tiempo a que MiniGameRuntime nos configure antes de arrancar
        // con los valores por defecto. Así la escena suelta sigue funcionando sin runtime.
        StartCoroutine(AutoStartIfUnconfigured());
    }

    private IEnumerator AutoStartIfUnconfigured()
    {
        yield return null;
        if (!configured)
            BeginSpawning();
    }

    public void Configure(MiniGameData data)
    {
        if (data == null)
            return;

        projectileSprite = data.projectileSprite;
        projectileScale = data.projectileScale;
        projectileFallSpeed = data.projectileFallSpeed;
        projectileRotationSpeed = data.projectileRotationSpeed;
        projectileDamage = data.projectileDamage;

        spawnPattern = data.spawnPattern;
        spawnInterval = Mathf.Max(0.01f, data.spawnInterval);
        minX = data.spawnMinX;
        maxX = data.spawnMaxX;
        columnCount = Mathf.Max(1, data.columnCount);
        projectilesPerWave = Mathf.Max(1, data.projectilesPerWave);
        overrideSpawnHeight = data.overrideSpawnHeight;
        spawnHeight = data.spawnHeight;

        movementDirection = data.movementDirection;
        minY = data.spawnMinY;
        maxY = data.spawnMaxY;
        horizontalSpawnEdgeX = data.horizontalSpawnEdgeX;

        configured = true;
        BeginSpawning();
    }

    public void BeginSpawning()
    {
        if (spawnRoutine != null)
            StopCoroutine(spawnRoutine);
        spawnRoutine = StartCoroutine(SpawnLoop());
    }

    public void StopSpawning()
    {
        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
            spawnRoutine = null;
        }
    }

    private IEnumerator SpawnLoop()
    {
        nextColumn = 0;
        while (true)
        {
            switch (spawnPattern)
            {
                case MiniGameData.SpawnPattern.RandomRain:
                    SpawnAt(RandomSpread());
                    break;

                case MiniGameData.SpawnPattern.FixedColumns:
                    for (int c = 0; c < columnCount; c++)
                        SpawnAt(GetColumnSpread(c));
                    break;

                case MiniGameData.SpawnPattern.Waves:
                    for (int w = 0; w < projectilesPerWave; w++)
                        SpawnAt(RandomSpread());
                    break;
            }

            yield return new WaitForSeconds(spawnInterval);
        }
    }

    // El "spread" es la coordenada que varía según el patrón de spawn: X cuando el
    // proyectil cae (Down), Y cuando se mueve horizontalmente (RightToLeft/LeftToRight).
    private float RandomSpread()
    {
        return movementDirection == MiniGameData.MovementDirection.Down
            ? Random.Range(minX, maxX)
            : Random.Range(minY, maxY);
    }

    private float GetColumnSpread(int index)
    {
        float lo = movementDirection == MiniGameData.MovementDirection.Down ? minX : minY;
        float hi = movementDirection == MiniGameData.MovementDirection.Down ? maxX : maxY;
        if (columnCount <= 1)
            return Mathf.Lerp(lo, hi, 0.5f);
        return Mathf.Lerp(lo, hi, index / (float)(columnCount - 1));
    }

    private void SpawnAt(float spread)
    {
        if (projectilePrefab == null)
            return;

        Vector3 spawnPos;
        Vector2 moveDirection;

        switch (movementDirection)
        {
            case MiniGameData.MovementDirection.RightToLeft:
                spawnPos = new Vector3(horizontalSpawnEdgeX, spread, 0f);
                moveDirection = Vector2.left;
                break;

            case MiniGameData.MovementDirection.LeftToRight:
                spawnPos = new Vector3(-horizontalSpawnEdgeX, spread, 0f);
                moveDirection = Vector2.right;
                break;

            default:
                float y = overrideSpawnHeight ? spawnHeight : transform.position.y;
                spawnPos = new Vector3(spread, y, 0f);
                moveDirection = Vector2.down;
                break;
        }

        GameObject instance = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);

        Projectile projectile = instance.GetComponent<Projectile>();
        if (projectile != null)
            projectile.Configure(projectileSprite, projectileScale, projectileFallSpeed, projectileRotationSpeed, projectileDamage, moveDirection);
    }
}
