using UnityEngine;

public class MissionActionBridge : MonoBehaviour
{
    [Header("Configuracion Core")]
    [SerializeField] private MissionManager missionManager;

    [Header("Accion que ejecuta este objeto")]
    [SerializeField] private MissionAction action;

    // Obtener MissionManager
    private MissionManager GetMissionManager()
    {
        if (missionManager != null)
        {
            return missionManager;
        }

        return MissionManager.Instance;
    }

    // Para eventos del Inspector de Unity
    public void ExecuteAction()
    {
        TryExecuteAction();
    }

    // Para los scripts de los minijuegos
    public bool TryExecuteAction()
    {
        MissionManager manager = GetMissionManager();

        if (manager == null)
        {
            Debug.LogWarning(
                "No se encontro MissionManager.",
                this
            );

            return false;
        }

        bool accepted = manager.TryCompleteAction(action);

        if (accepted)
        {
            Debug.Log(
                "Accion aceptada: " + action,
                this
            );
        }
        else
        {
            Debug.LogWarning(
                "Accion rechazada: " + action +
                ". No corresponde al objetivo actual.",
                this
            );
        }

        return accepted;
    }

    // Iniciar la secuencia de misiones
    public void BeginMission()
    {
        MissionManager manager = GetMissionManager();

        if (manager == null)
        {
            Debug.LogWarning(
                "No se puede iniciar: MissionManager no existe.",
                this
            );

            return;
        }

        manager.StartMission();
    }

    // Reiniciar la secuencia
    public void RestartMission()
    {
        MissionManager manager = GetMissionManager();

        if (manager == null)
        {
            Debug.LogWarning(
                "No se puede reiniciar: MissionManager no existe.",
                this
            );

            return;
        }

        manager.ResetMission();
    }
}