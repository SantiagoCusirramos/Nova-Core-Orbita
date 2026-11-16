using UnityEngine;

namespace NovaCore
{
    /// <summary>Pieza de práctica: al agarrarla (grip / F) completa su paso del tutorial.</summary>
    public class TutorialGrabbable : NovaInteractable
    {
        public int stepIndex = 1;
        public override bool CanGrab => true;
        public override Vector3 HoldLocalPosition => new Vector3(0f, 0f, 0.12f);

        public override void OnGrab(NovaInteractor interactor)
        {
            if (TutorialController.Instance != null) TutorialController.Instance.Complete(stepIndex);
        }
    }
}
