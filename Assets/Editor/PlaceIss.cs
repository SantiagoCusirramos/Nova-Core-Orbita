using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PlaceIss
{
    private const string GlbPath = "Assets/NovaCore/Art/ISS/ISS_stationary.glb";
    private const string ScenePath = "Assets/NovaCore/Scenes/01_Orbit.unity";

    public static string Run()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != ScenePath)
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        var existing = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "ISS");
        if (existing) Object.DestroyImmediate(existing);

        var main = AssetDatabase.LoadMainAssetAtPath(GlbPath) as GameObject;
        if (!main) return "no main asset";

        var instance = Object.Instantiate(main);
        instance.name = "ISS";
        SceneManager.MoveGameObjectToScene(instance, scene);
        instance.transform.localPosition = new Vector3(0f, 0f, 3.8f);
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one;

        // Static batching: the station never moves.
        foreach (var t in instance.GetComponentsInChildren<Transform>(true))
            t.gameObject.isStatic = true;

        var renderers = instance.GetComponentsInChildren<Renderer>(true);
        var filters = instance.GetComponentsInChildren<MeshFilter>(true);
        var bounds = renderers[0].bounds;
        foreach (var r in renderers) bounds.Encapsulate(r.bounds);

        var tris = filters.Where(f => f.sharedMesh).Sum(f => f.sharedMesh.triangles.Length / 3);
        var verts = filters.Where(f => f.sharedMesh).Sum(f => f.sharedMesh.vertexCount);
        var noTangent = filters.Count(f => f.sharedMesh && f.sharedMesh.tangents.Length == 0);

        var slots = renderers.SelectMany(r => r.sharedMaterials).ToArray();
        var unique = slots.Where(m => m).Distinct().Count();
        var missing = slots.Count(m => !m);

        var sb = new StringBuilder();
        sb.AppendLine(string.Format("instance={0} children={1}", instance.name, instance.GetComponentsInChildren<Transform>(true).Length));
        sb.AppendLine(string.Format("boundsSize={0} boundsCenter={1}", bounds.size.ToString("F2"), bounds.center.ToString("F2")));
        sb.AppendLine(string.Format("boundsMin={0} boundsMax={1}", bounds.min.ToString("F2"), bounds.max.ToString("F2")));
        sb.AppendLine(string.Format("renderers={0} triangles={1} vertices={2}", renderers.Length, tris, verts));
        sb.AppendLine(string.Format("materialSlots={0} uniqueMaterials={1} nullSlots={2}", slots.Length, unique, missing));
        sb.AppendLine(string.Format("meshesWithoutTangents={0}", noTangent));
        sb.AppendLine(string.Format("staticRenderers={0}", renderers.Count(r => r.gameObject.isStatic)));

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        sb.AppendLine("saved=" + true);
        Debug.Log("[PlaceIss]\n" + sb);
        return sb.ToString();
    }

    public static string FrameCamera()
    {
        var cam = Camera.main;
        var iss = GameObject.Find("ISS");
        if (!cam || !iss) return "missing camera or ISS";
        var center = iss.GetComponentInChildren<Renderer>().bounds.center;
        foreach (var r in iss.GetComponentsInChildren<Renderer>()) { }
        var b = new Bounds(center, Vector3.zero);
        foreach (var r in iss.GetComponentsInChildren<Renderer>()) b.Encapsulate(r.bounds);
        cam.transform.position = new Vector3(0f, b.center.y, b.center.z - 180f);
        cam.transform.rotation = Quaternion.LookRotation(b.center - cam.transform.position, Vector3.up);
        return string.Format("cam={0} euler={1} target={2}", cam.transform.position.ToString("F1"),
            cam.transform.eulerAngles.ToString("F1"), b.center.ToString("F1"));
    }
}
