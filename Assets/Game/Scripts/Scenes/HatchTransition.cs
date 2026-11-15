using UnityEngine;

namespace NovaCore
{
    /// <summary>
    /// Volumen al fondo de una escotilla: al meter la cabeza dentro, se pasa al módulo
    /// vecino (otra escena). Solo se "arma" cuando el jugador ha estado fuera al menos
    /// una vez, para no rebotar al llegar.
    /// </summary>
    public class HatchTransition : MonoBehaviour
    {
        public enum Condition { Always, AirlockInnerOpen, AirlockOuterOpen }

        [Header("Destino")]
        public string targetScene = "02_Nodo";
        public string targetSpawnId = "";
        public string destinationName = "Nodo 1 Unity";

        [Header("Volumen (metros, ejes locales)")]
        public Vector3 size = new Vector3(1.4f, 1.6f, 0.8f);

        [Header("Condición")]
        public Condition condition = Condition.Always;

        private Camera _cam;
        private bool _armed;
        private float _nextWarn;
        private float _enableTime;

        private void OnEnable() { _enableTime = Time.time; _armed = false; }

        private void Update()
        {
            if (SceneTransit.IsLoading) return;
            if (_cam == null || !_cam.isActiveAndEnabled) _cam = Camera.main;
            if (_cam == null) return;

            Vector3 p = transform.InverseTransformPoint(_cam.transform.position);
            Vector3 h = size * 0.5f;
            bool inside = Mathf.Abs(p.x) <= h.x && Mathf.Abs(p.y) <= h.y && Mathf.Abs(p.z) <= h.z;

            if (!inside)
            {
                if (Time.time - _enableTime > 0.5f) _armed = true;
                return;
            }
            if (!_armed) return;

            if (!IsOpen())
            {
                if (Time.unscaledTime >= _nextWarn)
                {
                    _nextWarn = Time.unscaledTime + 2f;
                    MissionHUD.Feedback(condition == Condition.AirlockOuterOpen
                        ? "Escotilla exterior cerrada: completa la preparación EVA y despresuriza."
                        : "Esclusa despresurizada: represuriza antes de volver al Nodo.", MissionHUD.Tone.Warning);
                }
                return;
            }

            _armed = false;
            MissionHUD.Feedback("Pasando a " + destinationName + "...", MissionHUD.Tone.Info, 1.5f);
            SceneTransit.Go(targetScene, targetSpawnId);
        }

        private bool IsOpen()
        {
            switch (condition)
            {
                case Condition.AirlockInnerOpen: return AirlockController.InnerHatchOpen;
                case Condition.AirlockOuterOpen: return AirlockController.OuterHatchOpen;
                default:                         return true;
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.25f);
            Gizmos.DrawCube(Vector3.zero, size);
        }
    }
}
