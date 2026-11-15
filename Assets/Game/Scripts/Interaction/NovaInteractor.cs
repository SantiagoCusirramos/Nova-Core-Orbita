using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR;

namespace NovaCore
{
    /// <summary>
    /// "Mano" que interactúa con NovaInteractable sin XR Interaction Toolkit.
    ///
    /// Modo VR (una por mando):  rayo láser desde el mando.
    ///   Gatillo  → usar (pulsar / mantener)
    ///   Grip     → agarrar mientras se mantiene; al soltar el grip se suelta el objeto
    ///
    /// Modo escritorio (una en la cámara): rayo desde el centro de la pantalla.
    ///   Click izquierdo → usar (pulsar / mantener)
    ///   F               → agarrar / soltar (alterna)
    ///
    /// Lo crea NovaInteractionRig; no hace falta añadirlo a mano.
    /// </summary>
    public class NovaInteractor : MonoBehaviour
    {
        public enum Source { Desktop, LeftHand, RightHand }

        [Header("Origen")]
        public Source source = Source.Desktop;

        [Tooltip("Distancia del rayo")]
        public float rayLength = 3.0f;

        [Tooltip("Rotación extra del rayo respecto al mando (grados, eje X). Ajusta si el láser apunta raro.")]
        public float rayPitchOffset = 0f;

        [Header("Objeto sujeto")]
        [Tooltip("Desplazamiento local del objeto sujeto en modo escritorio (relativo a la cámara)")]
        public Vector3 desktopHoldOffset = new Vector3(0.28f, -0.25f, 0.55f);

        [Header("Láser (solo VR)")]
        public bool showLaser = true;
        public Color laserColor = new Color(0.3f, 0.85f, 1f, 0.8f);
        public Color laserHitColor = new Color(0.4f, 1f, 0.5f, 0.9f);
        [Tooltip("Material del láser (lo asigna NovaInteractionRig). Debe soportar color de vértice.")]
        public Material laserMaterial;

        // ── Estado público ───────────────────────────────────────────────────
        public NovaInteractable Hovered { get; private set; }
        public NovaInteractable Held    { get; private set; }
        public RaycastHit LastHit       { get; private set; }
        public bool HasHit              { get; private set; }
        public Ray CurrentRay           { get; private set; }
        public bool IsVR => source != Source.Desktop;
        public XRNode Node => source == Source.LeftHand ? XRNode.LeftHand : XRNode.RightHand;

        /// <summary>Último interactor que pulsó "usar" (para el HUD).</summary>
        public static NovaInteractor LastActive { get; private set; }

        // ── Internos ─────────────────────────────────────────────────────────
        private bool _usePrev, _grabPrev;
        private NovaInteractable _usingTarget;
        private LineRenderer _laser;
        private Transform _holdAnchor;
        private Transform _heldPrevParent;
        private InputAction _aimPos, _aimRot;

        private void Start()
        {
            // Ancla donde se cuelgan los objetos sujetos
            var anchor = new GameObject("Hold Anchor").transform;
            anchor.SetParent(transform, false);
            anchor.localPosition = IsVR ? new Vector3(0f, -0.02f, 0.06f) : desktopHoldOffset;
            anchor.localRotation = Quaternion.identity;
            _holdAnchor = anchor;

            if (IsVR)
            {
                SetupAimPose();
                if (showLaser) CreateLaser();
            }
        }

        private void OnDestroy()
        {
            _aimPos?.Dispose();
            _aimRot?.Dispose();
        }

        private void OnDisable()
        {
            ForceRelease();
            SetHovered(null);
            if (_laser != null) _laser.enabled = false;
        }

        private void OnEnable()
        {
            if (_laser != null) _laser.enabled = showLaser;
        }

        // ════════════════════════════════════════════════════════════════════
        // BUCLE
        // ════════════════════════════════════════════════════════════════════

        private void Update()
        {
            CurrentRay = BuildRay();
            Raycast();

            bool use  = ReadUse();
            bool grab = ReadGrab();

            HandleGrab(grab);
            HandleUse(use);

            _usePrev  = use;
            _grabPrev = grab;

            UpdateLaser();
        }

        // ── Rayo ─────────────────────────────────────────────────────────────

        private Ray BuildRay()
        {
            // En VR intentamos usar la "pose de apuntado" (aim) de OpenXR, que es la dirección
            // natural del láser. Si el perfil no la expone, usamos la pose del mando.
            if (IsVR && TryGetAimPose(out Vector3 p, out Quaternion r))
            {
                r *= Quaternion.Euler(rayPitchOffset, 0f, 0f);
                return new Ray(p, r * Vector3.forward);
            }

            Quaternion rot = transform.rotation * Quaternion.Euler(rayPitchOffset, 0f, 0f);
            return new Ray(transform.position, rot * Vector3.forward);
        }

        private void Raycast()
        {
            HasHit = false;
            NovaInteractable target = null;

            // RaycastAll para poder saltarnos el objeto que llevamos en la mano
            var hits = Physics.RaycastAll(CurrentRay, rayLength, ~0, QueryTriggerInteraction.Ignore);
            float best = float.MaxValue;
            RaycastHit bestHit = default(RaycastHit);
            foreach (var h in hits)
            {
                if (h.collider == null) continue;
                if (Held != null && h.collider.transform.IsChildOf(Held.transform)) continue;
                if (h.collider is CharacterController) continue;
                if (h.distance < best) { best = h.distance; bestHit = h; }
            }

            if (best < float.MaxValue)
            {
                HasHit  = true;
                LastHit = bestHit;
                var it = bestHit.collider.GetComponentInParent<NovaInteractable>();
                if (it != null && it.isActiveAndEnabled && bestHit.distance <= it.maxDistance && it != Held)
                    target = it;
            }

            SetHovered(target);
        }

        private void SetHovered(NovaInteractable target)
        {
            if (Hovered == target) return;
            if (Hovered != null) Hovered.OnHoverExit(this);
            Hovered = target;
            if (Hovered != null)
            {
                Hovered.OnHoverEnter(this);
                if (IsVR) NovaXR.Haptic(Node, 0.15f, 0.03f);
            }
        }

        // ── Entrada ──────────────────────────────────────────────────────────

        private bool ReadUse()
        {
            if (IsVR) return NovaXR.Trigger(Node);

            var mouse = Mouse.current;
            // Solo con el cursor bloqueado (si está libre, el click es para la ventana)
            return mouse != null && mouse.leftButton.isPressed && Cursor.lockState == CursorLockMode.Locked;
        }

        private bool ReadGrab()
        {
            if (IsVR) return NovaXR.Grip(Node);
            var kb = Keyboard.current;
            return kb != null && kb.fKey.isPressed;
        }

        // ── Agarre ───────────────────────────────────────────────────────────

        private void HandleGrab(bool grab)
        {
            bool pressed  = grab && !_grabPrev;
            bool released = !grab && _grabPrev;

            if (IsVR)
            {
                // VR: mantener el grip = sujetar
                if (pressed && Held == null && Hovered != null)
                {
                    if (Hovered.CanGrab) Grab(Hovered);
                    else Hovered.OnGrabDenied(this);
                }
                else if (released && Held != null) Release();
            }
            else
            {
                // Escritorio: F alterna
                if (!pressed) return;
                if (Held != null) Release();
                else if (Hovered != null)
                {
                    if (Hovered.CanGrab) Grab(Hovered);
                    else Hovered.OnGrabDenied(this);
                }
            }
        }

        public void Grab(NovaInteractable target)
        {
            if (target == null || !target.CanGrab) return;
            if (target.Holder != null) target.Holder.ForceRelease();   // pasar de una mano a otra

            SetHovered(null);
            Held = target;
            target.SetHolder(this);
            _heldPrevParent = target.transform.parent;

            target.transform.SetParent(_holdAnchor, false);
            target.transform.localPosition = target.HoldLocalPosition;
            target.transform.localRotation = target.HoldLocalRotation;

            target.OnGrab(this);
            if (IsVR) NovaXR.Haptic(Node, 0.4f, 0.06f);
        }

        public void Release()
        {
            if (Held == null) return;
            var target = Held;
            Held = null;
            target.SetHolder(null);

            // Se queda flotando donde se soltó (gravedad cero)
            target.transform.SetParent(_heldPrevParent, true);
            _heldPrevParent = null;

            target.OnRelease(this);
            if (IsVR) NovaXR.Haptic(Node, 0.25f, 0.04f);
        }

        /// <summary>Suelta sin efectos (lo llaman objetos que se bloquean o se resetean).</summary>
        public void ForceRelease()
        {
            if (Held != null) Release();
        }

        /// <summary>El objeto sujeto se está desactivando: soltarlo sin tocar la jerarquía.</summary>
        public void DetachDisabled(NovaInteractable target)
        {
            if (Held != target) return;
            Held = null;
            _heldPrevParent = null;
            target.SetHolder(null);
        }

        // ── Uso ──────────────────────────────────────────────────────────────

        private void HandleUse(bool use)
        {
            bool pressed  = use && !_usePrev;
            bool released = !use && _usePrev;
            if (pressed) LastActive = this;

            // Con algo en la mano, el gatillo usa ese objeto (herramientas)
            if (Held != null)
            {
                if (pressed)  Held.OnHeldUsePressed(this);
                if (use)      Held.OnHeldUse(this, Time.deltaTime);
                if (released) Held.OnHeldUseReleased(this);
                return;
            }

            if (pressed)
            {
                _usingTarget = Hovered;
                if (_usingTarget != null)
                {
                    _usingTarget.OnUsePressed(this);
                    if (IsVR) NovaXR.Haptic(Node, 0.5f, 0.05f);
                }
            }

            if (use && _usingTarget != null)
            {
                // Si dejamos de apuntar, se corta el uso mantenido
                if (Hovered != _usingTarget)
                {
                    _usingTarget.OnUseReleased(this);
                    _usingTarget = null;
                }
                else
                {
                    _usingTarget.OnUseHeld(this, Time.deltaTime);
                }
            }

            if (released && _usingTarget != null)
            {
                _usingTarget.OnUseReleased(this);
                _usingTarget = null;
            }
        }

        // ── Pose de apuntado OpenXR (opcional) ───────────────────────────────

        private void SetupAimPose()
        {
            string hand = source == Source.LeftHand ? "{LeftHand}" : "{RightHand}";
            _aimPos = new InputAction("AimPos" + hand, InputActionType.Value,
                                      "<XRController>" + hand + "/pointerPosition", expectedControlType: "Vector3");
            _aimRot = new InputAction("AimRot" + hand, InputActionType.Value,
                                      "<XRController>" + hand + "/pointerRotation", expectedControlType: "Quaternion");
            _aimPos.Enable();
            _aimRot.Enable();
        }

        private bool TryGetAimPose(out Vector3 position, out Quaternion rotation)
        {
            position = transform.position;
            rotation = transform.rotation;
            if (_aimPos == null || _aimRot == null) return false;
            if (_aimPos.controls.Count == 0 || _aimRot.controls.Count == 0) return false;

            Quaternion lr = _aimRot.ReadValue<Quaternion>();
            if (Mathf.Abs(lr.x) + Mathf.Abs(lr.y) + Mathf.Abs(lr.z) + Mathf.Abs(lr.w) < 0.5f) return false;
            Vector3 lp = _aimPos.ReadValue<Vector3>();

            // Las poses de OpenXR vienen en espacio de tracking = espacio del padre del mando
            Transform space = transform.parent;
            if (space == null) return false;
            position = space.TransformPoint(lp);
            rotation = space.rotation * lr;
            return true;
        }

        // ── Láser ────────────────────────────────────────────────────────────

        private void CreateLaser()
        {
            _laser = gameObject.AddComponent<LineRenderer>();
            _laser.positionCount = 2;
            _laser.useWorldSpace = true;
            _laser.widthMultiplier = 0.004f;
            _laser.numCapVertices = 2;
            _laser.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _laser.receiveShadows = false;

            if (laserMaterial == null)
            {
                // Respaldo (en el Editor). En builds, el material debe venir asignado.
                Shader sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
                if (sh != null) laserMaterial = new Material(sh);
            }
            if (laserMaterial != null) _laser.sharedMaterial = laserMaterial;
        }

        private void UpdateLaser()
        {
            if (_laser == null) return;
            bool show = showLaser && Held == null;
            _laser.enabled = show;
            if (!show) return;

            float len = HasHit ? LastHit.distance : rayLength;
            Color c = Hovered != null ? laserHitColor : laserColor;
            _laser.startColor = c;
            _laser.endColor = new Color(c.r, c.g, c.b, Hovered != null ? c.a : 0.05f);
            _laser.SetPosition(0, CurrentRay.origin);
            _laser.SetPosition(1, CurrentRay.origin + CurrentRay.direction * len);
        }
    }
}
