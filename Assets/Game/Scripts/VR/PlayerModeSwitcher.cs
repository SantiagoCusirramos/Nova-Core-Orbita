using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NovaCore
{
    /// <summary>
    /// Cambia el modo del jugador entre Espectador y Interactivo en runtime.
    ///
    ///   Espectador  — OrbitFreeFly / VRMovement activos, SpaceStationPlayerController desactivado.
    ///   Interactivo — SpaceStationPlayerController activo, vuelo libre desactivado.
    ///
    /// El botón de UI (ModeToggleUI) llama a ToggleMode().
    /// También se puede llamar directamente desde código: SetMode(PlayerMode).
    ///
    /// Tecla de escritorio: Tab  (en VR: menu button del mando izquierdo — opcional)
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerModeSwitcher : MonoBehaviour
    {
        public enum PlayerMode { Spectator, Interactive }

        [Header("Modo inicial")]
        public PlayerMode initialMode = PlayerMode.Spectator;

        [Header("Referencias (opcionales — se buscan automáticamente)")]
        [Tooltip("Script de vuelo libre modo espectador. Se busca en el mismo GameObject si está vacío.")]
        public OrbitFreeFly orbitFreeFly;

        [Tooltip("Script de movimiento VR. Se busca en el mismo GameObject si está vacío.")]
        public VRMovement vrMovement;

        [Tooltip("Controlador FPS/VR para modo interactivo. Se busca en el mismo GameObject si está vacío.")]
        public SpaceStationPlayerController playerController;

        [Header("Opciones")]
        [Tooltip("Tecla de escritorio para alternar modo")]
        public Key toggleKey = Key.Tab;

        // ── Eventos ──────────────────────────────────────────────────────────
        /// <summary>Se invoca cada vez que el modo cambia. Parámetro: modo nuevo.</summary>
        public event Action<PlayerMode> OnModeChanged;

        // ── Estado ───────────────────────────────────────────────────────────
        private PlayerMode _currentMode;
        public  PlayerMode CurrentMode => _currentMode;

        // ────────────────────────────────────────────────────────────────────

        private void Awake()
        {
            // Auto-resolve references en el mismo objeto o hijos
            if (orbitFreeFly    == null) orbitFreeFly    = GetComponentInChildren<OrbitFreeFly>(true);
            if (vrMovement      == null) vrMovement      = GetComponentInChildren<VRMovement>(true);
            if (playerController == null) playerController = GetComponentInChildren<SpaceStationPlayerController>(true);
        }

        private void Start()
        {
            // Forzar el estado inicial sin disparar evento
            ApplyMode(initialMode, fireEvent: false);
            _currentMode = initialMode;
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb[toggleKey].wasPressedThisFrame)
                ToggleMode();

            // VR: menu button (Oculus menu, botón ☰ del mando izquierdo)
            var leftHand = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.LeftHand);
            if (leftHand.isValid && leftHand.TryGetFeatureValue(UnityEngine.XR.CommonUsages.menuButton, out bool menu) && menu)
                ToggleMode();
        }

        // ────────────────────────────────────────────────────────────────────

        /// <summary>Alterna entre Espectador e Interactivo.</summary>
        public void ToggleMode()
        {
            SetMode(_currentMode == PlayerMode.Spectator
                ? PlayerMode.Interactive
                : PlayerMode.Spectator);
        }

        /// <summary>Establece un modo concreto.</summary>
        public void SetMode(PlayerMode mode)
        {
            if (mode == _currentMode) return;
            _currentMode = mode;
            ApplyMode(mode, fireEvent: true);
        }

        // ────────────────────────────────────────────────────────────────────

        private void ApplyMode(PlayerMode mode, bool fireEvent)
        {
            bool spectator = mode == PlayerMode.Spectator;

            // Vuelo libre
            if (orbitFreeFly != null) orbitFreeFly.enabled = spectator;
            if (vrMovement   != null) vrMovement.enabled   = spectator;

            // Controlador físico
            if (playerController != null) playerController.enabled = !spectator;

            // CharacterController: solo activo en modo interactivo
            var cc = GetComponent<CharacterController>();
            if (cc != null) cc.enabled = !spectator;

            Debug.Log($"[PlayerModeSwitcher] Modo: {mode}");

            if (fireEvent) OnModeChanged?.Invoke(mode);
        }
    }
}
