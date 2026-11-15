using UnityEngine;
using UnityEngine.UI;

namespace NovaCore
{
    /// <summary>
    /// Botón de UI en World Space o Screen Space que muestra el modo activo
    /// (Espectador / Interactivo) y permite cambiarlo con un click.
    ///
    /// Setup:
    ///   1. Crear un Canvas (Screen Space - Overlay o World Space).
    ///   2. Añadir un Button. Asignar su Text como buttonLabel.
    ///   3. Añadir este componente al mismo GameObject que el Button.
    ///   4. Asignar o dejar que se auto-resuelva el PlayerModeSwitcher.
    ///
    /// El botón se actualiza automáticamente cuando el modo cambia
    /// via el evento PlayerModeSwitcher.OnModeChanged.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class ModeToggleUI : MonoBehaviour
    {
        [Header("Referencias")]
        [Tooltip("PlayerModeSwitcher en la escena. Se busca automáticamente si está vacío.")]
        public PlayerModeSwitcher modeSwitcher;

        [Tooltip("Texto del botón. Se busca en hijos si está vacío.")]
        public Text buttonLabel;

        [Header("Textos")]
        public string spectatorLabel    = "Modo: Espectador";
        public string interactiveLabel  = "Modo: Interactivo";

        private Button _button;

        // ────────────────────────────────────────────────────────────────────

        private void Awake()
        {
            _button = GetComponent<Button>();

            if (buttonLabel == null)
                buttonLabel = GetComponentInChildren<Text>(true);

            if (modeSwitcher == null)
                modeSwitcher = FindAnyObjectByType<PlayerModeSwitcher>();

            _button.onClick.AddListener(OnButtonClicked);
        }

        private void Start()
        {
            if (modeSwitcher != null)
            {
                modeSwitcher.OnModeChanged += UpdateLabel;
                UpdateLabel(modeSwitcher.CurrentMode);
            }
        }

        private void OnDestroy()
        {
            if (modeSwitcher != null)
                modeSwitcher.OnModeChanged -= UpdateLabel;
        }

        // ────────────────────────────────────────────────────────────────────

        private void OnButtonClicked()
        {
            modeSwitcher?.ToggleMode();
        }

        private void UpdateLabel(PlayerModeSwitcher.PlayerMode mode)
        {
            if (buttonLabel == null) return;
            buttonLabel.text = mode == PlayerModeSwitcher.PlayerMode.Spectator
                ? spectatorLabel
                : interactiveLabel;
        }
    }
}
