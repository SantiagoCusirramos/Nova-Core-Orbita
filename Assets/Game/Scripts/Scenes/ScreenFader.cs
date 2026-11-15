using System;
using System.Collections;
using UnityEngine;

namespace NovaCore
{
    /// <summary>
    /// Fundido a negro pegado a la cámara (funciona en VR: es un quad delante de los ojos).
    /// Al cargar la escena empieza en negro y aclara; SceneTransit lo usa para oscurecer
    /// antes de cambiar de escena.
    /// </summary>
    public class ScreenFader : MonoBehaviour
    {
        public static ScreenFader Instance { get; private set; }

        [Tooltip("Renderer del quad negro (material transparente URP Unlit)")]
        public Renderer fadeRenderer;
        public float fadeInSeconds = 0.6f;
        public float fadeOutSeconds = 0.3f;

        private Material _mat;
        private float _alpha = 1f;
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        private void Awake()
        {
            Instance = this;
            if (fadeRenderer != null)
            {
                _mat = fadeRenderer.material;   // instancia propia
                SetAlpha(1f);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (_mat != null) Destroy(_mat);
        }

        private void Start()
        {
            StartCoroutine(Fade(1f, 0f, fadeInSeconds, null));
        }

        public void FadeOutThen(Action done)
        {
            StopAllCoroutines();
            StartCoroutine(Fade(_alpha, 1f, fadeOutSeconds, done));
        }

        private IEnumerator Fade(float from, float to, float seconds, Action done)
        {
            float t = 0f;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                SetAlpha(Mathf.Lerp(from, to, Mathf.Clamp01(t / seconds)));
                yield return null;
            }
            SetAlpha(to);
            if (done != null) done();
        }

        private void SetAlpha(float a)
        {
            _alpha = a;
            if (fadeRenderer == null || _mat == null) return;
            fadeRenderer.enabled = a > 0.001f;
            _mat.SetColor(BaseColor, new Color(0f, 0f, 0f, a));
        }
    }
}
