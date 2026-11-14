using UnityEngine;

namespace NovaCore
{
    /// <summary>
    /// Luces de alarma de la estación: parpadean en rojo durante la emergencia de la
    /// Misión 1 (desde que se inicia la secuencia hasta confirmar la reparación) y
    /// quedan en ámbar suave durante la Misión 2. Usa MaterialPropertyBlock (sin
    /// instanciar materiales) y opcionalmente una luz puntual.
    /// </summary>
    public class AlarmLights : MonoBehaviour
    {
        public Renderer[] alarmRenderers;
        public Light[] alarmLights;
        public Color alarmColor = new Color(1f, 0.1f, 0.05f);
        public Color cautionColor = new Color(1f, 0.55f, 0.1f);
        public Color idleColor = new Color(0.15f, 0.02f, 0.02f);
        public float blinkSpeed = 3f;
        public float lightIntensity = 1.2f;

        private MaterialPropertyBlock _mpb;

        private void Update()
        {
            var m = MissionManager.Instance;
            Color c = idleColor;
            float li = 0f;

            if (m != null && m.CurrentState == MissionState.InProgress)
            {
                if (m.CurrentMissionIndex == 0)
                {
                    float k = 0.5f + 0.5f * Mathf.Sin(Time.time * blinkSpeed * Mathf.PI);
                    c = Color.Lerp(idleColor, alarmColor, k);
                    li = lightIntensity * k;
                }
                else
                {
                    c = cautionColor * 0.6f;
                    li = lightIntensity * 0.25f;
                }
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
    }
}
