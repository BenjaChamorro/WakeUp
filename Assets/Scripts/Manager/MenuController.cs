using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Menu principal: Nueva Partida / Continuar / Cargar Partida sobre los 3 slots de SaveManager.
public class MenuController : MonoBehaviour
{
    [System.Serializable]
    public class SlotRow
    {
        public Button loadButton;
        public TMP_Text label;
        public Button deleteButton;
        public TMP_Text deleteLabel;
    }

    [Header("Escenas")]
    [Tooltip("Escena donde empieza una partida nueva (y las partidas que aun no cambiaron de escena).")]
    [SerializeField] private string firstSceneName = "Stage1";

    [Header("Menu principal")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button loadGameButton;
    [SerializeField] private TMP_Text messageText;

    [Header("Cargar partida")]
    [SerializeField] private GameObject loadPanel;
    [Tooltip("Una fila por slot, en orden: save1, save2, save3.")]
    [SerializeField] private SlotRow[] slotRows;
    [SerializeField] private Button backButton;

    [Header("Textos")]
    [SerializeField] private string noFreeSlotMessage = "No hay espacio: borra una partida en Cargar Partida";
    [SerializeField] private string emptySlotText = "Libre";
    [SerializeField] private string deleteText = "Borrar";
    [SerializeField] private string confirmDeleteText = "Confirmar";

    // Slot cuyo boton de borrar ya se presiono una vez; el segundo clic lo borra.
    private int slotPendingDelete;

    private void Awake()
    {
        newGameButton.onClick.AddListener(OnNewGame);
        continueButton.onClick.AddListener(OnContinue);
        loadGameButton.onClick.AddListener(ShowLoadPanel);
        backButton.onClick.AddListener(ShowMainPanel);

        for (int i = 0; i < slotRows.Length; i++)
        {
            int slot = i + 1;
            slotRows[i].loadButton.onClick.AddListener(() => StartGame(slot));
            slotRows[i].deleteButton.onClick.AddListener(() => OnDelete(slot));
        }
    }

    private void Start()
    {
        ShowMainPanel();
    }

    private void ShowMainPanel()
    {
        mainPanel.SetActive(true);
        loadPanel.SetActive(false);
        SetMessage(string.Empty);

        bool hasSaves = SaveManager.AnySlotExists();
        continueButton.interactable = hasSaves;
        loadGameButton.interactable = hasSaves;
    }

    private void ShowLoadPanel()
    {
        mainPanel.SetActive(false);
        loadPanel.SetActive(true);
        SetMessage(string.Empty);

        slotPendingDelete = 0;
        RefreshSlotRows();
    }

    private void OnNewGame()
    {
        int slot = SaveManager.GetFirstFreeSlot();
        if (slot == 0)
        {
            SetMessage(noFreeSlotMessage);
            return;
        }

        StartGame(slot);
    }

    private void OnContinue()
    {
        int slot = SaveManager.GetLastPlayedSlot();
        if (slot > 0)
        {
            StartGame(slot);
        }
    }

    private void OnDelete(int slot)
    {
        if (slotPendingDelete != slot)
        {
            slotPendingDelete = slot;
            RefreshSlotRows();
            return;
        }

        SaveManager.DeleteSlot(slot);
        slotPendingDelete = 0;
        RefreshSlotRows();
    }

    private void RefreshSlotRows()
    {
        for (int i = 0; i < slotRows.Length; i++)
        {
            int slot = i + 1;
            SlotRow row = slotRows[i];
            bool exists = SaveManager.SlotExists(slot);

            row.label.text = "Partida " + slot + " - " + (exists ? DescribeSlot(slot) : emptySlotText);
            row.loadButton.interactable = exists;
            row.deleteButton.gameObject.SetActive(exists);
            row.deleteLabel.text = slotPendingDelete == slot ? confirmDeleteText : deleteText;
        }
    }

    private string DescribeSlot(int slot)
    {
        SaveData data = SaveManager.PeekSlot(slot);
        if (data == null)
        {
            return firstSceneName;
        }

        if (!string.IsNullOrWhiteSpace(data.savedActiveStage))
        {
            return data.savedActiveStage;
        }

        return string.IsNullOrWhiteSpace(data.preferredSceneName) ? firstSceneName : data.preferredSceneName;
    }

    // Deja el slot como partida activa y carga la escena donde quedo (o la primera si es nueva).
    private void StartGame(int slot)
    {
        SaveData data = SaveManager.PeekSlot(slot);
        string sceneName = firstSceneName;
        if (data != null && !string.IsNullOrWhiteSpace(data.preferredSceneName)
            && Application.CanStreamedLevelBeLoaded(data.preferredSceneName))
        {
            sceneName = data.preferredSceneName;
        }

        SaveManager.SelectSlot(slot);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResetSession();
        }

        SceneManager.LoadScene(sceneName);
    }

    private void SetMessage(string message)
    {
        if (messageText != null)
        {
            messageText.text = message;
        }
    }
}
