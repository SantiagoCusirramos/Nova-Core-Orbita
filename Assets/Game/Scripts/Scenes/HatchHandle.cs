using System.Collections;
using UnityEngine;
using UnityEngine.XR;

namespace NovaCore
{
    /// <summary>
    /// Escotilla que se abre con el mando (feedback: "abrir escotillas con los controles").
    /// Apuntar a la escotilla y MANTENER el gatillo (o clic izquierdo) 'holdSeconds':
    /// la hoja sube y la transición al módulo vecino queda habilitada.
    /// Si la condición de la esclusa no se cumple, avisa en el HUD y no abre.
    /// </summary>
    public class HatchHandle : NovaInteractable
    {
        [Tooltip("Transición que controla (la misma escotilla)")]
        public HatchTransition transition;
        [Tooltip("Lo que se mueve al abrir (la hoja de la escotilla). Vacío = este objeto.")]
        public Transform door;
        public float holdSeconds = 0.6f;
        public float openHeight = 1.7f;
        public float openSeconds = 0.7f;
        public string destinationName = "";

        public bool IsOpen { get; private set; }

        private float _hold;
        private bool _opening;
        private AudioSource _audio;

        public override bool AllowPoke => true;   // se puede pulsar tocándolo con el dedo

        public override string GetPrompt()
        {
            if (IsOpen) return "";
            int pct = Mathf.RoundToInt(Mathf.Clamp01(_hold / holdSeconds) * 100f);
            return "Abrir escotilla → " + destinationName + (pct > 0 ? "  " + pct + "%" : "  (mantener gatillo)");
        }

        public override void OnUsePressed(NovaInteractor interactor)
        {
            _hold = 0f;
            if (!IsOpen && transition != null && !transition.ConditionMet)
            {
                MissionHUD.Feedback(transition.ClosedMessage(), MissionHUD.Tone.Warning);
                Haptic(interactor, 0.3f, 0.15f);
            }
        }

        public override void OnUseHeld(NovaInteractor interactor, float deltaTime)
        {
            if (IsOpen || _opening) return;
            if (transition != null && !transition.ConditionMet) return;
            _hold += deltaTime;
            Haptic(interactor, 0.08f, 0.02f);
            if (_hold >= holdSeconds) StartCoroutine(Open(interactor));
        }

        public override void OnUseReleased(NovaInteractor interactor) { _hold = 0f; }

        /// <summary>Abre la escotilla sin interacción (atajos de prueba del editor).</summary>
        public void ForceOpen()
        {
            if (IsOpen || _opening) return;
            StartCoroutine(Open(null));
        }

        private IEnumerator Open(NovaInteractor interactor)
        {
            _opening = true;
            Haptic(interactor, 0.6f, 0.2f);
            PlayClunk();
            Transform d = door != null ? door : transform;
            Vector3 a = d.localPosition, b = a + Vector3.up * openHeight;
            for (float t = 0f; t < 1f; t += Time.deltaTime / Mathf.Max(0.05f, openSeconds))
            {
                d.localPosition = Vector3.Lerp(a, b, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }
            d.localPosition = b;
            IsOpen = true;
            _opening = false;
            MissionHUD.Feedback("Escotilla abierta. Avanza hacia " + destinationName + ".", MissionHUD.Tone.Success, 2f);
        }

        private static void Haptic(NovaInteractor it, float amp, float sec)
        {
            if (it == null) return;
            if (it.source == NovaInteractor.Source.LeftHand) NovaXR.Haptic(XRNode.LeftHand, amp, sec);
            else if (it.source == NovaInteractor.Source.RightHand) NovaXR.Haptic(XRNode.RightHand, amp, sec);
        }

        private void PlayClunk()
        {
            if (_audio == null)
            {
                _audio = gameObject.AddComponent<AudioSource>();
                _audio.spatialBlend = 1f;
                _audio.playOnAwake = false;
            }
            const int rate = 22050;
            int n = rate / 2;
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / rate;
                data[i] = (Mathf.Sin(2f * Mathf.PI * 70f * t) * 0.6f + Mathf.Sin(2f * Mathf.PI * 140f * t) * 0.2f)
                          * Mathf.Exp(-t * 9f) + (Random.value * 2f - 1f) * 0.15f * Mathf.Exp(-t * 30f);
            }
            var clip = AudioClip.Create("Hatch Clunk", n, 1, rate, false);
            clip.SetData(data, 0);
            _audio.PlayOneShot(clip, 0.8f);
        }
    }
}
