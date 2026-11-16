using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Video;
using UnityEngine.XR;
using XRCommonUsages = UnityEngine.XR.CommonUsages;

namespace NovaCore
{
    /// <summary>
    /// Video de introducción (lore): se ve al TERMINAR el entrenamiento, antes de empezar la
    /// misión en el Nodo (o al iniciar, si playOnStart está activo).
    /// Se proyecta en una pantalla grande delante del jugador con la sala a oscuras.
    /// OMITIR INTRO: botón A/X del mando, Espacio/Enter/Esc en el teclado, o apuntar al
    /// botón "OMITIR INTRO" y pulsar el gatillo / clic. Solo se ve una vez por partida.
    /// Al terminar (o al omitirlo) avisa con OnFinished y el entrenamiento empieza.
    /// </summary>
    public class IntroVideoPlayer : MonoBehaviour
    {
        public static IntroVideoPlayer Instance { get; private set; }
        /// <summary>True mientras el video está en pantalla (la música espera).</summary>
        public static bool IsPlaying => Instance != null && Instance._playing;
        private static bool _alreadyShown;

        public VideoClip clip;
        [Tooltip("Material Unlit para la pantalla (lo asigna el constructor; el video se pinta en _BaseMap)")]
        public Material screenMaterial;
        public float screenWidth = 3.2f;
        public float distance = 2.6f;
        [Tooltip("Apaga las luces de la sala mientras dura el video")]
        public bool darkenRoom = true;
        [Tooltip("Reproducir al cargar la escena. Si no, lo lanza Play() (al terminar el tutorial)")]
        public bool playOnStart = false;
        [Tooltip("Escena a la que se pasa al terminar u omitir el video (vacío = quedarse)")]
        public string loadSceneAfter = "";
        public string spawnIdAfter = "";
        [Tooltip("Dónde se coloca al jugador para ver el video (centro de la sala, mirando a una pared libre). Vacío = donde esté.")]
        public Transform viewPoint;

        public bool Done { get; private set; }
        public event Action OnFinished;

        private bool _playing;
        private VideoPlayer _vp;
        private GameObject _screen;
        private Light[] _lights;
        private bool[] _lightStates;
        private Color _ambient;
        private float _startTime;

        private void Awake() { Instance = this; }
        private void OnDestroy() { if (Instance == this) Instance = null; }

        private void Start()
        {
            if (playOnStart) Play();
        }

        private bool _requested;

        /// <summary>Muestra el video (una sola vez por partida). Al acabar u omitirlo, sigue el juego.</summary>
        public void Play()
        {
            if (_requested) return;
            _requested = true;
            if (clip == null || _alreadyShown) { Finish(); return; }
            _alreadyShown = true;
            Invoke(nameof(Begin), 0.3f);   // deja que el visor/cámara se coloque
        }

        /// <summary>True si el video está pedido pero aún no ha terminado.</summary>
        public bool Pending => _requested && !Done;

        private void Begin()
        {
            var cam = Camera.main;
            if (cam == null) { Finish(); return; }
            MoveToViewPoint(cam);

            // Pantalla delante del jugador, a la altura de los ojos, mirando hacia él
            Vector3 fwd = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up);
            if (fwd.sqrMagnitude < 0.01f) fwd = Vector3.forward;
            fwd.Normalize();
            _screen = new GameObject("Pantalla intro");
            _screen.transform.position = cam.transform.position + fwd * distance;
            _screen.transform.rotation = Quaternion.LookRotation(fwd, Vector3.up);

            float h = screenWidth * clip.height / Mathf.Max(1f, clip.width);
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "Video";
            Destroy(quad.GetComponent<Collider>());
            quad.transform.SetParent(_screen.transform, false);
            quad.transform.localScale = new Vector3(screenWidth, h, 1f);
            var r = quad.GetComponent<Renderer>();
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            if (screenMaterial != null) r.sharedMaterial = new Material(screenMaterial);

            // Marco negro un poco más grande (detrás)
            var frame = GameObject.CreatePrimitive(PrimitiveType.Quad);
            frame.name = "Marco";
            Destroy(frame.GetComponent<Collider>());
            frame.transform.SetParent(_screen.transform, false);
            frame.transform.localPosition = new Vector3(0f, 0f, 0.01f);
            frame.transform.localScale = new Vector3(screenWidth + 0.12f, h + 0.12f, 1f);
            var fr = frame.GetComponent<Renderer>();
            if (screenMaterial != null) { fr.sharedMaterial = new Material(screenMaterial); fr.sharedMaterial.SetColor("_BaseColor", Color.black); fr.sharedMaterial.mainTexture = null; }

            // Botón OMITIR INTRO debajo de la pantalla
            var skipGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
            skipGO.name = "Botón OMITIR INTRO";
            skipGO.transform.SetParent(_screen.transform, false);
            skipGO.transform.localPosition = new Vector3(screenWidth * 0.5f - 0.35f, -h * 0.5f - 0.17f, 0f);
            skipGO.transform.localScale = new Vector3(0.62f, 0.16f, 0.03f);
            var sr = skipGO.GetComponent<Renderer>();
            if (screenMaterial != null) { sr.sharedMaterial = new Material(screenMaterial); sr.sharedMaterial.SetColor("_BaseColor", new Color(0.05f, 0.25f, 0.35f)); sr.sharedMaterial.mainTexture = null; }
            var skip = skipGO.AddComponent<IntroSkipButton>();
            skip.player = this;
            skip.prompt = "Omitir intro";
            skip.maxDistance = 10f;
            NovaText.Create(_screen.transform, "Texto omitir", ">> OMITIR INTRO",
                            new Vector3(screenWidth * 0.5f - 0.35f, -h * 0.5f - 0.17f, -0.03f), Quaternion.identity,
                            new Vector2(0.6f, 0.14f), 60, Color.white);
            NovaText.Create(_screen.transform, "Ayuda omitir",
                            NovaXR.IsActive ? "A / X: omitir" : "Espacio / Enter: omitir",
                            new Vector3(-screenWidth * 0.5f + 0.5f, -h * 0.5f - 0.17f, -0.03f), Quaternion.identity,
                            new Vector2(1.0f, 0.1f), 70, new Color(0.7f, 0.8f, 0.9f));

            // Video y audio
            _vp = _screen.AddComponent<VideoPlayer>();
            _vp.playOnAwake = false;
            _vp.clip = clip;
            _vp.isLooping = false;
            _vp.renderMode = VideoRenderMode.MaterialOverride;
            _vp.targetMaterialRenderer = r;
            _vp.targetMaterialProperty = "_BaseMap";
            var audio = _screen.AddComponent<AudioSource>();
            audio.spatialBlend = 0f;
            audio.playOnAwake = false;
            _vp.audioOutputMode = VideoAudioOutputMode.AudioSource;
            _vp.controlledAudioTrackCount = 1;
            _vp.EnableAudioTrack(0, true);
            _vp.SetTargetAudioSource(0, audio);
            _vp.loopPointReached += _ => Finish();
            _vp.errorReceived += (_, msg) => { Debug.LogWarning("[IntroVideoPlayer] " + msg); Finish(); };
            _vp.Play();

            if (darkenRoom) Darken(true);
            _playing = true;
            _startTime = Time.unscaledTime;
        }

        private void Update()
        {
            if (!_playing) return;

            bool skip = false;
            var kb = Keyboard.current;
            if (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame)) skip = true;
            if (NovaXR.Button(XRNode.RightHand, XRCommonUsages.primaryButton) || NovaXR.Button(XRNode.LeftHand, XRCommonUsages.primaryButton)) skip = true;
            // Pequeña espera para no omitir con la misma pulsación que abrió el juego
            if (skip && Time.unscaledTime - _startTime > 0.6f) Skip();

            // Seguridad: si el video no arranca (códec no soportado) no bloquear el juego
            if (_vp != null && !_vp.isPlaying && Time.unscaledTime - _startTime > 8f && _vp.frame <= 0) Finish();
        }

        public void Skip() => Finish();

        /// <summary>Lleva al jugador al punto de vista (la cámara queda en él, mirando hacia su frente).</summary>
        private void MoveToViewPoint(Camera cam)
        {
            if (viewPoint == null) return;
            Transform root = cam.transform.root;
            if (root == cam.transform) return;
            var cc = root.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            // Girar el cuerpo para que la cabeza mire hacia el frente del punto de vista
            Vector3 camFwd = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up);
            Vector3 wantFwd = Vector3.ProjectOnPlane(viewPoint.forward, Vector3.up);
            if (camFwd.sqrMagnitude > 0.001f && wantFwd.sqrMagnitude > 0.001f)
                root.RotateAround(cam.transform.position, Vector3.up, Vector3.SignedAngle(camFwd, wantFwd, Vector3.up));
            // Llevar la cabeza (en horizontal) al punto de vista
            Vector3 d = viewPoint.position - cam.transform.position;
            d.y = 0f;
            root.position += d;

            var fly = root.GetComponent<OrbitFreeFly>();
            if (fly != null) fly.StopMotion();
            if (cc != null) { Physics.SyncTransforms(); cc.enabled = true; }
        }

        private void Finish()
        {
            if (Done) return;
            Done = true;
            _playing = false;
            if (_vp != null) _vp.Stop();
            if (_screen != null) Destroy(_screen);
            if (darkenRoom) Darken(false);
            OnFinished?.Invoke();
            if (!string.IsNullOrEmpty(loadSceneAfter)) GameIntroManager.PendingAutoStart = true;   // la misión empieza al llegar
            if (!string.IsNullOrEmpty(loadSceneAfter)) SceneTransit.Go(loadSceneAfter, spawnIdAfter);
        }

        private void Darken(bool on)
        {
            if (on)
            {
                _lights = FindObjectsByType<Light>(FindObjectsInactive.Exclude);
                _lightStates = new bool[_lights.Length];
                for (int i = 0; i < _lights.Length; i++) { _lightStates[i] = _lights[i].enabled; _lights[i].enabled = false; }
                _ambient = RenderSettings.ambientLight;
                RenderSettings.ambientLight = _ambient * 0.15f;
            }
            else if (_lights != null)
            {
                for (int i = 0; i < _lights.Length; i++) if (_lights[i] != null) _lights[i].enabled = _lightStates[i];
                RenderSettings.ambientLight = _ambient;
                _lights = null;
            }
        }
    }
}
