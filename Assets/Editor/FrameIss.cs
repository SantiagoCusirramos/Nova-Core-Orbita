using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class FrameIss
{
    public static string Run()
    {
        var cam = Camera.main;
        var iss = GameObject.Find("ISS");
        if (!cam || !iss) return "missing camera or ISS";

        var renderers = iss.GetComponentsInChildren<Renderer>(true);
        var b = renderers[0].bounds;
        foreach (var r in renderers) b.Encapsulate(r.bounds);

        var dist = 180f;
        cam.transform.position = new Vector3(b.center.x, b.center.y, b.center.z - dist);
        cam.transform.rotation = Quaternion.LookRotation(b.center - cam.transform.position, Vector3.up);

        var sb = new StringBuilder();
        sb.AppendLine(string.Format("cam={0} euler={1}", cam.transform.position.ToString("F2"), cam.transform.eulerAngles.ToString("F2")));

        float minX = 1f, maxX = 0f, minY = 1f, maxY = 0f;
        foreach (var sx in new[] { -1f, 1f })
            foreach (var sy in new[] { -1f, 1f })
                foreach (var sz in new[] { -1f, 1f })
                {
                    var c = b.center + Vector3.Scale(b.extents, new Vector3(sx, sy, sz));
                    var v = cam.WorldToViewportPoint(c);
                    minX = Mathf.Min(minX, v.x); maxX = Mathf.Max(maxX, v.x);
                    minY = Mathf.Min(minY, v.y); maxY = Mathf.Max(maxY, v.y);
                }
        sb.AppendLine(string.Format("viewport x=[{0:F3},{1:F3}] y=[{2:F3},{3:F3}]  (0..1 = dentro)", minX, maxX, minY, maxY));
        sb.AppendLine(string.Format("fitsHorizontally={0} fitsVertically={1}", minX > 0f && maxX < 1f, minY > 0f && maxY < 1f));
        sb.AppendLine(string.Format("screenFill={0:F1}% x {1:F1}%", (maxX - minX) * 100f, (maxY - minY) * 100f));

        var dir = cam.transform.forward;
        var sun = GameObject.Find("Sun");
        if (sun)
        {
            var lightDir = sun.transform.forward;
            var phong = Vector3.Dot(dir, -lightDir);
            sb.AppendLine(string.Format("sunDotView={0:F2} (>0 = sol de frente, <0 = sol por detras de la ISS)", phong));
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        return sb.ToString();
    }
}
