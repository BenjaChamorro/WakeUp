using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    private const string SavedStageKey = "SavedStage";
    private const string SavedPlayerPosKey = "SavedPlayerPos";
    private const string SavedCameraPosKey = "SavedCameraPos";
    private const string SavedCameraLimitsEnabledKey = "SavedCameraLimitsEnabled";
    private const string SavedCameraMinXKey = "SavedCameraMinX";
    private const string SavedCameraMaxXKey = "SavedCameraMaxX";
    private const string SavedCameraMinYKey = "SavedCameraMinY";
    private const string SavedCameraMaxYKey = "SavedCameraMaxY";
    public bool OnCombat { get; private set; }
    public Object CurrentEnemyAsset { get; private set; }
    private bool pendingStageRestore;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    // Guarda el índice de la escena actual y la posición del jugador (si existe)
    public void SaveCurrentStage()
    {
        int current = SceneManager.GetActiveScene().buildIndex;
        PlayerPrefs.SetInt(SavedStageKey, current);
        SavePlayerPosition();
        SaveCameraState();
        PlayerPrefs.Save();
    }

    private void SavePlayerPosition()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;
        Vector3 p = player.transform.position;
        PlayerPrefs.SetFloat(SavedPlayerPosKey + "_x", p.x);
        PlayerPrefs.SetFloat(SavedPlayerPosKey + "_y", p.y);
        PlayerPrefs.SetFloat(SavedPlayerPosKey + "_z", p.z);
    }

    private void SaveCameraState()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null) return;

        Vector3 p = mainCamera.transform.position;
        PlayerPrefs.SetFloat(SavedCameraPosKey + "_x", p.x);
        PlayerPrefs.SetFloat(SavedCameraPosKey + "_y", p.y);
        PlayerPrefs.SetFloat(SavedCameraPosKey + "_z", p.z);

        FollowCamera followCamera = mainCamera.GetComponent<FollowCamera>();
        if (followCamera == null) return;

        followCamera.GetCameraState(out bool limitsEnabled, out float minX, out float maxX, out float minY, out float maxY);
        PlayerPrefs.SetInt(SavedCameraLimitsEnabledKey, limitsEnabled ? 1 : 0);
        PlayerPrefs.SetFloat(SavedCameraMinXKey, minX);
        PlayerPrefs.SetFloat(SavedCameraMaxXKey, maxX);
        PlayerPrefs.SetFloat(SavedCameraMinYKey, minY);
        PlayerPrefs.SetFloat(SavedCameraMaxYKey, maxY);
    }

    // Llamar para entrar en combate (por defecto la escena de combate es 1)
    public void EnterCombat(int combatSceneIndex = 1, Object enemyAsset = null)
    {
        SaveCurrentStage();
        OnCombat = true;
        CurrentEnemyAsset = enemyAsset;
        SceneManager.LoadScene(combatSceneIndex);
    }

    // Llamar para salir del combate y volver al stage guardado
    public void ExitCombatAndReturn()
    {
        OnCombat = false;
        CurrentEnemyAsset = null;

        if (PlayerPrefs.HasKey(SavedStageKey))
        {
            pendingStageRestore = true;
            int saved = PlayerPrefs.GetInt(SavedStageKey);
            SceneManager.sceneLoaded += OnSceneLoadedRestore;
            SceneManager.LoadScene(saved);
            PlayerPrefs.DeleteKey(SavedStageKey);
        }
        else
        {
            pendingStageRestore = false;
            SceneManager.LoadScene(0);
        }
    }

    public bool ConsumePendingStageRestore()
    {
        bool result = pendingStageRestore;
        pendingStageRestore = false;
        return result;
    }

    private void OnSceneLoadedRestore(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= OnSceneLoadedRestore;
        RestorePlayerPosition();
        RestoreCameraState();
    }

    private void RestorePlayerPosition()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;
        if (PlayerPrefs.HasKey(SavedPlayerPosKey + "_x"))
        {
            float x = PlayerPrefs.GetFloat(SavedPlayerPosKey + "_x");
            float y = PlayerPrefs.GetFloat(SavedPlayerPosKey + "_y");
            float z = PlayerPrefs.GetFloat(SavedPlayerPosKey + "_z");
            player.transform.position = new Vector3(x, y, z);
        }
    }

    private void RestoreCameraState()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null) return;

        FollowCamera followCamera = mainCamera.GetComponent<FollowCamera>();
        if (followCamera != null && PlayerPrefs.HasKey(SavedCameraLimitsEnabledKey))
        {
            bool limitsEnabled = PlayerPrefs.GetInt(SavedCameraLimitsEnabledKey) == 1;
            float minX = PlayerPrefs.GetFloat(SavedCameraMinXKey);
            float maxX = PlayerPrefs.GetFloat(SavedCameraMaxXKey);
            float minY = PlayerPrefs.GetFloat(SavedCameraMinYKey);
            float maxY = PlayerPrefs.GetFloat(SavedCameraMaxYKey);
            followCamera.RestoreCameraState(limitsEnabled, minX, maxX, minY, maxY);
        }

        if (PlayerPrefs.HasKey(SavedCameraPosKey + "_x"))
        {
            float x = PlayerPrefs.GetFloat(SavedCameraPosKey + "_x");
            float y = PlayerPrefs.GetFloat(SavedCameraPosKey + "_y");
            float z = PlayerPrefs.GetFloat(SavedCameraPosKey + "_z");
            mainCamera.transform.position = new Vector3(x, y, z);
        }
    }

    // Limpia cualquier progreso guardado (opcional)
    public void ClearSavedProgress()
    {
        PlayerPrefs.DeleteKey(SavedStageKey);
        PlayerPrefs.DeleteKey(SavedPlayerPosKey + "_x");
        PlayerPrefs.DeleteKey(SavedPlayerPosKey + "_y");
        PlayerPrefs.DeleteKey(SavedPlayerPosKey + "_z");
        PlayerPrefs.DeleteKey(SavedCameraPosKey + "_x");
        PlayerPrefs.DeleteKey(SavedCameraPosKey + "_y");
        PlayerPrefs.DeleteKey(SavedCameraPosKey + "_z");
        PlayerPrefs.DeleteKey(SavedCameraLimitsEnabledKey);
        PlayerPrefs.DeleteKey(SavedCameraMinXKey);
        PlayerPrefs.DeleteKey(SavedCameraMaxXKey);
        PlayerPrefs.DeleteKey(SavedCameraMinYKey);
        PlayerPrefs.DeleteKey(SavedCameraMaxYKey);
    }
}
