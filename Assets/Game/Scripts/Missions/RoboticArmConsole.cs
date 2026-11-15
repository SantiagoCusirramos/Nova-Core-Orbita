using UnityEngine;
using UnityEngine.UI;

namespace NovaCore
{
    /// <summary>
    /// Minijuego robótico de la Misión 1 (energy_05 → EnergyModuleInstalled).
    /// El jugador MANTIENE el gatillo / click sobre el joystick de la consola y el brazo
    /// telescópico de la bahía exterior lleva el módulo desde la compuerta hasta el
    /// zócalo averiado. Si suelta, el brazo se detiene (el progreso se conserva).
    ///
    /// Se ve a través de la ventana del módulo de servicio. Sin XRI ni física.
    /// </summary>
    public class RoboticArmConsole : NovaInteractable, IMissionTarget
    {
        [Header("Misión")]
        public MissionAction action = MissionAction.EnergyModuleInstalled;
        public float duration = 7f;

        [Header("Brazo")]
        [Tooltip("Base giratoria del brazo")]
        public Transform armBase;
        [Tooltip("Segmento telescópico (cilindro con pivote en la base, eje +Y)")]
        public Transform armSegment;
        [Tooltip("Pinza del extremo")]
        public Transform gripper;
        [Tooltip("Módulo que lleva la pinza (se activa al cargar la compuerta)")]
        public Transform carriedModule;

        [Header("Recorrido")]
        public Transform pickPoint;     // salida de la compuerta en la bahía
        public Transform installPoint;  // zócalo averiado
        public float arcHeight = 0.8f;

        [Header("Indicadores")]
        public Text screen;
        public Renderer socketLight;
        public Joystick joystick = new Joystick();

        [System.Serializable]
        public class Joystick
        {
            public Transform stick;
            public float tiltDegrees = 18f;
        }

        public float Progress { get; private set; }

        private bool _done;
        private bool _driving;
        private float _nextWarn;
        private MaterialPropertyBlock _mpb;

        public MissionAction TargetAction => action;
        public Vector3 MarkerPosition => transform.position;
        public bool IsTargetAvailable => isActiveAndEnabled && !_done;

        protected override void OnEnable()  { base.OnEnable(); MissionGate.Register(this); }
        protected override void OnDisable() { base.OnDisable(); MissionGate.Unregister(this); }

        private void Start()
        {
            var m = MissionManager.Instance;
            if (m != null) m.OnMissionReset += ResetArm;
            ResetArm();

            // Venimos de otra escena con el módulo ya instalado
            if (MissionGate.IsDone(action))
            {
                _done = true;
                Progress = 1f;
                ApplyPose(1f);
                SetSocketColor(new Color(0.2f, 1f, 0.45f));
                UpdateScreen();
            }
        }

        private void OnDestroy()
        {
            var m = MissionManager.Instance;
            if (m != null) m.OnMissionReset -= ResetArm;
        }

        public override string GetPrompt() => _done ? "Brazo robótico (módulo instalado)" : prompt;

        public override void OnUsePressed(NovaInteractor interactor)
        {
            if (_done) { MissionHUD.Feedback("El módulo ya está instalado. Confirma la reparación.", MissionHUD.Tone.Info); return; }
            if (!MissionGate.IsCurrent(action))
            {
                MissionGate.TryComplete(action);   // explica qué toca ahora
                return;
            }
            _driving = true;
        }

        public override void OnUseHeld(NovaInteractor interactor, float deltaTime)
        {
            if (!_driving || _done) return;

            Progress = Mathf.Clamp01(Progress + deltaTime / Mathf.Max(0.1f, duration));
            ApplyPose(Progress);
            TiltStick(true);
            if (interactor != null && interactor.IsVR) NovaXR.Haptic(interactor.Node, 0.15f, deltaTime);

            if (Progress >= 1f)
            {
                _driving = false;
                if (MissionGate.TryComplete(action))
                {
                    _done = true;
                    SetSocketColor(new Color(0.2f, 1f, 0.45f));
                    MissionHUD.Feedback("Módulo energético instalado por el brazo robótico.", MissionHUD.Tone.Success);
                }
            }
            UpdateScreen();
        }

        public override void OnUseReleased(NovaInteractor interactor)
        {
            _driving = false;
            TiltStick(false);
            if (!_done && Progress > 0f && Progress < 1f && Time.unscaledTime >= _nextWarn)
            {
                MissionHUD.Feedback("Brazo detenido. Mantén pulsado para continuar.", MissionHUD.Tone.Info);
                _nextWarn = Time.unscaledTime + 2f;
            }
            UpdateScreen();
        }

        private void Update()
        {
            // Mostrar el módulo en la pinza solo cuando ya se cargó en la compuerta
            if (carriedModule != null)
            {
                bool loaded = MissionGate.IsDone(MissionAction.EnergyModuleLoadedIntoHatch);
                if (carriedModule.gameObject.activeSelf != loaded) carriedModule.gameObject.SetActive(loaded);
            }
            if (!_driving) UpdateScreen();
        }

        // ── Pose del brazo ───────────────────────────────────────────────────
        private void ApplyPose(float t)
        {
            if (pickPoint == null || installPoint == null) return;

            // Curva en arco entre la compuerta y el zócalo
            Vector3 a = pickPoint.position, b = installPoint.position;
            Vector3 mid = (a + b) * 0.5f + Vector3.up * arcHeight;
            float s = Mathf.SmoothStep(0f, 1f, t);
            Vector3 p = (1 - s) * (1 - s) * a + 2 * (1 - s) * s * mid + s * s * b;

            if (gripper != null) gripper.position = p;

            if (armBase != null && armSegment != null)
            {
                Vector3 from = armBase.position;
                Vector3 dir = p - from;
                float len = dir.magnitude;
                if (len > 0.001f)
                {
                    armSegment.position = from;
                    armSegment.rotation = Quaternion.FromToRotation(Vector3.up, dir / len);
                    Vector3 sc = armSegment.localScale;
                    sc.y = len;      // el hijo visual mide 1 m sobre +Y
                    armSegment.localScale = sc;
                }
            }

            if (gripper != null && installPoint != null)
                gripper.rotation = Quaternion.Slerp(pickPoint.rotation, installPoint.rotation, s);
        }

        private void TiltStick(bool on)
        {
            if (joystick == null || joystick.stick == null) return;
            joystick.stick.localRotation = on ? Quaternion.Euler(joystick.tiltDegrees, 0f, 0f) : Quaternion.identity;
        }

        private void UpdateScreen()
        {
            if (screen == null) return;
            string state;
            if (_done) state = "<color=#6CFF9A>MÓDULO INSTALADO — SISTEMA NOMINAL</color>";
            else if (!MissionGate.IsDone(MissionAction.RoboticConsoleActivated))
                state = "<color=#FF6A4D>CONSOLA INACTIVA</color>";
            else state = _driving ? "<color=#9FDCFF>MOVIENDO BRAZO...</color>" : "<color=#FFC14D>MANTÉN EL JOYSTICK PARA MOVER</color>";

            screen.text = "BRAZO ROBÓTICO — BAHÍA DE ENERGÍA\n" + state + "\n" +
                          NovaText.Bar(Progress, 20) + " " + Mathf.RoundToInt(Progress * 100f) + "%";
        }

        private void SetSocketColor(Color c)
        {
            if (socketLight == null) return;
            if (_mpb == null) _mpb = new MaterialPropertyBlock();
            socketLight.GetPropertyBlock(_mpb);
            _mpb.SetColor("_BaseColor", c);
            _mpb.SetColor("_EmissionColor", c * 2.5f);
            socketLight.SetPropertyBlock(_mpb);
        }

        private void ResetArm()
        {
            _done = false;
            _driving = false;
            Progress = 0f;
            ApplyPose(0f);
            TiltStick(false);
            SetSocketColor(new Color(1f, 0.2f, 0.1f));
            UpdateScreen();
        }
    }
}
