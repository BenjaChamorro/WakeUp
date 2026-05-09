using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    private const string SavedStageKey = "SavedStage";
    private const string SavedPlayerPosKey = "SavedPlayerPos";
    public bool OnCombat { get; private set; }
    public Object CurrentEnemyAsset { get; private set; }

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
            int saved = PlayerPrefs.GetInt(SavedStageKey);
            SceneManager.sceneLoaded += OnSceneLoadedRestore;
            SceneManager.LoadScene(saved);
            PlayerPrefs.DeleteKey(SavedStageKey);
        }
        else
        {
            SceneManager.LoadScene(0);
        }
    }

    private void OnSceneLoadedRestore(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= OnSceneLoadedRestore;
        RestorePlayerPosition();
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

    // Limpia cualquier progreso guardado (opcional)
    public void ClearSavedProgress()
    {
        PlayerPrefs.DeleteKey(SavedStageKey);
        PlayerPrefs.DeleteKey(SavedPlayerPosKey + "_x");
        PlayerPrefs.DeleteKey(SavedPlayerPosKey + "_y");
        PlayerPrefs.DeleteKey(SavedPlayerPosKey + "_z");
    }
}
