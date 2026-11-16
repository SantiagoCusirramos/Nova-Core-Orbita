using UnityEngine;
using UnityEngine.XR;

namespace NovaCore
{
    /// <summary>
    /// Mueve la mano con la pose del mando en cada frame y justo antes de renderizar
    /// (sin el retraso de un frame). Sustituye al TrackedPoseDriver en las manos: lee
    /// el dispositivo XR directamente (el mismo que usan los botones), así la mano no se
    /// queda congelada si el runtime no informa el "tracking state" del mando.
    /// Si un frame el mando no da pose, la mano se queda en la última pose válida.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class NovaHandPose : MonoBehaviour
    {
        public XRNode node = XRNode.LeftHand;

        public bool HasPose { get; private set; }

        private void OnEnable()  { Application.onBeforeRender += OnBeforeRender; }
        private void OnDisable() { Application.onBeforeRender -= OnBeforeRender; }

        private void Update()          { Apply(); }
        private void OnBeforeRender()  { Apply(); }

        private void Apply()
        {
            var dev = InputDevices.GetDeviceAtXRNode(node);
            if (!dev.isValid) return;

            bool gotPos = dev.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 p);
            bool gotRot = dev.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion r);
            if (gotPos) transform.localPosition = p;
            if (gotRot && (r.x != 0f || r.y != 0f || r.z != 0f || r.w != 0f)) transform.localRotation = r;
            HasPose |= gotPos && gotRot;
        }
    }
}
