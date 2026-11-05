using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class SetupEnvironment
{
    private const string Root = "Assets/NovaCore";
    private const string Art = Root + "/Art";
    private const string SkyboxDir = Art + "/Skybox";
    private const string MatDir = Art + "/Materials";
    private const string ScenesDir = Root + "/Scenes";
    private const string SettingsDir = Root + "/Settings";

    private const string Hdr4kPath = SkyboxDir + "/T_Skybox_DeepSpace_4K.hdr";
    private const string Png2kPath = SkyboxDir + "/T_Skybox_DeepSpace_2K.png";
    private const string Png4kPath = SkyboxDir + "/T_Skybox_DeepSpace_4K_LDR.png";

    private const string MatMainPath = MatDir + "/M_Skybox_DeepSpace.mat";
    private const string MatHdrPath = MatDir + "/M_Skybox_DeepSpace_HDR.mat";
    private const string MatLegacyMobilePath = MatDir + "/M_Skybox_DeepSpace_Mobile.mat";
    private const string ProfilePath = SettingsDir + "/NovaCore_OrbitProfile.asset";
    private const string ScenePath = ScenesDir + "/01_Orbit.unity";
    private const string SampleScenePath = "Assets/Scenes/SampleScene.unity";

    public static string Run()
    {
        EnsureFolder(Root);
        EnsureFolder(Art);
        EnsureFolder(SkyboxDir);
        EnsureFolder(MatDir);
        EnsureFolder(ScenesDir);
        EnsureFolder(SettingsDir);

        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        ConfigureSkyboxTexture(Hdr4kPath, 4096, false, TextureImporterCompression.Uncompressed, true);
        ConfigureSkyboxTexture(Png2kPath, 2048, true, TextureImporterCompression.CompressedHQ, false);
        ConfigureSkyboxTexture(Png4kPath, 2048, true, TextureImporterCompression.CompressedHQ, false);

        var cube4k = AssetDatabase.LoadAssetAtPath<Cubemap>(Hdr4kPath);
        var cube2k = AssetDatabase.LoadAssetAtPath<Cubemap>(Png2kPath);
        if (cube4k == null) throw new Exception("No se pudo cargar el cubemap HDR: " + Hdr4kPath);
        if (cube2k == null) throw new Exception("No se pudo cargar el cubemap 2K: " + Png2kPath);

        string sunDetails;
        var sunDir = FindBrightestDirection(cube4k, out sunDetails);
        var sunElevation = Mathf.Asin(Mathf.Clamp(sunDir.y, -1f, 1f)) * Mathf.Rad2Deg;

        FinalizeSkyboxTexture(Hdr4kPath, 2048, TextureImporterCompression.CompressedHQ);
        cube4k = AssetDatabase.LoadAssetAtPath<Cubemap>(Hdr4kPath);
        if (cube4k == null) throw new Exception("No se pudo recargar el cubemap HDR: " + Hdr4kPath);

        var matMain = CreateSkyboxMaterial(MatMainPath, "M_Skybox_DeepSpace", cube2k);
        var matHdr = CreateSkyboxMaterial(MatHdrPath, "M_Skybox_DeepSpace_HDR", cube4k);
        if (AssetDatabase.LoadAssetAtPath<Material>(MatLegacyMobilePath) != null)
            AssetDatabase.DeleteAsset(MatLegacyMobilePath);

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var sun = CreateSun(sunDir);
        var camera = CreateCamera();
        var volume = CreateVolume();

        RenderSettings.skybox = matMain;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.02f, 0.02f, 0.03f, 1f);
        RenderSettings.ambientIntensity = 1f;
        RenderSettings.fog = false;
        RenderSettings.sun = sun.GetComponent<Light>();
        RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
        RenderSettings.reflectionIntensity = 0.35f;

        EditorSceneManager.SaveScene(scene, ScenePath);
        AddSceneToBuildSettings(ScenePath);

        Selection.activeGameObject = camera;
        if (SceneView.lastActiveSceneView != null)
            SceneView.lastActiveSceneView.FrameSelected();

        var report = string.Format(
            "OK\nscene={0}\nskybox={1}\nhdrVariant={2}\nsunDir={3}\nsunElevation={4:F1} deg\nsunRotation={5}\ncamera={6}\nvolume={7}\ncubeFace={8}\ndetails={9}",
            ScenePath, MatMainPath, MatHdrPath, sunDir.ToString("F3"), sunElevation,
            sun.transform.eulerAngles.ToString("F1"), camera.transform.position.ToString("F1"),
            ProfilePath, cube2k.width, sunDetails);

        Debug.Log("[SetupEnvironment]\n" + report);
        return report;
    }

    private static void EnsureFolder(string path)
    {
        var full = System.IO.Path.Combine(Application.dataPath, "..", path);
        System.IO.Directory.CreateDirectory(full);
    }

    private static void ConfigureSkyboxTexture(string path, int maxSize, bool sRGB, TextureImporterCompression compression, bool readable)
    {
        if (AssetImporter.GetAtPath(path) == null)
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);

        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) throw new Exception("No hay TextureImporter para " + path);

        importer.textureType = TextureImporterType.Default;
        importer.textureShape = TextureImporterShape.TextureCube;
        importer.generateCubemap = TextureImporterGenerateCubemap.Cylindrical;
        importer.maxTextureSize = maxSize;
        importer.textureCompression = compression;
        importer.sRGBTexture = sRGB;
        importer.mipmapEnabled = true;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Trilinear;
        importer.anisoLevel = 1;
        importer.isReadable = readable;
        importer.SaveAndReimport();
    }

    private static void SetReadable(string path, bool readable)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null || importer.isReadable == readable) return;
        importer.isReadable = readable;
        importer.SaveAndReimport();
    }

    private static void FinalizeSkyboxTexture(string path, int maxSize, TextureImporterCompression compression)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return;
        importer.isReadable = false;
        importer.maxTextureSize = maxSize;
        importer.textureCompression = compression;
        importer.SaveAndReimport();
    }

    private static Material CreateSkyboxMaterial(string path, string name, Cubemap cube)
    {
        var shader = Shader.Find("Skybox/Cubemap");
        if (shader == null) throw new Exception("No se encontro el shader Skybox/Cubemap");

        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
        }
        else
        {
            mat.shader = shader;
        }

        mat.name = name;
        mat.SetTexture("_Tex", cube);
        mat.SetColor("_Tint", Color.white);
        mat.SetFloat("_Exposure", 1.0f);
        mat.SetFloat("_Rotation", 0f);
        EditorUtility.SetDirty(mat);
        AssetDatabase.SaveAssets();
        return mat;
    }

    private static GameObject CreateSun(Vector3 sunDir)
    {
        var go = new GameObject("Sun");
        go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.LookRotation(-sunDir.normalized, Vector3.up));

        var light = go.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(1f, 0.96f, 0.9f);
        light.intensity = 1.35f;
        light.shadows = LightShadows.None;
        light.lightmapBakeType = LightmapBakeType.Realtime;
        go.AddComponent<UniversalAdditionalLightData>();
        return go;
    }

    private static GameObject CreateCamera()
    {
        var go = new GameObject("Main Camera");
        go.tag = "MainCamera";
        go.transform.position = new Vector3(0f, 1.6f, -180f);
        go.transform.rotation = Quaternion.LookRotation(new Vector3(0f, 5.4f, 0f) - go.transform.position, Vector3.up);

        var cam = go.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.Skybox;
        cam.fieldOfView = 60f;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 5000f;
        cam.allowHDR = true;
        cam.allowMSAA = true;

        var data = go.AddComponent<UniversalAdditionalCameraData>();
        data.renderPostProcessing = true;
        go.AddComponent<AudioListener>();
        return go;
    }

    private static GameObject CreateVolume()
    {
        if (AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath) != null)
            AssetDatabase.DeleteAsset(ProfilePath);

        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        AssetDatabase.CreateAsset(profile, ProfilePath);

        var tonemapping = AddComponent<Tonemapping>(profile);
        tonemapping.mode.overrideState = true;
        tonemapping.mode.value = TonemappingMode.Neutral;

        var bloom = AddComponent<Bloom>(profile);
        bloom.threshold.overrideState = true;
        bloom.threshold.value = 0.9f;
        bloom.intensity.overrideState = true;
        bloom.intensity.value = 0.35f;
        bloom.scatter.overrideState = true;
        bloom.scatter.value = 0.6f;
        bloom.highQualityFiltering.overrideState = true;
        bloom.highQualityFiltering.value = false;
        bloom.downscale.overrideState = true;
        bloom.downscale.value = BloomDownscaleMode.Quarter;

        var color = AddComponent<ColorAdjustments>(profile);
        color.postExposure.overrideState = true;
        color.postExposure.value = 0.25f;
        color.contrast.overrideState = true;
        color.contrast.value = 5f;
        color.saturation.overrideState = true;
        color.saturation.value = 5f;

        var vignette = AddComponent<Vignette>(profile);
        vignette.intensity.overrideState = true;
        vignette.intensity.value = 0.15f;
        vignette.smoothness.overrideState = true;
        vignette.smoothness.value = 0.4f;

        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();

        var go = new GameObject("Global Volume");
        var volume = go.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 0f;
        volume.profile = profile;
        return go;
    }

    private static T AddComponent<T>(VolumeProfile profile) where T : VolumeComponent
    {
        var component = profile.Add<T>();
        if (!AssetDatabase.Contains(component))
            AssetDatabase.AddObjectToAsset(component, profile);
        return component;
    }

    private static void AddSceneToBuildSettings(string scenePath)
    {
        var scenes = EditorBuildSettings.scenes.ToList();
        var existing = scenes.FirstOrDefault(s => s.path == scenePath);
        if (existing == null)
        {
            scenes.Insert(0, new EditorBuildSettingsScene(scenePath, true));
        }
        else
        {
            existing.enabled = true;
            scenes.RemoveAll(s => s.path == scenePath);
            scenes.Insert(0, existing);
        }

        foreach (var s in scenes)
            if (s.path == SampleScenePath)
                s.enabled = false;

        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static Vector3 FindBrightestDirection(Cubemap cube, out string details)
    {
        var faceSize = cube.width;
        var mip = 0;
        while ((faceSize >> mip) > 256 && mip < cube.mipmapCount - 1)
            mip++;

        var size = faceSize >> mip;
        var best = -1f;
        var bestFace = 0;
        var bestX = 0;
        var bestY = 0;

        for (var f = 0; f < 6; f++)
        {
            var pixels = cube.GetPixels((CubemapFace)f, mip);
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var c = pixels[y * size + x];
                    var lum = Mathf.Max(c.r, Mathf.Max(c.g, c.b));
                    if (lum > best)
                    {
                        best = lum;
                        bestFace = f;
                        bestX = x;
                        bestY = y;
                    }
                }
            }
        }

        var u = 2f * (bestX + 0.5f) / size - 1f;
        var v = 2f * (bestY + 0.5f) / size - 1f;

        var dirA = FaceUvToDirection(bestFace, u, v);
        var dirB = FaceUvToDirection(bestFace, u, -v);
        var dir = dirA.y >= dirB.y ? dirA : dirB;

        details = string.Format(
            "face={0} uv=({1:F3},{2:F3}) faceSize={3} mip={4} max={5:F1} dirA={6} dirB={7}",
            bestFace, u, v, size, mip, best, dirA.ToString("F3"), dirB.ToString("F3"));

        return dir.normalized;
    }

    private static Vector3 FaceUvToDirection(int face, float u, float v)
    {
        switch (face)
        {
            case 0: return new Vector3(1f, -v, -u);   // +X
            case 1: return new Vector3(-1f, -v, u);   // -X
            case 2: return new Vector3(u, 1f, v);     // +Y
            case 3: return new Vector3(u, -1f, -v);   // -Y
            case 4: return new Vector3(u, -v, 1f);    // +Z
            default: return new Vector3(-u, -v, -1f); // -Z
        }
    }
}
