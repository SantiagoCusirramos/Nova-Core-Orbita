using UnityEngine;

namespace NovaCore
{
    /// <summary>
    /// Botón "OMITIR INTRO" bajo la pantalla del video: apuntar y MANTENER el gatillo / clic
    /// 0.8 s. (Con un solo toque el video se saltaba sin querer en el visor.)
    /// </summary>
    public class IntroSkipButton : NovaInteractable
    {
        public IntroVideoPlayer player;
        public float holdSeconds = 0.8f;
        private float _hold;

        public override string GetPrompt() =>
            "Omitir intro" + (_hold > 0f ? "  " + Mathf.RoundToInt(Mathf.Clamp01(_hold / holdSeconds) * 100f) + "%" : "  (mantener)");

        public override void OnUsePressed(NovaInteractor interactor) { _hold = 0f; }

        public override void OnUseHeld(NovaInteractor interactor, float deltaTime)
        {
            if (player == null || player.Elapsed < 1.5f) return;   // ignora el gatillo que venía pulsado
            _hold += deltaTime;
            if (_hold >= holdSeconds) player.Skip();
        }

        public override void OnUseReleased(NovaInteractor interactor) { _hold = 0f; }
    }
}
