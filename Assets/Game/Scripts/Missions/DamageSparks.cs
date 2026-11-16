using UnityEngine;

namespace NovaCore
{
    /// <summary>
    /// Chispas en una pieza dañada (p. ej. el panel solar en la EVA) hasta que se repara.
    /// Ráfagas aleatorias con destello; se apagan cuando la acción indicada ya está hecha.
    /// </summary>
    public class DamageSparks : MonoBehaviour
    {
        public ParticleSystem[] sparks;
        public MissionAction stopWhenDone = MissionAction.SolarPanelRepaired;
        public Vector2 burstInterval = new Vector2(0.5f, 2.2f);
        public Vector2Int burstCount = new Vector2Int(15, 40);
        public float flashIntensity = 3f;

        private float _next, _flash;
        private Light _light;

        private void Start() { _next = Time.time + Random.Range(burstInterval.x, burstInterval.y); }

        private void Update()
        {
            _flash = Mathf.MoveTowards(_flash, 0f, Time.deltaTime * 10f);
            if (_light != null) { _light.intensity = _flash * flashIntensity; _light.enabled = _flash > 0.01f; }

            if (MissionGate.IsDone(stopWhenDone) || sparks == null || sparks.Length == 0 || Time.time < _next) return;
            _next = Time.time + Random.Range(burstInterval.x, burstInterval.y);
            var ps = sparks[Random.Range(0, sparks.Length)];
            if (ps == null) return;
            ps.Emit(Random.Range(burstCount.x, burstCount.y + 1));

            if (_light == null)
            {
                var go = new GameObject("Destello");
                go.transform.SetParent(transform, false);
                _light = go.AddComponent<Light>();
                _light.type = LightType.Point;
                _light.range = 4f;
                _light.color = new Color(1f, 0.75f, 0.4f);
                _light.shadows = LightShadows.None;
            }
            _light.transform.position = ps.transform.position;
            _flash = 1f;
        }
    }
}
