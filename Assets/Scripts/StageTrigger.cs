using UnityEngine;

public class StageTrigger : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameManagerStage1 gameManager;

    [Header("Filter")]
    [SerializeField] private bool requireTag = true;
    [SerializeField] private string requiredTag = "Player";

    [Header("Behavior")]
    [SerializeField] private bool triggerOnlyOnce = true;
    [SerializeField] private bool goToStage12 = true;

    private bool wasTriggered;

    private void Awake()
    {
        if (gameManager == null)
        {
            gameManager = FindAnyObjectByType<GameManagerStage1>();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        TryTrigger(other.gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryTrigger(other.gameObject);
    }

    private void TryTrigger(GameObject otherObject)
    {
        if (wasTriggered && triggerOnlyOnce)
        {
            return;
        }

        if (requireTag && !otherObject.CompareTag(requiredTag))
        {
            return;
        }

        if (gameManager == null)
        {
            Debug.LogWarning("No se encontro GameManagerStage1 para cambiar de stage.");
            return;
        }

        if (goToStage12)
        {
            gameManager.ActivateStage12();
        }
        else
        {
            gameManager.ActivateStage11();
        }

        wasTriggered = true;
    }
}
