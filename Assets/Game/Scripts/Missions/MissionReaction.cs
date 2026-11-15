using UnityEngine;

namespace NovaCore
{
    /// <summary>
    /// "Cuando se complete la acción X, haz Y" — sin escribir código por objetivo.
    /// Escucha MissionManager.OnActionCompleted y al reiniciar deshace los cambios (RF-05).
    ///
    /// Ejemplos en la escena de interior:
    ///   AirlockDepressurized → cerrar escotilla interior, abrir la exterior.
    ///   EnergyModuleLoadedIntoHatch → ocultar la pieza y mostrarla en la bahía robótica.
    ///   MMUActivated → subir la velocidad del jugador (propulsión).
    /// </summary>
    public class MissionReaction : MonoBehaviour
    {
        [Tooltip("Acción que dispara esta reacción")]
        public MissionAction onAction = MissionAction.AirlockDepressurized;

        [Header("Objetos")]
        public GameObject[] enableObjects;
        public GameObject[] disableObjects;

        [Header("Escotillas")]
        public HatchDoor[] forceOpen;
        public HatchDoor[] forceClosed;
        public HatchDoor[] backToProximity;

        [Header("Jugador")]
        [Tooltip("> 0: cambia la velocidad base de OrbitFreeFly del jugador")]
        public float setPlayerSpeed = -1f;

        [Header("Aviso")]
        [TextArea] public string message = "";

        private bool[] _enableInitial, _disableInitial;
        private float _speedInitial = -1f;
        private OrbitFreeFly _fly;

        private void Awake()
        {
            _enableInitial  = Capture(enableObjects);
            _disableInitial = Capture(disableObjects);
        }

        private void Start()
        {
            var m = MissionManager.Instance;
            if (m != null)
            {
                m.OnActionCompleted += HandleAction;
                m.OnMissionReset    += HandleReset;
            }
            _fly = FindAnyObjectByType<OrbitFreeFly>();
            if (_fly != null) _speedInitial = _fly.moveSpeed;

            // Al entrar en la escena con la acción ya completada (venimos de otro módulo),
            // dejar los objetos en su estado final sin repetir el aviso.
            if (MissionGate.IsDone(onAction)) Apply(false);
        }

        private void OnDestroy()
        {
            var m = MissionManager.Instance;
            if (m != null)
            {
                m.OnActionCompleted -= HandleAction;
                m.OnMissionReset    -= HandleReset;
            }
        }

        private void HandleAction(MissionAction a)
        {
            if (a != onAction) return;
            Apply(true);
        }

        private void Apply(bool showMessage)
        {
            SetAll(enableObjects, true);
            SetAll(disableObjects, false);
            if (forceOpen != null)       foreach (var h in forceOpen)       if (h != null) h.ForceOpen();
            if (forceClosed != null)     foreach (var h in forceClosed)     if (h != null) h.ForceClosed();
            if (backToProximity != null) foreach (var h in backToProximity) if (h != null) h.SetMode(HatchDoor.Mode.Proximity);
            if (setPlayerSpeed > 0f && _fly != null) _fly.moveSpeed = setPlayerSpeed;
            if (showMessage && !string.IsNullOrEmpty(message)) MissionHUD.Feedback(message, MissionHUD.Tone.Success, 5f);
        }

        private void HandleReset()
        {
            Restore(enableObjects, _enableInitial);
            Restore(disableObjects, _disableInitial);
            RestoreHatches(forceOpen);
            RestoreHatches(forceClosed);
            RestoreHatches(backToProximity);
            if (_fly != null && _speedInitial > 0f) _fly.moveSpeed = _speedInitial;
        }

        private static bool[] Capture(GameObject[] arr)
        {
            if (arr == null) return new bool[0];
            var r = new bool[arr.Length];
            for (int i = 0; i < arr.Length; i++) r[i] = arr[i] != null && arr[i].activeSelf;
            return r;
        }

        private static void Restore(GameObject[] arr, bool[] state)
        {
            if (arr == null || state == null) return;
            for (int i = 0; i < arr.Length && i < state.Length; i++)
                if (arr[i] != null) arr[i].SetActive(state[i]);
        }

        private static void SetAll(GameObject[] arr, bool on)
        {
            if (arr == null) return;
            foreach (var g in arr) if (g != null) g.SetActive(on);
        }

        private static void RestoreHatches(HatchDoor[] arr)
        {
            if (arr == null) return;
            foreach (var h in arr) if (h != null) h.RestoreInitialMode();
        }
    }
}
