#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using NovaCore;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Nova Core → BUILD 02_Interior — Interior ISS + Misiones
///
/// Construye (y sobrescribe) la escena Assets/Game/Scenes/02_Interior.unity:
///   • Módulos emulados de la ISS a escala real (1 u = 1 m), decorados:
///       Nodo 1 Unity (hub) · Destiny (laboratorio) · Zvezda (servicio) ·
///       Quest (esclusa) · Cúpula — con racks, pasamanos, portátiles, bolsas CTB,
///       cables, luces, señalización y casco exterior.
///   • Exterior EVA: celosía, ala de paneles solares con avería, Tierra de fondo.
///   • Bahía robótica (visible por la ventana de Zvezda) con brazo telescópico.
///   • Los 16 objetivos de las 2 misiones cableados al Core (MissionManager).
///   • Las 4 mesas de reparación y herramientas portadas de NASA-SPACE-APPS (sin XRI).
///   • Player con OrbitFreeFly (con colisiones) + XRPlayerRig + NovaInteractionRig.
///
/// Además añade una consola "Entrar a la ISS" en 01_Orbit (sin tocar el Player salvo
/// añadir NovaInteractionRig) y registra ambas escenas en Build Settings.
/// Es idempotente: se puede ejecutar varias veces.
/// </summary>
public static class BuildInteriorScene
{
    const string ScenePath   = "Assets/Game/Scenes/02_Interior.unity";
    const string OrbitScene  = "Assets/NovaCore/Scenes/01_Orbit.unity";
    const string ArtDir      = "Assets/Game/Art/Interiors";
    const string MatDir      = ArtDir + "/Materials";
    const string MeshDir     = ArtDir + "/Meshes";
    const string TexDir      = ArtDir + "/Textures";
    const string ToolDir     = "Assets/Game/Art/Tools";
    const string SkyboxMat   = "Assets/NovaCore/Art/Materials/M_Skybox_DeepSpace.mat";
    const string MissionEnergy = "Assets/Game/Data/Missions/Mission_Energy.asset";
    const string MissionSolar  = "Assets/Game/Data/Missions/Mission_SolarPanel.asset";

    // Dimensiones comunes (metros)
    const float H      = 2.4f;   // altura interior de módulo
    const float WallT  = 0.06f;  // grosor de pared interior
    const float HullOff = 0.35f; // separación del casco exterior
    static readonly Vector2 HatchSize = new Vector2(1.3f, 1.5f);

    // Materiales
    static Material mWall, mFloor, mRack, mRackDark, mPanel, mHandrail, mMetal, mHull, mHullGold,
                    mLight, mScreen, mRed, mGreen, mAmber, mCyan, mCable, mBag, mGlass, mSolar,
                    mEarth, mAlarm, mOru, mPlant, mPlantLight, mSuit, mVisor, mBlackPanel,
                    mParticleAdd, mParticleMist, mLaser, mMarker, mController, mFrame;
    static Material mToolPGT, mToolHammer, mToolRatchet, mToolGrease;

    static System.Random rng;
    static Transform decorRoot;
    static readonly List<string> report = new List<string>();

    // ════════════════════════════════════════════════════════════════════════
    // MENÚ
    // ════════════════════════════════════════════════════════════════════════

    [MenuItem("Nova Core/BUILD 02_Interior — Interior ISS + Misiones")]
    public static void BuildFromMenu()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        string r = Build();
        EditorUtility.DisplayDialog("Nova Core — 02_Interior", r, "OK");
    }

    public static string Build()
    {
        report.Clear();
        rng = new System.Random(20261114);

        EnsureFolders();
        ConfigureTextures();
        CreateMaterials();

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        SetupEnvironment();
        var stationRoot = new GameObject("ISS Interior").transform;
        decorRoot = new GameObject("Decor").transform;
        decorRoot.SetParent(stationRoot, false);

        var systems = BuildSystems();
        var mm = systems.GetComponent<MissionManager>();

        BuildModules(stationRoot);
        var player = BuildPlayer(out PlayerSpawnBay spawnBay);
        BuildMissionContent(stationRoot, systems.transform, spawnBay, player);
        BuildExterior(stationRoot);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        report.Add("Escena guardada: " + ScenePath);

        AddPortalToOrbit();
        NovaProjectSetup.RegisterScenes();

        // Volver a dejar abierta la escena de interior
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        AssetDatabase.SaveAssets();

        string msg = "02_Interior construida.\n\n" + string.Join("\n", report) +
                     "\n\nPrueba: Play en 02_Interior → pulsa INICIAR (panel verde del Nodo).";
        Debug.Log("[BuildInteriorScene]\n" + msg);
        return msg;
    }

    // ════════════════════════════════════════════════════════════════════════
    // ASSETS
    // ════════════════════════════════════════════════════════════════════════

    static void EnsureFolders()
    {
        foreach (var p in new[] { "Assets/Game/Scenes", ArtDir, MatDir, MeshDir, TexDir, ToolDir, ToolDir + "/Materials" })
            EnsureFolder(p);
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }

    static void ConfigureTextures()
    {
        SetNormalMap(TexDir + "/T_Interior_Floor_Normal.png");
        SetNormalMap(TexDir + "/T_Interior_Wall_Normal.png");
        var earth = AssetImporter.GetAtPath(TexDir + "/T_Planet_Earth_Albedo.png") as TextureImporter;
        if (earth != null && earth.maxTextureSize != 2048)
        {
            earth.maxTextureSize = 2048;
            earth.SaveAndReimport();
        }
    }

    static void SetNormalMap(string path)
    {
        var ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti == null || ti.textureType == TextureImporterType.NormalMap) return;
        ti.textureType = TextureImporterType.NormalMap;
        ti.SaveAndReimport();
    }

    static Texture2D Tex(string file) => AssetDatabase.LoadAssetAtPath<Texture2D>(TexDir + "/" + file);

    static void CreateMaterials()
    {
        var wallA = Tex("T_Interior_Wall_Albedo.png");  var wallN = Tex("T_Interior_Wall_Normal.png");
        var floorA = Tex("T_Interior_Floor_Albedo.png"); var floorN = Tex("T_Interior_Floor_Normal.png");

        mWall      = Lit("M_ISS_Wall",       new Color(0.86f, 0.85f, 0.80f), 0.35f, 0f, wallA, wallN, new Vector2(2, 1));
        mFloor     = Lit("M_ISS_Deck",       new Color(0.72f, 0.72f, 0.70f), 0.30f, 0.1f, floorA, floorN, new Vector2(2, 2));
        mRack      = Lit("M_ISS_Rack",       new Color(0.90f, 0.89f, 0.84f), 0.30f, 0f, wallA, null, Vector2.one);
        mRackDark  = Lit("M_ISS_RackDark",   new Color(0.55f, 0.56f, 0.56f), 0.40f, 0.2f);
        mPanel     = Lit("M_ISS_Panel",      new Color(0.78f, 0.80f, 0.82f), 0.45f, 0.1f);
        mBlackPanel= Lit("M_ISS_PanelBlack", new Color(0.10f, 0.11f, 0.13f), 0.55f, 0.1f);
        mHandrail  = Lit("M_ISS_Handrail",   new Color(0.25f, 0.42f, 0.72f), 0.50f, 0.2f);
        mMetal     = Lit("M_ISS_Metal",      new Color(0.62f, 0.64f, 0.66f), 0.70f, 0.8f);
        mFrame     = Lit("M_ISS_HatchFrame", new Color(0.70f, 0.71f, 0.72f), 0.60f, 0.6f);
        mHull      = Lit("M_ISS_HullWhite",  new Color(0.93f, 0.93f, 0.90f), 0.25f, 0f);
        mHullGold  = Lit("M_ISS_HullGold",   new Color(0.85f, 0.65f, 0.25f), 0.75f, 0.9f);
        mCable     = Lit("M_ISS_Cable",      new Color(0.15f, 0.15f, 0.16f), 0.30f, 0f);
        mBag       = Lit("M_ISS_CTBBag",     new Color(0.86f, 0.82f, 0.70f), 0.15f, 0f);
        mOru       = Lit("M_ISS_EnergyORU",  new Color(0.85f, 0.70f, 0.15f), 0.50f, 0.4f, null, null, Vector2.one, new Color(0.25f, 0.18f, 0.02f));
        mSuit      = Lit("M_ISS_EMUSuit",    new Color(0.95f, 0.95f, 0.93f), 0.20f, 0f);
        mVisor     = Lit("M_ISS_Visor",      new Color(0.85f, 0.62f, 0.20f), 0.95f, 1f);
        mPlant     = Lit("M_ISS_Plant",      new Color(0.20f, 0.60f, 0.20f), 0.30f, 0f);

        mLight     = Lit("M_ISS_LightPanel", new Color(1f, 0.97f, 0.90f), 0.2f, 0f, null, null, Vector2.one, new Color(1f, 0.95f, 0.85f) * 1.6f);
        mScreen    = Lit("M_ISS_Screen",     new Color(0.05f, 0.12f, 0.18f), 0.8f, 0f, null, null, Vector2.one, new Color(0.25f, 0.75f, 1f) * 1.2f, Tex("T_Tech_Screen_Emission.png"));
        mRed       = Lit("M_ISS_LED_Red",    new Color(1f, 0.15f, 0.1f), 0.5f, 0f, null, null, Vector2.one, new Color(1f, 0.12f, 0.08f) * 2f);
        mGreen     = Lit("M_ISS_LED_Green",  new Color(0.2f, 1f, 0.45f), 0.5f, 0f, null, null, Vector2.one, new Color(0.15f, 1f, 0.35f) * 2f);
        mAmber     = Lit("M_ISS_LED_Amber",  new Color(1f, 0.6f, 0.1f), 0.5f, 0f, null, null, Vector2.one, new Color(1f, 0.55f, 0.1f) * 2f);
        mCyan      = Lit("M_ISS_LED_Cyan",   new Color(0.2f, 0.8f, 1f), 0.5f, 0f, null, null, Vector2.one, new Color(0.2f, 0.8f, 1f) * 2f);
        mAlarm     = Lit("M_ISS_AlarmStrip", new Color(0.3f, 0.05f, 0.05f), 0.4f, 0f, null, null, Vector2.one, new Color(0.15f, 0.02f, 0.02f));
        mPlantLight= Lit("M_ISS_VeggieLight",new Color(0.9f, 0.3f, 0.8f), 0.4f, 0f, null, null, Vector2.one, new Color(1f, 0.25f, 0.85f) * 1.5f);
        mMarker    = Lit("M_ISS_ObjectiveMarker", new Color(0.2f, 0.7f, 1f), 0.5f, 0f, null, null, Vector2.one, new Color(0.2f, 0.75f, 1f) * 3f);
        mController= Lit("M_ISS_Controller", new Color(0.12f, 0.12f, 0.14f), 0.6f, 0.2f);

        var solarA = Tex("T_Solar_Panel_Albedo.png");
        mSolar     = Lit("M_ISS_SolarArray", new Color(0.35f, 0.42f, 0.75f), 0.85f, 0.3f, solarA, null, new Vector2(12, 4));
        mEarth     = Lit("M_ISS_Earth",      Color.white, 0.25f, 0f, Tex("T_Planet_Earth_Albedo.png"), null, Vector2.one, new Color(0.03f, 0.05f, 0.09f));
        mGlass     = LitTransparent("M_ISS_Glass", new Color(0.55f, 0.75f, 0.90f, 0.18f));

        mParticleAdd  = Particle("M_ISS_Particle_Sparks", new Color(1f, 0.75f, 0.35f, 1f), additive: true);
        mParticleMist = Particle("M_ISS_Particle_Mist",   new Color(0.9f, 0.95f, 1f, 0.5f), additive: false);
        mLaser        = Particle("M_ISS_Laser",           Color.white, additive: false);

        var pgtTex = AssetDatabase.LoadAssetAtPath<Texture2D>(ToolDir + "/Models/pgt1_1_d.png");
        mToolPGT     = Lit("M_Tool_PistolGrip", new Color(0.82f, 0.82f, 0.80f), 0.55f, 0.6f, pgtTex, null, Vector2.one, default(Color), null, ToolDir + "/Materials");
        mToolHammer  = Lit("M_Tool_Hammer",     new Color(0.55f, 0.55f, 0.58f), 0.60f, 0.8f, null, null, Vector2.one, default(Color), null, ToolDir + "/Materials");
        mToolRatchet = Lit("M_Tool_Ratchet",    new Color(0.70f, 0.72f, 0.75f), 0.70f, 0.9f, null, null, Vector2.one, default(Color), null, ToolDir + "/Materials");
        mToolGrease  = Lit("M_Tool_GreaseGun",  new Color(0.85f, 0.30f, 0.12f), 0.45f, 0.3f, null, null, Vector2.one, default(Color), null, ToolDir + "/Materials");

        AssetDatabase.SaveAssets();
    }

    static Material LoadOrCreate(string name, Shader shader, string dir)
    {
        string path = (dir ?? MatDir) + "/" + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(shader);
            AssetDatabase.CreateAsset(m, path);
        }
        else if (m.shader != shader) m.shader = shader;
        return m;
    }

    static Shader LitShader()
    {
        var s = Shader.Find("Universal Render Pipeline/Lit");
        if (s == null) throw new System.Exception("No se encontró el shader URP Lit. ¿Está URP activo?");
        return s;
    }

    static Material Lit(string name, Color color, float smooth, float metal,
                        Texture2D albedo = null, Texture2D normal = null, Vector2 tiling = default(Vector2),
                        Color emission = default(Color), Texture2D emissionMap = null, string dir = null)
    {
        var m = LoadOrCreate(name, LitShader(), dir);
        if (tiling == Vector2.zero) tiling = Vector2.one;

        m.SetColor("_BaseColor", color);
        m.SetTexture("_BaseMap", albedo);
        m.SetTextureScale("_BaseMap", tiling);
        m.SetFloat("_Smoothness", smooth);
        m.SetFloat("_Metallic", metal);

        if (normal != null)
        {
            m.SetTexture("_BumpMap", normal);
            m.SetTextureScale("_BumpMap", tiling);
            m.EnableKeyword("_NORMALMAP");
        }
        else
        {
            m.SetTexture("_BumpMap", null);
            m.DisableKeyword("_NORMALMAP");
        }

        // Emisión siempre activa (negra por defecto) para que el resaltado al apuntar
        // y los indicadores por MaterialPropertyBlock funcionen en todos los materiales.
        m.EnableKeyword("_EMISSION");
        m.SetColor("_EmissionColor", emission);
        m.SetTexture("_EmissionMap", emissionMap);
        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        m.enableInstancing = true;
        EditorUtility.SetDirty(m);
        return m;
    }

    static Material LitTransparent(string name, Color color)
    {
        var m = Lit(name, color, 0.95f, 0f);
        m.SetFloat("_Surface", 1f);
        m.SetFloat("_Blend", 0f);
        m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
        m.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
        m.SetFloat("_ZWrite", 0f);
        m.SetOverrideTag("RenderType", "Transparent");
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        m.renderQueue = (int)RenderQueue.Transparent;
        m.SetShaderPassEnabled("DepthOnly", false);
        m.SetShaderPassEnabled("ShadowCaster", false);
        EditorUtility.SetDirty(m);
        return m;
    }

    static Material Particle(string name, Color color, bool additive)
    {
        var sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (sh == null) sh = LitShader();
        var m = LoadOrCreate(name, sh, null);
        m.SetColor("_BaseColor", color);
        m.SetFloat("_Surface", 1f);
        m.SetFloat("_Blend", additive ? 2f : 0f);
        m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        m.SetFloat("_DstBlend", additive ? (float)BlendMode.One : (float)BlendMode.OneMinusSrcAlpha);
        m.SetFloat("_ZWrite", 0f);
        m.SetOverrideTag("RenderType", "Transparent");
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.renderQueue = (int)RenderQueue.Transparent;
        EditorUtility.SetDirty(m);
        return m;
    }

    // ════════════════════════════════════════════════════════════════════════
    // ENTORNO
    // ════════════════════════════════════════════════════════════════════════

    static void SetupEnvironment()
    {
        var sky = AssetDatabase.LoadAssetAtPath<Material>(SkyboxMat);
        if (sky != null) RenderSettings.skybox = sky;
        else report.Add("AVISO: no se encontró " + SkyboxMat + " (cielo por defecto).");

        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.30f, 0.32f, 0.36f);
        RenderSettings.fog = false;

        var sunGO = new GameObject("Sol (luz direccional)");
        sunGO.transform.rotation = Quaternion.Euler(35f, 20f, 0f);
        var sun = sunGO.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.intensity = 1.35f;
        sun.color = new Color(1f, 0.97f, 0.92f);
        sun.shadows = LightShadows.Hard;
        sun.shadowStrength = 0.9f;
        sunGO.AddComponent<UniversalAdditionalLightData>();
        RenderSettings.sun = sun;

        // Tierra de fondo (lejos, por debajo de la estación)
        var earth = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        earth.name = "Tierra (fondo)";
        Object.DestroyImmediate(earth.GetComponent<Collider>());
        earth.transform.position = new Vector3(0f, -2600f, 400f);
        earth.transform.localScale = Vector3.one * 3600f;
        earth.transform.rotation = Quaternion.Euler(-20f, 70f, 10f);
        var er = earth.GetComponent<Renderer>();
        er.sharedMaterial = mEarth;
        er.shadowCastingMode = ShadowCastingMode.Off;
        er.receiveShadows = false;
        GameObjectUtility.SetStaticEditorFlags(earth, StaticEditorFlags.BatchingStatic);
    }

    static GameObject BuildSystems()
    {
        var go = new GameObject("Mission Systems");
        var mm = go.AddComponent<MissionManager>();
        var so = new SerializedObject(mm);
        var list = so.FindProperty("missions");
        var e = AssetDatabase.LoadAssetAtPath<MissionDefinition>(MissionEnergy);
        var s = AssetDatabase.LoadAssetAtPath<MissionDefinition>(MissionSolar);
        list.arraySize = 0;
        if (e != null) { list.InsertArrayElementAtIndex(list.arraySize); list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = e; }
        if (s != null) { list.InsertArrayElementAtIndex(list.arraySize); list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = s; }
        so.ApplyModifiedPropertiesWithoutUndo();
        if (e == null || s == null) report.Add("AVISO: falta algún asset de misión en Assets/Game/Data/Missions.");

        var tester = go.AddComponent<MissionDebugTester>();
        var tso = new SerializedObject(tester);
        var tp = tso.FindProperty("missionManager");
        if (tp != null) { tp.objectReferenceValue = mm; tso.ApplyModifiedPropertiesWithoutUndo(); }

        var hudGO = new GameObject("Mission HUD");
        hudGO.transform.SetParent(go.transform, false);
        var hud = hudGO.AddComponent<MissionHUD>();
        hud.markerMaterial = mMarker;

        report.Add("Core: MissionManager con Mission_Energy + Mission_SolarPanel, HUD y MissionDebugTester.");
        return go;
    }

    // ════════════════════════════════════════════════════════════════════════
    // GEOMETRÍA BÁSICA
    // ════════════════════════════════════════════════════════════════════════

    enum Face { PX, NX, PY, NY, PZ, NZ }

    struct Opening
    {
        public Vector2 center, size;
        public bool glass;
        public Opening(float u, float v, float w, float h, bool glass = false)
        { center = new Vector2(u, v); size = new Vector2(w, h); this.glass = glass; }
    }

    static Vector3 FaceNormal(Face f)
    {
        switch (f)
        {
            case Face.PX: return Vector3.right;
            case Face.NX: return Vector3.left;
            case Face.PY: return Vector3.up;
            case Face.NY: return Vector3.down;
            case Face.PZ: return Vector3.forward;
            default:      return Vector3.back;
        }
    }

    static Face FaceOf(Vector3 dir)
    {
        Vector3 a = new Vector3(Mathf.Abs(dir.x), Mathf.Abs(dir.y), Mathf.Abs(dir.z));
        if (a.x >= a.y && a.x >= a.z) return dir.x > 0 ? Face.PX : Face.NX;
        if (a.y >= a.z) return dir.y > 0 ? Face.PY : Face.NY;
        return dir.z > 0 ? Face.PZ : Face.NZ;
    }

    static Quaternion FaceRotation(Face f)
    {
        Vector3 n = FaceNormal(f);
        Vector3 up = (f == Face.PY || f == Face.NY) ? Vector3.forward : Vector3.up;
        return Quaternion.LookRotation(n, up);
    }

    static Vector2 FaceSize(Face f, Vector3 size)
    {
        switch (f)
        {
            case Face.PX: case Face.NX: return new Vector2(size.z, size.y);
            case Face.PY: case Face.NY: return new Vector2(size.x, size.z);
            default:                    return new Vector2(size.x, size.y);
        }
    }

    static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 localPos, Vector3 scale,
                           Material mat, bool collider = true, bool isStatic = true, bool castShadows = false,
                           Quaternion? localRot = null)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = localRot ?? Quaternion.identity;
        go.transform.localScale = scale;
        if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
        var r = go.GetComponent<Renderer>();
        if (mat != null) r.sharedMaterial = mat;
        r.shadowCastingMode = castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
        if (isStatic) GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);
        return go;
    }

    static GameObject Box(string name, Transform parent, Vector3 localPos, Vector3 size, Material mat,
                          bool collider = true, bool isStatic = true, bool castShadows = false, Quaternion? rot = null)
        => Prim(PrimitiveType.Cube, name, parent, localPos, size, mat, collider, isStatic, castShadows, rot);

    /// <summary>Cilindro de radio r y largo len a lo largo del eje local 'axis'.</summary>
    static GameObject Rod(string name, Transform parent, Vector3 localPos, float r, float len, Vector3 axis,
                          Material mat, bool collider = false, bool isStatic = true)
    {
        var rot = Quaternion.FromToRotation(Vector3.up, axis.normalized);
        return Prim(PrimitiveType.Cylinder, name, parent, localPos, new Vector3(r * 2f, len * 0.5f, r * 2f),
                    mat, collider, isStatic, false, rot);
    }

    /// <summary>
    /// Panel plano (cara de un módulo) con un hueco rectangular opcional. Coordenadas de cara:
    /// u = eje X local, v = eje Y local, normal = Z local.
    /// </summary>
    static void PanelWithOpening(Transform parent, string name, Vector3 center, Quaternion rot, Vector2 size,
                                 float thickness, Opening? op, Material mat, bool collider, bool castShadows)
    {
        var holder = new GameObject(name).transform;
        holder.SetParent(parent, false);
        holder.localPosition = center;
        holder.localRotation = rot;

        if (op == null)
        {
            Box("Panel", holder, Vector3.zero, new Vector3(size.x, size.y, thickness), mat, collider, true, castShadows);
            return;
        }

        var o = op.Value;
        float l = -size.x * 0.5f, r = size.x * 0.5f, b = -size.y * 0.5f, t = size.y * 0.5f;
        float ol = o.center.x - o.size.x * 0.5f, or_ = o.center.x + o.size.x * 0.5f;
        float ob = o.center.y - o.size.y * 0.5f, ot = o.center.y + o.size.y * 0.5f;

        AddRect(holder, "Left",   l,   ol,  b,  t,  thickness, mat, collider, castShadows);
        AddRect(holder, "Right",  or_, r,   b,  t,  thickness, mat, collider, castShadows);
        AddRect(holder, "Bottom", ol,  or_, b,  ob, thickness, mat, collider, castShadows);
        AddRect(holder, "Top",    ol,  or_, ot, t,  thickness, mat, collider, castShadows);

        if (o.glass)
            Box("Glass", holder, new Vector3(o.center.x, o.center.y, 0f), new Vector3(o.size.x, o.size.y, 0.02f),
                mGlass, true, true, false);
    }

    static void AddRect(Transform parent, string name, float x0, float x1, float y0, float y1, float th,
                        Material mat, bool collider, bool castShadows)
    {
        float w = x1 - x0, h = y1 - y0;
        if (w < 0.005f || h < 0.005f) return;
        Box(name, parent, new Vector3((x0 + x1) * 0.5f, (y0 + y1) * 0.5f, 0f), new Vector3(w, h, th), mat, collider, true, castShadows);
    }

    /// <summary>
    /// Módulo tipo caja: paredes interiores + casco exterior. 'skip' = caras compartidas con
    /// otro módulo (no se construyen). 'openings' = huecos por cara.
    /// </summary>
    static Transform Module(Transform parent, string name, Vector3 center, Vector3 size,
                            Face[] skip, Dictionary<Face, Opening> openings, bool hull = true, Face[] noHull = null)
    {
        var root = new GameObject(name).transform;
        root.SetParent(parent, false);
        root.localPosition = center;

        var skipSet = new HashSet<Face>(skip ?? new Face[0]);
        // Caras que conectan con otro módulo: sin casco (quedaría dentro del vecino)
        var connected = new HashSet<Face>(skipSet);
        if (noHull != null) foreach (var f in noHull) connected.Add(f);
        foreach (Face f in System.Enum.GetValues(typeof(Face)))
        {
            if (skipSet.Contains(f)) continue;
            Vector3 n = FaceNormal(f);
            Vector3 half = Vector3.Scale(size * 0.5f, new Vector3(Mathf.Abs(n.x), Mathf.Abs(n.y), Mathf.Abs(n.z)));
            float dist = half.magnitude;
            Quaternion rot = FaceRotation(f);
            Vector2 fs = FaceSize(f, size);
            Opening? op = null;
            if (openings != null && openings.TryGetValue(f, out Opening o)) op = o;

            Material inner = (f == Face.NY) ? mFloor : mWall;
            PanelWithOpening(root, "Wall " + f, n * (dist + WallT * 0.5f), rot, fs + Vector2.one * (WallT * 2f),
                             WallT, op, inner, true, true);

            if (!hull || connected.Contains(f)) continue;
            // El casco solo sobresale hacia los lados que NO conectan con otro módulo
            // (si no, asomaría dentro del módulo vecino).
            float ext = HullOff + WallT;
            float uPos = connected.Contains(FaceOf(rot * Vector3.right)) ? 0f : ext;
            float uNeg = connected.Contains(FaceOf(rot * Vector3.left))  ? 0f : ext;
            float vPos = connected.Contains(FaceOf(rot * Vector3.up))    ? 0f : ext;
            float vNeg = connected.Contains(FaceOf(rot * Vector3.down))  ? 0f : ext;
            Vector2 hs = new Vector2(fs.x + uPos + uNeg, fs.y + vPos + vNeg);
            Vector2 shift = new Vector2((uPos - uNeg) * 0.5f, (vPos - vNeg) * 0.5f);
            Opening? hop = null;
            if (op != null) { var ho = op.Value; ho.glass = false; ho.center -= shift; hop = ho; }
            Vector3 hullCenter = n * (dist + HullOff) + rot * new Vector3(shift.x, shift.y, 0f);
            PanelWithOpening(root, "Hull " + f, hullCenter, rot, hs, 0.05f, hop, mHull, true, true);

            // Marco del hueco entre pared y casco (para que no se vea la cavidad)
            if (op != null) Reveal(root, "Reveal " + f, n * (dist + HullOff * 0.5f + WallT * 0.5f), rot, op.Value, HullOff - WallT);
        }
        return root;
    }

    static void Reveal(Transform parent, string name, Vector3 center, Quaternion rot, Opening o, float depth)
    {
        var h = new GameObject(name).transform;
        h.SetParent(parent, false);
        h.localPosition = center;
        h.localRotation = rot;
        float t = 0.04f;
        Vector2 c = o.center, s = o.size;
        Box("L", h, new Vector3(c.x - s.x * 0.5f - t * 0.5f, c.y, 0f), new Vector3(t, s.y + t * 2f, depth), mFrame, true, true, true);
        Box("R", h, new Vector3(c.x + s.x * 0.5f + t * 0.5f, c.y, 0f), new Vector3(t, s.y + t * 2f, depth), mFrame, true, true, true);
        Box("B", h, new Vector3(c.x, c.y - s.y * 0.5f - t * 0.5f, 0f), new Vector3(s.x, t, depth), mFrame, true, true, true);
        Box("T", h, new Vector3(c.x, c.y + s.y * 0.5f + t * 0.5f, 0f), new Vector3(s.x, t, depth), mFrame, true, true, true);
    }

    /// <summary>Marco de escotilla visible por dentro (anillo metálico alrededor del hueco).</summary>
    static void HatchFrame(Transform parent, Vector3 worldCenter, Quaternion faceRot, Vector2 opening)
    {
        var h = new GameObject("Hatch Frame").transform;
        h.SetParent(parent, false);
        h.position = worldCenter;
        h.rotation = faceRot;
        float t = 0.08f, d = 0.10f;
        Box("L", h, new Vector3(-opening.x * 0.5f - t * 0.5f, 0f, 0f), new Vector3(t, opening.y + t * 2f, d), mFrame, false);
        Box("R", h, new Vector3( opening.x * 0.5f + t * 0.5f, 0f, 0f), new Vector3(t, opening.y + t * 2f, d), mFrame, false);
        Box("B", h, new Vector3(0f, -opening.y * 0.5f - t * 0.5f, 0f), new Vector3(opening.x, t, d), mFrame, false);
        Box("T", h, new Vector3(0f,  opening.y * 0.5f + t * 0.5f, 0f), new Vector3(opening.x, t, d), mFrame, false);
    }

    static Text Label(Transform parent, string text, Vector3 worldPos, Vector3 facing, Vector2 size, int fontSize, Color color)
    {
        var holder = new GameObject("Label").transform;
        holder.SetParent(parent, false);
        holder.position = worldPos;
        holder.rotation = Quaternion.LookRotation(facing, Mathf.Abs(Vector3.Dot(facing, Vector3.up)) > 0.9f ? Vector3.forward : Vector3.up);
        return NovaText.CreateFacing(holder, "Text", text, Vector3.zero, size, fontSize, color);
    }

    // ════════════════════════════════════════════════════════════════════════
    // DECORACIÓN (racks, pasamanos, etc.)
    // ════════════════════════════════════════════════════════════════════════

    static float Rand(float a, float b) => a + (float)rng.NextDouble() * (b - a);
    static bool Chance(float p) => rng.NextDouble() < p;

    /// <summary>
    /// Racks a lo largo de un módulo. axis = eje largo (X o Z). Se ponen en las 2 paredes
    /// laterales, el techo y el suelo, dejando libres los tramos de 'keepClear' (en metros
    /// a lo largo del eje, coordenadas absolutas).
    /// </summary>
    static void Racks(string moduleName, Vector3 center, Vector3 size, bool alongX, float margin,
                      float depth, bool walls = true, bool ceiling = true, bool floor = true, float handrailY = 0.15f)
    {
        var root = new GameObject("Racks " + moduleName).transform;
        root.SetParent(decorRoot, false);

        float len = alongX ? size.x : size.z;
        float width = alongX ? size.z : size.x;
        float pitch = 1.05f;
        int count = Mathf.Max(1, Mathf.FloorToInt((len - margin * 2f) / pitch));
        float start = -(count * pitch) * 0.5f + pitch * 0.5f;

        for (int i = 0; i < count; i++)
        {
            float a = start + i * pitch;
            // Laterales
            if (walls)
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    Vector3 lp = alongX ? new Vector3(a, 0f, side * (width * 0.5f - depth * 0.5f))
                                        : new Vector3(side * (width * 0.5f - depth * 0.5f), 0f, a);
                    Vector3 facing = alongX ? new Vector3(0, 0, -side) : new Vector3(-side, 0, 0);
                    RackUnit(root, center + lp, facing, new Vector2(pitch - 0.06f, size.y - 0.42f), depth);
                }
            }
            // Techo
            if (ceiling)
            {
                Vector3 lp = alongX ? new Vector3(a, size.y * 0.5f - 0.09f, 0f) : new Vector3(0f, size.y * 0.5f - 0.09f, a);
                Vector3 s = alongX ? new Vector3(pitch - 0.06f, 0.18f, width - depth * 2f - 0.1f)
                                   : new Vector3(width - depth * 2f - 0.1f, 0.18f, pitch - 0.06f);
                Box("Ceiling Rack", root, center + lp, s, mRack, true);
                if (i % 2 == 0)
                {
                    Vector3 ls = alongX ? new Vector3(0.7f, 0.02f, 0.35f) : new Vector3(0.35f, 0.02f, 0.7f);
                    Box("Light Panel", root, center + lp + Vector3.down * 0.10f, ls, mLight, false);
                }
            }
            // Suelo (cubierta con bolsas CTB)
            if (floor)
            {
                Vector3 lp = alongX ? new Vector3(a, -size.y * 0.5f + 0.09f, 0f) : new Vector3(0f, -size.y * 0.5f + 0.09f, a);
                Vector3 s = alongX ? new Vector3(pitch - 0.06f, 0.18f, width - depth * 2f - 0.1f)
                                   : new Vector3(width - depth * 2f - 0.1f, 0.18f, pitch - 0.06f);
                Box("Deck Rack", root, center + lp, s, mFloor, true);
                if (Chance(0.45f))
                {
                    Vector3 bag = new Vector3(Rand(0.35f, 0.5f), Rand(0.18f, 0.28f), Rand(0.3f, 0.45f));
                    Vector3 off = alongX ? new Vector3(Rand(-0.2f, 0.2f), 0.09f + bag.y * 0.5f, Rand(-0.35f, 0.35f))
                                         : new Vector3(Rand(-0.35f, 0.35f), 0.09f + bag.y * 0.5f, Rand(-0.2f, 0.2f));
                    Box("CTB Bag", root, center + lp + off, bag, mBag, false, true, false,
                        Quaternion.Euler(0f, Rand(-15f, 15f), 0f));
                }
            }
        }

        // Pasamanos a lo largo, a media altura, en ambos lados
        for (int side = -1; side <= 1; side += 2)
        {
            float off = width * 0.5f - depth - 0.06f;
            Vector3 p = alongX ? new Vector3(0f, handrailY, side * off) : new Vector3(side * off, handrailY, 0f);
            Vector3 ax = alongX ? Vector3.right : Vector3.forward;
            Rod("Handrail", root, center + p, 0.016f, len - margin * 2f, ax, mHandrail);
            // Cables por las esquinas del techo
            Vector3 c = alongX ? new Vector3(0f, size.y * 0.5f - 0.22f, side * (off - 0.05f))
                               : new Vector3(side * (off - 0.05f), size.y * 0.5f - 0.22f, 0f);
            Rod("Cable Bundle", root, center + c, 0.025f, len - margin * 2f, ax, mCable);
            // Tiras de alarma (parpadean en la emergencia)
            Vector3 al = alongX ? new Vector3(0f, size.y * 0.5f - 0.2f, side * (off + 0.04f))
                                : new Vector3(side * (off + 0.04f), size.y * 0.5f - 0.2f, 0f);
            var strip = Box("Alarm Strip", root, center + al,
                            alongX ? new Vector3(len - margin * 2f, 0.03f, 0.02f) : new Vector3(0.02f, 0.03f, len - margin * 2f),
                            mAlarm, false, false);
            alarmStrips.Add(strip.GetComponent<Renderer>());
        }
    }

    static readonly List<Renderer> alarmStrips = new List<Renderer>();

    /// <summary>Un rack frontal con subpaneles, LEDs y a veces un portátil.</summary>
    static void RackUnit(Transform parent, Vector3 pos, Vector3 facing, Vector2 face, float depth)
    {
        var rack = new GameObject("Rack").transform;
        rack.SetParent(parent, false);
        rack.position = pos;
        rack.rotation = Quaternion.LookRotation(facing, Vector3.up);

        Box("Body", rack, Vector3.zero, new Vector3(face.x, face.y, depth), mRack, true);

        float fz = depth * 0.5f + 0.005f;
        int panels = 2 + rng.Next(3);
        float y = face.y * 0.5f - 0.08f;
        for (int p = 0; p < panels && y > -face.y * 0.5f + 0.2f; p++)
        {
            float h = Rand(0.25f, 0.55f);
            float w = face.x * Rand(0.55f, 0.85f);
            var mat = Chance(0.3f) ? mRackDark : mPanel;
            Box("Sub Panel", rack, new Vector3(Rand(-0.05f, 0.05f), y - h * 0.5f, fz), new Vector3(w, h, 0.012f), mat, false);
            if (Chance(0.7f))
            {
                int leds = 1 + rng.Next(4);
                for (int l = 0; l < leds; l++)
                {
                    var lm = Chance(0.6f) ? mGreen : (Chance(0.5f) ? mAmber : mCyan);
                    Box("LED", rack, new Vector3(-w * 0.4f + l * 0.045f, y - 0.05f, fz + 0.01f), new Vector3(0.018f, 0.018f, 0.01f), lm, false);
                }
            }
            if (Chance(0.25f))
                Box("Knob", rack, new Vector3(w * 0.35f, y - h * 0.6f, fz + 0.015f), new Vector3(0.04f, 0.04f, 0.03f), mMetal, false);
            y -= h + 0.06f;
        }

        if (Chance(0.22f)) Laptop(rack, new Vector3(Rand(-0.15f, 0.15f), Rand(-0.1f, 0.25f), fz + 0.02f));

        // Asas de rack
        Box("Handle L", rack, new Vector3(-face.x * 0.5f + 0.04f, 0f, fz + 0.02f), new Vector3(0.02f, 0.18f, 0.03f), mMetal, false);
        Box("Handle R", rack, new Vector3( face.x * 0.5f - 0.04f, 0f, fz + 0.02f), new Vector3(0.02f, 0.18f, 0.03f), mMetal, false);
    }

    static void Laptop(Transform parent, Vector3 localPos)
    {
        var lap = new GameObject("Laptop").transform;
        lap.SetParent(parent, false);
        lap.localPosition = localPos;
        Box("Base", lap, new Vector3(0f, -0.12f, 0.12f), new Vector3(0.34f, 0.02f, 0.24f), mBlackPanel, false);
        Box("Lid", lap, new Vector3(0f, 0f, 0.01f), new Vector3(0.34f, 0.23f, 0.015f), mBlackPanel, false);
        Box("Screen", lap, new Vector3(0f, 0f, 0.02f), new Vector3(0.31f, 0.2f, 0.004f), mScreen, false);
    }

    static Light PointLight(Transform parent, string name, Vector3 pos, float range, float intensity, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        var l = go.AddComponent<Light>();
        l.type = LightType.Point;
        l.range = range;
        l.intensity = intensity;
        l.color = color;
        l.shadows = LightShadows.None;
        go.AddComponent<UniversalAdditionalLightData>();
        return l;
    }

    // ════════════════════════════════════════════════════════════════════════
    // MÓDULOS
    // ════════════════════════════════════════════════════════════════════════

    // Centros y tamaños (interior libre, metros). Suelo en y = 0.
    static readonly Vector3 NodeC    = new Vector3(0f, H * 0.5f, 0f),      NodeS    = new Vector3(3.0f, H, 3.0f);
    static readonly Vector3 LabC     = new Vector3(-5.75f, H * 0.5f, 0f),  LabS     = new Vector3(8.5f, H, 2.4f);
    static readonly Vector3 SvcC     = new Vector3(6.5f, H * 0.5f, 0f),    SvcS     = new Vector3(10f, H, 2.4f);
    static readonly Vector3 LockC    = new Vector3(0f, H * 0.5f, 4.0f),    LockS    = new Vector3(2.4f, H, 5.0f);
    static readonly Vector3 TunnelC  = new Vector3(0f, H * 0.5f, -2.0f),   TunnelS  = new Vector3(1.6f, 1.7f, 1.0f);
    static readonly Vector3 CupolaC  = new Vector3(0f, H * 0.5f, -3.4f),   CupolaS  = new Vector3(2.6f, H, 1.8f);

    static void BuildModules(Transform root)
    {
        alarmStrips.Clear();
        var hatch = new Opening(0f, 0f, HatchSize.x, HatchSize.y);
        var mods = new GameObject("Modules").transform;
        mods.SetParent(root, false);

        // Nodo: 4 escotillas laterales
        Module(mods, "NODO 1 · UNITY", NodeC, NodeS, null, new Dictionary<Face, Opening>
        {
            { Face.PX, hatch }, { Face.NX, hatch }, { Face.PZ, hatch }, { Face.NZ, hatch }
        }, true, new[] { Face.PX, Face.NX, Face.PZ, Face.NZ });

        // Destiny (laboratorio) hacia -X; su cara +X la pone el Nodo
        Module(mods, "DESTINY · LABORATORIO", LabC, LabS, new[] { Face.PX }, null);

        // Zvezda (servicio) hacia +X; ventana en el extremo hacia la bahía robótica
        Module(mods, "ZVEZDA · MÓDULO DE SERVICIO", SvcC, SvcS, new[] { Face.NX }, new Dictionary<Face, Opening>
        {
            { Face.PX, new Opening(0f, 0.1f, 1.6f, 0.9f, glass: true) }
        });

        // Quest (esclusa) hacia +Z; escotilla exterior al final
        Module(mods, "QUEST · ESCLUSA", LockC, LockS, new[] { Face.NZ }, new Dictionary<Face, Opening>
        {
            { Face.PZ, hatch }
        });

        // Túnel corto y Cúpula hacia -Z, con ventanas al suelo (Tierra) y al frente
        Module(mods, "Túnel Cúpula", TunnelC, TunnelS, new[] { Face.PZ, Face.NZ }, null, hull: true);
        Module(mods, "CÚPULA", CupolaC, CupolaS, null, new Dictionary<Face, Opening>
        {
            { Face.PZ, hatch },
            { Face.NZ, new Opening(0f, 0.1f, 1.8f, 1.2f, glass: true) },
            { Face.NY, new Opening(0f, 0f, 1.8f, 1.2f, glass: true) }
        }, true, new[] { Face.PZ });

        // Marcos de escotilla
        HatchFrame(mods, new Vector3(-1.5f, 1.2f, 0f), Quaternion.LookRotation(Vector3.right), HatchSize);
        HatchFrame(mods, new Vector3( 1.5f, 1.2f, 0f), Quaternion.LookRotation(Vector3.left), HatchSize);
        HatchFrame(mods, new Vector3(0f, 1.2f,  1.5f), Quaternion.LookRotation(Vector3.back), HatchSize);
        HatchFrame(mods, new Vector3(0f, 1.2f, -1.5f), Quaternion.LookRotation(Vector3.forward), HatchSize);
        HatchFrame(mods, new Vector3(0f, 1.2f, -2.5f), Quaternion.LookRotation(Vector3.forward), HatchSize);
        HatchFrame(mods, new Vector3(0f, 1.2f,  6.5f), Quaternion.LookRotation(Vector3.back), HatchSize);

        // Racks y decoración
        Racks("Destiny", LabC, LabS, true, 0.25f, 0.22f);
        Racks("Zvezda",  SvcC, SvcS, true, 0.35f, 0.22f);
        Racks("Quest",   LockC, LockS, false, 0.5f, 0.12f, walls: false, ceiling: true, floor: true, handrailY: -0.6f);
        NodeDecor();
        CupolaDecor();
        LabDecor();

        // Señalización sobre cada escotilla (desde el Nodo)
        Color sign = new Color(0.2f, 0.25f, 0.35f);
        Label(decorRoot, "<< DESTINY · LAB", new Vector3(-1.44f, 2.12f, 0f), Vector3.right, new Vector2(1.2f, 0.16f), 70, sign);
        Label(decorRoot, "ZVEZDA · SERVICIO >>", new Vector3(1.44f, 2.12f, 0f), Vector3.left, new Vector2(1.2f, 0.16f), 70, sign);
        Label(decorRoot, "QUEST · ESCLUSA (EVA)", new Vector3(0f, 2.12f, 1.44f), Vector3.back, new Vector2(1.2f, 0.16f), 70, sign);
        Label(decorRoot, "CÚPULA", new Vector3(0f, 2.12f, -1.44f), Vector3.forward, new Vector2(1.2f, 0.16f), 70, sign);
        Label(decorRoot, "DESTINY — LABORATORIO DE EE.UU.", new Vector3(-9.94f, 1.95f, 0f), Vector3.right, new Vector2(1.8f, 0.14f), 60, sign);
        Label(decorRoot, "ZVEZDA — BAHÍA ROBÓTICA >>", new Vector3(11.44f, 1.95f, 0f), Vector3.left, new Vector2(1.8f, 0.14f), 60, sign);

        // Luces
        var lights = new GameObject("Lights").transform;
        lights.SetParent(root, false);
        Color warm = new Color(1f, 0.95f, 0.86f);
        PointLight(lights, "Light Node",    new Vector3(0f, 2.0f, 0f), 6f, 1.3f, warm);
        PointLight(lights, "Light Lab A",   new Vector3(-4.0f, 2.0f, 0f), 6f, 1.2f, warm);
        PointLight(lights, "Light Lab B",   new Vector3(-8.0f, 2.0f, 0f), 6f, 1.2f, warm);
        PointLight(lights, "Light Svc A",   new Vector3(4.5f, 2.0f, 0f), 6f, 1.2f, warm);
        PointLight(lights, "Light Svc B",   new Vector3(9.0f, 2.0f, 0f), 6f, 1.2f, warm);
        PointLight(lights, "Light Airlock", new Vector3(0f, 2.0f, 4.2f), 6f, 1.1f, new Color(0.9f, 0.95f, 1f));
        PointLight(lights, "Light Cupola",  new Vector3(0f, 2.0f, -3.2f), 4f, 0.6f, warm);

        report.Add("Módulos: Nodo, Destiny, Zvezda, Quest, Cúpula (+ casco, racks, pasamanos, luces).");
    }

    static void NodeDecor()
    {
        var d = new GameObject("Node Decor").transform;
        d.SetParent(decorRoot, false);
        // Bolsas CTB en el techo y el suelo
        Box("CTB Ceiling 1", d, new Vector3(-0.9f, H - 0.18f, -0.9f), new Vector3(0.5f, 0.3f, 0.45f), mBag, true);
        Box("CTB Ceiling 2", d, new Vector3( 0.9f, H - 0.18f, -0.9f), new Vector3(0.45f, 0.3f, 0.5f), mBag, true);
        Box("CTB Floor 1",   d, new Vector3(-0.95f, 0.16f, -0.95f), new Vector3(0.5f, 0.3f, 0.45f), mBag, true);
        Box("CTB Floor 2",   d, new Vector3( 0.95f, 0.16f, -0.95f), new Vector3(0.45f, 0.28f, 0.5f), mBag, true);
        Box("Light Panel",   d, new Vector3(0f, H - 0.02f, 0f), new Vector3(0.8f, 0.02f, 0.8f), mLight, false);
        // Extintor
        Rod("Extintor", d, new Vector3(1.38f, 0.9f, -1.2f), 0.06f, 0.42f, Vector3.up, mRed);
        // Pasamanos en las esquinas
        foreach (var x in new[] { -1.42f, 1.42f })
            foreach (var z in new[] { -1.42f, 1.42f })
                Rod("Handrail", d, new Vector3(x, 1.2f, z), 0.016f, 1.8f, Vector3.up, mHandrail);
        // Tiras de alarma
        foreach (var z in new[] { -1.44f, 1.44f })
        {
            var s = Box("Alarm Strip", d, new Vector3(0f, H - 0.12f, z), new Vector3(2.8f, 0.03f, 0.02f), mAlarm, false, false);
            alarmStrips.Add(s.GetComponent<Renderer>());
        }
    }

    static void CupolaDecor()
    {
        var d = new GameObject("Cupola Decor").transform;
        d.SetParent(decorRoot, false);
        // Marcos de ventana (estilo cúpula: travesaños)
        for (int i = -1; i <= 1; i += 2)
        {
            Box("Window Mullion", d, new Vector3(i * 0.3f, 0.0f, -3.4f), new Vector3(0.05f, 0.06f, 1.2f), mFrame, false);
            Box("Window Mullion", d, new Vector3(i * 0.3f, 1.3f, -4.28f), new Vector3(0.05f, 1.2f, 0.06f), mFrame, false);
        }
        Rod("Handrail", d, new Vector3(-1.2f, 1.0f, -3.4f), 0.016f, 1.4f, Vector3.forward, mHandrail);
        Rod("Handrail", d, new Vector3( 1.2f, 1.0f, -3.4f), 0.016f, 1.4f, Vector3.forward, mHandrail);
        Label(d, "CÚPULA — VISTA DE LA TIERRA (mira abajo)", new Vector3(0f, 2.2f, -4.25f), Vector3.forward, new Vector2(1.8f, 0.12f), 50, new Color(0.2f, 0.25f, 0.35f));
    }

    static void LabDecor()
    {
        var d = new GameObject("Lab Decor").transform;
        d.SetParent(decorRoot, false);
        // Experimento Veggie (cámara de cultivo con luz rosa) en el extremo del laboratorio
        var veg = new GameObject("Veggie Experiment").transform;
        veg.SetParent(d, false);
        veg.position = new Vector3(-9.75f, 1.2f, 0f);
        veg.rotation = Quaternion.LookRotation(Vector3.right);
        Box("Chamber", veg, Vector3.zero, new Vector3(0.7f, 0.6f, 0.4f), mRackDark, true);
        Box("Grow Light", veg, new Vector3(0f, 0.28f, 0.05f), new Vector3(0.6f, 0.03f, 0.3f), mPlantLight, false);
        for (int i = 0; i < 6; i++)
            Prim(PrimitiveType.Sphere, "Plant", veg, new Vector3(-0.24f + i * 0.095f, -0.12f + Rand(0f, 0.08f), 0.08f),
                 Vector3.one * Rand(0.07f, 0.11f), mPlant, false);
        Box("Glass", veg, new Vector3(0f, 0f, 0.205f), new Vector3(0.66f, 0.56f, 0.01f), mGlass, false);
        Label(d, "VEGGIE · CULTIVO EN MICROGRAVEDAD", new Vector3(-9.6f, 1.62f, 0f), Vector3.right, new Vector2(0.8f, 0.08f), 40, new Color(0.2f, 0.25f, 0.35f));
    }

    // ════════════════════════════════════════════════════════════════════════
    // PLAYER
    // ════════════════════════════════════════════════════════════════════════

    static Transform BuildPlayer(out PlayerSpawnBay spawnBay)
    {
        var spawnGO = new GameObject("Spawn · Nodo");
        spawnGO.transform.SetPositionAndRotation(new Vector3(0f, 0f, -0.4f), Quaternion.LookRotation(Vector3.forward));
        spawnBay = spawnGO.AddComponent<PlayerSpawnBay>();
        spawnBay.sectorName = "Nodo 1 Unity";
        spawnBay.spawnPoint = spawnGO.transform;

        var player = new GameObject("Player");
        player.tag = "Player";
        player.transform.SetPositionAndRotation(spawnGO.transform.position, spawnGO.transform.rotation);

        var cc = player.AddComponent<CharacterController>();
        cc.radius = 0.22f;
        cc.height = 0.5f;
        cc.center = new Vector3(0f, 1.45f, 0f);
        cc.skinWidth = 0.02f;
        cc.stepOffset = 0f;
        cc.slopeLimit = 90f;
        cc.minMoveDistance = 0f;

        var camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        camGO.transform.SetParent(player.transform, false);
        camGO.transform.localPosition = new Vector3(0f, 1.45f, 0f);
        var cam = camGO.AddComponent<Camera>();
        cam.nearClipPlane = 0.03f;
        cam.farClipPlane = 8000f;
        cam.fieldOfView = 75f;
        cam.clearFlags = CameraClearFlags.Skybox;
        camGO.AddComponent<AudioListener>();
        var acd = camGO.AddComponent<UniversalAdditionalCameraData>();
        acd.renderPostProcessing = false;
        acd.renderShadows = true;

        var fly = player.AddComponent<OrbitFreeFly>();
        fly.moveSpeed = 2.2f;
        fly.sprintMultiplier = 2.2f;
        fly.speedRange = new Vector2(0.6f, 6f);
        fly.smoothing = 6f;
        fly.playerCamera = cam;

        player.AddComponent<XRPlayerRig>();
        var rig = player.AddComponent<NovaInteractionRig>();
        rig.laserMaterial = mLaser;
        rig.controllerMaterial = mController;

        report.Add("Player: OrbitFreeFly (con colisiones) + XRPlayerRig + NovaInteractionRig.");
        return player.transform;
    }

    // ════════════════════════════════════════════════════════════════════════
    // CONTENIDO DE MISIONES
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>Placa base de consola mirando hacia 'facing' (su +Z local apunta al jugador).</summary>
    static Transform Console(Transform parent, string name, Vector3 pos, Vector3 facing, Vector2 size, string title)
    {
        var c = new GameObject(name).transform;
        c.SetParent(parent, false);
        c.position = pos;
        c.rotation = Quaternion.LookRotation(facing, Vector3.up);
        Box("Plate", c, Vector3.zero, new Vector3(size.x, size.y, 0.04f), mBlackPanel, true, true);
        Box("Bezel", c, new Vector3(0f, 0f, -0.012f), new Vector3(size.x + 0.04f, size.y + 0.04f, 0.03f), mMetal, false);
        if (!string.IsNullOrEmpty(title))
            NovaText.CreateFacing(c, "Title", title, new Vector3(0f, size.y * 0.5f - 0.05f, 0.025f),
                                  new Vector2(size.x - 0.04f, 0.07f), 34, new Color(0.85f, 0.92f, 1f));
        return c;
    }

    static MissionButton Button(Transform console, string name, Vector3 local, MissionButton.Mode mode, MissionAction action,
                                string prompt, string labelText, Material capMat, float hold = 0f,
                                MissionZone zone = null, bool big = false)
    {
        var b = new GameObject(name).transform;
        b.SetParent(console, false);
        b.localPosition = local;
        b.localRotation = Quaternion.identity;

        float r = big ? 0.075f : 0.05f;
        Prim(PrimitiveType.Cylinder, "Base", b, new Vector3(0f, 0f, 0.025f), new Vector3(r * 2.6f, 0.012f, r * 2.6f), mMetal,
             false, false, false, Quaternion.Euler(90f, 0f, 0f));
        var cap = Prim(PrimitiveType.Cylinder, "Cap", b, new Vector3(0f, 0f, 0.05f), new Vector3(r * 2f, 0.02f, r * 2f), capMat,
                       true, false, false, Quaternion.Euler(90f, 0f, 0f));
        var light = Box("Status", b, new Vector3(0f, -r - 0.04f, 0.03f), new Vector3(0.05f, 0.015f, 0.01f), mAmber, false, false);

        var lbl = NovaText.CreateFacing(b, "Label", labelText, new Vector3(0f, -r - 0.10f, 0.03f), new Vector2(0.34f, 0.09f), 24, Color.white);

        var mb = b.gameObject.AddComponent<MissionButton>();
        mb.mode = mode;
        mb.action = action;
        mb.prompt = prompt;
        mb.holdSeconds = hold;
        mb.requiredZone = zone;
        mb.pressPart = cap.transform;
        mb.label = lbl;
        mb.idleLabel = labelText;
        mb.statusLight = mode == MissionButton.Mode.CompleteAction ? light.GetComponent<Renderer>() : null;
        mb.highlightRenderers = new[] { cap.GetComponent<Renderer>() };
        if (mode != MissionButton.Mode.CompleteAction) light.SetActive(false);
        return mb;
    }

    static void BuildMissionContent(Transform root, Transform systems, PlayerSpawnBay spawnBay, Transform player)
    {
        var mc = new GameObject("Mission Content").transform;
        mc.SetParent(root, false);

        // ── NODO: Iniciar / Reiniciar / Emergencia ──────────────────────────
        var startC = Console(mc, "Consola Inicio", new Vector3(-1.07f, 1.30f, 1.47f), Vector3.back, new Vector2(0.62f, 0.95f), "SECUENCIA DE MISIÓN");
        Button(startC, "Btn INICIAR", new Vector3(0f, 0.12f, 0f), MissionButton.Mode.StartMission, MissionAction.EmergencyActivated,
               "Iniciar secuencia de misiones", "INICIAR", mGreen, big: true);
        Button(startC, "Btn REINICIAR", new Vector3(0f, -0.25f, 0f), MissionButton.Mode.ResetMission, MissionAction.EmergencyActivated,
               "Reiniciar secuencia", "REINICIAR", mAmber);

        var emerC = Console(mc, "Consola Emergencia", new Vector3(1.07f, 1.30f, 1.47f), Vector3.back, new Vector2(0.62f, 0.95f), "ALERTA DE ENERGÍA");
        Button(emerC, "Btn Emergencia", new Vector3(0f, 0.05f, 0f), MissionButton.Mode.CompleteAction, MissionAction.EmergencyActivated,
               "Activar protocolo de emergencia", "PROTOCOLO DE\nEMERGENCIA", mRed, big: true);

        // ── DESTINY: módulo energético de repuesto (pickup) ─────────────────
        var holder = Box("Soporte ORU", mc, new Vector3(-6.5f, 1.0f, 0.93f), new Vector3(0.5f, 0.04f, 0.14f), mMetal, true);
        var oru = BuildOru(mc, new Vector3(-6.5f, 1.16f, 0.82f));
        var pickup = oru.AddComponent<MissionPickup>();
        pickup.itemId = "energy_module";
        pickup.prompt = "Módulo energético de repuesto (ORU)";
        pickup.completeOnGrab = true;
        pickup.grabAction = MissionAction.EnergyModuleCollected;
        pickup.lockUntilObjective = true;
        pickup.holdOffset = new Vector3(0f, 0f, 0.18f);
        Label(mc, "REPUESTO · MÓDULO ENERGÉTICO (ORU)", new Vector3(-6.5f, 1.5f, 0.97f), Vector3.back, new Vector2(0.9f, 0.08f), 34, new Color(0.15f, 0.2f, 0.3f));

        // ── ZVEZDA: compuerta de mantenimiento (drop zone) ──────────────────
        var hatchC = Console(mc, "Compuerta de mantenimiento", new Vector3(9.2f, 1.2f, 0.95f), Vector3.back, new Vector2(0.7f, 0.75f), "COMPUERTA DE MANTENIMIENTO");
        var slot = Box("Ranura", hatchC, new Vector3(0f, -0.05f, 0.03f), new Vector3(0.42f, 0.32f, 0.02f), mRackDark, false, false);
        var socket = new GameObject("Socket").transform;
        socket.SetParent(hatchC, false);
        socket.localPosition = new Vector3(0f, -0.05f, 0.14f);
        var ghost = new GameObject("Guía").transform;
        ghost.SetParent(hatchC, false);
        ghost.localPosition = new Vector3(0f, -0.05f, 0.04f);
        Box("G T", ghost, new Vector3(0f, 0.17f, 0f), new Vector3(0.44f, 0.015f, 0.01f), mAmber, false, false);
        Box("G B", ghost, new Vector3(0f, -0.17f, 0f), new Vector3(0.44f, 0.015f, 0.01f), mAmber, false, false);
        Box("G L", ghost, new Vector3(-0.22f, 0f, 0f), new Vector3(0.015f, 0.34f, 0.01f), mAmber, false, false);
        Box("G R", ghost, new Vector3(0.22f, 0f, 0f), new Vector3(0.015f, 0.34f, 0.01f), mAmber, false, false);
        var dz = hatchC.gameObject.AddComponent<MissionDropZone>();
        dz.acceptsItemId = "energy_module";
        dz.acceptRadius = 0.6f;
        dz.socket = socket;
        dz.action = MissionAction.EnergyModuleLoadedIntoHatch;
        dz.ghost = ghost.gameObject;

        // ── ZVEZDA: consola robótica + bahía ────────────────────────────────
        var arm = BuildRoboticBay(mc);
        var robC = Console(mc, "Consola Robótica", new Vector3(10.45f, 1.15f, -0.95f), Vector3.forward, new Vector2(1.05f, 0.9f), "CONTROL ROBÓTICO");
        Button(robC, "Btn Activar Consola", new Vector3(-0.36f, -0.05f, 0f), MissionButton.Mode.CompleteAction, MissionAction.RoboticConsoleActivated,
               "Activar consola robótica", "ACTIVAR\nCONSOLA", mCyan);
        Button(robC, "Btn Confirmar", new Vector3(0.36f, -0.05f, 0f), MissionButton.Mode.CompleteAction, MissionAction.RepairConfirmed,
               "Confirmar reparación energética", "CONFIRMAR\nREPARACIÓN", mGreen);
        var screen = NovaText.CreateFacing(robC, "Pantalla", "", new Vector3(0f, 0.26f, 0.03f), new Vector2(0.95f, 0.2f), 26, new Color(0.7f, 0.9f, 1f));
        Box("Pantalla Fondo", robC, new Vector3(0f, 0.26f, 0.021f), new Vector3(0.98f, 0.22f, 0.004f), mScreen, false);

        // Joystick (minijuego robótico)
        var joyBase = new GameObject("Joystick Brazo").transform;
        joyBase.SetParent(robC, false);
        joyBase.localPosition = new Vector3(0f, -0.08f, 0.02f);
        var jb = Prim(PrimitiveType.Cylinder, "Base", joyBase, new Vector3(0f, 0f, 0.02f), new Vector3(0.14f, 0.02f, 0.14f), mMetal,
                      true, false, false, Quaternion.Euler(90f, 0f, 0f));
        var stickPivot = new GameObject("Stick").transform;
        stickPivot.SetParent(joyBase, false);
        stickPivot.localPosition = new Vector3(0f, 0f, 0.04f);
        var stickRod = Rod("Rod", stickPivot, new Vector3(0f, 0f, 0.06f), 0.012f, 0.12f, Vector3.forward, mMetal, false, false);
        var knob = Prim(PrimitiveType.Sphere, "Knob", stickPivot, new Vector3(0f, 0f, 0.13f), Vector3.one * 0.05f, mRed, true, false);
        var rac = joyBase.gameObject.AddComponent<RoboticArmConsole>();
        rac.prompt = "Mover el brazo robótico";
        rac.action = MissionAction.EnergyModuleInstalled;
        rac.duration = 7f;
        rac.armBase = arm.armBase; rac.armSegment = arm.segment; rac.gripper = arm.gripper;
        rac.carriedModule = arm.carried; rac.pickPoint = arm.pick; rac.installPoint = arm.install;
        rac.socketLight = arm.socketLight;
        rac.screen = screen;
        rac.joystick.stick = stickPivot;
        rac.highlightRenderers = new[] { knob.GetComponent<Renderer>(), jb.GetComponent<Renderer>() };
        NovaText.CreateFacing(robC, "Joy Label", "MANTÉN PARA\nMOVER BRAZO", new Vector3(0f, -0.3f, 0.03f), new Vector2(0.3f, 0.08f), 22, Color.white);

        // Reacción: al cargar la compuerta, el repuesto "pasa" a la bahía
        var reactions = new GameObject("Mission Reactions").transform;
        reactions.SetParent(systems, false);
        var r12 = reactions.gameObject.AddComponent<MissionReaction>();
        r12.onAction = MissionAction.EnergyModuleLoadedIntoHatch;
        r12.disableObjects = new[] { oru };
        r12.message = "Repuesto en la bahía. Ve a la consola robótica.";

        // ── QUEST: esclusa (Misión 2) ───────────────────────────────────────
        var zoneGO = new GameObject("Zona Esclusa");
        zoneGO.transform.SetParent(mc, false);
        zoneGO.transform.position = LockC;
        var lockZone = zoneGO.AddComponent<MissionZone>();
        lockZone.zoneName = "la esclusa Quest";
        lockZone.size = new Vector3(2.4f, 2.4f, 4.9f);
        lockZone.completeAction = true;
        lockZone.action = MissionAction.ReturnedToAirlock;

        // Escotillas
        var inner = BuildHatch(mc, "Escotilla interior (Nodo↔Quest)", new Vector3(0f, 1.2f, 1.56f), Vector3.back, HatchDoor.Mode.Proximity);
        var outer = BuildHatch(mc, "Escotilla exterior (EVA)", new Vector3(0f, 1.2f, 6.44f), Vector3.back, HatchDoor.Mode.ForcedClosed);

        // Pared -X (x = -1.2): chequeo de traje, armario del traje, MMU
        var chkC = Console(mc, "Consola Traje", new Vector3(-1.17f, 1.3f, 2.5f), Vector3.right, new Vector2(0.6f, 0.75f), "TRAJE EMU");
        Button(chkC, "Btn Verificar Traje", new Vector3(0f, 0f, 0f), MissionButton.Mode.CompleteAction, MissionAction.SuitSystemsChecked,
               "Verificar sistemas del traje", "VERIFICAR\nSISTEMAS", mCyan, zone: lockZone);

        var suitC = Console(mc, "Armario Traje", new Vector3(-1.17f, 1.2f, 3.7f), Vector3.right, new Vector2(0.95f, 1.5f), "ARMARIO EVA");
        var suit = BuildSuit(mc, new Vector3(-0.88f, 0.3f, 3.45f));
        // El +X local de una consola que mira a +X es -Z del mundo: x = -0.3 → z ≈ 4.0 (libre del traje)
        Button(suitC, "Btn Equipar Traje", new Vector3(-0.3f, -0.05f, 0f), MissionButton.Mode.CompleteAction, MissionAction.SuitEquipped,
               "Equiparse con el traje", "EQUIPAR\nTRAJE", mGreen, zone: lockZone);
        var r4 = reactions.gameObject.AddComponent<MissionReaction>();
        r4.onAction = MissionAction.SuitEquipped;
        r4.disableObjects = new[] { suit };
        r4.message = "Traje EMU equipado.";

        var mmuC = Console(mc, "Unidad MMU", new Vector3(-1.17f, 1.3f, 5.6f), Vector3.right, new Vector2(0.62f, 0.8f), "UNIDAD DE MANIOBRA (MMU)");
        BuildMmu(mmuC);
        Button(mmuC, "Btn MMU", new Vector3(0f, -0.2f, 0f), MissionButton.Mode.CompleteAction, MissionAction.MMUActivated,
               "Activar unidad de propulsión", "ACTIVAR MMU", mCyan, zone: lockZone);
        var r14 = reactions.gameObject.AddComponent<MissionReaction>();
        r14.onAction = MissionAction.MMUActivated;
        r14.setPlayerSpeed = 3.6f;
        r14.message = "MMU activa: más empuje para la EVA. Sal por la escotilla exterior.";

        // Pared +X (x = +1.2): sellado/presión, preparación EVA, despresurizar
        var prepC = Console(mc, "Consola Preparación", new Vector3(1.17f, 1.3f, 4.2f), Vector3.left, new Vector2(0.6f, 0.75f), "PREPARACIÓN EVA");
        Button(prepC, "Btn Preparación", new Vector3(0f, 0f, 0f), MissionButton.Mode.CompleteAction, MissionAction.EVAPreparationCompleted,
               "Completar la preparación EVA", "PREPARACIÓN\nCOMPLETA", mGreen, zone: lockZone);

        var depC = Console(mc, "Panel Despresurización", new Vector3(1.17f, 1.3f, 5.6f), Vector3.left, new Vector2(0.6f, 0.75f), "ESCLUSA · PRESIÓN");
        Button(depC, "Btn Despresurizar", new Vector3(0f, 0f, 0f), MissionButton.Mode.CompleteAction, MissionAction.AirlockDepressurized,
               "Despresurizar la esclusa", "DESPRESURIZAR", mRed, hold: 3f, zone: lockZone, big: true);
        var r7 = reactions.gameObject.AddComponent<MissionReaction>();
        r7.onAction = MissionAction.AirlockDepressurized;
        r7.forceClosed = new[] { inner };
        r7.forceOpen = new[] { outer };
        r7.message = "Esclusa despresurizada: escotilla exterior abierta.";

        var r10 = reactions.gameObject.AddComponent<MissionReaction>();
        r10.onAction = MissionAction.ReturnedToAirlock;
        r10.forceClosed = new[] { outer };
        r10.message = "De vuelta en la esclusa. Sella la fuga para represurizar.";

        // Mesa de sellado (pistola de grasa) → AirlockRepressurized
        var seal = BuildRepairStation(mc, "Mesa Sellado · Represurizar", new Vector3(1.12f, 1.25f, 2.6f), Vector3.left,
                                      RepairStationType.GreaseGun_PressureSealant, SpaceToolType.GreaseGun,
                                      "SELLADO DE FUGA · ESCLUSA", true, MissionAction.AirlockRepressurized);
        BuildTool(mc, SpaceToolType.GreaseGun, new Vector3(1.0f, 0.75f, 3.25f), Vector3.left);
        var r11 = reactions.gameObject.AddComponent<MissionReaction>();
        r11.onAction = MissionAction.AirlockRepressurized;
        r11.backToProximity = new[] { inner };
        r11.message = "Esclusa represurizada. Ve a la Cúpula a verificar la energía.";

        // ── CÚPULA: verificación final + viaje a 01_Orbit ───────────────────
        var verC = Console(mc, "Consola Verificación", new Vector3(-1.27f, 1.25f, -3.4f), Vector3.right, new Vector2(0.6f, 0.8f), "ENERGÍA DE LA ESTACIÓN");
        Button(verC, "Btn Verificar Energía", new Vector3(0f, 0f, 0f), MissionButton.Mode.CompleteAction, MissionAction.StationPowerVerified,
               "Verificar estabilización energética", "VERIFICAR\nENERGÍA", mGreen, big: true);

        var tpC = Console(mc, "Consola Vista Orbital", new Vector3(1.27f, 1.25f, -3.4f), Vector3.left, new Vector2(0.6f, 0.8f), "VISTA ORBITAL");
        var tpBtn = new GameObject("Btn Vista Orbital").transform;
        tpBtn.SetParent(tpC, false);
        var tpCap = Prim(PrimitiveType.Cylinder, "Cap", tpBtn, new Vector3(0f, 0f, 0.05f), new Vector3(0.14f, 0.02f, 0.14f), mCyan,
                         true, false, false, Quaternion.Euler(90f, 0f, 0f));
        NovaText.CreateFacing(tpBtn, "Label", "VER LA ISS\nDESDE FUERA\n(01_Orbit)", new Vector3(0f, -0.18f, 0.03f), new Vector2(0.4f, 0.14f), 24, Color.white);
        var tp = tpBtn.gameObject.AddComponent<SceneTeleporter>();
        tp.sceneName = "01_Orbit";
        tp.prompt = "Ir a la vista orbital exterior";
        tp.highlightRenderers = new[] { tpCap.GetComponent<Renderer>() };

        // ── DESTINY: taller de práctica (las otras 2 mesas portadas) ────────
        BuildRepairStation(mc, "Mesa Martillo (práctica)", new Vector3(-8.6f, 1.3f, 0.93f), Vector3.back,
                           RepairStationType.Hammer_ArmorStraightening, SpaceToolType.Hammer, "TALLER · BLINDAJE", false, MissionAction.RepairConfirmed);
        BuildTool(mc, SpaceToolType.Hammer, new Vector3(-7.75f, 0.95f, 0.86f), Vector3.back);
        BuildRepairStation(mc, "Mesa Carraca (práctica)", new Vector3(-8.6f, 1.3f, -0.93f), Vector3.forward,
                           RepairStationType.Ratchet_FlangeBolts, SpaceToolType.Ratchet, "TALLER · BRIDA", false, MissionAction.RepairConfirmed);
        BuildTool(mc, SpaceToolType.Ratchet, new Vector3(-7.75f, 0.95f, -0.86f), Vector3.forward);
        Label(mc, "TALLER DE PRÁCTICA (opcional): coge una herramienta y repara", new Vector3(-8.2f, 2.02f, 0f), Vector3.right,
              new Vector2(1.6f, 0.1f), 36, new Color(0.15f, 0.2f, 0.3f));

        // ── Ambiente y reinicio ─────────────────────────────────────────────
        var alarm = systems.gameObject.AddComponent<AlarmLights>();
        alarm.alarmRenderers = alarmStrips.ToArray();
        alarm.alarmLights = new[] { PointLight(systems, "Alarm Light", new Vector3(0f, 2.1f, 0f), 7f, 0f, Color.red) };

        var rs = systems.gameObject.AddComponent<MissionResetSpawn>();
        rs.spawnBay = spawnBay;
        rs.player = player;

        report.Add("Misión 1 (6 objetivos) y Misión 2 (10 objetivos) cableadas; 4 mesas de reparación + 4 herramientas.");
    }


    static GameObject BuildOru(Transform parent, Vector3 pos)
    {
        var oru = new GameObject("Módulo Energético (ORU)");
        oru.transform.SetParent(parent, false);
        oru.transform.position = pos;
        oru.transform.rotation = Quaternion.LookRotation(Vector3.back);
        var body = Box("Body", oru.transform, Vector3.zero, new Vector3(0.34f, 0.24f, 0.22f), mOru, true, false);
        Box("Stripe", oru.transform, new Vector3(0f, 0.06f, 0.112f), new Vector3(0.34f, 0.03f, 0.005f), mBlackPanel, false, false);
        Box("LED", oru.transform, new Vector3(0.12f, -0.07f, 0.113f), new Vector3(0.025f, 0.025f, 0.005f), mGreen, false, false);
        Box("Handle", oru.transform, new Vector3(0f, 0.15f, 0f), new Vector3(0.22f, 0.025f, 0.03f), mMetal, false, false);
        NovaText.CreateFacing(oru.transform, "Tag", "ORU-PWR", new Vector3(0f, -0.02f, 0.115f), new Vector2(0.2f, 0.05f), 26, Color.black);
        return oru;
    }

    static GameObject BuildSuit(Transform parent, Vector3 floorPos)
    {
        var s = new GameObject("Traje EMU (en el armario)");
        s.transform.SetParent(parent, false);
        s.transform.position = floorPos;
        s.transform.rotation = Quaternion.LookRotation(Vector3.right);
        s.transform.localScale = Vector3.one * 0.85f;
        var t = s.transform;
        Prim(PrimitiveType.Capsule, "Torso", t, new Vector3(0f, 1.15f, 0f), new Vector3(0.5f, 0.38f, 0.36f), mSuit, false, false, true);
        Box("PLSS (mochila)", t, new Vector3(0f, 1.18f, -0.25f), new Vector3(0.48f, 0.6f, 0.2f), mSuit, false, false);
        Prim(PrimitiveType.Sphere, "Casco", t, new Vector3(0f, 1.62f, 0.02f), Vector3.one * 0.3f, mSuit, false, false);
        Prim(PrimitiveType.Sphere, "Visor", t, new Vector3(0f, 1.62f, 0.09f), new Vector3(0.24f, 0.2f, 0.18f), mVisor, false, false);
        Prim(PrimitiveType.Capsule, "Brazo L", t, new Vector3(-0.33f, 1.08f, 0f), new Vector3(0.16f, 0.32f, 0.16f), mSuit, false, false);
        Prim(PrimitiveType.Capsule, "Brazo R", t, new Vector3( 0.33f, 1.08f, 0f), new Vector3(0.16f, 0.32f, 0.16f), mSuit, false, false);
        Prim(PrimitiveType.Capsule, "Pierna L", t, new Vector3(-0.12f, 0.45f, 0f), new Vector3(0.18f, 0.38f, 0.18f), mSuit, false, false);
        Prim(PrimitiveType.Capsule, "Pierna R", t, new Vector3( 0.12f, 0.45f, 0f), new Vector3(0.18f, 0.38f, 0.18f), mSuit, false, false);
        Box("Panel DCM", t, new Vector3(0f, 0.98f, 0.2f), new Vector3(0.22f, 0.1f, 0.08f), mRackDark, false, false);
        return s;
    }

    static void BuildMmu(Transform console)
    {
        var m = new GameObject("MMU (visual)").transform;
        m.SetParent(console, false);
        m.localPosition = new Vector3(0f, 0.12f, 0.1f);
        Box("Body", m, Vector3.zero, new Vector3(0.36f, 0.3f, 0.14f), mHull, false, false);
        Box("Arm L", m, new Vector3(-0.22f, -0.05f, 0.08f), new Vector3(0.05f, 0.05f, 0.2f), mMetal, false, false);
        Box("Arm R", m, new Vector3( 0.22f, -0.05f, 0.08f), new Vector3(0.05f, 0.05f, 0.2f), mMetal, false, false);

        // Toberas con la malla portada de StationMeshBuilder (NASA-SPACE-APPS)
        Mesh nozzle = SaveMesh("Mesh_Nozzle", () => StationMeshBuilder.CreateTaperedCylinderMesh(0.018f, 0.03f, 0.05f, 10));
        foreach (var p in new[] { new Vector3(-0.15f, 0.13f, 0f), new Vector3(0.15f, 0.13f, 0f), new Vector3(-0.15f, -0.13f, 0f), new Vector3(0.15f, -0.13f, 0f) })
        {
            var n = new GameObject("Tobera");
            n.transform.SetParent(m, false);
            n.transform.localPosition = p;
            n.AddComponent<MeshFilter>().sharedMesh = nozzle;
            var r = n.AddComponent<MeshRenderer>();
            r.sharedMaterial = mMetal;
            r.shadowCastingMode = ShadowCastingMode.Off;
        }
    }

    static Mesh SaveMesh(string name, System.Func<Mesh> factory)
    {
        string path = MeshDir + "/" + name + ".asset";
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing != null) return existing;
        Mesh m = null;
        try { m = factory(); }
        catch (System.Exception ex) { Debug.LogWarning("[BuildInteriorScene] No se pudo generar " + name + ": " + ex.Message); }
        if (m == null) return Resources.GetBuiltinResource<Mesh>("Cylinder.fbx");
        AssetDatabase.CreateAsset(m, path);
        return m;
    }

    static HatchDoor BuildHatch(Transform parent, string name, Vector3 center, Vector3 facing, HatchDoor.Mode mode)
    {
        var h = new GameObject(name).transform;
        h.SetParent(parent, false);
        h.position = center;
        h.rotation = Quaternion.LookRotation(facing, Vector3.up);

        // Pivote en el borde superior: la hoja se "enrolla" hacia arriba
        var pivot = new GameObject("Hoja (pivote)").transform;
        pivot.SetParent(h, false);
        pivot.localPosition = new Vector3(0f, HatchSize.y * 0.5f, 0f);
        Box("Hoja", pivot, new Vector3(0f, -HatchSize.y * 0.5f, 0f), new Vector3(HatchSize.x + 0.04f, HatchSize.y + 0.04f, 0.05f), mPanel, true, false, true);
        Box("Ventanilla", pivot, new Vector3(0f, -HatchSize.y * 0.35f, 0.03f), new Vector3(0.2f, 0.2f, 0.01f), mGlass, false, false);
        Box("Manivela", pivot, new Vector3(0f, -HatchSize.y * 0.6f, 0.04f), new Vector3(0.35f, 0.04f, 0.04f), mMetal, false, false);

        var light = Box("Indicador", h, new Vector3(HatchSize.x * 0.5f + 0.12f, HatchSize.y * 0.5f, 0.02f), new Vector3(0.06f, 0.06f, 0.02f), mAmber, false, false);

        var door = h.gameObject.AddComponent<HatchDoor>();
        door.panel = pivot;
        door.rollUp = true;
        door.mode = mode;
        door.detectionRadius = 2.0f;
        door.statusLight = light.GetComponent<Renderer>();
        return door;
    }

    // ── Bahía robótica (fuera del extremo de Zvezda) ───────────────────────
    struct ArmRefs { public Transform armBase, segment, gripper, carried, pick, install; public Renderer socketLight; }

    static ArmRefs BuildRoboticBay(Transform parent)
    {
        var bay = new GameObject("Bahía Robótica (exterior)").transform;
        bay.SetParent(parent, false);
        float x0 = SvcC.x + SvcS.x * 0.5f + HullOff + 0.1f;   // justo fuera del casco

        Box("Plataforma", bay, new Vector3(x0 + 1.8f, -0.1f, 0f), new Vector3(3.6f, 0.08f, 2.8f), mMetal, true, true, true);
        foreach (var z in new[] { -1.35f, 1.35f })
        {
            Box("Larguero", bay, new Vector3(x0 + 1.8f, 1.2f, z), new Vector3(3.6f, 0.08f, 0.08f), mFrame, false, true, true);
            Box("Poste A", bay, new Vector3(x0 + 3.55f, 1.15f, z), new Vector3(0.08f, 2.5f, 0.08f), mFrame, false, true, true);
        }

        // Salida de la compuerta (en la pared exterior) y unidad de energía averiada
        Box("Salida Compuerta", bay, new Vector3(x0 + 0.15f, 1.2f, 0.75f), new Vector3(0.3f, 0.45f, 0.5f), mRackDark, false);
        var pick = new GameObject("Punto Recogida").transform;
        pick.SetParent(bay, false);
        pick.position = new Vector3(x0 + 0.45f, 1.2f, 0.75f);

        var unit = Box("Unidad de Energía", bay, new Vector3(x0 + 3.0f, 0.9f, -0.6f), new Vector3(0.7f, 1.0f, 0.7f), mHullGold, false, true, true);
        var sock = Box("Zócalo (luz)", bay, new Vector3(x0 + 2.62f, 1.2f, -0.6f), new Vector3(0.04f, 0.3f, 0.36f), mRed, false, false);
        var install = new GameObject("Punto Instalación").transform;
        install.SetParent(bay, false);
        install.position = new Vector3(x0 + 2.45f, 1.2f, -0.6f);

        // Brazo: base + segmento telescópico + pinza
        var armBase = Prim(PrimitiveType.Cylinder, "Base Brazo", bay, new Vector3(x0 + 1.5f, 0.05f, 0.2f), new Vector3(0.35f, 0.08f, 0.35f), mHull, false, false, true).transform;
        var segment = new GameObject("Segmento (pivote)").transform;
        segment.SetParent(bay, false);
        segment.position = armBase.position;
        Rod("Brazo", segment, new Vector3(0f, 0.5f, 0f), 0.06f, 1f, Vector3.up, mHull, false, false);
        var gripper = new GameObject("Pinza").transform;
        gripper.SetParent(bay, false);
        gripper.position = pick.position;
        Box("Pinza A", gripper, new Vector3(0f, 0.08f, 0f), new Vector3(0.2f, 0.04f, 0.25f), mMetal, false, false);
        Box("Pinza B", gripper, new Vector3(0f, -0.08f, 0f), new Vector3(0.2f, 0.04f, 0.25f), mMetal, false, false);
        var carried = BuildOru(gripper, gripper.position);
        Object.DestroyImmediate(carried.GetComponentInChildren<Collider>());
        carried.name = "Módulo en la pinza";
        carried.transform.localScale = Vector3.one * 0.6f;
        carried.SetActive(false);

        PointLight(bay, "Luz Bahía", new Vector3(x0 + 1.8f, 2.4f, 0f), 6f, 1.4f, new Color(0.85f, 0.95f, 1f));
        Label(bay, "BAHÍA ROBÓTICA · UNIDAD DE ENERGÍA", new Vector3(x0 + 3.5f, 2.2f, 0f), Vector3.left, new Vector2(1.6f, 0.14f), 60, new Color(0.9f, 0.95f, 1f));

        return new ArmRefs
        {
            armBase = armBase, segment = segment, gripper = gripper, carried = carried.transform,
            pick = pick, install = install, socketLight = sock.GetComponent<Renderer>()
        };
    }

    // ── Mesas de reparación (geometría para la lógica portada) ─────────────
    static SpaceRepairStation BuildRepairStation(Transform parent, string name, Vector3 pos, Vector3 facing,
                                                 RepairStationType type, SpaceToolType tool, string title,
                                                 bool linkMission, MissionAction action)
    {
        var st = new GameObject(name).transform;
        st.SetParent(parent, false);
        st.position = pos;
        st.rotation = Quaternion.LookRotation(facing, Vector3.up);

        // Tablero
        var board = Box("Tablero", st, Vector3.zero, new Vector3(0.8f, 0.7f, 0.05f), mBlackPanel, true, false);
        Box("Marco", st, new Vector3(0f, 0f, -0.012f), new Vector3(0.84f, 0.74f, 0.03f), mMetal, false, false);

        var rs = st.gameObject.AddComponent<SpaceRepairStation>();
        rs.stationType = type;
        rs.requiredTool = tool;
        rs.stationTitle = title;
        rs.linkToMission = linkMission;
        rs.missionAction = action;

        // Textos holográficos
        rs.uiTitleText       = NovaText.CreateFacing(st, "UI Title", title, new Vector3(0f, 0.31f, 0.03f), new Vector2(0.76f, 0.05f), 26, new Color(0.6f, 0.9f, 1f));
        rs.uiModeText        = NovaText.CreateFacing(st, "UI Mode", "", new Vector3(0f, 0.25f, 0.03f), new Vector2(0.76f, 0.05f), 18, Color.white);
        rs.uiStatusText      = NovaText.CreateFacing(st, "UI Status", "", new Vector3(0f, -0.25f, 0.03f), new Vector2(0.76f, 0.08f), 18, Color.white);
        rs.uiProgressBarText = NovaText.CreateFacing(st, "UI Progress", "", new Vector3(0f, -0.31f, 0.03f), new Vector2(0.76f, 0.04f), 18, new Color(0.8f, 0.9f, 1f));

        var fillRoot = new GameObject("Barra Progreso").transform;
        fillRoot.SetParent(st, false);
        fillRoot.localPosition = new Vector3(0f, 0.2f, 0.03f);
        fillRoot.localScale = new Vector3(0.6f, 0.015f, 0.008f);
        var fill = Box("Relleno", fillRoot, Vector3.zero, Vector3.one, mGreen, false, false);
        rs.uiProgressFillRenderer = fill.GetComponent<MeshRenderer>();

        var work = new GameObject("Zona de trabajo").transform;
        work.SetParent(st, false);
        work.localPosition = new Vector3(0f, -0.02f, 0.03f);

        switch (type)
        {
            case RepairStationType.PistolGrip_WelderAndDrill:
            {
                // Placas a soldar (el script mueve los pivotes en X: ∓0.045 → ∓0.001)
                var lp = new GameObject("Placa Izq (pivote)").transform; lp.SetParent(work, false); lp.localPosition = new Vector3(-0.045f, 0.05f, 0.02f);
                var rp = new GameObject("Placa Der (pivote)").transform; rp.SetParent(work, false); rp.localPosition = new Vector3( 0.045f, 0.05f, 0.02f);
                Box("Placa", lp, new Vector3(-0.1f, 0f, 0f), new Vector3(0.2f, 0.16f, 0.02f), mMetal, true, false);
                Box("Placa", rp, new Vector3( 0.1f, 0f, 0f), new Vector3(0.2f, 0.16f, 0.02f), mMetal, true, false);
                rs.weldLeftPlate = lp; rs.weldRightPlate = rp;
                var seam = Box("Costura", work, new Vector3(0f, 0.05f, 0.025f), new Vector3(0.012f, 0.16f, 0.01f), mAmber, false, false);
                rs.weldSeamRenderer = seam.GetComponent<MeshRenderer>();
                rs.weldSparksParticles = Sparks(work, new Vector3(0f, 0.05f, 0.04f), mParticleAdd, false);

                // Perno de precisión: el montaje mira hacia DENTRO del tablero para que el perno se hunda
                var mount = new GameObject("Montaje Perno").transform;
                mount.SetParent(work, false);
                mount.localPosition = new Vector3(0f, -0.13f, 0.02f);
                mount.localRotation = Quaternion.Euler(0f, 180f, 0f);
                var screw = new GameObject("Perno").transform;
                screw.SetParent(mount, false);
                screw.localPosition = new Vector3(0f, 0f, -0.085f);   // sobresale; se hunde hacia +Z del montaje
                screw.localRotation = Quaternion.Euler(0f, 180f, 0f);  // su +Z mira al jugador
                Rod("Cabeza", screw, Vector3.zero, 0.025f, 0.03f, Vector3.forward, mMetal, true, false);
                Rod("Rosca", screw, new Vector3(0f, 0f, -0.05f), 0.01f, 0.08f, Vector3.forward, mMetal, false, false);
                rs.precisionScrewTransform = screw;
                var ring = Rod("Indicador", mount, new Vector3(0f, 0f, 0.0f), 0.04f, 0.006f, Vector3.forward, mAmber, false, false);
                rs.screwIndicatorRenderer = ring.GetComponent<MeshRenderer>();
                break;
            }
            case RepairStationType.Hammer_ArmorStraightening:
            {
                // Montaje girado 180° para que la rotación (+36° en X) doble la placa HACIA el jugador
                var mountH = new GameObject("Montaje Bisagra").transform;
                mountH.SetParent(work, false);
                mountH.localPosition = new Vector3(0f, 0.12f, 0.05f);
                mountH.localRotation = Quaternion.Euler(0f, 180f, 0f);
                var hinge = new GameObject("Placa Doblada (bisagra)").transform;
                hinge.SetParent(mountH, false);
                hinge.localPosition = Vector3.zero;
                Box("Placa Blindaje", hinge, new Vector3(0f, -0.11f, 0f), new Vector3(0.36f, 0.22f, 0.02f), mHullGold, true, false);
                rs.bentArmorPlateTransform = hinge;
                rs.hammerImpactSparks = Sparks(work, new Vector3(0f, 0f, 0.06f), mParticleAdd, false);
                break;
            }
            case RepairStationType.Ratchet_FlangeBolts:
            {
                // Brida: su +Y local mira al jugador (los pernos giran en Y y se hunden en -Y)
                var flange = new GameObject("Brida").transform;
                flange.SetParent(work, false);
                flange.localPosition = new Vector3(0f, 0f, 0.02f);
                flange.localRotation = Quaternion.Euler(90f, 0f, 0f);
                Prim(PrimitiveType.Cylinder, "Disco", flange, Vector3.zero, new Vector3(0.3f, 0.01f, 0.3f), mMetal, true, false);
                var pipe = Rod("Tubería", work, new Vector3(0f, 0f, -0.0f), 0.06f, 0.75f, Vector3.right, mRackDark, false, false);
                rs.pipeFlowGlowRenderer = pipe.GetComponent<MeshRenderer>();
                var bolts = new Transform[6];
                for (int i = 0; i < bolts.Length; i++)
                {
                    float a = i * Mathf.PI * 2f / bolts.Length;
                    var b = Prim(PrimitiveType.Cylinder, "Perno " + (i + 1), flange,
                                 new Vector3(Mathf.Cos(a) * 0.11f, 0.05f, Mathf.Sin(a) * 0.11f), new Vector3(0.035f, 0.015f, 0.035f),
                                 mHullGold, true, false);
                    bolts[i] = b.transform;
                }
                rs.flangeBolts = bolts;
                break;
            }
            case RepairStationType.GreaseGun_PressureSealant:
            {
                var breach = new GameObject("Fisura").transform;
                breach.SetParent(work, false);
                breach.localPosition = new Vector3(0f, 0.02f, 0.03f);
                Box("Grieta", breach, Vector3.zero, new Vector3(0.18f, 0.025f, 0.01f), mRed, false, false);
                var plug = new GameObject("Tapón Espuma").transform;
                plug.SetParent(breach, false);
                plug.localRotation = Quaternion.Euler(90f, 0f, 0f);
                plug.localScale = Vector3.zero;
                Prim(PrimitiveType.Sphere, "Espuma", plug, Vector3.zero, Vector3.one, mBag, false, false);
                rs.foamSealantPlug = plug;
                rs.pressureLeakParticles = Sparks(breach, new Vector3(0f, 0f, 0.02f), mParticleMist, true);
                var gauge = Box("Manómetro", work, new Vector3(0.28f, -0.1f, 0.01f), new Vector3(0.04f, 0.25f, 0.01f), mRed, false, false);
                rs.pressureGaugeBarRenderer = gauge.GetComponent<MeshRenderer>();
                Box("Zona de impacto", work, new Vector3(0f, 0.02f, 0.02f), new Vector3(0.4f, 0.3f, 0.01f), mRackDark, true, false);
                break;
            }
        }

        return rs;
    }

    static ParticleSystem Sparks(Transform parent, Vector3 localPos, Material mat, bool playOnAwake)
    {
        var go = new GameObject("Partículas");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.playOnAwake = playOnAwake;
        main.loop = true;
        main.startLifetime = playOnAwake ? 0.8f : 0.3f;
        main.startSpeed = playOnAwake ? 0.4f : 1.4f;
        main.startSize = playOnAwake ? 0.04f : 0.012f;
        main.maxParticles = 60;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 0f;
        var em = ps.emission;
        em.rateOverTime = playOnAwake ? 25f : 120f;
        var sh = ps.shape;
        sh.shapeType = ParticleSystemShapeType.Cone;
        sh.angle = playOnAwake ? 15f : 30f;
        sh.radius = 0.01f;
        var r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = ShadowCastingMode.Off;
        r.receiveShadows = false;
        if (!playOnAwake) ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        return ps;
    }

    // ── Herramientas (modelos GLB de NASA, con respaldo procedural) ────────
    static SpaceTool BuildTool(Transform parent, SpaceToolType type, Vector3 dockPos, Vector3 facing)
    {
        var dock = new GameObject("Soporte " + SpaceTool.DisplayName(type)).transform;
        dock.SetParent(parent, false);
        dock.position = dockPos;
        dock.rotation = Quaternion.LookRotation(facing, Vector3.up);
        Box("Abrazadera", dock, new Vector3(0f, -0.06f, -0.06f), new Vector3(0.22f, 0.03f, 0.1f), mMetal, false, false);

        // Se construye en el origen sin rotación y se cuelga del soporte al final
        var toolGO = new GameObject(SpaceTool.DisplayName(type));

        string file; float length; Material mat;
        switch (type)
        {
            case SpaceToolType.PistolGripTool: file = "PistolGripTool.glb"; length = 0.30f; mat = mToolPGT; break;
            case SpaceToolType.Hammer:         file = "Hammer.glb";         length = 0.34f; mat = mToolHammer; break;
            case SpaceToolType.Ratchet:        file = "Ratchet.glb";        length = 0.30f; mat = mToolRatchet; break;
            default:                           file = "GreaseGun.glb";      length = 0.36f; mat = mToolGrease; break;
        }

        GameObject model = null;
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(ToolDir + "/Models/" + file);
        if (asset != null) model = PrefabUtility.InstantiatePrefab(asset) as GameObject;

        if (model != null)
        {
            if (PrefabUtility.IsPartOfPrefabInstance(model))
                PrefabUtility.UnpackPrefabInstance(model, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            model.transform.SetParent(toolGO.transform, false);
            foreach (var c in model.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
            foreach (var r in model.GetComponentsInChildren<Renderer>(true))
            {
                var mats = new Material[r.sharedMaterials.Length];
                for (int i = 0; i < mats.Length; i++) mats[i] = mat;
                r.sharedMaterials = mats;
                r.shadowCastingMode = ShadowCastingMode.Off;
            }
            NormalizeModel(model.transform, toolGO.transform, length);
        }
        else
        {
            report.Add("AVISO: no se pudo cargar " + file + " (se usa una forma simple).");
            Box("Cuerpo", toolGO.transform, new Vector3(0f, 0f, 0.06f), new Vector3(0.06f, 0.08f, 0.2f), mat, false, false);
            Box("Mango", toolGO.transform, new Vector3(0f, -0.07f, 0f), new Vector3(0.04f, 0.12f, 0.05f), mat, false, false);
        }

        toolGO.transform.SetParent(dock, false);
        toolGO.transform.localPosition = Vector3.zero;
        toolGO.transform.localRotation = Quaternion.identity;

        var col = toolGO.AddComponent<BoxCollider>();
        col.size = new Vector3(0.12f, 0.14f, length);
        col.center = Vector3.zero;

        var tool = toolGO.AddComponent<SpaceTool>();
        tool.toolType = type;
        tool.prompt = SpaceTool.DisplayName(type);
        tool.dockPoint = dock;
        tool.desktopHoldEuler = new Vector3(5f, -8f, 0f);
        if (type == SpaceToolType.PistolGripTool || type == SpaceToolType.GreaseGun)
            tool.effectParticles = Sparks(toolGO.transform, new Vector3(0f, 0f, length * 0.5f),
                                          type == SpaceToolType.GreaseGun ? mParticleMist : mParticleAdd, false);

        NovaText.CreateFacing(dock, "Etiqueta", SpaceTool.DisplayName(type), new Vector3(0f, -0.1f, -0.0f), new Vector2(0.3f, 0.04f), 22, new Color(0.15f, 0.2f, 0.3f));
        return tool;
    }

    /// <summary>Escala el modelo para que su lado mayor mida 'length' y lo centra en el padre.</summary>
    static void NormalizeModel(Transform model, Transform parent, float length)
    {
        model.localPosition = Vector3.zero;
        model.localRotation = Quaternion.identity;
        model.localScale = Vector3.one;

        var rs = model.GetComponentsInChildren<Renderer>(true);
        if (rs.Length == 0) return;
        Bounds b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        float max = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
        if (max < 1e-5f) return;

        float k = length / max;
        model.localScale = Vector3.one * k;

        // Recalcular y centrar
        b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        model.position += parent.position - b.center;

        // Orientar el lado más largo a lo largo de +Z (hacia donde apunta la mano)
        Vector3 s = b.size;
        if (s.y >= s.x && s.y >= s.z) model.RotateAround(parent.position, parent.right, 90f);
        else if (s.x >= s.y && s.x >= s.z) model.RotateAround(parent.position, parent.up, 90f);
    }

    // ════════════════════════════════════════════════════════════════════════
    // EXTERIOR (EVA)
    // ════════════════════════════════════════════════════════════════════════

    static void BuildExterior(Transform root)
    {
        var ext = new GameObject("Exterior EVA").transform;
        ext.SetParent(root, false);

        // Celosía (truss) desde la esclusa hacia el ala solar, a un lado de la ruta
        Box("Celosía", ext, new Vector3(1.9f, 1.2f, 13.4f), new Vector3(0.7f, 0.7f, 12.6f), mMetal, true, true, true);
        for (int i = 0; i < 12; i++)
            Box("Costilla", ext, new Vector3(1.9f, 1.2f, 7.6f + i * 1.05f), new Vector3(0.78f, 0.78f, 0.06f), mFrame, false, true, true);
        Rod("Pasamanos EVA", ext, new Vector3(1.45f, 1.45f, 13.4f), 0.018f, 12.4f, Vector3.forward, mAmber);
        Rod("Pasamanos EVA", ext, new Vector3(1.45f, 0.95f, 13.4f), 0.018f, 12.4f, Vector3.forward, mAmber);
        Label(ext, "RUTA EVA → PANEL SOLAR", new Vector3(1.45f, 1.85f, 7.8f), Vector3.left, new Vector2(1.4f, 0.14f), 60, new Color(1f, 0.85f, 0.4f));

        // Mástil y ala de paneles solares (dos mantas a cada lado)
        float zw = 20.4f;
        Rod("Mástil", ext, new Vector3(0f, 1.2f, zw), 0.12f, 4.2f, Vector3.up, mMetal, true);
        Box("Viga Ala", ext, new Vector3(0f, 1.2f, zw), new Vector3(27f, 0.16f, 0.16f), mMetal, true, true, true);
        foreach (int side in new[] { -1, 1 })
        {
            var blanket = Box("Manta Solar", ext, new Vector3(side * 7.6f, 1.2f, zw + 0.12f), new Vector3(12f, 4f, 0.04f), mSolar, true, true, true);
            Box("Marco Manta", ext, new Vector3(side * 7.6f, 3.22f, zw + 0.12f), new Vector3(12.1f, 0.05f, 0.06f), mFrame, false, true, true);
            Box("Marco Manta", ext, new Vector3(side * 7.6f, -0.82f, zw + 0.12f), new Vector3(12.1f, 0.05f, 0.06f), mFrame, false, true, true);
        }
        // Avería visible en la manta derecha
        Box("Daño (quemadura)", ext, new Vector3(2.6f, 1.7f, zw + 0.09f), new Vector3(0.9f, 0.6f, 0.01f), mRed, false, false);
        Box("Grieta", ext, new Vector3(2.4f, 1.55f, zw + 0.085f), new Vector3(1.4f, 0.04f, 0.01f), mAmber, false, false);
        Label(ext, "SECCIÓN DAÑADA", new Vector3(2.6f, 2.15f, zw + 0.08f), Vector3.back, new Vector2(1.0f, 0.14f), 60, new Color(1f, 0.5f, 0.35f));

        // Zona de llegada al panel (SolarPanelReached)
        var zGO = new GameObject("Zona Panel Solar");
        zGO.transform.SetParent(ext, false);
        zGO.transform.position = new Vector3(0.6f, 1.2f, 18.6f);
        var z = zGO.AddComponent<MissionZone>();
        z.zoneName = "junto al panel solar";
        z.size = new Vector3(3.4f, 3.2f, 2.6f);
        z.completeAction = true;
        z.action = MissionAction.SolarPanelReached;

        // Mesa de reparación del panel (PGT: soldar + atornillar) → SolarPanelRepaired
        Box("Soporte Mesa", ext, new Vector3(0.9f, 1.25f, 19.95f), new Vector3(0.9f, 0.8f, 0.5f), mMetal, true, true, true);
        BuildRepairStation(ext, "Mesa Panel Solar · Reparación EVA", new Vector3(0.9f, 1.25f, 19.68f), Vector3.back,
                           RepairStationType.PistolGrip_WelderAndDrill, SpaceToolType.PistolGripTool,
                           "REPARACIÓN DEL PANEL SOLAR", true, MissionAction.SolarPanelRepaired);
        BuildTool(ext, SpaceToolType.PistolGripTool, new Vector3(-0.1f, 1.0f, 19.7f), Vector3.back);

        // Luz de trabajo EVA
        var spot = new GameObject("Foco EVA");
        spot.transform.SetParent(ext, false);
        spot.transform.position = new Vector3(0f, 3.2f, 17.5f);
        spot.transform.rotation = Quaternion.LookRotation(new Vector3(0.2f, -0.7f, 1f));
        var sl = spot.AddComponent<Light>();
        sl.type = LightType.Spot;
        sl.range = 8f;
        sl.spotAngle = 60f;
        sl.intensity = 2.5f;
        sl.shadows = LightShadows.None;
        spot.AddComponent<UniversalAdditionalLightData>();

        report.Add("Exterior: celosía, ala solar con avería, mesa EVA (PGT), Tierra de fondo.");
    }

    // ════════════════════════════════════════════════════════════════════════
    // 01_ORBIT: consola de acceso al interior
    // ════════════════════════════════════════════════════════════════════════

    static void AddPortalToOrbit()
    {
        if (!File.Exists(OrbitScene)) { report.Add("AVISO: no existe " + OrbitScene); return; }
        var scene = EditorSceneManager.OpenScene(OrbitScene, OpenSceneMode.Single);

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null && player.GetComponent<NovaInteractionRig>() == null)
        {
            var rig = player.AddComponent<NovaInteractionRig>();
            rig.laserMaterial = mLaser;
            rig.controllerMaterial = mController;
        }
        else if (player != null)
        {
            var rig = player.GetComponent<NovaInteractionRig>();
            rig.laserMaterial = mLaser;
            rig.controllerMaterial = mController;
        }

        var old = GameObject.Find("Acceso Interior ISS");
        if (old != null) Object.DestroyImmediate(old);

        Vector3 basePos = player != null ? player.transform.position : new Vector3(0f, 1.6f, -180f);
        Vector3 fwd = player != null ? Vector3.ProjectOnPlane(player.transform.forward, Vector3.up).normalized : Vector3.forward;
        if (fwd.sqrMagnitude < 0.01f) fwd = Vector3.forward;
        Vector3 right = Vector3.Cross(Vector3.up, fwd);

        var c = new GameObject("Acceso Interior ISS").transform;
        c.position = basePos + fwd * 1.6f + right * 0.9f + Vector3.up * 0.2f;
        c.rotation = Quaternion.LookRotation(-fwd, Vector3.up);
        Box("Plate", c, Vector3.zero, new Vector3(0.55f, 0.6f, 0.04f), mBlackPanel, true, false);
        Box("Bezel", c, new Vector3(0f, 0f, -0.012f), new Vector3(0.59f, 0.64f, 0.03f), mMetal, false, false);
        NovaText.CreateFacing(c, "Title", "ENTRAR A LA ISS\n(interior y misiones)", new Vector3(0f, 0.19f, 0.025f), new Vector2(0.5f, 0.14f), 32, new Color(0.8f, 0.92f, 1f));
        var b = new GameObject("Btn Entrar").transform;
        b.SetParent(c, false);
        b.localPosition = new Vector3(0f, -0.08f, 0f);
        var cap = Prim(PrimitiveType.Cylinder, "Cap", b, new Vector3(0f, 0f, 0.05f), new Vector3(0.16f, 0.02f, 0.16f), mCyan, true, false, false, Quaternion.Euler(90f, 0f, 0f));
        NovaText.CreateFacing(b, "Label", "MANTÉN PARA ENTRAR", new Vector3(0f, -0.15f, 0.03f), new Vector2(0.45f, 0.05f), 22, Color.white);
        var tp = b.gameObject.AddComponent<SceneTeleporter>();
        tp.sceneName = "02_Interior";
        tp.prompt = "Entrar al interior de la ISS";
        tp.highlightRenderers = new[] { cap.GetComponent<Renderer>() };

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        report.Add("01_Orbit: consola 'Entrar a la ISS' + NovaInteractionRig en el Player (sin VRMovement).");
    }
}

#endif
