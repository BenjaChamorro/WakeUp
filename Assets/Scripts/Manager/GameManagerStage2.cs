using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class GameManagerStage2 : MonoBehaviour
{
    

    [Header("Stage GameObjects")]
    [SerializeField] private GameObject stage21Object;
    [SerializeField] private GameObject stage22Object;
    [SerializeField] private GameObject stage23Object;
    [SerializeField] private GameObject stage24Object;
    [SerializeField] private bool startOnStage21 = true;

    [Header("Scene Names (Optional)")]
    [SerializeField] private string stage21SceneName = "Stage2.1";
    [SerializeField] private string stage22SceneName = "Stage2.2";
    [SerializeField] private string stage23SceneName = "Stage2.3";
    [SerializeField] private string stage24SceneName = "Stage2.4";

    [Header("Player Respawn")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Transform stage21SpawnPoint;
    [SerializeField] private Transform stage22SpawnPoint;
    [SerializeField] private Transform stage23SpawnPoint;
    [SerializeField] private Transform stage24SpawnPoint;
    [SerializeField] private bool resetPlayerPositionOnStageChange = true;

    [Header("Camera")]
    [SerializeField] private FollowCamera followCamera;

    [Header("Debug")]
    [SerializeField] private bool enableDebugKeys = true;

    private bool isLoading;
    private bool restorePlayerPositionAfterCombat;
    private bool suppressSceneStateRestore;
    private bool isInitializingScene = true;
    private const int RestoreSceneStateTimeoutFrames = 120;

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
            StartCoroutine(RestoreSavedSceneStateWhenReady());
            return;
        }

        isInitializingScene = false;

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.CommitCurrentState();
        }
    }

    private IEnumerator RestoreSavedSceneStateWhenReady()
    {
        int elapsedFrames = 0;
        while (elapsedFrames < RestoreSceneStateTimeoutFrames)
        {
            bool hasPlayer = ResolvePlayerTransform();
            bool hasCamera = ResolveFollowCamera();

            if (hasPlayer && hasCamera)
            {
                break;
            }

            elapsedFrames++;
            yield return null;
        }

        RestoreSavedSceneState();
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
            ActivateStage21();
        }

        if (keyboard.digit2Key.wasPressedThisFrame)
        {
            ActivateStage22();
        }

        if (keyboard.digit3Key.wasPressedThisFrame)
        {
            ActivateStage23();
        }

        if (keyboard.digit4Key.wasPressedThisFrame)
        {
            ActivateStage24();
        }
    }

    public void ActivateStage21()
    {
        ActivateStage21(null);
    }

    public void ActivateStage21(Transform customSpawnPoint)
    {
        SetActiveStage(stage21Object);
        MovePlayerToStagePosition(customSpawnPoint != null ? customSpawnPoint : stage21SpawnPoint);
        FinalizeStageActivation();
    }

    public void ActivateStage22()
    {
        ActivateStage22(null);
    }

    public void ActivateStage22(Transform customSpawnPoint)
    {
        SetActiveStage(stage22Object);
        MovePlayerToStagePosition(customSpawnPoint != null ? customSpawnPoint : stage22SpawnPoint);
        FinalizeStageActivation();
    }

    public void ActivateStage23()
    {
        ActivateStage23(null);
    }

    public void ActivateStage23(Transform customSpawnPoint)
    {
        SetActiveStage(stage23Object);
        MovePlayerToStagePosition(customSpawnPoint != null ? customSpawnPoint : stage23SpawnPoint);
        FinalizeStageActivation();
    }

    public void ActivateStage24()
    {
        ActivateStage24(null);
    }

    public void ActivateStage24(Transform customSpawnPoint)
    {
        SetActiveStage(stage24Object);
        MovePlayerToStagePosition(customSpawnPoint != null ? customSpawnPoint : stage24SpawnPoint);
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

    public void LoadStage21Scene()
    {
        LoadSceneByName(stage21SceneName);
    }

    public void LoadStage22Scene()
    {
        LoadSceneByName(stage22SceneName);
    }

    public void LoadStage23Scene()
    {
        LoadSceneByName(stage23SceneName);
    }

    public void LoadStage24Scene()
    {
        LoadSceneByName(stage24SceneName);
    }

    private void SetInitialStage()
    {
        if (startOnStage21)
        {
            ActivateStage21();
        }
        else
        {
            ActivateStage22();
        }
    }

    private void SetActiveStage(GameObject stageToActivate)
    {
        if (stage21Object == null || stage22Object == null || stage23Object == null || stage24Object == null)
        {
            Debug.LogWarning("Asigna stage21Object, stage22Object, stage23Object y stage24Object en el Inspector.");
            return;
        }

        stage21Object.SetActive(stageToActivate == stage21Object);
        stage22Object.SetActive(stageToActivate == stage22Object);
        stage23Object.SetActive(stageToActivate == stage23Object);
        stage24Object.SetActive(stageToActivate == stage24Object);

        SaveActiveStage(stageToActivate);
    }

    private bool TryRestoreSavedStage()
    {
        if (SaveManager.Instance == null) return false;

        if (!SaveManager.Instance.GetSavedActiveStage(out string savedStageName))
        {
            return false;
        }

        if (savedStageName == "Stage2.1")
        {
            ActivateStage21();
            return true;
        }

        if (savedStageName == "Stage2.2")
        {
            ActivateStage22();
            return true;
        }

        if (savedStageName == "Stage2.3")
        {
            ActivateStage23();
            return true;
        }

        if (savedStageName == "Stage2.4")
        {
            ActivateStage24();
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
            ResolveFollowCamera();
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

        if (activeStage == stage21Object)
        {
            SaveManager.Instance.SaveActiveStage("Stage2.1");
            return;
        }

        if (activeStage == stage22Object)
        {
            SaveManager.Instance.SaveActiveStage("Stage2.2");
            return;
        }

        if (activeStage == stage23Object)
        {
            SaveManager.Instance.SaveActiveStage("Stage2.3");
        }

        if (activeStage == stage24Object)
        {
            SaveManager.Instance.SaveActiveStage("Stage2.4");
        }
    }

    private void MovePlayerToSpawn(Transform spawnPoint)
    {
        if (!resetPlayerPositionOnStageChange)
        {
            return;
        }

        ResolvePlayerTransform();

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
        ResolvePlayerTransform();

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

        bool restoredPlayerPosition = TryRestoreSavedPlayerPosition();
        bool restoredCameraBounds = TryRestoreSavedCameraBounds();

        if (!restoredPlayerPosition || !restoredCameraBounds)
        {
            Debug.LogWarning("[GameManagerStage2] No se pudieron restaurar todos los datos de escena a tiempo.");
        }
    }

    private bool TryRestoreSavedPlayerPosition()
    {
        if (!ResolvePlayerTransform() || SaveManager.Instance == null)
        {
            return false;
        }

        Vector3? savedPosition = SaveManager.Instance.GetPlayerPosition(SceneManager.GetActiveScene().name);
        if (!savedPosition.HasValue)
        {
            return false;
        }

        playerTransform.position = savedPosition.Value;
        return true;
    }

    private bool TryRestoreSavedCameraBounds()
    {
        if (!SaveManager.Instance.GetCameraBounds(SceneManager.GetActiveScene().name, out float minX, out float maxX, out float minY, out float maxY))
        {
            return false;
        }

        if (followCamera == null)
        {
            return false;
        }

        followCamera.SetCameraBounds(minX, maxX, minY, maxY);
        return true;
    }

    private void LoadSavedPlayerPosition()
    {
        if (!ResolvePlayerTransform() || SaveManager.Instance == null)
        {
            return;
        }

        Vector3? savedPosition = SaveManager.Instance.GetPlayerPosition(SceneManager.GetActiveScene().name);
        if (!savedPosition.HasValue)
        {
            return;
        }

        playerTransform.position = savedPosition.Value;
    }

    private void BindCameraToPlayer()
    {
        if (!ResolveFollowCamera())
        {
            return;
        }

        if (ResolvePlayerTransform() && playerTransform != null)
        {
            followCamera.SetTarget(playerTransform);
        }
    }

    private bool ResolvePlayerTransform()
    {
        if (playerTransform != null)
        {
            return true;
        }

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            playerTransform = playerObject.transform;
        }

        return playerTransform != null;
    }

    private bool ResolveFollowCamera()
    {
        if (followCamera != null)
        {
            return true;
        }

        followCamera = FindObjectOfType<FollowCamera>();
        if (followCamera == null && Camera.main != null)
        {
            followCamera = Camera.main.GetComponent<FollowCamera>();
        }

        return followCamera != null;
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

        if (stageName == "Stage2.1")
        {
            ActivateStage21();
            return;
        }

        if (stageName == "Stage2.2")
        {
            ActivateStage22();
            return;
        }

        if (stageName == "Stage2.3")
        {
            ActivateStage23();
            return;
        }

        if (stageName == "Stage2.4")
        {
            ActivateStage24();
        }
    }

    public string GetActiveStage()
    {
        if (stage21Object.activeSelf) return "Stage2.1";
        if (stage22Object.activeSelf) return "Stage2.2";
        if (stage23Object.activeSelf) return "Stage2.3";
        if (stage24Object.activeSelf) return "Stage2.4";
        return string.Empty;
    }
}
