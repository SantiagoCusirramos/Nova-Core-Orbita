using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using XRCommonUsages = UnityEngine.XR.CommonUsages;

namespace NovaCore
{
    /// <summary>
    /// Movimiento libre estilo "modo creativo" — sin gravedad, sin colisiones.
    /// Funciona en desktop (teclado + ratón) y en VR (thumbsticks Meta Quest).
    ///
    /// Desktop:
    ///   W/S  o flechas ↑↓     → avanzar / retroceder
    ///   A/D  o flechas ←→     → desplazarse lateral
    ///   E / Q                  → subir / bajar
    ///   Click derecho + ratón  → rotar cámara
    ///   Shift                  → turbo (3×)
    ///   Scroll                 → ajustar velocidad base
    ///   ESC                    → soltar / bloquear cursor
    ///
    /// VR (Meta Quest 2):
    ///   Thumbstick izquierdo   → avanzar / retroceder / lateral
    ///   Thumbstick derecho X   → snap turn ±45°
    ///   Thumbstick derecho Y   → subir / bajar
    ///   Gatillo izquierdo      → turbo
    /// </summary>
    public class OrbitFreeFly : MonoBehaviour
    {
        [Header("Velocidad")]
        [Tooltip("Velocidad base en unidades/segundo (1 unidad = 1 metro)")]
        public float moveSpeed = 25f;

        [Tooltip("Multiplicador Shift / gatillo izquierdo VR")]
        public float sprintMultiplier = 4f;

        [Tooltip("Límite mínimo y máximo de velocidad via scroll")]
        public Vector2 speedRange = new Vector2(2f, 300f);

        [Header("Ratón")]
        public float mouseSensitivity = 0.15f;
        public float pitchLimit = 85f;

        [Header("Suavizado")]
        [Tooltip("Inercia del movimiento. 0 = respuesta inmediata.")]
        [Range(0f, 20f)]
        public float smoothing = 10f;

        [Header("VR")]
        [Tooltip("Ángulo de snap turn en VR")]
        public float snapTurnAngle = 45f;

        [Header("Referencias")]
        [Tooltip("Cámara que rota en pitch. Si está vacío usa Camera.main.")]
        public Camera playerCamera;

        // ── estado interno ──────────────────────────────────────────────────
        private float _yaw;
        private float _pitch;
        private Vector3 _velocity;

        // VR snap turn
        private bool _snapTurnReady = true;

        // ── ciclo de vida ────────────────────────────────────────────────────
        private void Awake()
        {
            if (playerCamera == null)
                playerCamera = Camera.main;

            // Inicializar ángulos desde la rotación actual para no tener salto brusco
            _yaw   = transform.eulerAngles.y;
            _pitch = playerCamera != null
                ? playerCamera.transform.localEulerAngles.x
                : transform.eulerAngles.x;

            // Normalizar pitch a [-180, 180]
            if (_pitch > 180f) _pitch -= 360f;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;
        }

        private void Update()
        {
            if (IsVR())
            {
                HandleVRRotation();
                HandleVRMovement();
            }
            else
            {
                HandleDesktopCursorToggle();
                HandleDesktopRotation();
                HandleDesktopMovement();
                HandleScrollSpeed();
            }
        }

        // ════════════════════════════════════════════════════════════════════
        // DESKTOP
        // ════════════════════════════════════════════════════════════════════

        private void HandleDesktopCursorToggle()
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
            // Solo rotar mientras el cursor está bloqueado O con click derecho held
            bool canRotate = Cursor.lockState == CursorLockMode.Locked
                          || (Mouse.current != null && Mouse.current.rightButton.isPressed);

            if (!canRotate) return;

            var mouse = Mouse.current;
            if (mouse == null) return;

            Vector2 delta = mouse.delta.ReadValue();
            _yaw   += delta.x * mouseSensitivity;
            _pitch -= delta.y * mouseSensitivity;
            _pitch  = Mathf.Clamp(_pitch, -pitchLimit, pitchLimit);

            // Yaw en el transform raíz, pitch en la cámara (separa mirada de cuerpo)
            transform.rotation = Quaternion.Euler(0f, _yaw, 0f);

            if (playerCamera != null)
                playerCamera.transform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }

        private void HandleDesktopMovement()
        {
            var kb = Keyboard.current;

            float h  = 0f, v = 0f, up = 0f;
            bool sprint = false;

            if (kb != null)
            {
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed)    v  += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed)   v  -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed)  h  += 1f;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed)   h  -= 1f;
                if (kb.eKey.isPressed || kb.pageUpKey.isPressed)      up += 1f;
                if (kb.qKey.isPressed || kb.pageDownKey.isPressed)    up -= 1f;

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
        // VR
        // ════════════════════════════════════════════════════════════════════

        private void HandleVRRotation()
        {
            var rightHand = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            if (!rightHand.isValid) return;

            if (!rightHand.TryGetFeatureValue(XRCommonUsages.primary2DAxis, out Vector2 stick)) return;

            // Snap turn horizontal
            if (Mathf.Abs(stick.x) > 0.5f)
            {
                if (_snapTurnReady)
                {
                    transform.Rotate(Vector3.up, Mathf.Sign(stick.x) * snapTurnAngle);
                    _snapTurnReady = false;
                }
            }
            else if (Mathf.Abs(stick.x) < 0.2f)
            {
                _snapTurnReady = true;
            }
        }

        private void HandleVRMovement()
        {
            var leftHand  = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            var rightHand = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);

            float h = 0f, v = 0f, up = 0f;
            bool sprint = false;

            // Thumbstick izquierdo → movimiento horizontal/vertical
            if (leftHand.isValid && leftHand.TryGetFeatureValue(XRCommonUsages.primary2DAxis, out Vector2 leftStick))
            {
                h = leftStick.x;
                v = leftStick.y;
            }

            // Thumbstick derecho Y → subir/bajar
            if (rightHand.isValid && rightHand.TryGetFeatureValue(XRCommonUsages.primary2DAxis, out Vector2 rightStick))
            {
                up = rightStick.y;
            }

            // Gatillo izquierdo → turbo
            if (leftHand.isValid && leftHand.TryGetFeatureValue(XRCommonUsages.trigger, out float trigger))
                sprint = trigger > 0.5f;

            ApplyMovement(h, v, up, sprint);
        }

        // ════════════════════════════════════════════════════════════════════
        // MOVIMIENTO COMÚN
        // ════════════════════════════════════════════════════════════════════

        private void ApplyMovement(float h, float v, float up, bool sprint)
        {
            float speed = moveSpeed * (sprint ? sprintMultiplier : 1f);

            // Dirección relativa a la cámara (incluye el pitch para volar en diagonal)
            Transform cam = playerCamera != null ? playerCamera.transform : transform;
            Vector3 wish  = cam.right   * h
                          + cam.forward * v
                          + Vector3.up  * up;   // subir/bajar siempre en world-up

            Vector3 target = wish.magnitude > 0.001f
                ? wish.normalized * speed
                : Vector3.zero;

            // Suavizado con Lerp
            _velocity = smoothing > 0f
                ? Vector3.Lerp(_velocity, target, Time.deltaTime * smoothing)
                : target;

            transform.position += _velocity * Time.deltaTime;
        }

        // ════════════════════════════════════════════════════════════════════
        // UTIL
        // ════════════════════════════════════════════════════════════════════

        private static bool IsVR() => UnityEngine.XR.XRSettings.isDeviceActive;
    }
}
