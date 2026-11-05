using System.Linq;
using System.Text;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PlaceCamera
{
    private const float Distance = 150f;
    private const float YawOffset = 45f;

    public static string Run()
    {
        var scene = SceneManager.GetActiveScene();
        var cam = Camera.main;
        var iss = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "ISS");
        var sun = GameObject.Find("Sun");
        if (!cam || !iss || !sun) return "missing camera, ISS or Sun";

        var rs = iss.GetComponentsInChildren<Renderer>(true);
        var b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        var center = b.center;
        var sunDir = -sun.transform.forward;

        var pos = center + Quaternion.Euler(0f, YawOffset, 0f) * sunDir * Distance;
        var rot = Quaternion.LookRotation(center - pos, Vector3.up);
        cam.transform.SetPositionAndRotation(pos, rot);

        var sb = new StringBuilder();
        sb.AppendLine(string.Format("camera pos={0} euler={1}", pos.ToString("F2"), cam.transform.eulerAngles.ToString("F2")));
        sb.AppendLine(string.Format("distance={0} yawOffset={1} sunDotView={2:F2} (<0 = sol a la espalda, cara visible iluminada)",
            Distance, YawOffset, Vector3.Dot(cam.transform.forward, -sun.transform.forward)));

        foreach (var aspect in new[] { 1.25f, 0.9546f })
        {
            var prev = cam.aspect;
            cam.aspect = aspect;
            float minX = 1f, maxX = 0f, minY = 1f, maxY = 0f;
            for (var i = 0; i < 8; i++)
            {
                var c = center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                var v = cam.WorldToViewportPoint(c);
                minX = Mathf.Min(minX, v.x); maxX = Mathf.Max(maxX, v.x);
                minY = Mathf.Min(minY, v.y); maxY = Mathf.Max(maxY, v.y);
            }
            cam.aspect = prev;
            sb.AppendLine(string.Format("aspect {0:F3}: x=[{1:F3},{2:F3}] y=[{3:F3},{4:F3}] fill={5:F1}% x {6:F1}% fits={7}",
                aspect, minX, maxX, minY, maxY, (maxX - minX) * 100f, (maxY - minY) * 100f,
                minX > 0f && maxX < 1f && minY > 0f && maxY < 1f));
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        sb.AppendLine("saved=true");
        return sb.ToString();
    }
}
