using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class GameManagerStage1 : MonoBehaviour
{
    [Header("Stage GameObjects")]
    [SerializeField] private GameObject stage11Object;
    [SerializeField] private GameObject stage12Object;
    [SerializeField] private bool startOnStage11 = true;

    [Header("Scene Names (Optional)")]
    [SerializeField] private string stage11SceneName = "Stage1.1";
    [SerializeField] private string stage12SceneName = "Stage1.2";

    [Header("Player Respawn")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Transform stage11SpawnPoint;
    [SerializeField] private Transform stage12SpawnPoint;
    [SerializeField] private bool resetPlayerPositionOnStageChange = true;

    [Header("Debug")]
    [SerializeField] private bool enableDebugKeys = true;

    private bool isLoading;

    void Start()
    {
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
    }

    public void ActivateStage11()
    {
        SetActiveStage(stage11Object);
        MovePlayerToSpawn(stage11SpawnPoint);
    }

    public void ActivateStage12()
    {
        SetActiveStage(stage12Object);
        MovePlayerToSpawn(stage12SpawnPoint);
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
        if (stage11Object == null || stage12Object == null)
        {
            Debug.LogWarning("Asigna stage11Object y stage12Object en el Inspector.");
            return;
        }

        stage11Object.SetActive(stageToActivate == stage11Object);
        stage12Object.SetActive(stageToActivate == stage12Object);
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
