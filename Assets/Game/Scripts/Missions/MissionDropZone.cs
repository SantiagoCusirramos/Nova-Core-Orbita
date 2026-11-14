using System.Collections.Generic;
using UnityEngine;

namespace NovaCore
{
    /// <summary>
    /// Ranura donde se encaja una MissionPickup (p. ej. la compuerta de mantenimiento).
    /// Al soltar la pieza a menos de acceptRadius, se encaja y completa la acción.
    /// Si la acción no es la del objetivo actual, la pieza NO se encaja (se queda flotando)
    /// y el HUD explica qué toca hacer.
    /// </summary>
    public class MissionDropZone : MonoBehaviour, IMissionTarget
    {
        private static readonly List<MissionDropZone> _all = new List<MissionDropZone>();

        [Header("Pieza aceptada")]
        public string acceptsItemId = "energy_module";
        public float acceptRadius = 0.45f;

        [Tooltip("Punto exacto donde queda la pieza. Vacío = este objeto.")]
        public Transform socket;

        [Header("Misión")]
        public MissionAction action = MissionAction.EnergyModuleLoadedIntoHatch;

        [Header("Visual")]
        [Tooltip("Silueta/guía que se muestra mientras la ranura está vacía (opcional)")]
        public GameObject ghost;

        public MissionPickup Occupant { get; private set; }

        public MissionAction TargetAction => action;
        public Vector3 MarkerPosition => Socket.position;
        public bool IsTargetAvailable => isActiveAndEnabled && Occupant == null;

        private Transform Socket => socket != null ? socket : transform;

        private void OnEnable()
        {
            if (!_all.Contains(this)) _all.Add(this);
            MissionGate.Register(this);
        }

        private void OnDisable()
        {
            _all.Remove(this);
            MissionGate.Unregister(this);
        }

        private void Start()
        {
            var m = MissionManager.Instance;
            if (m != null) m.OnMissionReset += ResetZone;
            if (ghost != null) ghost.SetActive(true);
        }

        private void OnDestroy()
        {
            var m = MissionManager.Instance;
            if (m != null) m.OnMissionReset -= ResetZone;
        }

        /// <summary>La ranura libre más cercana que acepta esta pieza (o null).</summary>
        public static MissionDropZone FindBestFor(MissionPickup p)
        {
            MissionDropZone best = null;
            float bestD = float.MaxValue;
            foreach (var z in _all)
            {
                if (z == null || !z.isActiveAndEnabled || z.Occupant != null) continue;
                if (z.acceptsItemId != p.itemId) continue;
                float d = Vector3.Distance(z.Socket.position, p.transform.position);
                if (d <= z.acceptRadius && d < bestD) { bestD = d; best = z; }
            }
            return best;
        }

        public bool TryAccept(MissionPickup p)
        {
            if (p == null || Occupant != null) return false;
            if (!MissionGate.TryComplete(action)) return false;

            Occupant = p;
            p.SnapInto(Socket);
            if (ghost != null) ghost.SetActive(false);
            return true;
        }

        private void ResetZone()
        {
            Occupant = null;
            if (ghost != null) ghost.SetActive(true);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.6f);
            Gizmos.DrawWireSphere(Socket.position, acceptRadius);
        }
    }
}
