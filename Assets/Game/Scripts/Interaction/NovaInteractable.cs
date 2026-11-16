using System.Collections.Generic;
using UnityEngine;

namespace NovaCore
{
    /// <summary>
    /// Base de todo objeto con el que se puede interactuar SIN XR Interaction Toolkit.
    /// El NovaInteractor (mano VR o centro de pantalla) lanza un rayo; si choca con un
    /// collider cuyo padre tiene un NovaInteractable, le llama a estos métodos:
    ///
    ///   Hover   → OnHoverEnter / OnHoverExit   (resalta el objeto)
    ///   Usar    → OnUsePressed / OnUseHeld / OnUseReleased   (gatillo / click izquierdo)
    ///   Agarrar → OnGrab / OnRelease           (grip / tecla F) — solo si CanGrab
    ///
    /// Requiere al menos un Collider (no trigger) en el objeto o en sus hijos.
    /// </summary>
    public class NovaInteractable : MonoBehaviour
    {
        [Header("Interacción")]
        [Tooltip("Texto que se muestra al apuntar al objeto")]
        public string prompt = "Usar";

        [Tooltip("Distancia máxima desde la que se puede usar")]
        public float maxDistance = 3.0f;

        [Tooltip("Color de resaltado al apuntar")]
        public Color hoverColor = new Color(0.25f, 0.8f, 1f);

        [Tooltip("Renderers que se resaltan al apuntar. Vacío = todos los hijos.")]
        public Renderer[] highlightRenderers;

        public bool IsHovered => _hoverCount > 0;
        public NovaInteractor Holder { get; private set; }
        public bool IsHeld => Holder != null;

        private int _hoverCount;
        private MaterialPropertyBlock _mpb;
        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

        // ── Registro global (para depuración / búsquedas) ────────────────────
        public static readonly HashSet<NovaInteractable> All = new HashSet<NovaInteractable>();

        protected virtual void OnEnable()  { All.Add(this); }
        protected virtual void OnDisable()
        {
            All.Remove(this);
            // No se puede cambiar la jerarquía mientras Unity desactiva el objeto:
            // el interactor solo "olvida" el objeto, sin reparentarlo.
            if (Holder != null) Holder.DetachDisabled(this);
            _hoverCount = 0;
            ApplyHighlight(false);
        }

        protected virtual void Awake()
        {
            if (highlightRenderers == null || highlightRenderers.Length == 0)
                highlightRenderers = GetComponentsInChildren<Renderer>(true);
        }

        // ── Agarre ───────────────────────────────────────────────────────────
        /// <summary>¿Se puede agarrar ahora mismo?</summary>
        public virtual bool CanGrab => false;

        /// <summary>
        /// Se puede pulsar tocándolo con el dedo (VR): botones, consolas, escotillas.
        /// Al tocar se llama OnUsePressed / OnUseHeld / OnUseReleased, igual que con el gatillo.
        /// </summary>
        public virtual bool AllowPoke => false;

        /// <summary>Posición/rotación local al sujetarlo en la mano (o delante de la cámara).</summary>
        public virtual Vector3    HoldLocalPosition => Vector3.zero;
        public virtual Quaternion HoldLocalRotation => Quaternion.identity;

        internal void SetHolder(NovaInteractor interactor) { Holder = interactor; }

        public virtual void OnGrab(NovaInteractor interactor)    { }
        /// <summary>Se intentó agarrar pero CanGrab es false (para dar un aviso).</summary>
        public virtual void OnGrabDenied(NovaInteractor interactor) { }
        public virtual void OnRelease(NovaInteractor interactor) { }

        // ── Uso ──────────────────────────────────────────────────────────────
        /// <summary>Texto del aviso (puede cambiar según el estado).</summary>
        public virtual string GetPrompt() => prompt;

        public virtual void OnUsePressed(NovaInteractor interactor)  { }
        public virtual void OnUseHeld(NovaInteractor interactor, float deltaTime) { }
        public virtual void OnUseReleased(NovaInteractor interactor) { }

        /// <summary>Uso mientras el objeto está en la mano (herramientas).</summary>
        public virtual void OnHeldUsePressed(NovaInteractor interactor)  { }
        public virtual void OnHeldUse(NovaInteractor interactor, float deltaTime) { }
        public virtual void OnHeldUseReleased(NovaInteractor interactor) { }

        // ── Hover ────────────────────────────────────────────────────────────
        public virtual void OnHoverEnter(NovaInteractor interactor)
        {
            _hoverCount++;
            ApplyHighlight(true);
        }

        public virtual void OnHoverExit(NovaInteractor interactor)
        {
            _hoverCount = Mathf.Max(0, _hoverCount - 1);
            if (_hoverCount == 0) ApplyHighlight(false);
        }

        protected void ApplyHighlight(bool on)
        {
            if (highlightRenderers == null) return;
            if (_mpb == null) _mpb = new MaterialPropertyBlock();

            foreach (var r in highlightRenderers)
            {
                if (r == null) continue;
                r.GetPropertyBlock(_mpb);
                if (on) _mpb.SetColor(EmissionId, hoverColor * 0.6f);
                else    _mpb.Clear();
                r.SetPropertyBlock(_mpb);
            }
        }
    }
}
