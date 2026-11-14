using UnityEngine;
using UnityEngine.UI;

namespace NovaCore
{
    /// <summary>
    /// Botón / consola de la estación. Al usarlo (gatillo o click) puede:
    ///   - CompleteAction: completar una acción de misión (p. ej. EmergencyActivated).
    ///   - StartMission:   iniciar la secuencia (MissionManager.StartMission).
    ///   - ResetMission:   reiniciar la secuencia (MissionManager.ResetMission).
    ///
    /// Si holdSeconds > 0 hay que MANTENER el gatillo ese tiempo (p. ej. despresurizar).
    /// Si requiredZone está asignado, el jugador debe estar dentro de esa zona.
    /// </summary>
    public class MissionButton : NovaInteractable, IMissionTarget
    {
        public enum Mode { CompleteAction, StartMission, ResetMission }

        [Header("Misión")]
        public Mode mode = Mode.CompleteAction;
        public MissionAction action = MissionAction.EmergencyActivated;

        [Tooltip("0 = pulsar. >0 = mantener pulsado estos segundos.")]
        public float holdSeconds = 0f;

        [Tooltip("Opcional: el jugador debe estar dentro de esta zona para usarlo")]
        public MissionZone requiredZone;

        [Header("Visual")]
        [Tooltip("Parte que se hunde al pulsar (opcional)")]
        public Transform pressPart;
        public float pressDepth = 0.012f;

        [Tooltip("Texto del panel (opcional). Muestra el progreso al mantener.")]
        public Text label;
        [TextArea] public string idleLabel = "";

        [Tooltip("Luz/indicador que se pone verde al completar (opcional)")]
        public Renderer statusLight;
        public Color pendingColor = new Color(1f, 0.55f, 0.1f);
        public Color doneColor = new Color(0.2f, 1f, 0.45f);

        private float _hold;
        private bool _holding;
        private Vector3 _pressRest;
        private MaterialPropertyBlock _lightBlock;
        private float _nextWarn;

        // ── IMissionTarget ───────────────────────────────────────────────────
        public MissionAction TargetAction => action;
        public Vector3 MarkerPosition => transform.position;
        public bool IsTargetAvailable => mode == Mode.CompleteAction && isActiveAndEnabled;

        protected override void OnEnable()
        {
            base.OnEnable();
            if (mode == Mode.CompleteAction) MissionGate.Register(this);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            MissionGate.Unregister(this);
        }

        protected override void Awake()
        {
            base.Awake();
            if (pressPart != null) _pressRest = pressPart.localPosition;
            if (label != null && string.IsNullOrEmpty(idleLabel)) idleLabel = label.text;
        }

        private void Start()
        {
            var m = MissionManager.Instance;
            if (m != null)
            {
                m.OnObjectiveChanged += OnObjectiveChanged;
            }
            RefreshVisual();
        }

        private void OnDestroy()
        {
            var m = MissionManager.Instance;
            if (m != null) m.OnObjectiveChanged -= OnObjectiveChanged;
        }

        private void OnObjectiveChanged(MissionState s, string o) => RefreshVisual();

        public override string GetPrompt()
        {
            if (holdSeconds > 0f) return prompt + " (mantener)";
            return prompt;
        }

        // ── Uso ──────────────────────────────────────────────────────────────
        public override void OnUsePressed(NovaInteractor interactor)
        {
            SetPressed(true);

            if (mode == Mode.StartMission)
            {
                var m = MissionGate.Manager;
                if (m == null) { MissionHUD.Feedback("No hay MissionManager en la escena.", MissionHUD.Tone.Warning); return; }
                if (m.CurrentState != MissionState.NotStarted)
                {
                    MissionHUD.Feedback("La secuencia ya está en marcha.", MissionHUD.Tone.Info);
                    return;
                }
                m.StartMission();
                MissionHUD.Feedback("Secuencia iniciada. ¡Alerta en la estación!", MissionHUD.Tone.Success);
                return;
            }

            if (mode == Mode.ResetMission)
            {
                var m = MissionGate.Manager;
                if (m == null) return;
                m.ResetMission();
                MissionHUD.Feedback("Secuencia reiniciada.", MissionHUD.Tone.Info);
                return;
            }

            if (!CheckZone()) return;

            if (holdSeconds <= 0f)
            {
                MissionGate.TryComplete(action);
                RefreshVisual();
                return;
            }

            // Modo mantener: solo arranca si es el objetivo actual (si no, avisa)
            if (!MissionGate.IsCurrent(action))
            {
                MissionGate.TryComplete(action);   // muestra el aviso adecuado
                return;
            }
            _holding = true;
            _hold = 0f;
        }

        public override void OnUseHeld(NovaInteractor interactor, float deltaTime)
        {
            if (!_holding) return;
            if (!CheckZone()) { _holding = false; _hold = 0f; RefreshVisual(); return; }

            _hold += deltaTime;
            float t = Mathf.Clamp01(_hold / holdSeconds);
            if (label != null) label.text = idleLabel + "\n" + NovaText.Bar(t, 16) + " " + Mathf.RoundToInt(t * 100f) + "%";
            if (interactor != null && interactor.IsVR) NovaXR.Haptic(interactor.Node, 0.1f + 0.3f * t, deltaTime);

            if (t >= 1f)
            {
                _holding = false;
                MissionGate.TryComplete(action);
                RefreshVisual();
            }
        }

        public override void OnUseReleased(NovaInteractor interactor)
        {
            SetPressed(false);
            if (_holding)
            {
                _holding = false;
                _hold = 0f;
                MissionHUD.Feedback("Soltaste antes de tiempo: mantén pulsado hasta el 100%.", MissionHUD.Tone.Warning);
                RefreshVisual();
            }
        }

        public override void OnHoverExit(NovaInteractor interactor)
        {
            base.OnHoverExit(interactor);
            SetPressed(false);
        }

        // ── Utilidades ───────────────────────────────────────────────────────
        private bool CheckZone()
        {
            if (requiredZone == null || requiredZone.PlayerInside) return true;
            if (Time.unscaledTime >= _nextWarn)
            {
                MissionHUD.Feedback("Tienes que estar dentro de: " + requiredZone.zoneName, MissionHUD.Tone.Warning);
                _nextWarn = Time.unscaledTime + 1.5f;
            }
            return false;
        }

        private void SetPressed(bool down)
        {
            if (pressPart == null) return;
            pressPart.localPosition = _pressRest + (down ? Vector3.back * pressDepth : Vector3.zero);
        }

        private void RefreshVisual()
        {
            if (label != null) label.text = idleLabel;

            if (statusLight != null && mode == Mode.CompleteAction)
            {
                if (_lightBlock == null) _lightBlock = new MaterialPropertyBlock();
                Color c = MissionGate.IsDone(action) ? doneColor : pendingColor;
                statusLight.GetPropertyBlock(_lightBlock);
                _lightBlock.SetColor("_BaseColor", c);
                _lightBlock.SetColor("_EmissionColor", c * 2f);
                statusLight.SetPropertyBlock(_lightBlock);
            }
        }
    }
}
