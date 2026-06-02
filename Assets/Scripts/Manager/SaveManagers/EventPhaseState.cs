using UnityEngine;

public class EventPhaseState : MonoBehaviour
{
    [SerializeField] private string eventId;

    public string EventId => eventId;
    public bool WasActivated { get; private set; }
    public bool IsCompleted { get; private set; }

    private void Awake()
    {
        if (!string.IsNullOrWhiteSpace(eventId) && SaveManager.Instance != null)
        {
            IsCompleted = SaveManager.Instance.WasEventTriggered(eventId);
            WasActivated = IsCompleted;
        }
    }

    public void BeginPhase()
    {
        if (string.IsNullOrWhiteSpace(eventId))
        {
            Debug.LogWarning($"{name}: EventPhaseState necesita eventId.", this);
            return;
        }

        WasActivated = true;
        IsCompleted = false;

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.RegisterPendingEvent(eventId);
        }
    }

    public void CompletePhase()
    {
        if (string.IsNullOrWhiteSpace(eventId))
        {
            Debug.LogWarning($"{name}: EventPhaseState necesita eventId.", this);
            return;
        }

        if (SaveManager.Instance != null && !SaveManager.Instance.IsEventPending(eventId))
        {
            // Protege contra triggers de cierre mal configurados que intenten completar fases no iniciadas.
            Debug.LogWarning($"{name}: Se intento completar '{eventId}' pero no esta pendiente. Ignorado.", this);
            return;
        }

        WasActivated = true;
        IsCompleted = true;

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.UnregisterPendingEvent(eventId);

            SaveManager.Instance.MarkEventAsTriggered(eventId);
            SaveManager.Instance.CommitCurrentState();
        }
    }

    public void ResetPhase()
    {
        WasActivated = false;
        IsCompleted = false;

        if (SaveManager.Instance != null && !string.IsNullOrWhiteSpace(eventId))
        {
            SaveManager.Instance.UnregisterPendingEvent(eventId);
        }
    }
}
