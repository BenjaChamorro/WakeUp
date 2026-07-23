using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SaveData
{
    [System.Serializable]
    public class PlayerData
    {
        public float posX;
        public float posY;
        public float posZ;
    }

    [System.Serializable]
    public class CameraData
    {
        public float minX;
        public float maxX;
        public float minY;
        public float maxY;
    }

    [System.Serializable]
    public class ScenePlayerData
    {
        public string sceneName = string.Empty;
        public bool hasSavedPosition = false;
        public PlayerData playerData = new PlayerData();
    }

    [System.Serializable]
    public class SceneCameraData
    {
        public string sceneName = string.Empty;
        public bool hasSavedCameraBounds = false;
        public CameraData cameraData = new CameraData();
    }


    public PlayerData playerData = new PlayerData();
    public CameraData cameraData = new CameraData();
    public bool hasSavedCameraBounds = false;
    public List<ScenePlayerData> scenePlayerData = new List<ScenePlayerData>();
    public List<SceneCameraData> sceneCameraData = new List<SceneCameraData>();
    public List<string> completedEventIds = new List<string>();
    public List<string> shownAdviceDialogIds = new List<string>();
    public List<string> unlockedBlockIds = new List<string>();
    public List<string> defeatedEnemyIds = new List<string>();

    // Scene / session persistence
    public int savedSceneIndex = -1;
    public bool returnFromCombat = false;
    public string savedActiveStage = "";
    public string preferredSceneName = "";

    public SaveData()
    {
        playerData = new PlayerData();
        cameraData = new CameraData();
        scenePlayerData = new List<ScenePlayerData>();
        sceneCameraData = new List<SceneCameraData>();
        completedEventIds = new List<string>();
        shownAdviceDialogIds = new List<string>();
        unlockedBlockIds = new List<string> { SaveManager.DefaultUnlockedBlockId };
        defeatedEnemyIds = new List<string>();
        hasSavedCameraBounds = false;
    }
}
