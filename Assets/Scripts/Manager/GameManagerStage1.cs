using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class GameManagerStage1 : MonoBehaviour
{
    

    [Header("Stage GameObjects")]
    [SerializeField] private GameObject stage11Object;
    [SerializeField] private GameObject stage12Object;
    [SerializeField] private GameObject stage13Object;
    [SerializeField] private GameObject stage14Object;
    [SerializeField] private bool startOnStage11 = true;

    [Header("Scene Names (Optional)")]
    [SerializeField] private string stage11SceneName = "Stage1.1";
    [SerializeField] private string stage12SceneName = "Stage1.2";
    [SerializeField] private string stage13SceneName = "Stage1.3";
    [SerializeField] private string stage14SceneName = "Stage1.4";

    [Header("Player Respawn")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Transform stage11SpawnPoint;
    [SerializeField] private Transform stage12SpawnPoint;
    [SerializeField] private Transform stage13SpawnPoint;
    [SerializeField] private Transform stage14SpawnPoint;
    [SerializeField] private bool resetPlayerPositionOnStageChange = true;

    [Header("Camera")]
    [SerializeField] private FollowCamera followCamera;

    [Header("Debug")]
    [SerializeField] private bool enableDebugKeys = true;

    private bool isLoading;
    private bool restorePlayerPositionAfterCombat;
    private bool suppressSceneStateRestore;
    private bool isInitializingScene = true;

    void Start()
    {
        suppressSceneStateRestore = SaveManager.SuppressSceneStateRestoreOnNextSceneLoad;
        SaveManager.SuppressSceneStateRestoreOnNextSceneLoad = false;

        if (SaveManager.Instance != null)
        {
            restorePlayerPositionAfterCombat = SaveManager.Instance.ConsumeReturnFromCombatFlag();
        }

        if (!TryRestoreSavedStage())
        {
            SetInitialStage();
        }

        ApplySceneCameraDefaults();

        if (!restorePlayerPositionAfterCombat && !suppressSceneStateRestore)
        {
            RestoreSavedSceneState();
        }

        isInitializingScene = false;

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.CommitCurrentState();
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

        if (keyboard.digit4Key.wasPressedThisFrame)
        {
            ActivateStage14();
        }
    }

    public void ActivateStage11()
    {
        ActivateStage11(null);
    }

    public void ActivateStage11(Transform customSpawnPoint)
    {
        SetActiveStage(stage11Object);
        MovePlayerToStagePosition(customSpawnPoint != null ? customSpawnPoint : stage11SpawnPoint);
        FinalizeStageActivation();
    }

    public void ActivateStage12()
    {
        ActivateStage12(null);
    }

    public void ActivateStage12(Transform customSpawnPoint)
    {
        SetActiveStage(stage12Object);
        MovePlayerToStagePosition(customSpawnPoint != null ? customSpawnPoint : stage12SpawnPoint);
        FinalizeStageActivation();
    }

    public void ActivateStage13()
    {
        ActivateStage13(null);
    }

    public void ActivateStage13(Transform customSpawnPoint)
    {
        SetActiveStage(stage13Object);
        MovePlayerToStagePosition(customSpawnPoint != null ? customSpawnPoint : stage13SpawnPoint);
        FinalizeStageActivation();
    }

    public void ActivateStage14()
    {
        ActivateStage14(null);
    }

    public void ActivateStage14(Transform customSpawnPoint)
    {
        SetActiveStage(stage14Object);
        MovePlayerToStagePosition(customSpawnPoint != null ? customSpawnPoint : stage14SpawnPoint);
        FinalizeStageActivation();
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

    public void LoadStage14Scene()
    {
        LoadSceneByName(stage14SceneName);
    }

    private void SetInitialStage()
    {
        if (startOnStage11)
        {
            ActivateStage11();
        }
        else
        {
            ActivateStage12();
        }
    }

    private void SetActiveStage(GameObject stageToActivate)
    {
        if (stage11Object == null || stage12Object == null || stage13Object == null || stage14Object == null)
        {
            Debug.LogWarning("Asigna stage11Object, stage12Object, stage13Object y stage14Object en el Inspector.");
            return;
        }

        stage11Object.SetActive(stageToActivate == stage11Object);
        stage12Object.SetActive(stageToActivate == stage12Object);
        stage13Object.SetActive(stageToActivate == stage13Object);
        stage14Object.SetActive(stageToActivate == stage14Object);

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
            ActivateStage11();
            return true;
        }

        if (savedStageName == "Stage1.2")
        {
            ActivateStage12();
            return true;
        }

        if (savedStageName == "Stage1.3")
        {
            ActivateStage13();
            return true;
        }

        if (savedStageName == "Stage1.4")
        {
            ActivateStage14();
            return true;
        }

        return false;
    }

    private void FinalizeStageActivation()
    {
        if (isInitializingScene)
        {
            return;
        }

        ApplySceneCameraDefaults();

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.CommitCurrentState();
        }
    }

    private void ApplySceneCameraDefaults()
    {
        if (followCamera == null)
        {
            followCamera = FindObjectOfType<FollowCamera>();
        }

        if (followCamera != null)
        {
            followCamera.ResetToSceneDefaults();
            BindCameraToPlayer();
        }
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

        if (activeStage == stage14Object)
        {
            SaveManager.Instance.SaveActiveStage("Stage1.4");
        }
    }

    private void MovePlayerToSpawn(Transform spawnPoint)
    {
        if (!resetPlayerPositionOnStageChange)
        {
            return;
        }

        if (playerTransform == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                playerTransform = playerObject.transform;
            }
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

    private void MovePlayerToStagePosition(Transform spawnPoint)
    {
        if (restorePlayerPositionAfterCombat)
        {
            MovePlayerToSavedPosition(spawnPoint);
            restorePlayerPositionAfterCombat = false;
            return;
        }

        MovePlayerToSpawn(spawnPoint);
    }

    private void MovePlayerToSavedPosition(Transform fallbackSpawnPoint)
    {
        if (playerTransform == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                playerTransform = playerObject.transform;
            }
        }

        if (playerTransform == null)
        {
            return;
        }

        if (SaveManager.Instance != null)
        {
            Vector3? savedPosition = SaveManager.Instance.GetPlayerPosition();
            if (savedPosition.HasValue)
            {
                playerTransform.position = savedPosition.Value;
                return;
            }
        }

        MovePlayerToSpawn(fallbackSpawnPoint);
    }

    private void RestoreSavedSceneState()
    {
        if (SaveManager.Instance == null)
        {
            return;
        }

        LoadSavedPlayerPosition();

        if (SaveManager.Instance.GetCameraBounds(out float minX, out float maxX, out float minY, out float maxY))
        {
            if (followCamera == null)
            {
                followCamera = FindObjectOfType<FollowCamera>();
            }

            if (followCamera != null)
            {
                followCamera.SetCameraBounds(minX, maxX, minY, maxY);
            }
        }
    }

    private void LoadSavedPlayerPosition()
    {
        if (playerTransform == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                playerTransform = playerObject.transform;
            }
        }

        if (playerTransform == null || SaveManager.Instance == null)
        {
            return;
        }

        Vector3? savedPosition = SaveManager.Instance.GetPlayerPosition();
        if (!savedPosition.HasValue)
        {
            return;
        }

        playerTransform.position = savedPosition.Value;
    }

    private void BindCameraToPlayer()
    {
        if (followCamera == null)
        {
            return;
        }

        if (playerTransform == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                playerTransform = playerObject.transform;
            }
        }

        if (playerTransform != null)
        {
            followCamera.SetTarget(playerTransform);
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
            ActivateStage11();
            return;
        }

        if (stageName == "Stage1.2")
        {
            ActivateStage12();
            return;
        }

        if (stageName == "Stage1.3")
        {
            ActivateStage13();
            return;
        }

        if (stageName == "Stage1.4")
        {
            ActivateStage14();
        }
    }

    public string GetActiveStage()
    {
        if (stage11Object.activeSelf) return "Stage1.1";
        if (stage12Object.activeSelf) return "Stage1.2";
        if (stage13Object.activeSelf) return "Stage1.3";
        if (stage14Object.activeSelf) return "Stage1.4";
        return string.Empty;
    }
}
