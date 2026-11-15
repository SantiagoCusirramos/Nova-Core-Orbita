using UnityEngine;

namespace NovaCore
{
    /// <summary>
    /// Estado de las escotillas de la esclusa Quest calculado SOLO a partir del progreso de
    /// la misión (sin estado propio), así es coherente al entrar desde cualquier escena y
    /// al reiniciar:
    ///   Interior cerrada  ⇔ despresurizada (AirlockDepressurized) y aún no represurizada.
    ///   Exterior abierta  ⇔ despresurizada y aún sin volver (ReturnedToAirlock).
    /// </summary>
    public class AirlockController : MonoBehaviour
    {
        public HatchDoor innerHatch;
        public HatchDoor outerHatch;

        public static bool InnerHatchOpen =>
            !(MissionGate.IsDone(MissionAction.AirlockDepressurized) && !MissionGate.IsDone(MissionAction.AirlockRepressurized));

        public static bool OuterHatchOpen =>
            MissionGate.IsDone(MissionAction.AirlockDepressurized) && !MissionGate.IsDone(MissionAction.ReturnedToAirlock);

        private void Update()
        {
            if (innerHatch != null) innerHatch.SetMode(InnerHatchOpen ? HatchDoor.Mode.Proximity : HatchDoor.Mode.ForcedClosed);
            if (outerHatch != null) outerHatch.SetMode(OuterHatchOpen ? HatchDoor.Mode.ForcedOpen : HatchDoor.Mode.ForcedClosed);
        }
    }
}
