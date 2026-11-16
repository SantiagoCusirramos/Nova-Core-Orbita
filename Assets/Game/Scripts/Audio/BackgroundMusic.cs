using UnityEngine;

namespace NovaCore
{
    /// <summary>
    /// Música ambiental de fondo (feedback: "considerarías meterle música"). Una sola
    /// instancia para todo el juego: sobrevive a los cambios de módulo sin cortarse.
    /// Se queda en silencio mientras se reproduce el video de introducción.
    /// </summary>
    public class BackgroundMusic : MonoBehaviour
    {
        public static BackgroundMusic Instance { get; private set; }

        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 0.22f;
        public float fadeSeconds = 2.5f;

        private AudioSource _src;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);

            _src = gameObject.AddComponent<AudioSource>();
            _src.clip = clip;
            _src.loop = true;
            _src.playOnAwake = false;
            _src.spatialBlend = 0f;
            _src.volume = 0f;
            _src.priority = 200;
            if (clip != null) _src.Play();
        }

        private void Update()
        {
            if (_src == null) return;
            bool intro = IntroVideoPlayer.IsPlaying;
            float target = intro ? 0f : volume;
            _src.volume = Mathf.MoveTowards(_src.volume, target, Time.unscaledDeltaTime * volume / Mathf.Max(0.1f, fadeSeconds));
        }
    }
}
