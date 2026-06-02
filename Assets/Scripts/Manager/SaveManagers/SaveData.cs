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


    public PlayerData playerData = new PlayerData();
    public CameraData cameraData = new CameraData();
    public List<string> completedEventIds = new List<string>();
    public List<string> unlockedBlockIds = new List<string>();

    // Scene / session persistence
    public int savedSceneIndex = -1;
    public bool returnFromCombat = false;
    public string savedActiveStage = "";

    public SaveData()
    {
        playerData = new PlayerData();
        cameraData = new CameraData();
        completedEventIds = new List<string>();
        unlockedBlockIds = new List<string> { SaveManager.DefaultUnlockedBlockId };
    }
}
