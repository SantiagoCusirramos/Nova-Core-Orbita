public enum MissionAction
{
    // =====================================
    // MISION 1: MODULO ENERGETICO
    // =====================================

    EmergencyActivated = 0,
    EnergyModuleCollected = 1,
    EnergyModuleInstalled = 2,
    RepairConfirmed = 3,

    // =====================================
    // MISION 2: REPARACION EXTERIOR (EVA)
    // =====================================

    SuitEquipped = 4,
    SuitSystemsChecked = 5,
    EVAPreparationCompleted = 6,
    AirlockDepressurized = 7,
    SafetyTetherAttached = 8,
    SolarPanelRepaired = 9,
    ReturnedToAirlock = 10,
    AirlockRepressurized = 11,

    // =====================================
    // NUEVAS ACCIONES: MISION 1
    // =====================================

    EnergyModuleLoadedIntoHatch = 12,
    RoboticConsoleActivated = 13,

    // =====================================
    // NUEVAS ACCIONES: MISION 2
    // =====================================

    MMUActivated = 14,
    SolarPanelReached = 15,
    StationPowerVerified = 16
}