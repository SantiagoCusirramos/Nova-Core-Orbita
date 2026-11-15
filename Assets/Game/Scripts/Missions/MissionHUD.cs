using UnityEngine;
using UnityEngine.UI;

namespace NovaCore
{
    /// <summary>
    /// Panel de misión flotante que sigue la vista del jugador (VR y escritorio).
    /// Muestra: misión actual, objetivo, progreso, distancia al objetivo, avisos y
    /// lo que se puede hacer con el objeto apuntado. Además coloca un marcador
    /// (rombo que gira) encima del objeto del objetivo actual.
    ///
    /// Se suscribe a los eventos de MissionManager (RF-02 / RF-08).
    /// </summary>
    public class MissionHUD : MonoBehaviour
    {
        public enum Tone { Info, Success, Warning }

        public static MissionHUD Instance { get; private set; }

        [Header("Seguimiento de la vista")]
        public float distance = 0.95f;
        public float heightOffset = -0.32f;
        public float followSpeed = 4f;

        [Header("Marcador de objetivo")]
        public Material markerMaterial;
        public bool showMarker = true;

        [Header("Textos (se crean solos si están vacíos)")]
        public Text titleText;
        public Text objectiveText;
        public Text feedbackText;
        public Text promptText;

        private Transform _panel;
        private Transform _marker;
        private Camera _cam;
        private float _feedbackTimer;
        private string _missionLine = "";
        private bool _snapNext = true;

        private void Awake()
        {
            Instance = this;
            BuildPanelIfNeeded();
            BuildMarker();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            var m = MissionManager.Instance;
            if (m != null)
            {
                m.OnObjectiveChanged -= HandleObjectiveChanged;
                m.OnMissionChanged   -= HandleMissionChanged;
                m.OnMissionCompleted -= HandleCompleted;
            }
        }

        private void Start()
        {
            var m = MissionManager.Instance;
            if (m != null)
            {
                m.OnObjectiveChanged += HandleObjectiveChanged;
                m.OnMissionChanged   += HandleMissionChanged;
                m.OnMissionCompleted += HandleCompleted;
            }
            Refresh();
        }

        // ── Eventos del Core ─────────────────────────────────────────────────
        private void HandleObjectiveChanged(MissionState state, string objective) => Refresh();
        private void HandleMissionChanged(MissionDefinition def) => Refresh();
        private void HandleCompleted()
        {
            Feedback("ESTACIÓN ESTABILIZADA. ¡Misiones completadas!", Tone.Success);
            Refresh();
        }

        private void Refresh()
        {
            var m = MissionManager.Instance;
            if (m == null)
            {
                SetTitle("NOVA CORE · ISS");
                SetObjective("Sin sistema de misiones en esta escena.");
                return;
            }

            switch (m.CurrentState)
            {
                case MissionState.NotStarted:
                    if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == MissionGate.SceneTutorial)
                    {
                        _missionLine = "NOVA CORE · ENTRENAMIENTO";
                        SetObjective("Sigue las pantallas negras de la sala, paso a paso.");
                    }
                    else
                    {
                        _missionLine = "NOVA CORE · ISS — EN ESPERA";
                        SetObjective("Ve al panel verde INICIAR en el Nodo y púlsalo.");
                    }
                    break;

                case MissionState.Completed:
                    _missionLine = "MISIONES COMPLETADAS";
                    SetObjective(m.CurrentObjective);
                    break;

                default:
                    var def = m.CurrentMission;
                    int total = def != null ? def.ObjectiveCount : 0;
                    _missionLine = string.Format("MISIÓN {0}/{1}: {2}",
                        m.CurrentMissionIndex + 1, m.Missions.Count, def != null ? def.MissionName : "?");
                    SetObjective(string.Format("Paso {0}/{1}: {2}", m.CurrentObjectiveIndex + 1, total, m.CurrentObjective));
                    break;
            }
            SetTitle(_missionLine);
        }

        // ── API estática para avisos ─────────────────────────────────────────
        public static void Feedback(string message, Tone tone = Tone.Info, float seconds = 3.5f)
        {
            if (Instance == null)
            {
                Debug.Log("[MissionHUD] " + message);
                return;
            }
            Instance.ShowFeedback(message, tone, seconds);
        }

        private void ShowFeedback(string message, Tone tone, float seconds)
        {
            if (feedbackText == null) return;
            string col = tone == Tone.Success ? "#6CFF9A" : tone == Tone.Warning ? "#FFC14D" : "#9FDCFF";
            feedbackText.text = "<color=" + col + ">" + message + "</color>";
            _feedbackTimer = seconds;
        }

        // ── Bucle ────────────────────────────────────────────────────────────
        private void LateUpdate()
        {
            if (_cam == null || !_cam.isActiveAndEnabled) { _cam = Camera.main; _snapNext = true; }
            if (_cam == null) return;

            FollowView();
            UpdatePrompt();
            UpdateMarker();

            if (_feedbackTimer > 0f)
            {
                _feedbackTimer -= Time.deltaTime;
                if (_feedbackTimer <= 0f && feedbackText != null) feedbackText.text = "";
            }
        }

        private void FollowView()
        {
            if (_panel == null) return;
            Transform c = _cam.transform;

            // Solo el giro horizontal de la cabeza; el panel queda algo por debajo de la vista
            Vector3 fwd = Vector3.ProjectOnPlane(c.forward, Vector3.up);
            if (fwd.sqrMagnitude < 1e-4f) fwd = Vector3.ProjectOnPlane(c.up, Vector3.up);
            fwd.Normalize();

            Vector3 targetPos = c.position + fwd * distance + Vector3.up * heightOffset;
            Quaternion targetRot = Quaternion.LookRotation(targetPos - c.position, Vector3.up);

            if (_snapNext)
            {
                _panel.SetPositionAndRotation(targetPos, targetRot);
                _snapNext = false;
                return;
            }

            float k = 1f - Mathf.Exp(-followSpeed * Time.deltaTime);
            _panel.position = Vector3.Lerp(_panel.position, targetPos, k);
            _panel.rotation = Quaternion.Slerp(_panel.rotation, targetRot, k);
        }

        private void UpdatePrompt()
        {
            if (promptText == null) return;

            // Buscar un interactor activo que esté apuntando a algo
            NovaInteractor best = null;
            foreach (var i in FindInteractors())
            {
                if (i == null || !i.isActiveAndEnabled) continue;
                if (i.Held != null || i.Hovered != null) { best = i; break; }
            }

            string s = "";
            if (best != null)
            {
                bool vr = best.IsVR;
                if (best.Held != null)
                {
                    s = "Sujetando: <b>" + best.Held.GetPrompt() + "</b>  ·  " +
                        (vr ? "Gatillo: usar · Suelta el grip para soltar" : "Click: usar · F: soltar");
                }
                else if (best.Hovered != null)
                {
                    string grab = best.Hovered.CanGrab ? (vr ? " · Grip: agarrar" : " · F: agarrar") : "";
                    s = "<b>" + best.Hovered.GetPrompt() + "</b>  ·  " + (vr ? "Gatillo: usar" : "Click: usar") + grab;
                }
            }

            // Distancia al objetivo actual
            var target = MissionGate.FindCurrentTarget();
            if (target != null)
            {
                float d = Vector3.Distance(_cam.transform.position, target.MarkerPosition);
                s += (s.Length > 0 ? "\n" : "") + "<color=#9FDCFF>Objetivo a " + d.ToString("0.0") + " m (rombo azul)</color>";
            }
            else if (MissionGate.IsRunning)
            {
                // El objetivo está en otro módulo (otra escena)
                s += (s.Length > 0 ? "\n" : "") + "<color=#9FDCFF>Ve a: " + MissionGate.CurrentLocation() + "</color>";
            }

            bool xr = NovaXR.IsActive;
            if (MissionGate.IsRunning)
                s += "\n<color=#7FA8C8>" + (xr ? "Y" : "G") + ": mostrar camino · " + (xr ? "Mira tu muñeca izq.: tablet" : "Tab: tablet") + "</color>";

            // Propulsores del traje (EVA): velocidad y botones, separados de los de herramientas
            if (_fly == null && Time.unscaledTime > _nextFlySearch)
            {
                _nextFlySearch = Time.unscaledTime + 2f;
                _fly = FindAnyObjectByType<OrbitFreeFly>();
            }
            if (_fly != null && _fly.mode == OrbitFreeFly.MoveMode.Thrusters)
                s += "\n<color=#FFB86B>Propulsores " + _fly.CurrentSpeed.ToString("0.0") + " m/s · " +
                     (xr ? "Stick izq.: empujar · A: subir · B: bajar · Clic stick izq.: ESTABILIZAR"
                         : "WASD: empujar · Espacio/Ctrl: subir/bajar · X: ESTABILIZAR") + "</color>";

            var sc = MissionScore.Instance;
            if (sc != null && (MissionGate.IsRunning || sc.Score > 0))
                s += "\n<color=#FFD27A>Puntos " + sc.Score + " · Insignias " + sc.PatchesFound + "/" + sc.totalPatches + "</color>";

            promptText.text = s;
        }

        private OrbitFreeFly _fly;
        private float _nextFlySearch;

        private static NovaInteractor[] _cache;
        private static float _cacheTime = -10f;
        private static NovaInteractor[] FindInteractors()
        {
            if (_cache == null || Time.unscaledTime - _cacheTime > 1f)
            {
                _cache = FindObjectsByType<NovaInteractor>();
                _cacheTime = Time.unscaledTime;
            }
            return _cache;
        }

        private void UpdateMarker()
        {
            if (_marker == null) return;
            var target = showMarker ? MissionGate.FindCurrentTarget() : null;
            bool on = target != null;
            if (_marker.gameObject.activeSelf != on) _marker.gameObject.SetActive(on);
            if (!on) return;

            // Más grande cuanto más lejos (en EVA el objetivo puede estar a decenas de metros)
            float dist = Vector3.Distance(_cam.transform.position, target.MarkerPosition);
            float size = Mathf.Max(0.09f, dist * 0.03f);
            _marker.localScale = Vector3.one * size;
            float bob = Mathf.Sin(Time.time * 2.5f) * 0.05f;
            _marker.position = target.MarkerPosition + Vector3.up * (0.25f + size + bob);
            _marker.rotation = Quaternion.Euler(0f, Time.time * 90f, 0f) * Quaternion.Euler(45f, 0f, 45f);
        }

        // ── Construcción ─────────────────────────────────────────────────────
        private void SetTitle(string s)     { if (titleText != null) titleText.text = s; }
        private void SetObjective(string s) { if (objectiveText != null) objectiveText.text = s; }

        private void BuildPanelIfNeeded()
        {
            if (titleText != null && objectiveText != null)
            {
                _panel = titleText.canvas != null ? titleText.canvas.transform : transform;
                return;
            }

            var panelGO = new GameObject("HUD Panel");
            panelGO.transform.SetParent(transform, false);
            _panel = panelGO.transform;

            // Canvas mirando "hacia atrás" para leerse desde la cámara (ver NovaText)
            Quaternion face = Quaternion.identity;
            Color white = new Color(0.92f, 0.96f, 1f);
            titleText     = NovaText.Create(_panel, "Title",     "", new Vector3(0f,  0.120f, 0f), face, new Vector2(0.62f, 0.05f), 30, new Color(0.55f, 0.85f, 1f));
            objectiveText = NovaText.Create(_panel, "Objective", "", new Vector3(0f,  0.055f, 0f), face, new Vector2(0.62f, 0.08f), 26, white);
            // Avisos y ayudas más altos: el tutorial y las pistas usan 3-4 líneas
            feedbackText  = NovaText.Create(_panel, "Feedback",  "", new Vector3(0f, -0.035f, 0f), face, new Vector2(0.66f, 0.09f), 22, white);
            promptText    = NovaText.Create(_panel, "Prompt",    "", new Vector3(0f, -0.150f, 0f), face, new Vector2(0.66f, 0.13f), 18, white);
            feedbackText.verticalOverflow = VerticalWrapMode.Overflow;
            promptText.verticalOverflow = VerticalWrapMode.Overflow;
            titleText.fontStyle = FontStyle.Bold;

            // Dibujar el HUD por encima de las paredes (si no, se esconde al acercarse a ellas)
            var ui = Shader.Find("UI/Default");
            if (ui != null)
            {
                var overlay = new Material(ui) { name = "HUD Overlay (ZTest Always)" };
                overlay.SetInt("unity_GUIZTestMode", (int)UnityEngine.Rendering.CompareFunction.Always);
                overlay.renderQueue = 4000;
                foreach (var t in new[] { titleText, objectiveText, feedbackText, promptText }) t.material = overlay;
            }
        }

        private void BuildMarker()
        {
            if (markerMaterial == null) return;   // sin material no se crea (evita magenta en build)
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Objective Marker";
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(transform, false);
            go.transform.localScale = Vector3.one * 0.09f;
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = markerMaterial;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            go.SetActive(false);
            _marker = go.transform;
        }
    }
}
