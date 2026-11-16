using System.Collections;
using UnityEngine;

namespace NovaCore
{
    /// <summary>
    /// Inicio narrativo del documento oficial (sección 2.2): despertar → alarma → aviso del
    /// Centro de Control → controles básicos (sala de entrenamiento) → ir al centro de mando.
    /// Al llegar al Nodo desde el entrenamiento, el Centro de Control habla y la misión
    /// empieza sola (StartMission una sola vez). El botón INICIAR sigue sirviendo si se
    /// entra al Nodo directamente (pruebas en el editor).
    /// Independiente del Core: solo llama a MissionManager.StartMission().
    /// </summary>
    public class GameIntroManager : MonoBehaviour
    {
        /// <summary>Lo pone el entrenamiento al abrir su escotilla final.</summary>
        public static bool PendingAutoStart;

        public float delaySeconds = 2.5f;
        [TextArea] public string controlMessage =
            "CENTRO DE CONTROL: Eva, la anomalía eléctrica empeora. Energía de la estación en nivel crítico. Iniciamos el protocolo: sigue las indicaciones.";

        private IEnumerator Start()
        {
            if (!PendingAutoStart) yield break;
            PendingAutoStart = false;
            yield return new WaitForSeconds(delaySeconds);

            var m = MissionManager.Instance;
            if (m == null || m.CurrentState != MissionState.NotStarted) yield break;

            MissionHUD.Feedback(controlMessage, MissionHUD.Tone.Warning, 7f);
            var tel = m.GetComponent<StationTelemetry>();
            if (tel != null) tel.PostMessage(controlMessage.Replace("CENTRO DE CONTROL: ", ""));
            m.StartMission();
        }
    }
}
