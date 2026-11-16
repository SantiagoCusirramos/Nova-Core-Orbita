using UnityEngine;
using UnityEngine.UI;

namespace NovaCore
{
    /// <summary>
    /// Sala de entrenamiento (00_Tutorial): una "caja cerrada" con pantallas negras que
    /// explican los controles, paso a paso, antes de empezar la historia (feedback de mentores).
    /// Pasos: 1 moverse · 2 agarrar · 3 usar (gatillo) · 4 ver el camino y la tablet · 5 abrir la escotilla.
    /// Cada pantalla se pone verde al completar su paso. La escotilla final lleva al Nodo.
    /// </summary>
    public class TutorialController : MonoBehaviour
    {
        public static TutorialController Instance { get; private set; }

        [System.Serializable]
        public class Step
        {
            public string title;
            [TextArea] public string vrText;
            [TextArea] public string desktopText;
            public Image screen;        // fondo de la pantalla (se pone verde al terminar)
            public Text body;           // texto de la pantalla
            public Transform zone;      // solo paso 1: caja a la que hay que llegar
            public Vector3 zoneSize = new Vector3(1.2f, 3f, 1.2f);
            [HideInInspector] public bool done;
        }

        public Step[] steps;
        public HatchHandle exitHatch;
        public Color pendingColor = new Color(0.02f, 0.03f, 0.05f, 0.95f);
        public Color currentColor = new Color(0.03f, 0.08f, 0.14f, 0.95f);
        public Color doneColor = new Color(0.03f, 0.22f, 0.1f, 0.95f);

        private int _current;
        private bool _lastXR;
        private bool _guideWasPressed, _tabletSeen;

        private void Awake() { Instance = this; }
        private void OnDestroy() { if (Instance == this) Instance = null; }

        private void Start()
        {
            _lastXR = NovaXR.IsActive;
            RefreshScreens();
            // El video de introducción se ve al TERMINAR el entrenamiento (ver paso 5)
            BeginNarrative();
        }

        private bool _started;
        private bool _introRequested;

        private void BeginNarrative()
        {
            if (_started) return;
            _started = true;
            // Inicio narrativo (documento 2.2): despertar, alarma y aviso del Centro de Control
            PlayAlarm();
            MissionHUD.Feedback("Despiertas en la zona de descanso. Suena una alarma.\nCENTRO DE CONTROL: anomalía en la distribución eléctrica. " +
                                "Repasa los controles y ve al Nodo 1.", MissionHUD.Tone.Warning, 6f);
            Invoke(nameof(Announce), 6.5f);
        }

        private void PlayAlarm()
        {
            const int rate = 22050;
            int n = rate * 2;
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / rate;
                bool on = (t % 0.5f) < 0.3f;                       // dos pitidos por segundo
                float f = (t % 1f) < 0.5f ? 880f : 660f;
                data[i] = on ? Mathf.Sin(2f * Mathf.PI * f * t) * 0.25f * Mathf.Clamp01((2f - t) * 2f) : 0f;
            }
            var clip = AudioClip.Create("Alarm", n, 1, rate, false);
            clip.SetData(data, 0);
            var src = gameObject.AddComponent<AudioSource>();
            src.spatialBlend = 0f;
            src.PlayOneShot(clip, 0.6f);
        }

        private void Update()
        {
            if (_lastXR != NovaXR.IsActive) { _lastXR = NovaXR.IsActive; RefreshScreens(); }
#if UNITY_EDITOR
            // Atajo de prueba SOLO en el editor: F8 abre la escotilla de salida (para probar el video)
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && kb.f8Key.wasPressedThisFrame && exitHatch != null) exitHatch.ForceOpen();
            // F9: lleva al jugador dentro del túnel de la escotilla (como si la cruzara)
            if (kb != null && kb.f9Key.wasPressedThisFrame && exitHatch != null && Camera.main != null)
            {
                var root = Camera.main.transform.root;
                var cc = root.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;
                Vector3 d = exitHatch.transform.position - Camera.main.transform.position;
                d.y = 0f;
                root.position += d + exitHatch.transform.forward * 0.35f;   // forward de la escotilla apunta a la sala
                if (cc != null) { Physics.SyncTransforms(); cc.enabled = true; }
            }
#endif
            if (!_started || steps == null || _current >= steps.Length) return;

            // Paso 1: llegar a la zona marcada
            var s = steps[_current];
            if (s.zone != null)
            {
                var cam = Camera.main;
                if (cam != null)
                {
                    Vector3 p = s.zone.InverseTransformPoint(cam.transform.position);
                    Vector3 h = s.zoneSize * 0.5f;
                    if (Mathf.Abs(p.x) <= h.x && Mathf.Abs(p.y) <= h.y && Mathf.Abs(p.z) <= h.z) Complete(_current);
                }
            }

            // Paso 4: pulsar Y/G (camino) o abrir la tablet
            if (_current == 3)
            {
                bool g = (UnityEngine.InputSystem.Keyboard.current != null &&
                          (UnityEngine.InputSystem.Keyboard.current.gKey.isPressed || UnityEngine.InputSystem.Keyboard.current.tabKey.isPressed))
                      || NovaXR.Button(UnityEngine.XR.XRNode.LeftHand, UnityEngine.XR.CommonUsages.secondaryButton)
                      || NovaXR.Button(UnityEngine.XR.XRNode.LeftHand, UnityEngine.XR.CommonUsages.primaryButton);
                if (g && !_guideWasPressed) _tabletSeen = true;
                _guideWasPressed = g;
                if (_tabletSeen) Complete(3);
            }

            // Paso 5: escotilla abierta → al llegar al Nodo empezará la misión sola
            if (exitHatch != null && exitHatch.IsOpen && !_introRequested)
            {
                _introRequested = true;
                Complete(steps.Length - 1);
                // El video de introducción sale al CRUZAR la escotilla (HatchTransition)
            }
        }

        /// <summary>Lo llaman los objetos del tutorial (pieza para agarrar, botón...).</summary>
        public void Complete(int index)
        {
            if (steps == null || index < 0 || index >= steps.Length || steps[index].done) return;
            if (index != _current) return;   // en orden
            steps[index].done = true;
            _current++;
            RefreshScreens();
            if (_current < steps.Length)
            {
                MissionHUD.Feedback("¡Bien! " + steps[index].title + " completado.", MissionHUD.Tone.Success, 2f);
                Invoke(nameof(Announce), 1.8f);
            }
            else
            {
                MissionHUD.Feedback("Entrenamiento completo. Cruza la escotilla: mensaje entrante del Centro de Control...", MissionHUD.Tone.Success, 5f);
            }
        }

        /// <summary>Índice del paso actual (para que los objetos sepan si les toca).</summary>
        public int CurrentStep => _current;

        private void Announce()
        {
            if (steps == null || _current >= steps.Length) return;
            var s = steps[_current];
            MissionHUD.Feedback("Paso " + (_current + 1) + "/" + steps.Length + ": " + s.title + "\n" +
                                (NovaXR.IsActive ? s.vrText : s.desktopText), MissionHUD.Tone.Info, 8f);
        }

        private void RefreshScreens()
        {
            if (steps == null) return;
            bool xr = NovaXR.IsActive;
            for (int i = 0; i < steps.Length; i++)
            {
                var s = steps[i];
                string mark = s.done ? "  [OK]" : (i == _current ? "  <" : "");
                if (s.body != null)
                    s.body.text = "<b>" + (i + 1) + ". " + s.title.ToUpper() + mark + "</b>\n\n" + (xr ? s.vrText : s.desktopText);
                if (s.screen != null)
                    s.screen.color = s.done ? doneColor : (i == _current ? currentColor : pendingColor);
            }
        }
    }
}
