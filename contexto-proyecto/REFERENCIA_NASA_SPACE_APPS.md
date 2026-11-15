# Referencia — Proyecto `NASA-SPACE-APPS` (Space Station 5)

**Última actualización:** 2026-11-14
**Proyecto de origen:** `C:\Users\fdx_dracox\Documents\Projects\NASA-SPACE-APPS`
**Proyecto destino:** `C:\Users\fdx_dracox\Documents\Projects\NovaCoreOrbita\Nova-Core-Orbita`
**Qué es este documento:** inventario de las funcionalidades del proyecto `NASA-SPACE-APPS`,
qué conviene traer a Nova Core Órbita, **con rutas de origen y destino**, dependencias de cada
sistema y un veredicto (portar / adaptar / reutilizar lógica / evitar / requiere decisión).

> Documentos hermanos:
> - Plan de misiones e interiores: `contexto-proyecto/PLAN_INTERIORES_MISIONES.md`
> - Backlog único consolidado: `contexto-proyecto/TAREAS_PENDIENTES.md`
> - Estado real de Nova: `contexto-proyecto/ESTADO_ACTUAL_Y_TAREAS.md`
>
> ⚠️ **Regla de oro:** Nova tiene decisiones cerradas (sin XRI, sin `VRMovement` en el Player,
> `01_Orbit` es la demo). Este documento **no las revierte**: cuando un sistema de NASA depende
> de XRI, se marca como **«requiere decisión»** (ver `HANDOFF.md` §8) en vez de darlo por hecho.

---

## 0. Resumen ejecutivo (los 10 segundos)

| # | Sistema de `NASA-SPACE-APPS` | Veredicto | Por qué |
|---|---|---|---|
| 1 | Sistema de misiones (Core: `MissionManager` + enum) | ✅ **Ya está en Nova** | El Core de misiones vive en `Assets/Game/Scripts/Core/`; NASA solo lo *dispara* |
| 2 | 4 mesas de reparación + minijuegos (`SpaceRepairStation`) | 🟢 **Adaptar (alto valor)** | Lógica de minijuego casi agnóstica; encaja con `MissionAction` |
| 3 | Herramientas EVA (`SpaceToolInteractable`) | 🟡 **Requiere decisión** | Depende de XRI 3.3.2 (Nova no lo tiene) |
| 4 | Interiores procedurales (`StationArchitect`) | 🔴 **Evitar tal cual / reutilizar técnica** | Escala 500–1200 m, no es la ISS; la **técnica** sí sirve |
| 5 | Constructor de mallas de sala (`StationMeshBuilder`) | 🟢 **Portar (parcial)** | `CreateRoomMesh*` reutilizable para módulos emulados |
| 6 | `StationDoor`, `StationTransitPod`, `PlayerSpawnBay` | 🟢 **Portar (ligero)** | Componentes pequeños, sin XRI |
| 7 | Ambiente cósmico (`UniverseEnvironmentArchitect`, agujero negro…) | 🟡 **Opcional / evitar** | Muy pesado para Quest 2; Nova ya tiene su skybox |
| 8 | `SpaceStationPlayerController` (FPS con colisión) | 🟡 **Adaptar (solo interiores)** | Útil para el modo interior; **no** para `01_Orbit` |
| 9 | Texturas / shaders / materiales | 🟡 **Seleccionar pocas** | La mayoría son grandes; sólo interiores y un par de hull |
| 10 | `XRPlayerRig` propio de NASA | ⚪ **No portar** | Nova ya tiene su propio `XRPlayerRig` sin XRI |

**Idea fuerza:** de NASA se traen **dos cosas**: (a) la **lógica de los minijuegos de reparación**
y (b) la **técnica para generar salas pequeñas**, no el proyecto entero ni su escala de 1 km.

---

## 1. Qué es `NASA-SPACE-APPS` (ficha técnica)

| Dato | Valor |
|---|---|
| Escena principal | `Assets/SpaceStation5/Scenes/SpaceStation5.unity` (7.9 MB, ~910 GameObjects) |
| Concepto | Estación orbital "Space Station 5" de 1.000 m de diámetro, explorable por dentro y por fuera |
| Render | URP 17.6.0 (mismo que Nova) |
| XR | **XRI 3.3.2** + XR Hands 1.10.0 + AR Foundation 6.6.2 |
| Otros paquetes | AI Navigation 2.0.16, Timeline 6.6.0, pipeline 0.9.1-exp.1 |
| Interiores | **Procedurales** (generados por editor), con pasillos, rotonda, laboratorios, zona residencial |
| Ambiente | Tierra/Luna/Sol/agujero negro + domo estelar 4K |

Diferencias clave con Nova:

- Nova **no tiene XRI**; NASA **sí** (es la dependencia que condiciona todo lo táctil).
- Nova usa la **ISS real** (`ISS_stationary.glb`, 112 × 69 × 59 m); NASA usa una estación **ficticia**
  de escala gigante (anillo de 500 m de radio).
- Nova tiene el **Core de misiones** ya integrado; NASA tiene el **gameplay** que dispara esas acciones.

---

## 2. Inventario por sistema

### 2.1. Sistema de misiones (Core) — ya presente en Nova

| Elemento | Ruta en Nova |
|---|---|
| `MissionManager.cs` | `Assets/Game/Scripts/Core/MissionManager.cs` |
| `MissionAction.cs` (enum) | `Assets/Game/Scripts/Core/MissionAction.cs` |
| `MissionActionBridge.cs` | `Assets/Game/Scripts/Core/MissionActionBridge.cs` |
| `MissionDefinition.cs` | `Assets/Game/Scripts/Core/Definitions/MissionDefinition.cs` |
| `MissionObjective.cs` | `Assets/Game/Scripts/Core/Definitions/MissionObjective.cs` |
| Datos de misión | `Assets/Game/Data/Missions/Mission_Energy.asset`, `Mission_SolarPanel.asset` |
| Contratos | `Assets/Game/Docs/CONTRATO_INTEGRACION_CORE.md`, `INTEGRACION_MISIONES.md` |

**Veredicto: ✅ nada que portar aquí.** El Core ya vive en Nova. Lo que falta es **gameplay que
llame a `MissionActionBridge.TryExecuteAction()`** para cada objetivo. Eso es lo que se diseña en
`PLAN_INTERIORES_MISIONES.md`.

> El enum `MissionAction` **no se renumera nunca** (valores 0–16 en uso). Ver §5 del plan.

---

### 2.2. Mesas de reparación + minijuegos — 🟢 adaptar (alto valor)

| Archivo | Ruta de origen | Qué hace |
|---|---|---|
| `SpaceRepairStation.cs` | `Assets/Herramientas/Scripts/SpaceRepairStation.cs` | 4 minijuegos de reparación con progreso, estado y audio |
| `SpaceStationToolStationRuntime.cs` | `Assets/Herramientas/Scripts/SpaceStationToolStationRuntime.cs` | Auto-spawn de las 4 mesas + herramientas al cargar escena |
| `ToolStationBuilder.cs` | `Assets/Herramientas/Scripts/Editor/ToolStationBuilder.cs` | Menú de Editor que construye las mesas en la escena |
| `SpaceToolMeshLoader.cs` | `Assets/Herramientas/Scripts/SpaceToolMeshLoader.cs` | Carga OBJ en runtime + malla procedural de respaldo |

Tipos de minijuego (`RepairStationType`) y su encaje con las acciones de Nova:

| `RepairStationType` | Minijuego | `MissionAction` candidato |
|---|---|---|
| `PistolGrip_WelderAndDrill` | Soldar placa + taladrar tornillo de precisión | `RepairConfirmed` (3) / `SolarPanelRepaired` (9) |
| `Hammer_ArmorStraightening` | Enderezar placa de blindaje a golpes | `RepairConfirmed` (3) |
| `Ratchet_FlangeBolts` | Girar pernos de brida en orden | `EnergyModuleInstalled` (2) / `RepairConfirmed` (3) |
| `GreaseGun_PressureSealant` | Sellar fuga, subir presión de cámara | `AirlockRepressurized` (11) |

API útil que **ya expone** (sin XRI): `GetOverallProgress()`, `isTask1Complete`, `isTask2Complete`,
`task1Progress`, `task2Progress`, `ResetStation()` y los métodos `TryApplyWelder/Drill/Hammer/
Ratchet/Sealant(...)`. Es decir: **el progreso y la finalización se pueden leer sin XRI**.

**Dependencia:** el archivo **entero** referencia `SpaceToolInteractable` (que sí usa XRI). Para
portarlo sin XRI hay que **separar** la lógica de minijuego (agnóstica) del agarre de herramienta
(XRI). Ver estrategia en `PLAN_INTERIORES_MISIONES.md` §A.3.

**Veredicto:**
- ✅ Portar la **lógica** `SpaceRepairStation` (minijuegos + estado + audio).
- 🔴 **No** portar `SpaceToolInteractable` ni `SpaceStationToolStationRuntime` tal cual (XRI).
- 🟢 El disparo de la acción puede hacerse con el `MissionActionBridge` que ya existe en Nova.

---

### 2.3. Herramientas EVA — 🟡 requiere decisión

| Archivo | Ruta de origen | Dependencia |
|---|---|---|
| `SpaceToolInteractable.cs` | `Assets/Herramientas/Scripts/SpaceToolInteractable.cs` | **XRI 3.3.2** (`XRGrabInteractable`, `XRBaseInputInteractor`), Input System, haptics |
| Modelos | `Assets/Herramientas/PistolGripTool.obj`, `Hammer.obj`, `Ratchet.obj`, `GreaseGun.obj` (+ `.glb`) | — |
| Materiales | `Assets/Herramientas/Materials/M_Tool_*.mat` | URP/Lit |
| Textura | `Assets/Herramientas/pgt1_1_d.png` | — |

**El conflicto:** las herramientas están riggeadas para XRI. Nova decidió **no instalar XRI**
(`HANDOFF.md` §7). Por tanto:

- **Opción A (recomendada, ligera):** usar sólo los **modelos** (OBJ/GLB) y escribir un
  "holder" propio de herramienta sobre `InputDevices` (como ya hace `OrbitFreeFly` para los mandos).
  Las mesas llaman a los `TryApply*` con un `hitPoint`; no necesitan XRI.
- **Opción B (pesada):** instalar XRI y rehacer el rig con `XR Origin`. Reabre una decisión cerrada.

**Veredicto: 🟡 requiere decisión** — registrada como pregunta abierta en `HANDOFF.md` §8.
Por defecto, **Opción A**.

---

### 2.4. Interiores procedurales — 🔴 evitar tal cual, reutilizar la técnica

| Archivo | Ruta de origen | Qué hace |
|---|---|---|
| `StationArchitect.cs` | `Assets/SpaceStation5/Scripts/Editor/StationArchitect.cs` (2052 líneas) | Construye la estación entera (exterior + interiores) |
| `StationMeshBuilder.cs` | `Assets/SpaceStation5/Scripts/StationMeshBuilder.cs` (789 líneas) | Genera mallas de sala, pasillo curvo, casco |

Escala real que genera (incompatible con la ISS de Nova):

| Constante | Valor |
|---|---|
| `RingRadius` | 500 m (1.000 m de diámetro) |
| `SpineLength` | 1.200 m |
| `HubInteriorRadius` | 40 m (rotonda de 80 m) |
| `CorridorWidth` / `CorridorHeight` | 6 m / 4,2 m |
| Salas (obs., labs, residencial) | ~50 × 50 × 8 m |

Funciones útiles de `StationMeshBuilder` para **salas pequeñas**:

- `CreateRoomMesh(width, length, height, ...)`
- `CreateRoomMeshWithDoorway(width, length, height, doorWidth, doorHeight, DoorwayWall wall, ...)`
- Mallas de pasillo curvo / casco (opcionales).

**Veredicto:**
- 🔴 **No portar `StationArchitect` completo** (escala, materiales y dependencias de su escena).
- 🟢 **Sí portar la técnica**: una versión mínima que genere módulos ISS de ~7 m con
  `CreateRoomMeshWithDoorway`. Detallado en `PLAN_INTERIORES_MISIONES.md` §B.

---

### 2.5. Componentes pequeños reutilizables — 🟢 portar (ligero)

| Script | Ruta de origen | Qué hace | Dependencia |
|---|---|---|---|
| `StationDoor.cs` | `Assets/SpaceStation5/Scripts/StationDoor.cs` | Puerta corredera por proximidad | Ninguna (solo `Transform`) |
| `StationTransitPod.cs` | `Assets/SpaceStation5/Scripts/StationTransitPod.cs` | Ascensor/pod entre puntos | Ninguna |
| `PlayerSpawnBay.cs` | `Assets/SpaceStation5/Scripts/PlayerSpawnBay.cs` | Punto de spawn/teleport con anillos y luz | Ninguna |

> Nova **ya tiene** un `PlayerSpawnBay.cs` en `Assets/Game/Scripts/VR/PlayerSpawnBay.cs`
> (sin uso). Comparar antes de importar para no duplicar.

**Veredicto: 🟢 portar/adaptar** — son pequeños, sin XRI, y encajan con el modo interior.

---

### 2.6. Ambiente cósmico — 🟡 opcional / evitar

| Script | Ruta de origen | Qué hace | Veredicto |
|---|---|---|---|
| `UniverseEnvironmentArchitect.cs` | `Assets/SpaceStation5/Scripts/Editor/UniverseEnvironmentArchitect.cs` | Tierra, Luna, Sol, agujero negro, domo estelar | 🔴 evitar (pesado) |
| `CelestialOrbitController.cs` | `Assets/SpaceStation5/Scripts/CelestialOrbitController.cs` | Rotación orbital de todos los astros | 🟡 opcional |
| `BlackHoleController.cs` | `Assets/SpaceStation5/Scripts/BlackHoleController.cs` | Disco de acreción, precesión | 🔴 evitar |
| `AnimatedSpaceEnvironment.cs` | `Assets/SpaceStation5/Scripts/AnimatedSpaceEnvironment.cs` | Deriva de UV del domo estelar | 🟡 opcional |
| `TextureGenerator.cs` | `Assets/SpaceStation5/Scripts/Editor/TextureGenerator.cs` | Texturas procedurales | ⚪ no portar |

**Contexto:** Nova ya tiene su **skybox deep space 2K** y el Sol alineado (Fase 1 completa). Traer
el sistema completo de NASA (agujero negro 21 MB + domo 4K) **rompería el presupuesto de Quest 2**.

**Veredicto:** como máximo, una **Tierra de fondo** ligera (ya prevista en Nova con
`OrbitalEnvironment.cs`). El resto, no.

---

### 2.7. Controlador de jugador — 🟡 adaptar sólo para interiores

| Script | Ruta de origen | Notas |
|---|---|---|
| `SpaceStationPlayerController.cs` | `Assets/SpaceStation5/Scripts/SpaceStationPlayerController.cs` | FPS con `CharacterController`, head-bob, linterna, desktop + VR |

Nova ya tiene uno equivalente **sin uso** en `Assets/Game/Scripts/VR/SpaceStationPlayerController.cs`.
Recordatorio de la decisión cerrada: **no** añadirlo al `Player` de `01_Orbit` (duplicaría
movimiento con `OrbitFreeFly`). Sólo tiene sentido en una **escena de interior** separada.

---

### 2.8. Rig VR de NASA — ⚪ no portar

| Script | Ruta de origen | Motivo |
|---|---|---|
| `XRPlayerRig.cs` | `Assets/SpaceStation5/Scripts/XRPlayerRig.cs` | Nova ya tiene el suyo (`Assets/Game/Scripts/VR/XRPlayerRig.cs`) sin XRI |

---

## 3. Matriz de dependencias (¿qué arrastra cada sistema?)

| Sistema | XRI | XR Hands | AI Nav | Timeline | glTFast | URP |
|---|:--:|:--:|:--:|:--:|:--:|:--:|
| `SpaceRepairStation` (minijuegos) | ⚠️ indirecta¹ | — | — | — | — | ✅ |
| `SpaceToolInteractable` (herramientas) | ❌ **sí** | — | — | — | — | ✅ |
| `SpaceStationToolStationRuntime` | ❌ **sí** (spawnea herramientas) | — | — | — | — | ✅ |
| `StationMeshBuilder` | — | — | — | — | — | ✅ |
| `StationDoor` / `StationTransitPod` / `PlayerSpawnBay` | — | — | — | — | — | ✅ |
| `UniverseEnvironmentArchitect` | — | — | — | — | — | ✅ |

¹ El archivo `SpaceRepairStation.cs` referencia `SpaceToolInteractable` (tipo `dockedTool`). La
**lógica** no necesita XRI, pero hay que **desacoplar** el tipo para compilar sin XRI.

**Paquetes de NASA que Nova NO tiene:** `com.unity.xr.interaction.toolkit`, `com.unity.xr.hands`,
`com.unity.xr.arfoundation`, `com.unity.ai.navigation`, `com.unity.timeline`.
**Paquetes compartidos:** URP 17.6.0, Input System 1.20.1, OpenXR 1.18.0, XR Management 4.7.0, glTFast.

---

## 4. Lo que NO conviene portar (lista negra)

1. **`StationArchitect.cs` completo** y su escena `SpaceStation5.unity` (7.9 MB, ~910 objetos, escala 1 km).
2. **El sistema de agujero negro** (modelo de 21 MB + shaders + domo 4K): incompatible con Quest 2.
3. **`UniverseEnvironmentArchitect`**: duplica lo que Nova ya resolvió con skybox.
4. **Texturas grandes** (T_Deep_Space_Stars 1,1 MB, T_Planet_Earth_* , T_BlackHole_*): sólo si se
   downescalan a ASTC 6×6 / 1024. Por defecto, **no**.
5. **`SpaceToolInteractable` / `SpaceStationToolStationRuntime`** tal cual (XRI).
6. **`XRPlayerRig` de NASA**: Nova ya tiene el suyo.

---

## 5. Mapa de rutas: origen → destino propuesto en Nova

| Elemento | Origen (NASA-SPACE-APPS) | Destino propuesto (Nova) | Acción |
|---|---|---|---|
| Lógica de minijuegos | `Assets/Herramientas/Scripts/SpaceRepairStation.cs` | `Assets/Game/Scripts/Interaction/SpaceRepairStation.cs` | Portar y **desacoplar** XRI |
| Cargador de mallas | `Assets/Herramientas/Scripts/SpaceToolMeshLoader.cs` | `Assets/Game/Scripts/Interaction/SpaceToolMeshLoader.cs` | Portar (quitar XRI) |
| Modelos de herramienta | `Assets/Herramientas/*.obj` | `Assets/Game/Art/Tools/` | Copiar OBJ/GLB + materiales |
| Materiales de herramienta | `Assets/Herramientas/Materials/M_Tool_*.mat` | `Assets/Game/Art/Tools/Materials/` | Copiar |
| Sala procedural | `Assets/SpaceStation5/Scripts/StationMeshBuilder.cs` | `Assets/Game/Scripts/Interiors/StationMeshBuilder.cs` | Portar sólo `CreateRoomMesh*` |
| Puerta | `Assets/SpaceStation5/Scripts/StationDoor.cs` | `Assets/Game/Scripts/Interiors/StationDoor.cs` | Copiar |
| Pod/ascensor | `Assets/SpaceStation5/Scripts/StationTransitPod.cs` | `Assets/Game/Scripts/Interiors/StationTransitPod.cs` | Copiar (opcional) |
| Spawn | `Assets/SpaceStation5/Scripts/PlayerSpawnBay.cs` | `Assets/Game/Scripts/Interiors/PlayerSpawnBay.cs` | Comparar con el existente |
| Materiales interior | `Assets/SpaceStation5/Materials/M_Interior_*.mat` | `Assets/Game/Art/Interiors/Materials/` | Copiar (son ligeros) |
| Texturas interior | `Assets/SpaceStation5/Textures/T_Interior_*.png` | `Assets/Game/Art/Interiors/Textures/` | Copiar (31–25 KB, ligeras) |

> Las rutas de destino son **propuestas**; se confirmarán al implementar (ver `TAREAS_PENDIENTES.md`).
> Convención Nova: scripts de gameplay en `Assets/Game/Scripts/…`, arte en `Assets/Game/Art/…`.

---

## 6. Documento de funciones — cobertura y revisión

**Documento recibido:** `contexto-proyecto/Nasa_SpaceApps_Documentacion.pdf`
**Título:** «NASA Space Apps | Unity VR Merge técnico v1.1» — 23 páginas, equipo **Luis, Santi,
Yoshua, Diogo y Camila**. Texto extraído y buscable en `contexto-proyecto/_pdf_extraido.txt`.

### 6.0. Veredicto de la revisión (resumen)

- El documento es la **especificación oficial** de la experiencia: una emergencia, **2 misiones
  (6 + 10 objetivos)**, contrato de Core, reparto de trabajo, pruebas y plan.
- **Nova ya cumple el Core al 100 %** (API, eventos, enum 0–16, los dos `.asset` con el orden exacto).
  El documento describe Core como «escrito, sin verificar en el repo nuevo»: en Nova **está verificado**
  (compila, assets creados, `MissionDebugTester` presente).
- El documento asume **XR Interaction Toolkit + XR Origin** (Santi, §9.3). **Nova no tiene XRI** por
  decisión cerrada → **conflicto abierto** (ver 6.4 y `HANDOFF.md` §8).
- El documento pide **3 cosas que hoy no existen ni en Nova ni en `NASA-SPACE-APPS`**:
  `GameIntroManager`, el **minijuego robótico** (brazo/sonda desde consola) y el **MMU**.
- De `NASA-SPACE-APPS` lo **directamente reutilizable** para este documento es la **mesa de
  reparación EVA** (`SpaceRepairStation`, minijuego tipo welder) como base del minijuego del panel.

### 6.1. Qué ya cumple Nova (verificado en código y assets)

| Elemento del documento (§) | Ruta en Nova | Estado |
|---|---|---|
| `MissionManager` + API (§5.1, §5.3) | `Assets/Game/Scripts/Core/MissionManager.cs` | ✅ Completo |
| `MissionState` (NotStarted/InProgress/Completed) | `Assets/Game/Scripts/Core/MissionState.cs` | ✅ |
| `MissionAction` 0–16 (§5.4) | `Assets/Game/Scripts/Core/MissionAction.cs` | ✅ 17 valores |
| `MissionActionBridge` (`ExecuteAction`/`TryExecuteAction`/`BeginMission`/`RestartMission`) | `Assets/Game/Scripts/Core/MissionActionBridge.cs` | ✅ |
| `MissionDefinition` / `MissionObjective` | `Assets/Game/Scripts/Core/Definitions/` | ✅ |
| Eventos (§5.5) | `MissionManager.cs` líneas 82–89 | ✅ Los 5 exactos |
| `Mission_Energy.asset` (6 objetivos, §6.1) | `Assets/Game/Data/Missions/Mission_Energy.asset` | ✅ Orden 0,1,12,13,2,3 idéntico |
| `Mission_SolarPanel.asset` (10 objetivos, §7.1) | `Assets/Game/Data/Missions/Mission_SolarPanel.asset` | ✅ Orden 4,5,6,7,14,15,9,10,11,16 idéntico |
| `SafetyTetherAttached` (8) sin usar (§5.4) | enum + assets | ✅ Confirmado |
| `MissionDebugTester` (§5.1, §12) | `Assets/Game/Tests/MissionDebugTester.cs` | ✅ Existe |
| Estructura `Assets/Game/…` (§4.1) | `Assets/Game/` | ✅ Casi idéntica (ver 6.3) |
| Escenario nuevo (§4.1 «Entorno_3D_Nuevo») | `Assets/NovaCore/Art/ISS/` + skybox | ✅ Es la ISS + ambiente |

> **Conclusión:** todo lo que el documento describe como «Core escrito, sin verificar» **está
> implementado y coincide exactamente** en Nova. No hay que reescribir el Core.

### 6.2. Qué cubre `NASA-SPACE-APPS` (reutilizable para este documento)

| Necesidad del documento | Cobertura en `NASA-SPACE-APPS` | Ruta |
|---|---|---|
| Minijuego EVA del panel (§7.2) | `SpaceRepairStation` tipo **welder/drill** (soldar + taladrar) | `Assets/Herramientas/Scripts/SpaceRepairStation.cs` |
| Minijuego de sellado/presión | Tipo **sealant** (subir presión de cámara) | idem |
| Puntos de spawn / esclusa | `PlayerSpawnBay`, `StationDoor` | `Assets/SpaceStation5/Scripts/` |
| Interiores navegables (base) | Técnica de `StationMeshBuilder` | `Assets/SpaceStation5/Scripts/StationMeshBuilder.cs` |
| FPS con colisión para interiores | `SpaceStationPlayerController` | `Assets/SpaceStation5/Scripts/` |

### 6.3. Qué es trabajo nuevo (ni Nova ni `NASA-SPACE-APPS` lo tienen)

| Necesidad del documento (§) | Estado | Nota |
|---|---|---|
| `GameIntroManager` — despertar + alarma + Centro de Control (§2.2) | ❌ No existe | Documento lo marca «no confirmado» |
| **Minijuego robótico** (brazo/sonda desde consola) (§6.2) | ❌ No existe | `NASA-SPACE-APPS` **no** tiene brazo robótico jugable |
| **MMU** simplificado + navegación EVA (§7.1 solar_05) | ❌ No existe | Tampoco en `NASA-SPACE-APPS` |
| **UI de misión** (Camila): panel reactivo a eventos (§9.6) | ❌ No existe | Sólo `ModeToggleUI` (no en escena) |
| `Interaction/RoboticRepair/`, `Interaction/SolarPanelRepair/` (§4.1) | ❌ Carpetas propuestas | `Assets/Game/Scripts/Interaction/` está vacía |
| `Flow/` para `GameIntroManager` (§4.1) | ❌ No existe | — |

> La estructura de `Assets/Game/` de Nova **ya coincide** con §4.1 salvo `Flow/`,
> `Interaction/RoboticRepair/` y `Interaction/SolarPanelRepair/` (propuestas).

### 6.4. Conflictos que requieren decisión

1. **XR Interaction Toolkit / XR Origin (§9.3) vs. Nova sin XRI.**
   El documento asigna a Santi «OpenXR/XR Interaction Toolkit, XR Origin». Nova usa
   `XRPlayerRig` sin XRI (decisión cerrada, `HANDOFF.md` §7). → `DEC-01`.
2. **Minijuego de Misión 1 ≠ mesa de reparación.**
   El documento define el minijuego de energía como **brazo/sonda robótico operado desde consola**
   (§6.2), **no** como la mesa física de `SpaceRepairStation`. Por tanto, el mapeo
   «ratchet → `EnergyModuleInstalled`» que figuraba en `PLAN_INTERIORES_MISIONES.md` §A.4 era
   **incorrecto** y queda corregido (ver §C de ese documento). `SpaceRepairStation` sirve para el
   **minijuego del panel (EVA)**, no para el robótico.
3. **Mecánica final del minijuego del panel (§7.2):** el documento la deja **provisional**
   («panel rasgado con refuerzos»). Hay que decidirla antes de implementar.

### 6.5. Coherencia enum ↔ assets ↔ documento

| Acción (valor) | Misión 1 | Misión 2 | Documento |
|---|:--:|:--:|:--:|
| 0 EmergencyActivated | ✅ energy_01 | — | ✅ §6.1 |
| 1 EnergyModuleCollected | ✅ energy_02 | — | ✅ |
| 12 EnergyModuleLoadedIntoHatch | ✅ energy_03 | — | ✅ |
| 13 RoboticConsoleActivated | ✅ energy_04 | — | ✅ |
| 2 EnergyModuleInstalled | ✅ energy_05 | — | ✅ |
| 3 RepairConfirmed | ✅ energy_06 | — | ✅ |
| 4–7 Suit/EVA/Airlock | — | ✅ solar_01–04 | ✅ §7.1 |
| 14 MMUActivated | — | ✅ solar_05 | ✅ |
| 15 SolarPanelReached | — | ✅ solar_06 | ✅ |
| 9 SolarPanelRepaired | — | ✅ solar_07 | ✅ |
| 10 ReturnedToAirlock | — | ✅ solar_08 | ✅ |
| 11 AirlockRepressurized | — | ✅ solar_09 | ✅ |
| 16 StationPowerVerified | — | ✅ solar_10 | ✅ |
| 8 SafetyTetherAttached | — | — | ✅ (no usado) |

**Sin discrepancias:** el enum, los dos `.asset` y el documento **coinciden exactamente**.

---

*Referencia generada el 2026-11-14 a partir de la inspección directa de `NASA-SPACE-APPS`.*
