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
        if (!TryRestoreSavedStage())
        {
            SetInitialStage();
        }
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

        SaveActiveStage(stageToActivate);
    }

    private bool TryRestoreSavedStage()
    {
        if (SaveManager.Instance == null) return false;

        if (!SaveManager.Instance.GetSavedActiveStage(out string savedStageName))
        {
            return false;
        }

        if (savedStageName == "Stage1.1")
        {
            SetActiveStage(stage11Object);
            return true;
        }

        if (savedStageName == "Stage1.2")
        {
            SetActiveStage(stage12Object);
            return true;
        }

        if (savedStageName == "Stage1.3")
        {
            SetActiveStage(stage13Object);
            return true;
        }

        return false;
    }

    private void SaveActiveStage(GameObject activeStage)
    {
        if (activeStage == null || SaveManager.Instance == null)
        {
            return;
        }

        if (activeStage == stage11Object)
        {
            SaveManager.Instance.SaveActiveStage("Stage1.1");
            return;
        }

        if (activeStage == stage12Object)
        {
            SaveManager.Instance.SaveActiveStage("Stage1.2");
            return;
        }

        if (activeStage == stage13Object)
        {
            SaveManager.Instance.SaveActiveStage("Stage1.3");
        }
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

    public void ActivateSavedStage(string stageName)
    {
        if (string.IsNullOrEmpty(stageName)) return;

        if (stageName == "Stage1.1")
        {
            SetActiveStage(stage11Object);
            return;
        }

        if (stageName == "Stage1.2")
        {
            SetActiveStage(stage12Object);
            return;
        }

        if (stageName == "Stage1.3")
        {
            SetActiveStage(stage13Object);
        }
    }

    public string GetActiveStage()
    {
        if (stage11Object.activeSelf) return "Stage1.1";
        if (stage12Object.activeSelf) return "Stage1.2";
        if (stage13Object.activeSelf) return "Stage1.3";
        return string.Empty;
    }
}
