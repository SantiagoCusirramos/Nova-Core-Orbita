# CONTRATO DE INTEGRACIÓN CON CORE — NASA SPACE APPS VR

**Versión:** 1.0  
**Escena de integración:** `Assets/Game/Scenes/MissionVR.unity`  
**Responsable de Core y coordinación:** Luis  
**Estado:** contrato acordado; integración y pruebas de Unity pendientes.

## 1. Arquitectura acordada

- `MissionManager` administra dos misiones configuradas en el Inspector y valida **el orden de sus objetivos**.
- `MissionDefinition` y `MissionObjective` contienen la definición de cada misión y sus objetivos.
- `MissionAction` define las acciones que pueden notificarse al Core.
- `MissionActionBridge` es la entrada para las interacciones y minijuegos. Su versión actual expone `ExecuteAction()` (eventos del Inspector) y `TryExecuteAction()` (código, devuelve `bool`).
- **Solo una instancia activa de `MissionManager`** debe existir en la escena principal.
- `MissionActionBridge` puede dejar la referencia `Mission Manager` sin asignar, pues buscará `MissionManager.Instance` al ejecutarse. Esta dependencia debe existir y estar activa durante el juego.
- **Core NO valida por sí mismo** qué objeto fue manipulado ni si una animación, reparación o condición física sucedió. Es responsabilidad del componente interactivo validar la acción *antes* de notificarla.

```text
Jugador / controles VR
      ↓
Objeto interactivo o minijuego (valida su propio éxito)
      ↓
MissionActionBridge.TryExecuteAction() / ExecuteAction()
      ↓
MissionManager.TryCompleteAction(MissionAction)
      ↓
Avance del objetivo / cambio de misión / finalización
      ↓
Eventos de Core → UI, audio y otros sistemas
```

## 2. Misiones configuradas en Core

**Misión 1:** `Mission_Energy.asset` — `mission_energy` — 6 objetivos.  
**Misión 2:** `Mission_SolarPanel.asset` — `mission_solar` — 10 objetivos.

Orden de la lista `Missions` del `MissionManager` en `MissionVR.unity`:
1. `Mission_Energy`
2. `Mission_SolarPanel`

La Misión 2 arranca automáticamente después de aceptar el último objetivo de la Misión 1. **La finalización de la Misión 1 no emite `OnMissionCompleted`** cuando hay una segunda misión; emite `OnMissionChanged` al iniciar la siguiente.

## 3. Contrato de acciones: Misión 1

| Orden | `MissionAction` | Cuándo debe notificarse | Responsable principal |
|---|---|---|---|
| 1 | `EmergencyActivated` | La consola confirma activación del protocolo. | Yoshua |
| 2 | `EnergyModuleCollected` | El jugador recoge el repuesto correcto. | Yoshua, con controles de Santi |
| 3 | `EnergyModuleLoadedIntoHatch` | La compuerta acepta el repuesto correcto. | Yoshua |
| 4 | `RoboticConsoleActivated` | La consola entra correctamente al modo de control robótico. | Yoshua + Santi |
| 5 | `EnergyModuleInstalled` | El minijuego confirma extracción del módulo defectuoso, instalación segura del repuesto y comprobación final. | Yoshua |
| 6 | `RepairConfirmed` | El jugador confirma el diagnóstico de reparación desde la consola correspondiente. | Yoshua + Camila |

**Regla fundamental:** el minijuego robótico no debe enviar `EnergyModuleInstalled` al *iniciar*, sino **una sola vez después de completarse correctamente**. Sus pasos internos no deben convertirse en estados dentro de `MissionManager`.

## 4. Contrato de acciones: Misión 2

| Orden | `MissionAction` | Cuándo debe notificarse | Responsable principal |
|---|---|---|---|
| 1 | `SuitEquipped` | El traje correcto está equipado. | Yoshua + Santi |
| 2 | `SuitSystemsChecked` | Se completan los chequeos simulados del traje. | Yoshua |
| 3 | `EVAPreparationCompleted` | La preparación EVA se valida. | Yoshua |
| 4 | `AirlockDepressurized` | La esclusa confirma el estado requerido en la simulación. | Yoshua |
| 5 | `MMUActivated` | El sistema de propulsión personal está activado y listo. | Santi |
| 6 | `SolarPanelReached` | El jugador llega y se estabiliza en la zona correcta. | Santi + Yoshua |
| 7 | `SolarPanelRepaired` | El minijuego exterior confirma que se completaron sus pasos y comprobaciones. | Yoshua |
| 8 | `ReturnedToAirlock` | El jugador regresa correctamente a la esclusa. | Santi + Yoshua |
| 9 | `AirlockRepressurized` | La esclusa confirma la represurización simulada. | Yoshua |
| 10 | `StationPowerVerified` | El jugador confirma la estabilización energética desde el sistema previsto. | Yoshua + Camila |

**Nota de diseño:** el minijuego exterior todavía puede cambiar internamente. La acción final seguirá siendo `SolarPanelRepaired`, salvo acuerdo del equipo. `SafetyTetherAttached` existe en el enum, pero **no es un objetivo configurado** en la versión actual.

## 5. Uso de `MissionActionBridge`

1. Cada objeto interactivo debe tener su propio `MissionActionBridge` con la acción adecuada (el prefab `CoreActionBridge_Template` es una referencia reutilizable).
2. El responsable de la interacción valida localmente los requisitos (objeto correcto, posición correcta, secuencia completa, etc.).
3. Cuando todo sea válido, llama a `TryExecuteAction()` desde código o a `ExecuteAction()` desde un evento del Inspector.
4. Si `TryExecuteAction()` devuelve `false`, **Core no avanzó**; el sistema debe conservar o recuperar su estado de forma controlada.
5. Evitar llamadas duplicadas, especialmente si un minijuego dispara varios eventos de éxito o un botón se activa más de una vez.
6. No modificar `MissionManager` ni sus índices desde scripts de interacciones.

Ejemplo ilustrativo para un minijuego (Yoshua):

```csharp
using UnityEngine;

public class RepairCompletionExample : MonoBehaviour
{
    [SerializeField] private MissionActionBridge completionBridge;
    private bool reported;

    // Llamar solo después de que el propio minijuego compruebe el éxito.
    public void OnRepairReallyCompleted()
    {
        if (reported || completionBridge == null) return;
        reported = completionBridge.TryExecuteAction();
    }
}
```

Este es **un ejemplo de API**, no un script que deba añadirse directamente a producción. Cada minijuego controla cuándo sus requisitos son válidos.

## 6. Eventos de Core y responsabilidades de los receptores

| Evento | Cuándo se produce | Quién lo consume |
|---|---|---|
| `OnMissionStarted` | Se inicia la secuencia general. | UI / secuencia inicial, según diseño. |
| `OnMissionChanged(MissionDefinition)` | Se activa la primera misión o se cambia a la segunda. | Camila (título de misión), futuros sistemas narrativos. |
| `OnObjectiveChanged(MissionState, string)` | Cambia el objetivo activo, se inicia o se completa la secuencia, o se reinicia. | Camila (HUD/indicaciones). |
| `OnMissionCompleted` | Se completa la última misión configurada. | UI final y posibles cinemáticas futuras. |
| `OnMissionReset` | Se reinicia toda la secuencia lógica. | Todos los sistemas con estado físico. |

**Importante:** `ResetMission()` reinicia el progreso lógico, no la posición física de módulos, brazos, herramientas, MMU, traje, panel ni consola. Cada sistema es responsable de restaurarse al recibir `OnMissionReset`.

## 7. Responsabilidades y entregables

| Integrante | Entregable de integración |
|---|---|
| **Luis** | Core, dos `MissionDefinition` configuradas, secuencia de objetivos, revisión de acciones, pruebas y coordinación de escena. |
| **Santi** | Prefab XR/VR, locomoción, control de consola robótica, operación MMU y señales de llegada/estabilización que necesiten las interacciones. |
| **Yoshua** | Prefabs y scripts de consolas, compuerta, módulos, minijuegos y esclusa; llamados validados a `MissionActionBridge`. |
| **Diogo** | Entorno actualizado, modelos 3D, colliders, zonas de trabajo y puntos de anclaje; prefabs usables por los demás. |
| **Camila** | HUD de misión/objetivo y mensajes del Centro de Control suscritos a los eventos; ayudas, checklist y mensajes de éxito/error. |

## 8. Reglas de trabajo con el repositorio

- `MissionVR.unity` es la **escena oficial de integración**. La importación del nuevo modelo 3D está pendiente.
- Evitar que varias personas editen la misma escena simultáneamente; integrar en forma coordinada.
- Preferir **prefabs reutilizables** y escenas de prueba por área. No trasladar el código de Core a los minijuegos.
- Los cambios de `MissionAction` o eventos públicos deben coordinarse con Luis, pues pueden afectar referencias serializadas de Unity.
- No renumerar valores ya asignados del enum `MissionAction`.
- No dar por aprobada la integración hasta ejecutar las pruebas de Unity con ambas misiones.

## 9. Validación final (pospuesta hasta que el equipo integre sus sistemas)

- [ ] Unity compila el repositorio actual.
- [ ] `MissionVR.unity` contiene un solo `MissionManager`.
- [ ] Ambas misiones están asignadas en el orden correcto.
- [ ] Se aceptan las 16 acciones en su orden configurado.
- [ ] Se rechazan acciones incorrectas sin avanzar el objetivo.
- [ ] `EnergyModuleInstalled` no termina la Misión 1 por sí sola.
- [ ] `RepairConfirmed` inicia Misión 2 y emite `OnMissionChanged`.
- [ ] `SolarPanelRepaired` permite continuar el regreso EVA, no termina el juego.
- [ ] `StationPowerVerified` emite `OnMissionCompleted`.
- [ ] El reinicio restablece Core y todos los sistemas externos.
- [ ] La UI muestra objetivos/misiones correctos.

## 10. Cinemáticas (decisión pendiente)

Se han propuesto tres secuencias narrativas: introducción, revelación del daño exterior entre misiones y final. **No forman parte del alcance de código actual y no requieren cambiar este contrato todavía**. Si se aprueban, se coordinarán mediante un sistema externo de flujo narrativo, sin convertirlas en objetivos de Core.