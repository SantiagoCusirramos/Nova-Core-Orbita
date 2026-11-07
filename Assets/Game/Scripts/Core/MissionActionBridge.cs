using UnityEngine;

public class MissionActionBridge : MonoBehaviour
{
    [Header("Sistema de misiones")]
    [SerializeField]
    private MissionManager missionManager;

    [Header("Accion que representa este objeto")]
    [SerializeField]
    private MissionAction action;

    // Se ejecuta cuando el jugador interactua
    public void ExecuteAction()
    {
        if (missionManager == null)
        {
            Debug.LogError(
                "No se ha asignado el MissionManager."
            );
            return;
        }

        bool success = missionManager.TryCompleteAction(action);

        if (success)
        {
            Debug.Log("Accion completada: " + action);
        }
        else
        {
            Debug.LogWarning(
                "La accion no corresponde al objetivo actual."
            );
        }
    }

    // Iniciar la mision
    public void BeginMission()
    {
        if (missionManager != null)
        {
            missionManager.StartMission();
        }
    }

    // Reiniciar la mision
    public void RestartMission()
    {
        if (missionManager != null)
        {
            missionManager.ResetMission();
        }
    }
}