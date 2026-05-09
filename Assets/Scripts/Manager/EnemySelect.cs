using UnityEngine;

public class EnemySelect : MonoBehaviour
{
    [SerializeField] private Object enemyAsset;
    public int combatSceneIndex = 1;
    public bool triggerOnce = true;
    bool triggered = false;
    
    void OnTriggerEnter2D(Collider2D other)
    {
        if (triggerOnce && triggered) return;
        if (!other.CompareTag("Player")) return;
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("GameManager not found in scene.");
            return;
        }

        GameManager.Instance.EnterCombat(combatSceneIndex, enemyAsset);
        triggered = true;
    }
}
