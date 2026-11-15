using UnityEngine;

namespace NovaCore
{
    /// <summary>
    /// Nova Core Órbita — Entorno orbital (Tierra, Luna, Sol) visto desde la ISS.
    ///
    /// Convención: 1 unidad de Unity = 1 metro.
    /// La ISS (o el Player) se queda quieta; el "mundo" gira como lo vería
    /// un astronauta: Tierra abajo, Sol y Luna salen y se ponen cada 92 min.
    ///
    /// Datos reales:
    ///   Altitud ISS ........... 408 km
    ///   Radio Tierra .......... 6 378.137 km
    ///   Radio Luna ............ 1 737.4 km
    ///   Distancia Tierra-Luna . 384 400 km
    ///   Radio Sol ............. 695 700 km
    ///   Distancia Tierra-Sol .. 149 597 870 km
    ///   Periodo orbital ISS ... 92.68 min | Inclinación 51.6°
    ///
    /// Uso mínimo:
    ///   1. Colocar este componente en un GameObject vacío en la escena.
    ///   2. Asignar earthPrefab y moonPrefab (GLB/FBX de Tierra y Luna).
    ///   3. Asignar spacePanorama con la textura T_Skybox_DeepSpace_2K.png.
    ///   4. El script crea automáticamente la luz solar si no se asigna sunLight.
    ///   5. Ajustar timeScale: 1 = tiempo real, 60 = 1 min real = 1 h simulada.
    /// </summary>
    [DisallowMultipleComponent]
    public class OrbitalEnvironment : MonoBehaviour
    {
        // ── Constantes físicas reales (metros) ───────────────────────────────
        const float KM          = 1000f;
        const float EARTH_RADIUS = 6378.137f * KM;
        const float MOON_RADIUS  = 1737.4f   * KM;
        const float SUN_RADIUS   = 695700f   * KM;
        const float MOON_DIST    = 384400f   * KM;
        const float SUN_DIST     = 149597870f * KM;
        const float ISS_PERIOD_S = 92.68f    * 60f;
        const float SIDEREAL_DAY = 86164.1f;

        // ── Inspector ────────────────────────────────────────────────────────

        [Header("Referencias de escena")]
        [Tooltip("El punto de referencia orbital (Player o ISS). Si queda vacío busca un objeto 'ISS' o usa el Player.")]
        public Transform referencePoint;

        [Tooltip("Prefab / modelo de la Tierra (GLB o FBX ya importado)")]
        public GameObject earthPrefab;

        [Tooltip("Prefab / modelo de la Luna (GLB o FBX ya importado)")]
        public GameObject moonPrefab;

        [Tooltip("Directional Light que hará de Sol. Si queda vacío se crea automáticamente.")]
        public Light sunLight;

        public Camera mainCamera;

        [Header("Skybox espacial")]
        [Tooltip("T_Skybox_DeepSpace_2K.png importada como Texture2D. Si queda vacío se usa el cielo de Unity.")]
        public Texture2D spacePanorama;

        [Tooltip("Material de skybox. Si queda vacío se crea automáticamente con el shader NovaCore/SpacePanorama.")]
        public Material spaceSkybox;

        [Tooltip("UV del Sol horneado en el panorama. Para deep_space_combined_2k: (0.4221, 0.5671).")]
        public Vector2 bakedSunUV = new Vector2(0.4221f, 0.5671f);

        [Tooltip("Rota el panorama para que su Sol coincida con la luz direccional")]
        public bool alignSunToPanorama = true;

        [Tooltip("Luz ambiental tenue para que la ISS no quede completamente negra en la sombra")]
        public bool overrideAmbient = true;
        public Color ambientColor = new Color(0.035f, 0.04f, 0.055f);

        [Header("Órbita")]
        public float issAltitudeKm  = 408f;
        public float inclinationDeg = 51.6f;

        [Tooltip("Normal del plano orbital. (1,0,0) = la estación avanza hacia +Z con la Tierra en -Y")]
        public Vector3 orbitNormal = Vector3.right;

        [Tooltip("1 = tiempo real. 60 = 1 minuto real = 1 hora simulada. 0 = pausa.")]
        [Range(0f, 3600f)]
        public float timeScale = 1f;

        [Header("Posición inicial")]
        public Vector3 sunDirection  = new Vector3(0.35f, 0.25f, 0.9f);
        public Vector3 moonDirection = new Vector3(-0.7f, 0.2f, -0.65f);
        public float   startSpinDeg  = 0f;
        [Tooltip("Ajusta qué cara de la Luna mira hacia la Tierra si el modelo aparece girado")]
        public Vector3 moonFaceEuler = Vector3.zero;

        [Header("Distancias mostradas (compresión de perspectiva)")]
        [Tooltip("ON: mantiene el tamaño angular y dirección reales pero acerca los cuerpos para evitar problemas de precisión float")]
        public bool compressDistantBodies = true;
        public float moonDisplayDistance  = 5e7f;    // 50 000 km
        public float sunDisplayDistance   = 1e8f;    // 100 000 km

        [Header("Efectos")]
        [Tooltip("La luz del Sol se atenúa cuando la Tierra tapa al Sol (noche en la estación)")]
        public bool eclipseNight = true;

        [Tooltip("Muestra un HUD de depuración con la posición orbital")]
        public bool showDebugHud = true;

        // ── Estado interno ───────────────────────────────────────────────────
        private Transform earthPivot, moonPivot, sunPivot;
        private float     orbitAngle, spinAngle;
        private float     sunBaseIntensity = 1f;
        private Quaternion skyAlign        = Quaternion.identity;

        // HUD
        private float hudAlt, hudMoonKm, hudSunKm, hudSunVis = 1f;

        // ════════════════════════════════════════════════════════════════════
        // INICIO
        // ════════════════════════════════════════════════════════════════════

        private void Start()
        {
            // Resolver el punto de referencia
            if (referencePoint == null)
            {
                var iss = GameObject.Find("ISS");
                if (iss != null)
                    referencePoint = iss.transform;
                else
                {
                    var player = GameObject.FindGameObjectWithTag("Player");
                    referencePoint = player != null ? player.transform : transform;
                }
            }

            if (mainCamera == null) mainCamera = Camera.main;

            // ── Skybox ─────────────────────────────────────────────────────
            if (mainCamera != null) mainCamera.clearFlags = CameraClearFlags.Skybox;

            if (spaceSkybox == null)
            {
                string shaderName = spacePanorama != null ? "NovaCore/SpacePanorama" : "Skybox/Procedural";
                Shader sky = Shader.Find(shaderName);

                if (sky != null)
                {
                    spaceSkybox = new Material(sky);
                    if (spacePanorama != null) spaceSkybox.SetTexture("_MainTex", spacePanorama);
                }
                else
                {
                    Debug.LogWarning($"[OrbitalEnvironment] Shader '{shaderName}' no encontrado. " +
                                     "Asegúrate de que NovaCoreSpacePanorama.shader está en el proyecto.", this);
                }
            }

            // Alinear el Sol del panorama con la dirección real del Sol
            if (spacePanorama != null && alignSunToPanorama)
                skyAlign = Quaternion.FromToRotation(DirFromPanoramaUV(bakedSunUV), sunDirection.normalized);

            if (spaceSkybox != null) RenderSettings.skybox = spaceSkybox;

            if (overrideAmbient)
            {
                RenderSettings.ambientMode  = UnityEngine.Rendering.AmbientMode.Flat;
                RenderSettings.ambientLight = ambientColor;
            }

            // Far clip plane para los cuerpos lejanos
            float needFar = (compressDistantBodies ? sunDisplayDistance : SUN_DIST) * 1.5f;
            if (mainCamera != null && mainCamera.farClipPlane < needFar)
                mainCamera.farClipPlane = needFar;

            // ── Luz solar ─────────────────────────────────────────────────
            if (sunLight == null) sunLight = RenderSettings.sun;
            if (sunLight == null)
            {
                foreach (var l in FindObjectsByType<Light>(FindObjectsInactive.Exclude))
                    if (l.type == LightType.Directional) { sunLight = l; break; }
            }
            if (sunLight == null)
            {
                var go = new GameObject("Sol (luz direccional)");
                sunLight = go.AddComponent<Light>();
                sunLight.type    = LightType.Directional;
                sunLight.shadows = LightShadows.Soft;
                sunLight.intensity = 1.35f;
            }
            sunBaseIntensity  = sunLight.intensity;
            RenderSettings.sun = sunLight;

            // ── Cuerpos celestes ──────────────────────────────────────────
            if (earthPrefab != null)
                earthPivot = BuildBody("Tierra", earthPrefab, EARTH_RADIUS);
            else
                Debug.LogWarning("[OrbitalEnvironment] Falta asignar earthPrefab (modelo de la Tierra).", this);

            if (moonPrefab != null)
                moonPivot = BuildBody("Luna", moonPrefab, MOON_RADIUS);
            else
                Debug.LogWarning("[OrbitalEnvironment] Falta asignar moonPrefab (modelo de la Luna).", this);

            sunPivot = BuildSun();

            // Estado inicial
            spinAngle = startSpinDeg;
            Apply(0f);
        }

        private void LateUpdate()
        {
            Apply(Time.deltaTime * timeScale);
        }

        private void OnDestroy()
        {
            if (earthPivot != null) Destroy(earthPivot.gameObject);
            if (moonPivot  != null) Destroy(moonPivot.gameObject);
            if (sunPivot   != null) Destroy(sunPivot.gameObject);
        }

        // ════════════════════════════════════════════════════════════════════
        // SIMULACIÓN ORBITAL
        // ════════════════════════════════════════════════════════════════════

        private void Apply(float dt)
        {
            orbitAngle = (orbitAngle + 360f / ISS_PERIOD_S * dt) % 360f;
            spinAngle  = (spinAngle  + 360f / SIDEREAL_DAY * dt) % 360f;

            Vector3 origin = referencePoint != null ? referencePoint.position : Vector3.zero;
            Vector3 normal = orbitNormal.normalized;

            // Vector del centro de la Tierra respecto a la ISS
            Vector3 toEarth = Vector3.down * (EARTH_RADIUS + issAltitudeKm * KM);
            Vector3 earthCenter = origin + toEarth;

            // Rotación orbital: el cielo inercial gira respecto a la estación
            Quaternion orbitRot = Quaternion.AngleAxis(-orbitAngle, normal);

            // ── Tierra ────────────────────────────────────────────────────
            if (earthPivot != null)
            {
                Vector3 travel = Vector3.Cross(normal, Vector3.up);
                if (travel.sqrMagnitude < 1e-4f) travel = Vector3.forward;
                Vector3 spinAxis = Quaternion.AngleAxis(inclinationDeg, travel.normalized) * normal;

                earthPivot.position = earthCenter;
                earthPivot.rotation = orbitRot
                                    * Quaternion.FromToRotation(Vector3.up, spinAxis)
                                    * Quaternion.AngleAxis(spinAngle, Vector3.up);
                earthPivot.localScale = Vector3.one;
            }

            // ── Sol ───────────────────────────────────────────────────────
            Vector3 sunVec  = toEarth + (orbitRot * sunDirection.normalized) * SUN_DIST;
            float   dSun    = sunVec.magnitude;
            Vector3 dirSun  = sunVec / dSun;
            PlaceDistant(sunPivot, origin, dirSun, dSun, sunDisplayDistance);

            if (sunLight != null)
            {
                sunLight.transform.rotation = Quaternion.LookRotation(-dirSun);
                hudSunVis = eclipseNight ? SunVisibility(toEarth, dirSun) : 1f;
                sunLight.intensity = sunBaseIntensity * hudSunVis;
            }

            // ── Luna ──────────────────────────────────────────────────────
            Vector3 moonVec = toEarth + (orbitRot * moonDirection.normalized) * MOON_DIST;
            float   dMoon   = moonVec.magnitude;
            Vector3 dirMoon = moonVec / dMoon;
            PlaceDistant(moonPivot, origin, dirMoon, dMoon, moonDisplayDistance);
            if (moonPivot != null)
                moonPivot.rotation = Quaternion.LookRotation(-dirMoon, Vector3.up)
                                   * Quaternion.Euler(moonFaceEuler);

            // ── Cielo (shader) ────────────────────────────────────────────
            // La rotación del skybox sigue el marco inercial para que las estrellas
            // parezcan fijas y el Sol pintado coincida con el Sol real.
            Shader.SetGlobalMatrix("_NovaSkyRotation",
                Matrix4x4.Rotate(Quaternion.Inverse(skyAlign) * Quaternion.Inverse(orbitRot)));
            Shader.SetGlobalVector("_NovaSunDir",
                new Vector4(dirSun.x, dirSun.y, dirSun.z, hudSunVis));

            hudAlt    = issAltitudeKm;
            hudMoonKm = dMoon / KM;
            hudSunKm  = dSun  / KM;
        }

        // ════════════════════════════════════════════════════════════════════
        // HELPERS
        // ════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Coloca un cuerpo lejano. Con compresión mantiene la dirección y tamaño
        /// angular reales pero lo acerca para evitar problemas de precisión float.
        /// </summary>
        private void PlaceDistant(Transform pivot, Vector3 origin, Vector3 dir, float trueDist, float displayDist)
        {
            if (pivot == null) return;
            float d = compressDistantBodies ? displayDist : trueDist;
            float k = d / trueDist;
            pivot.position   = origin + dir * d;
            pivot.localScale = Vector3.one * k;
        }

        /// <summary>
        /// 0 = eclipse total (noche), 1 = Sol visible.
        /// Incluye un gradiente ~1.2% para simular el amanecer/ocaso orbital.
        /// </summary>
        private static float SunVisibility(Vector3 toEarth, Vector3 dirSun)
        {
            float t = Vector3.Dot(toEarth, dirSun);
            if (t <= 0f) return 1f;
            float miss = (toEarth - dirSun * t).magnitude;
            return Mathf.Clamp01((miss - EARTH_RADIUS) / (EARTH_RADIUS * 0.012f));
        }

        private Transform BuildBody(string bodyName, GameObject prefab, float radiusMeters)
        {
            var pivot = new GameObject(bodyName).transform;
            pivot.position = Vector3.zero;

            var model = Instantiate(prefab, pivot).transform;
            model.localPosition = Vector3.zero;
            model.localRotation = Quaternion.identity;
            model.localScale    = Vector3.one;

            if (MeasureBounds(pivot, out Bounds b))
            {
                float r = Mathf.Max(b.extents.x, b.extents.y, b.extents.z);
                float s = radiusMeters / r;
                model.localScale    = Vector3.one * s;
                model.localPosition = -b.center * s;
            }
            else
            {
                Debug.LogWarning($"[OrbitalEnvironment] '{bodyName}' no tiene Renderers.", this);
            }

            Cleanup(pivot);
            return pivot;
        }

        private Transform BuildSun()
        {
            var pivot  = new GameObject("Sol (visual)").transform;
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "Disco solar";
            Destroy(sphere.GetComponent<Collider>());
            sphere.transform.SetParent(pivot, false);
            sphere.transform.localScale = Vector3.one * (SUN_RADIUS * 2f);

            var r  = sphere.GetComponent<Renderer>();
            Shader sh = Shader.Find("Universal Render Pipeline/Unlit")
                     ?? Shader.Find("Unlit/Color");
            if (sh != null)
            {
                var mat = new Material(sh);
                mat.color = new Color(1f, 0.96f, 0.82f);
                r.sharedMaterial = mat;
            }
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows    = false;
            return pivot;
        }

        /// <summary>Convierte un UV de panorama equirectangular a dirección 3D.</summary>
        private static Vector3 DirFromPanoramaUV(Vector2 uv)
        {
            float lon = (0.5f - uv.x) * 2f * Mathf.PI;
            float lat = (1f - uv.y) * Mathf.PI;
            return new Vector3(
                Mathf.Sin(lat) * Mathf.Cos(lon),
                Mathf.Cos(lat),
                Mathf.Sin(lat) * Mathf.Sin(lon));
        }

        private static bool MeasureBounds(Transform root, out Bounds bounds)
        {
            var rs = root.GetComponentsInChildren<Renderer>();
            bounds = new Bounds(root.position, Vector3.zero);
            if (rs.Length == 0) return false;
            bounds = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++) bounds.Encapsulate(rs[i].bounds);
            return true;
        }

        private static void Cleanup(Transform root)
        {
            foreach (var c in root.GetComponentsInChildren<Collider>())
                Destroy(c);
            foreach (var r in root.GetComponentsInChildren<Renderer>())
            {
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows    = false;
            }
        }

        // ════════════════════════════════════════════════════════════════════
        // HUD DEBUG
        // ════════════════════════════════════════════════════════════════════

        private void OnGUI()
        {
            if (!showDebugHud) return;
            string estado = hudSunVis < 0.05f ? "NOCHE (eclipse)"
                          : hudSunVis < 0.95f ? "AMANECER / OCASO"
                          : "DÍA";

            GUI.Label(new Rect(12, 12, 450, 100),
                $"Altitud ISS:  {hudAlt:F0} km\n" +
                $"Luna:         {hudMoonKm:N0} km\n" +
                $"Sol:          {hudSunKm:N0} km\n" +
                $"Estado:       {estado}   (×{timeScale})");
        }
    }
}
