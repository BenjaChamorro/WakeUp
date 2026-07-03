using UnityEngine;

// Lee la variante de minijuego activa y la aplica a la escena: configura el ProjectileSpawner
// (tipo de proyectil, sprite y patrón de spawn) y reconstruye la disposición de plataformas.
// Espeja el patrón de EnemyCombatRuntime: la variante llega desde el combate, pero también puede
// asignarse a mano en el inspector para probar la MiniGameScene de forma aislada.
public class MiniGameRuntime : MonoBehaviour {
    [Header("Datos del minijuego")]
    [Tooltip("Variante usada si NO se entra desde un combate (para probar la escena suelta).")]
    [SerializeField] private MiniGameData currentMiniGame;

    [Header("Referencias de escena")]
    [SerializeField] private ProjectileSpawner projectileSpawner;
    [SerializeField] private SpriteRenderer enemySpriteRenderer;
    [SerializeField] private Transform platformsParent;
    [Tooltip("Plantilla de plataforma. Si está vacío, se clona la primera plataforma existente bajo 'platformsParent'.")]
    [SerializeField] private GameObject platformPrefab;
    [Tooltip("Conserva las plataformas con tag 'Floor' al reconstruir el escenario (para que el suelo no desaparezca).")]
    [SerializeField] private bool preserveFloor = true;
    [SerializeField] private MiniGameTimer miniGameTimer;
    [SerializeField] private CoinSpawner coinSpawner;

    void Awake() {
        AutoAssignReferences();
    }

    void Start() {
        // Si venimos de un combate, la variante del minijuego viaja dentro del EnemyCombatData activo.
        if (GameManager.Instance != null
            && GameManager.Instance.CurrentEnemyAsset is EnemyCombatData enemy
            && enemy.miniGameVariant != null) {
            currentMiniGame = enemy.miniGameVariant;
        }

        ApplyMiniGameData();
    }

    public void ApplyMiniGameData() {
        if (currentMiniGame == null) {
            Debug.LogWarning("MiniGameRuntime: no hay MiniGameData asignado; se usa la configuración por defecto de la escena.");
            return;
        }

        BuildPlatforms();
        ApplyEnemyVisual();

        if (projectileSpawner != null) {
            projectileSpawner.Configure(currentMiniGame);
        } else {
            Debug.LogWarning("MiniGameRuntime: no se encontró ProjectileSpawner en la escena.");
        }

        if (miniGameTimer != null) {
            miniGameTimer.SetTimerActive(currentMiniGame.useSurvivalTimer);
            if (currentMiniGame.useSurvivalTimer) {
                miniGameTimer.Configure(currentMiniGame.survivalTime);
            }
        }

        if (coinSpawner != null) {
            coinSpawner.SetSpawnerActive(currentMiniGame.useCoins);
            if (currentMiniGame.useCoins) {
                coinSpawner.Configure(currentMiniGame);
            }
        }
    }

    private void ApplyEnemyVisual() {
        if (enemySpriteRenderer == null) {
            Debug.LogWarning("MiniGameRuntime: no se encontró el SpriteRenderer del enemigo en la escena.");
            return;
        }

        if (currentMiniGame.enemySprite != null) {
            enemySpriteRenderer.sprite = currentMiniGame.enemySprite;
        }

        // (0,0) = conservar la escala del prefab; cualquier otro valor la sobreescribe.
        if (currentMiniGame.enemyScale.x > 0f && currentMiniGame.enemyScale.y > 0f) {
            Vector3 scale = currentMiniGame.enemyScale;
            enemySpriteRenderer.transform.localScale = new Vector3(scale.x, scale.y, enemySpriteRenderer.transform.localScale.z);
        }
    }

    private void BuildPlatforms() {
        if (platformsParent == null || currentMiniGame.platforms == null || currentMiniGame.platforms.Count == 0) {
            return;
        }

        // Elige la plantilla: el prefab asignado, o la primera plataforma de la escena que NO sea suelo.
        // Nunca se usa el Floor como molde (si no, al clonarlo se desactivaría y desaparecería).
        GameObject template = platformPrefab;
        bool templateIsSceneChild = false;
        if (template == null) {
            for (int i = 0; i < platformsParent.childCount; i++) {
                GameObject child = platformsParent.GetChild(i).gameObject;
                if (preserveFloor && child.CompareTag("Floor")) {
                    continue;
                }
                template = child;
                templateIsSceneChild = true;
                break;
            }
        }

        if (template == null) {
            Debug.LogWarning("MiniGameRuntime: no hay plantilla de plataforma para clonar. Asigna 'Platform Prefab' en el MiniGameRuntime cuando el prefab del minijuego ya no tenga plataformas propias (solo el Floor).");
            return;
        }

        // Si la plantilla es una plataforma de la propia escena, la dejamos como molde desactivado.
        if (templateIsSceneChild) {
            template.SetActive(false);
        }

        // Limpia las plataformas previas (excepto la plantilla-molde y, si se pidió, el suelo).
        for (int i = platformsParent.childCount - 1; i >= 0; i--) {
            GameObject child = platformsParent.GetChild(i).gameObject;
            if (templateIsSceneChild && child == template) {
                continue;
            }
            if (preserveFloor && child.CompareTag("Floor")) {
                continue;
            }
            Destroy(child);
        }

        for (int i = 0; i < currentMiniGame.platforms.Count; i++) {
            PlatformPlacement placement = currentMiniGame.platforms[i];
            if (placement == null) {
                continue;
            }

            GameObject go = Instantiate(template, platformsParent);
            go.SetActive(true);
            go.name = string.IsNullOrWhiteSpace(placement.platformName) ? "Plataform" : placement.platformName;

            if (!string.IsNullOrWhiteSpace(placement.tag)) {
                try {
                    go.tag = placement.tag;
                } catch (UnityException) {
                    Debug.LogWarning($"MiniGameRuntime: el tag '{placement.tag}' no está definido en el proyecto; se deja el tag de la plantilla.");
                }
            }

            go.transform.localPosition = placement.localPosition;
            go.transform.localScale = placement.localScale == Vector3.zero ? Vector3.one : placement.localScale;

            Platform platform = go.GetComponent<Platform>();
            if (platform != null) {
                platform.SetLayout(placement.size, placement.borderThickness);
            }
        }

        // "Auto Sync Transforms" está desactivado en el proyecto (Physics2D Settings), así que sin
        // este flush manual el motor de físicas no se entera de la posición/escala recién asignadas
        // hasta el siguiente paso físico. Durante ese primer paso, el collider de la plataforma clonada
        // queda momentáneamente en el origen (0,0,0), lo que puede solaparse por completo con el Player
        // u otros colliders y hacer que Box2D "explote" la posición a un valor absurdo al corregirlo.
        Physics2D.SyncTransforms();
    }

    private void AutoAssignReferences() {
        if (projectileSpawner == null) {
            projectileSpawner = FindObjectOfType<ProjectileSpawner>(true);
        }

        if (enemySpriteRenderer == null) {
            GameObject enemy = GameObject.Find("Enemy");
            if (enemy != null) {
                enemySpriteRenderer = enemy.GetComponent<SpriteRenderer>();
            }
        }

        if (platformsParent == null) {
            GameObject container = GameObject.Find("Plataforms");
            if (container != null) {
                platformsParent = container.transform;
            }
        }

        if (miniGameTimer == null) {
            miniGameTimer = FindObjectOfType<MiniGameTimer>(true);
        }

        if (coinSpawner == null) {
            coinSpawner = FindObjectOfType<CoinSpawner>(true);
        }
    }
}
