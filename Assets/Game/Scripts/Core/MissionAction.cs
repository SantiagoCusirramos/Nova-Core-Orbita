public enum MissionAction
{
    // ===================================
    // MISION 01: EMERGENCIA ENERGETICA
    // ===================================

    EmergencyActivated = 0,
    EnergyModuleCollected = 1,
    EnergyModuleInstalled = 2,
    RepairConfirmed = 3,

    // ===================================
    // MISION 02: REPARACION EXTERIOR (EVA)
    // ===================================

    SuitEquipped = 4,             // Equiparse con el traje
    SuitSystemsChecked = 5,       // Verificar sistemas del traje
    EVAPreparationCompleted = 6,  // Completar preparacion EVA
    AirlockDepressurized = 7,     // Despresurizar la esclusa
    SafetyTetherAttached = 8,     // Conectar sujecion de seguridad
    SolarPanelRepaired = 9,       // Reparar el panel solar
    ReturnedToAirlock = 10,       // Regresar a la esclusa
    AirlockRepressurized = 11     // Represurizar la esclusa
}