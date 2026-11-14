using UnityEngine;

namespace NovaCore
{
    /// <summary>
    /// Volumen (caja) que detecta si la cabeza del jugador está dentro. No usa física:
    /// compara la posición de la cámara con la caja, así funciona igual con o sin
    /// CharacterController y en VR.
    ///
    /// Si completeAction está activo, al entrar completa la acción (p. ej. SolarPanelReached,
    /// ReturnedToAirlock) — solo cuando es el objetivo actual.
    /// También la usan otros objetos como requisito (MissionButton.requiredZone).
    /// </summary>
    public class MissionZone : MonoBehaviour, IMissionTarget
    {
        public string zoneName = "Zona";

        [Tooltip("Tamaño de la caja en metros (centrada en este objeto, ejes locales)")]
        public Vector3 size = new Vector3(2f, 2.4f, 2f);

        [Header("Misión")]
        public bool completeAction = false;
        public MissionAction action = MissionAction.SolarPanelReached;

        public bool PlayerInside { get; private set; }

        private Camera _cam;

        public MissionAction TargetAction => action;
        public Vector3 MarkerPosition => transform.position;
        public bool IsTargetAvailable => completeAction && isActiveAndEnabled;

        private void OnEnable()  { if (completeAction) MissionGate.Register(this); }
        private void OnDisable() { MissionGate.Unregister(this); PlayerInside = false; }

        private void Update()
        {
            if (_cam == null || !_cam.isActiveAndEnabled) _cam = Camera.main;
            if (_cam == null) return;

            Vector3 local = transform.InverseTransformPoint(_cam.transform.position);
            Vector3 h = size * 0.5f;
            bool inside = Mathf.Abs(local.x) <= h.x && Mathf.Abs(local.y) <= h.y && Mathf.Abs(local.z) <= h.z;

            // Completa al estar dentro mientras su acción es el objetivo actual
            // (también si el jugador ya estaba dentro cuando el objetivo cambió).
            if (inside && completeAction && MissionGate.IsCurrent(action))
                MissionGate.TryComplete(action);

            PlayerInside = inside;
        }

        private void OnDrawGizmos()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = completeAction ? new Color(0.2f, 1f, 0.5f, 0.25f) : new Color(0.3f, 0.7f, 1f, 0.2f);
            Gizmos.DrawCube(Vector3.zero, size);
            Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, 0.9f);
            Gizmos.DrawWireCube(Vector3.zero, size);
        }
    }
}
