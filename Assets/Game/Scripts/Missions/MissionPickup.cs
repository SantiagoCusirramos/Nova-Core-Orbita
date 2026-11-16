using UnityEngine;

namespace NovaCore
{
    /// <summary>
    /// Objeto que se puede agarrar y llevar (p. ej. el módulo energético de repuesto).
    ///   - completeOnGrab: al agarrarlo completa grabAction (EnergyModuleCollected).
    ///   - Al soltarlo cerca de un MissionDropZone que lo acepte, se encaja y completa
    ///     la acción de esa zona (EnergyModuleLoadedIntoHatch).
    /// Si se suelta en otro sitio se queda flotando (gravedad cero).
    /// El estado se restaura al reiniciar la misión (RF-05).
    /// </summary>
    public class MissionPickup : NovaInteractable, IMissionTarget
    {
        [Header("Pieza")]
        public string itemId = "energy_module";

        [Header("Misión al agarrar")]
        public bool completeOnGrab = true;
        public MissionAction grabAction = MissionAction.EnergyModuleCollected;

        [Tooltip("Si está activo, no deja agarrarlo hasta que su objetivo (o uno posterior) esté activo")]
        public bool lockUntilObjective = true;

        [Tooltip("Al agarrarlo se guarda en la bolsa del astronauta (desaparece). Útil cuando el destino está en otra escena.")]
        public bool stowOnGrab = false;
        [TextArea] public string stowMessage = "Guardado en tu bolsa de transporte.";

        [Header("Llevarlo en la mano entre módulos")]
        [Tooltip("Una vez recogido, viaja contigo en la mano a cada módulo hasta entregarlo")]
        public bool carryAcrossScenes = false;
        [Tooltip("Acción que lo entrega (después de ella ya no viaja contigo)")]
        public MissionAction deliveredAction = MissionAction.EnergyModuleLoadedIntoHatch;
        [Tooltip("Copia en otros módulos: solo aparece (en tu mano) si lo llevas")]
        public bool onlyWhenCarried = false;

        /// <summary>Mano con la que se llevaba (para volver a ponerlo en la misma al cambiar de módulo).</summary>
        public static NovaInteractor.Source CarryHand = NovaInteractor.Source.RightHand;

        [Header("Sujeción")]
        public Vector3 holdOffset = new Vector3(0f, 0f, 0.12f);
        public Vector3 holdEuler = Vector3.zero;

        public bool IsSocketed { get; private set; }

        private Vector3 _startPos;
        private Quaternion _startRot;
        private Transform _startParent;
        private Collider[] _colliders;
        private float _nextWarn;

        // ── IMissionTarget ───────────────────────────────────────────────────
        public MissionAction TargetAction => grabAction;
        public Vector3 MarkerPosition => transform.position;
        public bool IsTargetAvailable => completeOnGrab && isActiveAndEnabled && !IsSocketed;

        public override bool CanGrab
        {
            get
            {
                if (IsSocketed) return false;
                if (!lockUntilObjective || !completeOnGrab) return true;
                return MissionGate.IsCurrent(grabAction) || MissionGate.IsDone(grabAction);
            }
        }

        public override Vector3 HoldLocalPosition => holdOffset;
        public override Quaternion HoldLocalRotation => Quaternion.Euler(holdEuler);

        protected override void Awake()
        {
            base.Awake();
            _startParent = transform.parent;
            _startPos = transform.localPosition;
            _startRot = transform.localRotation;
            _colliders = GetComponentsInChildren<Collider>(true);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (completeOnGrab) MissionGate.Register(this);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            MissionGate.Unregister(this);
        }

        private void Start()
        {
            var m = MissionManager.Instance;
            if (m != null) m.OnMissionReset += ResetPickup;

            // Si ya se recogió (venimos de otra escena), no debe estar aquí
            if (stowOnGrab && completeOnGrab && MissionGate.IsDone(grabAction))
                gameObject.SetActive(false);

            if (carryAcrossScenes)
            {
                bool collected = MissionGate.IsDone(grabAction);
                bool delivered = MissionGate.IsDone(deliveredAction);
                if (delivered) gameObject.SetActive(false);                       // ya está instalado
                else if (collected) StartCoroutine(AttachToHand());                // lo traes en la mano
                else if (onlyWhenCarried) gameObject.SetActive(false);             // copia: aún no lo tienes
            }
        }

        /// <summary>Al entrar a un módulo llevando la pieza, aparece en tu mano.</summary>
        private System.Collections.IEnumerator AttachToHand()
        {
            float until = Time.time + 6f;
            while (Time.time < until)
            {
                var rig = FindAnyObjectByType<NovaInteractionRig>();
                NovaInteractor it = null;
                if (rig != null)
                {
                    if (NovaXR.IsActive)
                        it = CarryHand == NovaInteractor.Source.LeftHand ? (rig.Left ?? rig.Right) : (rig.Right ?? rig.Left);
                    else it = rig.Desktop;
                }
                if (it != null && it.isActiveAndEnabled && it.Held == null)
                {
                    yield return null;   // un frame para que el interactor termine su Start
                    it.Grab(this);
                    MissionHUD.Feedback("Llevas el módulo de repuesto en la mano.", MissionHUD.Tone.Info, 2.5f);
                    yield break;
                }
                yield return null;
            }
        }

        private void OnDestroy()
        {
            var m = MissionManager.Instance;
            if (m != null) m.OnMissionReset -= ResetPickup;
        }

        public override string GetPrompt() => IsSocketed ? prompt + " (instalado)" : prompt;

        public override void OnUsePressed(NovaInteractor interactor)
        {
            // Usar sin agarrar: explicar cómo se coge
            if (!CanGrab && !IsSocketed && Time.unscaledTime >= _nextWarn)
            {
                MissionGate.TryComplete(grabAction);   // muestra qué toca ahora
                _nextWarn = Time.unscaledTime + 1.5f;
            }
            else if (CanGrab)
            {
                MissionHUD.Feedback(interactor.IsVR ? "Mantén el GRIP para agarrarlo." : "Pulsa F para agarrarlo.");
            }
        }

        public override void OnGrabDenied(NovaInteractor interactor)
        {
            if (IsSocketed) { MissionHUD.Feedback("La pieza ya está instalada."); return; }
            MissionGate.TryComplete(grabAction);   // explica qué toca ahora
        }

        public override void OnGrab(NovaInteractor interactor)
        {
            CarryHand = interactor.source;
            SetCollidersEnabled(false);
            bool done = false;
            if (completeOnGrab && MissionGate.IsCurrent(grabAction))
                done = MissionGate.TryComplete(grabAction);

            if (stowOnGrab && done)
            {
                interactor.ForceRelease();
                transform.SetParent(_startParent, false);
                gameObject.SetActive(false);
                if (!string.IsNullOrEmpty(stowMessage)) MissionHUD.Feedback(stowMessage, MissionHUD.Tone.Success, 4f);
            }
        }

        public override void OnRelease(NovaInteractor interactor)
        {
            SetCollidersEnabled(true);

            var zone = MissionDropZone.FindBestFor(this);
            if (zone != null) zone.TryAccept(this);
        }

        /// <summary>Lo llama el MissionDropZone al aceptar la pieza.</summary>
        public void SnapInto(Transform socket)
        {
            if (Holder != null) Holder.ForceRelease();
            IsSocketed = true;
            var zg = GetComponent<ZeroGBody>();
            if (zg != null) zg.Freeze();
            transform.SetParent(socket, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }

        public void ResetPickup()
        {
            if (Holder != null) Holder.ForceRelease();
            if (onlyWhenCarried) { gameObject.SetActive(false); return; }
            if (!gameObject.activeSelf) gameObject.SetActive(true);
            IsSocketed = false;
            var zgb = GetComponent<ZeroGBody>();
            if (zgb != null) zgb.Freeze();
            transform.SetParent(_startParent, false);
            transform.localPosition = _startPos;
            transform.localRotation = _startRot;
            SetCollidersEnabled(true);
        }

        private void SetCollidersEnabled(bool on)
        {
            if (_colliders == null) return;
            foreach (var c in _colliders) if (c != null) c.enabled = on;
        }
    }
}
