using System.Collections.Generic;
using UnityEngine;

// Tabla de música del juego. MusicManager la carga desde Resources/MusicLibrary.
[CreateAssetMenu(fileName = "MusicLibrary", menuName = "WakeUp/Audio/Music Library")]
public class MusicLibrary : ScriptableObject
{
    [System.Serializable]
    public class SceneMusic
    {
        public string sceneName;
        public AudioClip clip;
    }

    [Header("Exploración")]
    [Tooltip("Música de cada escena de stage. Al volver de un combate continúa donde quedó.")]
    public List<SceneMusic> stageMusic = new List<SceneMusic>();

    [Header("Combate")]
    [Tooltip("Escenas que usan música de combate.")]
    public List<string> combatSceneNames = new List<string>();
    [Tooltip("Música de combate por defecto. Un enemigo la reemplaza con 'Combat Music' en su EnemyCombatData.")]
    public AudioClip defaultCombatMusic;

    [Header("Mezcla")]
    [Range(0f, 1f)] public float volume = 0.5f;
    [Min(0f)] public float fadeSeconds = 0.4f;

    public AudioClip GetStageMusic(string sceneName)
    {
        for (int i = 0; i < stageMusic.Count; i++)
        {
            if (stageMusic[i] != null && stageMusic[i].sceneName == sceneName)
            {
                return stageMusic[i].clip;
            }
        }

        return null;
    }

    public bool IsCombatScene(string sceneName)
    {
        return combatSceneNames.Contains(sceneName);
    }
}
