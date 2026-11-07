using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "NewMission",
    menuName = "SpaceApps/Missions/Mission Definition"
)]
public class MissionDefinition : ScriptableObject
{
    [Header("Informacion de la mision")]

    [SerializeField]
    private string missionId;

    [SerializeField]
    private string missionName;

    [SerializeField]
    [TextArea(2, 4)]
    private string description;

    [Header("Objetivos de la mision")]

    [SerializeField]
    private List<MissionObjective> objectives =
        new List<MissionObjective>();

    // Propiedades publicas de solo lectura
    public string MissionId => missionId;

    public string MissionName => missionName;

    public string Description => description;

    public IReadOnlyList<MissionObjective> Objectives => objectives;

    // Cantidad de objetivos
    public int ObjectiveCount => objectives.Count;
}