using UnityEngine;
using UnityEngine.SceneManagement;

namespace NovaCore
{
    /// <summary>
    /// Consola que cambia de escena al usarla (p. ej. 01_Orbit ⇄ 02_Interior).
    /// La escena destino debe estar en Build Settings (el menú del constructor la añade).
    /// </summary>
    public class SceneTeleporter : NovaInteractable
    {
        [Tooltip("Nombre de la escena destino (sin .unity)")]
        public string sceneName = "02_Nodo";

        [Tooltip("Punto de llegada en la escena destino (PlayerSpawnBay.spawnId). Vacío = por defecto.")]
        public string spawnId = "";

        [Tooltip("Mantener pulsado este tiempo para viajar (evita viajes accidentales)")]
        public float holdSeconds = 1.0f;

        private float _hold;
        private bool _loading;

        public override bool AllowPoke => true;   // se puede pulsar tocándolo con el dedo

        public override string GetPrompt() => prompt + " (mantener)";

        public override void OnUsePressed(NovaInteractor interactor) { _hold = 0f; }

        public override void OnUseHeld(NovaInteractor interactor, float deltaTime)
        {
            if (_loading) return;
            _hold += deltaTime;
            if (_hold < holdSeconds) return;

            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                MissionHUD.Feedback("La escena '" + sceneName + "' no está en Build Settings.", MissionHUD.Tone.Warning);
                Debug.LogWarning("[SceneTeleporter] Añade '" + sceneName + "' a File → Build Settings.", this);
                _hold = 0f;
                return;
            }

            _loading = true;
            SceneTransit.Go(sceneName, spawnId);
        }

        public override void OnUseReleased(NovaInteractor interactor) { _hold = 0f; }
    }
}
