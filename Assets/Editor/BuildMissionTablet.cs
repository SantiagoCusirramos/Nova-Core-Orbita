using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Menu: Nova Core > Crear Tablet de Mision.
/// Arma el Canvas World Space (1600x1000), conecta todos los campos de MissionTabletUI,
/// crea StationTelemetry con las dos misiones y guarda el prefab en Assets/Game/Prefabs/UI.
/// </summary>
public static class BuildMissionTablet
{
    static readonly Color Bg = new Color(0.02f, 0.03f, 0.05f);
    static readonly Color Panel = new Color(0.05f, 0.075f, 0.11f);
    static readonly Color Line = new Color(0.16f, 0.23f, 0.30f);
    static readonly Color Amber = new Color(1f, 0.69f, 0f);
    static readonly Color Cyan = new Color(0.20f, 0.88f, 1f);
    static readonly Color Dim = new Color(0.55f, 0.64f, 0.71f);
    static readonly Color Green = new Color(0.24f, 1f, 0.55f);

    static Sprite sprite;

    [MenuItem("Nova Core/Importar TMP Essentials")]
    public static void ImportTmpEssentials()
    {
        const string pkg = "Packages/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage";
        if (Application.isBatchMode)
        {
            AssetDatabase.importPackageCompleted += _ => EditorApplication.Exit(0);
            AssetDatabase.importPackageFailed += (_, e) => { Debug.LogError(e); EditorApplication.Exit(1); };
        }
#pragma warning disable 0618 // ImportPackage sigue funcionando; evita el aviso amarillo en consola
        AssetDatabase.ImportPackage(pkg, false);
#pragma warning restore 0618
    }

    [MenuItem("Nova Core/Crear Tablet de Mision")]
    public static void Build()
    {
        if (TMP_Settings.instance == null || TMP_Settings.defaultFontAsset == null)
            Debug.LogWarning("TextMeshPro no tiene fuente por defecto. Usa Nova Core > Importar TMP Essentials y vuelve a ejecutar.");

        sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        // ---- Canvas raiz ----
        var root = new GameObject("MissionTablet", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        var rt = (RectTransform)root.transform;
        rt.sizeDelta = new Vector2(1600, 1000);
        rt.localScale = Vector3.one * 0.0006f;
        root.GetComponent<CanvasScaler>().dynamicPixelsPerUnit = 3f;

        var visual = Box(rt, "Visual", 0, 0, 1, 1, Bg);
        Border(visual);

        // ---- Encabezado ----
        var missionLabel = Txt(visual, "MissionLabel", "MISIÓN 1 DE 2", 30, Dim, TextAlignmentOptions.TopLeft, 0.04f, 0.92f, 0.6f, 0.97f);
        var missionName = Txt(visual, "MissionName", "Nombre de la misión", 54, Amber, TextAlignmentOptions.Left, 0.04f, 0.84f, 0.65f, 0.92f);
        Txt(visual, "TimeLabel", "TIEMPO RESTANTE", 30, Dim, TextAlignmentOptions.TopRight, 0.65f, 0.92f, 0.96f, 0.97f);
        var timeText = Txt(visual, "Time", "30:00", 80, Cyan, TextAlignmentOptions.Right, 0.65f, 0.82f, 0.96f, 0.93f);
        Box(visual, "HeaderLine", 0.04f, 0.815f, 0.96f, 0.82f, Line);

        // ---- Pestana ESTADO ----
        var pEstado = Group(visual, "Tab_Estado");
        var cardE = Box(pEstado, "EnergyCard", 0.04f, 0.17f, 0.42f, 0.80f, Panel); Border(cardE);
        Txt(cardE, "L", "ENERGÍA DE LA ESTACIÓN", 28, Dim, TextAlignmentOptions.TopLeft, 0.06f, 0.82f, 0.96f, 0.95f);
        var energyPct = Txt(cardE, "Pct", "15%", 130, Color.white, TextAlignmentOptions.Left, 0.06f, 0.52f, 0.96f, 0.80f);
        var barBg = Box(cardE, "BarBg", 0.06f, 0.36f, 0.94f, 0.46f, Color.black); Border(barBg);
        var fillGo = Box(barBg, "Fill", 0.01f, 0.1f, 0.99f, 0.9f, Green);
        var fill = fillGo.GetComponent<Image>();
        fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Horizontal; fill.fillOrigin = 0; fill.fillAmount = 0.15f;
        var energyState = Txt(cardE, "State", "CRÍTICA", 40, Dim, TextAlignmentOptions.Left, 0.06f, 0.18f, 0.96f, 0.32f);

        var cardO = Box(pEstado, "ObjectiveCard", 0.45f, 0.17f, 0.96f, 0.80f, Panel); Border(cardO);
        Txt(cardO, "L", "OBJETIVO ACTUAL", 28, Dim, TextAlignmentOptions.TopLeft, 0.05f, 0.85f, 0.95f, 0.95f);
        var curObj = Txt(cardO, "Current", "Objetivo", 56, Amber, TextAlignmentOptions.TopLeft, 0.05f, 0.30f, 0.95f, 0.84f);

        // ---- Pestana OBJETIVOS ----
        var pObj = Group(visual, "Tab_Objetivos");
        var cardL = Box(pObj, "ListCard", 0.04f, 0.17f, 0.96f, 0.80f, Panel); Border(cardL);
        var summary = Txt(cardL, "Summary", "", 28, Dim, TextAlignmentOptions.TopLeft, 0.04f, 0.90f, 0.96f, 0.98f);
        var rowTexts = new TMP_Text[4]; var rowTags = new TMP_Text[4];
        var rowIcons = new Image[4]; var rowFrames = new Image[4];
        for (int i = 0; i < 4; i++)
        {
            float top = 0.88f - i * 0.215f, bot = top - 0.195f;
            var row = Box(cardL, "Row" + i, 0.03f, bot, 0.97f, top, Panel);
            rowFrames[i] = row.GetComponent<Image>(); rowFrames[i].color = Line;
            var inner = Box(row, "Inner", 0.004f, 0.04f, 0.996f, 0.96f, Panel);
            // el texto debe colgar directamente de la fila (el script activa/desactiva su padre)
            rowIcons[i] = Box(row, "Icon", 0.025f, 0.28f, 0.065f, 0.72f, Dim).GetComponent<Image>();
            rowTexts[i] = Txt(row, "Text", "Objetivo", 40, Color.white, TextAlignmentOptions.Left, 0.09f, 0.05f, 0.76f, 0.95f);
            rowTags[i] = Txt(row, "Tag", "PENDIENTE", 26, Dim, TextAlignmentOptions.Right, 0.76f, 0.05f, 0.975f, 0.95f);
        }

        // ---- Pestana CONTROL ----
        var pCtl = Group(visual, "Tab_Control");
        var cardC = Box(pCtl, "ControlCard", 0.04f, 0.17f, 0.96f, 0.80f, Panel); Border(cardC);
        Txt(cardC, "L", "CENTRO DE CONTROL", 28, Dim, TextAlignmentOptions.TopLeft, 0.04f, 0.90f, 0.96f, 0.98f);
        var ctl = Txt(cardC, "Messages", "", 44, Color.white, TextAlignmentOptions.TopLeft, 0.04f, 0.05f, 0.96f, 0.88f);
        ctl.richText = true;

        // ---- Navegacion ----
        var tabsUi = root.AddComponent<MissionTabletUI>();
        string[] names = { "ESTADO", "OBJETIVOS", "CONTROL" };
        for (int i = 0; i < 3; i++)
        {
            float x0 = 0.04f + i * 0.32f;
            var b = Box(visual, "Nav_" + names[i], x0, 0.03f, x0 + 0.30f, 0.13f, Panel);
            Border(b);
            Txt(b, "T", names[i], 38, Cyan, TextAlignmentOptions.Center, 0, 0, 1, 1);
            var btn = b.AddComponent<Button>();
            UnityEventTools.AddIntPersistentListener(btn.onClick, tabsUi.ShowTab, i);
        }

        // ---- Telemetria (sin tocar el core) ----
        var telGo = new GameObject("StationTelemetry");
        telGo.transform.SetParent(root.transform, false);
        var tel = telGo.AddComponent<StationTelemetry>();
        var telSo = new SerializedObject(tel);
        var list = telSo.FindProperty("missions");
        string[] assets = { "Assets/Game/Data/Missions/Mission_Energy.asset", "Assets/Game/Data/Missions/Mission_SolarPanel.asset" };
        list.arraySize = assets.Length;
        for (int i = 0; i < assets.Length; i++)
            list.GetArrayElementAtIndex(i).objectReferenceValue = AssetDatabase.LoadAssetAtPath<MissionDefinition>(assets[i]);

        // Mensajes de ejemplo (EDITABLES en el Inspector). Marcados como provisionales.
        var msgs = new (string id, string text)[] {
            ("energy_01", "Eva, aquí Control. Detectamos una falla en la distribución eléctrica. Activa el protocolo de emergencia."),
            ("energy_02", "Protocolo activo. Localiza el módulo energético de repuesto."),
            ("energy_03", "Módulo en tu poder. Llévalo a la compuerta de mantenimiento."),
            ("energy_04", "Repuesto recibido. Activa la consola de control robótico."),
            ("energy_05", "Consola lista. Sustituye el módulo averiado con el brazo."),
            ("energy_06", "Instalación detectada. Confirma el diagnóstico de reparación."),
            ("solar_01", "Nueva alerta: daño en el panel solar. Equípate el traje."),
            ("solar_07", "Estás junto al panel. Procede con la reparación."),
            ("solar_10", "Confirma desde el sistema que la energía quedó estabilizada."),
        };
        var mp = telSo.FindProperty("messagesByObjective");
        mp.arraySize = msgs.Length;
        for (int i = 0; i < msgs.Length; i++)
        {
            var e = mp.GetArrayElementAtIndex(i);
            e.FindPropertyRelative("objectiveId").stringValue = msgs[i].id;
            e.FindPropertyRelative("text").stringValue = msgs[i].text;
        }
        telSo.ApplyModifiedPropertiesWithoutUndo();

        // ---- Conectar MissionTabletUI ----
        var so = new SerializedObject(tabsUi);
        Ref(so, "telemetry", tel);
        Ref(so, "missionLabel", missionLabel); Ref(so, "missionName", missionName); Ref(so, "timeText", timeText);
        Ref(so, "energyPercent", energyPct); Ref(so, "energyState", energyState); Ref(so, "energyFill", fill);
        Ref(so, "currentObjectiveText", curObj); Ref(so, "objectivesSummary", summary); Ref(so, "controlMessages", ctl);
        Arr(so, "rowTexts", rowTexts); Arr(so, "rowTags", rowTags); Arr(so, "rowIcons", rowIcons); Arr(so, "rowFrames", rowFrames);
        Arr(so, "tabPanels", new Object[] { pEstado, pObj, pCtl });
        so.ApplyModifiedPropertiesWithoutUndo();

        // ---- Ancla a la muneca ----
        var anchor = root.AddComponent<WristTabletAnchor>();
        var aso = new SerializedObject(anchor);
        Ref(aso, "visual", visual);
        aso.ApplyModifiedPropertiesWithoutUndo();

        // ---- Guardar prefab ----
        const string dir = "Assets/Game/Prefabs/UI";
        if (!AssetDatabase.IsValidFolder(dir)) AssetDatabase.CreateFolder("Assets/Game/Prefabs", "UI");
        string path = dir + "/MissionTablet.prefab";
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        Debug.Log("Tablet creada: " + path + ". Arrastrala a la escena MissionVR (necesita un MissionManager activo).");
    }

    // ---------- helpers ----------
    static void Ref(SerializedObject so, string prop, Object o) { so.FindProperty(prop).objectReferenceValue = o; }

    static void Arr(SerializedObject so, string prop, Object[] a)
    {
        var p = so.FindProperty(prop); p.arraySize = a.Length;
        for (int i = 0; i < a.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = a[i];
    }

    static GameObject Group(GameObject parent, string name)
    {
        var g = new GameObject(name, typeof(RectTransform));
        g.transform.SetParent(parent.transform, false);
        Stretch((RectTransform)g.transform, 0, 0, 1, 1);
        return g;
    }

    static GameObject Box(Transform parent, string name, float x0, float y0, float x1, float y1, Color c)
        => Box(parent.gameObject, name, x0, y0, x1, y1, c);

    static GameObject Box(GameObject parent, string name, float x0, float y0, float x1, float y1, Color c)
    {
        var g = new GameObject(name, typeof(RectTransform), typeof(Image));
        g.transform.SetParent(parent.transform, false);
        Stretch((RectTransform)g.transform, x0, y0, x1, y1);
        var im = g.GetComponent<Image>(); im.sprite = sprite; im.type = Image.Type.Sliced; im.color = c;
        return g;
    }

    static void Border(GameObject g)
    {
        var o = g.AddComponent<Outline>();
        o.effectColor = Line; o.effectDistance = new Vector2(3, -3);
    }

    static TMP_Text Txt(GameObject parent, string name, string text, float size, Color c, TextAlignmentOptions al,
                        float x0, float y0, float x1, float y1)
    {
        var g = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        g.transform.SetParent(parent.transform, false);
        Stretch((RectTransform)g.transform, x0, y0, x1, y1);
        var t = g.GetComponent<TextMeshProUGUI>();
        t.text = text; t.fontSize = size; t.color = c; t.alignment = al;
        t.textWrappingMode = TextWrappingModes.Normal; t.overflowMode = TextOverflowModes.Ellipsis;
        return t;
    }

    static void Stretch(RectTransform r, float x0, float y0, float x1, float y1)
    {
        r.anchorMin = new Vector2(x0, y0); r.anchorMax = new Vector2(x1, y1);
        r.offsetMin = r.offsetMax = Vector2.zero;
    }
}
