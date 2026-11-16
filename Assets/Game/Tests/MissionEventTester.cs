using UnityEngine;

public class MissionEventTester : MonoBehaviour
{
    [Header("Sistema de misiones")]
    [SerializeField]
    private MissionManager missionManager;

    private void Start()
    {
        if (missionManager == null)
            missionManager = MissionManager.Instance;

        if (missionManager == null)
        {
            Debug.LogError(
                "[EVENT TEST] No se encontro MissionManager."
            );
            return;
        }

        missionManager.OnMissionStarted += HandleMissionStarted;
        missionManager.OnMissionChanged += HandleMissionChanged;
        missionManager.OnObjectiveChanged += HandleObjectiveChanged;
        missionManager.OnMissionCompleted += HandleMissionCompleted;
        missionManager.OnMissionReset += HandleMissionReset;

        Debug.Log("[EVENT TEST] Eventos suscritos correctamente.");
    }

    private void HandleMissionStarted()
    {
        Debug.Log("[EVENT TEST] OnMissionStarted recibido.");
    }

    private void HandleMissionChanged(MissionDefinition mission)
    {
        Debug.Log(
            "[EVENT TEST] OnMissionChanged: " +
            mission.MissionName
        );
    }

    private void HandleObjectiveChanged(
        MissionState state,
        string objective
    )
    {
        Debug.Log(
            "[EVENT TEST] OnObjectiveChanged | Estado: " +
            state + " | Objetivo: " + objective
        );
    }

    private void HandleMissionCompleted()
    {
        Debug.Log(
            "[EVENT TEST] OnMissionCompleted recibido."
        );
    }

    private void HandleMissionReset()
    {
        Debug.Log(
            "[EVENT TEST] OnMissionReset recibido."
        );
    }

    private void OnDestroy()
    {
        if (missionManager == null)
            return;

        missionManager.OnMissionStarted -= HandleMissionStarted;
        missionManager.OnMissionChanged -= HandleMissionChanged;
        missionManager.OnObjectiveChanged -= HandleObjectiveChanged;
        missionManager.OnMissionCompleted -= HandleMissionCompleted;
        missionManager.OnMissionReset -= HandleMissionReset;
    }
}