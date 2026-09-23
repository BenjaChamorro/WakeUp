using UnityEngine;

public class Stage2Trigger : MonoBehaviour
{
    public enum TargetStage
    {
        Stage21,
        Stage22,
        Stage23,
        Stage24
    }

    [Header("References")]
    [SerializeField] private GameManagerStage2 gameManager;
    [SerializeField] private FollowCamera followCamera;

    [Header("Filter")]
    [SerializeField] private bool requireTag = true;
    [SerializeField] private string requiredTag = "Player";

    [Header("Behavior")]
    [SerializeField] private TargetStage targetStage = TargetStage.Stage22;

    [Header("Teleport")]
    [SerializeField] private bool useCustomTpPoint;
    [SerializeField] private Transform tpPoint;

    [Header("Camera Bounds")]
    [SerializeField] private bool updateCameraBounds;
    [SerializeField] private float minX = -10f;
    [SerializeField] private float maxX = 10f;
    [SerializeField] private float minY = -5f;
    [SerializeField] private float maxY = 5f;

    private void Awake()
    {
        if (gameManager == null)
        {
            gameManager = FindAnyObjectByType<GameManagerStage2>();
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
        if (requireTag && !otherObject.CompareTag(requiredTag))
        {
            return;
        }

        if (gameManager == null)
        {
            Debug.LogWarning("No se encontro GameManagerStage2 para cambiar de stage.");
            return;
        }

        // Obtener el nombre del stage destino
        string targetStageName = GetStageName(targetStage);

        // Verificar si ya estamos en el stage destino
        if (SaveManager.Instance.GetSavedActiveStage(out string currentStage))
        {
            if (currentStage == targetStageName)
            {
                return; // Ya estamos en el stage destino, no hacer nada
            }
        }

        // Activar el stage destino
        ActivateTargetStage();

        // Actualizar cámara si es necesario
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
    }

    private void ActivateTargetStage()
    {
        switch (targetStage)
        {
            case TargetStage.Stage21:
                gameManager.ActivateStage21(useCustomTpPoint ? tpPoint : null);
                break;
            case TargetStage.Stage22:
                gameManager.ActivateStage22(useCustomTpPoint ? tpPoint : null);
                break;
            case TargetStage.Stage23:
                gameManager.ActivateStage23(useCustomTpPoint ? tpPoint : null);
                break;
            case TargetStage.Stage24:
                gameManager.ActivateStage24(useCustomTpPoint ? tpPoint : null);
                break;
            default:
                Debug.LogWarning($"ActivateTargetStage no soporta {targetStage} aún. Añade el método en GameManagerStage2.");
                break;
        }
    }

    private string GetStageName(TargetStage stage)
    {
        return stage switch
        {
            TargetStage.Stage21 => "Stage2.1",
            TargetStage.Stage22 => "Stage2.2",
            TargetStage.Stage23 => "Stage2.3",
            TargetStage.Stage24 => "Stage2.4",
            _ => "Stage2.1"
        };
    }


}
