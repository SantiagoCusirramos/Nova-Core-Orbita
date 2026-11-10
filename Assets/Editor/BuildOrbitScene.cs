#if UNITY_EDITOR
/// <summary>
/// Nova Core Órbita — Script de setup completo.
///
/// Nova Core → BUILD 01_Orbit — Espacio + ISS + Player
///
/// Qué hace en un solo clic:
///   1. Configura las texturas del skybox (Cubemap, Cylindrical, mipmaps).
///   2. Crea / actualiza los materiales del skybox (2K para Quest, 4K HDR para PC).
///   3. Abre / recrea la escena 01_Orbit.
///   4. Coloca la ISS_stationary.glb con materiales URP.
///   5. Crea el Player con OrbitFreeFly + VRMovement + XRPlayerRig.
///   6. Coloca la cámara encuadrando la ISS a ~180 m.
///   7. Configura la luz solar alineada con el Sol del skybox.
///   8. Crea el Global Volume con perfil de post-proceso.
///   9. Ajusta RenderSettings (skybox, ambient, fog).
/// </summary>

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class BuildOrbitScene
{
    // ── Rutas ─────────────────────────────────────────────────────────────
    const string SCENE_PATH   = "Assets/NovaCore/Scenes/01_Orbit.unity";
    const string SKY_DIR      = "Assets/NovaCore/Art/Skybox";
    const string MAT_DIR      = "Assets/NovaCore/Art/Materials";
    const string SETTINGS_DIR = "Assets/NovaCore/Settings";
    const string ISS_PATH     = "Assets/NovaCore/Art/ISS/ISS_stationary.glb";
    const string ISS_MAT_DIR  = "Assets/NovaCore/Art/ISS/Materials";

    const string TEX_2K  = SKY_DIR + "/T_Skybox_DeepSpace_2K.png";
    const string TEX_4K  = SKY_DIR + "/T_Skybox_DeepSpace_4K_LDR.png";
    const string TEX_HDR = SKY_DIR + "/T_Skybox_DeepSpace_4K.hdr";

    const string MAT_2K  = MAT_DIR + "/M_Skybox_DeepSpace.mat";
    const string MAT_HDR = MAT_DIR + "/M_Skybox_DeepSpace_HDR.mat";

    const string PROFILE_PATH = SETTINGS_DIR + "/NovaCore_OrbitProfile.asset";

    // ── Entrada ───────────────────────────────────────────────────────────
    [MenuItem("Nova Core/BUILD 01_Orbit — Espacio + ISS + Player")]
    public static void Build()
    {
        try
        {
            EditorUtility.DisplayProgressBar("Nova Core — Build 01_Orbit", "Preparando…", 0f);

            EnsureFolders();
            EditorUtility.DisplayProgressBar("Nova Core", "Configurando texturas skybox…", 0.1f);
            ConfigureTextures();

            EditorUtility.DisplayProgressBar("Nova Core", "Creando materiales skybox…", 0.25f);
            var skyMat = CreateSkyboxMaterials();

            EditorUtility.DisplayProgressBar("Nova Core", "Abriendo escena 01_Orbit…", 0.35f);
            var scene = OpenOrCreateScene();

            EditorUtility.DisplayProgressBar("Nova Core", "Limpiando objetos anteriores…", 0.40f);
            ClearScene();

            EditorUtility.DisplayProgressBar("Nova Core", "Creando luz solar…", 0.45f);
            var sun = CreateSun();

            EditorUtility.DisplayProgressBar("Nova Core", "Creando Player…", 0.55f);
            var cam = CreatePlayer();

            EditorUtility.DisplayProgressBar("Nova Core", "Instanciando ISS…", 0.65f);
            CreateISS();

            EditorUtility.DisplayProgressBar("Nova Core", "Creando Global Volume…", 0.75f);
            CreateVolume();

            EditorUtility.DisplayProgressBar("Nova Core", "Ajustando RenderSettings…", 0.85f);
            ApplyRenderSettings(skyMat, sun);

            EditorUtility.DisplayProgressBar("Nova Core", "Guardando escena…", 0.95f);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AddToBuildSettings(SCENE_PATH);

            EditorUtility.ClearProgressBar();

            Debug.Log(
                "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n" +
                "✅  01_Orbit lista para probar\n" +
                "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n\n" +
                "JERARQUÍA:\n" +
                "  Sun              Directional Light (alineada con el skybox)\n" +
                "  Player           OrbitFreeFly + VRMovement + XRPlayerRig\n" +
                "  └─ Main Camera\n" +
                "  ISS              ISS_stationary.glb (estática)\n" +
                "  Global Volume    Post-proceso URP\n\n" +
                "CONTROLES:\n" +
                "  WASD + clic derecho → mover + rotar (desktop)\n" +
                "  E/Q o rueda        → subir/bajar / velocidad\n" +
                "  Thumbstick Quest   → mover / snap turn\n\n" +
                "SI LA ISS APARECE MAGENTA:\n" +
                "  Nova Core → Convert ISS Materials (URP)\n"
            );
        }
        catch (Exception e)
        {
            EditorUtility.ClearProgressBar();
            Debug.LogError("[BuildOrbitScene] Error: " + e.Message + "\n" + e.StackTrace);
        }
    }

    // ════════════════════════════════════════════════════════════════════
    // 1. CARPETAS
    // ════════════════════════════════════════════════════════════════════

    static void EnsureFolders()
    {
        foreach (var path in new[] { SKY_DIR, MAT_DIR, SETTINGS_DIR, ISS_MAT_DIR })
            EnsureFolder(path);
    }

    static void EnsureFolder(string path)
    {
        var parts   = path.Split('/');
        var current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            var next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    // ════════════════════════════════════════════════════════════════════
    // 2. TEXTURAS
    // ════════════════════════════════════════════════════════════════════

    static void ConfigureTextures()
    {
        // 2K PNG → Cubemap (Cylindrical / lat-lon) para el skybox principal
        ConfigureCubemap(TEX_2K, 2048, false, TextureImporterCompression.CompressedHQ);
        // 4K LDR PNG → Cubemap alternativo
        ConfigureCubemap(TEX_4K, 2048, false, TextureImporterCompression.CompressedHQ);
        // 4K HDR → Cubemap HDR de alta calidad
        ConfigureCubemap(TEX_HDR, 2048, false, TextureImporterCompression.Uncompressed);
    }

    static void ConfigureCubemap(string path, int maxSize, bool sRGB, TextureImporterCompression compression)
    {
        if (!System.IO.File.Exists(
                System.IO.Path.Combine(Application.dataPath, "..", path).Replace('/', '\\')))
        {
            Debug.LogWarning("[BuildOrbitScene] Textura no encontrada: " + path);
            return;
        }

        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            importer = AssetImporter.GetAtPath(path) as TextureImporter;
        }
        if (importer == null) { Debug.LogWarning("[BuildOrbitScene] No hay importer para: " + path); return; }

        bool dirty = false;
        if (importer.textureType       != TextureImporterType.Default)                        { importer.textureType       = TextureImporterType.Default;                       dirty = true; }
        if (importer.textureShape      != TextureImporterShape.TextureCube)                   { importer.textureShape      = TextureImporterShape.TextureCube;                  dirty = true; }
        if (importer.generateCubemap   != TextureImporterGenerateCubemap.Cylindrical)         { importer.generateCubemap   = TextureImporterGenerateCubemap.Cylindrical;         dirty = true; }
        if (importer.maxTextureSize    != maxSize)                                            { importer.maxTextureSize    = maxSize;                                           dirty = true; }
        if (importer.textureCompression!= compression)                                        { importer.textureCompression= compression;                                       dirty = true; }
        if (importer.sRGBTexture       != sRGB)                                               { importer.sRGBTexture       = sRGB;                                              dirty = true; }
        if (!importer.mipmapEnabled)                                                          { importer.mipmapEnabled     = true;                                              dirty = true; }
        if (importer.wrapMode          != TextureWrapMode.Clamp)                              { importer.wrapMode          = TextureWrapMode.Clamp;                             dirty = true; }
        if (importer.filterMode        != FilterMode.Trilinear)                               { importer.filterMode        = FilterMode.Trilinear;                              dirty = true; }

        if (dirty) importer.SaveAndReimport();
    }

    // ════════════════════════════════════════════════════════════════════
    // 3. MATERIALES SKYBOX
    // ════════════════════════════════════════════════════════════════════

    static Material CreateSkyboxMaterials()
    {
        // Shader disponible en todos los proyectos URP
        var shader = Shader.Find("Skybox/Cubemap");
        if (shader == null) shader = Shader.Find("Skybox/6 Sided");
        if (shader == null)
        {
            Debug.LogError("[BuildOrbitScene] No se encontró ningún shader de Skybox.");
            return null;
        }

        var cube2k  = AssetDatabase.LoadAssetAtPath<Cubemap>(TEX_2K);
        var cubeHdr = AssetDatabase.LoadAssetAtPath<Cubemap>(TEX_HDR);

        // Material principal (2K, para Quest y preview rápido)
        var mat2k = GetOrCreateMaterial(MAT_2K, shader);
        if (cube2k != null)
        {
            mat2k.SetTexture("_Tex", cube2k);
            mat2k.SetColor("_Tint", Color.white);
            mat2k.SetFloat("_Exposure", 1.2f);
            mat2k.SetFloat("_Rotation", 0f);
            EditorUtility.SetDirty(mat2k);
        }
        else
            Debug.LogWarning("[BuildOrbitScene] Cubemap 2K no disponible aún (Unity puede tardar en reimportar). Vuelve a ejecutar si el skybox aparece negro.");

        // Material HDR (para PC)
        if (cubeHdr != null)
        {
            var matHdr = GetOrCreateMaterial(MAT_HDR, shader);
            matHdr.SetTexture("_Tex", cubeHdr);
            matHdr.SetColor("_Tint", Color.white);
            matHdr.SetFloat("_Exposure", 1.0f);
            matHdr.SetFloat("_Rotation", 0f);
            EditorUtility.SetDirty(matHdr);
        }

        AssetDatabase.SaveAssets();
        return mat2k;
    }

    static Material GetOrCreateMaterial(string path, Shader shader)
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(shader) { name = System.IO.Path.GetFileNameWithoutExtension(path) };
            AssetDatabase.CreateAsset(mat, path);
        }
        else
        {
            mat.shader = shader;
        }
        return mat;
    }

    // ════════════════════════════════════════════════════════════════════
    // 4. ESCENA
    // ════════════════════════════════════════════════════════════════════

    static Scene OpenOrCreateScene()
    {
        var current = SceneManager.GetActiveScene();
        if (current.path == SCENE_PATH && current.isLoaded) return current;

        if (System.IO.File.Exists(
                System.IO.Path.Combine(Application.dataPath, "..", SCENE_PATH).Replace('/', '\\')))
            return EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
        else
            return EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    }

    static void ClearScene()
    {
        string[] names = { "Sun", "Directional Light", "Player", "Main Camera",
                           "Global Volume", "Orbital Environment", "ISS" };
        foreach (var n in names)
        {
            var go = GameObject.Find(n);
            if (go != null) UnityEngine.Object.DestroyImmediate(go);
        }
    }

    // ════════════════════════════════════════════════════════════════════
    // 5. LUZ SOLAR
    // ════════════════════════════════════════════════════════════════════

    // Sol del panorama deep_space_combined está en UV (0.4221, 0.4338)
    // → ángulo de elevación ~11.9° y azimut ~151.8° (medido del README del skybox)
    static GameObject CreateSun()
    {
        var go  = new GameObject("Sun");
        var l   = go.AddComponent<Light>();
        l.type      = LightType.Directional;
        l.intensity = 1.35f;
        l.color     = new Color(1f, 0.97f, 0.9f);
        l.shadows   = LightShadows.Soft;
        go.transform.rotation = Quaternion.Euler(11.9f, 151.8f, 0f);

        // URP additional data
        var urd = go.AddComponent<UniversalAdditionalLightData>();
        return go;
    }

    // ════════════════════════════════════════════════════════════════════
    // 6. PLAYER
    // ════════════════════════════════════════════════════════════════════

    static Camera CreatePlayer()
    {
        // El Player se coloca a 180 m frente a la ISS (que está en el origen)
        var playerGO = new GameObject("Player");
        playerGO.transform.SetPositionAndRotation(
            new Vector3(0f, 1.6f, -180f),
            Quaternion.LookRotation(Vector3.forward, Vector3.up));

        // Movimiento desktop — modo creativo sin gravedad
        var fly = playerGO.AddComponent<NovaCore.OrbitFreeFly>();
        fly.moveSpeed = 25f;

        // Movimiento VR — se combina con TrackedPoseDriver en la cámara
        playerGO.AddComponent<NovaCore.VRMovement>();

        // Rig XR runtime — crea TrackedPoseDrivers para cabeza y mandos al iniciar en VR
        playerGO.AddComponent<NovaCore.XRPlayerRig>();

        // Switcher espectador/interactivo (empieza en modo espectador/vuelo libre)
        var sw = playerGO.AddComponent<NovaCore.PlayerModeSwitcher>();
        sw.initialMode = NovaCore.PlayerModeSwitcher.PlayerMode.Spectator;

        // ── Cámara hija ────────────────────────────────────────────────
        var camGO = new GameObject("Main Camera");
        camGO.transform.SetParent(playerGO.transform, false);
        camGO.transform.localPosition = Vector3.zero;
        camGO.transform.localRotation = Quaternion.identity;

        var cam = camGO.AddComponent<Camera>();
        cam.tag           = "MainCamera";
        cam.nearClipPlane = 0.05f;
        cam.farClipPlane  = 2_000_000_000f;   // 2 Gm — suficiente para cuerpos celestes comprimidos
        cam.fieldOfView   = 90f;
        cam.clearFlags    = CameraClearFlags.Skybox;
        cam.allowHDR      = true;
        cam.allowMSAA     = true;

        camGO.AddComponent<UniversalAdditionalCameraData>();
        camGO.AddComponent<AudioListener>();

        return cam;
    }

    // ════════════════════════════════════════════════════════════════════
    // 7. ISS
    // ════════════════════════════════════════════════════════════════════

    static void CreateISS()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ISS_PATH);
        if (prefab == null)
        {
            Debug.LogWarning("[BuildOrbitScene] No se encontró la ISS en: " + ISS_PATH +
                             "\nAsegúrate de que ISS_stationary.glb está en Assets/NovaCore/Art/ISS/");
            return;
        }

        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.name = "ISS";
        // El centro del modelo está en (0, 5.4, -3.8) aprox, así que offset para centrarlo
        go.transform.SetPositionAndRotation(new Vector3(0f, 0f, 3.8f), Quaternion.identity);
        go.isStatic = true;

        // Marcar todos los hijos como estáticos también
        foreach (Transform child in go.GetComponentsInChildren<Transform>(true))
            child.gameObject.isStatic = true;

        Debug.Log("[BuildOrbitScene] ISS instanciada (" +
                  go.GetComponentsInChildren<Renderer>().Length + " renderers).\n" +
                  "Si aparece magenta → Nova Core → Convert ISS Materials (URP)");
    }

    // ════════════════════════════════════════════════════════════════════
    // 8. GLOBAL VOLUME
    // ════════════════════════════════════════════════════════════════════

    static void CreateVolume()
    {
        // Reusar perfil existente si ya fue creado por SetupEnvironment
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(PROFILE_PATH);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, PROFILE_PATH);

            // Tonemapping
            var tone = profile.Add<Tonemapping>();
            tone.mode.overrideState = true;
            tone.mode.value         = TonemappingMode.Neutral;

            // Bloom suave
            var bloom = profile.Add<Bloom>();
            bloom.threshold.overrideState           = true; bloom.threshold.value           = 0.9f;
            bloom.intensity.overrideState           = true; bloom.intensity.value           = 0.35f;
            bloom.scatter.overrideState             = true; bloom.scatter.value             = 0.6f;
            bloom.highQualityFiltering.overrideState= true; bloom.highQualityFiltering.value= false;
            bloom.downscale.overrideState           = true; bloom.downscale.value           = BloomDownscaleMode.Quarter;

            // Ajustes de color
            var color = profile.Add<ColorAdjustments>();
            color.postExposure.overrideState = true; color.postExposure.value = 0.25f;
            color.contrast.overrideState     = true; color.contrast.value     = 5f;
            color.saturation.overrideState   = true; color.saturation.value   = 5f;

            // Viñeta ligera
            var vig = profile.Add<Vignette>();
            vig.intensity.overrideState  = true; vig.intensity.value  = 0.15f;
            vig.smoothness.overrideState = true; vig.smoothness.value = 0.4f;

            foreach (var comp in profile.components)
                if (!AssetDatabase.Contains(comp))
                    AssetDatabase.AddObjectToAsset(comp, profile);

            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
        }

        var volGO = new GameObject("Global Volume");
        var vol   = volGO.AddComponent<Volume>();
        vol.isGlobal      = true;
        vol.priority      = 0f;
        vol.sharedProfile = profile;
    }

    // ════════════════════════════════════════════════════════════════════
    // 9. RENDER SETTINGS
    // ════════════════════════════════════════════════════════════════════

    static void ApplyRenderSettings(Material skyMat, GameObject sunGO)
    {
        if (skyMat != null) RenderSettings.skybox = skyMat;

        RenderSettings.ambientMode  = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.035f, 0.04f, 0.055f);  // oscuridad de espacio
        RenderSettings.fog          = false;
        RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
        RenderSettings.reflectionIntensity   = 0.35f;

        if (sunGO != null)
            RenderSettings.sun = sunGO.GetComponent<Light>();
    }

    // ════════════════════════════════════════════════════════════════════
    // 10. BUILD SETTINGS
    // ════════════════════════════════════════════════════════════════════

    static void AddToBuildSettings(string path)
    {
        var scenes = EditorBuildSettings.scenes.ToList();
        if (scenes.Any(s => s.path == path)) return;
        scenes.Insert(0, new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
#endif
