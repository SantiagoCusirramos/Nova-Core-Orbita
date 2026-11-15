# Tareas pendientes — Nova Core Órbita (backlog único)

**Última actualización:** 2026-11-14
**Qué es:** el **único** backlog del proyecto. Consolida y **sustituye** como lista de tareas a:
- `ESTADO_ACTUAL_Y_TAREAS.md` §10
- `HANDOFF.md` §6 (próximos 3 pasos)
- `contexto_nova_core_orbita_vr.md` §10
- y las tareas nuevas de portar `NASA-SPACE-APPS` (ver `REFERENCIA_NASA_SPACE_APPS.md`)

> Los documentos antiguos **conservan** sus secciones por contexto histórico, pero la lista viva
> es **esta**. Si hay discrepancia, manda este archivo.

**Leyenda de prioridad:** 🔴 bloqueante · 🟡 necesario · 🟢 mejora · ⚪ fase futura
**Convención de ID:** `VR-` VR/visor · `OPT-` optimización · `VIS-` visual · `INT-` interiores ·
`MIS-` misiones · `TOOL-` herramientas/mesas · `DOC-` documentación · `DEC-` decisión pendiente.

---

## 1. 🔴 Bloqueante para VR

| ID | Tarea | Criterio de aceptación | Origen | Depende de |
|---|---|---|---|---|
| VR-01 | Primer build Android / Quest Link | Logs `[XRPlayerRig] HMD: …` y `Mando izquierdo/derecho: …` aparecen con visor conectado | ESTADO §10 | — |
| VR-02 | Asignar modelos visuales de mando | `leftControllerModelPrefab` / `rightControllerModelPrefab` de `XRPlayerRig` no vacíos y visibles en VR | ESTADO §10, contexto §10 | VR-01 |
| VR-03 | Revisar `Project Validation` de OpenXR | Sin errores pendientes en `XR Plug-in Management → OpenXR` | ESTADO §10 | — |
| DEC-01 | **Decidir** interacción: XRI vs. interactores propios | Decisión escrita en `HANDOFF.md` §7/§8; si es XRI, plan de migración del rig | ESTADO §10, contexto §10 | — |
| DEC-02 | **Decidir** si se usan las herramientas EVA de NASA (XRI) o modelos propios | Opción A (holder propio) o B (XRI) registrada | REFERENCIA §2.3 | DEC-01 |

> ⚠️ `DEC-01`/`DEC-02` reabren una decisión cerrada (sin XRI). No se implementa nada de XRI hasta
> que se cierren explícitamente.

---

## 2. 🟡 Necesario para que la demo funcione bien en Quest

| ID | Tarea | Criterio de aceptación | Origen | Depende de |
|---|---|---|---|---|
| OPT-01 | Perfil URP móvil (`Mobile_RPAsset`) | Android usa Mobile RP: sin HDR, MSAA 4×, post mínimo | ESTADO §10 | — |
| OPT-02 | Texturas ISS en ASTC 6×6 / 1024 | Cada textura de `Assets/NovaCore/Art/ISS/Materials/` con override Android | ESTADO §10 | — |
| OPT-03 | Medir draw calls en dispositivo | Reporte de draw calls con Frame Debugger en Quest | ESTADO §10 | VR-01, OPT-01 |
| OPT-04 | Single Pass Instanced | Verificado activo en OpenXR Render Mode | contexto §10 (ya configurado, confirmar) | — |

---

## 3. 🟢 Mejoras visuales

| ID | Tarea | Criterio de aceptación | Origen | Depende de |
|---|---|---|---|---|
| VIS-01 | Rotación lenta de paneles solares | Nodos `panel_*` rotan ~1°/s sin coste perceptible | ESTADO §10 | — |
| VIS-02 | Guardar la ISS como Prefab | `Assets/NovaCore/Prefabs/ISS.prefab` existe y se reutiliza | ESTADO §10 | — |
| VIS-03 | Puntos de observación alternativos | Varios spawns con `PlayerSpawnBay` (arriba/lateral/frontal) | ESTADO §10 | — |
| VIS-04 | Tierra/Luna de fondo | `OrbitalEnvironment` en la escena con Tierra visible | ESTADO §10, contexto §10 | — |
| VIS-05 | Sustituir ISS por modelo nuevo comprado | Checklist `ESTADO` §9 cumplida; métricas actualizadas | ESTADO §10 | (compra) |

---

## 4. 🟢 Portar `NASA-SPACE-APPS` (nuevo)

Detalle y rutas: `REFERENCIA_NASA_SPACE_APPS.md` §2 y §5.

| ID | Tarea | Criterio de aceptación | Origen | Depende de |
|---|---|---|---|---|
| TOOL-01 | Portar `SpaceRepairStation` **desacoplada de XRI** | Compila sin XRI; `GetOverallProgress()`/`isTask*Complete` funcionan | REFERENCIA §2.2 | DEC-02 |
| TOOL-02 | Portar `SpaceToolMeshLoader` (sin XRI) | Carga OBJ en runtime o usa malla de respaldo | REFERENCIA §2.2 | — |
| TOOL-03 | Copiar modelos y materiales de herramienta | OBJ/GLB + `M_Tool_*.mat` en `Assets/Game/Art/Tools/` | REFERENCIA §5 | — |
| TOOL-04 | Adaptador `SpaceRepairStation → MissionActionBridge` | Completar un minijuego dispara la acción correcta | REFERENCIA §2.2, PLAN §A.3 | TOOL-01, MIS-02 |
| INT-01 | Portar `StationMeshBuilder.CreateRoomMesh*` | Genera una sala de prueba con puerta a escala ISS | REFERENCIA §2.4, PLAN §B.2 | — |
| INT-02 | Portar `StationDoor` | Puerta corredera abre por proximidad | REFERENCIA §2.5 | — |
| INT-03 | Portar `StationTransitPod` (opcional) | Pod se mueve entre dos puntos | REFERENCIA §2.5 | — |
| INT-04 | Copiar materiales/texturas de interior | `M_Interior_*` + `T_Interior_*` en `Assets/Game/Art/Interiors/` | REFERENCIA §5 | — |
| MIS-01 | Script `MissionPickup` (recoger/instalar objeto) | Objeto con trigger dispara su acción | PLAN §A.6 | — |

---

## 5. ⚪ Fase futura (interiores + misiones completas)

Plan detallado: `PLAN_INTERIORES_MISIONES.md`.

| ID | Tarea | Criterio de aceptación | Origen | Depende de |
|---|---|---|---|---|
| INT-05 | Módulo emulado de prueba (nodo) | Módulo 7,2 × 4,2 m con colisión y spawn | PLAN §B.3–B.4 | INT-01, INT-04 |
| INT-06 | Nodo + laboratorio conectados | 2 módulos unidos con puerta y pasillo, navegables | PLAN §B.7 F2 | INT-05, INT-02 |
| INT-07 | Esclusa + salida EVA | Esclusa funcional + trigger al exterior | PLAN §B.7 F3 | INT-06 |
| INT-08 | Escena `02_Interior.unity` (o `MissionVR` reconstruida) | Escena abre con player interior + spawn | PLAN §A.7, §B.5 | INT-05 |
| INT-09 | Player interior con `CharacterController` | `SpaceStationPlayerController` funciona en `02_Interior` (**no** en `01_Orbit`) | PLAN §B.5 | INT-08 |
| MIS-02 | Cablear Misión 1 (Energía, 6 objetivos) | Los 6 objetivos se completan en orden | PLAN §A.4–A.5 | INT-06, TOOL-04, MIS-01 |
| MIS-03 | Cablear Misión 2 (EVA, 10 objetivos) | Los 10 objetivos se completan en orden | PLAN §A.4–A.5 | INT-07, TOOL-04, MIS-01 |
| MIS-04 | Verificar `ResetMission()` con objetos físicos | Reset deja la escena en estado inicial | PLAN §A.2 | MIS-02, MIS-03 |
| VIS-06 | Animación Canadarm2 / DEXTRE | Brazos animados (requiere separar geometría) | ESTADO §10 | (Blender) |
| VIS-07 | UI / HUD en VR | Canvas world-space + raycaster XR | ESTADO §10, contexto §10 | DEC-01 |

---

## 6. ⚪ Deuda técnica / limpieza

| ID | Tarea | Criterio de aceptación | Origen |
|---|---|---|---|
| DOC-01 | Arreglar mojibake en 3 scripts VR | Comentarios legibles en `SpaceStationPlayerController`, `VRMovement`, `PlayerModeSwitcher` | ESTADO §11 |
| DOC-02 | Decidir futuro de `MissionVR.unity` legacy | Reconstruida o eliminada; Build Settings coherente | ESTADO §8 |
| DOC-03 | Actualizar `ESTADO_ACTUAL_Y_TAREAS.md` §10 → apuntar a este backlog | Sección §10 enlaza aquí y no mantiene lista propia | este archivo |

---

## 7. Trazabilidad con los documentos antiguos

| Documento | Sección | Equivalente aquí |
|---|---|---|
| `ESTADO_ACTUAL_Y_TAREAS.md` | §10 Bloqueante | VR-01…VR-03, DEC-01 |
| `ESTADO_ACTUAL_Y_TAREAS.md` | §10 Necesario | OPT-01…OPT-04 |
| `ESTADO_ACTUAL_Y_TAREAS.md` | §10 Mejoras | VIS-01…VIS-05 |
| `ESTADO_ACTUAL_Y_TAREAS.md` | §10 Fase posterior | INT-05…INT-09, MIS-02…MIS-04, VIS-06, VIS-07 |
| `HANDOFF.md` | §6 Próximos 3 pasos | VR-01, VR-02, VR-03 |
| `contexto_nova_core_orbita_vr.md` | §10 Bloqueante/Necesario/Mejoras/Futuro | misma cobertura que arriba |
| `REFERENCIA_NASA_SPACE_APPS.md` | §2, §5 | TOOL-01…TOOL-04, INT-01…INT-04 |
| `PLAN_INTERIORES_MISIONES.md` | §A, §B | MIS-01…MIS-04, INT-05…INT-09 |

---

## 8. Documento de funciones (`Nasa_SpaceApps_Documentacion.pdf` v1.1)

**Fuente:** `contexto-proyecto/Nasa_SpaceApps_Documentacion.pdf` (equipo Luis/Santi/Yoshua/Diogo/Camila).
Revisión completa y cobertura: `REFERENCIA_NASA_SPACE_APPS.md` §6 y `PLAN_INTERIORES_MISIONES.md` §C.

> **Nota:** el **Core** que describe el documento ya está implementado y verificado en Nova
> (enum 0–16, los dos `.asset` con el orden exacto, API, eventos y `MissionDebugTester`). Estas
> tareas son **sólo el gameplay/UI** que falta para cumplir el documento.

| ID | Tarea | Criterio de aceptación | Depende de |
|---|---|---|---|
| FUN-01 | `GameIntroManager` (despertar + alarma + Centro de Control) | Inicia `StartMission()` una sola vez; independiente de Core | DEC-01 |
| FUN-02 | **Minijuego robótico** (brazo/sonda desde consola) | Emite `EnergyModuleInstalled` (2) una sola vez tras validar pasos | INT-08 |
| FUN-03 | **MMU** simplificado + navegación y estabilización | Emite `MMUActivated` (14) y `SolarPanelReached` (15) cuando procede | DEC-01 |
| FUN-04 | **UI de misión** (panel reactivo a eventos de Core) | Muestra `CurrentMission`/`CurrentObjective`; escucha los 5 eventos; se desuscribe al desactivar | — |
| FUN-05 | Compuerta + consola robótica como objetos interactivos | energy_03 (12) y energy_04 (13) completables | INT-06 |
| FUN-06 | **Minijuego del panel EVA** (mecánica provisional) | Emite `SolarPanelRepaired` (9) una sola vez | DEC-01, TOOL-01 |
| FUN-07 | Objetos EVA (traje, checklist, esclusa, retorno, verificación) | solar_01–04 y solar_08–10 completables | INT-07 |
| FUN-08 | Configurar `MissionManager` con los 2 `.asset` en la escena de integración | `StartMission()` arranca con 6+10 objetivos en orden | INT-08 |
| FUN-09 | Sistema de registro de pruebas (matriz P01–P16 del documento) | Cada caso P01–P16 con fecha, responsable y evidencia | FUN-02, FUN-06, MIS-02, MIS-03 |

> **Criterio de cierre del documento** (§14): un jugador completa las 2 misiones de principio a fin,
> los minijuegos notifican su éxito **una sola vez**, Core respeta el orden y la UI refleja el progreso.

### 8.1. Cobertura funciones → tareas (resumen)

| Requisito del documento | Cubierto por |
|---|---|
| RF-01, RF-03, RF-04, RF-09 (Core) | ✅ Ya implementado (nada que hacer) |
| RF-02, RF-08 (UI) | FUN-04 |
| RF-05 (reinicio + objetos) | MIS-04, FUN-05, FUN-07 |
| RF-06 (recorrer en VR) | VR-01, VR-02, DEC-01 |
| RF-07, RF-14 (compuerta/consola) | FUN-05 |
| RF-10 (despertar) | FUN-01 |
| RF-11 (EVA + MMU) | FUN-03, FUN-07 |
| RF-12 (robótico) | FUN-02 |
| RF-13 (panel) | FUN-06 |
| RF-15 (pruebas) | FUN-09 |

---

*Backlog consolidado el 2026-11-14. Es la lista viva de tareas del proyecto.*
