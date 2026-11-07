using UnityEngine;

public class MissionDebugTester : MonoBehaviour
{
    [Header("Sistema de misiones")]
    [SerializeField]
    private MissionManager missionManager;

    // Verificar que podemos realizar las pruebas
    private bool CanTest()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Primero activa el modo Play.");
            return false;
        }

        if (missionManager == null)
        {
            missionManager = MissionManager.Instance;
        }

        if (missionManager == null)
        {
            Debug.LogError("No existe un MissionManager asignado.");
            return false;
        }

        return true;
    }

    // PRUEBA 1: Iniciar la secuencia
    [ContextMenu("Pruebas/1. Iniciar misiones")]
    private void TestStart()
    {
        if (!CanTest()) return;

        missionManager.StartMission();
        ShowProgress();
    }

    // PRUEBA 2: Completar el objetivo actual
    [ContextMenu("Pruebas/2. Completar objetivo actual")]
    private void TestCompleteObjective()
    {
        if (!CanTest()) return;

        MissionObjective objective =
            missionManager.CurrentMissionObjective;

        if (objective == null)
        {
            Debug.LogWarning("No hay un objetivo activo.");
            return;
        }

        bool success = missionManager.TryCompleteAction(
            objective.RequiredAction
        );

        Debug.Log(success
            ? "Objetivo completado correctamente."
            : "No se pudo completar el objetivo.");

        ShowProgress();
    }

    // PRUEBA 3: Intentar una accion incorrecta
    [ContextMenu("Pruebas/3. Probar accion incorrecta")]
    private void TestWrongAction()
    {
        if (!CanTest()) return;

        MissionObjective objective =
            missionManager.CurrentMissionObjective;

        if (objective == null)
        {
            Debug.LogWarning("No hay un objetivo activo.");
            return;
        }

        MissionAction wrongAction =
            objective.RequiredAction == MissionAction.EmergencyActivated
            ? MissionAction.EnergyModuleCollected
            : MissionAction.EmergencyActivated;

        int previousMission = missionManager.CurrentMissionIndex;
        int previousObjective = missionManager.CurrentObjectiveIndex;

        bool accepted = missionManager.TryCompleteAction(wrongAction);

        bool unchanged =
            previousMission == missionManager.CurrentMissionIndex &&
            previousObjective == missionManager.CurrentObjectiveIndex;

        if (!accepted && unchanged)
        {
            Debug.Log("PRUEBA APROBADA: Accion incorrecta rechazada.");
        }
        else
        {
            Debug.LogError(
                "PRUEBA FALLIDA: La accion incorrecta altero el progreso."
            );
        }
    }

    // PRUEBA 4: Consultar el progreso
    [ContextMenu("Pruebas/4. Mostrar progreso")]
    private void TestShowProgress()
    {
        if (!CanTest()) return;

        ShowProgress();
    }

    private void ShowProgress()
    {
        Debug.Log("Estado general: " + missionManager.CurrentState);

        MissionDefinition mission = missionManager.CurrentMission;

        if (mission != null)
        {
            Debug.Log("Mision actual: " + mission.MissionName);
            Debug.Log(
                "Numero de mision: " +
                (missionManager.CurrentMissionIndex + 1)
            );

            Debug.Log(
                "Numero de objetivo: " +
                (missionManager.CurrentObjectiveIndex + 1)
            );
        }

        Debug.Log("Objetivo actual: " + missionManager.CurrentObjective);
    }

    // PRUEBA 5: Reiniciar toda la secuencia
    [ContextMenu("Pruebas/5. Reiniciar misiones")]
    private void TestReset()
    {
        if (!CanTest()) return;

        missionManager.ResetMission();

        Debug.Log("Secuencia reiniciada.");
        ShowProgress();
    }
}