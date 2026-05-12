using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class GameManagerStage1 : MonoBehaviour
{
    [Header("Stage GameObjects")]
    [SerializeField] private GameObject stage11Object;
    [SerializeField] private GameObject stage12Object;
    [SerializeField] private GameObject stage13Object;
    [SerializeField] private bool startOnStage11 = true;

    [Header("Scene Names (Optional)")]
    [SerializeField] private string stage11SceneName = "Stage1.1";
    [SerializeField] private string stage12SceneName = "Stage1.2";
    [SerializeField] private string stage13SceneName = "Stage1.3";

    [Header("Player Respawn")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Transform stage11SpawnPoint;
    [SerializeField] private Transform stage12SpawnPoint;
    [SerializeField] private Transform stage13SpawnPoint;
    [SerializeField] private bool resetPlayerPositionOnStageChange = true;

    [Header("Debug")]
    [SerializeField] private bool enableDebugKeys = true;

    private bool isLoading;

    void Start()
    {
        if (TryRestoreAfterCombat())
        {
            return;
        }

        SetInitialStage();
    }

    void Update()
    {
        if (!enableDebugKeys)
        {
            return;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        // Solo para pruebas rapidas.
        if (keyboard.digit1Key.wasPressedThisFrame)
        {
            ActivateStage11();
        }

        if (keyboard.digit2Key.wasPressedThisFrame)
        {
            ActivateStage12();
        }

        if (keyboard.digit3Key.wasPressedThisFrame)
        {
            ActivateStage13();
        }
    }

    public void ActivateStage11()
    {
        ActivateStage11(null);
    }

    public void ActivateStage11(Transform customSpawnPoint)
    {
        SetActiveStage(stage11Object);
        MovePlayerToSpawn(customSpawnPoint != null ? customSpawnPoint : stage11SpawnPoint);
    }

    public void ActivateStage12()
    {
        ActivateStage12(null);
    }

    public void ActivateStage12(Transform customSpawnPoint)
    {
        SetActiveStage(stage12Object);
        MovePlayerToSpawn(customSpawnPoint != null ? customSpawnPoint : stage12SpawnPoint);
    }

    public void ActivateStage13()
    {
        ActivateStage13(null);
    }

    public void ActivateStage13(Transform customSpawnPoint)
    {
        SetActiveStage(stage13Object);
        MovePlayerToSpawn(customSpawnPoint != null ? customSpawnPoint : stage13SpawnPoint);
    }

    public void RestartCurrentScene()
    {
        if (isLoading)
        {
            return;
        }

        string currentScene = SceneManager.GetActiveScene().name;
        LoadSceneByName(currentScene);
    }

    public void LoadStage11Scene()
    {
        LoadSceneByName(stage11SceneName);
    }

    public void LoadStage12Scene()
    {
        LoadSceneByName(stage12SceneName);
    }

    public void LoadStage13Scene()
    {
        LoadSceneByName(stage13SceneName);
    }

    private void SetInitialStage()
    {
        if (startOnStage11)
        {
            SetActiveStage(stage11Object);
        }
        else
        {
            SetActiveStage(stage12Object);
        }
    }

    private bool TryRestoreAfterCombat()
    {
        if (GameManager.Instance == null)
        {
            return false;
        }

        if (!GameManager.Instance.ConsumePendingStageRestore())
        {
            return false;
        }

        GameObject playerObject = playerTransform != null ? playerTransform.gameObject : GameObject.FindGameObjectWithTag("Player");
        if (playerObject == null)
        {
            return false;
        }

        RestoreStageForPlayerPosition(playerObject.transform.position);
        return true;
    }

    public void RestoreStageForPlayerPosition(Vector3 playerPosition)
    {
        GameObject stageToActivate = ResolveStageByPosition(playerPosition);
        if (stageToActivate == null)
        {
            stageToActivate = startOnStage11 ? stage11Object : stage12Object;
        }

        SetActiveStage(stageToActivate);
    }

    private GameObject ResolveStageByPosition(Vector3 playerPosition)
    {
        if (IsPositionInsideStage(stage11Object, playerPosition)) return stage11Object;
        if (IsPositionInsideStage(stage12Object, playerPosition)) return stage12Object;
        if (IsPositionInsideStage(stage13Object, playerPosition)) return stage13Object;

        return null;
    }

    private bool IsPositionInsideStage(GameObject stageObject, Vector3 worldPosition)
    {
        if (stageObject == null)
        {
            return false;
        }

        Collider stageCollider3D = stageObject.GetComponentInChildren<Collider>(true);
        if (stageCollider3D != null)
        {
            return stageCollider3D.bounds.Contains(worldPosition);
        }

        Collider2D stageCollider2D = stageObject.GetComponentInChildren<Collider2D>(true);
        if (stageCollider2D != null)
        {
            Vector2 point2D = new Vector2(worldPosition.x, worldPosition.y);
            return stageCollider2D.bounds.Contains(point2D);
        }

        Renderer[] renderers = stageObject.GetComponentsInChildren<Renderer>(true);
        if (renderers == null || renderers.Length == 0)
        {
            return false;
        }

        Bounds combined = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            combined.Encapsulate(renderers[i].bounds);
        }

        return combined.Contains(worldPosition);
    }

    private void SetActiveStage(GameObject stageToActivate)
    {
        if (stage11Object == null || stage12Object == null || stage13Object == null)
        {
            Debug.LogWarning("Asigna stage11Object, stage12Object y stage13Object en el Inspector.");
            return;
        }

        stage11Object.SetActive(stageToActivate == stage11Object);
        stage12Object.SetActive(stageToActivate == stage12Object);
        stage13Object.SetActive(stageToActivate == stage13Object);
    }

    private void MovePlayerToSpawn(Transform spawnPoint)
    {
        if (!resetPlayerPositionOnStageChange)
        {
            return;
        }

        if (playerTransform == null || spawnPoint == null)
        {
            return;
        }

        playerTransform.position = spawnPoint.position;

        Rigidbody rb = playerTransform.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        Rigidbody2D rb2D = playerTransform.GetComponent<Rigidbody2D>();
        if (rb2D != null)
        {
            rb2D.linearVelocity = Vector2.zero;
            rb2D.angularVelocity = 0f;
        }
    }

    private void LoadSceneByName(string sceneName)
    {
        if (isLoading || string.IsNullOrWhiteSpace(sceneName))
        {
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"La escena '{sceneName}' no esta en Build Settings.");
            return;
        }

        isLoading = true;
        SceneManager.LoadScene(sceneName);
    }
}
