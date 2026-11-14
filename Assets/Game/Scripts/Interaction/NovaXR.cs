using UnityEngine;
using UnityEngine.XR;
using XRCommonUsages = UnityEngine.XR.CommonUsages;

namespace NovaCore
{
    /// <summary>
    /// Punto único para saber si hay VR activo y leer botones de los mandos (sin XRI).
    /// Todos los sistemas (OrbitFreeFly, XRPlayerRig, interacción) usan este criterio
    /// para que nunca haya uno en modo VR y otro en modo escritorio a la vez.
    /// </summary>
    public static class NovaXR
    {
        /// <summary>true cuando el visor está activo y renderizando.</summary>
        public static bool IsActive => XRSettings.isDeviceActive;

        public static InputDevice Device(XRNode node) => InputDevices.GetDeviceAtXRNode(node);

        public static bool Button(XRNode node, InputFeatureUsage<bool> usage)
        {
            var dev = Device(node);
            return dev.isValid && dev.TryGetFeatureValue(usage, out bool v) && v;
        }

        public static float Axis(XRNode node, InputFeatureUsage<float> usage)
        {
            var dev = Device(node);
            return dev.isValid && dev.TryGetFeatureValue(usage, out float v) ? v : 0f;
        }

        public static Vector2 Stick(XRNode node)
        {
            var dev = Device(node);
            return dev.isValid && dev.TryGetFeatureValue(XRCommonUsages.primary2DAxis, out Vector2 v) ? v : Vector2.zero;
        }

        /// <summary>Gatillo (índice) pulsado.</summary>
        public static bool Trigger(XRNode node) => Axis(node, XRCommonUsages.trigger) > 0.6f
                                                || Button(node, XRCommonUsages.triggerButton);

        /// <summary>Botón lateral (grip) pulsado.</summary>
        public static bool Grip(XRNode node) => Axis(node, XRCommonUsages.grip) > 0.6f
                                             || Button(node, XRCommonUsages.gripButton);

        /// <summary>Vibración corta del mando (si el dispositivo la soporta).</summary>
        public static void Haptic(XRNode node, float amplitude, float seconds)
        {
            var dev = Device(node);
            if (!dev.isValid) return;
            if (dev.TryGetHapticCapabilities(out HapticCapabilities caps) && caps.supportsImpulse)
                dev.SendHapticImpulse(0u, Mathf.Clamp01(amplitude), seconds);
        }
    }
}
