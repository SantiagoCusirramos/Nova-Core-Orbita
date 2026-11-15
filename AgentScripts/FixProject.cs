using System;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class FixProject
{
    const string SCENE_PATH = "Assets/NovaCore/Scenes/01_Orbit.unity";
    const string PROFILE_PATH = "Assets/NovaCore/Settings/NovaCore_OrbitProfile.asset";
    const string MAT_2K = "Assets/NovaCore/Art/Materials/M_Skybox_DeepSpace.mat";

    public static string All()
    {
        var sb = new StringBuilder();

        // ── Abrir escena ────────────────────────────────────────────────────
        var scene = SceneManager.GetActiveScene();
        if (scene.path != SCENE_PATH)
            scene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);

        // ── 1. Volume profile ───────────────────────────────────────────────
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(PROFILE_PATH);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, PROFILE_PATH);
            var tone = profile.Add<Tonemapping>();
            tone.mode.overrideState = true; tone.mode.value = TonemappingMode.Neutral;
            var bloom = profile.Add<Bloom>();
            bloom.threshold.overrideState = true; bloom.threshold.value = 0.9f;
            bloom.intensity.overrideState = true; bloom.intensity.value = 0.35f;
            bloom.scatter.overrideState = true; bloom.scatter.value = 0.6f;
            var color = profile.Add<ColorAdjustments>();
            color.postExposure.overrideState = true; color.postExposure.value = 0.25f;
            color.contrast.overrideState = true; color.contrast.value = 5f;
            color.saturation.overrideState = true; color.saturation.value = 5f;
            var vig = profile.Add<Vignette>();
            vig.intensity.overrideState = true; vig.intensity.value = 0.15f;
            vig.smoothness.overrideState = true; vig.smoothness.value = 0.4f;
            foreach (var comp in profile.components)
                if (!AssetDatabase.Contains(comp)) AssetDatabase.AddObjectToAsset(comp, profile);
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            sb.AppendLine("VolumeProfile: creado " + PROFILE_PATH);
        }
        else sb.AppendLine("VolumeProfile: existente " + PROFILE_PATH +
                           " (" + string.Join(",", profile.components.Select(c => c.GetType().Name)) + ")");

        var vol = UnityEngine.Object.FindAnyObjectByType<Volume>();
        if (vol == null)
        {
            var go = new GameObject("Global Volume");
            vol = go.AddComponent<Volume>();
        }
        vol.isGlobal = true;
        vol.sharedProfile = profile;
        EditorUtility.SetDirty(vol);
        sb.AppendLine("Global Volume: profile asignado = " + (vol.sharedProfile ? vol.sharedProfile.name : "NULL"));

        // ── 2. Skybox material en RenderSettings ────────────────────────────
        var sky = AssetDatabase.LoadAssetAtPath<Material>(MAT_2K);
        if (sky != null && sky.HasProperty("_Tex") && sky.GetTexture("_Tex") != null)
        {
            RenderSettings.skybox = sky;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
            RenderSettings.reflectionIntensity = 0.35f;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.035f, 0.04f, 0.055f);
            sb.AppendLine("Skybox: " + sky.name + " _Tex=" + sky.GetTexture("_Tex").name);
        }
        else sb.AppendLine("Skybox: ERROR material sin textura");

        // ── 3. Sun ──────────────────────────────────────────────────────────
        var sun = GameObject.Find("Sun");
        if (sun != null)
        {
            var l = sun.GetComponent<Light>();
            if (l != null)
            {
                l.type = LightType.Directional;
                l.intensity = 1.35f;
                l.color = new Color(1f, 0.97f, 0.9f);
                l.shadows = LightShadows.Soft;
                sun.transform.rotation = Quaternion.Euler(11.9f, 151.8f, 0f);
                RenderSettings.sun = l;
                EditorUtility.SetDirty(l);
            }
            sb.AppendLine("Sun: intensidad 1.35, sombras Soft, rot (11.9,151.8,0)");
        }

        // ── 4. Cámara ───────────────────────────────────────────────────────
        var cam = Camera.main;
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.fieldOfView = 75f;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 5000f;
            cam.allowHDR = true;
            cam.allowMSAA = true;
            var data = cam.GetUniversalAdditionalCameraData();
            if (data != null) { data.renderPostProcessing = true; data.antialiasing = AntialiasingMode.FastApproximateAntialiasing; }
            EditorUtility.SetDirty(cam);
            sb.AppendLine("Camera: clear=Skybox fov=75 near=0.05 far=5000 postFX=true");
        }

        // ── 5. Player: rig XR (sin duplicar movimiento) ─────────────────────
        var player = GameObject.Find("Player");
        if (player != null)
        {
            if (player.GetComponent<NovaCore.XRPlayerRig>() == null)
            {
                player.AddComponent<NovaCore.XRPlayerRig>();
                sb.AppendLine("Player: XRPlayerRig añadido (head/controllers tracking en VR)");
            }
            else sb.AppendLine("Player: XRPlayerRig ya presente");
        }

        // ── 6. ISS: comprobar materiales ────────────────────────────────────
        var iss = GameObject.Find("ISS");
        if (iss != null)
        {
            var renderers = iss.GetComponentsInChildren<Renderer>(true);
            var mats = renderers.SelectMany(r => r.sharedMaterials).Where(m => m != null).Distinct().ToArray();
            int unsupported = 0, magenta = 0, withTex = 0;
            foreach (var m in mats)
            {
                if (m.shader == null || !m.shader.isSupported) unsupported++;
                if (m.shader != null && m.shader.name == "Hidden/InternalErrorShader") magenta++;
                if (m.HasProperty("baseColorTexture") && m.GetTexture("baseColorTexture") != null) withTex++;
                else if (m.HasProperty("_BaseMap") && m.GetTexture("_BaseMap") != null) withTex++;
            }
            sb.AppendLine("ISS: renderers=" + renderers.Length + " mats=" + mats.Length +
                          " sinTextura=" + (mats.Length - withTex) + " shaderNoSoportado=" + unsupported +
                          " magenta=" + magenta);
        }

        // ── 7. Guardar ──────────────────────────────────────────────────────
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        sb.AppendLine("Escena guardada: " + SCENE_PATH);
        return sb.ToString();
    }
}
