using UnityEngine;

namespace NovaCore
{
    /// <summary>
    /// Insignia de misión escondida en un módulo (gamificación). Apuntar y pulsar el
    /// gatillo / clic izquierdo para recogerla: +50 puntos. Gira y brilla un poco para que
    /// se note al verla; si ya se recogió en esta partida, no aparece al volver.
    /// </summary>
    public class MissionPatch : NovaInteractable
    {
        [Tooltip("Identificador único (uno por módulo)")]
        public string patchId = "nodo";
        public string patchName = "Insignia";
        public float spinSpeed = 60f;

        private Vector3 _base;

        protected override void Awake()
        {
            base.Awake();
            _base = transform.localPosition;
        }

        private void Start()
        {
            if (MissionScore.Instance != null && MissionScore.Instance.HasPatch(patchId)) gameObject.SetActive(false);
        }

        private void Update()
        {
            transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);
            transform.localPosition = _base + Vector3.up * Mathf.Sin(Time.time * 2f) * 0.02f;
        }

        public override bool AllowPoke => true;   // se puede pulsar tocándolo con el dedo

        public override string GetPrompt() => "Recoger " + patchName;

        public override void OnUsePressed(NovaInteractor interactor)
        {
            var s = MissionScore.Instance;
            int total = s != null ? s.totalPatches : 5;
            if (s != null && s.CollectPatch(patchId))
                MissionHUD.Feedback(patchName + " recogida  +" + s.pointsPerPatch + "\nInsignias: " + s.PatchesFound + "/" + total,
                                    MissionHUD.Tone.Success, 3f);
            gameObject.SetActive(false);
        }
    }
}
