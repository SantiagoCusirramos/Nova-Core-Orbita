using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.XR;
using XRCommonUsages = UnityEngine.XR.CommonUsages;

namespace NovaCore
{
    /// <summary>
    /// Guía al jugador hacia el objetivo actual (feedback de mentores: "me perdía").
    ///   - Línea de flechas que avanza siempre hacia el objetivo, a la altura del suelo.
    ///   - Flecha 3D delante de la vista (estilo brújula) con la distancia.
    ///   - Si el objetivo está en otro módulo, guía hasta la escotilla correcta.
    /// Cuándo aparece:
    ///   - Unos segundos cada vez que cambia el objetivo.
    ///   - Si el jugador tarda más de 'hintDelay' segundos, se queda fija y da una pista.
    ///   - Siempre que se pulse Y (mando izquierdo) o G (teclado).
    /// Va en "Mission Systems" de cada escena; no necesita referencias.
    /// </summary>
    public class ObjectiveGuide : MonoBehaviour
    {
        [Header("Cuándo mostrar")]
        [Tooltip("Segundos que se ve la guía al cambiar de objetivo")]
        public float showOnChangeSeconds = 10f;
        [Tooltip("Segundos sin avanzar antes de dejar la guía fija y dar una pista")]
        public float hintDelay = 25f;

        [Header("Aspecto")]
        [Tooltip("Material base (Sprites/Default). Lo asigna el constructor para que el shader entre en la build.")]
        public Material guideMaterial;
        public Color guideColor = new Color(0.25f, 0.85f, 1f, 0.95f);
        [Tooltip("Altura de la línea sobre el suelo del jugador (m)")]
        public float lineHeight = 0.08f;
        public float lineWidth = 0.12f;
        [Tooltip("Velocidad con la que avanzan las flechas")]
        public float scrollSpeed = 1.2f;
        [Tooltip("Distancia a la que se considera que el jugador ya llegó (m)")]
        public float arriveDistance = 1.0f;

        private LineRenderer _line;
        private Material _lineMat;
        private Transform _arrow;
        private TextMesh _arrowText;
        private float _visibleUntil;
        private float _objectiveSince;
        private bool _hintGiven;
        private int _lastKey = int.MinValue;
        private bool _toggleWasPressed;
        private bool _forced;

        private void Start()
        {
            BuildVisuals();
            _objectiveSince = Time.time;
            _visibleUntil = Time.time + showOnChangeSeconds;
        }

        private void OnDestroy()
        {
            if (_lineMat != null) Destroy(_lineMat);
        }

        private void Update()
        {
            var cam = Camera.main;
            if (cam == null || _line == null) return;

            // Cambio de objetivo → mostrar unos segundos y reiniciar el temporizador de pista
            int key = ObjectiveKey();
            if (key != _lastKey)
            {
                _lastKey = key;
                _objectiveSince = Time.time;
                _hintGiven = false;
                _forced = false;
                _visibleUntil = Time.time + showOnChangeSeconds;
            }

            // Botón para mostrar/ocultar la guía a voluntad
            bool pressed = (Keyboard.current != null && Keyboard.current.gKey.isPressed)
                        || NovaXR.Button(XRNode.LeftHand, XRCommonUsages.secondaryButton);
            if (pressed && !_toggleWasPressed)
            {
                _forced = !_forced;
                if (!_forced) _visibleUntil = 0f;
            }
            _toggleWasPressed = pressed;

            // Demora → pista y guía fija
            bool stuck = Time.time - _objectiveSince > hintDelay && MissionGate.IsRunning;
            if (stuck && !_hintGiven)
            {
                _hintGiven = true;
                if (MissionScore.Instance != null) MissionScore.Instance.NotifyHint();
                MissionHUD.Feedback("Pista: " + HintFor() + "\nSigue las flechas azules.", MissionHUD.Tone.Info, 7f);
            }

            bool show = _forced || stuck || Time.time < _visibleUntil;
            Vector3 target;
            string label;
            bool has = TryGetGuideTarget(out target, out label);

            Vector3 head = cam.transform.position;
            if (has && Vector3.Distance(Flat(head), Flat(target)) < arriveDistance && Mathf.Abs(head.y - target.y) < 2f)
                show = false;

            SetVisible(show && has);
            if (!(show && has)) return;

            DrawLine(cam.transform, target);
            DrawArrow(cam.transform, target, label);
        }

        // ── Objetivo de la guía ──────────────────────────────────────────────

        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

        private static int ObjectiveKey()
        {
            var m = MissionGate.Manager;
            if (m == null) return -1;
            return ((int)m.CurrentState * 1000) + m.CurrentMissionIndex * 100 + m.CurrentObjectiveIndex;
        }

        private bool TryGetGuideTarget(out Vector3 pos, out string label)
        {
            pos = Vector3.zero;
            label = "";
            var m = MissionGate.Manager;
            if (m == null || m.CurrentState == MissionState.Completed) return false;

            string here = SceneManager.GetActiveScene().name;
            if (here == MissionGate.SceneTutorial) return false;
            string goal = MissionGate.CurrentTargetScene();

            if (here == goal)
            {
                if (!MissionGate.IsRunning)
                {
                    // Sin misión: ir al botón INICIAR
                    foreach (var b in FindObjectsByType<MissionButton>())
                        if (b.mode == MissionButton.Mode.StartMission) { pos = b.transform.position; label = "INICIAR"; return true; }
                    return false;
                }
                var t = MissionGate.FindCurrentTarget();
                if (t == null) return false;
                pos = t.MarkerPosition;
                label = "OBJETIVO";
                return true;
            }

            string next = MissionGate.NextHop(here, goal);

            // En la vista exterior: el cartel del módulo
            foreach (var mk in FindObjectsByType<ModuleMarker>())
                if (mk.sceneName == next) { pos = mk.transform.position; label = mk.moduleName.ToUpper(); return true; }

            // En interiores: la escotilla que lleva al siguiente módulo
            foreach (var h in FindObjectsByType<HatchTransition>())
                if (h.targetScene == next) { pos = h.transform.position; label = "→ " + h.destinationName.ToUpper(); return true; }

            return false;
        }

        private static string HintFor()
        {
            if (!MissionGate.IsRunning)
                return "ve al Nodo 1 Unity y pulsa el panel verde INICIAR.";
            var o = MissionGate.Manager.CurrentMissionObjective;
            if (o == null) return "revisa la tablet de tu muñeca izquierda.";
            switch (o.RequiredAction)
            {
                case MissionAction.EmergencyActivated:          return "el botón rojo de EMERGENCIA está en el Nodo. Mantén pulsado.";
                case MissionAction.EnergyModuleCollected:       return "el módulo de repuesto está en Destiny. Agárralo con el grip (F).";
                case MissionAction.EnergyModuleLoadedIntoHatch: return "en Zvezda, pulsa INTRODUCIR REPUESTO junto a la compuerta.";
                case MissionAction.RoboticConsoleActivated:     return "en Zvezda, activa la consola del brazo robótico.";
                case MissionAction.EnergyModuleInstalled:       return "mantén el joystick de la consola para mover el brazo.";
                case MissionAction.RepairConfirmed:             return "confirma el diagnóstico en la consola de Zvezda.";
                case MissionAction.SuitEquipped:                return "en Quest, ponte el traje del soporte.";
                case MissionAction.SuitSystemsChecked:          return "en Quest, pulsa VERIFICAR TRAJE.";
                case MissionAction.EVAPreparationCompleted:     return "en Quest, completa la preparación EVA en la consola.";
                case MissionAction.AirlockDepressurized:        return "en Quest, mantén DESPRESURIZAR.";
                case MissionAction.MMUActivated:                return "en Quest, activa la mochila de propulsión.";
                case MissionAction.SolarPanelReached:           return "sal por la escotilla exterior y vuela con los propulsores hasta el panel.";
                case MissionAction.SolarPanelRepaired:          return "usa las herramientas de la estación de reparación (gatillo).";
                case MissionAction.ReturnedToAirlock:           return "vuelve volando a la esclusa Quest.";
                case MissionAction.AirlockRepressurized:        return "en Quest, mantén REPRESURIZAR.";
                case MissionAction.StationPowerVerified:        return "ve a la Cúpula y confirma la energía.";
                default:                                        return "sigue las flechas.";
            }
        }

        // ── Dibujo ───────────────────────────────────────────────────────────

        private void DrawLine(Transform cam, Vector3 target)
        {
            // La línea sale del suelo delante del jugador, va al pie del objetivo y sube hasta él.
            var player = cam.root;
            float floorY = player != cam ? player.position.y + lineHeight : cam.position.y - 1.3f;
            bool eva = SceneManager.GetActiveScene().name == MissionGate.SceneEva
                    || SceneManager.GetActiveScene().name == MissionGate.SceneOrbit;
            if (eva) floorY = cam.position.y - 0.5f;   // en el espacio no hay suelo: a la altura del pecho

            Vector3 fwd = Vector3.ProjectOnPlane(target - cam.position, Vector3.up);
            Vector3 start = cam.position + (fwd.sqrMagnitude > 0.01f ? fwd.normalized * 0.6f : Vector3.zero);
            start.y = floorY;

            Vector3 foot = new Vector3(target.x, eva ? target.y : floorY, target.z);

            if (eva || Mathf.Abs(target.y - floorY) < 0.2f)
            {
                _line.positionCount = 2;
                _line.SetPosition(0, start);
                _line.SetPosition(1, target);
            }
            else
            {
                _line.positionCount = 3;
                _line.SetPosition(0, start);
                _line.SetPosition(1, foot);
                _line.SetPosition(2, target);
            }

            // Flechas avanzando hacia el objetivo
            if (_lineMat != null)
                _lineMat.mainTextureOffset = new Vector2(-Time.time * scrollSpeed, 0f);
        }

        private void DrawArrow(Transform cam, Vector3 target, string label)
        {
            // Flecha flotando abajo en el campo de visión, apuntando al objetivo
            _arrow.position = cam.position + cam.forward * 0.75f - cam.up * 0.22f;
            Vector3 to = target - _arrow.position;
            if (to.sqrMagnitude > 0.0001f) _arrow.rotation = Quaternion.LookRotation(to.normalized, cam.up);

            float d = Vector3.Distance(cam.position, target);
            _arrowText.text = label + "  " + (d < 10f ? d.ToString("0.0") : d.ToString("0")) + " m";
            var tt = _arrowText.transform;
            tt.position = _arrow.position - cam.up * 0.05f;
            tt.rotation = Quaternion.LookRotation(cam.forward, cam.up);
        }

        private void SetVisible(bool on)
        {
            if (_line.enabled != on) _line.enabled = on;
            if (_arrow.gameObject.activeSelf != on) _arrow.gameObject.SetActive(on);
            if (_arrowText.gameObject.activeSelf != on) _arrowText.gameObject.SetActive(on);
        }

        private void BuildVisuals()
        {
            var shader = guideMaterial != null ? guideMaterial.shader : Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("UI/Default");
            if (shader == null) { Debug.LogWarning("[ObjectiveGuide] No hay shader para la guía."); return; }

            _lineMat = new Material(shader) { name = "Guide (runtime)" };
            _lineMat.mainTexture = MakeChevronTexture();
            _lineMat.renderQueue = 3100;

            var lineGO = new GameObject("Guía · línea");
            lineGO.transform.SetParent(transform, false);
            _line = lineGO.AddComponent<LineRenderer>();
            _line.sharedMaterial = _lineMat;
            _line.textureMode = LineTextureMode.Tile;
            _line.widthMultiplier = lineWidth;
            _line.numCornerVertices = 2;
            _line.alignment = LineAlignment.View;
            _line.useWorldSpace = true;
            _line.startColor = guideColor;
            _line.endColor = guideColor;
            _line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _line.receiveShadows = false;
            _line.textureScale = new Vector2(2f, 1f);   // 2 flechas por metro

            // Flecha 3D (cono aplanado + cola) hecha con primitivas
            _arrow = new GameObject("Guía · flecha").transform;
            _arrow.SetParent(transform, false);
            var arrowMat = new Material(shader) { name = "Guide Arrow (runtime)", color = guideColor, renderQueue = 3101 };
            var head = Piece(PrimitiveType.Cube, _arrow, new Vector3(0f, 0f, 0.035f), new Vector3(0.05f, 0.012f, 0.05f), arrowMat);
            head.localRotation = Quaternion.Euler(0f, 45f, 0f);
            Piece(PrimitiveType.Cube, _arrow, new Vector3(0f, 0f, -0.01f), new Vector3(0.018f, 0.012f, 0.06f), arrowMat);

            var textGO = new GameObject("Guía · distancia");
            textGO.transform.SetParent(transform, false);
            _arrowText = textGO.AddComponent<TextMesh>();
            _arrowText.fontSize = 48;
            _arrowText.characterSize = 0.0018f;
            _arrowText.anchor = TextAnchor.UpperCenter;
            _arrowText.alignment = TextAlignment.Center;
            _arrowText.color = guideColor;
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font != null)
            {
                _arrowText.font = font;
                textGO.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            }

            SetVisible(false);
        }

        private static Transform Piece(PrimitiveType type, Transform parent, Vector3 pos, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(type);
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            return go.transform;
        }

        /// <summary>Textura de una flecha "&gt;" que se repite a lo largo de la línea.</summary>
        private static Texture2D MakeChevronTexture()
        {
            const int w = 64, h = 32;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                name = "Guide Chevron",
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear
            };
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                float v = Mathf.Abs((y + 0.5f) / h - 0.5f) * 2f;          // 0 en el centro, 1 en el borde
                for (int x = 0; x < w; x++)
                {
                    float u = (x + 0.5f) / w;
                    // Chevron: banda diagonal que apunta hacia +u
                    float center = 0.55f - v * 0.35f;
                    float dist = Mathf.Abs(u - center);
                    float a = Mathf.Clamp01((0.11f - dist) / 0.03f);
                    a *= Mathf.Clamp01((0.95f - v) / 0.1f);
                    px[y * w + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return tex;
        }
    }
}
