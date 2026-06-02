using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }
    public const string DefaultUnlockedBlockId = "print";

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

    private void InitializeSavePath()
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
            savePath = Path.Combine(Application.persistentDataPath, "save.json");
            savePath = savePath.Replace("\\", "/");
            return;
        }

        savePath = Path.Combine(saveDir, "save.json");
        savePath = savePath.Replace("\\", "/");
    }

    public void LoadGame()
    {
        if (File.Exists(savePath))
        {
            string json = File.ReadAllText(savePath);
            saveData = JsonUtility.FromJson<SaveData>(json);
            bool changed = EnsureSaveDataDefaults();
            if (changed)
            {
                SaveGame();
            }
            LoadPlayerPosition();
            LoadCameraBounds();
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

        string json = JsonUtility.ToJson(saveData, true);
        File.WriteAllText(savePath, json);
        Debug.Log("[SaveManager] Juego guardado en: " + savePath);
    }

    public void ClearAllData()
    {
        saveData = new SaveData();
        pendingEventIds.Clear();
        if (File.Exists(savePath))
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
        saveData.playerData.posX = position.x;
        saveData.playerData.posY = position.y;
        saveData.playerData.posZ = position.z;
        SaveGame();
    }

    public void LoadPlayerPosition()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null || saveData == null || saveData.playerData == null)
        {
            return;
        }

        player.transform.position = new Vector3(saveData.playerData.posX, saveData.playerData.posY, saveData.playerData.posZ);
    }

    public Vector3? GetPlayerPosition()
    {
        if (saveData == null || saveData.playerData == null)
        {
            return null;
        }
        return new Vector3(saveData.playerData.posX, saveData.playerData.posY, saveData.playerData.posZ);
    }

    // ========== CAMERA DATA ==========
    public void SaveCameraBounds(float minX, float maxX, float minY, float maxY)
    {
        saveData.cameraData.minX = minX;
        saveData.cameraData.maxX = maxX;
        saveData.cameraData.minY = minY;
        saveData.cameraData.maxY = maxY;
        SaveGame();
    }

    public void LoadCameraBounds()
    {
        FollowCamera fc = FindObjectOfType<FollowCamera>();
        if (fc == null || saveData == null || saveData.cameraData == null)
        {
            return;
        }

        fc.SetCameraBounds(
            saveData.cameraData.minX,
            saveData.cameraData.maxX,
            saveData.cameraData.minY,
            saveData.cameraData.maxY
        );
    }

    public bool GetCameraBounds(out float minX, out float maxX, out float minY, out float maxY)
    {
        if (saveData == null || saveData.cameraData == null)
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

        if (saveData.unlockedBlockIds == null)
        {
            saveData.unlockedBlockIds = new List<string>();
            changed = true;
        }

        if (!saveData.unlockedBlockIds.Contains(DefaultUnlockedBlockId))
        {
            saveData.unlockedBlockIds.Insert(0, DefaultUnlockedBlockId);
            changed = true;
        }

        return changed;
    }
}
