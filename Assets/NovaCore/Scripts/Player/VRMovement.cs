using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using XRCommonUsages = UnityEngine.XR.CommonUsages;

namespace NovaCore
{
    /// <summary>
    /// Movimiento VR dedicado para la escena 01_Orbit — modo espectador sin gravedad.
    ///
    /// La rotación de CABEZA (pitch/yaw del HMD) la gestiona el TrackedPoseDriver
    /// del objeto cámara, NO este script. VRMovement solo mueve el body y hace
    /// snap turn del transform raíz cuando el usuario usa el thumbstick derecho.
    ///
    /// Controles Meta Quest 2:
    ///   Thumbstick izquierdo X/Y  → mover lateral / avanzar (relativo a la mirada)
    ///   Thumbstick derecho X      → snap turn body ±45°
    ///   Thumbstick derecho Y      → subir / bajar (world-up)
    ///   Gatillo izquierdo         → turbo (4×)
    ///
    /// Setup esperado en la jerarquía de Unity:
    ///   Player  (Transform) ← añadir este script
    ///   └─ Camera Offset  (Transform vacío, hijo de Player)
    ///      └─ Main Camera  (Camera + TrackedPoseDriver)
    ///            positionAction : <XRHMD>/centerEyePosition
    ///            rotationAction : <XRHMD>/centerEyeRotation
    ///
    /// En desktop (sin HMD) el script cede el control a OrbitFreeFly si está presente
    /// en el mismo GameObject, o bien puede usarse de forma autónoma con el fallback
    /// de teclado/ratón incluido aquí.
    /// </summary>
    [DisallowMultipleComponent]
    public class VRMovement : MonoBehaviour
    {
        // ── Inspector ───────────────────────────────────────────────────────
        [Header("Velocidad")]
        [Tooltip("Velocidad base en m/s")]
        public float moveSpeed = 25f;

        [Tooltip("Multiplicador al presionar el gatillo izquierdo (turbo)")]
        public float turboMultiplier = 4f;

        [Tooltip("Rango de ajuste de velocidad via scroll de ratón (desktop)")]
        public Vector2 speedRange = new Vector2(2f, 300f);

        [Header("VR")]
        [Tooltip("Ángulo de snap turn (grados) al inclinar el thumbstick derecho")]
        public float snapTurnAngle = 45f;

        [Tooltip("Umbral del thumbstick para activar el snap turn")]
        [Range(0.3f, 0.9f)]
        public float snapTurnThreshold = 0.6f;

        [Tooltip("Umbral inferior para resetear el snap turn (zona muerta)")]
        [Range(0.05f, 0.3f)]
        public float snapTurnReset = 0.15f;

        [Header("Suavizado")]
        [Tooltip("Lerp de velocidad. 0 = instantáneo, 20 = muy suave.")]
        [Range(0f, 20f)]
        public float smoothing = 8f;

        [Header("Desktop (fallback sin HMD)")]
        public float mouseSensitivity = 0.15f;
        public float pitchLimit = 85f;

        [Header("Referencias")]
        [Tooltip("Cámara hija (con TrackedPoseDriver en VR). Si está vacío, usa Camera.main.")]
        public Camera headCamera;

        // ── Estado interno ───────────────────────────────────────────────────
        private Vector3 _velocity;
        private bool    _snapReady = true;

        // Desktop
        private float _yaw;
        private float _pitch;

        // ── Vida ─────────────────────────────────────────────────────────────
        private void Awake()
        {
            if (headCamera == null)
                headCamera = GetComponentInChildren<Camera>(true);
            if (headCamera == null)
                headCamera = Camera.main;

            _yaw   = transform.eulerAngles.y;
            _pitch = headCamera != null
                ? headCamera.transform.localEulerAngles.x
                : 0f;
            if (_pitch > 180f) _pitch -= 360f;
        }

        private void Update()
        {
            if (IsVR())
            {
                HandleVRSnapTurn();
                HandleVRTranslation();
            }
            else
            {
                // Si hay OrbitFreeFly en el mismo objeto, deja que él maneje desktop
                if (GetComponent<OrbitFreeFly>() != null) return;

                HandleDesktopCursor();
                HandleDesktopRotation();
                HandleDesktopTranslation();
                HandleScrollSpeed();
            }
        }

        // ════════════════════════════════════════════════════════════════════
        // VR
        // ════════════════════════════════════════════════════════════════════

        private void HandleVRSnapTurn()
        {
            var right = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            if (!right.isValid) return;
            if (!right.TryGetFeatureValue(XRCommonUsages.primary2DAxis, out Vector2 stick)) return;

            if (Mathf.Abs(stick.x) > snapTurnThreshold)
            {
                if (_snapReady)
                {
                    transform.Rotate(Vector3.up, Mathf.Sign(stick.x) * snapTurnAngle, Space.World);
                    _snapReady = false;
                }
            }
            else if (Mathf.Abs(stick.x) < snapTurnReset)
            {
                _snapReady = true;
            }
        }

        private void HandleVRTranslation()
        {
            var left  = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            var right = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);

            float h = 0f, v = 0f, up = 0f;
            bool turbo = false;

            // Thumbstick izquierdo → lateral y avance
            if (left.isValid && left.TryGetFeatureValue(XRCommonUsages.primary2DAxis, out Vector2 leftStick))
            {
                h = leftStick.x;
                v = leftStick.y;
            }

            // Thumbstick derecho Y → subir/bajar
            if (right.isValid && right.TryGetFeatureValue(XRCommonUsages.primary2DAxis, out Vector2 rightStick))
            {
                up = rightStick.y;
            }

            // Gatillo izquierdo → turbo
            if (left.isValid && left.TryGetFeatureValue(XRCommonUsages.trigger, out float trigger))
                turbo = trigger > 0.5f;

            ApplyMovement(h, v, up, turbo);
        }

        // ════════════════════════════════════════════════════════════════════
        // DESKTOP FALLBACK
        // ════════════════════════════════════════════════════════════════════

        private void HandleDesktopCursor()
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.escapeKey.wasPressedThisFrame)
            {
                bool locked = Cursor.lockState == CursorLockMode.Locked;
                Cursor.lockState = locked ? CursorLockMode.None : CursorLockMode.Locked;
                Cursor.visible   = locked;
            }
        }

        private void HandleDesktopRotation()
        {
            bool canRotate = Cursor.lockState == CursorLockMode.Locked
                          || (Mouse.current != null && Mouse.current.rightButton.isPressed);
            if (!canRotate) return;

            var mouse = Mouse.current;
            if (mouse == null) return;

            Vector2 delta = mouse.delta.ReadValue();
            _yaw   += delta.x * mouseSensitivity;
            _pitch -= delta.y * mouseSensitivity;
            _pitch  = Mathf.Clamp(_pitch, -pitchLimit, pitchLimit);

            transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
            if (headCamera != null)
                headCamera.transform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }

        private void HandleDesktopTranslation()
        {
            var kb = Keyboard.current;
            float h = 0f, v = 0f, up = 0f;
            bool sprint = false;

            if (kb != null)
            {
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed)   v  += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed)  v  -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) h  += 1f;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed)  h  -= 1f;
                if (kb.eKey.isPressed || kb.pageUpKey.isPressed)     up += 1f;
                if (kb.qKey.isPressed || kb.pageDownKey.isPressed)   up -= 1f;
                sprint = kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed;
            }

            ApplyMovement(h, v, up, sprint);
        }

        private void HandleScrollSpeed()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;
            float scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) < 0.001f) return;
            moveSpeed = Mathf.Clamp(moveSpeed + scroll * 0.02f * moveSpeed, speedRange.x, speedRange.y);
        }

        // ════════════════════════════════════════════════════════════════════
        // MOVIMIENTO COMPARTIDO
        // ════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Mueve el transform raíz en la dirección pedida, orientado a la cámara (cabeza).
        /// El movimiento hacia delante/atrás respeta el pitch del HMD para volar en diagonal.
        /// Subir/bajar siempre usa el eje world-up para que sea intuitivo.
        /// </summary>
        private void ApplyMovement(float h, float v, float up, bool turbo)
        {
            float speed = moveSpeed * (turbo ? turboMultiplier : 1f);

            Transform cam = headCamera != null ? headCamera.transform : transform;
            Vector3 wish  = cam.right   * h
                          + cam.forward * v
                          + Vector3.up  * up;

            Vector3 target = wish.magnitude > 0.001f
                ? wish.normalized * speed
                : Vector3.zero;

            _velocity = smoothing > 0f
                ? Vector3.Lerp(_velocity, target, Time.deltaTime * smoothing)
                : target;

            transform.position += _velocity * Time.deltaTime;
        }

        // ════════════════════════════════════════════════════════════════════
        // UTIL
        // ════════════════════════════════════════════════════════════════════

        private static bool IsVR() => XRSettings.isDeviceActive;
    }
}
