using UnityEngine;
using UnityEngine.UI;

namespace NovaCore
{
    /// <summary>
    /// Cartel flotante sobre un módulo de la ISS en la vista exterior (01_Orbit / EVA).
    /// Apuntando y manteniendo el gatillo/click se entra al módulo (su escena).
    /// El cartel siempre mira a la cámara.
    /// </summary>
    public class ModuleMarker : NovaInteractable
    {
        public string sceneName = "02_Nodo";
        public string spawnId = "";
        public string moduleName = "Nodo 1 Unity";
        public float holdSeconds = 0.8f;

        [Tooltip("Parte que gira hacia la cámara (cartel)")]
        public Transform billboard;
        public Text label;

        private float _hold;
        private Camera _cam;

        public override string GetPrompt() => "Entrar a " + moduleName + " (mantener)";

        public override void OnUsePressed(NovaInteractor interactor) { _hold = 0f; }

        public override void OnUseHeld(NovaInteractor interactor, float deltaTime)
        {
            _hold += deltaTime;
            if (label != null)
                label.text = moduleName + "\n" + NovaText.Bar(_hold / holdSeconds, 10);
            if (_hold >= holdSeconds)
            {
                _hold = 0f;
                SceneTransit.Go(sceneName, spawnId);
            }
        }

        public override void OnUseReleased(NovaInteractor interactor)
        {
            _hold = 0f;
            if (label != null) label.text = moduleName;
        }

        private void LateUpdate()
        {
            if (billboard == null) return;
            if (_cam == null || !_cam.isActiveAndEnabled) _cam = Camera.main;
            if (_cam == null) return;
            Vector3 d = billboard.position - _cam.transform.position;
            if (d.sqrMagnitude > 1e-4f) billboard.rotation = Quaternion.LookRotation(d, Vector3.up);
        }
    }
}
