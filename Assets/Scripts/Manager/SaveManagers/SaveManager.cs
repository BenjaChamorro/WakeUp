using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.IO;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }
    public static bool SuppressPreferredSceneLoadOnNextBoot { get; set; }
    public static bool SuppressSceneStateRestoreOnNextSceneLoad { get; set; }
    public const string DefaultUnlockedBlockId = "print";
    public const int MaxSlots = 3;

    private const string LastSlotPrefsKey = "WakeUp.LastSaveSlot";
    private const string LegacySaveFileName = "save.json";

    // Partida elegida en el menu. 0 = ninguna (escena abierta suelta en el editor): se usa la ultima jugada.
    public static int SelectedSlot { get; set; }

    public int CurrentSlot { get; private set; }

    private SaveData saveData;
    private string savePath;
    private readonly HashSet<string> pendingEventIds = new HashSet<string>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeSavePath();
            LoadGame();
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void OnDisable()
    {
        if (Application.isPlaying)
        {
            AutoSaveCurrentState();
        }
    }

    private void InitializeSavePath()
    {
        MigrateLegacySave();

        int slot = SelectedSlot > 0 ? SelectedSlot : GetLastPlayedSlot();
        if (slot <= 0)
        {
            slot = 1;
        }

        SetCurrentSlot(slot);
    }

    private void SetCurrentSlot(int slot)
    {
        CurrentSlot = slot;
        SelectedSlot = slot;
        savePath = GetSlotPath(slot);
        PlayerPrefs.SetInt(LastSlotPrefsKey, slot);
        PlayerPrefs.Save();
    }

    // ========== SAVE SLOTS ==========
    public static string GetSaveDirectory()
    {
        string saveDir = Path.Combine(Application.dataPath, "saves");
        try
        {
            if (!Directory.Exists(saveDir))
            {
                Directory.CreateDirectory(saveDir);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[SaveManager] No se pudo crear la carpeta 'Assets/saves': " + e.Message + ". Usando persistentDataPath en su lugar.");
            saveDir = Application.persistentDataPath;
        }

        return saveDir.Replace("\\", "/");
    }

    public static string GetSlotPath(int slot)
    {
        return GetSaveDirectory() + "/save" + slot + ".json";
    }

    public static bool SlotExists(int slot)
    {
        return slot >= 1 && slot <= MaxSlots && File.Exists(GetSlotPath(slot));
    }

    public static bool AnySlotExists()
    {
        for (int slot = 1; slot <= MaxSlots; slot++)
        {
            if (SlotExists(slot))
            {
                return true;
            }
        }

        return false;
    }

    // Primer slot sin archivo (save1, luego save2, luego save3). 0 = los tres estan ocupados.
    public static int GetFirstFreeSlot()
    {
        for (int slot = 1; slot <= MaxSlots; slot++)
        {
            if (!SlotExists(slot))
            {
                return slot;
            }
        }

        return 0;
    }

    // Ultima partida jugada. Si esa ya no existe, la que se guardo mas recientemente. 0 = no hay partidas.
    public static int GetLastPlayedSlot()
    {
        int lastSlot = PlayerPrefs.GetInt(LastSlotPrefsKey, 0);
        if (SlotExists(lastSlot))
        {
            return lastSlot;
        }

        int newestSlot = 0;
        System.DateTime newestTime = System.DateTime.MinValue;
        for (int slot = 1; slot <= MaxSlots; slot++)
        {
            if (!SlotExists(slot))
            {
                continue;
            }

            System.DateTime writeTime = File.GetLastWriteTimeUtc(GetSlotPath(slot));
            if (newestSlot == 0 || writeTime > newestTime)
            {
                newestSlot = slot;
                newestTime = writeTime;
            }
        }

        return newestSlot;
    }

    // Lee una partida sin cargarla (para mostrarla en el menu). null = no existe o esta corrupta.
    public static SaveData PeekSlot(int slot)
    {
        if (!SlotExists(slot))
        {
            return null;
        }

        try
        {
            return JsonUtility.FromJson<SaveData>(File.ReadAllText(GetSlotPath(slot)));
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[SaveManager] No se pudo leer la partida " + slot + ": " + e.Message);
            return null;
        }
    }

    // Deja 'slot' como la partida activa. Si el SaveManager ya existe (se vuelve al menu en la misma
    // sesion) cambia de archivo ahi mismo; si no, lo tomara al crearse en la primera escena de juego.
    public static void SelectSlot(int slot)
    {
        SelectedSlot = slot;
        SuppressPreferredSceneLoadOnNextBoot = false;
        SuppressSceneStateRestoreOnNextSceneLoad = false;

        if (Instance != null)
        {
            Instance.SwitchToSlot(slot);
        }
    }

    public static void DeleteSlot(int slot)
    {
        if (slot < 1 || slot > MaxSlots)
        {
            return;
        }

        if (Instance != null && Instance.CurrentSlot == slot)
        {
            Instance.DetachSlot();
        }

        if (SelectedSlot == slot)
        {
            SelectedSlot = 0;
        }

        if (PlayerPrefs.GetInt(LastSlotPrefsKey, 0) == slot)
        {
            PlayerPrefs.DeleteKey(LastSlotPrefsKey);
            PlayerPrefs.Save();
        }

        string path = GetSlotPath(slot);
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        // En el editor la carpeta esta dentro de Assets, asi que Unity le crea un .meta al archivo.
        if (File.Exists(path + ".meta"))
        {
            File.Delete(path + ".meta");
        }

        Debug.Log("[SaveManager] Partida " + slot + " borrada");
    }

    private void SwitchToSlot(int slot)
    {
        pendingEventIds.Clear();
        SetCurrentSlot(slot);

        if (!ReadSaveFile())
        {
            saveData = new SaveData();
            SaveGame();
        }
    }

    // La partida activa fue borrada: se suelta el archivo para no volver a escribirlo.
    private void DetachSlot()
    {
        pendingEventIds.Clear();
        saveData = new SaveData();
        savePath = null;
        CurrentSlot = 0;
    }

    // Antes habia un unico 'save.json': pasa a ser la partida 1 si todavia no hay partidas nuevas.
    private static void MigrateLegacySave()
    {
        string legacyPath = GetSaveDirectory() + "/" + LegacySaveFileName;
        if (!File.Exists(legacyPath) || AnySlotExists())
        {
            return;
        }

        File.Move(legacyPath, GetSlotPath(1));
        if (File.Exists(legacyPath + ".meta"))
        {
            File.Delete(legacyPath + ".meta");
        }
    }

    private bool ReadSaveFile()
    {
        if (string.IsNullOrEmpty(savePath) || !File.Exists(savePath))
        {
            return false;
        }

        string json = File.ReadAllText(savePath);
        saveData = JsonUtility.FromJson<SaveData>(json);
        bool changed = EnsureSaveDataDefaults();
        if (changed)
        {
            SaveGame();
        }

        return true;
    }

    public void LoadGame()
    {
        if (ReadSaveFile())
        {
            bool suppressPreferredSceneLoad = SuppressPreferredSceneLoadOnNextBoot;
            SuppressPreferredSceneLoadOnNextBoot = false;

            if (!suppressPreferredSceneLoad && IsCombatTestExecutionSceneLoaded())
            {
                suppressPreferredSceneLoad = true;
            }

            if (!suppressPreferredSceneLoad && TryLoadPreferredSceneOnStartup())
            {
                return;
            }
            Debug.Log("[SaveManager] Juego cargado desde: " + savePath);
        }
        else
        {
            saveData = new SaveData();
            Debug.Log("[SaveManager] Nuevo juego iniciado");
            SaveGame();
        }
    }

    public void SaveGame()
    {
        if (saveData == null)
        {
            saveData = new SaveData();
        }

        EnsureSaveDataDefaults();

        if (string.IsNullOrEmpty(savePath))
        {
            return;
        }

        string json = JsonUtility.ToJson(saveData, true);
        File.WriteAllText(savePath, json);
        Debug.Log("[SaveManager] Juego guardado en: " + savePath);
    }

    public void ClearAllData()
    {
        saveData = new SaveData();
        pendingEventIds.Clear();
        if (!string.IsNullOrEmpty(savePath) && File.Exists(savePath))
        {
            File.Delete(savePath);
        }
        Debug.Log("[SaveManager] Datos borrados");
    }

    // ========== SCENE / SESSION DATA ==========
    public void SetSavedSceneIndex(int sceneIndex)
    {
        if (saveData == null) saveData = new SaveData();
        saveData.savedSceneIndex = sceneIndex;
        SaveGame();
    }

    public bool TryConsumeSavedSceneIndex(out int sceneIndex)
    {
        sceneIndex = -1;
        if (saveData == null) return false;
        if (saveData.savedSceneIndex < 0) return false;
        sceneIndex = saveData.savedSceneIndex;
        saveData.savedSceneIndex = -1;
        SaveGame();
        return true;
    }

    public void SetReturnFromCombatFlag(bool value)
    {
        if (saveData == null) saveData = new SaveData();
        saveData.returnFromCombat = value;
        SaveGame();
    }

    public bool ConsumeReturnFromCombatFlag()
    {
        if (saveData == null) return false;
        bool val = saveData.returnFromCombat;
        saveData.returnFromCombat = false;
        SaveGame();
        return val;
    }

    public void SaveActiveStage(string stageName)
    {
        if (saveData == null) saveData = new SaveData();
        saveData.savedActiveStage = stageName ?? string.Empty;
        SaveGame();
    }

    public bool SetPreferredScene(string sceneName)
    {
        if (saveData == null)
        {
            saveData = new SaveData();
        }

        string normalizedSceneName = string.IsNullOrWhiteSpace(sceneName) ? string.Empty : sceneName;
        if (saveData.preferredSceneName == normalizedSceneName)
        {
            return false;
        }

        saveData.preferredSceneName = normalizedSceneName;
        SaveGame();
        return true;
    }

    public void ClearPreferredScene()
    {
        SetPreferredScene(string.Empty);
    }

    public void RegisterPendingEvent(string eventId)
    {
        if (string.IsNullOrWhiteSpace(eventId))
        {
            return;
        }

        pendingEventIds.Add(eventId);
    }

    public void UnregisterPendingEvent(string eventId)
    {
        if (string.IsNullOrWhiteSpace(eventId))
        {
            return;
        }

        pendingEventIds.Remove(eventId);
    }

    public bool HasPendingEvents()
    {
        return pendingEventIds.Count > 0;
    }

    public bool IsEventPending(string eventId)
    {
        if (string.IsNullOrWhiteSpace(eventId))
        {
            return false;
        }

        return pendingEventIds.Contains(eventId);
    }

    public void CommitCurrentState()
    {
        AutoSaveCurrentState();
    }

    public bool GetSavedActiveStage(out string stageName)
    {
        stageName = string.Empty;
        if (saveData == null) return false;
        if (string.IsNullOrEmpty(saveData.savedActiveStage)) return false;
        stageName = saveData.savedActiveStage;
        return true;
    }

    // ========== PLAYER DATA ==========
    public void SavePlayerPosition(Vector3 position)
    {
        SavePlayerPosition(SceneManager.GetActiveScene().name, position);
    }

    public void SavePlayerPosition(string sceneName, Vector3 position)
    {
        if (saveData == null)
        {
            saveData = new SaveData();
        }

        SaveData.ScenePlayerData scenePlayerData = GetOrCreateScenePlayerData(sceneName);
        scenePlayerData.playerData.posX = position.x;
        scenePlayerData.playerData.posY = position.y;
        scenePlayerData.playerData.posZ = position.z;
        scenePlayerData.hasSavedPosition = true;
        SaveGame();
    }

    public void LoadPlayerPosition()
    {
        LoadPlayerPosition(SceneManager.GetActiveScene().name);
    }

    public void LoadPlayerPosition(string sceneName)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            return;
        }

        Vector3? savedPosition = GetPlayerPosition(sceneName);
        if (!savedPosition.HasValue)
        {
            return;
        }

        player.transform.position = savedPosition.Value;
    }

    public Vector3? GetPlayerPosition()
    {
        return GetPlayerPosition(SceneManager.GetActiveScene().name);
    }

    public Vector3? GetPlayerPosition(string sceneName)
    {
        if (saveData == null || string.IsNullOrWhiteSpace(sceneName))
        {
            return null;
        }

        SaveData.ScenePlayerData scenePlayerData = FindScenePlayerData(sceneName);
        if (scenePlayerData == null || !scenePlayerData.hasSavedPosition || scenePlayerData.playerData == null)
        {
            return null;
        }

        return new Vector3(scenePlayerData.playerData.posX, scenePlayerData.playerData.posY, scenePlayerData.playerData.posZ);
    }

    // ========== CAMERA DATA ==========
    public void SaveCameraBounds(float minX, float maxX, float minY, float maxY)
    {
        SaveCameraBounds(SceneManager.GetActiveScene().name, minX, maxX, minY, maxY);
    }

    public void SaveCameraBounds(string sceneName, float minX, float maxX, float minY, float maxY)
    {
        if (saveData == null)
        {
            saveData = new SaveData();
        }

        SaveData.SceneCameraData sceneCameraData = GetOrCreateSceneCameraData(sceneName);
        sceneCameraData.cameraData.minX = minX;
        sceneCameraData.cameraData.maxX = maxX;
        sceneCameraData.cameraData.minY = minY;
        sceneCameraData.cameraData.maxY = maxY;
        sceneCameraData.hasSavedCameraBounds = true;

        saveData.cameraData.minX = minX;
        saveData.cameraData.maxX = maxX;
        saveData.cameraData.minY = minY;
        saveData.cameraData.maxY = maxY;
        saveData.hasSavedCameraBounds = true;
        SaveGame();
    }

    public void LoadCameraBounds()
    {
        LoadCameraBounds(SceneManager.GetActiveScene().name);
    }

    public void LoadCameraBounds(string sceneName)
    {
        FollowCamera fc = FindObjectOfType<FollowCamera>();
        if (fc == null || saveData == null)
        {
            return;
        }

        if (!TryGetCameraBounds(sceneName, out float minX, out float maxX, out float minY, out float maxY))
        {
            return;
        }

        fc.SetCameraBounds(minX, maxX, minY, maxY);
    }

    public bool GetCameraBounds(out float minX, out float maxX, out float minY, out float maxY)
    {
        if (saveData == null || saveData.cameraData == null || !saveData.hasSavedCameraBounds)
        {
            minX = maxX = minY = maxY = 0;
            return false;
        }

        minX = saveData.cameraData.minX;
        maxX = saveData.cameraData.maxX;
        minY = saveData.cameraData.minY;
        maxY = saveData.cameraData.maxY;
        return true;
    }

    public bool GetCameraBounds(string sceneName, out float minX, out float maxX, out float minY, out float maxY)
    {
        if (saveData == null || string.IsNullOrWhiteSpace(sceneName))
        {
            minX = maxX = minY = maxY = 0;
            return false;
        }

        SaveData.SceneCameraData sceneCameraData = FindSceneCameraData(sceneName);
        if (sceneCameraData == null || !sceneCameraData.hasSavedCameraBounds || sceneCameraData.cameraData == null)
        {
            minX = maxX = minY = maxY = 0;
            return false;
        }

        minX = sceneCameraData.cameraData.minX;
        maxX = sceneCameraData.cameraData.maxX;
        minY = sceneCameraData.cameraData.minY;
        maxY = sceneCameraData.cameraData.maxY;
        return true;
    }

    private bool TryGetCameraBounds(string sceneName, out float minX, out float maxX, out float minY, out float maxY)
    {
        if (saveData == null || string.IsNullOrWhiteSpace(sceneName))
        {
            minX = maxX = minY = maxY = 0;
            return false;
        }

        return GetCameraBounds(sceneName, out minX, out maxX, out minY, out maxY);
    }

    // ========== TRIGGERS DATA ==========
    // Note: Stage triggers no longer use SaveManager.
    // Only EventPhaseState uses MarkEventAsTriggered for multi-phase events.
    // Completed events are persisted in SaveData.completedEventIds.
    
    public void MarkEventAsTriggered(string eventId)
    {
        if (string.IsNullOrWhiteSpace(eventId))
        {
            return;
        }

        if (saveData == null)
        {
            saveData = new SaveData();
        }

        if (saveData.completedEventIds == null)
        {
            saveData.completedEventIds = new List<string>();
        }

        if (!saveData.completedEventIds.Contains(eventId))
        {
            saveData.completedEventIds.Add(eventId);
            SaveGame();
        }
    }

    public bool MarkEnemyAsDefeated(string enemyId)
    {
        if (string.IsNullOrWhiteSpace(enemyId))
        {
            return false;
        }

        EnsureSaveDataDefaults();

        if (saveData.defeatedEnemyIds.Contains(enemyId))
        {
            return false;
        }

        saveData.defeatedEnemyIds.Add(enemyId);
        SaveGame();
        return true;
    }

    public bool WasEnemyDefeated(string enemyId)
    {
        if (saveData == null || string.IsNullOrWhiteSpace(enemyId))
        {
            return false;
        }

        EnsureSaveDataDefaults();
        return saveData.defeatedEnemyIds.Contains(enemyId);
    }

    public IReadOnlyList<string> GetUnlockedBlockIds()
    {
        EnsureSaveDataDefaults();
        return saveData.unlockedBlockIds;
    }

    public bool UnlockCodeBlock(string blockId)
    {
        if (string.IsNullOrWhiteSpace(blockId))
        {
            return false;
        }

        EnsureSaveDataDefaults();

        if (saveData.unlockedBlockIds.Contains(blockId))
        {
            return false;
        }

        saveData.unlockedBlockIds.Add(blockId);
        SaveGame();
        return true;
    }

    public bool WasEventTriggered(string eventId)
    {
        if (saveData == null || string.IsNullOrWhiteSpace(eventId))
        {
            return false;
        }

        if (saveData.completedEventIds == null)
        {
            return false;
        }

        return saveData.completedEventIds.Contains(eventId);
    }

    public string GetSavePath()
    {
        return savePath;
    }

    // Guarda automáticamente estado relevante al cerrar o pausar la aplicación
    private void OnApplicationQuit()
    {
        AutoSaveCurrentState();
    }

    private void OnApplicationPause(bool pause)
    {
        if (pause)
        {
            AutoSaveCurrentState();
        }
    }

    private void AutoSaveCurrentState()
    {
        if (HasPendingEvents())
        {
            return;
        }

        // Guarda posición del jugador si existe
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            SavePlayerPosition(player.transform.position);
        }

        // Guarda camera bounds si existe FollowCamera
        FollowCamera fc = FindObjectOfType<FollowCamera>();
        if (fc == null && Camera.main != null)
        {
            fc = Camera.main.GetComponent<FollowCamera>();
        }

        if (fc != null && fc.TryGetCameraBounds(out float minX, out float maxX, out float minY, out float maxY))
        {
            SaveCameraBounds(minX, maxX, minY, maxY);
        }

        // Guarda active stage si existe GameManagerStage1
        GameManagerStage1 gmStage1 = FindObjectOfType<GameManagerStage1>();
        if (gmStage1 != null)
        {
            string activeStage = gmStage1.GetActiveStage();
            if (!string.IsNullOrEmpty(activeStage))
            {
                SaveActiveStage(activeStage);
            }
        }
    }

    private bool TryLoadPreferredSceneOnStartup()
    {
        if (saveData == null || string.IsNullOrWhiteSpace(saveData.preferredSceneName))
        {
            return false;
        }

        if (GameManager.Instance != null && GameManager.Instance.OnCombat)
        {
            return false;
        }

        string currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (currentSceneName == saveData.preferredSceneName)
        {
            return false;
        }

        if (!UnityEngine.Application.CanStreamedLevelBeLoaded(saveData.preferredSceneName))
        {
            Debug.LogWarning("[SaveManager] La escena preferida '" + saveData.preferredSceneName + "' no esta en Build Settings.");
            return false;
        }

        Debug.Log("[SaveManager] Cargando escena preferida: " + saveData.preferredSceneName);
        UnityEngine.SceneManagement.SceneManager.LoadScene(saveData.preferredSceneName);
        return true;
    }

    private bool IsCombatTestExecutionSceneLoaded()
    {
        EnemyCombatRuntime[] combatRuntimes = FindObjectsOfType<EnemyCombatRuntime>(true);
        for (int i = 0; i < combatRuntimes.Length; i++)
        {
            EnemyCombatRuntime runtime = combatRuntimes[i];
            if (runtime != null && runtime.IsTestExecutionMode)
            {
                return true;
            }
        }

        return false;
    }

    private bool EnsureSaveDataDefaults()
    {
        bool changed = false;

        if (saveData == null)
        {
            saveData = new SaveData();
            changed = true;
        }

        if (saveData.completedEventIds == null)
        {
            saveData.completedEventIds = new List<string>();
            changed = true;
        }

        if (saveData.shownAdviceDialogIds == null)
        {
            saveData.shownAdviceDialogIds = new List<string>();
            changed = true;
        }

        if (saveData.unlockedBlockIds == null)
        {
            saveData.unlockedBlockIds = new List<string>();
            changed = true;
        }

        if (saveData.defeatedEnemyIds == null)
        {
            saveData.defeatedEnemyIds = new List<string>();
            changed = true;
        }

        if (saveData.scenePlayerData == null)
        {
            saveData.scenePlayerData = new List<SaveData.ScenePlayerData>();
            changed = true;
        }

        if (saveData.sceneCameraData == null)
        {
            saveData.sceneCameraData = new List<SaveData.SceneCameraData>();
            changed = true;
        }

        if (!saveData.unlockedBlockIds.Contains(DefaultUnlockedBlockId))
        {
            saveData.unlockedBlockIds.Insert(0, DefaultUnlockedBlockId);
            changed = true;
        }

        return changed;
    }

    public bool WasAdviceDialogShown(string adviceId)
    {
        if (saveData == null || string.IsNullOrWhiteSpace(adviceId))
        {
            return false;
        }

        EnsureSaveDataDefaults();
        return saveData.shownAdviceDialogIds.Contains(adviceId);
    }

    public bool MarkAdviceDialogAsShown(string adviceId)
    {
        if (string.IsNullOrWhiteSpace(adviceId))
        {
            return false;
        }

        EnsureSaveDataDefaults();

        if (saveData.shownAdviceDialogIds.Contains(adviceId))
        {
            return false;
        }

        saveData.shownAdviceDialogIds.Add(adviceId);
        SaveGame();
        return true;
    }

    public bool ClearAdviceDialogShown(string adviceId)
    {
        if (saveData == null || string.IsNullOrWhiteSpace(adviceId))
        {
            return false;
        }

        EnsureSaveDataDefaults();

        if (!saveData.shownAdviceDialogIds.Remove(adviceId))
        {
            return false;
        }

        SaveGame();
        return true;
    }

    private SaveData.ScenePlayerData FindScenePlayerData(string sceneName)
    {
        if (saveData == null || saveData.scenePlayerData == null || string.IsNullOrWhiteSpace(sceneName))
        {
            return null;
        }

        for (int i = 0; i < saveData.scenePlayerData.Count; i++)
        {
            SaveData.ScenePlayerData entry = saveData.scenePlayerData[i];
            if (entry != null && entry.sceneName == sceneName)
            {
                return entry;
            }
        }

        return null;
    }

    private SaveData.ScenePlayerData GetOrCreateScenePlayerData(string sceneName)
    {
        if (saveData == null)
        {
            saveData = new SaveData();
        }

        if (saveData.scenePlayerData == null)
        {
            saveData.scenePlayerData = new List<SaveData.ScenePlayerData>();
        }

        string normalizedSceneName = string.IsNullOrWhiteSpace(sceneName)
            ? SceneManager.GetActiveScene().name
            : sceneName;

        SaveData.ScenePlayerData existing = FindScenePlayerData(normalizedSceneName);
        if (existing != null)
        {
            return existing;
        }

        SaveData.ScenePlayerData created = new SaveData.ScenePlayerData
        {
            sceneName = normalizedSceneName,
            hasSavedPosition = false,
            playerData = new SaveData.PlayerData()
        };

        saveData.scenePlayerData.Add(created);
        return created;
    }

    private SaveData.SceneCameraData FindSceneCameraData(string sceneName)
    {
        if (saveData == null || saveData.sceneCameraData == null || string.IsNullOrWhiteSpace(sceneName))
        {
            return null;
        }

        for (int i = 0; i < saveData.sceneCameraData.Count; i++)
        {
            SaveData.SceneCameraData entry = saveData.sceneCameraData[i];
            if (entry != null && entry.sceneName == sceneName)
            {
                return entry;
            }
        }

        return null;
    }

    private SaveData.SceneCameraData GetOrCreateSceneCameraData(string sceneName)
    {
        if (saveData == null)
        {
            saveData = new SaveData();
        }

        if (saveData.sceneCameraData == null)
        {
            saveData.sceneCameraData = new List<SaveData.SceneCameraData>();
        }

        string normalizedSceneName = string.IsNullOrWhiteSpace(sceneName)
            ? SceneManager.GetActiveScene().name
            : sceneName;

        SaveData.SceneCameraData existing = FindSceneCameraData(normalizedSceneName);
        if (existing != null)
        {
            return existing;
        }

        SaveData.SceneCameraData created = new SaveData.SceneCameraData
        {
            sceneName = normalizedSceneName,
            hasSavedCameraBounds = false,
            cameraData = new SaveData.CameraData()
        };

        saveData.sceneCameraData.Add(created);
        return created;
    }
}
