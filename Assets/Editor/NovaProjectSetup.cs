#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Nova Core → Configurar proyecto (Quest 2 + Build Settings)
///
/// Ajustes de proyecto que conviene tener antes del primer build en el visor:
///   • Build Settings: 01_Orbit (índice 0) y 02_Interior (índice 1).
///   • Player Settings: nombre de empresa/producto e identificador Android.
///   • Android: API mínima 32 (Meta Quest con firmware actual; Project Validation lo pide).
///   • URP móvil (Mobile_RPAsset): HDR desactivado (caro en Quest 2).
/// No toca nada más. Se puede ejecutar varias veces.
/// </summary>
public static class NovaProjectSetup
{
    const string OrbitScene    = "Assets/NovaCore/Scenes/01_Orbit.unity";
    const string InteriorScene = "Assets/Game/Scenes/02_Interior.unity";
    const string MobileRP      = "Assets/Settings/Mobile_RPAsset.asset";

    [MenuItem("Nova Core/Configurar proyecto (Quest 2 + Build Settings)")]
    public static void RunFromMenu()
    {
        string r = Run();
        EditorUtility.DisplayDialog("Nova Core — Configuración", r, "OK");
    }

    public static string Run()
    {
        var log = new List<string>();

        log.Add(RegisterScenes());

        PlayerSettings.companyName = "NovaCore";
        PlayerSettings.productName = "Nova Core Orbita";
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.novacore.orbita");
        log.Add("Player Settings: NovaCore / Nova Core Orbita / com.novacore.orbita");

        if ((int)PlayerSettings.Android.minSdkVersion < 32)
        {
            PlayerSettings.Android.minSdkVersion = (AndroidSdkVersions)32;
            log.Add("Android: API mínima → 32");
        }
        else log.Add("Android: API mínima ya era " + (int)PlayerSettings.Android.minSdkVersion);

        var rp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(MobileRP);
        if (rp != null)
        {
            if (rp.supportsHDR)
            {
                rp.supportsHDR = false;
                EditorUtility.SetDirty(rp);
                log.Add("URP móvil: HDR desactivado");
            }
            else log.Add("URP móvil: HDR ya estaba desactivado");
        }
        else log.Add("AVISO: no se encontró " + MobileRP);

        AssetDatabase.SaveAssets();
        string msg = string.Join("\n", log) +
                     "\n\nSiguiente: Project Settings → XR Plug-in Management → Project Validation (Android) y corrige lo que marque.";
        Debug.Log("[NovaProjectSetup]\n" + msg);
        return msg;
    }

    /// <summary>01_Orbit y 02_Interior en Build Settings (conserva otras escenas, desactivadas).</summary>
    public static string RegisterScenes()
    {
        var scenes = new List<EditorBuildSettingsScene>();
        if (File.Exists(OrbitScene))    scenes.Add(new EditorBuildSettingsScene(OrbitScene, true));
        if (File.Exists(InteriorScene)) scenes.Add(new EditorBuildSettingsScene(InteriorScene, true));

        foreach (var s in EditorBuildSettings.scenes)
        {
            if (s.path == OrbitScene || s.path == InteriorScene) continue;
            scenes.Add(new EditorBuildSettingsScene(s.path, false));
        }
        EditorBuildSettings.scenes = scenes.ToArray();
        return "Build Settings: " + string.Join(", ", scenes.FindAll(x => x.enabled).ConvertAll(x => Path.GetFileNameWithoutExtension(x.path)));
    }
}
#endif
