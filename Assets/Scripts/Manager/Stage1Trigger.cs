using UnityEngine;

public class Stage1Trigger : MonoBehaviour
{
    public enum TargetStage
    {
        Stage11,
        Stage12,
        Stage13,
        Stage14
    }

    [Header("References")]
    [SerializeField] private GameManagerStage1 gameManager;
    [SerializeField] private FollowCamera followCamera;

    [Header("Filter")]
    [SerializeField] private bool requireTag = true;
    [SerializeField] private string requiredTag = "Player";

    [Header("Behavior")]
    [SerializeField] private TargetStage targetStage = TargetStage.Stage12;

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
        if (requireTag && !otherObject.CompareTag(requiredTag))
        {
            return;
        }

        if (gameManager == null)
        {
            Debug.LogWarning("No se encontro GameManagerStage1 para cambiar de stage.");
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
            case TargetStage.Stage11:
                gameManager.ActivateStage11(useCustomTpPoint ? tpPoint : null);
                break;
            case TargetStage.Stage12:
                gameManager.ActivateStage12(useCustomTpPoint ? tpPoint : null);
                break;
            case TargetStage.Stage13:
                gameManager.ActivateStage13(useCustomTpPoint ? tpPoint : null);
                break;
            case TargetStage.Stage14:
                gameManager.ActivateStage14(useCustomTpPoint ? tpPoint : null);
                break;
            default:
                Debug.LogWarning($"ActivateTargetStage no soporta {targetStage} aún. Añade el método en GameManagerStage1.");
                break;
        }
    }

    private string GetStageName(TargetStage stage)
    {
        return stage switch
        {
            TargetStage.Stage11 => "Stage1.1",
            TargetStage.Stage12 => "Stage1.2",
            TargetStage.Stage13 => "Stage1.3",
            TargetStage.Stage14 => "Stage1.4",
            _ => "Stage1.1"
        };
    }


}
