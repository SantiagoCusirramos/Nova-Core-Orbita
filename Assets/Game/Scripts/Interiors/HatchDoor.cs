using UnityEngine;

namespace NovaCore
{
    /// <summary>
    /// Escotilla de la ISS. Portada de StationDoor (NASA-SPACE-APPS) y ampliada:
    ///   - Proximity: se abre sola cuando la cabeza del jugador se acerca.
    ///   - ForcedOpen / ForcedClosed: la controlan las misiones (esclusa).
    /// La hoja se enrolla hacia arriba (escala Y desde un pivote en su borde superior) o se
    /// desliza; su collider bloquea el paso mientras está cerrada.
    /// </summary>
    public class HatchDoor : MonoBehaviour
    {
        public enum Mode { Proximity, ForcedOpen, ForcedClosed }

        [Header("Hoja")]
        [Tooltip("Pivote de la hoja. En modo enrollar, el pivote está en el borde superior.")]
        public Transform panel;
        [Tooltip("true = se enrolla hacia arriba (escala Y). false = se desliza openOffset.")]
        public bool rollUp = true;
        [Tooltip("Escala Y de la hoja abierta (modo enrollar)")]
        public float openScaleY = 0.04f;
        [Tooltip("Desplazamiento local de la hoja al abrir (modo deslizar)")]
        public Vector3 openOffset = new Vector3(0f, 1.7f, 0f);
        public float openSpeed = 3.5f;

        [Header("Comportamiento")]
        public Mode mode = Mode.Proximity;
        public float detectionRadius = 2.2f;

        [Header("Indicador (opcional)")]
        public Renderer statusLight;
        public Color openColor = new Color(0.2f, 1f, 0.45f);
        public Color closedColor = new Color(1f, 0.25f, 0.15f);

        public bool IsOpen { get; private set; }

        private Mode _initialMode;
        private Vector3 _closedPos;
        private Vector3 _closedScale = Vector3.one;
        private Camera _cam;
        private MaterialPropertyBlock _mpb;
        private bool _lastLightState;
        private bool _lightInit;

        private void Awake()
        {
            _initialMode = mode;
            if (panel != null)
            {
                _closedPos = panel.localPosition;
                _closedScale = panel.localScale;
            }
        }

        public void SetMode(Mode m) { mode = m; }
        public void ForceOpen()     { mode = Mode.ForcedOpen; }
        public void ForceClosed()   { mode = Mode.ForcedClosed; }
        public void RestoreInitialMode() { mode = _initialMode; }

        private void Update()
        {
            switch (mode)
            {
                case Mode.ForcedOpen:   IsOpen = true;  break;
                case Mode.ForcedClosed: IsOpen = false; break;
                default:
                    if (_cam == null || !_cam.isActiveAndEnabled) _cam = Camera.main;
                    IsOpen = _cam != null && Vector3.Distance(transform.position, _cam.transform.position) < detectionRadius;
                    break;
            }

            if (panel != null)
            {
                float k = 1f - Mathf.Exp(-openSpeed * Time.deltaTime);
                if (rollUp)
                {
                    Vector3 sc = _closedScale;
                    if (IsOpen) sc.y = _closedScale.y * openScaleY;
                    panel.localScale = Vector3.Lerp(panel.localScale, sc, k);
                }
                else
                {
                    Vector3 target = IsOpen ? _closedPos + openOffset : _closedPos;
                    panel.localPosition = Vector3.Lerp(panel.localPosition, target, k);
                }
            }

            UpdateLight();
        }

        private void UpdateLight()
        {
            if (statusLight == null) return;
            if (_lightInit && _lastLightState == IsOpen) return;
            _lightInit = true;
            _lastLightState = IsOpen;

            if (_mpb == null) _mpb = new MaterialPropertyBlock();
            Color c = IsOpen ? openColor : closedColor;
            statusLight.GetPropertyBlock(_mpb);
            _mpb.SetColor("_BaseColor", c);
            _mpb.SetColor("_EmissionColor", c * 2f);
            statusLight.SetPropertyBlock(_mpb);
        }
    }
}
