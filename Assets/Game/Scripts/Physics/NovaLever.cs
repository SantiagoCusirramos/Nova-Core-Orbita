using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR;

namespace NovaCore
{
    /// <summary>
    /// Palanca física: se baja con la mano y, al llegar al final del recorrido, ejecuta
    /// su acción (completar un objetivo de misión y/o un evento).
    ///   VR: agarrar el pomo con el grip (de cerca) y moverla, o apuntar y mantener el gatillo.
    ///   Escritorio: apuntar y mantener clic izquierdo (baja poco a poco).
    /// Si se suelta antes del final, vuelve sola arriba.
    /// </summary>
    public class NovaLever : NovaInteractable, IMissionTarget
    {
        // Para que la guía y el rombo del HUD encuentren la palanca
        public MissionAction TargetAction => action;
        public Vector3 MarkerPosition => transform.position;
        public bool IsTargetAvailable => completeMissionAction && !Activated && isActiveAndEnabled;
        protected override void OnEnable()  { base.OnEnable();  MissionGate.Register(this); }
        protected override void OnDisable() { base.OnDisable(); MissionGate.Unregister(this); }

        [Header("Partes")]
        [Tooltip("Pivote que gira (eje X local)")]
        public Transform pivot;
        [Tooltip("Ángulo arriba (reposo) y abajo (activada), en grados sobre X local")]
        public float upAngle = -35f;
        public float downAngle = 45f;
        [Range(0.5f, 1f)] public float activateAt = 0.9f;
        public float desktopSpeed = 0.8f;   // recorrido por segundo con clic mantenido
        public float returnSpeed = 1.5f;

        [Header("Acción")]
        public bool completeMissionAction = true;
        public MissionAction action = MissionAction.AirlockDepressurized;
        public MissionZone requiredZone;
        public UnityEvent onActivated;
        public Renderer statusLight;

        public bool Activated { get; private set; }

        /// <summary>La mano la empuja al tocarla (sin gatillo).</summary>
        public override bool AllowPoke => true;

        private float _t;           // 0 = arriba, 1 = abajo
        private bool _dragging;
        private NovaInteractor _dragger;

        public override string GetPrompt()
        {
            if (Activated) return prompt + " (hecho)";
            return prompt + "  " + Mathf.RoundToInt(_t * 100f) + "%  (" +
                   (NovaXR.IsActive ? "agarra el pomo y bájala" : "mantén clic") + ")";
        }

        private void Start()
        {
            if (completeMissionAction && MissionGate.IsDone(action)) SetActivated(true);
            Apply();
        }

        // Láser + gatillo (o clic en escritorio)
        public override void OnUseHeld(NovaInteractor interactor, float dt)
        {
            if (Activated) return;
            if (interactor.IsVR) DragTo(interactor.transform.position);
            else _t = Mathf.MoveTowards(_t, 1f, dt * desktopSpeed);
            _dragging = true;
            Check(interactor);
        }

        public override void OnUseReleased(NovaInteractor interactor) { _dragging = false; }

        // Agarre directo con la mano (lo llama NovaInteractor)
        public void BeginDrag(NovaInteractor it) { _dragger = it; _dragging = true; NovaXR.Haptic(it.Node, 0.3f, 0.05f); }
        public void EndDrag(NovaInteractor it) { if (_dragger == it) { _dragger = null; _dragging = false; } }

        private void Update()
        {
            if (_dragger != null && !Activated) { DragTo(_dragger.transform.position); Check(_dragger); }
            if (!_dragging && !Activated && _t > 0f) _t = Mathf.MoveTowards(_t, 0f, Time.deltaTime * returnSpeed);
            Apply();
        }

        /// <summary>Ángulo de la palanca según dónde está la mano respecto al pivote.</summary>
        private void DragTo(Vector3 handWorld)
        {
            if (pivot == null) return;
            Transform space = pivot.parent != null ? pivot.parent : pivot;
            Vector3 local = space.InverseTransformPoint(handWorld) - space.InverseTransformPoint(pivot.position);
            // Ángulo en el plano YZ (giro sobre X): 0° = recta hacia arriba
            float ang = Mathf.Atan2(local.z, local.y) * Mathf.Rad2Deg;
            _t = Mathf.Clamp01(Mathf.InverseLerp(upAngle, downAngle, ang));
        }

        private void Check(NovaInteractor it)
        {
            if (Activated || _t < activateAt) return;
            if (completeMissionAction)
            {
                if (requiredZone != null && !requiredZone.PlayerInside)
                {
                    MissionHUD.Feedback("Tienes que estar dentro de " + requiredZone.zoneName + ".", MissionHUD.Tone.Warning);
                    _t = activateAt - 0.15f;
                    return;
                }
                if (!MissionGate.TryComplete(action)) { _t = activateAt - 0.15f; return; }
            }
            SetActivated(true);
            onActivated?.Invoke();
            if (it != null && it.IsVR) NovaXR.Haptic(it.Node, 0.8f, 0.15f);
        }

        private void SetActivated(bool on)
        {
            Activated = on;
            if (on) _t = 1f;
            if (statusLight != null)
            {
                var mpb = new MaterialPropertyBlock();
                statusLight.GetPropertyBlock(mpb);
                Color c = on ? new Color(0.2f, 1f, 0.45f) : new Color(1f, 0.55f, 0.1f);
                mpb.SetColor("_BaseColor", c);
                mpb.SetColor("_EmissionColor", c * 2f);
                statusLight.SetPropertyBlock(mpb);
            }
        }

        private void Apply()
        {
            if (pivot != null) pivot.localRotation = Quaternion.Euler(Mathf.Lerp(upAngle, downAngle, _t), 0f, 0f);
        }
    }
}
