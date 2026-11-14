# Plan ligero — Misiones e interiores emulados de la ISS

**Última actualización:** 2026-11-14
**Proyecto:** Nova Core Órbita (`C:\Users\fdx_dracox\Documents\Projects\NovaCoreOrbita\Nova-Core-Orbita`)
**Objetivo:** (A) definir un **planteamiento ligero** para conectar el Core de misiones a gameplay
real, y (B) planificar **módulos/salas pequeñas emuladas** dentro de la ISS actual (que es sólo
exterior).

> Documentos hermanos:
> - Inventario de `NASA-SPACE-APPS`: `contexto-proyecto/REFERENCIA_NASA_SPACE_APPS.md`
> - Backlog consolidado: `contexto-proyecto/TAREAS_PENDIENTES.md`
> - Estado real de Nova: `contexto-proyecto/ESTADO_ACTUAL_Y_TAREAS.md`

---

## Parte A — Planteamiento ligero de misiones

### A.1. Punto de partida (lo que ya existe en Nova)

El Core está **completo y compilando** en Nova:

| Pieza | Ruta |
|---|---|
| `MissionManager` | `Assets/Game/Scripts/Core/MissionManager.cs` |
| `MissionAction` (enum) | `Assets/Game/Scripts/Core/MissionAction.cs` |
| `MissionActionBridge` | `Assets/Game/Scripts/Core/MissionActionBridge.cs` |
| `MissionDefinition` | `Assets/Game/Scripts/Core/Definitions/MissionDefinition.cs` |
| `MissionObjective` | `Assets/Game/Scripts/Core/Definitions/MissionObjective.cs` |
| Misión 1 | `Assets/Game/Data/Missions/Mission_Energy.asset` (6 objetivos) |
| Misión 2 | `Assets/Game/Data/Missions/Mission_SolarPanel.asset` (10 objetivos) |

**Lo que falta:** objetos de escena que, al interactuar, llamen a
`MissionActionBridge.TryExecuteAction()`. Hoy el Core **no está en `01_Orbit`**; sólo en la escena
legacy `MissionVR` (que no está en Build Settings).

### A.2. Reglas que NO se pueden romper

1. **El enum `MissionAction` no se renumera.** Valores en uso: `0`–`16` (ver tabla A.4).
2. **El avance es secuencial**: `MissionManager` sólo acepta la acción del objetivo actual.
   Ejecutar una acción fuera de orden no avanza (y `TryExecuteAction()` devuelve `false`).
3. **Una sola instancia de `MissionManager`** activa por escena.
4. **`ResetMission()` no restaura objetos físicos**: si un objetivo implica mover/instalar algo,
   el reset hay que gestionarlo aparte.
5. `SafetyTetherAttached` (8) está definido pero **sin usar** en las misiones actuales.

### A.3. Cómo dispara NASA las acciones vs. cómo debe hacerlo Nova

| Aspecto | `NASA-SPACE-APPS` | Nova (planteamiento ligero) |
|---|---|---|
| Entrada | `SpaceToolInteractable` con XRI (grab + trigger) | **Holder propio** sobre `InputDevices` + `MissionActionBridge` |
| Minijuego | `SpaceRepairStation.TryApply*` | **Misión 1:** brazo/sonda robótico (nuevo). **Misión 2 (panel):** reutilizar la lógica tipo welder de `SpaceRepairStation` |
| Disparo | Llamada interna al completar | `MissionActionBridge.TryExecuteAction()` al 100 % del minijuego |
| Dependencia | XRI 3.3.2 | **Sin XRI** (decisión cerrada) |

> ⚠️ **Corrección respecto al documento oficial** (`Nasa_SpaceApps_Documentacion.pdf` §6.2):
> el minijuego de la **Misión 1** es un **brazo/sonda robótico operado desde una consola**, **no** una
> mesa física de reparación. La `SpaceRepairStation` de `NASA-SPACE-APPS` sirve para el
> **minijuego del panel (EVA, Misión 2)**, no para el robótico.

**Estrategia ligera recomendada:** portar `SpaceRepairStation` **desacoplada** (sin el tipo
`SpaceToolInteractable`) y añadir un pequeño adaptador `MissionActionBridge` en la mesa que, al
completar el minijuego, dispare la acción. Así no se instala XRI y se reutiliza el 80 % del trabajo
de NASA.

### A.4. Mapa completo de acciones (0–16) y dónde ocurren

| Valor | Acción | Misión | Dónde ocurre (planteamiento ligero) |
|:--:|---|---|---|
| 0 | `EmergencyActivated` | Energía | Consola de emergencia (módulo nodo) |
| 1 | `EnergyModuleCollected` | Energía | Módulo de repuesto en laboratorio |
| 12 | `EnergyModuleLoadedIntoHatch` | Energía | Compuerta de mantenimiento |
| 13 | `RoboticConsoleActivated` | Energía | Consola robótica |
| 2 | `EnergyModuleInstalled` | Energía | **Minijuego robótico** (brazo/sonda desde consola) — **nuevo** |
| 3 | `RepairConfirmed` | Energía | Consola: diagnóstico/confirmación final — **nuevo** |
| 4 | `SuitEquipped` | EVA | Esclusa: armario de traje |
| 5 | `SuitSystemsChecked` | EVA | Esclusa: consola de chequeo |
| 6 | `EVAPreparationCompleted` | EVA | Esclusa: cierre de ciclo |
| 7 | `AirlockDepressurized` | EVA | Esclusa: panel de despresurización |
| 8 | `SafetyTetherAttached` | EVA | *(sin usar)* gancho de amarre |
| 9 | `SolarPanelRepaired` | EVA | Exterior: **minijuego del panel** (base: welder/drill) |
| 10 | `ReturnedToAirlock` | EVA | Esclusa: trigger de retorno |
| 11 | `AirlockRepressurized` | EVA | Esclusa: sellado/presión (base: sealant) |
| 14 | `MMUActivated` | EVA | Unidad de maniobra |
| 15 | `SolarPanelReached` | EVA | Exterior: trigger de llegada |
| 16 | `StationPowerVerified` | EVA | Consola de verificación final |

> **Mapeo corregido según el documento oficial** (`Nasa_SpaceApps_Documentacion.pdf`):
> - **Misión 1 (robótico):** [2] `EnergyModuleInstalled` ← brazo/sonda desde consola (**nuevo**);
>   [3] `RepairConfirmed` ← confirmación en consola (**nuevo**). **No** usa las mesas de `SpaceRepairStation`.
> - **Misión 2 (EVA):** [9] `SolarPanelRepaired` ← minijuego del panel, base **welder/drill**;
>   [11] `AirlockRepressurized` ← sellado/presión, base **sealant**.
> Ver `REFERENCIA_NASA_SPACE_APPS.md` §2.2 y §6.4.

### A.5. Flujo propuesto

```
MISIÓN 1 — ENERGÍA (interior)                 MISIÓN 2 — EVA (interior → exterior)
[0] consola emergencia                          [4] armario traje
   → [1] recoger módulo                           → [5] consola chequeo
   → [12] meter en compuerta                       → [6] cerrar preparación
   → [13] consola robótica                         → [7] despresurizar
   → [2] brazo robótico: instalar módulo           → (8 tether, opcional)
   → [3] confirmar reparación en consola           → [15] llegar al panel
                                                    → [9] welder: reparar panel
                                                    → [10] volver a esclusa
                                                    → [11] sealant: represurizar
                                                    → [14] MMU
                                                    → [16] verificar energía
```

**Planteamiento ligero:** no se inventan misiones nuevas. Se implementan **las 2 que ya existen**,
cada una en su zona, con el mínimo de objetos interactivos. Nada de sistemas genéricos de inventario.

### A.6. Qué implementar (mínimo viable por objetivo)

| Tipo de objetivo | Implementación mínima | Script |
|---|---|---|
| "Activar/verificar consola" | `Collider` + botón → `MissionActionBridge` | `MissionActionBridge` (ya existe) |
| "Recoger/instalar objeto" | Objeto con `Rigidbody` + trigger de zona → bridge | Nuevo `MissionPickup.cs` |
| "Minijuego de reparación" | `SpaceRepairStation` adaptada → bridge al 100 % | `SpaceRepairStation` (portada) |
| "Llegar a zona" | Trigger de volumen → bridge | `MissionActionBridge` sobre trigger |

### A.7. Escena de misiones

Se recomienda **una escena de interior separada** (no meter misiones en `01_Orbit`, que es la demo
exterior verificada). Opciones:

- Reutilizar `Assets/Game/Scenes/MissionVR.unity` (legacy) reconstruida con los módulos nuevos, **o**
- Crear `Assets/Game/Scenes/02_Interior.unity`.

`01_Orbit` se queda como está (decisión cerrada).

---

## Parte B — Interiores emulados de la ISS

### B.1. El problema

`Assets/NovaCore/Art/ISS/ISS_stationary.glb` es **sólo exterior** (112 × 69 × 59 m). No trae
interiores, y los interiores de `NASA-SPACE-APPS` son una estación **ficticia de 1 km**, no la ISS.
Por tanto **no se pueden "exportar" los interiores de NASA**: hay que **emular** módulos pequeños
coherentes con la estructura de la ISS real.

### B.2. Estrategia recomendada: módulos emulados (no portar `StationArchitect`)

- **Unidad de construcción:** un módulo cilíndrico de ~4,2 m de diámetro interior, ~2,2 m de altura
  libre y 6–11 m de largo, como los módulos reales de la ISS.
- **Técnica:** versión mínima de `StationMeshBuilder.CreateRoomMeshWithDoorway(...)` (ver
  `REFERENCIA_NASA_SPACE_APPS.md` §2.4), escalada a tamaño ISS.
- **Escala:** **1 unidad = 1 metro** (regla cerrada de Nova).
- **Anclaje:** cada módulo se coloca sobre la estructura exterior existente o en una escena de
  interior independiente; no se mezcla con `01_Orbit`.

### B.3. Módulos propuestos (coherentes con la ISS real)

| Módulo emulado | Largo | Diám. int. | Uso en misión | Ancla sugerida |
|---|---:|---:|---|---|
| **Nodo** (Harmony/Unity) | 7,2 m | 4,2 m | Hub central, consola de emergencia [0] | Centro de la escena |
| **Laboratorio** (Destiny) | 8,5 m | 4,2 m | Módulo de repuesto [1], compuerta [12] | Conectado al nodo |
| **Módulo de servicio** (Zvezda) | 13,1 m | 4,2 m | Consola robótica [13], minijuego robótico [2] | Extremo opuesto |
| **Esclusa** (Quest) | 5,5 m | 4,0 m | Inicio/fin de EVA [4]–[11] | Lateral del nodo |
| **Cúpula** (Cupola) | 1,5 m | 3,0 m | Vista / verificación [16] | Sobre el nodo |
| **Pasillo corto** (PMA) | 2,5 m | 2,0 m | Conector entre módulos | Entre módulos |
| **Exterior** (panel solar) | — | — | Minijuego EVA [9],[15] | Fuera de la esclusa |

> Las dimensiones son las **reales** de los módulos de la ISS (aprox.) para dar coherencia visual.
> El interior útil de cada módulo: corredor central de ~2,2 m con racks a ambos lados.

### B.4. Detalle de construcción por módulo

- **Malla:** `CreateRoomMeshWithDoorway(largo, diám, altura, 1,1, 2,0, pared, true)` — puerta de
  ~1,1 × 2,0 m (medida real de escotilla ISS).
- **Suelo:** placa cuadrada + `MeshCollider` (para `CharacterController` del modo interior).
- **Racks laterales:** cubos de ~0,6 × 2,0 × 0,5 m a ambos lados (detalle, no colisión completa).
- **Iluminación:** **baked** (lightmap) o luz de ambiente; **evitar luces en tiempo real** en Quest.
- **Materiales:** reutilizar los ligeros de NASA:
  `Assets/SpaceStation5/Materials/M_Interior_Wall.mat`, `M_Interior_Floor.mat`,
  `M_Interior_Ceiling.mat` (copiar a `Assets/Game/Art/Interiors/Materials/`).
- **Texturas:** `T_Interior_Wall_Albedo.png` (6 KB), `T_Interior_Floor_Albedo.png` (31 KB) — ligeras.

### B.5. Escena y locomoción

- **Escena nueva/legacy:** `Assets/Game/Scenes/02_Interior.unity` (o reconstruir `MissionVR`).
- **Player interior:** aquí **sí** tiene sentido `SpaceStationPlayerController`
  (`Assets/Game/Scripts/VR/SpaceStationPlayerController.cs`) por su `CharacterController` + colisión.
  En `01_Orbit` sigue prohibido (decisión cerrada).
- **VR:** `XRPlayerRig` (`Assets/Game/Scripts/VR/XRPlayerRig.cs`) + locomoción con sticks.
- **Spawn:** `PlayerSpawnBay` (ver `REFERENCIA_NASA_SPACE_APPS.md` §2.5) en el nodo central.
- **Puertas:** `StationDoor` entre módulos (corredera por proximidad).

### B.6. Presupuesto de rendimiento (Quest 2)

| Aspecto | Objetivo | Nota |
|---|---|---|
| Triángulos visibles (interior) | ≤ 150k | Los módulos son cajas: baratos |
| Draw calls | ≤ 80 | 1 material por superficie + batching |
| Materiales | 3–5 | Pared / suelo / techo / emisivo |
| Texturas | ASTC 6×6, 1024 | Ya son pequeñas; sólo override Android |
| Luces en tiempo real | 0 (baked) | Ambiente + emisivos |
| Render | Single Pass Instanced | Ya configurado en Nova |
| Post-proceso | Mínimo | Bloom ligero |

### B.7. Fases de implementación

1. **F1 — Malla de sala mínima.** Portar `CreateRoomMeshWithDoorway` y generar **un** módulo de prueba.
2. **F2 — Nodo + laboratorio.** Conectar 2 módulos con puerta y pasillo; colisiones; spawn.
3. **F3 — Esclusa + EVA.** Añadir esclusa y el salto al exterior (panel solar).
4. **F4 — Minijuegos.** Portar `SpaceRepairStation` desacoplada y engancharla al `MissionActionBridge`.
5. **F5 — Misiones completas.** Cablear los 6 + 10 objetivos en la escena de interior.
6. **F6 — Optimización.** Baked lighting, ASTC, medir draw calls en dispositivo.

### B.8. Alternativa descartada (y por qué)

- **Comprar/usar un modelo de ISS con interiores ya hechos:** la mayoría de modelos gratuitos no
  traen interiores navegables a escala real; los de pago suelen ser sólo exterior. La vía de módulos
  emulados es **más ligera, controlable y coherente** con el modelo actual.
- **Exportar interiores de `NASA-SPACE-APPS`:** imposible sin reescalar (1 km → ISS) y sin arrastrar
  su escena y materiales. Descartado en `REFERENCIA_NASA_SPACE_APPS.md` §4.

---

## Parte C — Trazabilidad con el documento de funciones

**Documento:** `contexto-proyecto/Nasa_SpaceApps_Documentacion.pdf` (v1.1). Texto extraído:
`contexto-proyecto/_pdf_extraido.txt`. Revisión completa en `REFERENCIA_NASA_SPACE_APPS.md` §6.

### C.1. Objetivo → módulo emulado → estado

| # | Objetivo (acción) | Módulo/zona (Parte B) | Estado en Nova |
|---|---|---|---|
| energy_01 | EmergencyActivated (0) | Nodo: consola de emergencia | ⏳ Falta objeto + bridge |
| energy_02 | EnergyModuleCollected (1) | Laboratorio: repuesto agarrable | ⏳ Falta `MissionPickup` |
| energy_03 | EnergyModuleLoadedIntoHatch (12) | Laboratorio: compuerta | ⏳ Falta compuerta |
| energy_04 | RoboticConsoleActivated (13) | Servicio: consola robótica | ⏳ Falta consola |
| energy_05 | EnergyModuleInstalled (2) | **Minijuego robótico (nuevo)** | ⏳ No existe |
| energy_06 | RepairConfirmed (3) | Servicio: confirmación en consola | ⏳ Falta consola |
| solar_01 | SuitEquipped (4) | Esclusa: armario de traje | ⏳ Falta objeto |
| solar_02 | SuitSystemsChecked (5) | Esclusa: consola de chequeo | ⏳ Falta objeto |
| solar_03 | EVAPreparationCompleted (6) | Esclusa | ⏳ Falta objeto |
| solar_04 | AirlockDepressurized (7) | Esclusa: panel despresurizar | ⏳ Falta objeto |
| solar_05 | MMUActivated (14) | **MMU (nuevo)** | ⏳ No existe |
| solar_06 | SolarPanelReached (15) | Exterior: trigger de llegada | ⏳ Falta trigger |
| solar_07 | SolarPanelRepaired (9) | Exterior: **minijuego del panel** (base welder) | ⏳ No existe |
| solar_08 | ReturnedToAirlock (10) | Esclusa: trigger de retorno | ⏳ Falta trigger |
| solar_09 | AirlockRepressurized (11) | Esclusa: sellado/presión (base sealant) | ⏳ Falta objeto |
| solar_10 | StationPowerVerified (16) | Cúpula: consola de verificación | ⏳ Falta consola |

### C.2. Requisitos funcionales del documento → estado real

| RF (§3.1) | Estado en Nova |
|---|---|
| RF-01 Iniciar secuencia | ✅ Core |
| RF-02 Mostrar misión/objetivo (UI) | ❌ UI pendiente |
| RF-03 Bloquear acciones fuera de orden | ✅ Core |
| RF-04 Avanzar entre 2 misiones | ✅ Core |
| RF-05 Reiniciar + restaurar objetos | 🟡 Lógica lista; objetos pendientes |
| RF-06 Recorrer la estación en VR | 🟡 `XRPlayerRig` + locomoción; falta probar |
| RF-07 Manipular módulo/compuerta/consola | ❌ Diseñado, no implementado |
| RF-08 Victoria tras la 2ª misión | 🟡 Evento `OnMissionCompleted` sí; UI no |
| RF-09 Añadir misiones por ScriptableObject | ✅ Core |
| RF-10 Despertar/alarma/Centro de Control | ❌ `GameIntroManager` (nuevo) |
| RF-11 EVA con MMU y retorno | ❌ MMU (nuevo) |
| RF-12 Minijuego robótico → `EnergyModuleInstalled` | ❌ No existe |
| RF-13 Minijuego exterior → `SolarPanelRepaired` | ❌ Provisional |
| RF-14 Compuerta + consola como accesos | ❌ No implementado |
| RF-15 Registrar pruebas | ❌ Pendiente |

### C.3. Trabajo nuevo derivado del documento

Estos elementos **no están ni en Nova ni en `NASA-SPACE-APPS`** y son tareas nuevas
(IDs en `TAREAS_PENDIENTES.md` §8):
`GameIntroManager` (FUN-01), minijuego robótico (FUN-02), MMU (FUN-03), UI de misión (FUN-04),
compuerta/consola (FUN-05), minijuego del panel (FUN-06), sistema de pruebas (FUN-07).

---

*Plan generado el 2026-11-14. Sustituye a cualquier propuesta previa de interiores, que queda obsoleta.*
