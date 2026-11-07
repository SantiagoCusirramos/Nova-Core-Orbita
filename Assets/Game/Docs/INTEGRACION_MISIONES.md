# NASA SPACE APPS
# DOCUMENTACIÓN DE INTEGRACIÓN - V0.4

## 1. Objetivo

Implementar un sistema escalable de misiones espaciales
para un videojuego desarrollado en Unity 3D con VR.

El jugador despierta en una estación espacial durante
una emergencia y debe completar una secuencia de
misiones para estabilizar sus sistemas.

La arquitectura permite agregar misiones y objetivos
sin modificar la lógica principal de MissionManager.

## 2. Organización del proyecto

Assets/Game/
├── Scripts/
│   ├── Core/
│   │   ├── MissionManager.cs
│   │   ├── MissionState.cs
│   │   ├── MissionAction.cs
│   │   ├── MissionActionBridge.cs
│   │   └── Definitions/
│   │       ├── MissionDefinition.cs
│   │       └── MissionObjective.cs
│   ├── VR/
│   ├── Interaction/
│   └── UI/
├── Data/
│   └── Missions/
├── Scenes/
│   └── MissionVR.unity
├── Prefabs/
├── Docs/
└── Tests/
    └── MissionDebugTester.cs

SpaceStation5 contiene el escenario original.
Game contiene los nuevos sistemas y mecánicas.

## 3. Arquitectura de misiones

MissionDefinition:
Define una misión mediante un ScriptableObject.
Contiene identificador, nombre, descripción y objetivos.

MissionObjective:
Define un objetivo individual mediante:
- ObjectiveId
- Description
- RequiredAction

MissionManager:
Gestiona una lista ordenada de MissionDefinition.
Valida las acciones y controla el progreso.

MissionActionBridge:
Comunica las interacciones al MissionManager.

MissionDebugTester:
Permite simular acciones sin controles VR.

## 4. Estados generales

MissionState utiliza:

- NotStarted: secuencia sin iniciar.
- InProgress: secuencia en ejecución.
- Completed: todas las misiones completadas.

Los estados específicos de la versión anterior
se conservan temporalmente hasta finalizar
la migración de todos los scripts.

## 5. Métodos públicos

StartMission()
Inicia la primera misión configurada.

TryCompleteAction(MissionAction action)
Verifica si la acción corresponde al objetivo actual.
Devuelve true si se acepta y false si se rechaza.

ResetMission()
Reinicia toda la secuencia de misiones.

## 6. Propiedades públicas

CurrentState
Estado general de la secuencia.

CurrentMission
Definición de la misión actual.

CurrentMissionObjective
Objetivo actualmente activo.

CurrentMissionIndex
Índice de la misión actual, comenzando en 0.

CurrentObjectiveIndex
Índice del objetivo actual, comenzando en 0.

CurrentObjective
Descripción del objetivo que debe mostrar la interfaz.

## 7. Eventos públicos

OnMissionStarted
Se emite al iniciar la secuencia de misiones.

OnObjectiveChanged
Se emite cuando cambia el objetivo o el estado
general notificado.

OnMissionChanged
Se emite cuando comienza una nueva misión o fase.

OnMissionCompleted
Se emite al completar TODA la secuencia,
no cada misión individual.

OnMissionReset
Se emite al reiniciar la secuencia.

## 8. Integración por responsable

### Luis - Core y coordinación

- Mantener MissionManager.
- Configurar la secuencia de misiones.
- Validar el orden de objetivos.
- Verificar los eventos.
- Coordinar la integración del equipo.

### Santi - Sistema VR

- Configurar XR Origin.
- Implementar locomoción y teleportación.
- Configurar controladores VR.
- Permitir interacciones con objetos.
- No modificar MissionManager.

### Yoshua - Interacciones

- Implementar consolas, botones y objetos.
- Utilizar MissionActionBridge.
- Configurar MissionAction en cada interacción.
- Llamar ExecuteAction() al completar una acción.
- Restaurar objetos al reiniciar la misión.
- Evitar enviar acciones desde objetos incorrectos.

Las interacciones no deben modificar directamente
los estados o índices de MissionManager.

### Diogo - Escenario 3D

- Preparar las zonas de la estación espacial.
- Ubicar consolas y componentes interactivos.
- Verificar colisiones y espacios navegables.
- Mantener las referencias al cambiar modelos 3D.
- Coordinar modificaciones de MissionVR.unity.

### Camila - Interfaz y pruebas

- Mostrar CurrentObjective.
- Mostrar el nombre de CurrentMission.
- Escuchar OnObjectiveChanged.
- Escuchar OnMissionChanged.
- Mostrar la victoria mediante OnMissionCompleted.
- Implementar los botones de inicio y reinicio.
- Registrar resultados de las pruebas.

Los componentes que utilicen eventos deberán
suscribirse y desuscribirse correctamente.

## 9. Misiones previstas

### Misión 01: Emergencia energética (MVP)

1. EmergencyActivated
2. EnergyModuleCollected
3. EnergyModuleInstalled
4. RepairConfirmed

### Misión 02: Reparación exterior (opcional)

1. SuitEquipped
2. SuitSystemsChecked
3. EVAPreparationCompleted
4. AirlockDepressurized
5. SafetyTetherAttached
6. SolarPanelRepaired
7. ReturnedToAirlock
8. AirlockRepressurized

La misión EVA será una simulación educativa
simplificada, no un procedimiento operativo real.

## 10. Reglas de escalabilidad

- Las misiones se configurarán con ScriptableObjects.
- No escribir transiciones específicas en MissionManager.
- Cada MissionDefinition tendrá una lista ordenada.
- Cada objetivo tendrá un identificador único
  dentro de su misión.
- Mantener los valores numéricos de MissionAction
  para evitar incompatibilidades con datos guardados.
- Nuevas acciones deben coordinarse con Yoshua.
- Los cambios de eventos deben comunicarse a Camila.
- El código no debe depender de nombres de modelos 3D.

## 11. Configuración pendiente en Unity

Cuando Unity compile correctamente:

1. Crear Mission_Energy.asset.
2. Configurar sus cuatro objetivos.
3. Añadir el asset a MissionManager.
4. Crear MissionManager en la escena.
5. Configurar MissionDebugTester.
6. Ejecutar pruebas funcionales.
7. Integrar las interacciones y la interfaz.
8. Probar la secuencia completa.

## 12. Limitaciones actuales

- Unity continúa pendiente de solucionar
  los errores de compilación relacionados con VR.
- Los ScriptableObjects todavía no están configurados.
- El código refactorizado todavía no se ha probado.
- ResetMission no restaura físicamente los objetos.
- Las misiones actuales son secuenciales.
- No se soportan objetivos paralelos.
- El sistema valida acciones por tipo, pero no
  verifica por sí mismo qué objeto las originó.
- La misión EVA es una ampliación opcional.

## 13. Estado del proyecto

Arquitectura de Core: preparada.
Refactorización de MissionManager: realizada.
MissionDebugTester: actualizado.
Acciones EVA: declaradas.
Documentación: actualizada a V0.4.

Pendiente:
- Validación de compilación.
- Configuración de MissionDefinition.asset.
- Pruebas de Core.
- Pruebas de integración.
- Validación de la experiencia VR.