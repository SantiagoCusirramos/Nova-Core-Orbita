using UnityEngine;

namespace NovaCore
{
    /// <summary>
    /// Ambiente de emergencia de la estación (feedback: cambiar luces titilantes por chispas).
    ///   - Misión 1 (falla eléctrica): tiras en rojo fijo, sin parpadeo, y chispas en
    ///     ráfagas aleatorias desde los paneles dañados, con un destello breve.
    ///   - Misión 2: tiras en ámbar suave y sin chispas (la falla interior ya se reparó).
    /// Usa MaterialPropertyBlock (sin instanciar materiales).
    /// </summary>
    public class AlarmLights : MonoBehaviour
    {
        public Renderer[] alarmRenderers;
        public Light[] alarmLights;
        [Tooltip("Emisores de chispas (ParticleSystem sin emisión continua: se disparan por ráfagas)")]
        public ParticleSystem[] sparks;

        public Color alarmColor = new Color(1f, 0.1f, 0.05f);
        public Color cautionColor = new Color(1f, 0.55f, 0.1f);
        public Color idleColor = new Color(0.15f, 0.02f, 0.02f);
        public float lightIntensity = 0.8f;

        [Header("Chispas")]
        public Vector2 burstInterval = new Vector2(0.8f, 3.2f);
        public Vector2Int burstCount = new Vector2Int(12, 35);
        public float flashIntensity = 2.5f;
        [Tooltip("Sonido de chisporroteo (opcional; si está vacío se genera uno)")]
        public AudioClip crackleClip;

        private MaterialPropertyBlock _mpb;
        private float _nextBurst;
        private float _flash;
        private Light _flashLight;
        private AudioSource _audio;

        private void Start()
        {
            _nextBurst = Time.time + Random.Range(burstInterval.x, burstInterval.y);
            if (crackleClip == null) crackleClip = MakeCrackle();
        }

        private void Update()
        {
            var m = MissionManager.Instance;
            Color c = idleColor;
            float li = 0f;
            bool emergency = false;

            if (m != null && m.CurrentState == MissionState.InProgress)
            {
                if (m.CurrentMissionIndex == 0)
                {
                    emergency = true;
                    c = alarmColor * 0.85f;
                    li = lightIntensity;
                }
                else
                {
                    c = cautionColor * 0.6f;
                    li = lightIntensity * 0.25f;
                }
            }

            if (emergency) UpdateSparks();
            _flash = Mathf.MoveTowards(_flash, 0f, Time.deltaTime * 12f);
            if (_flashLight != null)
            {
                _flashLight.intensity = _flash * flashIntensity;
                _flashLight.enabled = _flash > 0.01f;
            }

            if (_mpb == null) _mpb = new MaterialPropertyBlock();
            if (alarmRenderers != null)
            {
                foreach (var r in alarmRenderers)
                {
                    if (r == null) continue;
                    r.GetPropertyBlock(_mpb);
                    _mpb.SetColor("_BaseColor", c);
                    _mpb.SetColor("_EmissionColor", c * 2.5f);
                    r.SetPropertyBlock(_mpb);
                }
            }

            if (alarmLights != null)
            {
                foreach (var l in alarmLights)
                {
                    if (l == null) continue;
                    l.color = c;
                    l.intensity = li;
                    l.enabled = li > 0.01f;
                }
            }
        }

        private void UpdateSparks()
        {
            if (sparks == null || sparks.Length == 0 || Time.time < _nextBurst) return;
            _nextBurst = Time.time + Random.Range(burstInterval.x, burstInterval.y);

            var ps = sparks[Random.Range(0, sparks.Length)];
            if (ps == null) return;
            ps.Emit(Random.Range(burstCount.x, burstCount.y + 1));

            // Destello y chisporroteo en el punto de la ráfaga
            if (_flashLight == null)
            {
                var go = new GameObject("Destello chispas");
                go.transform.SetParent(transform, false);
                _flashLight = go.AddComponent<Light>();
                _flashLight.type = LightType.Point;
                _flashLight.range = 3f;
                _flashLight.color = new Color(1f, 0.75f, 0.4f);
                _flashLight.shadows = LightShadows.None;
                _audio = go.AddComponent<AudioSource>();
                _audio.spatialBlend = 1f;
                _audio.minDistance = 0.5f;
                _audio.maxDistance = 8f;
                _audio.playOnAwake = false;
            }
            _flashLight.transform.position = ps.transform.position;
            _flash = 1f;
            if (_audio != null && crackleClip != null)
            {
                _audio.pitch = Random.Range(0.85f, 1.2f);
                _audio.PlayOneShot(crackleClip, 0.55f);
            }
        }

        /// <summary>Chisporroteo procedural: chasquidos de ruido con caída rápida.</summary>
        private static AudioClip MakeCrackle()
        {
            const int rate = 22050;
            int n = (int)(rate * 0.45f);
            var data = new float[n];
            var rnd = new System.Random(7);
            int pops = 14;
            for (int p = 0; p < pops; p++)
            {
                int start = rnd.Next(0, n - 600);
                int len = 200 + rnd.Next(0, 500);
                float amp = 0.3f + (float)rnd.NextDouble() * 0.7f;
                for (int i = 0; i < len && start + i < n; i++)
                {
                    float env = Mathf.Exp(-i / (len * 0.25f));
                    data[start + i] += ((float)rnd.NextDouble() * 2f - 1f) * amp * env;
                }
            }
            for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(data[i], -1f, 1f) * 0.8f;
            var clip = AudioClip.Create("Crackle", n, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
