using UnityEngine;
using UnityEngine.Events;

// Lee la variante de minijuego activa y la aplica a la escena: configura el ProjectileSpawner
// (tipo de proyectil, sprite y patrón de spawn) y reconstruye la disposición de plataformas.
// La variante sale del enemigo del combate (EnemyCombatData.miniGameVariant); la del inspector
// es solo un respaldo para probar escenas sueltas.
// Además maneja cada partida: la reinicia desde cero cada vez que se activa el minijuego y avisa
// por 'onMiniGameFinished' cuando termina (ganada o perdida).
public class MiniGameRuntime : MonoBehaviour {
    [Header("Datos del minijuego")]
    [Tooltip("Respaldo para pruebas: se usa en la MiniGameScene suelta o al probar Code-Console con un enemigo sin minijuego. En un combate real (entrando desde un stage) nunca se usa.")]
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
    [SerializeField] private PlayerHealth playerHealth;

    [Header("Eventos")]
    [Tooltip("Se invoca al terminar la partida: true = ganó (sobrevivió o recogió todas las monedas), false = perdió (murió o se acabó el tiempo antes de recoger las monedas).")]
    public UnityEvent<bool> onMiniGameFinished;

    private EnemyCombatRuntime combatRuntime;
    private EnemyCombatData activeEnemy;
    private MiniGameData activeMiniGame;
    private Vector3 playerStartPosition;
    private bool sessionPending;
    private bool sessionActive;

    void Awake() {
        AutoAssignReferences();

        if (playerHealth != null) {
            playerStartPosition = playerHealth.transform.localPosition;
            playerHealth.onDied.AddListener(HandlePlayerDied);
        }

        if (miniGameTimer != null) {
            miniGameTimer.onTimeUp.AddListener(HandleTimeUp);
        }

        if (coinSpawner != null) {
            coinSpawner.onAllCoinsCollected.AddListener(HandleAllCoinsCollected);
        }
    }

    void OnDestroy() {
        if (playerHealth != null) {
            playerHealth.onDied.RemoveListener(HandlePlayerDied);
        }

        if (miniGameTimer != null) {
            miniGameTimer.onTimeUp.RemoveListener(HandleTimeUp);
        }

        if (coinSpawner != null) {
            coinSpawner.onAllCoinsCollected.RemoveListener(HandleAllCoinsCollected);
        }
    }

    void OnEnable() {
        // Start() solo corre la primera vez que se activa el objeto; OnEnable corre en cada apertura.
        // La partida se arranca en el siguiente Update y no aquí: así no se cambia el SetActive de
        // los hijos (timer, spawner de monedas) mientras Unity todavía está activando el prefab, y
        // en la primera apertura los demás componentes ya corrieron su Start().
        sessionPending = true;
    }

    void OnDisable() {
        sessionPending = false;
        sessionActive = false;
        StopGameplay();
        ClearSpawnedObjects();
    }

    void Update() {
        if (sessionPending) {
            sessionPending = false;
            BeginSession();
        }
    }

    // Deja el minijuego como nuevo y lo arranca: limpia los restos de la partida anterior,
    // devuelve al jugador a su posición inicial con la vida llena y vuelve a aplicar la variante.
    private void BeginSession() {
        activeEnemy = ResolveCurrentEnemy();
        activeMiniGame = ResolveMiniGame();

        if (activeEnemy != null && activeEnemy.miniGameVariant == null && activeMiniGame != null) {
            Debug.LogWarning($"MiniGameRuntime: '{activeEnemy.enemyDisplayName}' no tiene minijuego (miniGameVariant); se usa el de respaldo del inspector ({activeMiniGame.name}).");
        }

        ClearSpawnedObjects();
        ResetPlayer();
        ApplyMiniGameData();

        if (activeMiniGame == null) {
            RestartWithSceneDefaults();
        }

        sessionActive = true;
    }

    // Minijuego que corresponde al enemigo del combate. 'currentMiniGame' (inspector) es solo un respaldo
    // para pruebas: en un combate real, un enemigo sin minijuego propio devuelve null en vez de mostrar
    // el de otro enemigo. CombateUIFlowController lo consulta antes de abrir el minijuego.
    public MiniGameData ResolveMiniGame() {
        EnemyCombatData enemy = ResolveCurrentEnemy();
        if (enemy != null && enemy.miniGameVariant != null) {
            return enemy.miniGameVariant;
        }

        return IsRealCombat() ? null : currentMiniGame;
    }

    // El enemigo se toma del combate (EnemyCombatRuntime) y no del GameManager por separado: así el
    // minijuego coincide con lo que se ve en pantalla, también al probar Code-Console suelta (donde se
    // usa el enemigo del inspector de CombatManager). En la MiniGameScene suelta no hay combate.
    private EnemyCombatData ResolveCurrentEnemy() {
        if (combatRuntime == null) {
            combatRuntime = FindObjectOfType<EnemyCombatRuntime>(true);
        }

        return combatRuntime != null ? combatRuntime.CurrentEnemy : null;
    }

    // Se entró al combate desde un stage, no es una prueba de la escena suelta.
    private static bool IsRealCombat() {
        return GameManager.Instance != null && GameManager.Instance.CurrentEnemyAsset != null;
    }

    public void ApplyMiniGameData() {
        if (activeMiniGame == null) {
            Debug.LogWarning("MiniGameRuntime: no hay MiniGameData asignado; se usa la configuración por defecto de la escena.");
            return;
        }

        BuildPlatforms();
        ApplyEnemyVisual();

        if (projectileSpawner != null) {
            projectileSpawner.Configure(activeMiniGame);
        } else {
            Debug.LogWarning("MiniGameRuntime: no se encontró ProjectileSpawner en la escena.");
        }

        if (miniGameTimer != null) {
            miniGameTimer.SetTimerActive(activeMiniGame.useSurvivalTimer);
            if (activeMiniGame.useSurvivalTimer) {
                miniGameTimer.Configure(activeMiniGame.survivalTime);
                miniGameTimer.StartTimer();
            }
        }

        if (coinSpawner != null) {
            coinSpawner.SetSpawnerActive(activeMiniGame.useCoins);
            if (activeMiniGame.useCoins) {
                coinSpawner.Configure(activeMiniGame);
            }
        }
    }

    // Sin variante asignada: se reinicia lo que haya en la escena con los valores de su inspector.
    private void RestartWithSceneDefaults() {
        if (projectileSpawner != null) {
            projectileSpawner.BeginSpawning();
        }

        if (coinSpawner != null && coinSpawner.gameObject.activeInHierarchy) {
            coinSpawner.SpawnCoins();
        }

        if (miniGameTimer != null && miniGameTimer.gameObject.activeInHierarchy) {
            miniGameTimer.Restart();
        }
    }

    private void ResetPlayer() {
        if (playerHealth == null) {
            Debug.LogWarning("MiniGameRuntime: no se encontró PlayerHealth en la escena; el jugador no se reinicia entre partidas.");
            return;
        }

        playerHealth.ResetHealth();

        Transform player = playerHealth.transform;
        player.localPosition = playerStartPosition;

        Rigidbody2D body = player.GetComponent<Rigidbody2D>();
        if (body != null) {
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
        }

        // Mismo motivo que en BuildPlatforms: sin este flush la física no ve la posición recién asignada.
        Physics2D.SyncTransforms();
    }

    private void HandleTimeUp() {
        // Con monedas, el tiempo es el límite para recogerlas: si se acaba antes, se pierde.
        // Sin monedas, basta con sobrevivir hasta el final.
        bool coinsPending = coinSpawner != null && coinSpawner.gameObject.activeInHierarchy;
        EndSession(!coinsPending);
    }

    private void HandleAllCoinsCollected() {
        EndSession(true);
    }

    private void HandlePlayerDied() {
        EndSession(false);
    }

    private void EndSession(bool won) {
        if (!sessionActive) {
            return;
        }

        sessionActive = false;
        StopGameplay();
        onMiniGameFinished.Invoke(won);
    }

    private void StopGameplay() {
        if (projectileSpawner != null) {
            projectileSpawner.StopSpawning();
        }

        if (miniGameTimer != null) {
            miniGameTimer.StopTimer();
        }
    }

    private void ClearSpawnedObjects() {
        if (projectileSpawner != null) {
            projectileSpawner.ClearProjectiles();
        }

        if (coinSpawner != null) {
            coinSpawner.ClearCoins();
        }
    }

    private void ApplyEnemyVisual() {
        if (enemySpriteRenderer == null) {
            Debug.LogWarning("MiniGameRuntime: no se encontró el SpriteRenderer del enemigo en la escena.");
            return;
        }

        // Si la variante no trae sprite propio se usa el del enemigo del combate, para que nunca aparezca el de otro.
        Sprite enemySprite = activeMiniGame.enemySprite != null
            ? activeMiniGame.enemySprite
            : activeEnemy != null ? activeEnemy.enemySprite : null;
        if (enemySprite != null) {
            enemySpriteRenderer.sprite = enemySprite;
        }

        // (0,0) = conservar la escala del prefab; cualquier otro valor la sobreescribe.
        if (activeMiniGame.enemyScale.x > 0f && activeMiniGame.enemyScale.y > 0f) {
            Vector3 scale = activeMiniGame.enemyScale;
            enemySpriteRenderer.transform.localScale = new Vector3(scale.x, scale.y, enemySpriteRenderer.transform.localScale.z);
        }
    }

    private void BuildPlatforms() {
        if (platformsParent == null || activeMiniGame.platforms == null || activeMiniGame.platforms.Count == 0) {
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

        for (int i = 0; i < activeMiniGame.platforms.Count; i++) {
            PlatformPlacement placement = activeMiniGame.platforms[i];
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

        if (playerHealth == null) {
            playerHealth = FindObjectOfType<PlayerHealth>(true);
        }
    }
}
