using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Reproduce la música de fondo según la escena cargada. Se crea solo al iniciar el juego y sobrevive
// a los cambios de escena, así que no hay que colocarlo en ninguna escena.
public class MusicManager : MonoBehaviour
{
    private const string LibraryResourcePath = "MusicLibrary";

    public static MusicManager Instance { get; private set; }

    private MusicLibrary library;
    private AudioSource source;
    private Coroutine switchRoutine;
    private AudioClip targetClip;
    private bool hasTarget;
    private bool sourceClipResumes;
    private readonly Dictionary<AudioClip, float> resumeTimes = new Dictionary<AudioClip, float>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null)
        {
            return;
        }

        GameObject go = new GameObject("MusicManager");
        DontDestroyOnLoad(go);
        Instance = go.AddComponent<MusicManager>();
    }

    void Awake()
    {
        library = Resources.Load<MusicLibrary>(LibraryResourcePath);
        if (library == null)
        {
            Debug.LogWarning("[MusicManager] No se encontro 'Resources/" + LibraryResourcePath + "'. No habra musica.");
        }

        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = true;
        source.volume = 0f;

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Start()
    {
        RefreshForScene(SceneManager.GetActiveScene());
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode == LoadSceneMode.Single)
        {
            RefreshForScene(scene);
        }
    }

    private void RefreshForScene(Scene scene)
    {
        if (library == null)
        {
            return;
        }

        if (library.IsCombatScene(scene.name))
        {
            Play(ResolveCombatMusic(), false);
            return;
        }

        Play(library.GetStageMusic(scene.name), true);
    }

    // Para momentos que no dependen de la escena (p. ej. los créditos). Dura hasta el próximo cambio de escena.
    public void PlayTrack(AudioClip clip)
    {
        if (library == null)
        {
            return;
        }

        Play(clip, false);
    }

    private AudioClip ResolveCombatMusic()
    {
        EnemyCombatData enemy = GameManager.Instance != null ? GameManager.Instance.CurrentEnemyAsset as EnemyCombatData : null;

        // Escena de combate abierta suelta (pruebas): se usa el enemigo asignado en el inspector.
        if (enemy == null)
        {
            EnemyCombatRuntime combatRuntime = FindObjectOfType<EnemyCombatRuntime>(true);
            enemy = combatRuntime != null ? combatRuntime.CurrentEnemy : null;
        }

        if (enemy != null && enemy.combatMusic != null)
        {
            return enemy.combatMusic;
        }

        return library.defaultCombatMusic;
    }

    // Si la pista pedida ya es la que suena (p. ej. dos combates seguidos) no se reinicia.
    private void Play(AudioClip clip, bool resume)
    {
        if (hasTarget && clip == targetClip)
        {
            return;
        }

        if (source.isPlaying && sourceClipResumes && source.clip != null)
        {
            resumeTimes[source.clip] = source.time;
        }

        hasTarget = true;
        targetClip = clip;

        if (switchRoutine != null)
        {
            StopCoroutine(switchRoutine);
        }

        switchRoutine = StartCoroutine(SwitchTo(clip, resume));
    }

    private IEnumerator SwitchTo(AudioClip clip, bool resume)
    {
        if (source.isPlaying && source.clip != clip)
        {
            yield return FadeVolume(0f);
            source.Stop();
        }

        if (clip == null)
        {
            switchRoutine = null;
            yield break;
        }

        if (source.clip != clip || !source.isPlaying)
        {
            source.clip = clip;
            sourceClipResumes = resume;

            float startTime = 0f;
            if (resume && resumeTimes.TryGetValue(clip, out float savedTime) && savedTime < clip.length)
            {
                startTime = savedTime;
            }

            source.volume = 0f;
            source.time = startTime;
            source.Play();
        }

        yield return FadeVolume(library.volume);
        switchRoutine = null;
    }

    private IEnumerator FadeVolume(float target)
    {
        float start = source.volume;
        float duration = library.fadeSeconds;

        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            source.volume = Mathf.Lerp(start, target, t / duration);
            yield return null;
        }

        source.volume = target;
    }
}
