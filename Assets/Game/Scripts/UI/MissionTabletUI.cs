using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Tablet de Eva (Canvas World Space). Solo lee MissionManager y StationTelemetry.
/// Se actualiza por eventos del Core; el reloj se refresca en Update (solo cuando cambia el segundo).
/// </summary>
public class MissionTabletUI : MonoBehaviour
{
    private enum RowState { Pending, Current, Done }

    [Header("Fuentes de datos")]
    [SerializeField] private MissionManager missionManager;   // vacio = MissionManager.Instance
    [SerializeField] private StationTelemetry telemetry;      // opcional

    [Header("Encabezado")]
    [SerializeField] private TMP_Text missionLabel;   // "MISION 1 DE 2"
    [SerializeField] private TMP_Text missionName;
    [SerializeField] private TMP_Text timeText;
    [SerializeField] private int totalMissions = 2;

    [Header("Pestana ESTADO")]
    [SerializeField] private TMP_Text energyPercent;
    [SerializeField] private TMP_Text energyState;
    [SerializeField] private Image energyFill;            // Image Type = Filled, Horizontal
    [SerializeField] private TMP_Text currentObjectiveText;

    [Header("Pestana OBJETIVOS (4 filas)")]
    [SerializeField] private TMP_Text[] rowTexts = new TMP_Text[4];
    [SerializeField] private TMP_Text[] rowTags = new TMP_Text[4];
    [SerializeField] private Image[] rowIcons = new Image[4];
    [SerializeField] private Image[] rowFrames = new Image[4];
    [SerializeField] private TMP_Text objectivesSummary;

    [Header("Pestana CONTROL")]
    [SerializeField] private TMP_Text controlMessages;

    [Header("Pestanas")]
    [SerializeField] private GameObject[] tabPanels = new GameObject[3];  // Estado, Objetivos, Control

    [Header("Colores")]
    [SerializeField] private Color amber = new Color(1f, 0.69f, 0f);
    [SerializeField] private Color green = new Color(0.24f, 1f, 0.55f);
    [SerializeField] private Color red = new Color(1f, 0.30f, 0.30f);
    [SerializeField] private Color dim = new Color(0.55f, 0.64f, 0.71f);
    [SerializeField] private Color cyan = new Color(0.20f, 0.88f, 1f);

    private MissionManager manager;
    private int lastSecond = -1;

    private void OnEnable()
    {
        Bind();
        Redraw();
    }

    private void Start()
    {
        Bind();       // por si el MissionManager se creo despues de OnEnable
        ShowTab(0);
        Redraw();
    }

    private void Bind()
    {
        if (manager != null) return;
        manager = missionManager != null ? missionManager : MissionManager.Instance;
        if (manager == null) return;

        manager.OnMissionChanged += OnMissionChanged;
        manager.OnObjectiveChanged += OnObjectiveChanged;
        manager.OnMissionCompleted += Redraw;
        manager.OnMissionReset += Redraw;

        if (telemetry != null) telemetry.OnMessagesChanged += Redraw;
    }

    private void OnDisable()
    {
        if (manager != null)
        {
            manager.OnMissionChanged -= OnMissionChanged;
            manager.OnObjectiveChanged -= OnObjectiveChanged;
            manager.OnMissionCompleted -= Redraw;
            manager.OnMissionReset -= Redraw;
            manager = null;
        }
        if (telemetry != null) telemetry.OnMessagesChanged -= Redraw;
    }

    private void OnMissionChanged(MissionDefinition m) { Redraw(); }
    private void OnObjectiveChanged(MissionState s, string t) { Redraw(); }

    private void Update()
    {
        if (manager == null) { Bind(); return; }
        if (telemetry == null || timeText == null) return;

        int sec = Mathf.CeilToInt(telemetry.TimeRemaining);
        if (sec == lastSecond) return;
        lastSecond = sec;
        DrawTelemetry();
    }

    /// <summary>Cambia de pestana (0 Estado, 1 Objetivos, 2 Control). Enlazable a botones o a un input de mando.</summary>
    public void ShowTab(int index)
    {
        for (int i = 0; i < tabPanels.Length; i++)
            if (tabPanels[i] != null) tabPanels[i].SetActive(i == index);
    }

    public void NextTab()
    {
        for (int i = 0; i < tabPanels.Length; i++)
        {
            if (tabPanels[i] != null && tabPanels[i].activeSelf) { ShowTab((i + 1) % tabPanels.Length); return; }
        }
        ShowTab(0);
    }

    private void Redraw()
    {
        if (manager == null) return;

        MissionState state = manager.CurrentState;
        MissionDefinition mission = manager.CurrentMission;

        // Encabezado
        if (state == MissionState.NotStarted)
        {
            Set(missionLabel, "EN ESPERA");
            Set(missionName, "Esperando inicio de misión");
        }
        else if (state == MissionState.Completed)
        {
            Set(missionLabel, "SECUENCIA COMPLETA");
            Set(missionName, "Estación estabilizada");
        }
        else if (mission != null)
        {
            Set(missionLabel, "MISIÓN " + (manager.CurrentMissionIndex + 1) + " DE " + totalMissions);
            Set(missionName, mission.MissionName);
        }

        // Objetivo actual (texto ya resuelto por el Core)
        Set(currentObjectiveText, manager.CurrentObjective);

        DrawObjectives(state, mission);
        DrawTelemetry();
    }

    private void DrawObjectives(MissionState state, MissionDefinition mission)
    {
        int rows = rowTexts.Length;

        if (mission == null || state != MissionState.InProgress)
        {
            for (int i = 0; i < rows; i++) SetRow(i, null, RowState.Pending, false);
            Set(objectivesSummary, state == MissionState.Completed ? "TODOS LOS OBJETIVOS COMPLETADOS" : "");
            return;
        }

        int count = mission.ObjectiveCount;
        int current = manager.CurrentObjectiveIndex;

        // Ventana de 4 filas centrada en el objetivo actual (pocos elementos por pantalla)
        int start = Mathf.Clamp(current - 1, 0, Mathf.Max(0, count - rows));

        for (int r = 0; r < rows; r++)
        {
            int i = start + r;
            if (i >= count) { SetRow(r, null, RowState.Pending, false); continue; }

            RowState rs = i < current ? RowState.Done : i == current ? RowState.Current : RowState.Pending;
            SetRow(r, mission.Objectives[i].Description, rs, false);
        }

        Set(objectivesSummary, count + " OBJETIVOS · " + current + " COMPLETADOS");
    }

    private void SetRow(int r, string text, RowState rs, bool allDone)
    {
        bool visible = text != null || allDone;
        if (rowTexts[r] != null) rowTexts[r].gameObject.transform.parent.gameObject.SetActive(visible);
        if (!visible) return;

        if (allDone) rs = RowState.Done;

        Color c = rs == RowState.Done ? green : rs == RowState.Current ? amber : dim;
        string tag = rs == RowState.Done ? "COMPLETADO" : rs == RowState.Current ? "EN CURSO" : "PENDIENTE";

        if (rowTexts[r] != null) { rowTexts[r].text = text ?? ""; rowTexts[r].color = rs == RowState.Done ? dim : Color.white; }
        if (rowTags[r] != null) { rowTags[r].text = tag; rowTags[r].color = c; }
        if (rowIcons[r] != null) rowIcons[r].color = rs == RowState.Pending ? dim : c;
        if (rowFrames[r] != null) rowFrames[r].color = rs == RowState.Current ? amber : new Color(0.16f, 0.23f, 0.30f);
    }

    private void DrawTelemetry()
    {
        if (telemetry == null) return;

        // Tiempo
        int sec = Mathf.CeilToInt(telemetry.TimeRemaining);
        Set(timeText, (sec / 60).ToString("00") + ":" + (sec % 60).ToString("00"));
        if (timeText != null) timeText.color = sec < 300 ? red : cyan;

        // Energia
        float e = telemetry.Energy01;
        Set(energyPercent, Mathf.RoundToInt(e * 100f) + "%");
        if (energyFill != null)
        {
            energyFill.fillAmount = e;
            energyFill.color = e < 0.3f ? red : e < 0.7f ? amber : green;
        }
        Set(energyState, e < 0.3f ? "CRÍTICA" : e < 0.7f ? "INESTABLE" : "ESTABLE");

        // Mensajes de Control
        if (controlMessages != null)
        {
            var sb = new System.Text.StringBuilder();
            var list = telemetry.Messages;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                sb.Append("<color=#34E0FF><size=60%>CONTROL</size></color>\n").Append(list[i]).Append("\n\n");
            }
            controlMessages.text = sb.ToString();
        }
    }

    private static void Set(TMP_Text t, string s) { if (t != null) t.text = s; }
}
