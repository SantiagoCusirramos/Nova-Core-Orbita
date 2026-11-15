using UnityEngine;

namespace NovaCore
{
    /// <summary>Botón de práctica: mantener gatillo / clic izquierdo 'holdSeconds' para completar su paso.</summary>
    public class TutorialButton : NovaInteractable
    {
        public int stepIndex = 2;
        public float holdSeconds = 1f;
        public Renderer statusLight;
        private float _hold;
        private bool _done;

        public override string GetPrompt()
        {
            if (_done) return "Hecho";
            int pct = Mathf.RoundToInt(Mathf.Clamp01(_hold / holdSeconds) * 100f);
            return prompt + (pct > 0 ? "  " + pct + "%" : "  (mantener)");
        }

        public override void OnUsePressed(NovaInteractor interactor) { _hold = 0f; }

        public override void OnUseHeld(NovaInteractor interactor, float deltaTime)
        {
            if (_done) return;
            _hold += deltaTime;
            if (_hold < holdSeconds) return;
            _done = true;
            if (statusLight != null)
            {
                var mpb = new MaterialPropertyBlock();
                statusLight.GetPropertyBlock(mpb);
                mpb.SetColor("_BaseColor", new Color(0.2f, 1f, 0.45f));
                mpb.SetColor("_EmissionColor", new Color(0.2f, 1f, 0.45f) * 2f);
                statusLight.SetPropertyBlock(mpb);
            }
            if (TutorialController.Instance != null) TutorialController.Instance.Complete(stepIndex);
        }

        public override void OnUseReleased(NovaInteractor interactor) { _hold = 0f; }
    }
}
