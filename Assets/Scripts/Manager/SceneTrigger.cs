using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTrigger : MonoBehaviour
{
    [Header("Filter")]
    [SerializeField] private bool requireTag = true;
    [SerializeField] private string requiredTag = "Player";

    [Header("Scene")]
    [SerializeField] private string sceneName;

    private void OnTriggerEnter(Collider other)
    {
        TryLoadScene(other.gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryLoadScene(other.gameObject);
    }

    private void TryLoadScene(GameObject otherObject)
    {
        if (otherObject == null)
        {
            return;
        }

        if (requireTag && !otherObject.CompareTag(requiredTag))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("SceneTrigger no tiene una escena asignada.");
            return;
        }

        string currentSceneName = SceneManager.GetActiveScene().name;
        if (currentSceneName == sceneName)
        {
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"La escena '{sceneName}' no esta en Build Settings.");
            return;
        }

        SceneManager.LoadScene(sceneName);
    }
}