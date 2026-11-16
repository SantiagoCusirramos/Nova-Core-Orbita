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
/// Nova Core → BUILD Interiores ISS (5 módulos + EVA)
///
/// Construye (y sobrescribe) una escena por cada módulo del modelo ISS_stationary.glb,
/// conectadas por escotillas (al meter la cabeza en la escotilla se pasa al módulo vecino):
///
///   02_Nodo     — "02 Unity Node 1": hub, inicio/reinicio, emergencia, bitácora.
///   03_Destiny  — "09 Destiny Space Laboratory": repuesto energético, taller de práctica.
///   04_Zvezda   — "05 Zvezda (SM) Service Module": compuerta, consola y brazo robótico.
///   05_Quest    — "12 Quest Airlock": traje, MMU, presión, sellado de fuga.
///   06_Cupula   — "38 Cupola": vista de la Tierra, verificación final, vista orbital.
///   07_EVA      — exterior real (copia de 01_Orbit con la ISS a escala 1:1): panel solar.
///
/// En 01_Orbit añade marcadores sobre cada módulo del modelo: apuntar + mantener = entrar.
/// El MissionManager persiste entre escenas: la historia continúa al cambiar de módulo.
/// Es idempotente: se puede ejecutar varias veces.
/// </summary>
public static class BuildIssScenes
{
    public const string SceneDir = "Assets/Game/Scenes/ISS";
    public const string SNodo = "02_Nodo", SDestiny = "03_Destiny", SZvezda = "04_Zvezda",
                        SQuest = "05_Quest", SCupula = "06_Cupula", SEva = "07_EVA",
                        STutorial = "00_Tutorial";
    public static readonly string[] SceneNames = { SNodo, SDestiny, SZvezda, SQuest, SCupula, SEva };
    public static string ScenePathOf(string name) => SceneDir + "/" + name + ".unity";

    const string OrbitScene  = "Assets/NovaCore/Scenes/01_Orbit.unity";
    const string ArtDir      = "Assets/Game/Art/Interiors";
    const string MatDir      = ArtDir + "/Materials";
    const string MeshDir     = ArtDir + "/Meshes";
    const string TexDir      = ArtDir + "/Textures";
    const string ToolDir     = "Assets/Game/Art/Tools";
    const string SkyboxMat   = "Assets/NovaCore/Art/Materials/M_Skybox_DeepSpace.mat";
    const string MissionEnergy = "Assets/Game/Data/Missions/Mission_Energy.asset";
    const string MissionSolar  = "Assets/Game/Data/Missions/Mission_SolarPanel.asset";
    const string AstronautModel = "Assets/Game/Art/Characters/Astronaut.glb";
    const string MoonTexture    = "Assets/Game/Art/Exterior/T_Moon_Disc.png";
    const string TabletPrefab   = "Assets/Game/Prefabs/UI/MissionTablet.prefab";
    const string IntroVideo     = "Assets/Game/Art/Video/Intro_UltimaLuz.mp4";
    const string MusicClip      = "Assets/Game/Audio/Music/Ambient_Orbita.wav";
    const string TutorialIconDir = "Assets/Game/Art/UI/Tutorial";

    // Dimensiones comunes (metros)
    const float H      = 2.4f;   // altura interior de módulo
    const float WallT  = 0.06f;  // grosor de pared interior
    const float HullOff = 0.35f; // separación del casco exterior (bahía robótica)
    static readonly Vector2 HatchSize = new Vector2(1.3f, 1.5f);

    // Materiales
    static Material mWall, mFloor, mRack, mRackDark, mPanel, mHandrail, mMetal, mHull, mHullGold,
                    mLight, mScreen, mRed, mGreen, mAmber, mCyan, mCable, mBag, mGlass, mSolar,
                    mEarth, mAlarm, mOru, mPlant, mPlantLight, mSuit, mVisor, mBlackPanel,
                    mParticleAdd, mParticleMist, mLaser, mMarker, mController, mFrame, mFade,
                    mGlove, mGuide, mMoon, mGold, mVideo;
    static Material mToolPGT, mToolHammer, mToolRatchet, mToolGrease;

    static System.Random rng = new System.Random(1);
    static Transform decorRoot;
    static readonly List<string> report = new List<string>();

    // ════════════════════════════════════════════════════════════════════════
    // MENÚ
    // ════════════════════════════════════════════════════════════════════════

    [MenuItem("Nova Core/BUILD Interiores ISS (5 módulos + EVA)")]
    public static void BuildFromMenu()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        string r = Build();
        EditorUtility.DisplayDialog("Nova Core — Interiores ISS", r, "OK");
    }

    public static string Build()
    {
        report.Clear();
        EnsureFolders();
        ConfigureTextures();
        CreateMaterials();

        BuildEva();          // primero: se copia de 01_Orbit antes de añadirle marcadores
        BuildTutorial();
        BuildNodo();
        BuildDestiny();
        BuildZvezda();
        BuildQuest();
        BuildCupula();
        AddMarkersToOrbit();

        report.Add(NovaProjectSetup.RegisterScenes());
        if (!AssetDatabase.IsValidFolder("Assets/TextMesh Pro"))
            report.Add("AVISO: la tablet de Camila usa TextMeshPro. Ejecuta 'Nova Core > Importar TMP Essentials' (una vez) para que se vean sus textos.");
        EditorSceneManager.OpenScene(ScenePathOf(SNodo), OpenSceneMode.Single);
        AssetDatabase.SaveAssets();

        string msg = "Interiores ISS construidos.\n\n" + string.Join("\n", report) +
                     "\n\nPrueba: Play en 00_Tutorial (entrenamiento) → escotilla → Nodo → INICIAR.";
        Debug.Log("[BuildIssScenes]\n" + msg);
        return msg;
    }

    // ════════════════════════════════════════════════════════════════════════
    // ASSETS
    // ════════════════════════════════════════════════════════════════════════

    static void EnsureFolders()
    {
        foreach (var p in new[] { "Assets/Game/Scenes", SceneDir, ArtDir, MatDir, MeshDir, TexDir, ToolDir, ToolDir + "/Materials" })
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
        var moon = AssetImporter.GetAtPath(MoonTexture) as TextureImporter;
        if (moon != null && (!moon.alphaIsTransparency || moon.wrapMode != TextureWrapMode.Clamp || moon.maxTextureSize != 1024))
        {
            moon.alphaIsTransparency = true;
            moon.wrapMode = TextureWrapMode.Clamp;
            moon.maxTextureSize = 1024;
            moon.mipmapEnabled = true;
            moon.SaveAndReimport();
        }
        var earth = AssetImporter.GetAtPath(TexDir + "/T_Planet_Earth_Albedo.png") as TextureImporter;
        if (earth != null && earth.maxTextureSize != 2048)
        {
            earth.maxTextureSize = 2048;
            earth.SaveAndReimport();
        }

        // Paredes y suelo: mipmaps + filtrado anisótropo (sin esto se "pixelan" y hacen moiré a distancia)
        foreach (var f in new[] { "T_Interior_Wall_Albedo.png", "T_Interior_Wall_Normal.png", "T_Interior_Floor_Albedo.png",
                                  "T_Interior_Floor_Normal.png", "T_Tech_Screen_Emission.png", "T_Solar_Panel_Albedo.png" })
        {
            var ti = AssetImporter.GetAtPath(TexDir + "/" + f) as TextureImporter;
            if (ti == null) continue;
            if (ti.mipmapEnabled && ti.anisoLevel >= 8 && ti.filterMode == FilterMode.Trilinear && ti.wrapMode == TextureWrapMode.Repeat) continue;
            ti.mipmapEnabled = true;
            ti.anisoLevel = 8;
            ti.filterMode = FilterMode.Trilinear;
            ti.wrapMode = TextureWrapMode.Repeat;
            ti.SaveAndReimport();
        }

        // Iconos de las pantallas del tutorial como Sprites
        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { TutorialIconDir }))
        {
            var ti = AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid)) as TextureImporter;
            if (ti == null || (ti.textureType == TextureImporterType.Sprite && ti.alphaIsTransparency)) continue;
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.alphaIsTransparency = true;
            ti.mipmapEnabled = true;
            ti.SaveAndReimport();
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

        mWall      = Lit("M_ISS_Wall",       new Color(0.86f, 0.85f, 0.80f), 0.35f, 0f, wallA, wallN, Vector2.one);
        mFloor     = Lit("M_ISS_Deck",       new Color(0.72f, 0.72f, 0.70f), 0.30f, 0.1f, floorA, floorN, Vector2.one);
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
        mGlove     = Lit("M_ISS_Glove",      new Color(0.93f, 0.93f, 0.90f), 0.25f, 0f);
        mGold      = Lit("M_ISS_PatchGold",  new Color(1f, 0.78f, 0.25f), 0.8f, 0.9f, null, null, Vector2.one, new Color(0.6f, 0.4f, 0.05f));
        var unlit = Shader.Find("Universal Render Pipeline/Unlit");
        if (unlit != null)
        {
            mVideo = LoadOrCreate("M_IntroVideo", unlit, MatDir);
            mVideo.SetColor("_BaseColor", Color.white);
            EditorUtility.SetDirty(mVideo);
        }
        var sprite = Shader.Find("Sprites/Default");
        if (sprite != null)
        {
            mGuide = LoadOrCreate("M_ISS_Guide", sprite, MatDir);
            mMoon  = LoadOrCreate("M_Moon_Disc", sprite, MatDir);
            var moonTex = AssetDatabase.LoadAssetAtPath<Texture2D>(MoonTexture);
            if (moonTex != null) { mMoon.mainTexture = moonTex; EditorUtility.SetDirty(mMoon); }
        }

        var solarA = Tex("T_Solar_Panel_Albedo.png");
        mSolar     = Lit("M_ISS_SolarArray", new Color(0.35f, 0.42f, 0.75f), 0.85f, 0.3f, solarA, null, new Vector2(12, 4));
        mEarth     = Lit("M_ISS_Earth",      Color.white, 0.25f, 0f, Tex("T_Planet_Earth_Albedo.png"), null, Vector2.one, new Color(0.03f, 0.05f, 0.09f));
        mGlass     = LitTransparent("M_ISS_Glass", new Color(0.55f, 0.75f, 0.90f, 0.18f));

        mParticleAdd  = Particle("M_ISS_Particle_Sparks", new Color(1f, 0.75f, 0.35f, 1f), additive: true);
        mParticleMist = Particle("M_ISS_Particle_Mist",   new Color(0.9f, 0.95f, 1f, 0.5f), additive: false);
        mLaser        = Particle("M_ISS_Laser",           Color.white, additive: false);
        var soft = SoftParticleTexture();
        if (soft != null)
            foreach (var pm in new[] { mParticleAdd, mParticleMist })
            {
                pm.SetTexture("_BaseMap", soft);
                pm.mainTexture = soft;
                EditorUtility.SetDirty(pm);
            }
        mFade         = FadeMaterial("M_ISS_Fade");

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

    /// <summary>Negro transparente sin luz, dibujado al final (fundido de pantalla).</summary>
    static Material FadeMaterial(string name)
    {
        var sh = Shader.Find("Universal Render Pipeline/Unlit");
        if (sh == null) sh = LitShader();
        var m = LoadOrCreate(name, sh, null);
        m.SetColor("_BaseColor", new Color(0f, 0f, 0f, 1f));
        m.SetFloat("_Surface", 1f);
        m.SetFloat("_Blend", 0f);
        m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
        m.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
        m.SetFloat("_ZWrite", 0f);
        m.SetOverrideTag("RenderType", "Transparent");
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.renderQueue = 4500;
        EditorUtility.SetDirty(m);
        return m;
    }

    /// <summary>Textura de punto suave para chispas y niebla (sin ella cada partícula es un cuadrado).</summary>
    static Texture2D SoftParticleTexture()
    {
        string path = TexDir + "/T_Particle_Soft.png";
        var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (existing != null) return existing;
        const int n = 64;
        var t = new Texture2D(n, n, TextureFormat.RGBA32, false);
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(n * 0.5f, n * 0.5f)) / (n * 0.5f);
                float a = Mathf.Clamp01(1f - d);
                a = a * a * (3f - 2f * a);
                t.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        File.WriteAllBytes(path, t.EncodeToPNG());
        Object.DestroyImmediate(t);
        AssetDatabase.ImportAsset(path);
        var ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti != null) { ti.alphaIsTransparency = true; ti.wrapMode = TextureWrapMode.Clamp; ti.SaveAndReimport(); }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
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
    // GEOMETRÍA Y DECORACIÓN
    // ════════════════════════════════════════════════════════════════════════
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

        // Paredes y suelo: UVs en metros para que la textura no se estire en módulos largos
        if (type == PrimitiveType.Cube && mat != null && (mat == mWall || mat == mFloor || mat == mRack))
            go.GetComponent<MeshFilter>().sharedMesh = TiledCube(scale, mat == mFloor ? 1.0f : 1.2f);
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
        // El marco invade 1 cm el hueco: si su cara interior coincide con el canto de la pared,
        // las dos mallas se pisan y el borde parpadea (z-fighting).
        opening -= new Vector2(0.02f, 0.02f);
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
    // CONTENIDO DE MISIONES
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>Placa base de consola mirando hacia 'facing' (su +Z local apunta al jugador).</summary>
    static Transform Console(Transform parent, string name, Vector3 pos, Vector3 facing, Vector2 size, string title)
    {
        var c = new GameObject(name).transform;
        c.SetParent(parent, false);
        // 4 cm más hacia la sala: si no, la parte trasera de la consola atravesaba los
        // subpaneles y asas de los racks de la pared y esas mallas parpadeaban (z-fighting)
        c.position = pos + facing.normalized * 0.04f;
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

    static void AddOruPhysics(GameObject oru)
    {
        oru.AddComponent<Rigidbody>();
        var zg = oru.AddComponent<ZeroGBody>();
        zg.mass = 6f;            // un ORU real pesa bastante más; 6 kg para que se sienta pesado pero manejable
        zg.bounciness = 0.3f;
        zg.startFrozen = true;
    }

    /// <summary>
    /// Copia del módulo de repuesto en un módulo de paso: solo aparece (en tu mano) si lo
    /// llevas. Así se puede llevar de Destiny a Zvezda pasando por el Nodo.
    /// </summary>
    static void OruProxy(Ctx c, Vector3 pos)
    {
        var oru = BuildOru(c.content, pos);
        oru.name = "Módulo energético (llevado en la mano)";
        AddOruPhysics(oru);
        var p = oru.AddComponent<MissionPickup>();
        p.itemId = "energy_module";
        p.prompt = "Módulo energético de repuesto (ORU)";
        p.completeOnGrab = false;
        p.grabAction = MissionAction.EnergyModuleCollected;
        p.lockUntilObjective = false;
        p.carryAcrossScenes = true;
        p.onlyWhenCarried = true;
        p.deliveredAction = MissionAction.EnergyModuleLoadedIntoHatch;
        p.holdOffset = new Vector3(0f, -0.04f, 0.18f);
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

    /// <summary>
    /// Cubo unidad con UVs proporcionales al tamaño real de cada cara (1 repetición cada
    /// 'tile' metros). Se guarda como asset y se reutiliza para tamaños iguales.
    /// </summary>
    static Mesh TiledCube(Vector3 size, float tile)
    {
        Vector3 a = new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z));
        string key = string.Format(System.Globalization.CultureInfo.InvariantCulture,
                                   "TiledCube_{0:0.00}x{1:0.00}x{2:0.00}_t{3:0.0}", a.x, a.y, a.z, tile);
        return SaveMesh(key, () =>
        {
            var v = new List<Vector3>(); var n = new List<Vector3>(); var uv = new List<Vector2>(); var tri = new List<int>();
            TiledFace(v, n, uv, tri, tile, Vector3.forward, Vector3.left,    Vector3.up,      a.x, a.y);
            TiledFace(v, n, uv, tri, tile, Vector3.back,    Vector3.right,   Vector3.up,      a.x, a.y);
            TiledFace(v, n, uv, tri, tile, Vector3.right,   Vector3.forward, Vector3.up,      a.z, a.y);
            TiledFace(v, n, uv, tri, tile, Vector3.left,    Vector3.back,    Vector3.up,      a.z, a.y);
            TiledFace(v, n, uv, tri, tile, Vector3.up,      Vector3.right,   Vector3.forward, a.x, a.z);
            TiledFace(v, n, uv, tri, tile, Vector3.down,    Vector3.right,   Vector3.back,    a.x, a.z);
            var m = new Mesh { name = key };
            m.SetVertices(v); m.SetNormals(n); m.SetUVs(0, uv); m.SetTriangles(tri, 0);
            m.RecalculateTangents();
            m.RecalculateBounds();
            return m;
        });
    }

    // Una cara del cubo: (normal, eje u, eje v, tamaño u, tamaño v). Sin funciones locales (compila también en mcs).
    static void TiledFace(List<Vector3> v, List<Vector3> n, List<Vector2> uv, List<int> tri, float tile,
                          Vector3 nn, Vector3 u, Vector3 w, float su, float sv)
    {
        int i0 = v.Count;
        Vector3 c = nn * 0.5f;
        v.Add(c - u * 0.5f - w * 0.5f); v.Add(c + u * 0.5f - w * 0.5f);
        v.Add(c + u * 0.5f + w * 0.5f); v.Add(c - u * 0.5f + w * 0.5f);
        for (int k = 0; k < 4; k++) n.Add(nn);
        float U = Mathf.Max(0.01f, su / tile), V = Mathf.Max(0.01f, sv / tile);
        uv.Add(new Vector2(0, 0)); uv.Add(new Vector2(U, 0)); uv.Add(new Vector2(U, V)); uv.Add(new Vector2(0, V));
        tri.Add(i0); tri.Add(i0 + 2); tri.Add(i0 + 1); tri.Add(i0); tri.Add(i0 + 3); tri.Add(i0 + 2);
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

    /// <summary>Emisor de chispas por ráfagas (sin emisión continua; lo dispara AlarmLights).</summary>
    static ParticleSystem SparkBurst(Transform parent, Vector3 worldPos, Vector3 dir)
    {
        var go = new GameObject("Chispas");
        go.transform.SetParent(parent, false);
        go.transform.position = worldPos;
        go.transform.rotation = Quaternion.LookRotation((dir + Vector3.down * 0.6f).normalized, Vector3.up);
        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.playOnAwake = true;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.8f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 3.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.006f, 0.018f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.95f, 0.6f), new Color(1f, 0.55f, 0.15f));
        main.gravityModifier = 0.6f;
        main.maxParticles = 120;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        var em = ps.emission;
        em.rateOverTime = 0f;
        var sh = ps.shape;
        sh.shapeType = ParticleSystemShapeType.Cone;
        sh.angle = 35f;
        sh.radius = 0.02f;
        var col = ps.colorOverLifetime;
        col.enabled = true;
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(new Color(1f, 0.9f, 0.6f), 0f), new GradientColorKey(new Color(1f, 0.35f, 0.05f), 1f) },
                  new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        col.color = g;
        var r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = mParticleAdd;
        r.renderMode = ParticleSystemRenderMode.Stretch;
        r.velocityScale = 0.04f;
        r.lengthScale = 1.5f;
        r.shadowCastingMode = ShadowCastingMode.Off;
        r.receiveShadows = false;
        return ps;
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
            case SpaceToolType.PistolGripTool: file = "PistolGripTool.obj"; length = 0.30f; mat = mToolPGT; break;
            case SpaceToolType.Hammer:         file = "Hammer.obj";         length = 0.34f; mat = mToolHammer; break;
            case SpaceToolType.Ratchet:        file = "Ratchet.obj";        length = 0.30f; mat = mToolRatchet; break;
            default:                           file = "GreaseGun.obj";      length = 0.36f; mat = mToolGrease; break;
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

        // Gravedad cero: en su soporte queda fija; soltada lejos flota en línea recta y rebota
        toolGO.AddComponent<Rigidbody>();
        var zgb = toolGO.AddComponent<ZeroGBody>();
        zgb.startFrozen = true;
        zgb.mass = type == SpaceToolType.PistolGripTool ? 3.4f : type == SpaceToolType.Hammer ? 1.1f
                 : type == SpaceToolType.Ratchet ? 0.8f : 1.6f;   // masas aproximadas de herramientas reales
        zgb.bounciness = 0.35f;

        var tool = toolGO.AddComponent<SpaceTool>();
        tool.toolType = type;
        tool.prompt = SpaceTool.DisplayName(type);
        tool.dockPoint = dock;
        tool.desktopHoldEuler = new Vector3(5f, -8f, 0f);
        // Agarre en VR medido sobre los modelos: el MANGO queda en el puño y la punta hacia
        // delante (pistolas) o la cabeza hacia arriba (martillo y carraca, como se sujetan).
        // Pose del mando OpenXR: +Z hacia delante desde el puño, mango del objeto hacia -Y.
        switch (type)
        {
            case SpaceToolType.PistolGripTool: tool.vrHoldEuler = Vector3.zero;              tool.vrHoldPosition = new Vector3(0f, 0.0f, 0.075f);  break;
            case SpaceToolType.GreaseGun:      tool.vrHoldEuler = Vector3.zero;              tool.vrHoldPosition = new Vector3(0f, 0.04f, -0.07f); break;
            case SpaceToolType.Hammer:         tool.vrHoldEuler = new Vector3(90f, 0f, 0f);  tool.vrHoldPosition = new Vector3(0f, 0.095f, 0f);    break;
            default:                           tool.vrHoldEuler = new Vector3(90f, 0f, 0f);  tool.vrHoldPosition = new Vector3(0f, 0.06f, 0f);     break;
        }
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
    // INFRAESTRUCTURA COMÚN DE ESCENA
    // ════════════════════════════════════════════════════════════════════════

    class Ctx
    {
        public string name;
        public Scene scene;
        public Transform root, content, systems, reactions;
        public Transform player;
        public PlayerSpawnBay defaultBay;
    }

    struct Spawn
    {
        public string id; public Vector3 pos; public Vector3 facing;
        public Spawn(string id, Vector3 pos, Vector3 facing) { this.id = id; this.pos = pos; this.facing = facing; }
    }

    static Ctx NewInteriorScene(string name, int seed, bool sun, bool earth)
    {
        rng = new System.Random(seed);
        alarmStrips.Clear();

        var c = new Ctx { name = name };
        c.scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var sky = AssetDatabase.LoadAssetAtPath<Material>(SkyboxMat);
        if (sky != null) RenderSettings.skybox = sky;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.32f, 0.34f, 0.38f);
        RenderSettings.fog = false;
        RenderSettings.sun = null;

        if (sun) CreateSun();
        // Misma apariencia que antes pero 4 veces más cerca y más pequeña: así la cámara puede
        // usar un plano lejano de 1500 m y las paredes no parpadean (z-fighting por precisión).
        if (earth) CreateEarth(new Vector3(0f, -650f, 100f), 900f);

        c.root = new GameObject("ISS · " + name).transform;
        decorRoot = new GameObject("Decor").transform;
        decorRoot.SetParent(c.root, false);
        c.content = new GameObject("Mission Content").transform;
        c.content.SetParent(c.root, false);

        BuildSystems(c);
        return c;
    }

    static void CreateSun()
    {
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
    }

    static void CreateEarth(Vector3 pos, float diameter = 3600f)
    {
        var earth = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        earth.name = "Tierra (fondo)";
        Object.DestroyImmediate(earth.GetComponent<Collider>());
        earth.transform.position = pos;
        earth.transform.localScale = Vector3.one * diameter;
        earth.transform.rotation = Quaternion.Euler(-20f, 70f, 10f);
        var er = earth.GetComponent<Renderer>();
        er.sharedMaterial = mEarth;
        er.shadowCastingMode = ShadowCastingMode.Off;
        er.receiveShadows = false;
        GameObjectUtility.SetStaticEditorFlags(earth, StaticEditorFlags.BatchingStatic);
    }

    /// <summary>MissionManager persistente (raíz) + HUD + tester + contenedor de reacciones.</summary>
    static void BuildSystems(Ctx c)
    {
        var mmGO = new GameObject("MissionManager (persistente)");
        var mm = mmGO.AddComponent<MissionManager>();
        var so = new SerializedObject(mm);
        var list = so.FindProperty("missions");
        var e = AssetDatabase.LoadAssetAtPath<MissionDefinition>(MissionEnergy);
        var s = AssetDatabase.LoadAssetAtPath<MissionDefinition>(MissionSolar);
        list.arraySize = 0;
        if (e != null) { list.InsertArrayElementAtIndex(list.arraySize); list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = e; }
        if (s != null) { list.InsertArrayElementAtIndex(list.arraySize); list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = s; }
        var persist = so.FindProperty("persistAcrossScenes");
        if (persist != null) persist.boolValue = true;
        so.ApplyModifiedPropertiesWithoutUndo();
        if (e == null || s == null) report.Add("AVISO: falta algún asset de misión en Assets/Game/Data/Missions.");

        // Telemetría de la tablet de Camila (reloj, energía, mensajes de Control) y puntuación:
        // viven en el objeto persistente para no reiniciarse al cambiar de módulo.
        AddTelemetry(mmGO, e, s);
        mmGO.AddComponent<MissionScore>();

        var sys = new GameObject("Mission Systems");
        c.systems = sys.transform;
        var tester = sys.AddComponent<MissionDebugTester>();
        var tso = new SerializedObject(tester);
        var tp = tso.FindProperty("missionManager");
        if (tp != null) { tp.objectReferenceValue = mm; tso.ApplyModifiedPropertiesWithoutUndo(); }

        var hudGO = new GameObject("Mission HUD");
        hudGO.transform.SetParent(sys.transform, false);
        hudGO.AddComponent<MissionHUD>().markerMaterial = mMarker;

        var music = AssetDatabase.LoadAssetAtPath<AudioClip>(MusicClip);
        if (music != null)
        {
            var mGO = new GameObject("Música de fondo");
            mGO.AddComponent<BackgroundMusic>().clip = music;
        }
        else report.Add("AVISO: falta " + MusicClip + " (sin música).");

        var guideGO = new GameObject("Guía al objetivo");
        guideGO.transform.SetParent(sys.transform, false);
        guideGO.AddComponent<ObjectiveGuide>().guideMaterial = mGuide;

        c.reactions = new GameObject("Mission Reactions").transform;
        c.reactions.SetParent(sys.transform, false);
    }

    static void AddTelemetry(GameObject mmGO, MissionDefinition energy, MissionDefinition solar)
    {
        var tel = mmGO.AddComponent<StationTelemetry>();
        var so = new SerializedObject(tel);
        var list = so.FindProperty("missions");
        var defs = new List<MissionDefinition>();
        if (energy != null) defs.Add(energy);
        if (solar != null) defs.Add(solar);
        list.arraySize = defs.Count;
        for (int i = 0; i < defs.Count; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = defs[i];

        // Mensajes del Centro de Control por objetivo (los mismos que propuso Camila, ampliados)
        var msgs = new (string id, string text)[] {
            ("energy_01", "Eva, aquí Control. Falla en la distribución eléctrica. Activa el protocolo de emergencia en el Nodo."),
            ("energy_02", "Protocolo activo. El módulo energético de repuesto está en Destiny."),
            ("energy_03", "Módulo en tu bolsa. Llévalo a la compuerta de mantenimiento de Zvezda."),
            ("energy_04", "Repuesto recibido. Activa la consola de control robótico."),
            ("energy_05", "Consola lista. Sustituye el módulo averiado con el brazo robótico."),
            ("energy_06", "Instalación detectada. Confirma el diagnóstico de reparación."),
            ("solar_01", "Nueva alerta: daño en el panel solar. Ve a la esclusa Quest y equípate el traje."),
            ("solar_04", "Traje verificado. Despresuriza la esclusa."),
            ("solar_05", "Activa la mochila de propulsión. Fuera usarás los propulsores: A sube, B baja."),
            ("solar_06", "Escotilla abierta. Vuela hasta el panel dañado siguiendo las flechas."),
            ("solar_07", "Estás junto al panel. Procede con la reparación."),
            ("solar_08", "Panel reparado. Regresa a la esclusa."),
            ("solar_10", "Confirma desde la Cúpula que la energía quedó estabilizada."),
        };
        var mp = so.FindProperty("messagesByObjective");
        mp.arraySize = msgs.Length;
        for (int i = 0; i < msgs.Length; i++)
        {
            var el = mp.GetArrayElementAtIndex(i);
            el.FindPropertyRelative("objectiveId").stringValue = msgs[i].id;
            el.FindPropertyRelative("text").stringValue = msgs[i].text;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>Tablet de misión de Camila en la muñeca izquierda (sin su telemetría propia: usa la persistente).</summary>
    static void AddTablet(Transform player)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TabletPrefab);
        if (prefab == null) { report.Add("AVISO: no existe " + TabletPrefab + " (usa Nova Core > Crear Tablet de Mision)."); return; }
        var t = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        if (t == null) return;
        PrefabUtility.UnpackPrefabInstance(t, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        t.name = "Tablet de Misión (muñeca)";
        t.transform.SetParent(player, false);
        foreach (var tel in t.GetComponentsInChildren<StationTelemetry>(true)) Object.DestroyImmediate(tel.gameObject);
    }

    /// <summary>Player (cámara, colisiones, VR, interacción, llegada por escotilla y fundido).</summary>
    static void BuildPlayer(Ctx c, Spawn[] spawns, bool collisions, float speed)
    {
        // Puntos de llegada. El primero es el de por defecto (spawnId vacío).
        for (int i = 0; i < spawns.Length; i++)
        {
            var sp = spawns[i];
            var go = new GameObject("Spawn · " + (string.IsNullOrEmpty(sp.id) ? "por defecto" : sp.id));
            go.transform.SetParent(c.root, false);
            go.transform.SetPositionAndRotation(sp.pos, Quaternion.LookRotation(sp.facing, Vector3.up));
            var bay = go.AddComponent<PlayerSpawnBay>();
            bay.sectorName = c.name + " · " + (string.IsNullOrEmpty(sp.id) ? "inicio" : sp.id);
            bay.spawnId = sp.id;
            bay.spawnPoint = go.transform;
            if (i == 0) c.defaultBay = bay;
        }

        var player = new GameObject("Player");
        player.tag = "Player";
        player.transform.SetPositionAndRotation(spawns[0].pos, Quaternion.LookRotation(spawns[0].facing, Vector3.up));

        if (collisions)
        {
            var cc = player.AddComponent<CharacterController>();
            cc.radius = 0.22f;
            cc.height = 0.5f;
            cc.center = new Vector3(0f, 1.45f, 0f);
            cc.skinWidth = 0.02f;
            cc.stepOffset = 0f;
            cc.slopeLimit = 90f;
            cc.minMoveDistance = 0f;
        }

        var camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        camGO.transform.SetParent(player.transform, false);
        camGO.transform.localPosition = new Vector3(0f, 1.45f, 0f);
        var cam = camGO.AddComponent<Camera>();
        cam.nearClipPlane = 0.05f;   // antes 0.03/8000: poca precisión de profundidad → parpadeos
        cam.farClipPlane = 1500f;
        cam.fieldOfView = 75f;
        cam.clearFlags = CameraClearFlags.Skybox;
        camGO.AddComponent<AudioListener>();
        var acd = camGO.AddComponent<UniversalAdditionalCameraData>();
        acd.renderPostProcessing = false;
        acd.renderShadows = true;
        AddFader(camGO.transform);

        // Interiores: se camina como con gravedad (feedback: moverse "normal" dentro de la nave)
        var fly = player.AddComponent<OrbitFreeFly>();
        fly.mode = OrbitFreeFly.MoveMode.Walk;
        fly.moveSpeed = speed;
        fly.sprintMultiplier = 1.8f;
        fly.speedRange = new Vector2(0.6f, speed * 2.5f);
        fly.smoothing = 8f;
        fly.playerCamera = cam;

        player.AddComponent<XRPlayerRig>();
        var rig = player.AddComponent<NovaInteractionRig>();
        rig.laserMaterial = mLaser;
        rig.controllerMaterial = mGlove;   // manos con guante en vez de mandos
        player.AddComponent<SceneEntry>();
        AddTablet(player.transform);

        c.player = player.transform;

        var rs = c.systems.gameObject.AddComponent<MissionResetSpawn>();
        rs.spawnBay = c.defaultBay;
        rs.player = player.transform;
    }

    static void AddFader(Transform cam)
    {
        var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
        q.name = "Fundido (negro)";
        Object.DestroyImmediate(q.GetComponent<Collider>());
        q.transform.SetParent(cam, false);
        q.transform.localPosition = new Vector3(0f, 0f, 0.06f);
        q.transform.localScale = new Vector3(0.6f, 0.6f, 1f);
        var r = q.GetComponent<Renderer>();
        r.sharedMaterial = mFade;
        r.shadowCastingMode = ShadowCastingMode.Off;
        r.receiveShadows = false;
        var f = cam.gameObject.AddComponent<ScreenFader>();
        f.fadeRenderer = r;
    }

    static void FinishScene(Ctx c, int lights)
    {
        var alarm = c.systems.gameObject.AddComponent<AlarmLights>();
        alarm.alarmRenderers = alarmStrips.ToArray();
        alarm.alarmLights = new[] { PointLight(c.systems, "Alarm Light", new Vector3(0f, 2.1f, 0f) + c.root.position, 7f, 0f, Color.red) };

        // Chispas en paneles dañados (sustituyen al parpadeo de las luces)
        var sparks = new List<ParticleSystem>();
        var sparkRoot = new GameObject("Chispas (paneles dañados)").transform;
        sparkRoot.SetParent(c.root, false);
        for (int i = 0; i < alarmStrips.Count && sparks.Count < 3; i += Mathf.Max(1, alarmStrips.Count / 3))
        {
            var strip = alarmStrips[i];
            if (strip == null) continue;
            Vector3 pos = strip.bounds.center + Vector3.down * 0.06f;
            Vector3 inward = Vector3.ProjectOnPlane(c.root.position - pos, Vector3.up);
            if (inward.sqrMagnitude < 0.01f) inward = Vector3.forward;
            var panel = Box("Panel dañado", sparkRoot, pos + Vector3.down * 0.06f, new Vector3(0.22f, 0.14f, 0.22f), mRackDark, false, false);
            panel.transform.rotation = Quaternion.LookRotation(inward.normalized, Vector3.up);
            sparks.Add(SparkBurst(sparkRoot, pos, inward.normalized));
        }
        alarm.sparks = sparks.ToArray();

        EditorSceneManager.MarkSceneDirty(c.scene);
        EditorSceneManager.SaveScene(c.scene, ScenePathOf(c.name));
        report.Add("Escena: " + c.name);
    }

    /// <summary>
    /// Escotilla de salida hacia otro módulo: marco, rótulo, vestíbulo corto (túnel) y
    /// volumen de transición al fondo. Devuelve el punto de llegada al volver por aquí.
    /// </summary>
    static Spawn Exit(Ctx c, Vector3 moduleCenter, Vector3 moduleSize, Face face,
                      string targetScene, string targetSpawnId, string destName, string arrivalId,
                      HatchTransition.Condition cond = HatchTransition.Condition.Always)
    {
        Vector3 n = FaceNormal(face);
        Vector3 half = Vector3.Scale(moduleSize * 0.5f, new Vector3(Mathf.Abs(n.x), Mathf.Abs(n.y), Mathf.Abs(n.z)));
        Vector3 faceCenter = moduleCenter + n * half.magnitude;
        Vector3 hatchCenter = new Vector3(faceCenter.x, 1.2f, faceCenter.z);

        // Vestíbulo (túnel de 1 m) más allá del hueco
        var tunnelSize = (Mathf.Abs(n.x) > 0.5f) ? new Vector3(1.0f, 1.7f, 1.6f) : new Vector3(1.6f, 1.7f, 1.0f);
        Vector3 tunnelC = hatchCenter + n * (0.5f + WallT + 0.015f);
        Module(c.root, "Vestíbulo → " + destName, tunnelC, tunnelSize, new[] { FaceOf(-n) }, null, false);

        // Escotilla cerrada (decorativa) al fondo del túnel
        var door = new GameObject("Escotilla → " + destName).transform;
        Transform doorLeaf;
        door.SetParent(c.root, false);
        door.position = hatchCenter + n * (1.0f + WallT - 0.04f);
        door.rotation = Quaternion.LookRotation(-n, Vector3.up);
        // La hoja se abre con el mando (HatchHandle); el marco con el rótulo se queda
        doorLeaf = new GameObject("Hoja móvil").transform;
        doorLeaf.SetParent(door, false);
        var leaf = Box("Hoja", doorLeaf, Vector3.zero, new Vector3(1.3f, 1.5f, 0.05f), mPanel, true, false, true);
        var handle = Box("Manivela", doorLeaf, new Vector3(0f, -0.2f, 0.04f), new Vector3(0.4f, 0.05f, 0.05f), mMetal, false, false);
        Box("Luz", doorLeaf, new Vector3(0.5f, 0.6f, 0.03f), new Vector3(0.07f, 0.07f, 0.02f),
            cond == HatchTransition.Condition.Always ? mGreen : mAmber, false, false);
        NovaText.CreateFacing(doorLeaf, "Destino", "→ " + destName.ToUpper(), new Vector3(0f, 0.3f, 0.03f),
                              new Vector2(1.2f, 0.12f), 70, new Color(0.15f, 0.2f, 0.3f));
        NovaText.CreateFacing(doorLeaf, "Instrucción", "MANTÉN EL GATILLO PARA ABRIR", new Vector3(0f, -0.42f, 0.03f),
                              new Vector2(1.2f, 0.08f), 40, new Color(0.15f, 0.2f, 0.3f));
        var hh = door.gameObject.AddComponent<HatchHandle>();
        hh.door = doorLeaf;
        hh.destinationName = destName;
        hh.prompt = "Abrir escotilla → " + destName;
        hh.highlightRenderers = new[] { leaf.GetComponent<Renderer>(), handle.GetComponent<Renderer>() };
        lastHatch = hh;

        HatchFrame(c.root, hatchCenter - n * 0.0f, Quaternion.LookRotation(-n), HatchSize);

        // Rótulo sobre la escotilla, visible desde dentro del módulo
        Label(decorRoot, destName.ToUpper(), hatchCenter - n * 0.07f + Vector3.up * 0.93f, -n,
              new Vector2(1.4f, 0.15f), 64, new Color(0.15f, 0.2f, 0.3f));

        // Volumen de transición
        var t = new GameObject("Transición → " + targetScene);
        t.transform.SetParent(c.root, false);
        t.transform.position = hatchCenter + n * (0.55f + WallT);
        t.transform.rotation = Quaternion.LookRotation(n, Vector3.up);
        var ht = t.AddComponent<HatchTransition>();
        ht.targetScene = targetScene;
        ht.targetSpawnId = targetSpawnId;
        ht.destinationName = destName;
        ht.size = new Vector3(1.4f, 1.6f, 0.7f);
        ht.condition = cond;
        ht.gate = hh;
        hh.transition = ht;

        // Punto de llegada (al volver de ese módulo): 1.2 m dentro, mirando al interior
        Vector3 arrive = new Vector3(hatchCenter.x, 0f, hatchCenter.z) - n * 1.2f;
        return new Spawn(arrivalId, arrive, -n);
    }

    /// <summary>
    /// Objeto suelto que flota en gravedad cero (deriva lenta y gira). Se puede agarrar y
    /// lanzar; rebota contra paredes y racks.
    /// </summary>
    static GameObject Loose(Transform parent, string name, Vector3 pos, Vector3 size, Material mat, float mass,
                            PrimitiveType shape = PrimitiveType.Cube)
    {
        var go = Prim(shape, name, parent, pos, size, mat, true, false, true,
                      Quaternion.Euler(Rand(0f, 360f), Rand(0f, 360f), Rand(0f, 360f)));
        go.AddComponent<Rigidbody>();
        var zg = go.AddComponent<ZeroGBody>();
        zg.mass = mass;
        zg.startFrozen = false;
        zg.initialVelocity = new Vector3(Rand(-0.06f, 0.06f), Rand(-0.03f, 0.03f), Rand(-0.06f, 0.06f));
        zg.initialSpin = new Vector3(Rand(-0.4f, 0.4f), Rand(-0.4f, 0.4f), Rand(-0.4f, 0.4f));
        zg.returnHomeDistance = 12f;
        var g = go.AddComponent<ZeroGGrabbable>();
        g.prompt = name + " (agarrar y lanzar)";
        return go;
    }

    /// <summary>Palanca en una consola: se baja con la mano (o manteniendo clic).</summary>
    static NovaLever Lever(Transform console, string name, Vector3 local, string prompt, bool mission, MissionAction action,
                           MissionZone zone = null)
    {
        var root = new GameObject(name).transform;
        root.SetParent(console, false);
        root.localPosition = local;
        Box("Base", root, new Vector3(0f, 0f, 0.025f), new Vector3(0.12f, 0.08f, 0.05f), mMetal, false, false);
        var pivot = new GameObject("Pivote").transform;
        pivot.SetParent(root, false);
        pivot.localPosition = new Vector3(0f, 0f, 0.05f);
        Rod("Brazo", pivot, new Vector3(0f, 0.13f, 0f), 0.012f, 0.26f, Vector3.up, mMetal, false, false);
        var knob = Prim(PrimitiveType.Sphere, "Pomo", pivot, new Vector3(0f, 0.27f, 0f), Vector3.one * 0.07f, mRed, true, false);
        var led = Box("Luz", root, new Vector3(0.1f, 0f, 0.03f), new Vector3(0.03f, 0.03f, 0.02f), mAmber, false, false);
        var lever = root.gameObject.AddComponent<NovaLever>();
        lever.pivot = pivot;
        lever.upAngle = 10f;     // casi vertical
        lever.downAngle = 100f;  // tirada hacia ti y abajo
        lever.prompt = prompt;
        lever.completeMissionAction = mission;
        lever.action = action;
        lever.requiredZone = zone;
        lever.statusLight = led.GetComponent<Renderer>();
        lever.highlightRenderers = new[] { knob.GetComponent<Renderer>() };
        lever.maxDistance = 3f;
        return lever;
    }

    /// <summary>Última escotilla creada por Exit() (la usa el tutorial).</summary>
    static HatchHandle lastHatch;

    /// <summary>Insignia escondida (gamificación): una por módulo, +50 puntos.</summary>
    static void Patch(Ctx c, string id, string moduleName, Vector3 pos)
    {
        var go = new GameObject("Insignia · " + moduleName);
        go.transform.SetParent(c.content, false);
        go.transform.position = pos;
        var disc = Prim(PrimitiveType.Cylinder, "Disco", go.transform, Vector3.zero, new Vector3(0.12f, 0.006f, 0.12f), mGold,
                        false, false, false, Quaternion.Euler(90f, 0f, 0f));
        var core = Prim(PrimitiveType.Cylinder, "Centro", go.transform, new Vector3(0f, 0f, 0.004f), new Vector3(0.07f, 0.004f, 0.07f), mCyan,
                        false, false, false, Quaternion.Euler(90f, 0f, 0f));
        NovaText.Create(go.transform, "Texto", "NOVA", new Vector3(0f, -0.075f, 0f), Quaternion.Euler(0f, 180f, 0f),
                        new Vector2(0.12f, 0.03f), 22, new Color(1f, 0.85f, 0.4f));
        var sc = go.AddComponent<SphereCollider>();
        sc.radius = 0.12f;
        var p = go.AddComponent<MissionPatch>();
        p.patchId = id;
        p.patchName = "Insignia de " + moduleName;
        p.prompt = p.patchName;
        p.highlightRenderers = new[] { disc.GetComponent<Renderer>(), core.GetComponent<Renderer>() };
    }

    /// <summary>
    /// La Luna (foto Apollo 11 recortada como disco con transparencia) como cartel lejano
    /// que siempre mira a la cámara. Se acerca si el plano lejano de la cámara no llega.
    /// </summary>
    static void AddMoon(Transform parent, Vector3 offsetFromOrigin, float size)
    {
        if (mMoon == null || mMoon.mainTexture == null) { report.Add("AVISO: falta " + MoonTexture + " (sin Luna)."); return; }
        var cam = Object.FindAnyObjectByType<Camera>();
        float far = cam != null ? cam.farClipPlane : 8000f;
        float dist = offsetFromOrigin.magnitude;
        float k = dist > far * 0.8f ? (far * 0.8f) / dist : 1f;

        var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
        q.name = "Luna (Apollo 11)";
        Object.DestroyImmediate(q.GetComponent<Collider>());
        q.transform.SetParent(parent, true);
        q.transform.position = (cam != null ? cam.transform.position : Vector3.zero) + offsetFromOrigin * k;
        q.transform.localScale = Vector3.one * size * k;
        var r = q.GetComponent<Renderer>();
        r.sharedMaterial = mMoon;
        r.shadowCastingMode = ShadowCastingMode.Off;
        r.receiveShadows = false;
        q.AddComponent<FaceCamera>();
    }

    /// <summary>
    /// Astronauta (modelo de recursos/, convertido sin Draco y ya en metros con los pies en 0).
    /// 'feet' = posición de los pies. Se asegura de que quede DE PIE (si el importador lo deja
    /// tumbado lo endereza) y lo escala para no superar 'height' de alto ni 'maxWidth' de ancho.
    /// </summary>
    static GameObject AddAstronaut(Transform parent, string name, Vector3 feet, Quaternion rot, float height, float maxWidth = 2f)
    {
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(AstronautModel);
        if (asset == null) { report.Add("AVISO: no se pudo cargar " + AstronautModel + "."); return null; }
        var model = PrefabUtility.InstantiatePrefab(asset) as GameObject;
        if (model == null) return null;
        PrefabUtility.UnpackPrefabInstance(model, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        foreach (var c in model.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);

        // Contenedor: el modelo se corrige dentro y el contenedor lleva posición/rotación finales
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        model.transform.SetParent(go.transform, false);
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.identity;
        model.transform.localScale = Vector3.one;

        var rs = model.GetComponentsInChildren<Renderer>(true);
        if (rs.Length == 0) { report.Add("AVISO: el modelo de astronauta no tiene mallas."); return go; }
        System.Func<Bounds> bounds = () => { Bounds bb = rs[0].bounds; foreach (var r in rs) bb.Encapsulate(r.bounds); return bb; };

        // De pie: en pose T el alto (Y) es la dimensión mayor o casi; la profundidad es la menor
        Bounds b = bounds();
        if (b.size.y < Mathf.Max(b.size.x, b.size.z) * 0.6f)
        {
            model.transform.localRotation = Quaternion.Euler(b.size.z > b.size.x ? -90f : 0f, 0f, b.size.z > b.size.x ? 0f : 90f);
            b = bounds();
            report.Add("Astronauta: el importador lo dejó tumbado; se enderezó automáticamente.");
        }

        float k = Mathf.Min(height / Mathf.Max(1e-4f, b.size.y), maxWidth / Mathf.Max(1e-4f, Mathf.Max(b.size.x, b.size.z)));
        model.transform.localScale = Vector3.one * k;
        b = bounds();
        // Pies en el origen del contenedor, centrado en X/Z
        model.transform.position += new Vector3(-b.center.x, -b.min.y, -b.center.z);

        go.transform.SetPositionAndRotation(feet, rot);
        b = bounds();
        report.Add(string.Format("Astronauta '{0}': {1:0.00} m de alto × {2:0.00} m de ancho.", name, b.size.y, Mathf.Max(b.size.x, b.size.z)));
        return go;
    }

    static Dictionary<Face, Opening> Openings(params object[] pairs)
    {
        var d = new Dictionary<Face, Opening>();
        for (int i = 0; i + 1 < pairs.Length; i += 2) d[(Face)pairs[i]] = (Opening)pairs[i + 1];
        return d;
    }

    static Opening HatchOpening => new Opening(0f, 0f, HatchSize.x, HatchSize.y);

    // ════════════════════════════════════════════════════════════════════════
    // 00 · TUTORIAL (sala de entrenamiento cerrada, antes de la historia)
    // ════════════════════════════════════════════════════════════════════════

    static readonly Vector3 TutC = new Vector3(0f, H * 0.5f, 0f), TutS = new Vector3(6f, H, 6f);

    /// <summary>Pantalla negra con texto (Canvas world-space: fondo Image + Text).</summary>
    static (Image bg, Text body) TutorialScreen(Transform parent, string name, Vector3 pos, Vector3 facing, Vector2 size, string icon = null)
    {
        var holder = new GameObject(name).transform;
        holder.SetParent(parent, false);
        holder.position = pos;
        holder.rotation = Quaternion.LookRotation(facing, Vector3.up);
        Box("Marco", holder, new Vector3(0f, 0f, -0.02f), new Vector3(size.x + 0.06f, size.y + 0.06f, 0.03f), mMetal, false, false);

        var t = NovaText.Create(holder, "Pantalla", "", new Vector3(0f, 0f, 0.002f), Quaternion.Euler(0f, 180f, 0f),
                                size, 46, new Color(0.9f, 0.95f, 1f), TextAnchor.UpperLeft);
        var canvas = t.canvas.transform;
        var bgGO = new GameObject("Fondo", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        bgGO.transform.SetParent(canvas, false);
        bgGO.transform.SetAsFirstSibling();
        var brt = bgGO.GetComponent<RectTransform>();
        brt.anchorMin = Vector2.zero; brt.anchorMax = Vector2.one; brt.offsetMin = Vector2.zero; brt.offsetMax = Vector2.zero;
        var img = bgGO.GetComponent<Image>();
        img.color = new Color(0.02f, 0.03f, 0.05f, 0.95f);
        img.raycastTarget = false;
        var trt = t.rectTransform;
        float iconPx = 0f;
        var sprite = string.IsNullOrEmpty(icon) ? null : AssetDatabase.LoadAssetAtPath<Sprite>(TutorialIconDir + "/" + icon + ".png");
        if (sprite != null)
        {
            // Icono del mando con el botón resaltado (y las teclas equivalentes), a la izquierda
            iconPx = size.y * 1000f - 70f;
            var icGO = new GameObject("Icono", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            icGO.transform.SetParent(canvas, false);
            var irt = icGO.GetComponent<RectTransform>();
            irt.anchorMin = new Vector2(0f, 0.5f); irt.anchorMax = new Vector2(0f, 0.5f);
            irt.pivot = new Vector2(0f, 0.5f);
            irt.sizeDelta = new Vector2(iconPx, iconPx);
            irt.anchoredPosition = new Vector2(35f, 0f);
            var ii = icGO.GetComponent<Image>();
            ii.sprite = sprite;
            ii.preserveAspect = true;
            ii.raycastTarget = false;
        }
        trt.offsetMin = new Vector2(45f + (iconPx > 0f ? iconPx + 35f : 0f), 35f); trt.offsetMax = new Vector2(-45f, -35f);
        return (img, t);
    }

    static void BuildTutorial()
    {
        var c = NewInteriorScene(STutorial, 7, false, false);
        Module(c.root, "SALA DE ENTRENAMIENTO", TutC, TutS, null, Openings(Face.PZ, HatchOpening), false);
        Exit(c, TutC, TutS, Face.PZ, SNodo, "", "Nodo 1 Unity · Empezar misión", "from_nodo");
        var exitHatch = lastHatch;

        var tc = c.systems.gameObject.AddComponent<TutorialController>();
        tc.exitHatch = exitHatch;
        var steps = new List<TutorialController.Step>();
        Vector2 sz = new Vector2(2.1f, 1.15f);   // pantallas grandes (feedback: aprovechar el espacio)

        // 1 · Moverse: llegar a la plataforma verde
        var pad = new GameObject("Plataforma · ven aquí").transform;
        pad.SetParent(c.content, false);
        pad.position = new Vector3(-1.8f, 0f, 1.6f);
        Prim(PrimitiveType.Cylinder, "Disco", pad, new Vector3(0f, 0.01f, 0f), new Vector3(1.1f, 0.01f, 1.1f), mGreen, false, false);
        Label(pad, "VEN AQUÍ", pad.position + Vector3.up * 0.9f, Vector3.right, new Vector2(0.8f, 0.12f), 60, new Color(0.2f, 1f, 0.45f));
        var s1 = TutorialScreen(c.content, "Pantalla 1 · Moverse", new Vector3(-2.93f, 1.5f, 1.3f), Vector3.right, sz, "T1_Moverse");
        steps.Add(new TutorialController.Step
        {
            title = "Moverse",
            vrText = "Empuja el STICK IZQUIERDO para caminar hacia donde miras.\nSTICK DERECHO a los lados: girar.\n\nCamina hasta la plataforma verde.",
            desktopText = "W A S D: caminar.  Ratón: mirar.\nShift: correr.  Esc: soltar el ratón.\n\nCamina hasta la plataforma verde.",
            screen = s1.bg, body = s1.body, zone = pad, zoneSize = new Vector3(1.1f, 3.2f, 1.1f)
        });

        // 2 · Agarrar: caja de práctica sobre un pedestal
        Box("Pedestal", c.content, new Vector3(-2.2f, 0.45f, -1.0f), new Vector3(0.5f, 0.9f, 0.5f), mRackDark, true);
        var box = Box("Caja de práctica", c.content, new Vector3(-2.2f, 1.03f, -1.0f), new Vector3(0.24f, 0.24f, 0.24f), mOru, true, false, true);
        box.AddComponent<Rigidbody>();
        var boxZg = box.AddComponent<ZeroGBody>();
        boxZg.startFrozen = true;   // espera en el pedestal hasta que la agarres
        boxZg.mass = 2f;
        var tg = box.AddComponent<TutorialGrabbable>();
        tg.stepIndex = 1;
        tg.prompt = "Caja de práctica";
        var s2 = TutorialScreen(c.content, "Pantalla 2 · Agarrar", new Vector3(-2.93f, 1.5f, -1.25f), Vector3.right, sz, "T2_Agarrar");
        steps.Add(new TutorialController.Step
        {
            title = "Agarrar",
            vrText = "Apunta a la caja con el láser y MANTÉN EL GRIP (botón lateral del mando).\nSuéltalo para soltarla.\n\nLas manos se cierran al pulsar.",
            desktopText = "Apunta con la mira (punto blanco) a la caja y pulsa F para agarrarla.\nF otra vez para soltarla.",
            screen = s2.bg, body = s2.body
        });

        // 3 · Usar: botón de práctica (mantener gatillo)
        var cons = Console(c.content, "Consola de práctica", new Vector3(2.93f, 1.25f, -2.05f), Vector3.left, new Vector2(0.6f, 0.6f), "PRÁCTICA");
        var btn = new GameObject("Botón de práctica").transform;
        btn.SetParent(cons, false);
        var cap = Prim(PrimitiveType.Cylinder, "Cap", btn, new Vector3(0f, -0.04f, 0.05f), new Vector3(0.16f, 0.02f, 0.16f), mCyan,
                       true, false, false, Quaternion.Euler(90f, 0f, 0f));
        var led = Box("Luz", btn, new Vector3(0f, 0.17f, 0.03f), new Vector3(0.06f, 0.06f, 0.02f), mRed, false, false);
        var tb = btn.gameObject.AddComponent<TutorialButton>();
        tb.stepIndex = 2;
        tb.prompt = "Botón de práctica";
        tb.statusLight = led.GetComponent<Renderer>();
        tb.highlightRenderers = new[] { cap.GetComponent<Renderer>() };
        // Práctica libre: palanca y objetos para lanzar (gravedad cero)
        var leverC = Console(c.content, "Consola palanca", new Vector3(2.93f, 1.25f, 1.0f), Vector3.left, new Vector2(0.5f, 0.6f), "PALANCA");
        Lever(leverC, "Palanca de práctica", new Vector3(0f, -0.12f, 0f), "Palanca de práctica", false, MissionAction.RepairConfirmed);
        Loose(c.content, "Pelota (lánzala)", new Vector3(-0.8f, 1.4f, 0.4f), Vector3.one * 0.12f, mAmber, 0.2f, PrimitiveType.Sphere);
        Loose(c.content, "Pelota (lánzala) 2", new Vector3(0.6f, 1.2f, 1.0f), Vector3.one * 0.12f, mCyan, 0.2f, PrimitiveType.Sphere);
        Loose(c.content, "Bolsa de práctica", new Vector3(0.2f, 1.7f, -1.2f), new Vector3(0.3f, 0.2f, 0.22f), mBag, 2f);
        var s3 = TutorialScreen(c.content, "Pantalla 3 · Usar", new Vector3(2.93f, 1.5f, -0.35f), Vector3.left, new Vector2(1.95f, 1.15f), "T3_Usar");
        steps.Add(new TutorialController.Step
        {
            title = "Usar botones y herramientas",
            vrText = "Apunta al botón azul y MANTÉN EL GATILLO (dedo índice).\nEl gatillo usa botones, consolas y herramientas.\nEl grip solo agarra.",
            desktopText = "Apunta al botón azul y MANTÉN CLIC IZQUIERDO.\nEl clic usa botones, consolas y herramientas.\nF solo agarra.",
            screen = s3.bg, body = s3.body
        });

        // 4 · Guía y tablet
        var s4 = TutorialScreen(c.content, "Pantalla 4 · Guía", new Vector3(1.2f, 1.5f, -2.93f), Vector3.forward, sz, "T4_Guia");
        steps.Add(new TutorialController.Step
        {
            title = "Camino y tablet",
            vrText = "Si te pierdes, pulsa Y: aparecen flechas azules hasta el objetivo.\nGira la muñeca izquierda para ver la TABLET de misión (X cambia de pestaña).\n\nPulsa Y o X ahora.",
            desktopText = "Si te pierdes, pulsa G: aparecen flechas azules hasta el objetivo.\nTab abre la TABLET de misión (1, 2, 3 cambian de pestaña).\n\nPulsa G o Tab ahora.",
            screen = s4.bg, body = s4.body
        });

        // 5 · Escotilla (sale al Nodo y empieza la historia)
        var s5 = TutorialScreen(c.content, "Pantalla 5 · Escotilla", new Vector3(-1.95f, 1.5f, 2.93f), Vector3.back, new Vector2(1.9f, 1.15f), "T5_Escotilla");
        steps.Add(new TutorialController.Step
        {
            title = "Abrir la escotilla",
            vrText = "Apunta a la escotilla y MANTÉN EL GATILLO para abrirla.\nAl cruzarla empieza la misión en el Nodo 1.",
            desktopText = "Apunta a la escotilla y MANTÉN CLIC IZQUIERDO para abrirla.\nAl cruzarla empieza la misión en el Nodo 1.",
            screen = s5.bg, body = s5.body
        });
        tc.steps = steps.ToArray();

        // Información (no es un paso): controles del exterior y práctica libre con el taladro
        var info = TutorialScreen(c.content, "Pantalla · Exterior (EVA)", new Vector3(-1.2f, 1.5f, -2.93f), Vector3.forward, sz, "T6_EVA");
        info.body.text = "<b>FUERA DE LA ESTACIÓN (EVA)</b>\n\nVR: stick izq. empuja · A sube · B baja\nClic stick izq.: ESTABILIZAR (frenar)\n" +
                         "Teclado: WASD empuja · Espacio/Ctrl sube/baja · X estabiliza\n\nEn el espacio no hay rozamiento: si sueltas, sigues flotando.";
        info.body.fontSize = 38;
        BuildRepairStation(c.content, "Mesa de práctica · Taladro", new Vector3(2.88f, 1.25f, 1.95f), Vector3.left,
                           RepairStationType.PistolGrip_WelderAndDrill, SpaceToolType.PistolGripTool, "PRÁCTICA LIBRE · TALADRO", false, MissionAction.RepairConfirmed);
        BuildTool(c.content, SpaceToolType.PistolGripTool, new Vector3(2.8f, 0.85f, 2.6f), Vector3.left);

        Label(decorRoot, "SALA DE ENTRENAMIENTO · NOVA CORE", new Vector3(0f, 2.25f, -2.93f), Vector3.forward, new Vector2(2.4f, 0.13f), 56, new Color(0.15f, 0.2f, 0.3f));

        // Video de introducción al terminar el entrenamiento (con botón OMITIR INTRO)
        var introClip = AssetDatabase.LoadAssetAtPath<UnityEngine.Video.VideoClip>(IntroVideo);
        if (introClip != null)
        {
            var ivp = c.systems.gameObject.AddComponent<IntroVideoPlayer>();
            ivp.clip = introClip;
            ivp.screenMaterial = mVideo;
            ivp.playOnStart = false;          // se ve al CRUZAR la escotilla del entrenamiento (lore)
            ivp.loadSceneAfter = SNodo;       // al acabar u omitirlo, al Nodo: empieza la misión
            var vp = new GameObject("Punto de vista del video").transform;
            vp.SetParent(c.root, false);
            vp.position = new Vector3(-0.6f, 0f, -0.3f);
            vp.rotation = Quaternion.LookRotation(Vector3.right, Vector3.up);   // mira a la pared +X
            ivp.viewPoint = vp;
            ivp.distance = 2.4f;
        }
        else report.Add("AVISO: falta " + IntroVideo + " (sin video de introducción).");

        var lights = new GameObject("Lights").transform; lights.SetParent(c.root, false);
        PointLight(lights, "Light Tutorial A", new Vector3(0f, 2.1f, 0f), 7f, 1.3f, new Color(1f, 0.96f, 0.9f));

        BuildPlayer(c, new[] { new Spawn("", new Vector3(0f, 0f, -0.8f), Vector3.right) }, true, 1.4f);
        FinishScene(c, 1);
    }

    // ════════════════════════════════════════════════════════════════════════
    // 02 · NODO 1 UNITY
    // ════════════════════════════════════════════════════════════════════════

    static void BuildNodo()
    {
        var c = NewInteriorScene(SNodo, 101, false, false);
        Module(c.root, "NODO 1 · UNITY", NodeC, NodeS, null,
               Openings(Face.PX, HatchOpening, Face.NX, HatchOpening, Face.PZ, HatchOpening, Face.NZ, HatchOpening), false);

        var sDes = Exit(c, NodeC, NodeS, Face.NX, SDestiny, "from_nodo", "Destiny · Laboratorio", "from_destiny");
        var sZve = Exit(c, NodeC, NodeS, Face.PX, SZvezda,  "from_nodo", "Zvezda · Servicio",    "from_zvezda");
        var sQue = Exit(c, NodeC, NodeS, Face.PZ, SQuest,   "from_nodo", "Quest · Esclusa",      "from_quest");
        var sCup = Exit(c, NodeC, NodeS, Face.NZ, SCupula,  "from_nodo", "Cúpula",               "from_cupola");
        NodeDecor();

        // Inicio / reinicio / emergencia (paneles a los lados de la escotilla de Quest)
        var startC = Console(c.content, "Consola Inicio", new Vector3(-1.07f, 1.30f, 1.47f), Vector3.back, new Vector2(0.62f, 0.95f), "SECUENCIA DE MISIÓN");
        Button(startC, "Btn INICIAR", new Vector3(0f, 0.12f, 0f), MissionButton.Mode.StartMission, MissionAction.EmergencyActivated,
               "Iniciar secuencia de misiones", "INICIAR", mGreen, big: true);
        Button(startC, "Btn REINICIAR", new Vector3(0f, -0.25f, 0f), MissionButton.Mode.ResetMission, MissionAction.EmergencyActivated,
               "Reiniciar secuencia", "REINICIAR", mAmber);

        var emerC = Console(c.content, "Consola Emergencia", new Vector3(1.07f, 1.30f, 1.47f), Vector3.back, new Vector2(0.62f, 0.95f), "ALERTA DE ENERGÍA");
        Button(emerC, "Btn Emergencia", new Vector3(0f, 0.05f, 0f), MissionButton.Mode.CompleteAction, MissionAction.EmergencyActivated,
               "Activar protocolo de emergencia", "PROTOCOLO DE\nEMERGENCIA", mRed, big: true);

        // Bitácora (contexto de la historia)
        var logC = Console(c.content, "Bitácora", new Vector3(-1.07f, 1.30f, -1.47f), Vector3.forward, new Vector2(0.66f, 0.95f), "BITÁCORA · CENTRO DE CONTROL");
        NovaText.CreateFacing(logC, "Texto",
            "Una falla en el sistema eléctrico deja a la estación con energía crítica.\n\n" +
            "MISIÓN 1 · Sustituye el módulo energético con el brazo robótico (Destiny → Zvezda).\n\n" +
            "MISIÓN 2 · Sal al exterior por la esclusa Quest y repara el panel solar.\n\n" +
            "Pulsa INICIAR cuando estés listo.",
            new Vector3(0f, -0.05f, 0.025f), new Vector2(0.6f, 0.75f), 22, new Color(0.85f, 0.92f, 1f));

        var lights = new GameObject("Lights").transform; lights.SetParent(c.root, false);
        PointLight(lights, "Light Node", new Vector3(0f, 2.0f, 0f), 6f, 1.3f, new Color(1f, 0.95f, 0.86f));

        Patch(c, "nodo", "Nodo 1", new Vector3(1.25f, 2.05f, -1.25f));
        c.systems.gameObject.AddComponent<GameIntroManager>();   // misión automática al venir del entrenamiento
        OruProxy(c, new Vector3(0f, 1.2f, 0.5f));
        Loose(c.content, "Bolsa de carga", new Vector3(0.7f, 1.6f, -0.6f), new Vector3(0.3f, 0.2f, 0.22f), mBag, 2.5f);
        Loose(c.content, "Linterna", new Vector3(-0.6f, 1.3f, 0.5f), new Vector3(0.05f, 0.11f, 0.05f), mMetal, 0.4f, PrimitiveType.Cylinder);
        BuildPlayer(c, new[] { new Spawn("", new Vector3(0f, 0f, -0.4f), Vector3.forward), sDes, sZve, sQue, sCup }, true, 1.4f);
        FinishScene(c, 1);
    }

    // ════════════════════════════════════════════════════════════════════════
    // 03 · DESTINY (laboratorio)
    // ════════════════════════════════════════════════════════════════════════

    static void BuildDestiny()
    {
        var c = NewInteriorScene(SDestiny, 202, false, false);
        Module(c.root, "DESTINY · LABORATORIO", LabC, LabS, null, Openings(Face.PX, HatchOpening), false);
        var sBack = Exit(c, LabC, LabS, Face.PX, SNodo, "from_destiny", "Nodo 1 Unity", "from_nodo");

        Racks("Destiny", LabC, LabS, true, 0.25f, 0.22f);
        LabDecor();
        Label(decorRoot, "DESTINY — LABORATORIO DE EE.UU.", new Vector3(-9.94f, 1.95f, 0f), Vector3.right, new Vector2(1.8f, 0.14f), 60, new Color(0.15f, 0.2f, 0.3f));

        // Repuesto energético: al cogerlo se guarda en la bolsa (se instala en Zvezda)
        Box("Soporte ORU", c.content, new Vector3(-6.5f, 1.0f, 0.93f), new Vector3(0.5f, 0.04f, 0.14f), mMetal, true);
        var oru = BuildOru(c.content, new Vector3(-6.5f, 1.16f, 0.82f));
        AddOruPhysics(oru);
        var pickup = oru.AddComponent<MissionPickup>();
        pickup.itemId = "energy_module";
        pickup.prompt = "Módulo energético de repuesto (ORU)";
        pickup.completeOnGrab = true;
        pickup.grabAction = MissionAction.EnergyModuleCollected;
        pickup.lockUntilObjective = true;
        pickup.stowOnGrab = false;   // ahora se lleva EN LA MANO hasta Zvezda
        pickup.carryAcrossScenes = true;
        pickup.deliveredAction = MissionAction.EnergyModuleLoadedIntoHatch;
        pickup.holdOffset = new Vector3(0f, -0.04f, 0.18f);
        Label(c.content, "REPUESTO · MÓDULO ENERGÉTICO (ORU)", new Vector3(-6.5f, 1.5f, 0.97f), Vector3.back, new Vector2(0.9f, 0.08f), 34, new Color(0.15f, 0.2f, 0.3f));

        // Taller de práctica: las otras dos mesas portadas de NASA-SPACE-APPS
        BuildRepairStation(c.content, "Mesa Martillo (práctica)", new Vector3(-8.6f, 1.3f, 0.93f), Vector3.back,
                           RepairStationType.Hammer_ArmorStraightening, SpaceToolType.Hammer, "TALLER · BLINDAJE", false, MissionAction.RepairConfirmed);
        BuildTool(c.content, SpaceToolType.Hammer, new Vector3(-7.75f, 0.95f, 0.86f), Vector3.back);
        BuildRepairStation(c.content, "Mesa Carraca (práctica)", new Vector3(-8.6f, 1.3f, -0.93f), Vector3.forward,
                           RepairStationType.Ratchet_FlangeBolts, SpaceToolType.Ratchet, "TALLER · BRIDA", false, MissionAction.RepairConfirmed);
        BuildTool(c.content, SpaceToolType.Ratchet, new Vector3(-7.75f, 0.95f, -0.86f), Vector3.forward);
        Label(c.content, "TALLER DE PRÁCTICA (opcional): coge una herramienta y repara", new Vector3(-8.2f, 2.02f, 0f), Vector3.right,
              new Vector2(1.6f, 0.1f), 36, new Color(0.15f, 0.2f, 0.3f));

        var lights = new GameObject("Lights").transform; lights.SetParent(c.root, false);
        Color warm = new Color(1f, 0.95f, 0.86f);
        PointLight(lights, "Light Lab A", new Vector3(-4.0f, 2.0f, 0f), 6f, 1.2f, warm);
        PointLight(lights, "Light Lab B", new Vector3(-8.0f, 2.0f, 0f), 6f, 1.2f, warm);

        Patch(c, "destiny", "Destiny", new Vector3(-9.55f, 0.35f, 0.65f));
        Loose(c.content, "Cuaderno de experimentos", new Vector3(-3.2f, 1.5f, 0.2f), new Vector3(0.21f, 0.02f, 0.28f), mPanel, 0.3f);
        Loose(c.content, "Bebida (bolsa)", new Vector3(-5.0f, 1.7f, -0.3f), new Vector3(0.09f, 0.14f, 0.03f), mCyan, 0.3f);
        Loose(c.content, "Pelota de pruebas", new Vector3(-7.2f, 1.4f, 0.1f), Vector3.one * 0.1f, mAmber, 0.15f, PrimitiveType.Sphere);
        var def = sBack; def.id = "";
        BuildPlayer(c, new[] { def, sBack }, true, 1.4f);
        FinishScene(c, 2);
    }

    // ════════════════════════════════════════════════════════════════════════
    // 04 · ZVEZDA (módulo de servicio + bahía robótica)
    // ════════════════════════════════════════════════════════════════════════

    static void BuildZvezda()
    {
        var c = NewInteriorScene(SZvezda, 303, true, true);
        Module(c.root, "ZVEZDA · MÓDULO DE SERVICIO", SvcC, SvcS, null,
               Openings(Face.NX, HatchOpening, Face.PX, new Opening(0f, 0.1f, 1.6f, 0.9f, glass: true)), false);
        var sBack = Exit(c, SvcC, SvcS, Face.NX, SNodo, "from_zvezda", "Nodo 1 Unity", "from_nodo");

        Racks("Zvezda", SvcC, SvcS, true, 0.35f, 0.22f);
        Label(decorRoot, "ZVEZDA — BAHÍA ROBÓTICA >>", new Vector3(11.44f, 1.95f, 0f), Vector3.left, new Vector2(1.8f, 0.14f), 60, new Color(0.15f, 0.2f, 0.3f));

        // Compuerta de mantenimiento: introducir el repuesto que traes en la bolsa
        var hatchC = Console(c.content, "Compuerta de mantenimiento", new Vector3(9.2f, 1.2f, 0.95f), Vector3.back, new Vector2(0.7f, 0.85f), "COMPUERTA DE MANTENIMIENTO");
        Box("Ranura", hatchC, new Vector3(0f, 0.08f, 0.03f), new Vector3(0.42f, 0.3f, 0.02f), mRackDark, false, false);
        var inSlot = BuildOru(hatchC, hatchC.TransformPoint(new Vector3(0f, 0.08f, 0.13f)));
        foreach (var col in inSlot.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(col);
        inSlot.name = "Repuesto en la compuerta";
        inSlot.transform.localScale = Vector3.one * 0.85f;
        inSlot.SetActive(false);

        // Ranura: suelta el módulo que traes en la mano delante de la compuerta
        var slotGO = new GameObject("Ranura del módulo (soltar aquí)");
        slotGO.transform.SetParent(hatchC, false);
        slotGO.transform.localPosition = new Vector3(0f, 0.08f, 0.13f);
        var ghost = BuildOru(slotGO.transform, slotGO.transform.position);
        ghost.name = "Silueta";
        foreach (var col in ghost.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(col);
        foreach (var r in ghost.GetComponentsInChildren<Renderer>()) r.sharedMaterial = mGlass;
        foreach (var t in ghost.GetComponentsInChildren<Canvas>()) Object.DestroyImmediate(t.gameObject);
        var drop = slotGO.AddComponent<MissionDropZone>();
        drop.acceptsItemId = "energy_module";
        drop.acceptRadius = 0.5f;
        drop.socket = slotGO.transform;
        drop.action = MissionAction.EnergyModuleLoadedIntoHatch;
        drop.ghost = ghost;
        drop.hideOnAccept = true;   // lo muestra 'Repuesto en la compuerta' (también al volver al módulo)
        NovaText.CreateFacing(hatchC, "Instrucción", "ACERCA EL MÓDULO Y SUÉLTALO AQUÍ", new Vector3(0f, -0.28f, 0.03f),
                              new Vector2(0.65f, 0.08f), 28, new Color(1f, 0.8f, 0.3f));
        var r12 = c.reactions.gameObject.AddComponent<MissionReaction>();
        r12.onAction = MissionAction.EnergyModuleLoadedIntoHatch;
        r12.enableObjects = new[] { inSlot };
        r12.message = "Repuesto en la bahía. Activa la consola robótica.";

        // Consola robótica + brazo en la bahía exterior (visible por la ventana)
        var arm = BuildRoboticBay(c.content);
        // Consola de mando frente a la ventana de la bahía (mesa inclinada): miras al brazo mientras lo manejas
        var robC = Console(c.content, "Consola Robótica", new Vector3(10.75f, 0.82f, 0f), Vector3.left, new Vector2(1.05f, 0.9f), "CONTROL ROBÓTICO");
        robC.rotation = Quaternion.LookRotation(Vector3.left, Vector3.up) * Quaternion.Euler(-55f, 0f, 0f);
        Box("Pedestal consola", c.content, new Vector3(10.95f, 0.35f, 0f), new Vector3(0.35f, 0.7f, 0.9f), mRackDark, true, true, true);
        Button(robC, "Btn Activar Consola", new Vector3(-0.36f, -0.05f, 0f), MissionButton.Mode.CompleteAction, MissionAction.RoboticConsoleActivated,
               "Activar consola robótica", "ACTIVAR\nCONSOLA", mCyan);
        Button(robC, "Btn Confirmar", new Vector3(0.36f, -0.05f, 0f), MissionButton.Mode.CompleteAction, MissionAction.RepairConfirmed,
               "Confirmar reparación energética", "CONFIRMAR\nREPARACIÓN", mGreen);
        var screen = NovaText.CreateFacing(robC, "Pantalla", "", new Vector3(0f, 0.26f, 0.03f), new Vector2(0.95f, 0.2f), 26, new Color(0.7f, 0.9f, 1f));
        Box("Pantalla Fondo", robC, new Vector3(0f, 0.26f, 0.021f), new Vector3(0.98f, 0.22f, 0.004f), mScreen, false);
        BuildJoystick(robC, arm, screen);

        var lights = new GameObject("Lights").transform; lights.SetParent(c.root, false);
        Color warm = new Color(1f, 0.95f, 0.86f);
        PointLight(lights, "Light Svc A", new Vector3(4.5f, 2.0f, 0f), 6f, 1.2f, warm);
        PointLight(lights, "Light Svc B", new Vector3(9.0f, 2.0f, 0f), 6f, 1.2f, warm);

        Patch(c, "zvezda", "Zvezda", new Vector3(3.3f, 2.0f, -0.8f));
        OruProxy(c, new Vector3(4.5f, 1.2f, 0f));   // aparece en tu mano al llegar con el módulo
        Loose(c.content, "Bolsa de comida", new Vector3(5.5f, 1.6f, 0.3f), new Vector3(0.25f, 0.15f, 0.2f), mBag, 1.5f);
        Loose(c.content, "Llave suelta", new Vector3(7.5f, 1.2f, -0.4f), new Vector3(0.03f, 0.18f, 0.02f), mMetal, 0.25f);
        var def = sBack; def.id = "";
        BuildPlayer(c, new[] { def, sBack }, true, 1.4f);
        FinishScene(c, 2);
    }

    static void BuildJoystick(Transform robC, ArmRefs arm, Text screen)
    {
        var joyBase = new GameObject("Joystick Brazo").transform;
        joyBase.SetParent(robC, false);
        joyBase.localPosition = new Vector3(0f, -0.08f, 0.02f);
        var jb = Prim(PrimitiveType.Cylinder, "Base", joyBase, new Vector3(0f, 0f, 0.02f), new Vector3(0.14f, 0.02f, 0.14f), mMetal,
                      true, false, false, Quaternion.Euler(90f, 0f, 0f));
        var stickPivot = new GameObject("Stick").transform;
        stickPivot.SetParent(joyBase, false);
        stickPivot.localPosition = new Vector3(0f, 0f, 0.04f);
        Rod("Rod", stickPivot, new Vector3(0f, 0f, 0.06f), 0.012f, 0.12f, Vector3.forward, mMetal, false, false);
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
    }

    // ════════════════════════════════════════════════════════════════════════
    // 05 · QUEST (esclusa)
    // ════════════════════════════════════════════════════════════════════════

    static void BuildQuest()
    {
        var c = NewInteriorScene(SQuest, 404, false, false);
        Module(c.root, "QUEST · ESCLUSA", LockC, LockS, null, Openings(Face.NZ, HatchOpening, Face.PZ, HatchOpening), false);
        var sNodo = Exit(c, LockC, LockS, Face.NZ, SNodo, "from_quest", "Nodo 1 Unity", "from_nodo", HatchTransition.Condition.AirlockInnerOpen);
        var sEva  = Exit(c, LockC, LockS, Face.PZ, SEva,  "from_quest", "Exterior (EVA)", "from_eva",  HatchTransition.Condition.AirlockOuterOpen);

        Racks("Quest", LockC, LockS, false, 0.5f, 0.12f, walls: false, ceiling: true, floor: true, handrailY: -0.6f);
        Label(decorRoot, "QUEST — ESCLUSA CONJUNTA", new Vector3(0f, 2.1f, 6.42f), Vector3.back, new Vector2(1.4f, 0.12f), 54, new Color(0.15f, 0.2f, 0.3f));

        var zoneGO = new GameObject("Zona Esclusa");
        zoneGO.transform.SetParent(c.content, false);
        zoneGO.transform.position = LockC;
        var lockZone = zoneGO.AddComponent<MissionZone>();
        lockZone.zoneName = "la esclusa Quest";
        lockZone.size = new Vector3(2.4f, 2.4f, 4.9f);
        lockZone.completeAction = true;
        lockZone.action = MissionAction.ReturnedToAirlock;

        var inner = BuildHatch(c.content, "Escotilla interior (→ Nodo)", new Vector3(0f, 1.2f, 1.56f), Vector3.back, HatchDoor.Mode.Proximity);
        var outer = BuildHatch(c.content, "Escotilla exterior (EVA)", new Vector3(0f, 1.2f, 6.44f), Vector3.back, HatchDoor.Mode.ForcedClosed);
        var ac = c.systems.gameObject.AddComponent<AirlockController>();
        ac.innerHatch = inner;
        ac.outerHatch = outer;

        // Pared -X: chequeo de traje, armario, MMU
        var chkC = Console(c.content, "Consola Traje", new Vector3(-1.17f, 1.3f, 2.5f), Vector3.right, new Vector2(0.6f, 0.75f), "TRAJE EMU");
        Button(chkC, "Btn Verificar Traje", Vector3.zero, MissionButton.Mode.CompleteAction, MissionAction.SuitSystemsChecked,
               "Verificar sistemas del traje", "VERIFICAR\nSISTEMAS", mCyan, zone: lockZone);

        var suitC = Console(c.content, "Armario Traje", new Vector3(-1.17f, 1.2f, 3.7f), Vector3.right, new Vector2(0.95f, 1.5f), "ARMARIO EVA");
        var suit = BuildSuit(c.content, new Vector3(-0.88f, 0.3f, 3.45f));
        // Traje en el armario: el modelo de astronauta de recursos/ (si carga) en lugar del procedural
        var astroSuit = AddAstronaut(c.content, "Traje EMU (modelo)", new Vector3(-0.85f, 0.18f, 3.6f),
                                     Quaternion.LookRotation(Vector3.right, Vector3.up), 1.6f, 1.45f);
        if (astroSuit != null) { Object.DestroyImmediate(suit); suit = astroSuit; }
        Button(suitC, "Btn Equipar Traje", new Vector3(-0.3f, -0.05f, 0f), MissionButton.Mode.CompleteAction, MissionAction.SuitEquipped,
               "Equiparse con el traje", "EQUIPAR\nTRAJE", mGreen, zone: lockZone);
        var r4 = c.reactions.gameObject.AddComponent<MissionReaction>();
        r4.onAction = MissionAction.SuitEquipped;
        r4.disableObjects = new[] { suit };
        r4.message = "Traje EMU equipado.";

        var mmuC = Console(c.content, "Unidad MMU", new Vector3(-1.17f, 1.3f, 5.6f), Vector3.right, new Vector2(0.62f, 0.8f), "UNIDAD DE MANIOBRA (MMU)");
        BuildMmu(mmuC);
        Button(mmuC, "Btn MMU", new Vector3(0f, -0.2f, 0f), MissionButton.Mode.CompleteAction, MissionAction.MMUActivated,
               "Activar unidad de propulsión", "ACTIVAR MMU", mCyan, zone: lockZone);
        var r14 = c.reactions.gameObject.AddComponent<MissionReaction>();
        r14.onAction = MissionAction.MMUActivated;
        r14.message = "MMU activa. Sal por la escotilla exterior hacia el panel solar.";

        // Pared +X: sellado (represurizar), preparación EVA, despresurizar
        var prepC = Console(c.content, "Consola Preparación", new Vector3(1.17f, 1.3f, 4.2f), Vector3.left, new Vector2(0.6f, 0.75f), "PREPARACIÓN EVA");
        Button(prepC, "Btn Preparación", Vector3.zero, MissionButton.Mode.CompleteAction, MissionAction.EVAPreparationCompleted,
               "Completar la preparación EVA", "PREPARACIÓN\nCOMPLETA", mGreen, zone: lockZone);

        var depC = Console(c.content, "Panel Despresurización", new Vector3(1.17f, 1.3f, 5.6f), Vector3.left, new Vector2(0.6f, 0.75f), "ESCLUSA · PRESIÓN");
        // Palanca (feedback: interactuar directamente con palancas)
        Lever(depC, "Palanca Despresurizar", new Vector3(0f, -0.12f, 0f), "Palanca DESPRESURIZAR", true,
              MissionAction.AirlockDepressurized, lockZone);
        NovaText.CreateFacing(depC, "Etiqueta palanca", "DESPRESURIZAR\n(baja la palanca)", new Vector3(0f, 0.2f, 0.03f),
                              new Vector2(0.5f, 0.12f), 30, Color.white);
        var r7 = c.reactions.gameObject.AddComponent<MissionReaction>();
        r7.onAction = MissionAction.AirlockDepressurized;
        r7.message = "Esclusa despresurizada: escotilla exterior abierta.";
        var r10 = c.reactions.gameObject.AddComponent<MissionReaction>();
        r10.onAction = MissionAction.ReturnedToAirlock;
        r10.message = "De vuelta en la esclusa. Sella la fuga para represurizar.";

        BuildRepairStation(c.content, "Mesa Sellado · Represurizar", new Vector3(1.12f, 1.25f, 2.6f), Vector3.left,
                           RepairStationType.GreaseGun_PressureSealant, SpaceToolType.GreaseGun,
                           "SELLADO DE FUGA · ESCLUSA", true, MissionAction.AirlockRepressurized);
        BuildTool(c.content, SpaceToolType.GreaseGun, new Vector3(1.0f, 0.75f, 3.25f), Vector3.left);
        var r11 = c.reactions.gameObject.AddComponent<MissionReaction>();
        r11.onAction = MissionAction.AirlockRepressurized;
        r11.message = "Esclusa represurizada. Ve a la Cúpula (por el Nodo) a verificar la energía.";

        var lights = new GameObject("Lights").transform; lights.SetParent(c.root, false);
        PointLight(lights, "Light Airlock", new Vector3(0f, 2.0f, 4.2f), 6f, 1.1f, new Color(0.9f, 0.95f, 1f));

        Patch(c, "quest", "Quest", new Vector3(0.85f, 0.35f, 6.0f));
        OruProxy(c, new Vector3(0f, 1.2f, 2.5f));
        Loose(c.content, "Guante de repuesto", new Vector3(0.4f, 1.7f, 4.6f), new Vector3(0.12f, 0.18f, 0.06f), mGlove, 0.3f);
        var def = sNodo; def.id = "";
        BuildPlayer(c, new[] { def, sNodo, sEva }, true, 1.4f);
        FinishScene(c, 1);
    }

    // ════════════════════════════════════════════════════════════════════════
    // 06 · CÚPULA
    // ════════════════════════════════════════════════════════════════════════

    static void BuildCupula()
    {
        var c = NewInteriorScene(SCupula, 505, true, true);
        Module(c.root, "CÚPULA", CupolaC, CupolaS, null,
               Openings(Face.PZ, HatchOpening,
                        Face.NZ, new Opening(0f, 0.1f, 1.8f, 1.2f, glass: true),
                        Face.NY, new Opening(0f, 0f, 1.8f, 1.2f, glass: true)), false);
        var sBack = Exit(c, CupolaC, CupolaS, Face.PZ, SNodo, "from_cupola", "Nodo 1 Unity", "from_nodo");
        CupolaDecor();

        var verC = Console(c.content, "Consola Verificación", new Vector3(-1.27f, 1.25f, -3.4f), Vector3.right, new Vector2(0.6f, 0.8f), "ENERGÍA DE LA ESTACIÓN");
        Button(verC, "Btn Verificar Energía", Vector3.zero, MissionButton.Mode.CompleteAction, MissionAction.StationPowerVerified,
               "Verificar estabilización energética", "VERIFICAR\nENERGÍA", mGreen, big: true);

        var tpC = Console(c.content, "Consola Vista Orbital", new Vector3(1.27f, 1.25f, -3.4f), Vector3.left, new Vector2(0.6f, 0.8f), "VISTA ORBITAL");
        var tpBtn = new GameObject("Btn Vista Orbital").transform;
        tpBtn.SetParent(tpC, false);
        var tpCap = Prim(PrimitiveType.Cylinder, "Cap", tpBtn, new Vector3(0f, 0f, 0.05f), new Vector3(0.14f, 0.02f, 0.14f), mCyan,
                         true, false, false, Quaternion.Euler(90f, 0f, 0f));
        NovaText.CreateFacing(tpBtn, "Label", "VER LA ISS\nDESDE FUERA\n(01_Orbit)", new Vector3(0f, -0.18f, 0.03f), new Vector2(0.4f, 0.14f), 24, Color.white);
        var tp = tpBtn.gameObject.AddComponent<SceneTeleporter>();
        tp.sceneName = "01_Orbit";
        tp.spawnId = "";
        tp.prompt = "Ir a la vista orbital exterior";
        tp.highlightRenderers = new[] { tpCap.GetComponent<Renderer>() };

        var lights = new GameObject("Lights").transform; lights.SetParent(c.root, false);
        PointLight(lights, "Light Cupola", new Vector3(0f, 2.0f, -3.2f), 4f, 0.6f, new Color(1f, 0.95f, 0.86f));

        // Por defecto se aparece en el centro de la Cúpula mirando por la ventana
        Patch(c, "cupula", "Cúpula", new Vector3(-0.95f, 2.05f, -2.75f));
        OruProxy(c, new Vector3(0f, 1.2f, -2.6f));
        Loose(c.content, "Cámara de fotos", new Vector3(0.5f, 1.5f, -2.9f), new Vector3(0.14f, 0.09f, 0.08f), mBlackPanel, 1.2f);
        AddMoon(c.root, new Vector3(-700f, 950f, -3000f), 260f);
        var def = new Spawn("", new Vector3(0f, 0f, -3.1f), Vector3.back);
        BuildPlayer(c, new[] { def, sBack }, true, 1.3f);
        FinishScene(c, 1);
    }

    // ════════════════════════════════════════════════════════════════════════
    // 07 · EVA (exterior real: copia de 01_Orbit con la ISS a escala 1:1)
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Colisiones del modelo exterior de la ISS (para no atravesarlo): MeshCollider en cada
    /// malla del modelo y CharacterController en el jugador. Devuelve cuántas mallas.
    /// </summary>
    static int AddIssColliders(GameObject player)
    {
        var anyPart = GameObject.Find("12 Quest Airlock") ?? GameObject.Find("02 Unity Node 1");
        if (anyPart == null) { report.Add("AVISO: no se encontró el modelo de la ISS para ponerle colisiones."); return 0; }
        Transform root = anyPart.transform.root;
        int added = 0;
        foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true))
        {
            if (mf.sharedMesh == null || mf.GetComponent<Collider>() != null) continue;
            var mc = mf.gameObject.AddComponent<MeshCollider>();
            mc.sharedMesh = mf.sharedMesh;
            added++;
        }

        if (player != null)
        {
            var cc = player.GetComponent<CharacterController>();
            if (cc == null) cc = player.AddComponent<CharacterController>();
            cc.radius = 0.3f;          // casco + mochila
            cc.height = 0.7f;
            cc.center = new Vector3(0f, 1.45f, 0f);   // OrbitFreeFly lo mueve a la cabeza cada frame
            cc.skinWidth = 0.03f;
            cc.stepOffset = 0f;
            cc.slopeLimit = 90f;
            cc.minMoveDistance = 0f;
        }
        return added;
    }

    static Bounds? BoundsOf(string objectName)
    {
        var go = GameObject.Find(objectName);
        if (go == null) return null;
        var rs = go.GetComponentsInChildren<Renderer>(true);
        if (rs.Length == 0) return null;
        Bounds b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        return b;
    }

    static void BuildEva()
    {
        if (!File.Exists(OrbitScene)) { report.Add("AVISO: no existe " + OrbitScene + " (sin EVA)."); return; }

        string evaPath = ScenePathOf(SEva);
        if (File.Exists(evaPath)) AssetDatabase.DeleteAsset(evaPath);
        if (!AssetDatabase.CopyAsset(OrbitScene, evaPath)) { report.Add("AVISO: no se pudo copiar 01_Orbit para la EVA."); return; }

        rng = new System.Random(606);
        alarmStrips.Clear();
        var scene = EditorSceneManager.OpenScene(evaPath, OpenSceneMode.Single);

        foreach (var n in new[] { "Acceso Interior ISS", "Marcadores Módulos" })
        {
            var old = GameObject.Find(n);
            if (old != null) Object.DestroyImmediate(old);
        }

        var c = new Ctx { name = SEva, scene = scene };
        c.root = new GameObject("EVA · Contenido").transform;
        decorRoot = new GameObject("Decor").transform;
        decorRoot.SetParent(c.root, false);
        c.content = c.root;
        BuildSystems(c);
        CreateEarth(new Vector3(0f, -2600f, 400f));

        // Dónde está la esclusa Quest y el panel solar más cercano en el modelo
        Bounds? quest = BoundsOf("12 Quest Airlock");
        Vector3 q = quest.HasValue ? quest.Value.center : new Vector3(0f, 0f, -10f);
        float qRadius = quest.HasValue ? quest.Value.extents.magnitude : 3f;

        // Ala de paneles solares más cercana a Quest. En este modelo las alas grandes son los
        // objetos "* Truss_01/_02" de P4/S4/P6/S6 (los "panel_*" son los paneles pequeños
        // del segmento ruso).
        string[] wings = { "23 S4 Truss_01", "23 S4 Truss_02", "20 P4 Truss_01", "20 P4 Truss_02",
                           "32 S6 Truss_01", "32 S6 Truss_02", "08 P6 Truss_01", "08 P6 Truss_02" };
        Bounds? best = null; float bestD = float.MaxValue; string bestName = "";
        foreach (var w in wings)
        {
            Bounds? b = BoundsOf(w);
            if (!b.HasValue) continue;
            float d = Vector3.Distance(b.Value.center, q);
            if (d < bestD) { bestD = d; best = b; bestName = w; }
        }
        Vector3 p = best.HasValue ? best.Value.center : q + new Vector3(25f, 0f, 10f);
        Vector3 dir = (p - q); dir = dir.sqrMagnitude > 0.01f ? dir.normalized : Vector3.forward;
        if (!quest.HasValue) report.Add("AVISO: no se encontró '12 Quest Airlock' en el modelo (posición aproximada).");
        if (!best.HasValue) report.Add("AVISO: no se encontró ninguna ala solar (P4/S4/P6/S6) en el modelo (posición aproximada).");

        // Punto de trabajo: en el borde de la caja del ala que mira hacia Quest
        if (best.HasValue)
        {
            Vector3 e = best.Value.extents;
            float t = float.MaxValue;
            if (Mathf.Abs(dir.x) > 1e-3f) t = Mathf.Min(t, e.x / Mathf.Abs(dir.x));
            if (Mathf.Abs(dir.y) > 1e-3f) t = Mathf.Min(t, e.y / Mathf.Abs(dir.y));
            if (Mathf.Abs(dir.z) > 1e-3f) t = Mathf.Min(t, e.z / Mathf.Abs(dir.z));
            if (t < float.MaxValue) p = best.Value.center - dir * t;
        }

        Vector3 spawnPos = q + dir * (qRadius + 2.5f);
        Vector3 station = p - dir * 1.5f;
        Quaternion stationRot = Quaternion.LookRotation(-dir, Vector3.up);
        Vector3 right = stationRot * Vector3.right;

        // Jugador existente de 01_Orbit → jugador EVA
        var pl = GameObject.FindGameObjectWithTag("Player");
        if (pl != null)
        {
            foreach (var mb in pl.GetComponents<MonoBehaviour>())
                if (mb is PlayerModeSwitcher || mb is VRMovement || mb is SpaceStationPlayerController) Object.DestroyImmediate(mb);

            pl.transform.SetPositionAndRotation(spawnPos, Quaternion.LookRotation(Vector3.ProjectOnPlane(dir, Vector3.up).sqrMagnitude > 0.01f ? Vector3.ProjectOnPlane(dir, Vector3.up) : Vector3.forward, Vector3.up));
            // Fuera: mochila de propulsores con inercia (feedback: "simular el propulsor")
            var fly = pl.GetComponent<OrbitFreeFly>();
            if (fly != null)
            {
                fly.mode = OrbitFreeFly.MoveMode.Thrusters;
                fly.thrustAcceleration = 0.9f;
                fly.maxThrustSpeed = 2.5f;
                fly.passiveDamping = 0.15f;
                fly.stabilizeDamping = 2.5f;
            }
            var rig = pl.GetComponent<NovaInteractionRig>();
            if (rig == null) rig = pl.AddComponent<NovaInteractionRig>();
            rig.laserMaterial = mLaser; rig.controllerMaterial = mGlove;
            rig.desktopRayLength = 4f; rig.vrRayLength = 5f;
            AddTablet(pl.transform);
            if (pl.GetComponent<SceneEntry>() == null) pl.AddComponent<SceneEntry>();
            var cam = pl.GetComponentInChildren<Camera>(true);
            if (cam != null && cam.GetComponent<ScreenFader>() == null) AddFader(cam.transform);
            c.player = pl.transform;
        }
        else report.Add("AVISO: 01_Orbit no tiene Player con tag 'Player'.");

        // Colisiones con el exterior de la ISS
        report.Add("EVA: colisiones en " + AddIssColliders(pl) + " mallas de la ISS.");

        // Puntos de llegada
        var bayGO = new GameObject("Spawn · desde Quest");
        bayGO.transform.SetParent(c.root, false);
        bayGO.transform.SetPositionAndRotation(spawnPos, pl != null ? pl.transform.rotation : Quaternion.identity);
        var bay = bayGO.AddComponent<PlayerSpawnBay>();
        bay.sectorName = "EVA · salida de la esclusa Quest";
        bay.spawnId = "";
        bay.spawnPoint = bayGO.transform;
        if (c.player != null)
        {
            var rs = c.systems.gameObject.AddComponent<MissionResetSpawn>();
            rs.spawnBay = bay; rs.player = c.player;
        }

        // Regreso a la esclusa (transición a 05_Quest)
        var back = new GameObject("Transición → 05_Quest");
        back.transform.SetParent(c.root, false);
        back.transform.position = spawnPos - dir * 1.2f;
        back.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
        var ht = back.AddComponent<HatchTransition>();
        ht.targetScene = SQuest; ht.targetSpawnId = "from_eva"; ht.destinationName = "la esclusa Quest";
        ht.size = new Vector3(2.4f, 2.4f, 1.2f);
        var ring = Prim(PrimitiveType.Cylinder, "Anillo escotilla Quest", back.transform, Vector3.zero, new Vector3(1.8f, 0.02f, 1.8f),
                        mAmber, false, false, false, Quaternion.Euler(90f, 0f, 0f));
        Label(back.transform, "← REGRESO A LA ESCLUSA QUEST", back.transform.position + Vector3.up * 1.4f, -dir, new Vector2(2.4f, 0.25f), 120, new Color(1f, 0.85f, 0.4f));

        // Llegada al panel (SolarPanelReached) y mesa de reparación (SolarPanelRepaired)
        var zGO = new GameObject("Zona Panel Solar");
        zGO.transform.SetParent(c.root, false);
        zGO.transform.SetPositionAndRotation(station - dir * 1.2f, stationRot);
        var z = zGO.AddComponent<MissionZone>();
        z.zoneName = "junto al panel solar";
        z.size = new Vector3(4f, 4f, 3f);
        z.completeAction = true;
        z.action = MissionAction.SolarPanelReached;

        var mount = new GameObject("Soporte Mesa EVA").transform;
        mount.SetParent(c.root, false);
        mount.SetPositionAndRotation(station, stationRot);
        Box("Brazo de anclaje", mount, new Vector3(0f, 0f, -0.6f), new Vector3(0.15f, 0.15f, 1.2f), mMetal, false, false, true);
        BuildRepairStation(c.root, "Mesa Panel Solar · Reparación EVA", station, -dir,
                           RepairStationType.PistolGrip_WelderAndDrill, SpaceToolType.PistolGripTool,
                           "REPARACIÓN DEL PANEL SOLAR", true, MissionAction.SolarPanelRepaired);
        BuildTool(c.root, SpaceToolType.PistolGripTool, station + right * 0.75f - dir * 0.1f, -dir);
        Label(c.root, "SECCIÓN DAÑADA DEL PANEL", station + Vector3.up * 0.75f, -dir, new Vector2(1.6f, 0.18f), 90, new Color(1f, 0.5f, 0.35f));

        // Chispas en el panel dañado (sustituyen a la luz parpadeante); paran al repararlo
        var dmgRoot = new GameObject("Chispas panel dañado").transform;
        dmgRoot.SetParent(c.root, false);
        var dmg = dmgRoot.gameObject.AddComponent<DamageSparks>();
        dmg.stopWhenDone = MissionAction.SolarPanelRepaired;
        dmg.sparks = new[]
        {
            SparkBurst(dmgRoot, station + right * 0.5f + Vector3.up * 0.35f, -dir),
            SparkBurst(dmgRoot, station - right * 0.6f + Vector3.up * 0.2f, -dir),
            SparkBurst(dmgRoot, station + Vector3.up * 0.55f, -dir + Vector3.up * 0.3f),
        };
        foreach (var ps in dmg.sparks) { var mm = ps.main; mm.gravityModifier = 0f; }   // en el espacio no caen

        // Luz de trabajo
        var spot = new GameObject("Foco EVA");
        spot.transform.SetParent(c.root, false);
        spot.transform.position = station - dir * 2.5f + Vector3.up * 2f;
        spot.transform.rotation = Quaternion.LookRotation(station - spot.transform.position);
        var sl = spot.AddComponent<Light>();
        sl.type = LightType.Spot; sl.range = 10f; sl.spotAngle = 55f; sl.intensity = 3f; sl.shadows = LightShadows.None;
        spot.AddComponent<UniversalAdditionalLightData>();

        // Compañero de EVA flotando junto al panel, y la Luna al fondo
        var buddy = AddAstronaut(c.root, "Astronauta compañero (EVA)", station - right * 2.2f - dir * 0.8f - Vector3.up * 0.6f,
                                 Quaternion.LookRotation(right, Vector3.up), 1.8f);
        if (buddy != null)
            Label(c.root, "CMDTE. RÍOS · APOYO EVA", buddy.transform.position + Vector3.up * 2.1f, -dir, new Vector2(1.4f, 0.14f), 70, new Color(0.75f, 0.95f, 1f));
        AddMoon(c.root, new Vector3(-1500f, 1800f, -5000f), 380f);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        report.Add("Escena: " + SEva + " (panel: " + (bestName.Length > 0 ? bestName : "?") + ", " + Vector3.Distance(spawnPos, station).ToString("0") + " m desde Quest)");
    }

    // ════════════════════════════════════════════════════════════════════════
    // 01_ORBIT: marcadores sobre cada módulo del modelo
    // ════════════════════════════════════════════════════════════════════════

    static void AddMarkersToOrbit()
    {
        if (!File.Exists(OrbitScene)) return;
        var scene = EditorSceneManager.OpenScene(OrbitScene, OpenSceneMode.Single);

        foreach (var n in new[] { "Acceso Interior ISS", "Marcadores Módulos" })
        {
            var old = GameObject.Find(n);
            if (old != null) Object.DestroyImmediate(old);
        }

        var pl = GameObject.FindGameObjectWithTag("Player");
        if (pl != null)
        {
            var rig = pl.GetComponent<NovaInteractionRig>();
            if (rig == null) rig = pl.AddComponent<NovaInteractionRig>();
            rig.laserMaterial = mLaser; rig.controllerMaterial = mGlove;
            rig.desktopRayLength = 450f; rig.vrRayLength = 450f;
            if (pl.GetComponent<SceneEntry>() == null) pl.AddComponent<SceneEntry>();
            var cam = pl.GetComponentInChildren<Camera>(true);
            if (cam != null && cam.GetComponent<ScreenFader>() == null) AddFader(cam.transform);
        }

        var root = new GameObject("Marcadores Módulos").transform;
        report.Add("01_Orbit: colisiones en " + AddIssColliders(pl) + " mallas de la ISS.");

        // Astronauta flotando junto a la esclusa Quest y la Luna de fondo
        foreach (var n in new[] { "Astronauta (exterior)", "Luna (Apollo 11)" })
        {
            var old = GameObject.Find(n);
            if (old != null) Object.DestroyImmediate(old);
        }
        Bounds? qb = BoundsOf("12 Quest Airlock");
        if (qb.HasValue)
            AddAstronaut(root, "Astronauta (exterior)", qb.Value.center + new Vector3(4f, 2f, 2f), Quaternion.Euler(10f, -120f, 15f), 1.8f);
        AddMoon(root, new Vector3(-1500f, 1800f, -5000f), 380f);
        var modules = new[]
        {
            new[] { "02 Unity Node 1",               SNodo,    "Nodo 1 Unity" },
            new[] { "09 Destiny Space Laboratory",   SDestiny, "Destiny · Laboratorio" },
            new[] { "05 Zvezda (SM) Service Module", SZvezda,  "Zvezda · Servicio" },
            new[] { "12 Quest Airlock",              SQuest,   "Quest · Esclusa" },
            new[] { "38 Cupola",                     SCupula,  "Cúpula" },
        };

        int placed = 0;
        foreach (var m in modules)
        {
            Bounds? b = BoundsOf(m[0]);
            if (!b.HasValue) { report.Add("AVISO: no se encontró '" + m[0] + "' en 01_Orbit."); continue; }

            var mk = new GameObject("Marcador · " + m[2]);
            mk.transform.SetParent(root, false);
            mk.transform.position = b.Value.center + Vector3.up * (b.Value.extents.y + 6f + placed * 0.5f);

            var orb = Prim(PrimitiveType.Sphere, "Esfera", mk.transform, Vector3.zero, Vector3.one * 3f, mCyan, false, false);
            // Zona de clic generosa: a ~170 m una esfera de 3 m es difícil de apuntar
            var hit = mk.AddComponent<SphereCollider>();
            hit.radius = 9f;
            hit.isTrigger = true;   // el láser lo detecta, pero el jugador no choca con él
            Rod("Línea", mk.transform, Vector3.down * ((b.Value.extents.y + 6f) * 0.5f), 0.08f, b.Value.extents.y + 6f, Vector3.up, mCyan);

            var bb = new GameObject("Cartel").transform;
            bb.SetParent(mk.transform, false);
            bb.localPosition = Vector3.up * 2.6f;
            // Texto pequeño escalado ×20 (fuentes enormes desbordan el atlas de la fuente)
            var scaler = new GameObject("Escala").transform;
            scaler.SetParent(bb, false);
            scaler.localScale = Vector3.one * 20f;
            var lbl = NovaText.Create(scaler, "Texto", m[2], Vector3.zero, Quaternion.identity, new Vector2(0.8f, 0.15f), 70, new Color(0.75f, 0.95f, 1f));
            lbl.horizontalOverflow = HorizontalWrapMode.Overflow;

            var marker = mk.AddComponent<ModuleMarker>();
            marker.sceneName = m[1];
            marker.spawnId = "";
            marker.moduleName = m[2];
            marker.billboard = bb;
            marker.label = lbl;
            marker.maxDistance = 600f;
            marker.prompt = "Entrar a " + m[2];
            marker.highlightRenderers = new[] { orb.GetComponent<Renderer>() };
            placed++;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        report.Add("01_Orbit: " + placed + " marcadores de módulo (apuntar + mantener = entrar).");
    }
}
#endif
