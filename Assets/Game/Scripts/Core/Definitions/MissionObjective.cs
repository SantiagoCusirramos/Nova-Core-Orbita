using System;
using UnityEngine;

[Serializable]
public class MissionObjective
{
    // Identificador unico del objetivo
    [SerializeField]
    private string objectiveId;

    // Descripcion que vera el jugador
    [SerializeField]
    [TextArea(2, 4)]
    private string description;

    // Accion necesaria para completar el objetivo
    [SerializeField]
    private MissionAction requiredAction;

    // Propiedades publicas de solo lectura
    public string ObjectiveId => objectiveId;

    public string Description => description;

    public MissionAction RequiredAction => requiredAction;
}