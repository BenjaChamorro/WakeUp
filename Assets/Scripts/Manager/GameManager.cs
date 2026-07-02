using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Combat")]
    [SerializeField] private int combatSceneIndex = 1;

    
    private readonly HashSet<string> succeededEncounters = new HashSet<string>();
    public bool OnCombat { get; private set; }
    public Object CurrentEnemyAsset { get; private set; }
    public string CurrentEncounterKey { get; private set; }

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
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.SetSavedSceneIndex(current);
        }
        SavePlayerPosition();
        SaveCameraBounds();
    }

    private void SavePlayerPosition()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;
        Vector3 p = player.transform.position;
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.SavePlayerPosition(p);
        }
    }

    public int CombatSceneIndex => combatSceneIndex;

    // Llamar para entrar en combate usando la escena de combate configurada en el inspector.
    public void EnterCombat(Object enemyAsset = null, string encounterKey = null)
    {
        if (SceneManager.GetActiveScene().buildIndex == combatSceneIndex)
        {
            Debug.LogWarning($"[GameManager] La escena de combate configurada ({combatSceneIndex}) coincide con la escena actual. Revisa el inspector.");
            return;
        }

        SaveCurrentStage();
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.SetReturnFromCombatFlag(true);
        }
        OnCombat = true;
        CurrentEnemyAsset = enemyAsset;
        CurrentEncounterKey = encounterKey;
        SceneManager.LoadScene(combatSceneIndex);
    }

    // Llamar para salir del combate y volver al stage guardado
    public void ExitCombatAndReturn()
    {
        OnCombat = false;
        CurrentEnemyAsset = null;
        CurrentEncounterKey = null;
        if (SaveManager.Instance != null && SaveManager.Instance.TryConsumeSavedSceneIndex(out int saved))
        {
            SceneManager.sceneLoaded += OnSceneLoadedRestore;
            SceneManager.LoadScene(saved);
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
        RestoreCameraBounds();
    }

    private void RestorePlayerPosition()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;
        if (SaveManager.Instance != null)
        {
            Vector3? savedPos = SaveManager.Instance.GetPlayerPosition();
            if (savedPos.HasValue)
            {
                player.transform.position = savedPos.Value;
            }
        }
    }

    private void SaveCameraBounds()
    {
        FollowCamera followCamera = FindAnyObjectByType<FollowCamera>();
        if (followCamera == null)
        {
            return;
        }

        if (!followCamera.TryGetCameraBounds(out float minX, out float maxX, out float minY, out float maxY))
        {
            return;
        }

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.SaveCameraBounds(minX, maxX, minY, maxY);
        }
    }

    private void RestoreCameraBounds()
    {
        FollowCamera followCamera = FindAnyObjectByType<FollowCamera>();
        if (followCamera == null && Camera.main != null)
        {
            followCamera = Camera.main.GetComponent<FollowCamera>();
        }

        if (followCamera == null || SaveManager.Instance == null)
        {
            return;
        }

        if (SaveManager.Instance.GetCameraBounds(out float minX, out float maxX, out float minY, out float maxY))
        {
            followCamera.SetCameraBounds(minX, maxX, minY, maxY);
        }
    }

    // Limpia cualquier progreso guardado (opcional)
    public void ClearSavedProgress()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.ClearAllData();
        }
    }

    public bool IsEncounterSucceeded(string encounterKey)
    {
        if (string.IsNullOrWhiteSpace(encounterKey)) return false;
        return succeededEncounters.Contains(encounterKey);
    }

    public void SetEncounterSucceeded(string encounterKey, bool succeeded)
    {
        if (string.IsNullOrWhiteSpace(encounterKey)) return;

        if (succeeded)
        {
            succeededEncounters.Add(encounterKey);
        }
        else
        {
            succeededEncounters.Remove(encounterKey);
        }
    }

    public void MarkCurrentEncounterSucceeded()
    {
        if (string.IsNullOrWhiteSpace(CurrentEncounterKey)) return;
        SetEncounterSucceeded(CurrentEncounterKey, true);
    }
}
