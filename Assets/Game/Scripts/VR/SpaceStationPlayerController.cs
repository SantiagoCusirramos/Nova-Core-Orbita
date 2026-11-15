using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using XRCommonUsages = UnityEngine.XR.CommonUsages;

namespace NovaCore
{
    /// <summary>
    /// Controlador de jugador en primera persona con soporte completo para:
    ///   - Desktop: teclado + ratÃ³n (New Input System)
    ///   - VR: Meta Quest 2 â€” head tracking via XR, thumbsticks, snap turn
    ///
    /// Incluye:
    ///   - Movimiento con colisiÃ³n (CharacterController)
    ///   - Head bobbing (desactivado en VR para evitar mareo)
    ///   - Linterna (F / botÃ³n Y del mando)
    ///   - ProtecciÃ³n anti-void (respawn si cae a Y < -15)
    ///   - Snap turn Â±45Â° en VR
    ///
    /// Portado de SpaceStation5.SpaceStationPlayerController â†’ namespace NovaCore.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class SpaceStationPlayerController : MonoBehaviour
    {
        [Header("Movimiento")]
        public float walkSpeed   = 5.0f;
        public float sprintSpeed = 9.0f;
        public float jumpHeight  = 1.3f;
        public float gravity     = -15.0f;

        [Header("Mirada")]
        public float mouseSensitivity = 0.15f;
        public float lookUpLimit      = 85.0f;
        public float lookDownLimit    = -85.0f;
        public float vrSnapTurnAngle  = 45.0f;

        [Header("Head Bobbing")]
        public bool  enableHeadBob          = true;
        public float bobFrequency           = 8.0f;
        public float bobHorizontalAmplitude = 0.04f;
        public float bobVerticalAmplitude   = 0.05f;

        [Header("Seguridad y Spawn")]
        [Tooltip("PosiciÃ³n de respawn si el jugador cae fuera del escenario")]
        public Vector3 spawnPosition = new Vector3(0f, 1.2f, 6.0f);

        [Header("Referencias")]
        public Camera playerCamera;
        public Light  flashlight;

        // â”€â”€ Estado interno â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        private CharacterController controller;
        private Vector3 velocity;
        private float   verticalRotation;
        private Vector3 defaultCameraLocalPos;
        private float   bobTimer;
        private bool    vrSnapTurnReset = true;
        private bool    vrJumpHeld;
        private bool    vrFlashlightHeld;

        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        private void Awake()
        {
            controller        = GetComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = 0.4f;
            controller.center = new Vector3(0f, 0.9f, 0f);

            if (playerCamera == null)
                playerCamera = GetComponentInChildren<Camera>();

            if (playerCamera != null)
                defaultCameraLocalPos = playerCamera.transform.localPosition;

            if (spawnPosition == Vector3.zero)
                spawnPosition = transform.position;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;
        }

        private void Update()
        {
            // Anti-void: respawn si cae demasiado
            if (transform.position.y < -15.0f)
            {
                velocity            = Vector3.zero;
                controller.enabled  = false;
                transform.position  = spawnPosition;
                controller.enabled  = true;
            }

            HandleRotation();
            HandleMovement();
            HandleHeadBob();
            HandleFlashlight();
            HandleCursorToggle();
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // ROTACIÃ“N
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        private void HandleRotation()
        {
            bool isVR = XRSettings.isDeviceActive;

            if (isVR)
            {
                // En VR la rotaciÃ³n de cabeza la gestiona el TrackedPoseDriver del HMD.
                // AquÃ­ solo se aplica el snap turn del body.
                float turnInput = 0f;

                var rightHand = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
                if (rightHand.isValid && rightHand.TryGetFeatureValue(XRCommonUsages.primary2DAxis, out Vector2 rightStick))
                    turnInput = rightStick.x;

                // Fallback: gamepad derecho
                if (Mathf.Abs(turnInput) < 0.1f)
                {
                    var gp = Gamepad.current;
                    if (gp != null) turnInput = gp.rightStick.ReadValue().x;
                }

                if (Mathf.Abs(turnInput) > 0.5f)
                {
                    if (vrSnapTurnReset)
                    {
                        transform.Rotate(Vector3.up * Mathf.Sign(turnInput) * vrSnapTurnAngle);
                        vrSnapTurnReset = false;
                    }
                }
                else if (Mathf.Abs(turnInput) < 0.2f)
                {
                    vrSnapTurnReset = true;
                }
                return;
            }

            // Desktop â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
            if (Cursor.lockState != CursorLockMode.Locked) return;
            var mouse = Mouse.current;
            if (mouse == null) return;

            Vector2 delta = mouse.delta.ReadValue();
            transform.Rotate(Vector3.up * (delta.x * mouseSensitivity));

            verticalRotation -= delta.y * mouseSensitivity;
            verticalRotation  = Mathf.Clamp(verticalRotation, lookDownLimit, lookUpLimit);

            if (playerCamera != null)
                playerCamera.transform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // MOVIMIENTO
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        private void HandleMovement()
        {
            bool isGrounded = controller.isGrounded;
            if (isGrounded && velocity.y < 0f)
                velocity.y = -2f;

            var  kb           = Keyboard.current;
            float h           = 0f, v = 0f;
            bool  isSprinting = false, jumpPressed = false;

            if (kb != null)
            {
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed)   v += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed)  v -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) h += 1f;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed)  h -= 1f;
                isSprinting = kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed;
                jumpPressed = kb.spaceKey.wasPressedThisFrame;
            }

            // VR: thumbstick izquierdo
            var leftHand = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            if (leftHand.isValid)
            {
                if (leftHand.TryGetFeatureValue(XRCommonUsages.primary2DAxis, out Vector2 leftStick))
                {
                    h += leftStick.x;
                    v += leftStick.y;
                }
                if (leftHand.TryGetFeatureValue(XRCommonUsages.primary2DAxisClick, out bool thumbClick) && thumbClick)
                    isSprinting = true;
            }

            // VR: botÃ³n A (primaryButton) del mando derecho â†’ saltar (edge-triggered)
            var rightHand = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            bool vrJumpNow = false;
            if (rightHand.isValid && rightHand.TryGetFeatureValue(XRCommonUsages.primaryButton, out bool aBtn))
                vrJumpNow = aBtn;

            if (vrJumpNow && !vrJumpHeld) jumpPressed = true;
            vrJumpHeld = vrJumpNow;

            // Gamepad
            var gp = Gamepad.current;
            if (gp != null)
            {
                Vector2 stick = gp.leftStick.ReadValue();
                h += stick.x;
                v += stick.y;
                if (gp.leftStickButton.isPressed)   isSprinting = true;
                if (gp.buttonSouth.wasPressedThisFrame) jumpPressed = true;
            }

            float speed   = isSprinting ? sprintSpeed : walkSpeed;
            Vector3 fwd   = transform.forward;
            Vector3 right = transform.right;

            if (playerCamera != null && XRSettings.isDeviceActive)
            {
                fwd   = Vector3.ProjectOnPlane(playerCamera.transform.forward, Vector3.up).normalized;
                right = Vector3.ProjectOnPlane(playerCamera.transform.right,   Vector3.up).normalized;
            }

            Vector3 move = right * h + fwd * v;
            if (move.sqrMagnitude > 1f) move.Normalize();

            if (jumpPressed && isGrounded)
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

            velocity.y += gravity * Time.deltaTime;

            controller.Move((move * speed + velocity) * Time.deltaTime);
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // HEAD BOB, LINTERNA, CURSOR
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        private void HandleHeadBob()
        {
            if (XRSettings.isDeviceActive) return;   // Evitar mareo en VR
            if (!enableHeadBob || playerCamera == null) return;

            var  kb       = Keyboard.current;
            bool moving   = false, sprinting = false;
            if (kb != null)
            {
                moving   = kb.wKey.isPressed || kb.sKey.isPressed || kb.aKey.isPressed || kb.dKey.isPressed;
                sprinting = kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed;
            }

            if (controller.isGrounded && moving)
            {
                float mult = sprinting ? 1.4f : 1.0f;
                bobTimer += Time.deltaTime * bobFrequency * mult;
                float bobX = Mathf.Cos(bobTimer * 0.5f) * bobHorizontalAmplitude;
                float bobY = Mathf.Sin(bobTimer)         * bobVerticalAmplitude;
                playerCamera.transform.localPosition = defaultCameraLocalPos + new Vector3(bobX, bobY, 0f);
            }
            else
            {
                bobTimer = 0f;
                playerCamera.transform.localPosition = Vector3.Lerp(
                    playerCamera.transform.localPosition, defaultCameraLocalPos, Time.deltaTime * 6f);
            }
        }

        private void HandleFlashlight()
        {
            bool toggle = false;

            var kb = Keyboard.current;
            if (kb != null && kb.fKey.wasPressedThisFrame) toggle = true;

            // VR: botÃ³n Y (secondaryButton) del mando izquierdo â€” edge-triggered
            var leftHand = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            bool vrFNow = false;
            if (leftHand.isValid && leftHand.TryGetFeatureValue(XRCommonUsages.secondaryButton, out bool yBtn))
                vrFNow = yBtn;

            if (vrFNow && !vrFlashlightHeld) toggle = true;
            vrFlashlightHeld = vrFNow;

            if (toggle && flashlight != null)
                flashlight.enabled = !flashlight.enabled;
        }

        private void HandleCursorToggle()
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            if (!kb.escapeKey.wasPressedThisFrame) return;

            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible   = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible   = false;
            }
        }
    }
}
