using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Datos de la tablet que el Core NO tiene: tiempo restante, energia y mensajes de Control.
/// Solo LEE MissionManager (propiedades y eventos publicos); no lo modifica.
/// </summary>
public class StationTelemetry : MonoBehaviour
{
    [Serializable]
    public class ControlMessage
    {
        public string objectiveId;          // ej. energy_01 (se envia cuando ese objetivo pasa a ser el actual)
        [TextArea(1, 3)] public string text;
    }

    [Header("Misiones (mismo orden que en MissionManager)")]
    [SerializeField] private List<MissionDefinition> missions = new List<MissionDefinition>();

    [Header("Tiempo")]
    [SerializeField] private float totalSeconds = 1800f;

    [Header("Energia")]
    [Range(0f, 1f)] [SerializeField] private float startEnergy = 0.15f;

    [Header("Mensajes de Control")]
    [SerializeField] private List<ControlMessage> messagesByObjective = new List<ControlMessage>();
    [SerializeField] private int historySize = 3;

    public float TimeRemaining { get; private set; }
    public float Energy01 { get; private set; }
    public IReadOnlyList<string> Messages => messages;

    public event Action OnMessagesChanged;

    private readonly List<string> messages = new List<string>();
    private bool running;
    private MissionManager manager;

    private void Awake()
    {
        TimeRemaining = totalSeconds;
        Energy01 = startEnergy;
    }

    private void OnEnable() { TryBind(); }
    private void Start() { TryBind(); }

    private void TryBind()
    {
        if (manager != null) return;
        manager = MissionManager.Instance;
        if (manager == null) return;

        manager.OnMissionStarted += HandleStarted;
        manager.OnObjectiveChanged += HandleObjective;
        manager.OnMissionCompleted += HandleCompleted;
        manager.OnMissionReset += HandleReset;

        if (manager.CurrentState == MissionState.InProgress) running = true;
        RefreshEnergy();
    }

    private void OnDisable()
    {
        if (manager == null) return;
        manager.OnMissionStarted -= HandleStarted;
        manager.OnObjectiveChanged -= HandleObjective;
        manager.OnMissionCompleted -= HandleCompleted;
        manager.OnMissionReset -= HandleReset;
        manager = null;
    }

    private void Update()
    {
        if (!running) return;
        TimeRemaining = Mathf.Max(0f, TimeRemaining - Time.deltaTime);
    }

    private void HandleStarted()
    {
        TimeRemaining = totalSeconds;
        running = true;
    }

    private void HandleObjective(MissionState state, string text)
    {
        RefreshEnergy();

        MissionObjective obj = manager.CurrentMissionObjective;
        if (obj == null) return;

        foreach (ControlMessage m in messagesByObjective)
        {
            if (m.objectiveId == obj.ObjectiveId) { PostMessage(m.text); break; }
        }
    }

    private void HandleCompleted() { running = false; RefreshEnergy(); }

    private void HandleReset()
    {
        running = false;
        TimeRemaining = totalSeconds;
        messages.Clear();
        RefreshEnergy();
        OnMessagesChanged?.Invoke();
    }

    /// <summary>Energia derivada del progreso: startEnergy -> 1 segun objetivos completados.</summary>
    private void RefreshEnergy()
    {
        if (manager == null) return;

        int total = 0, completed = 0;
        for (int i = 0; i < missions.Count; i++)
        {
            if (missions[i] == null) continue;
            total += missions[i].ObjectiveCount;
            if (i < manager.CurrentMissionIndex) completed += missions[i].ObjectiveCount;
        }

        if (manager.CurrentState == MissionState.Completed) completed = total;
        else if (manager.CurrentState == MissionState.InProgress) completed += manager.CurrentObjectiveIndex;

        float p = total > 0 ? (float)completed / total : 0f;
        Energy01 = Mathf.Lerp(startEnergy, 1f, p);
    }

    /// <summary>Otros sistemas (narrativa, minijuegos) pueden enviar mensajes de Control.</summary>
    public void PostMessage(string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        messages.Add(text);
        while (messages.Count > historySize) messages.RemoveAt(0);
        OnMessagesChanged?.Invoke();
    }
}
