using System.Collections.Generic;
using UnityEngine;

namespace NovaCore
{
    /// <summary>
    /// Objeto de escena que corresponde a un objetivo de misión (botón, pieza, zona, mesa...).
    /// El HUD lo usa para señalar dónde está el objetivo actual.
    /// </summary>
    public interface IMissionTarget
    {
        MissionAction TargetAction { get; }
        Vector3 MarkerPosition { get; }
        bool IsTargetAvailable { get; }
    }

    /// <summary>
    /// Puerta de entrada del gameplay al Core de misiones (MissionManager).
    /// Comprueba ANTES de llamar si la acción es la del objetivo actual: así el jugador
    /// recibe un aviso claro en el HUD y la consola no se llena de warnings.
    /// </summary>
    public static class MissionGate
    {
        private static readonly List<IMissionTarget> _targets = new List<IMissionTarget>();

        public static MissionManager Manager => MissionManager.Instance;

        public static bool IsRunning =>
            Manager != null && Manager.CurrentState == MissionState.InProgress;

        public static bool IsCurrent(MissionAction action)
        {
            if (!IsRunning) return false;
            var obj = Manager.CurrentMissionObjective;
            return obj != null && obj.RequiredAction == action;
        }

        /// <summary>¿Esta acción ya se completó en la secuencia actual?</summary>
        public static bool IsDone(MissionAction action)
        {
            var m = Manager;
            if (m == null || m.CurrentState == MissionState.NotStarted) return false;
            if (m.CurrentState == MissionState.Completed) return true;

            var missions = m.Missions;
            for (int mi = 0; mi < missions.Count; mi++)
            {
                var def = missions[mi];
                if (def == null) continue;
                for (int oi = 0; oi < def.ObjectiveCount; oi++)
                {
                    var o = def.Objectives[oi];
                    if (o == null || o.RequiredAction != action) continue;
                    if (mi < m.CurrentMissionIndex) return true;
                    if (mi == m.CurrentMissionIndex && oi < m.CurrentObjectiveIndex) return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Intenta completar la acción. Devuelve true si el Core la aceptó.
        /// Si no corresponde, avisa en el HUD sin tocar el progreso.
        /// </summary>
        public static bool TryComplete(MissionAction action)
        {
            var m = Manager;
            if (m == null)
            {
                MissionHUD.Feedback("Sistema de misiones no disponible en esta escena.", MissionHUD.Tone.Warning);
                return false;
            }

            if (m.CurrentState == MissionState.NotStarted)
            {
                MissionHUD.Feedback("Primero pulsa INICIAR en el panel verde del Nodo.", MissionHUD.Tone.Warning);
                return false;
            }

            if (m.CurrentState == MissionState.Completed)
            {
                MissionHUD.Feedback("Todas las misiones ya están completadas.", MissionHUD.Tone.Info);
                return false;
            }

            if (!IsCurrent(action))
            {
                if (IsDone(action))
                    MissionHUD.Feedback("Eso ya está hecho. Ahora: " + m.CurrentObjective, MissionHUD.Tone.Info);
                else
                    MissionHUD.Feedback("Todavía no. Ahora: " + m.CurrentObjective, MissionHUD.Tone.Warning);
                return false;
            }

            bool ok = m.TryCompleteAction(action);
            if (ok) MissionHUD.Feedback("Objetivo completado.", MissionHUD.Tone.Success);
            return ok;
        }

        // ── Dónde ocurre cada acción (módulos en escenas separadas) ──────────
        /// <summary>Módulo de la ISS donde se realiza la acción (para guiar al jugador).</summary>
        public static string LocationOf(MissionAction a)
        {
            switch (a)
            {
                case MissionAction.EmergencyActivated:          return "Nodo 1 Unity";
                case MissionAction.EnergyModuleCollected:       return "Destiny (laboratorio)";
                case MissionAction.EnergyModuleLoadedIntoHatch:
                case MissionAction.RoboticConsoleActivated:
                case MissionAction.EnergyModuleInstalled:
                case MissionAction.RepairConfirmed:             return "Zvezda (módulo de servicio)";
                case MissionAction.SuitEquipped:
                case MissionAction.SuitSystemsChecked:
                case MissionAction.EVAPreparationCompleted:
                case MissionAction.AirlockDepressurized:
                case MissionAction.MMUActivated:
                case MissionAction.ReturnedToAirlock:
                case MissionAction.AirlockRepressurized:        return "Quest (esclusa)";
                case MissionAction.SolarPanelReached:
                case MissionAction.SolarPanelRepaired:          return "Exterior (EVA) por la esclusa Quest";
                case MissionAction.StationPowerVerified:        return "Cúpula";
                default:                                        return "la estación";
            }
        }

        /// <summary>Módulo del objetivo actual (o vacío si no hay misión en curso).</summary>
        public static string CurrentLocation()
        {
            if (!IsRunning) return "";
            var o = Manager.CurrentMissionObjective;
            return o == null ? "" : LocationOf(o.RequiredAction);
        }

        // ── Registro de objetivos en escena ──────────────────────────────────
        public static void Register(IMissionTarget t)   { if (t != null && !_targets.Contains(t)) _targets.Add(t); }
        public static void Unregister(IMissionTarget t) { _targets.Remove(t); }

        /// <summary>Objeto de escena del objetivo actual (o null).</summary>
        public static IMissionTarget FindCurrentTarget()
        {
            if (!IsRunning) return null;
            var obj = Manager.CurrentMissionObjective;
            if (obj == null) return null;
            var action = obj.RequiredAction;
            for (int i = _targets.Count - 1; i >= 0; i--)
            {
                var t = _targets[i];
                if (t == null || (t is Object uo && uo == null)) { _targets.RemoveAt(i); continue; }
                if (t.TargetAction == action && t.IsTargetAvailable) return t;
            }
            return null;
        }
    }
}
