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

# 14. INTEGRACIÓN DE LAS DOS MISIONES - V0.5

## 14.1. Organización general

El videojuego tendrá dos misiones consecutivas:

Misión 1:
Sustitución robótica del módulo energético.

Misión 2:
Reparación exterior del panel solar.

MissionManager controla el orden de los objetivos.

Los minijuegos funcionan de manera independiente
y notifican a Core cuando terminan correctamente.

## 14.2. Integración de la Misión 1

Archivo de configuración previsto:
Mission_Energy.asset

Secuencia de acciones:

1. EmergencyActivated
2. EnergyModuleCollected
3. EnergyModuleLoadedIntoHatch
4. RoboticConsoleActivated
5. EnergyModuleInstalled
6. RepairConfirmed

Responsabilidades:

Luis:
- Configurar los objetivos.
- Validar las acciones recibidas.
- Comprobar el cambio a la segunda misión.

Santi:
- Configurar los controles VR.
- Implementar el modo de operación robótica.
- Desactivar temporalmente la locomoción.
- Mantener libre el movimiento de la cabeza.

Yoshua:
- Programar las interacciones del módulo.
- Programar la compuerta.
- Programar los brazos mecánicos.
- Implementar el minijuego robótico.
- Validar la finalización del minijuego.

Diogo:
- Preparar los modelos 3D.
- Organizar el compartimiento de mantenimiento.
- Preparar las zonas de interacción.

Camila:
- Mostrar instrucciones del Centro de Control.
- Implementar interfaz de operación robótica.
- Mostrar indicadores y mensajes de error.

IMPORTANTE:

EnergyModuleInstalled solo debe enviarse
cuando el minijuego robótico termine correctamente.

Los pasos internos del minijuego no deben
modificar directamente MissionManager.

## 14.3. Integración de la Misión 2

Archivo de configuración previsto:
Mission_SolarPanel.asset

Secuencia de acciones:

1. SuitEquipped
2. SuitSystemsChecked
3. EVAPreparationCompleted
4. AirlockDepressurized
5. MMUActivated
6. SolarPanelReached
7. SolarPanelRepaired
8. ReturnedToAirlock
9. AirlockRepressurized
10. StationPowerVerified

Responsabilidades:

Luis:
- Configurar los objetivos.
- Validar el progreso de la misión.
- Comprobar la finalización del videojuego.

Santi:
- Programar el movimiento del MMU.
- Implementar desplazamiento y estabilización.
- Mantener controles VR cómodos.

Yoshua:
- Programar las interacciones del traje.
- Implementar mecanismos de la esclusa.
- Programar el minijuego del panel solar.
- Validar las condiciones de reparación.

Diogo:
- Preparar el escenario exterior.
- Crear modelos del MMU y panel solar.
- Preparar herramientas y puntos de anclaje.

Camila:
- Crear instrucciones EVA.
- Mostrar indicadores del MMU.
- Preparar interfaz del minijuego.
- Mostrar confirmaciones y advertencias.

IMPORTANTE:

SolarPanelReached solo debe enviarse
cuando el jugador llegue y esté estabilizado.

SolarPanelRepaired solo debe enviarse
al completar correctamente el minijuego.

El diseño definitivo del minijuego exterior
todavía está pendiente de confirmación.

## 14.4. Contrato de los minijuegos con Core

Cada minijuego debe:

1. Comprobar que puede comenzar.
2. Mantener sus pasos internos.
3. Validar la finalización de cada etapa.
4. Permitir corregir errores.
5. Notificar a Core al finalizar.
6. Restaurarse cuando se reinicie la misión.

Para completar un objetivo se utilizará:

MissionActionBridge.ExecuteAction()

Cada puente tendrá configurada la acción
correspondiente en el Inspector.

No debe enviarse una acción al iniciar
el minijuego, sino al completarlo correctamente.

## 14.5. Eventos utilizados

OnMissionStarted:
Inicio de toda la secuencia del videojuego.

OnMissionChanged:
Comienza la primera misión o cambia de fase.

OnObjectiveChanged:
El objetivo activo cambia.

OnMissionCompleted:
Se completa la última misión configurada.

OnMissionReset:
Se solicita restaurar el estado de los sistemas.

IMPORTANTE:

OnMissionCompleted no se ejecuta al terminar
la Misión 1 si existe una segunda misión.

El cambio se notifica mediante OnMissionChanged.

## 14.6. Reinicio y recuperación

Cuando se ejecute ResetMission():

- Core regresa a NotStarted.
- Los índices se reinician.
- Se actualiza la interfaz.
- Se emite OnMissionReset.

Cada sistema externo deberá restaurar
sus propios objetos.

Ejemplos:

- Devolver el módulo energético.
- Restaurar la compuerta.
- Reiniciar el brazo mecánico.
- Restaurar el panel solar.
- Restablecer el estado del MMU.
- Restaurar las pantallas de los minijuegos.

MissionManager no realiza estas operaciones
físicas directamente.

## 14.7. Reglas para los desarrolladores

- No modificar MissionManager sin coordinación.
- No alterar directamente los índices de Core.
- No enviar acciones antes de validar el objeto.
- No completar objetivos desde objetos incorrectos.
- No duplicar MissionManager en la escena.
- No bloquear la rotación natural del visor VR.
- Mantener los minijuegos separados de Core.
- Probar cada sistema de manera independiente.
- Documentar los eventos utilizados.

## 14.8. Estado de implementación

Core:
Código preparado, pendiente de compilación
y pruebas en Unity.

Misión 1:
Objetivos definidos.
ScriptableObject pendiente de configurar.
Minijuego robótico pendiente de desarrollar.

Misión 2:
Objetivos definidos.
ScriptableObject pendiente de configurar.
Minijuego exterior pendiente de decisión final.

Integración:
Pendiente de resolver los errores de Unity
y conectar los componentes del equipo.