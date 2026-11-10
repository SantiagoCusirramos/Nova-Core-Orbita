#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace NovaCore.Editor
{
    /// <summary>
    /// Reconstruye 01_Orbit con:
    ///   - Skybox deep space
    ///   - Luz solar alineada
    ///   - ISS_stationary.glb en la escena (si ya está importado)
    ///   - Player con OrbitFreeFly + VRMovement + XRPlayerRig
    ///   - Global Volume URP
    ///   - OrbitalEnvironment listo para Tierra / Luna
    ///
    /// Nova Core → Setup 01_Orbit (Espacio + ISS + Player)
    /// </summary>
    public static class SetupSpaceEnvironment
    {
        const string ISS_ASSET = "Assets/NovaCore/Art/ISS/ISS_stationary.glb";
        const string SKY_MAT_A = "Assets/NovaCore/Art/Materials/M_Skybox_DeepSpace.mat";
        const string SKY_MAT_B = "Assets/NovaCore/Art/Skybox/M_Skybox_DeepSpace.mat";
        const string SKY_TEX_A = "Assets/NovaCore/Art/Skybox/T_Skybox_DeepSpace_2K.png";
        const string SKY_TEX_B = "Assets/NovaCore/Art/Skybox/deep_space_combined_2k.png";
        const string SKY_TEX_C = "Assets/NovaCore/Art/Skybox/deep_space_combined_4k.png";

        [MenuItem("Nova Core/Setup 01_Orbit (Espacio + ISS + Player)")]
        public static void Run()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.isLoaded)
            {
                Debug.LogError("[Setup01Orbit] No hay escena cargada. Abre 01_Orbit primero.");
                return;
            }

            // ── Limpiar objetos que se van a recrear ─────────────────────
            string[] toRemove = { "Sun", "Player", "Global Volume", "Orbital Environment", "ISS" };
            foreach (var n in toRemove) DestroyIfExists(n);

            // ── 1. Luz solar ──────────────────────────────────────────────
            // El Sol en el panorama deep_space_combined está en euler (11.9°, 151.8°, 0°)
            var sunGO = new GameObject("Sun");
            var sun   = sunGO.AddComponent<Light>();
            sun.type      = LightType.Directional;
            sun.intensity = 1.35f;
            sun.color     = new Color(1f, 0.97f, 0.9f);
            sun.shadows   = LightShadows.Soft;
            sunGO.transform.rotation = Quaternion.Euler(11.9f, 151.8f, 0f);
            RenderSettings.sun       = sun;
            Undo.RegisterCreatedObjectUndo(sunGO, "Create Sun");

            // ── 2. Skybox ─────────────────────────────────────────────────
            var skyMat = AssetDatabase.LoadAssetAtPath<Material>(SKY_MAT_A)
                      ?? AssetDatabase.LoadAssetAtPath<Material>(SKY_MAT_B);

            if (skyMat != null)
            {
                RenderSettings.skybox = skyMat;
                Debug.Log("[Setup01Orbit] Skybox asignado: " + skyMat.name);
            }
            else
            {
                Debug.LogWarning("[Setup01Orbit] ⚠ No se encontró M_Skybox_DeepSpace.mat.\n" +
                    "Pasos para crearlo:\n" +
                    "  1. Extrae 'deep_space_combined_2k.png' del zip en Recursos/ y cópialo a\n" +
                    "     Assets/NovaCore/Art/Skybox/\n" +
                    "  2. Create > Material → shader 'Skybox/Cubemap'\n" +
                    "     (o 'NovaCore/SpacePanorama' si quieres animación orbital)\n" +
                    "  3. Asígnalo en Window > Rendering > Lighting > Environment > Skybox Material");
            }

            // Luz ambiental mínima para que la ISS no quede 100% negra en la sombra
            RenderSettings.ambientMode  = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.035f, 0.04f, 0.055f);

            // ── 3. Player ─────────────────────────────────────────────────
            // Punto de observación a 180 m frente a la ISS (que estará en origen)
            var playerGO = new GameObject("Player");
            playerGO.transform.SetPositionAndRotation(
                new Vector3(0f, 1.6f, -180f),
                Quaternion.LookRotation(Vector3.forward));     // mirando hacia la ISS

            // Movimiento desktop
            var freeFly = playerGO.AddComponent<OrbitFreeFly>();
            freeFly.moveSpeed = 25f;

            // Movimiento VR (TrackedPoseDriver en cámara hija)
            playerGO.AddComponent<VRMovement>();

            // Rig XR runtime (mandos, floor tracking)
            playerGO.AddComponent<XRPlayerRig>();

            // Switcher espectador / interactivo
            var switcher = playerGO.AddComponent<PlayerModeSwitcher>();
            switcher.initialMode = PlayerModeSwitcher.PlayerMode.Spectator;

            // Cámara hija
            var camGO = new GameObject("Main Camera");
            camGO.transform.SetParent(playerGO.transform, false);
            camGO.transform.localPosition = Vector3.zero;
            camGO.transform.localRotation = Quaternion.identity;

            var cam = camGO.AddComponent<Camera>();
            cam.tag           = "MainCamera";
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane  = 2e9f;
            cam.fieldOfView   = 90f;
            cam.clearFlags    = CameraClearFlags.Skybox;
            cam.allowHDR      = true;
            camGO.AddComponent<AudioListener>();

            Undo.RegisterCreatedObjectUndo(playerGO, "Create Player");

            // ── 4. ISS ────────────────────────────────────────────────────
            var issPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ISS_ASSET);
            if (issPrefab != null)
            {
                // Instanciar ISS en el origen; a 180 m el Player ya la encuadra bien
                var issGO = (GameObject)PrefabUtility.InstantiatePrefab(issPrefab);
                issGO.name = "ISS";
                issGO.transform.SetPositionAndRotation(
                    new Vector3(0f, 0f, 3.8f),   // offset del pivote del modelo
                    Quaternion.identity);
                issGO.isStatic = true;

                Undo.RegisterCreatedObjectUndo(issGO, "Place ISS");
                Debug.Log("[Setup01Orbit] ISS instanciada en la escena (247k tris, estática).");

                // Si hay materiales magenta → ejecutar ConvertIssMaterials desde el menú
                Debug.Log("[Setup01Orbit] Si los materiales aparecen magenta, usa:\n" +
                          "  Nova Core → Convert ISS Materials (URP)");
            }
            else
            {
                Debug.LogWarning("[Setup01Orbit] ⚠ No se encontró ISS_stationary.glb en:\n  " +
                    ISS_ASSET + "\nVerifica que el archivo esté importado en el proyecto.");
            }

            // ── 5. Global Volume ──────────────────────────────────────────
            var volGO = new GameObject("Global Volume");
            var vol   = volGO.AddComponent<UnityEngine.Rendering.Volume>();
            vol.isGlobal = true;
            Undo.RegisterCreatedObjectUndo(volGO, "Create Global Volume");

            // Intentar conectar perfil si ya existe
            var profile = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.VolumeProfile>(
                "Assets/NovaCore/Settings/NovaCore_OrbitProfile.asset");
            if (profile != null)
            {
                vol.sharedProfile = profile;
                Debug.Log("[Setup01Orbit] NovaCore_OrbitProfile conectado al Global Volume.");
            }
            else
            {
                Debug.LogWarning("[Setup01Orbit] ⚠ Asigna NovaCore_OrbitProfile al Global Volume (inspector).");
            }

            // ── 6. OrbitalEnvironment ─────────────────────────────────────
            var envGO = new GameObject("Orbital Environment");
            var env   = envGO.AddComponent<OrbitalEnvironment>();
            env.sunLight     = sun;
            env.mainCamera   = cam;
            env.timeScale    = 1f;
            env.showDebugHud = true;

            // Asignar textura panorama si ya está importada
            var panoTex = AssetDatabase.LoadAssetAtPath<Texture2D>(SKY_TEX_A)
                       ?? AssetDatabase.LoadAssetAtPath<Texture2D>(SKY_TEX_B)
                       ?? AssetDatabase.LoadAssetAtPath<Texture2D>(SKY_TEX_C);
            if (panoTex != null)
            {
                env.spacePanorama = panoTex;
                Debug.Log("[Setup01Orbit] Panorama asignado: " + panoTex.name);
            }
            if (skyMat != null) env.spaceSkybox = skyMat;

            Undo.RegisterCreatedObjectUndo(envGO, "Create Orbital Environment");

            // ── 7. Guardar ────────────────────────────────────────────────
            EditorSceneManager.MarkSceneDirty(scene);

            Debug.Log("[Setup01Orbit] ✅ Escena configurada.\n\n" +
                "JERARQUÍA:\n" +
                "  Sun              ← Directional Light (11.9°, 151.8°, 0°)\n" +
                "  Player           ← OrbitFreeFly + VRMovement + XRPlayerRig + PlayerModeSwitcher\n" +
                "  └─ Main Camera   ← Tag=MainCamera, FOV=90, far=2e9\n" +
                "  ISS              ← ISS_stationary.glb, Static, (0, 0, 3.8)\n" +
                "  Global Volume    ← Volume isGlobal\n" +
                "  Orbital Environment ← OrbitalEnvironment.cs\n\n" +
                "PRÓXIMOS PASOS:\n" +
                "  1. Si falta skybox: extrae deep_space_combined_2k.png de Recursos/ zip\n" +
                "     → Assets/NovaCore/Art/Skybox/ → crea material con shader Skybox/Cubemap\n" +
                "  2. Si materiales ISS son magenta: Nova Core → Convert ISS Materials (URP)\n" +
                "  3. Pulsa Play → muévete con WASD + clic derecho (desktop)\n" +
                "     o con thumbstick izquierdo del Quest 2 (VR)\n" +
                "  4. Para Tierra/Luna: arrastra los modelos al OrbitalEnvironment en inspector");
        }

        // ────────────────────────────────────────────────────────────────────

        static void DestroyIfExists(string objectName)
        {
            var go = GameObject.Find(objectName);
            if (go == null) return;
            Undo.DestroyObjectImmediate(go);
            Debug.Log($"[Setup01Orbit] Eliminado: '{objectName}'");
        }
    }
}
#endif
