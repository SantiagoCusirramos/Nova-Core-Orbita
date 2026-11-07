using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class MissionManager : MonoBehaviour
{
    // INSTANCIA PRINCIPAL
    public static MissionManager Instance { get; private set; }

    // MISIONES CONFIGURADAS
    [Header("Secuencia de misiones")]
    [SerializeField]
    private List<MissionDefinition> missions =
        new List<MissionDefinition>();

    // ESTADO GENERAL
    [Header("Estado actual")]
    [SerializeField]
    private MissionState currentState = MissionState.NotStarted;

    // INDICES DE PROGRESO
    private int currentMissionIndex = -1;
    private int currentObjectiveIndex = -1;

    // PROPIEDADES PUBLICAS
    public MissionState CurrentState => currentState;

    public int CurrentMissionIndex => currentMissionIndex;

    public int CurrentObjectiveIndex => currentObjectiveIndex;

    // MISION ACTUAL
    public MissionDefinition CurrentMission
    {
        get
        {
            if (currentMissionIndex < 0 ||
                currentMissionIndex >= missions.Count)
                return null;

            return missions[currentMissionIndex];
        }
    }

    // OBJETIVO ACTUAL
    public MissionObjective CurrentMissionObjective
    {
        get
        {
            MissionDefinition mission = CurrentMission;

            if (mission == null ||
                currentObjectiveIndex < 0 ||
                currentObjectiveIndex >= mission.ObjectiveCount)
                return null;

            return mission.Objectives[currentObjectiveIndex];
        }
    }

    // TEXTO DEL OBJETIVO ACTUAL
    public string CurrentObjective
    {
        get
        {
            if (currentState == MissionState.NotStarted)
                return "Esperando inicio de mision";

            if (currentState == MissionState.Completed)
                return "Estacion estabilizada. Mision completada";

            MissionObjective objective = CurrentMissionObjective;

            return objective != null
                ? objective.Description
                : "Objetivo no disponible";
        }
    }

    // EVENTOS PARA OTROS SISTEMAS
    public event Action OnMissionStarted;
    public event Action OnMissionCompleted;
    public event Action OnMissionReset;

    public event Action<MissionState, string> OnObjectiveChanged;

    // Nuevo evento: cambia la mision o fase actual
    public event Action<MissionDefinition> OnMissionChanged;

    // INICIALIZACION
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError(
                "Ya existe un MissionManager en la escena.",
                this
            );

            enabled = false;
            Destroy(this);
            return;
        }

        Instance = this;

        currentState = MissionState.NotStarted;
        currentMissionIndex = -1;
        currentObjectiveIndex = -1;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // INICIAR SECUENCIA DE MISIONES
    public void StartMission()
    {
        if (currentState != MissionState.NotStarted)
        {
            Debug.LogWarning("La mision ya esta iniciada.");
            return;
        }

        if (!ValidateMissions())
            return;

        currentMissionIndex = 0;
        currentObjectiveIndex = 0;
        currentState = MissionState.InProgress;

        Debug.Log("Secuencia de misiones iniciada.");

        OnMissionStarted?.Invoke();
        OnMissionChanged?.Invoke(CurrentMission);

        NotifyObjectiveChanged();
    }

    // VALIDAR ACCION DEL JUGADOR
    public bool TryCompleteAction(MissionAction action)
    {
        if (currentState != MissionState.InProgress)
        {
            Debug.LogWarning("No hay una mision en progreso.");
            return false;
        }

        MissionObjective objective = CurrentMissionObjective;

        if (objective == null)
        {
            Debug.LogError("No existe un objetivo activo.");
            return false;
        }

        if (objective.RequiredAction != action)
        {
            Debug.LogWarning(
                "Accion incorrecta: " + action +
                " | Esperada: " + objective.RequiredAction
            );

            return false;
        }

        Debug.Log("Objetivo completado: " + objective.Description);

        AdvanceObjective();

        return true;
    }

    // AVANZAR AL SIGUIENTE OBJETIVO
    private void AdvanceObjective()
    {
        currentObjectiveIndex++;

        if (currentObjectiveIndex >= CurrentMission.ObjectiveCount)
        {
            AdvanceMission();
            return;
        }

        NotifyObjectiveChanged();
    }

    // AVANZAR A LA SIGUIENTE MISION
    private void AdvanceMission()
    {
        Debug.Log(
            "Mision finalizada: " + CurrentMission.MissionName
        );

        currentMissionIndex++;

        // Todas las misiones completadas
        if (currentMissionIndex >= missions.Count)
        {
            currentState = MissionState.Completed;

            Debug.Log("Todas las misiones completadas.");

            NotifyObjectiveChanged();
            OnMissionCompleted?.Invoke();

            return;
        }

        // Preparar la siguiente mision
        currentObjectiveIndex = 0;

        Debug.Log(
            "Nueva mision: " + CurrentMission.MissionName
        );

        OnMissionChanged?.Invoke(CurrentMission);
        NotifyObjectiveChanged();
    }

    // REINICIAR TODA LA SECUENCIA
    public void ResetMission()
    {
        currentState = MissionState.NotStarted;

        currentMissionIndex = -1;
        currentObjectiveIndex = -1;

        NotifyObjectiveChanged();

        OnMissionReset?.Invoke();

        Debug.Log("Secuencia de misiones reiniciada.");
    }

    // NOTIFICAR CAMBIO DE OBJETIVO
    private void NotifyObjectiveChanged()
    {
        Debug.Log("Objetivo actual: " + CurrentObjective);

        OnObjectiveChanged?.Invoke(
            currentState,
            CurrentObjective
        );
    }

    // VALIDAR CONFIGURACION
    private bool ValidateMissions()
    {
        if (missions == null || missions.Count == 0)
        {
            Debug.LogError(
                "No hay misiones configuradas en MissionManager."
            );

            return false;
        }

        for (int i = 0; i < missions.Count; i++)
        {
            MissionDefinition mission = missions[i];

            if (mission == null || mission.ObjectiveCount == 0)
            {
                Debug.LogError(
                    "Mision sin configurar o sin objetivos. Indice: " + i
                );

                return false;
            }

            for (int j = 0; j < mission.ObjectiveCount; j++)
            {
                if (mission.Objectives[j] == null)
                {
                    Debug.LogError(
                        "Objetivo nulo en mision " + i +
                        ", posicion " + j
                    );

                    return false;
                }
            }
        }

        return true;
    }
}