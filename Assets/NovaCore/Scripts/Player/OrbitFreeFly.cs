using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using XRCommonUsages = UnityEngine.XR.CommonUsages;

namespace NovaCore
{
    /// <summary>
    /// Movimiento del jugador con tres modos (campo 'mode'):
    ///   FreeFly   → vuelo libre (vista exterior).
    ///   Walk      → interiores: se camina como con gravedad, a altura fija.
    ///   Thrusters → EVA: propulsores con inercia. Teclado: WASD + Espacio/Ctrl, X = estabilizar.
    ///               VR: stick izq. empuja, A sube, B baja, click stick izq. = estabilizar.
    ///
    /// Movimiento libre estilo "gravedad cero". Funciona en escritorio (teclado + ratón)
    /// y en VR (thumbsticks Meta Quest).
    ///
    /// Colisiones: si el Player tiene un CharacterController habilitado, el movimiento usa
    /// CharacterController.Move (no atraviesa paredes: interiores de la ISS). Si no lo tiene
    /// (escena 01_Orbit), se mueve libremente como antes.
    ///
    /// Escritorio:
    ///   W/S  o flechas ↑↓     → avanzar / retroceder
    ///   A/D  o flechas ←→     → desplazarse lateral
    ///   E / Q                  → subir / bajar
    ///   Cursor bloqueado o click derecho + ratón → mirar
    ///   Shift                  → turbo
    ///   Scroll                 → ajustar velocidad base
    ///   ESC                    → soltar / bloquear cursor
    ///
    /// VR (Meta Quest 2):
    ///   Stick izquierdo        → avanzar / retroceder / lateral (hacia donde miras)
    ///   Stick derecho X        → snap turn ±45° (gira alrededor de la cabeza)
    ///   Stick derecho Y        → subir / bajar
    ///   Click stick izquierdo  → turbo (los gatillos quedan libres para interactuar)
    /// </summary>
    public class OrbitFreeFly : MonoBehaviour
    {
        public enum MoveMode
        {
            /// <summary>Vuelo libre (vista exterior 01_Orbit).</summary>
            FreeFly,
            /// <summary>Interior con gravedad simulada: caminar a altura fija, sin subir/bajar.</summary>
            Walk,
            /// <summary>Exterior (EVA): mochila de propulsores con inercia, como el SAFER de la NASA.</summary>
            Thrusters
        }

        [Header("Modo de movimiento")]
        public MoveMode mode = MoveMode.FreeFly;

        [Header("Propulsores (solo modo Thrusters)")]
        [Tooltip("Aceleración de los propulsores en m/s²")]
        public float thrustAcceleration = 0.9f;
        [Tooltip("Velocidad máxima en m/s")]
        public float maxThrustSpeed = 2.5f;
        [Tooltip("Freno automático suave (0 = inercia pura, como en el espacio)")]
        public float passiveDamping = 0.15f;
        [Tooltip("Fuerza del botón ESTABILIZAR (frena del todo)")]
        public float stabilizeDamping = 2.5f;

        /// <summary>True mientras algún propulsor está encendido (para sonido/efectos).</summary>
        public bool IsThrusting { get; private set; }
        /// <summary>Velocidad actual en m/s (la muestra el HUD en EVA).</summary>
        public float CurrentSpeed => _velocity.magnitude;

        [Header("Velocidad")]
        [Tooltip("Velocidad base en unidades/segundo (1 unidad = 1 metro)")]
        public float moveSpeed = 25f;

        [Tooltip("Multiplicador Shift / click del stick izquierdo en VR")]
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

        [Tooltip("Zona muerta del eje vertical del stick derecho (evita subir/bajar al hacer snap turn)")]
        [Range(0f, 0.9f)]
        public float verticalDeadZone = 0.35f;

        [Header("Colisiones (solo si hay CharacterController)")]
        [Tooltip("Mueve la cápsula del CharacterController a la posición de la cabeza cada frame")]
        public bool syncColliderToHead = true;

        [Header("Referencias")]
        [Tooltip("Cámara que rota en pitch. Si está vacío usa Camera.main.")]
        public Camera playerCamera;

        // ── estado interno ──────────────────────────────────────────────────
        private float _yaw;
        private float _pitch;
        private Vector3 _velocity;
        private CharacterController _cc;

        // VR snap turn
        private bool _snapTurnReady = true;

        // Botón ESTABILIZAR (X en teclado, click del stick izquierdo en VR) — solo Thrusters
        private bool _stabilize;

        // ── ciclo de vida ────────────────────────────────────────────────────
        private void Awake()
        {
            if (playerCamera == null)
                playerCamera = Camera.main;

            _cc = GetComponent<CharacterController>();

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
            if (playerCamera == null) playerCamera = Camera.main;

            if (NovaXR.IsActive)
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

        /// <summary>Detiene la inercia (útil tras un teletransporte).</summary>
        public void StopMotion() => _velocity = Vector3.zero;

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
                if (kb.eKey.isPressed || kb.pageUpKey.isPressed || kb.spaceKey.isPressed)      up += 1f;
                if (kb.qKey.isPressed || kb.pageDownKey.isPressed || kb.leftCtrlKey.isPressed) up -= 1f;

                sprint = kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed;
                _stabilize = kb.xKey.isPressed;
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
            Vector2 stick = NovaXR.Stick(XRNode.RightHand);

            // Snap turn horizontal
            if (Mathf.Abs(stick.x) > 0.5f)
            {
                if (_snapTurnReady)
                {
                    SnapTurn(Mathf.Sign(stick.x) * snapTurnAngle);
                    _snapTurnReady = false;
                }
            }
            else if (Mathf.Abs(stick.x) < 0.2f)
            {
                _snapTurnReady = true;
            }
        }

        /// <summary>
        /// Gira el rig alrededor de la cabeza (no del centro del guardián), para que el
        /// usuario gire sobre sí mismo aunque se haya movido dentro de su espacio físico.
        /// </summary>
        private void SnapTurn(float degrees)
        {
            Vector3 pivot = playerCamera != null ? playerCamera.transform.position : transform.position;
            transform.RotateAround(pivot, Vector3.up, degrees);

            // Con CharacterController hay que sincronizar o el siguiente Move usa la pose vieja
            if (_cc != null && _cc.enabled) Physics.SyncTransforms();
        }

        private void HandleVRMovement()
        {
            Vector2 leftStick  = NovaXR.Stick(XRNode.LeftHand);
            Vector2 rightStick = NovaXR.Stick(XRNode.RightHand);

            float h = leftStick.x;
            float v = leftStick.y;
            float up = 0f;
            bool sprint = false;

            if (mode == MoveMode.Thrusters)
            {
                // Botones propios del traje, separados de los de herramientas (grip/gatillo):
                //   A = propulsor arriba, B = propulsor abajo, click stick izquierdo = ESTABILIZAR
                if (NovaXR.Button(XRNode.RightHand, XRCommonUsages.primaryButton))   up += 1f;
                if (NovaXR.Button(XRNode.RightHand, XRCommonUsages.secondaryButton)) up -= 1f;
                _stabilize = NovaXR.Button(XRNode.LeftHand, XRCommonUsages.primary2DAxisClick);
            }
            else
            {
                // Stick derecho Y → subir/bajar, solo si el gesto es claramente vertical
                if (Mathf.Abs(rightStick.y) > verticalDeadZone && Mathf.Abs(rightStick.y) > Mathf.Abs(rightStick.x))
                    up = rightStick.y;

                // Click del stick izquierdo → turbo (gatillos libres para interactuar)
                sprint = NovaXR.Button(XRNode.LeftHand, XRCommonUsages.primary2DAxisClick);
            }

            ApplyMovement(h, v, up, sprint);
        }

        // ════════════════════════════════════════════════════════════════════
        // MOVIMIENTO COMÚN
        // ════════════════════════════════════════════════════════════════════

        private void ApplyMovement(float h, float v, float up, bool sprint)
        {
            Transform cam = playerCamera != null ? playerCamera.transform : transform;

            if (mode == MoveMode.Thrusters)
            {
                ApplyThrusters(cam, h, v, up);
            }
            else
            {
                float speed = moveSpeed * (sprint ? sprintMultiplier : 1f);
                Vector3 wish;

                if (mode == MoveMode.Walk)
                {
                    // Caminar: solo en el plano horizontal (como con gravedad), sin subir ni bajar
                    Vector3 fwd = Vector3.ProjectOnPlane(cam.forward, Vector3.up);
                    if (fwd.sqrMagnitude < 0.0001f) fwd = Vector3.ProjectOnPlane(cam.up, Vector3.up);
                    fwd.Normalize();
                    Vector3 right = Vector3.Cross(Vector3.up, fwd);
                    wish = right * h + fwd * v;
                }
                else
                {
                    // Vuelo libre: relativo a la cámara (incluye el pitch para volar en diagonal)
                    wish = cam.right * h + cam.forward * v + Vector3.up * up;
                }

                Vector3 target = wish.magnitude > 0.001f
                    ? Vector3.ClampMagnitude(wish, 1f) * speed
                    : Vector3.zero;

                _velocity = smoothing > 0f
                    ? Vector3.Lerp(_velocity, target, Time.deltaTime * smoothing)
                    : target;
                if (mode == MoveMode.Walk) _velocity.y = 0f;
                IsThrusting = false;
            }

            Vector3 delta = _velocity * Time.deltaTime;

            if (_cc != null && _cc.enabled)
            {
                if (syncColliderToHead && playerCamera != null)
                    _cc.center = transform.InverseTransformPoint(playerCamera.transform.position);

                Vector3 before = transform.position;
                _cc.Move(delta);

                // Si chocamos, la inercia no sigue empujando contra la pared
                if (Time.deltaTime > 0f)
                    _velocity = (transform.position - before) / Time.deltaTime;
            }
            else
            {
                transform.position += delta;
            }
        }

        /// <summary>
        /// Física de la mochila de propulsores: el stick acelera (no mueve directamente),
        /// al soltar se sigue flotando con la misma velocidad y ESTABILIZAR frena.
        /// </summary>
        private void ApplyThrusters(Transform cam, float h, float v, float up)
        {
            Vector3 dir = cam.right * h + cam.forward * v + Vector3.up * up;
            IsThrusting = dir.sqrMagnitude > 0.01f;
            if (IsThrusting)
                _velocity += Vector3.ClampMagnitude(dir, 1f) * thrustAcceleration * Time.deltaTime;

            float damping = _stabilize ? stabilizeDamping : (IsThrusting ? 0f : passiveDamping);
            if (damping > 0f) _velocity *= Mathf.Exp(-damping * Time.deltaTime);

            _velocity = Vector3.ClampMagnitude(_velocity, maxThrustSpeed);
        }

    }
}
