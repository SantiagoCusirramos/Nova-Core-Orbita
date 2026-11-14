using System.Collections;
using UnityEngine;

namespace NovaCore
{
    public enum SpaceToolType
    {
        PistolGripTool,
        Hammer,
        Ratchet,
        GreaseGun
    }

    /// <summary>
    /// Herramienta EVA — PORTADA de NASA-SPACE-APPS (SpaceToolInteractable) SIN XR Interaction
    /// Toolkit (decisión cerrada de Nova, HANDOFF §7, «Opción A»).
    ///
    ///   Agarrar: grip (VR) / F (escritorio).
    ///   Usar:    gatillo (VR) / click izquierdo (escritorio), apuntando a la mesa.
    ///
    ///   PGT (Pistol Grip Tool): mantener → soldar; cuando la soldadura termina pasa solo a
    ///                           modo taladro (mantener recto frente al perno).
    ///   Martillo:  pulsar → golpe (3 golpes enderezan la placa).
    ///   Carraca:   pulsar → una carrera de apriete sobre el perno apuntado.
    ///   Pistola de grasa: mantener → inyectar sellador hasta 1.01 bar.
    ///
    /// Al soltarla cerca de su soporte (dock) vuelve a él; si no, flota. Al reiniciar la
    /// misión vuelve al soporte.
    /// </summary>
    public class SpaceTool : NovaInteractable
    {
        [Header("Herramienta")]
        public SpaceToolType toolType = SpaceToolType.PistolGripTool;
        [Tooltip("Alcance desde el mando/cámara hasta la mesa")]
        public float useRange = 2.2f;

        [Header("Soporte")]
        public Transform dockPoint;
        public float dockSnapRadius = 0.5f;

        [Header("Sujeción")]
        public Vector3 vrHoldPosition = new Vector3(0f, -0.01f, 0.04f);
        public Vector3 vrHoldEuler = new Vector3(0f, 0f, 0f);
        public Vector3 desktopHoldEuler = new Vector3(8f, -12f, 0f);

        [Header("Efectos (opcionales)")]
        public ParticleSystem effectParticles;
        public Light toolLight;

        private AudioSource _audio;
        private bool _busy;
        private float _feedbackCooldown;
        private Vector3 _restLocalPos;
        private Quaternion _restLocalRot;

        private static AudioClip s_Buzz, s_Clang, s_Click, s_Hiss;

        public override bool CanGrab => true;

        public override Vector3 HoldLocalPosition => Holder != null && Holder.IsVR ? vrHoldPosition : Vector3.zero;
        public override Quaternion HoldLocalRotation =>
            Quaternion.Euler(Holder != null && Holder.IsVR ? vrHoldEuler : desktopHoldEuler);

        public static string DisplayName(SpaceToolType t)
        {
            switch (t)
            {
                case SpaceToolType.PistolGripTool: return "Pistol Grip Tool (PGT)";
                case SpaceToolType.Hammer:         return "Martillo EVA";
                case SpaceToolType.Ratchet:        return "Llave de carraca";
                case SpaceToolType.GreaseGun:      return "Pistola de sellador";
                default:                           return t.ToString();
            }
        }

        public override string GetPrompt() => DisplayName(toolType);

        protected override void Awake()
        {
            base.Awake();
            if (string.IsNullOrEmpty(prompt) || prompt == "Usar") prompt = DisplayName(toolType);

            _audio = GetComponent<AudioSource>();
            if (_audio == null) _audio = gameObject.AddComponent<AudioSource>();
            _audio.playOnAwake = false;
            _audio.spatialBlend = 1f;
            _audio.minDistance = 0.5f;
            _audio.maxDistance = 8f;

            BuildClips();
        }

        private void Start()
        {
            var m = MissionManager.Instance;
            if (m != null) m.OnMissionReset += ReturnToDock;
            if (toolLight != null) toolLight.enabled = false;
        }

        private void OnDestroy()
        {
            var m = MissionManager.Instance;
            if (m != null) m.OnMissionReset -= ReturnToDock;
        }

        // ── Agarre ───────────────────────────────────────────────────────────
        public override void OnGrab(NovaInteractor interactor)
        {
            // Recolocar con la pose correcta ahora que Holder ya está asignado
            transform.localPosition = HoldLocalPosition;
            transform.localRotation = HoldLocalRotation;
            _restLocalPos = transform.localPosition;
            _restLocalRot = transform.localRotation;
            SetColliders(false);
        }

        public override void OnRelease(NovaInteractor interactor)
        {
            StopEffects();
            StopAllCoroutines();
            _busy = false;
            SetColliders(true);

            if (dockPoint != null && Vector3.Distance(transform.position, dockPoint.position) <= dockSnapRadius)
                ReturnToDock();
        }

        public void ReturnToDock()
        {
            if (Holder != null) Holder.ForceRelease();
            if (dockPoint == null) return;
            transform.SetParent(dockPoint, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }

        private void SetColliders(bool on)
        {
            foreach (var c in GetComponentsInChildren<Collider>(true)) c.enabled = on;
        }

        // ── Uso en la mano ───────────────────────────────────────────────────
        public override void OnHeldUsePressed(NovaInteractor interactor)
        {
            switch (toolType)
            {
                case SpaceToolType.Hammer:
                    if (!_busy) StartCoroutine(HammerStrike(interactor));
                    break;
                case SpaceToolType.Ratchet:
                    if (!_busy) RatchetStroke(interactor);
                    break;
            }
        }

        public override void OnHeldUse(NovaInteractor interactor, float dt)
        {
            switch (toolType)
            {
                case SpaceToolType.PistolGripTool: PistolGrip(interactor, dt); break;
                case SpaceToolType.GreaseGun:      Sealant(interactor, dt);    break;
            }
        }

        public override void OnHeldUseReleased(NovaInteractor interactor)
        {
            StopEffects();
            if (!_busy)
            {
                transform.localPosition = _restLocalPos;
                transform.localRotation = _restLocalRot;
            }
        }

        // ── Lógica por herramienta (portada) ─────────────────────────────────
        private SpaceRepairStation StationUnderRay(NovaInteractor it, RepairStationType wanted, out RaycastHit hit)
        {
            hit = it.LastHit;
            if (!it.HasHit || hit.distance > useRange) return null;
            var st = hit.collider.GetComponentInParent<SpaceRepairStation>();
            if (st == null) return null;
            if (st.stationType != wanted)
            {
                Warn("Esta mesa necesita: " + DisplayName(st.requiredTool));
                return null;
            }
            return st;
        }

        private void PistolGrip(NovaInteractor it, float dt)
        {
            Vibrate(0.003f);
            if (toolLight != null)
            {
                toolLight.enabled = true;
                toolLight.intensity = 2f + Mathf.Sin(Time.time * 60f) * 0.8f;
            }
            Play(s_Buzz, 0.5f, 1.4f, loop: true);

            var st = StationUnderRay(it, RepairStationType.PistolGrip_WelderAndDrill, out RaycastHit hit);
            if (st == null) { StopParticlesOnly(); return; }

            PlaceParticles(hit);
            if (!st.isTask1Complete)
            {
                st.TryApplyWelder(hit.point, it.CurrentRay.direction, dt);
                st.lastFeedback = "<color=#00E5FF>SOLDANDO PLACAS: " + Mathf.RoundToInt(st.task1Progress * 100f) + "%</color>";
            }
            else if (!st.isTask2Complete)
            {
                string fb;
                st.TryApplyDrill(hit.point, it.CurrentRay.direction, dt, out fb);
                st.lastFeedback = fb;
            }
            if (it.IsVR) NovaXR.Haptic(it.Node, 0.25f, dt);
        }

        private void Sealant(NovaInteractor it, float dt)
        {
            Vibrate(0.002f);
            Play(s_Hiss, 0.5f, 1f, loop: true);

            var st = StationUnderRay(it, RepairStationType.GreaseGun_PressureSealant, out RaycastHit hit);
            if (st == null) { StopParticlesOnly(); return; }

            PlaceParticles(hit);
            st.TryApplySealant(hit.point, dt);
            st.lastFeedback = "<color=#FFE066>INYECTANDO SELLADOR | PRESIÓN: " +
                              st.currentChamberPressure.ToString("F2") + " / 1.01 bar</color>";
            if (it.IsVR) NovaXR.Haptic(it.Node, 0.2f, dt);
        }

        private IEnumerator HammerStrike(NovaInteractor it)
        {
            _busy = true;
            Quaternion r0 = transform.localRotation;
            Vector3 p0 = transform.localPosition;
            Quaternion up = r0 * Quaternion.Euler(-35f, 0f, 0f);
            Quaternion down = r0 * Quaternion.Euler(40f, 0f, 0f);

            yield return Animate(r0, up, p0, p0 + new Vector3(0f, 0.04f, -0.04f), 0.08f);
            yield return Animate(up, down, transform.localPosition, p0 + new Vector3(0f, -0.04f, 0.08f), 0.07f);

            var st = StationUnderRay(it, RepairStationType.Hammer_ArmorStraightening, out RaycastHit hit);
            if (st != null)
            {
                st.TryApplyHammer(hit.point, 1f);
                st.lastFeedback = "<color=#00FF88>¡GOLPE! BLINDAJE " + Mathf.RoundToInt(st.task1Progress * 100f) + "%</color>";
            }
            else if (it.HasHit && it.LastHit.distance <= useRange)
            {
                Play(s_Clang, 0.8f, Random.Range(0.95f, 1.05f));
            }
            if (it.IsVR) NovaXR.Haptic(it.Node, 0.9f, 0.12f);

            yield return Animate(down, r0, transform.localPosition, p0, 0.12f);
            _busy = false;
        }

        private void RatchetStroke(NovaInteractor it)
        {
            Play(s_Click, 0.9f, Random.Range(0.98f, 1.05f));
            StartCoroutine(Twist(0.14f));

            var st = StationUnderRay(it, RepairStationType.Ratchet_FlangeBolts, out RaycastHit hit);
            if (st != null)
            {
                st.TryApplyRatchet(hit.collider.transform, 1);
                st.lastFeedback = "<color=#00E5FF>APRIETE DE BRIDA: " + Mathf.RoundToInt(st.task1Progress * 100f) + "%</color>";
            }
            if (it.IsVR) NovaXR.Haptic(it.Node, 0.6f, 0.05f);
        }

        // ── Animaciones / efectos ────────────────────────────────────────────
        private IEnumerator Animate(Quaternion ra, Quaternion rb, Vector3 pa, Vector3 pb, float t)
        {
            float e = 0f;
            while (e < t)
            {
                float k = e / t;
                transform.localRotation = Quaternion.Slerp(ra, rb, k);
                transform.localPosition = Vector3.Lerp(pa, pb, k);
                e += Time.deltaTime;
                yield return null;
            }
            transform.localRotation = rb;
            transform.localPosition = pb;
        }

        private IEnumerator Twist(float t)
        {
            _busy = true;
            Quaternion r0 = transform.localRotation;
            Quaternion r1 = r0 * Quaternion.Euler(0f, 0f, 35f);
            yield return Animate(r0, r1, transform.localPosition, transform.localPosition, t * 0.5f);
            yield return Animate(r1, r0, transform.localPosition, transform.localPosition, t * 0.5f);
            _busy = false;
        }

        private void Vibrate(float amount)
        {
            transform.localPosition = _restLocalPos + Random.insideUnitSphere * amount;
        }

        private void PlaceParticles(RaycastHit hit)
        {
            if (effectParticles == null) return;
            effectParticles.transform.position = hit.point;
            effectParticles.transform.forward = hit.normal;
            if (!effectParticles.isPlaying) effectParticles.Play();
        }

        private void StopParticlesOnly()
        {
            if (effectParticles != null && effectParticles.isPlaying) effectParticles.Stop();
        }

        private void StopEffects()
        {
            StopParticlesOnly();
            if (toolLight != null) toolLight.enabled = false;
            if (_audio != null && _audio.loop) { _audio.loop = false; _audio.Stop(); }
        }

        private void Warn(string msg)
        {
            if (Time.unscaledTime < _feedbackCooldown) return;
            _feedbackCooldown = Time.unscaledTime + 2f;
            MissionHUD.Feedback(msg, MissionHUD.Tone.Warning);
        }

        private void Play(AudioClip clip, float vol, float pitch, bool loop = false)
        {
            if (_audio == null || clip == null) return;
            if (loop)
            {
                if (_audio.isPlaying && _audio.clip == clip) return;
                _audio.clip = clip;
                _audio.loop = true;
                _audio.volume = vol;
                _audio.pitch = pitch;
                _audio.Play();
            }
            else
            {
                _audio.pitch = pitch;
                _audio.PlayOneShot(clip, vol);
            }
        }

        // ── Audio procedural (como en el original: sin assets de audio) ──────
        private static void BuildClips()
        {
            if (s_Buzz == null)  s_Buzz  = MakeClip("Tool_Buzz", 0.5f, (t) => Mathf.Sign(Mathf.Sin(2f * Mathf.PI * 180f * t)) * 0.15f + Mathf.Sin(2f * Mathf.PI * 360f * t) * 0.1f);
            if (s_Clang == null) s_Clang = MakeClip("Tool_Clang", 0.35f, (t) => (Mathf.Sin(2f * Mathf.PI * 440f * t) + Mathf.Sin(2f * Mathf.PI * 880f * t) * 0.5f) * Mathf.Exp(-t * 14f) * 0.6f);
            if (s_Click == null) s_Click = MakeClip("Tool_Click", 0.08f, (t) => (Random.value * 2f - 1f) * Mathf.Exp(-t * 60f) * 0.8f);
            if (s_Hiss == null)  s_Hiss  = MakeClip("Tool_Hiss", 0.6f, (t) => (Random.value * 2f - 1f) * 0.25f);
        }

        private static AudioClip MakeClip(string name, float seconds, System.Func<float, float> f)
        {
            const int rate = 22050;
            int n = Mathf.Max(1, Mathf.FloorToInt(rate * seconds));
            var data = new float[n];
            for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(f((float)i / rate), -1f, 1f);
            var clip = AudioClip.Create(name, n, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
