using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Boton "Salir" de los stages: guarda la partida y vuelve al menu.
// Solo se ve en la exploracion normal; se oculta durante dialogos, consejos de Clippy y eventos.
public class ExitToMenuButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private string menuSceneName = "Menu";

    private DialogBoxController[] dialogControllers;
    private DialogAdvices[] dialogAdvices;
    private MainMovement playerMovement;

    private void Awake()
    {
        button.onClick.AddListener(ExitToMenu);
        button.gameObject.SetActive(false);
    }

    private void Start()
    {
        dialogControllers = FindObjectsOfType<DialogBoxController>(true);
        dialogAdvices = FindObjectsOfType<DialogAdvices>(true);
        playerMovement = FindObjectOfType<MainMovement>(true);
    }

    private void Update()
    {
        bool visible = IsExploring();
        if (button.gameObject.activeSelf != visible)
        {
            button.gameObject.SetActive(visible);
        }
    }

    private bool IsExploring()
    {
        if (GameManager.Instance != null && GameManager.Instance.OnCombat)
        {
            return false;
        }

        // Evento de varias fases en curso (EventPhaseState).
        if (SaveManager.Instance != null && SaveManager.Instance.HasPendingEvents())
        {
            return false;
        }

        // Los dialogos y eventos bloquean al jugador desactivando su movimiento.
        if (playerMovement != null && !playerMovement.isActiveAndEnabled)
        {
            return false;
        }

        for (int i = 0; i < dialogControllers.Length; i++)
        {
            if (dialogControllers[i] != null && dialogControllers[i].IsShowing)
            {
                return false;
            }
        }

        for (int i = 0; i < dialogAdvices.Length; i++)
        {
            if (dialogAdvices[i] != null && dialogAdvices[i].IsShowing)
            {
                return false;
            }
        }

        return true;
    }

    public void ExitToMenu()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.CommitCurrentState();
        }

        SceneManager.LoadScene(menuSceneName);
    }
}
