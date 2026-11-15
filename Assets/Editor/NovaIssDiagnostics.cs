#if UNITY_EDITOR
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Nova Core → Diagnóstico → Listar partes de la ISS: tamaños y centros de cada parte (en la consola).</summary>
public static class NovaIssDiagnostics
{
    [MenuItem("Nova Core/Diagnóstico/Listar partes de la ISS")]
    public static void ListParts()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.OpenScene("Assets/NovaCore/Scenes/01_Orbit.unity", OpenSceneMode.Single);
        var iss = GameObject.Find("ISS");
        if (iss == null) { Debug.LogWarning("[NovaIssDiagnostics] No hay objeto 'ISS' en la escena abierta."); return; }
        var sb = new StringBuilder("[NovaIssDiagnostics] nombre | centro | tamaño\n");
        foreach (var r in iss.GetComponentsInChildren<Renderer>(true))
        {
            var b = r.bounds;
            sb.AppendLine(r.gameObject.name + " | " + b.center.ToString("F1") + " | " + b.size.ToString("F1"));
        }
        var all = iss.GetComponentsInChildren<Renderer>(true);
        Bounds t = all[0].bounds; foreach (var r in all) t.Encapsulate(r.bounds);
        sb.AppendLine("TOTAL | " + t.center.ToString("F1") + " | " + t.size.ToString("F1"));
        Debug.Log(sb.ToString());
    }
}
#endif
