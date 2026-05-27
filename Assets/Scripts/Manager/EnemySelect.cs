using UnityEngine;

public class EnemySelect : MonoBehaviour
{
    [SerializeField] private Object enemyAsset;
    [SerializeField] private string encounterId;
    [SerializeField] private bool succeed;
    public int combatSceneIndex = 1;
    public bool triggerOnce = true;
    bool triggered = false;

    public bool Succeed => succeed;

    void Awake()
    {
        // Reinicia estado en cada ejecución del juego.
        succeed = false;
        triggered = false;
        SyncSucceedFromGameManager();
    }

    void OnEnable()
    {
        SyncSucceedFromGameManager();
        
        // Si el encuentro no fue completado, resetea triggered para permitir entrada de nuevo
        if (!succeed)
        {
            triggered = false;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"[EnemySelect] OnTriggerEnter2D detectado con: {other.gameObject.name}, Tag: {other.tag}");

        SyncSucceedFromGameManager();
        Debug.Log($"[EnemySelect] succeed={succeed}, triggerOnce={triggerOnce}, triggered={triggered}");

        if (succeed)
        {
            Debug.LogWarning("[EnemySelect] Este encuentro ya fue completado (succeed=true).");
            return;
        }

        if (triggerOnce && triggered)
        {
            Debug.LogWarning("[EnemySelect] Ya fue disparado una vez y triggerOnce está activado.");
            return;
        }

        if (!other.CompareTag("Player"))
        {
            Debug.LogWarning($"[EnemySelect] El objeto que entró NO tiene tag 'Player'. Tag actual: {other.tag}");
            return;
        }

        if (GameManager.Instance == null)
        {
            Debug.LogError("[EnemySelect] GameManager no encontrado en la escena.");
            return;
        }

        Debug.Log($"[EnemySelect] Iniciando combate. enemyAsset={enemyAsset}, encounterId={encounterId}");
        GameManager.Instance.EnterCombat(combatSceneIndex, enemyAsset, BuildEncounterKey());
        triggered = true;
    }

    public void SetSucceed(bool value)
    {
        succeed = value;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetEncounterSucceeded(BuildEncounterKey(), value);
        }
    }

    private void SyncSucceedFromGameManager()
    {
        if (GameManager.Instance == null) return;

        succeed = GameManager.Instance.IsEncounterSucceeded(BuildEncounterKey());
    }

    private string BuildEncounterKey()
    {
        string resolvedId = string.IsNullOrWhiteSpace(encounterId)
            ? gameObject.scene.name + "_" + BuildTransformPath(transform)
            : encounterId.Trim();

        return resolvedId;
    }

    private static string BuildTransformPath(Transform current)
    {
        if (current == null) return "unknown_enemy";

        string path = current.name;
        Transform node = current.parent;
        while (node != null)
        {
            path = node.name + "/" + path;
            node = node.parent;
        }

        return path;
    }
}
