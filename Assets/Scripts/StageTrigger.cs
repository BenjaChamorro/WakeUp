using UnityEngine;

public class StageTrigger : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameManagerStage1 gameManager;
    [SerializeField] private FollowCamera followCamera;

    [Header("Filter")]
    [SerializeField] private bool requireTag = true;
    [SerializeField] private string requiredTag = "Player";

    [Header("Behavior")]
    [SerializeField] private bool triggerOnlyOnce = true;
    [SerializeField] private bool goToStage12 = true;

    [Header("Teleport")]
    [SerializeField] private bool useCustomTpPoint;
    [SerializeField] private Transform tpPoint;

    [Header("Camera Bounds")]
    [SerializeField] private bool updateCameraBounds;
    [SerializeField] private float minX = -10f;
    [SerializeField] private float maxX = 10f;
    [SerializeField] private float minY = -5f;
    [SerializeField] private float maxY = 5f;

    private bool wasTriggered;

    private void Awake()
    {
        if (gameManager == null)
        {
            gameManager = FindAnyObjectByType<GameManagerStage1>();
        }

        if (followCamera == null && Camera.main != null)
        {
            followCamera = Camera.main.GetComponent<FollowCamera>();
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
            gameManager.ActivateStage12(useCustomTpPoint ? tpPoint : null);
        }
        else
        {
            gameManager.ActivateStage11(useCustomTpPoint ? tpPoint : null);
        }

        if (updateCameraBounds)
        {
            if (followCamera != null)
            {
                followCamera.SetCameraBounds(minX, maxX, minY, maxY);
            }
            else
            {
                Debug.LogWarning("No se encontro FollowCamera para actualizar limites de camara.");
            }
        }

        wasTriggered = true;
    }
}
