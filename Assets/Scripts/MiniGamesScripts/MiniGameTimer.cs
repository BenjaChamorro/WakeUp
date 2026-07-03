using TMPro;
using UnityEngine;
using UnityEngine.Events;

// Contador en pantalla del tiempo que el jugador debe sobrevivir en el minijuego.
// El tiempo se define en el inspector (para probar la escena suelta) y MiniGameRuntime
// lo sobreescribe con MiniGameData.survivalTime cuando el minijuego viene de un combate.
public class MiniGameTimer : MonoBehaviour {
    [Tooltip("Tiempo (s) que debe sobrevivir el jugador. Se usa si no llega un valor desde MiniGameData.")]
    [SerializeField] private float survivalTime = 30f;

    [SerializeField] private TMP_Text timerText;

    [Tooltip("Si está activo, el contador arranca solo en Start().")]
    [SerializeField] private bool startOnAwake = true;

    public UnityEvent onTimeUp;

    private float timeRemaining;
    private bool isRunning;

    void Start() {
        timeRemaining = survivalTime;
        UpdateDisplay();

        if (startOnAwake) {
            StartTimer();
        }
    }

    void Update() {
        if (!isRunning) {
            return;
        }

        timeRemaining -= Time.deltaTime;

        if (timeRemaining <= 0f) {
            timeRemaining = 0f;
            isRunning = false;
            UpdateDisplay();
            onTimeUp.Invoke();
            return;
        }

        UpdateDisplay();
    }

    // Permite que MiniGameRuntime fije el tiempo real del minijuego según MiniGameData.
    public void Configure(float newSurvivalTime) {
        survivalTime = newSurvivalTime;
        timeRemaining = survivalTime;
        UpdateDisplay();
    }

    public void StartTimer() {
        isRunning = true;
    }

    public void StopTimer() {
        isRunning = false;
    }

    // Activa/desactiva por completo el contador (oculta el texto y detiene la cuenta).
    // Lo usa MiniGameRuntime para minijuegos donde la victoria no depende del tiempo (ej. recoger monedas).
    public void SetTimerActive(bool active) {
        if (!active) {
            isRunning = false;
        }
        gameObject.SetActive(active);
    }

    private void UpdateDisplay() {
        if (timerText == null) {
            return;
        }

        int seconds = Mathf.CeilToInt(timeRemaining);
        int minutes = seconds / 60;
        seconds %= 60;
        timerText.text = $"{minutes:00}:{seconds:00}";
    }
}
