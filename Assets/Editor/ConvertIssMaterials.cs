using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class ConvertIssMaterials
{
    private const string MatFolder = "Assets/NovaCore/Art/ISS/Materials";
    private const string ScenePath = "Assets/NovaCore/Scenes/01_Orbit.unity";

    [UnityEditor.MenuItem("Nova Core/Convert ISS Materials (URP)")]
    public static void RunFromMenu() { Run(); }

    public static string Run()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != ScenePath) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        var iss = GameObject.Find("ISS");
        if (!iss) return "no ISS in scene";

        if (!AssetDatabase.IsValidFolder(MatFolder))
        {
            var parts = MatFolder.Split('/');
            var current = parts[0];
            for (var i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        var lit = Shader.Find("Universal Render Pipeline/Lit");
        if (!lit) return "URP Lit shader not found";

        var renderers = iss.GetComponentsInChildren<Renderer>(true);
        var oldMats = renderers.SelectMany(r => r.sharedMaterials).Where(m => m).Distinct().ToArray();
        var map = new Dictionary<Material, Material>();
        var sb = new StringBuilder();

        foreach (var old in oldMats)
        {
            var safeName = new string(old.name.Where(c => c != '*' && c != '/' && c != '\\' && c != ':' && c != '"' && c != '<' && c != '>' && c != '|' && c != '?').ToArray()).Trim();
            var assetPath = MatFolder + "/M_" + safeName + ".mat";

            if (AssetDatabase.LoadAssetAtPath<Material>(assetPath)) AssetDatabase.DeleteAsset(assetPath);

            var mat = new Material(lit);
            mat.name = "M_" + safeName;

            var baseTex = old.HasProperty("baseColorTexture") ? old.GetTexture("baseColorTexture") : null;
            if (baseTex) mat.SetTexture("_BaseMap", baseTex);

            var baseColor = old.HasProperty("baseColorFactor") ? old.GetColor("baseColorFactor") : Color.white;
            mat.SetColor("_BaseColor", baseColor);

            var metallic = old.HasProperty("metallicFactor") ? old.GetFloat("metallicFactor") : 0f;
            var roughness = old.HasProperty("roughnessFactor") ? old.GetFloat("roughnessFactor") : 1f;
            mat.SetFloat("_Metallic", metallic);
            mat.SetFloat("_Smoothness", Mathf.Clamp01(1f - roughness));

            var normalTex = old.HasProperty("normalTexture") ? old.GetTexture("normalTexture") : null;
            if (normalTex)
            {
                mat.SetTexture("_BumpMap", normalTex);
                mat.SetFloat("_BumpScale", old.HasProperty("normalTexture_scale") ? old.GetFloat("normalTexture_scale") : 1f);
                mat.EnableKeyword("_NORMALMAP");
            }

            var emissive = old.HasProperty("emissiveFactor") ? old.GetColor("emissiveFactor") : Color.black;
            if (emissive.maxColorComponent > 0.001f)
            {
                mat.SetColor("_EmissionColor", emissive);
                mat.EnableKeyword("_EMISSION");
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            }

            mat.SetFloat("_Surface", 0f);
            mat.SetFloat("_Blend", 0f);
            mat.SetFloat("_AlphaClip", 0f);
            mat.SetFloat("_SrcBlend", (float)BlendMode.One);
            mat.SetFloat("_DstBlend", (float)BlendMode.Zero);
            mat.SetFloat("_ZWrite", 1f);
            mat.SetFloat("_Cull", (float)CullMode.Back);
            mat.renderQueue = (int)RenderQueue.Geometry;
            mat.enableInstancing = true;

            AssetDatabase.CreateAsset(mat, assetPath);
            map[old] = mat;
            sb.AppendLine(string.Format("  {0} -> {1} (base={2}, metallic={3:F2}, smooth={4:F2}, normal={5})",
                old.name, assetPath, baseTex ? baseTex.name : "-", metallic, 1f - roughness, normalTex ? normalTex.name : "-"));
        }

        AssetDatabase.SaveAssets();

        var replaced = 0;
        foreach (var r in renderers)
        {
            var mats = r.sharedMaterials;
            for (var i = 0; i < mats.Length; i++)
            {
                if (mats[i] && map.TryGetValue(mats[i], out var fresh))
                {
                    mats[i] = fresh;
                    r.sharedMaterials = mats;
                    replaced++;
                }
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        var stillOld = iss.GetComponentsInChildren<Renderer>(true)
            .SelectMany(r => r.sharedMaterials).Where(m => m)
            .Count(m => m.shader.name == "Shader Graphs/glTF-pbrMetallicRoughness");
        var newUnique = iss.GetComponentsInChildren<Renderer>(true)
            .SelectMany(r => r.sharedMaterials).Where(m => m)
            .Distinct().Count();

        return sb.ToString() +
               string.Format("created={0} slotsReplaced={1} slotsStillOnGraphShader={2} uniqueMaterialsNow={3} saved={4}",
                   map.Count, replaced, stillOld, newUnique, true);
    }
}
