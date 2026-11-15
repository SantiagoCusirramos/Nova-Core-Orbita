# Contexto del proyecto: Nova Core Orbita — Experiencia VR de la ISS

**Última actualización:** 2026-11-10 (sesión 4)
**Estado:** Fase 1 ✅ (ambiente) — Fase 2 ✅ (ISS integrada y verificada) — Fase 3 🟡 (VR implementado, **sin probar en visor físico**)
**Meta inmediata:** primer build/Quest Link para validar HMD + mandos reales
**Documentos hermanos (todos en `contexto-proyecto/`):** `README.md` (índice + fase actual) · `ESTADO_ACTUAL_Y_TAREAS.md` (estado y tareas) · `HANDOFF.md` (cómo retomar)

---

## 1. Resumen y estado actual

**Nova Core Orbita** es la versión oficial del proyecto para el NASA Space Apps
Challenge 2026 (desafío *Space Mission Design Game*). Consiste en una
experiencia de realidad virtual con dos escenas:

1. **`01_Orbit`** — modo **espectador**: la ISS a escala real en un entorno
   espacial exterior, con vuelo libre (`OrbitFreeFly`).
2. **`MissionVR`** — modo **juego/misiones**: interior de la estación
   (SpaceStation5, heredada del prototipo) con el sistema de misiones de Core
   (2 misiones, 16 objetivos) y cambio de modo espectador/interactivo en
   runtime (`PlayerModeSwitcher`).

El proyecto lo desarrolla un equipo de 5 personas (ver
`Assets/Game/Docs/CONTRATO_INTEGRACION_CORE.md`): Luis (Core), Santi (VR),
Yoshua (interacciones), Diogo (escenario 3D), Camila (UI).

A diferencia de la etapa anterior, este proyecto **empieza de cero** y se
construye de forma ordenada:

- Se crea un proyecto Unity nuevo y limpio en `NovaCoreOrbita`.
- **Todo lo que Unity usa vive dentro del proyecto**, en `Nova-Core-Orbita/Assets/`
  (modelo ISS, materiales, skybox, escenas, scripts, configuración XR).
- La carpeta hermana `Recursos/` (fuera del proyecto) queda **solo como fuente original**:
  ZIPs descargados, USDZ descartado y copias de referencia. Unity **no la referencia**.
- El proyecto anterior `NASA-SPACE-APPS` (Space Station 5) queda como
  **laboratorio de pruebas y referencia**, no como base del proyecto oficial.
- La prioridad es el **ambiente espacial** y después la **integración de la
  ISS**, para terminar con una demo VR estable en Meta Quest 2.

> **Nota de organización:** la documentación de contexto está en
> `Nova-Core-Orbita/contexto-proyecto/`, **dentro del repo**, para que viaje con el proyecto.
> Unity solo importa lo que está dentro de `Assets/`, así que esa carpeta no genera
> reimports. `../Recursos/` está fuera y no se versiona.

---

## 2. Objetivo de la demo

Conseguir una primera experiencia demostrable y estable que incluya:

1. Un entorno espacial exterior creíble: estrellas, Vía Láctea, nebulosas,
   Sol y sensación de profundidad.
2. El modelo 3D de la ISS a escala real, observable desde fuera y desde
   distintos ángulos.
3. Un rig VR compatible con Meta Quest 2 (OpenXR + XR Management; **sin** XR Interaction Toolkit).
4. Un punto de observación cómodo y un encuadre que permita ver la estación
   completa.
5. Rendimiento estable en el dispositivo (objetivo: 72 Hz).

La sorpresa para el equipo es mostrar la ISS ya integrada en un ambiente
espacial terminado, no un cubo de prueba.

---

## 3. Relación con el prototipo anterior

El proyecto `Documents/Projects/NASA-SPACE-APPS` se conserva como referencia.
Contiene:

- Unity **6000.6.7f1**, URP 17.6, OpenXR 1.18, XR Management 4.7 y
  XR Interaction Toolkit 3.3.2.
- Una estación procedural propia ("Space Station 5") de ~1 km, con interiores,
  puertas, tránsito y rig VR.
- Scripts y shaders propios (`XRPlayerRig`, `SpaceStationPlayerController`,
  `BlackHoleController`, `EarthAtmosphere`, etc.).

**No se usará la estación procedural ni su escena.** Sí puede servir como
referencia para:

- Configuración de OpenXR / Quest ya validada.
- Ideas de rig VR, locomoción e interacción.
- Shaders de atmósfera, Sol o agujero negro, si se necesitan.
- Texturas de Tierra/Luna/Sol (verificar su licencia antes de reutilizarlas).

---

## 4. Recursos disponibles

> **Dónde están:** el modelo, los materiales y el skybox **usados por Unity** están en
> `Assets/NovaCore/Art/…` (ver §6). `../Recursos/` (carpeta hermana, fuera del repo) solo
> conserva las **fuentes originales**: el GLB sin importar, el USDZ descartado, los ZIP del
> skybox y copias de referencia del shader. Nada de eso lo carga el proyecto.

### 4.1. `ISS_stationary.glb` — modelo principal

| Métrica | Valor |
|---|---|
| Formato | glTF 2.0 binario (GLB), generado con Blender glTF I/O |
| Peso | 42.43 MB |
| Triángulos | ~247,547 |
| Mallas | 131 |
| Primitivas | 343 |
| Materiales | 28 del glTF + material por defecto = **29 en Unity** (12 primitivas sin material asignado) |
| Texturas | 26 embebidas: 21 de 1024×1024 y 5 de 512×512 (PNG) |
| Animaciones | 0 |
| Skins | 0 |
| Extensiones | Ninguna (sin Draco ni meshopt) |
| Tamaño real | ~112 × 69 × 59 m |
| Escala | 1 unidad = 1 metro (sin escalas de nodo) |
| Centro aproximado | (0, 5.4, −3.8) en coordenadas del modelo |

**Lectura del modelo:**

- Es un modelo **completo y detallado** del exterior de la ISS: incluye
  trusses, módulos presurizados, Canadarm2, DEXTRE, Cupola, PMA, plataformas
  externas y experimentos.
- La geometría está separada en **131 objetos**, con nombres de módulo
  reconocibles (Zvezda, Zarya, Destiny, Columbus, Kibo, Harmony, etc.). Eso
  facilita ocultar, mover o sustituir piezas.
- Los **grandes paneles solares no aparecen como nodos con nombre propio**;
  parecen formar parte de las mallas de los trusses. Para animarlos habría que
  separar geometría en Blender.
- Los nombres de malla son genéricos (`Cylinder.022`, `Cube.008`, etc.). Si se
  quiere editar por componente, conviene renombrarlos.
- Las texturas son de 1K, suficiente para VR exterior, pero se notan de cerca.
- Hay 12 primitivas sin material; al importar usarán el material por defecto.

**Escala y encuadre:** la estación mide ~112 m de envergadura. Para verla
completa en VR, el observador debe estar a unos **150–200 m**. A 100 m ya ocupa
gran parte del campo de visión. Esto define la escena: no es un objeto de
museo a 2 m, es una estructura enorme en el espacio.

**Licencia:** recurso de NASA. Conservar la ficha/fuente original y el crédito.
Los recursos 3D de NASA suelen ser de uso libre, pero conviene documentarlo.

### 4.2. `ISS_stationary.usdz` — descartado para Quest

- 15.96 MB; contiene `ISS_stationary.usdc` (10.24 MB) y 26 imágenes JPG.
- Es la variante para Apple visionOS / PolySpatial.
- Unity no lo importa de forma nativa en un proyecto URP para Quest.
- **No se usará.** Se conserva solo como referencia.

### 4.3. `deep-space-skybox-1.0.zip` — ambiente principal

- **Licencia: CC0 1.0** (dominio público, sin obligación de crédito).
- Contenido principal:
  - `hdr/deep_space_combined_4k.hdr`: 4096×2048 equirectangular, HDR real,
    pico de luminancia ~387 (Sol).
  - `png/deep_space_combined_4k.png` y `..._2k.png`: versiones tone-mapped.
  - `previews/`, `manifest.json`, `README.md`, `LICENSE.txt`.
- El Sol está horneado en el skybox, en UV (0.422, 0.4338): 28.1° del centro
  de la imagen y 11.9° sobre el horizonte.
- Instrucciones del autor para Unity:
  1. Importar el HDR.
  2. Texture Shape → **Cube**.
  3. Mapping → **Latitude-Longitude Layout (Cylindrical)**.
  4. Crear material con shader **Skybox/Cubemap**.
  5. Asignarlo en **Window > Rendering > Lighting > Environment**.
- **Recomendación Quest 2:** empezar con la versión **2K** y medir memoria y
  nitidez. Probar el HDR 4K solo si la calidad lo justifica. El HDR es oscuro
  a propósito; si hace falta, subir la exposición o la intensidad del skybox.
- El skybox **no ilumina la escena**: hace falta una luz direccional para el
  Sol. Hay que alinearla con el Sol del skybox (o rotar el skybox).

### 4.4. `Space Skybox Pack.zip` — alternativa

- **Licencia: CC0** (Jettelly).
- 36 PNG de 1024×1024: 6 cubemaps completos en variantes black/blue
  (`common`, `nebula`, `shine`).
- Ventaja: cubemaps nativos, más ligeros y fáciles de usar que un HDR
  equirectangular.
- Útil como skybox temporal, como variante visual o para comparar rendimiento
  en Quest 2.
- Para usarlo: crear un **Cubemap** con las 6 caras (`_FRONT`, `_BACK`,
  `_LEFT`, `_RIGHT`, `_UP`, `_DOWN`).

---

## 5. Decisiones tomadas

- **Proyecto nuevo y oficial** en `NovaCoreOrbita`.
- `NASA-SPACE-APPS` queda como laboratorio de pruebas; no se usa su estación.
- Modelo base: **`ISS_stationary.glb`**.
- **Sin XR Interaction Toolkit**: rig propio con OpenXR + Input System (`XRPlayerRig`).
  Se valoró instalarlo y se descartó para no rehacer el Player ni duplicar locomoción.
- **USDZ descartado** para Quest.
- Ambiente: **skybox HDR CC0** como opción principal; pack Jettelly como
  alternativa.
- No se comprarán assets por ahora.
- **Blender** se usará solo si hace falta editar geometría; primero se importa
  y se evalúa en Unity.
- **Quest 2** es el objetivo de rendimiento desde el primer día.
- Se prioriza lo visual/demostrable: ambiente + ISS antes que interiores o
  mecánicas complejas.

---

## 6. Estructura real del proyecto (estado actual)

```
NOVA-CORE-ORBITA/
├─ Assets/
│  ├─ Editor/                        ← utilidades de editor (menús "Nova Core/…")
│  │  ├─ BuildOrbitScene.cs          ← reconstruye 01_Orbit en 1 clic
│  │  ├─ SetupSpaceEnvironment.cs    ← variante alternativa del setup
│  │  ├─ ConvertIssMaterials.cs      ← convierte materiales de la ISS a URP
│  │  ├─ SetupEnvironment.cs         ← setup heredado (skybox detallado)
│  │  ├─ PlaceIss.cs / FrameIss.cs / FixCamera.cs / PlaceCamera.cs
│  ├─ Game/                          ← sistema de misiones heredado (equipo NASA Space Apps)
│  │  ├─ Scripts/
│  │  │  ├─ Core/                    ← MissionManager, MissionDefinition,
│  │  │  │  │                            MissionObjective, MissionAction,
│  │  │  │  │                            MissionActionBridge, MissionState
│  │  │  ├─ VR/                      ← XRPlayerRig, SpaceStationPlayerController,
│  │  │  │  │                            PlayerSpawnBay, PlayerModeSwitcher
│  │  │  └─ UI/                      ← ModeToggleUI
│  │  ├─ Data/Missions/              ← Mission_Energy.asset, Mission_SolarPanel.asset
│  │  ├─ Scenes/MissionVR.unity      ← LEGACY: estación procedural, NO está en Build Settings
│  │  ├─ Prefabs/Interaction/        ← CoreActionBridge_Template.prefab
│  │  ├─ Docs/                       ← contrato de integración, docs de misiones
│  │  └─ Tests/MissionDebugTester.cs
│  ├─ NovaCore/                      ← modo espectador (ISS exterior) — ESCENA ACTIVA
│  │  ├─ Art/
│  │  │  ├─ ISS/                     ← ISS_stationary.glb + Materials/ (URP)
│  │  │  ├─ Skybox/                  ← cubemaps + NovaCoreSpacePanorama.shader
│  │  │  └─ Materials/               ← M_Skybox_DeepSpace(.mat/_HDR.mat)
│  │  ├─ Scenes/01_Orbit.unity       ← escena principal (build index 0)
│  │  ├─ Scripts/
│  │  │  ├─ Player/                  ← OrbitFreeFly.cs (activo), VRMovement.cs (sin uso)
│  │  │  └─ Environment/             ← OrbitalEnvironment.cs (sin uso en escena)
│  │  └─ Settings/                   ← NovaCore_OrbitProfile.asset
│  ├─ Scenes/SampleScene.unity       ← escena original de Unity (deshabilitada en Build)
│  ├─ Settings/                      ← PC_RPAsset + Mobile_RPAsset
│  ├─ InputSystem_Actions.inputactions ← asset por defecto de Unity (no es de XRI)
│  └─ XR/                            ← OpenXR loader + settings (Standalone y Android)
├─ ProjectSettings/
│  └─ EditorBuildSettings.asset      ← 01_Orbit (0, habilitada) · SampleScene (deshabilitada)
└─ Packages/
   └─ manifest.json                  ← glTFast, OpenXR, XR Management, Input System, URP
```

**Correcciones respecto a versiones anteriores de este documento:**
- ❌ **XR Interaction Toolkit NO está instalado** (ni `com.unity.xr.interaction.toolkit`
  ni `Assets/Samples/XR Interaction Toolkit/…`). El rig se escribió sin XRI a propósito.
- ❌ `Assets/Game/Scripts/Environment/` **no existe** (StationDoor, StationTransitPod,
  CelestialOrbitController, BlackHoleController, AnimatedSpaceEnvironment no están en este proyecto).
- ❌ `Assets/Editor/SetupMissionVR.cs` no existe: `MissionVR` sigue con la estación procedural.
- ❌ `MissionVR` **no está en Build Settings**; solo `01_Orbit` está habilitada.

---

## 6b. Estado de la escena 01_Orbit (jerarquía verificada 2026-11-10)

```
01_Orbit.unity
├─ Sun                  ← Directional Light, intensidad 1.35, Soft shadows, euler (11.9, 151.8, 0)
│                          RenderSettings.sun → este objeto
├─ Player               ← posición (0, 1.6, -180) — observador a 180 m de la ISS
│  ├─ OrbitFreeFly.cs   ← vuelo libre desktop + VR (activo)
│  ├─ XRPlayerRig.cs    ← crea en runtime "Camera Offset" + "Left/Right Hand"
│  └─ Main Camera       ← localPos (0,0,0), FOV 75, near 0.05, far 5000,
│                          Clear Flags = Skybox, post-processing ON, FXAA
├─ Global Volume        ← isGlobal, perfil NovaCore_OrbitProfile conectado ✅
└─ ISS                  ← posición (0, 0, 3.8), 131 renderers, 247.5k tris, estático
   └─ _root
      └─ [módulos: Zvezda, Zarya, Destiny, Columbus, Kibo, Harmony,
          trusses S0/S1/P1/P3…/P6, Canadarm2, DEXTRE, Cupola, PMA, ELC…]
```

Métricas medidas en Play Mode: ISS a **180.1 m**, bounds **112.0 × 69.0 × 58.8 m**,
viewport x 0.39–0.61 / y 0.37–0.67 (centrada), **29 materiales (22 con textura)**,
0 materiales magenta.

> ⚠️ El `Player` **no tiene `CharacterController`**: `SpaceStationPlayerController` no se puede
> añadir tal cual, y no debe convivir con `OrbitFreeFly` sin un `PlayerModeSwitcher`.

### 6c. Estado real del stack VR (resumen)

| Pieza | Estado |
|---|---|
| OpenXR loader (PC + Android) | ✅ activo, `InitManagerOnStart = 1` |
| Render Mode | ✅ Single Pass Instanced |
| Perfiles de mando | ✅ Oculus Touch, Meta Quest Touch Plus/Pro (+ Quest Support en Android) |
| Tracking de HMD | ✅ implementado en `XRPlayerRig` vía `TrackedPoseDriver` (runtime) |
| Tracking de mandos | ✅ posición/rotación creadas en runtime; ❌ sin modelos visuales |
| Locomoción VR | ✅ sticks + snap turn + turbo en `OrbitFreeFly` |
| Interacción (grab/ray/UI/teleport) | ❌ no implementada (no hay XRI) |
| Prueba en visor físico | ❌ pendiente (solo probado en Editor sin HMD) |

Detalle completo en `contexto-proyecto/ESTADO_ACTUAL_Y_TAREAS.md` §3.

---

## 7. Plan de trabajo por fases

### Fase 0 — Crear el proyecto (en curso)

- Crear el proyecto Unity con **URP 3D**.
- Versión recomendada: **6000.6.7f1** (la misma del prototipo, para reaprovechar
  la configuración XR ya probada) o la LTS más reciente.
- Instalar y configurar:
  - OpenXR + XR Management.
  - Input System.
  - Android Build Support (SDK/NDK/OpenJDK) para Quest.
  - ~~XR Interaction Toolkit + Starter Assets~~ → **descartado**: el rig se implementó sin XRI
    (ver §6c y la nota de §7 Fase 3).
- Activar OpenXR en Desktop y Android, y **Meta Quest Support** en Android.
- Pasar **Project Validation → Fix All**.
- Crear la estructura de carpetas y la escena `01_Orbit`.
- (Recomendado) `git init` con un `.gitignore` de Unity y commit inicial.

### Fase 1 — Ambiente espacial (prioridad)

1. Copiar el skybox a `Assets/NovaCore/Art/Skybox/`.
2. Configurar la textura como **Cube** + **Latitude-Longitude**.
3. Crear el material `M_Skybox_DeepSpace` con `Skybox/Cubemap`.
4. Asignarlo en **Lighting > Environment**.
5. Añadir una **Directional Light** y alinearla con el Sol del skybox.
6. Ajustar tonemapping, exposición y bloom en el volumen URP.
7. Probar en editor y en Quest 2 (2K vs 4K).
8. (Opcional) Añadir una Tierra de fondo reutilizando texturas del prototipo
   anterior, verificando su licencia.

### Fase 2 — Integración de la ISS

1. Instalar **Unity glTFast** (`com.unity.cloud.gltfast`) desde Package Manager.
   - Alternativa: convertir el GLB a FBX en Blender si glTFast da problemas.
2. Importar `ISS_stationary.glb` en `Assets/NovaCore/Art/ISS/`.
3. Revisar escala, orientación y pivote; medir con una referencia de 1.8 m.
4. Verificar que los materiales se vean correctos en URP.
5. Marcar la geometría como **Static** y activar **static batching**.
6. Ajustar texturas: **ASTC 6×6 o 4×4**, mipmaps, max size 1024.
7. Colocar la ISS a ~150–200 m del punto de observación.
8. Revisar draw calls con **Frame Debugger** y el **Profiler**.

### Fase 3 — VR y locomoción

- Montar **XR Origin (VR)** con `TrackedPoseDriver` para la cámara.
- Configurar mandos, interactores y UI básica si hace falta.
- Locomoción: teleportación o movimiento continuo + giro por pasos (snap turn).
- Definir un punto de spawn cómodo con la estación bien encuadrada.
- Probar en editor con el **XR Interaction Simulator**.

> **Estado real (2026-11-10):** se implementó **sin XR Interaction Toolkit**: `XRPlayerRig`
> construye el rig en runtime (cámara + `Camera Offset` + `Left/Right Hand` con
> `TrackedPoseDriver`) y `OrbitFreeFly` hace la locomoción (sticks + snap turn + turbo).
> **No hay interactores, UI en VR ni teletransporte.** Pendiente: probar en visor físico.

### Fase 4 — Prueba en Quest 2 y optimización

- Build Android con OpenXR y Meta Quest Support.
- Objetivo: **72 Hz estables**.
- Single Pass Instanced, sin sombras en tiempo real o muy limitadas.
- Presupuesto: ≤250k triángulos visibles, ≤100–150 draw calls, texturas ASTC.
- Evitar post-procesado pesado.
- Probar en el dispositivo y ajustar según resultados.

### Fase 5 — Ampliaciones

- Animar paneles solares (requiere separar geometría en Blender).
- Añadir naves, vehículos o elementos de acoplamiento.
- Interiores explorables.
- Interacción con componentes, brazos robóticos, etc.

---

## 8. Presupuesto técnico para Quest 2

| Aspecto | Objetivo |
|---|---|
| FPS | 72 estables (90 si se puede) |
| Triángulos visibles | ≤ 250k |
| Draw calls | ≤ 100–150 (ideal); usar static batching |
| Materiales | Reducir o usar atlas si es necesario |
| Texturas ISS | ASTC 6×6 / 4×4, 1024 px |
| Skybox | 2K para Quest; 4K solo si el rendimiento lo permite |
| Luces | 1 direccional + ambiente; sombras en tiempo real mínimas |
| Post-proceso | Bloom y tonemapping ligeros |
| Render | Single Pass Instanced |

---

## 9. Riesgos y puntos pendientes

- **Licencia de NASA:** confirmar la ficha original y conservar el crédito.
- **glTFast + URP:** verificar que los materiales se generen correctamente; si
  aparecen magenta, convertirlos a URP/Lit.
- **Draw calls:** 343 primitivas y 28 materiales es el punto más delicado para
  Quest 2. El static batching es clave.
- **Escala:** el modelo está a escala real (~112 m); hay que ajustar el punto
  de observación, no encogerlo sin criterio.
- **Sol horneado en el skybox:** alinear la luz direccional o rotar el skybox.
- **Jerarquía y nombres:** los nodos tienen nombres de módulo, pero las mallas
  son genéricas. Para editar por pieza, renombrar.
- **Paneles solares:** no están separados; animarlos exige trabajo en Blender.
- **Interiores:** el modelo es solo exterior. Los interiores son una fase
  posterior.
- **Blender:** decidir si se edita el GLB o se reconstruye; primero evaluar en
  Unity.
- **Código heredado:** el proyecto anterior tiene scripts valiosos; reutilizar
  solo después de revisarlos y adaptarlos.

---

## 10. Lo que falta — lista priorizada

> **Actualizado 2026-11-10 (sesión 4):** la compilación, el skybox y la ISS ya están
> verificados en Play Mode. **XR Interaction Toolkit NO está instalado** (la nota
> anterior que lo daba por resuelto era incorrecta). La lista refleja el estado real.

### 🔴 Bloqueante (sin esto no hay VR en el Quest)

1. **Primer build Android / Quest Link** — conectar el Quest por USB, activar modo
   desarrollador, `File → Build Settings → Android → Switch Platform → Build And Run`.
   Verificar en consola los logs `[XRPlayerRig] HMD: …` y `[XRPlayerRig] Mando …: …`.
2. **Modelos visuales de mando** — asignar prefabs en `XRPlayerRig`
   (`leftControllerModelPrefab` / `rightControllerModelPrefab`), hoy vacíos.
3. **Decidir la estrategia de interacción** — instalar XRI (obliga a rehacer el rig
   con `XR Origin`) **o** escribir interactores propios sobre `InputDevices`
   (coherente con el enfoque actual sin XRI).

### 🟡 Necesario para que la demo funcione bien en Quest

3. **Perfil URP para Android** — en `Edit → Project Settings → Quality`,
   columna Android debe apuntar a `Mobile_RPAsset`. En ese asset: desactivar
   HDR, MSAA a 4x, post-processing mínimo o desactivado.
4. **Single Pass Instanced** — en `Project Settings → XR Plug-in Management →
   OpenXR → Render Mode → Single Pass Instanced`. Evita doblar los draw calls
   en estéreo VR.
5. **Texturas ISS en ASTC** — las texturas del GLB están en PNG sin comprimir
   para Android. En `Assets/NovaCore/Art/ISS/Materials/` cada textura necesita
   Override para Android: formato **ASTC 6×6**, max size 1024. Sin esto el
   Quest puede quedarse sin memoria.
6. **Verificar draw calls** — con el Frame Debugger en dispositivo confirmar
   que el static batching agrupa correctamente los 343 sub-meshes de la ISS.

### 🟢 Mejoras visuales (cuando lo anterior funcione)

7. **Rotación lenta de paneles solares** — script simple que rote los nodos
   `panel_*` en eje Y (~1°/seg). Dar vida a la escena sin coste de rendimiento.
8. **ISS como Prefab** — guardar en `Assets/NovaCore/Prefabs/ISS.prefab` para
   reusar en futuras escenas limpiamente.
9. **Punto de spawn alternativo** — poder ver la ISS desde arriba, lateral y
   frontal. Varios puntos de teleport o un selector simple.
10. **Tierra de fondo** — esfera con textura de la Tierra en el horizonte
    inferior. Da contexto espacial real y profundidad a la escena.

### ⚪ Fase futura (no tocar hasta tener lo anterior)

11. Interiores explorables de la ISS (requiere modelado en Blender desde cero)
12. Animación del Canadarm2 (requiere separar geometría en Blender)
13. Interacción con componentes de la estación (agarrar/mirar piezas) — requiere el
    sistema de interacción del punto 3
14. Mecánicas de juego / misiones — *base ya existe en `Assets/Game/Scripts/Core/`
    (MissionManager + 2 misiones); falta conectarla a gameplay real*
15. UI espacial / HUD en VR

---

## 10b. Scripts de runtime disponibles

| Script | Ubicación | ¿En `01_Orbit`? | Estado real |
|---|---|---|---|
| `OrbitFreeFly.cs` | `Assets/NovaCore/Scripts/Player/` | ✅ Sí | Movimiento libre desktop + VR. Único script de locomoción activo |
| `XRPlayerRig.cs` | `Assets/Game/Scripts/VR/` | ✅ Sí | Crea rig HMD + mandos en runtime (sin XRI) |
| `VRMovement.cs` | `Assets/NovaCore/Scripts/Player/` | ❌ No | Alternativa a `OrbitFreeFly`; **no añadir ambas** |
| `SpaceStationPlayerController.cs` | `Assets/Game/Scripts/VR/` | ❌ No | FPS con colisión; requiere `CharacterController` |
| `PlayerSpawnBay.cs` | `Assets/Game/Scripts/VR/` | ❌ No | Base para spawn/teleport |
| `PlayerModeSwitcher.cs` | `Assets/Game/Scripts/VR/` | ❌ No | Alterna espectador/interactivo |
| `ModeToggleUI.cs` | `Assets/Game/Scripts/UI/` | ❌ No | Botón uGUI (no clicable en VR sin módulo XR) |
| `OrbitalEnvironment.cs` | `Assets/NovaCore/Scripts/Environment/` | ❌ No | Tierra/Luna/Sol orbitales; ningún GameObject lo usa |
| `MissionManager` + Core | `Assets/Game/Scripts/Core/` | ❌ No | Solo referenciado en `MissionVR` (legacy) |

> Los scripts marcados "❌ No" compilan y están listos, pero **no forman parte de la demo actual**.
> `VRMovement` y `SpaceStationPlayerController` no deben añadirse al `Player` sin quitar antes
> `OrbitFreeFly` (duplicarían el movimiento).

## 10c. Scripts de editor disponibles

| Script | Ubicación | Para qué sirve |
|---|---|---|
| `SetupEnvironment.cs` | `Assets/Editor/` | Recrea escena 01_Orbit desde cero |
| `PlaceIss.cs` | `Assets/Editor/` | Instancia ISS en la escena |
| `ConvertIssMaterials.cs` | `Assets/Editor/` | Convierte materiales a URP/Lit |
| `FixCamera.cs` | `Assets/Editor/` | Corrige la cámara si se rompe |
| `FrameIss.cs` | `Assets/Editor/` | Ajusta encuadre de cámara |
| `PlaceCamera.cs` | `Assets/Editor/` | Reposiciona la cámara |

---

## 11. Registro de cambios

- **2026-11-05 (mañana):** se redefine el proyecto como oficial. Se descarta
  la estación procedural anterior como base, se prioriza el ambiente espacial,
  se inspeccionan los recursos y se documentan las métricas reales del GLB.

- **2026-11-05 (tarde) — Problema: Display 1 en negro / escena rota:**
  Al reabrir el proyecto, la escena `01_Orbit.unity` no mostraba nada en
  Display 1. Causas identificadas:
  1. El `Global Volume` tenía `sharedProfile: {fileID: 0}` (nulo) — el
     `NovaCore_OrbitProfile.asset` existía en disco pero no estaba conectado,
     por lo que el post-processing no aplicaba.
  2. La ISS no estaba serializada en el `.unity` (solo Sun, Main Camera y
     Global Volume).
  3. La escena nunca había sido commiteada a git (archivo untracked).

  **Solución aplicada:**
  - Se reconectó el `VolumeProfile` directamente en el YAML de la escena
    (`sharedProfile: {fileID: 11400000, guid: 5e8a57a94af32744ea893a0c9bf52b36}`).
  - Se creó un script `AutoRestoreDisplay1.cs` con `[InitializeOnLoad]` que
    ejecutó `SetupEnvironment.Run()` + `PlaceIss.Run()` + `VerifyScene.Run()`
    al compilar, recreando la escena completa con todos los objetos correctos.
  - Resultado verificado: Sun ✅, Main Camera ✅, Global Volume con perfil ✅,
    ISS instanciada (131 renderers, 247k tris, 29 materiales) ✅.
  - El script `AutoRestoreDisplay1.cs` se autoeliminó tras ejecutarse.

- **2026-11-05 (tarde) — Limpieza de código basura:**
  Se eliminaron ~45 scripts de diagnóstico generados por agentes IA durante
  sesiones anteriores (`Assets/Editor/` y `AgentScripts/`). Se conservaron
  solo los útiles: `SetupEnvironment.cs`, `PlaceIss.cs`, `ConvertIssMaterials.cs`,
  `FixCamera.cs`, `FrameIss.cs`, `PlaceCamera.cs`. También se borraron logs
  temporales de la raíz del proyecto.

- **2026-11-05 (noche) — Movimiento libre (OrbitFreeFly):**
  Se creó `Assets/NovaCore/Scripts/Player/OrbitFreeFly.cs` — script de
  movimiento modo creativo/fantasma sin gravedad ni colisiones. Funciona en
  desktop (WASD + click derecho para rotar + scroll para velocidad) y en VR
  Meta Quest 2 (thumbstick izquierdo para mover, derecho para snap turn ±45°
  y subir/bajar, gatillo izquierdo para turbo). Lógica extraída y adaptada
  de `SpaceStationPlayerController.cs` y `XRPlayerRig.cs` del proyecto
  referencia `NASA-SPACE-APPS`.

  Error de compilación corregido: `CS0104 CommonUsages ambiguous reference`
  → solucionado con alias `using XRCommonUsages = UnityEngine.XR.CommonUsages`.

  Escena `01_Orbit.unity` reestructurada directamente en YAML:
  - Creado `Player` (fileID 88800000) en `(0, 1.6, -180)` con `OrbitFreeFly`.
  - `Main Camera` (fileID 699924558) movida como hijo de `Player`,
    localPosition `(0, 0, 0)`.
  - `SceneRoots` actualizado: apunta a `Player` en lugar de a la cámara suelta.

- **2026-11-09 (sesión 2) — Diagnóstico y corrección ISS + modo espectador:**

  **Diagnóstico del problema "modelo incorrecto":**
  - La escena `MissionVR.unity` contenía toda la estación procedural `SpaceStation5`
    (la del proyecto anterior) con todos sus módulos, habitaciones, corredores, etc.
    Era esa escena la que se veía al abrir el proyecto, no `01_Orbit`.
  - El `Player_Astronaut_VR` en `MissionVR` tenía el `SpaceStationPlayerController`
    referenciado con el namespace `SpaceStation5` (antiguo), que Unity marcaba como
    "Missing Script" al entrar en modo Play.
  - El modo espectador (`OrbitFreeFly`) no estaba presente en `MissionVR`.
  - La escena `01_Orbit` está correcta: ISS oficial + skybox + `OrbitFreeFly` ✅.

  **Correcciones realizadas:**
  1. `PlayerModeSwitcher.cs`: campo `initialMode` cambiado de `private` a `public`
     y el valor por defecto cambiado a `Spectator` (vuelo libre al arrancar).
  2. Creado `Assets/Game/Scripts/UI/ModeToggleUI.cs` — botón de UI para cambiar
     de modo en pantalla (esquina inferior izquierda). Muestra el modo activo y
     cambia al opuesto al presionarlo. Se actualiza automáticamente vía evento
     `PlayerModeSwitcher.OnModeChanged`.
  3. Creado `Assets/Editor/SetupMissionVR.cs` — script de editor que reconstruye
     `MissionVR.unity` desde cero con:
     - ISS_stationary.glb (modelo oficial)
     - Skybox Deep Space + Sol alineado
     - Global Volume con NovaCore_OrbitProfile
     - Player con `OrbitFreeFly` + `SpaceStationPlayerController` + `PlayerModeSwitcher`
     - Canvas con botón de cambio de modo

     > ⚠️ **Corrección (2026-11-10):** ese script **ya no existe** en el proyecto y
     > `MissionVR` sigue conteniendo la estación procedural. La reconstrucción descrita
     > aquí **no se llegó a aplicar**. No busques el menú "Reconstruir MissionVR": no está.

  **Para aplicar los cambios, en Unity:**
  Ir al menú: **Nova Core → Reconstruir MissionVR (ISS + Modos)**
  Esto elimina toda la SpaceStation5 y crea la escena correcta con la ISS real.

  > ⚠️ Obsoleto: ese menú ya no existe (ver corrección arriba).

  **Limpieza realizada:**
  - Revisado el proyecto `NASA-SPACE-APPS`: NO contiene ningún modelo 3D de
    ISS alternativo. El único modelo ISS en NOVA-CORE-ORBITA es el oficial
    `Assets/NovaCore/Art/ISS/ISS_stationary.glb` (42 MB, 247k tris) — correcto
    y sin duplicado.
  - Corregidos los namespaces de los scripts VR importados al proyecto:
    todos tenían `namespace SpaceStation5` y ahora usan `namespace NovaCore`:
    - `Assets/Game/Scripts/VR/SpaceStationPlayerController.cs` → `NovaCore`
    - `Assets/Game/Scripts/VR/XRPlayerRig.cs` → `NovaCore`
    - `Assets/Game/Scripts/VR/PlayerSpawnBay.cs` → `NovaCore`
  - Corregida la referencia en `PlayerModeSwitcher.cs`:
    `SpaceStation5.SpaceStationPlayerController` → `SpaceStationPlayerController`
    (ya en el mismo namespace `NovaCore`).

  **Nuevo script creado: `VRMovement.cs`:**
  - Ubicación: `Assets/NovaCore/Scripts/Player/VRMovement.cs`
  - Propósito: script dedicado de movimiento VR para la escena `01_Orbit`.
    Es la versión mejorada y comentada de `OrbitFreeFly`, diseñada
    específicamente para cuando tengas los lentes puestos.
  - Diferencias clave respecto a `OrbitFreeFly`:
    1. **La rotación de cabeza (pitch/yaw del HMD)** la delega al
       `TrackedPoseDriver` de Unity — eso significa que mover físicamente
       la cabeza con el Quest 2 mueve la cámara sin código adicional.
       `VRMovement` solo gestiona el snap turn del body (el transform raíz).
    2. **Movimiento orientado a la mirada**: avanzar/retroceder va hacia
       donde apunta la cámara (cabeza), no hacia donde apunta el body.
       Perfecto para explorar la ISS en 3D.
    3. Mismo esquema de controles que `OrbitFreeFly` para el thumbstick
       pero con documentación clara de la jerarquía esperada.
  - **Setup en Unity** (ver comentario en el script):
    1. Player (vacío) ← añadir `VRMovement`
    2. Camera Offset (hijo vacío)
    3. Main Camera (hijo de Camera Offset) ← añadir `TrackedPoseDriver`
       con Position: `<XRHMD>/centerEyePosition`
       y Rotation: `<XRHMD>/centerEyeRotation`

- **Etapa anterior:** prototipo `NASA-SPACE-APPS` con Space Station 5,
  OpenXR y entorno cósmico; se conserva como laboratorio de pruebas.

- **2026-11-11 (sesión 3) — Implementación de skybox deep space + scripts VR:**

  **Assets copiados a Assets/ (antes solo estaban en Recursos/):**
  - `T_Skybox_DeepSpace_2K.png` → `Assets/NovaCore/Art/Skybox/`
  - `T_Skybox_DeepSpace_4K.hdr` → `Assets/NovaCore/Art/Skybox/`
  - `T_Skybox_DeepSpace_4K_LDR.png` → `Assets/NovaCore/Art/Skybox/`
  - `NovaCoreSpacePanorama.shader` → `Assets/NovaCore/Art/Skybox/`
  - ISS ya estaba importada ✅ (42 MB GLB)
  - Materiales URP de la ISS generados (28 mats en `Assets/NovaCore/Art/ISS/Materials/`)
  - `M_Skybox_DeepSpace.mat` y `M_Skybox_DeepSpace_HDR.mat` creados en `Assets/NovaCore/Art/Materials/`
  - `NovaCore_OrbitProfile.asset` creado en `Assets/NovaCore/Settings/`

  **Scripts nuevos creados:**
  - `Assets/NovaCore/Scripts/Player/VRMovement.cs` — movimiento VR que delega rotación al TrackedPoseDriver
  - `Assets/NovaCore/Scripts/Environment/OrbitalEnvironment.cs` — Tierra, Luna, Sol con física real
  - `Assets/Game/Scripts/VR/XRPlayerRig.cs` — rig VR runtime (reescrito sin XR Interaction Toolkit)
  - `Assets/Game/Scripts/VR/SpaceStationPlayerController.cs` — portado de NASA-SPACE-APPS
  - `Assets/Game/Scripts/VR/PlayerModeSwitcher.cs` — alterna espectador/interactivo
  - `Assets/Game/Scripts/VR/PlayerSpawnBay.cs` — puntos de teleport
  - `Assets/Game/Scripts/UI/ModeToggleUI.cs` — botón UI de cambio de modo
  - `Assets/Editor/BuildOrbitScene.cs` — setup completo de la escena en 1 clic

  **Error resuelto:** `CS0234 'Interaction' does not exist in 'UnityEngine.XR'`
  — `XRPlayerRig.cs` fue reescrito eliminando la dependencia de
  `com.unity.xr.interaction.toolkit` (no instalado en este proyecto).

  **Estado al cierre de sesión:** assets en su lugar, scripts escritos, integración
  en la escena `01_Orbit` **sin confirmar** (no se pudo probar en Play).
  Próxima sesión: verificar compilación → ejecutar `Nova Core → BUILD 01_Orbit` → probar.

- **2026-11-10 (sesión 4) — Cierre de la integración y verificación real:**

  **Errores corregidos:**
  - `CS0104` ×5 (`CommonUsages` ambiguo) en `SpaceStationPlayerController.cs`
    → alias `using XRCommonUsages = UnityEngine.XR.CommonUsages;`.
  - `CS0618` ×2 (`FindObjectsByType<Light>(FindObjectsSortMode.None)` obsoleto) en
    `OrbitalEnvironment.cs:177` → `FindObjectsByType<Light>(FindObjectsInactive.Exclude)`.

  **Integración aplicada y guardada en `01_Orbit`:**
  - `Global Volume` → `sharedProfile` = `NovaCore_OrbitProfile` (guid `5e8a57a9…`).
  - `RenderSettings.skybox` = `M_Skybox_DeepSpace` (cubemap 2K, Cylindrical, 2048, mips).
  - `Sun` = intensidad 1.35, Soft shadows, euler (11.9, 151.8, 0), `RenderSettings.sun`.
  - `Main Camera` = Clear Flags Skybox, FOV 75, near 0.05, far 5000, post-proceso + FXAA.
  - `Player` = `OrbitFreeFly` + `XRPlayerRig` (se descartó añadir `VRMovement` para no
    duplicar el movimiento).

  **Verificación en Play Mode (medida, no impresión visual):**
  - ISS a 180.1 m, bounds 112.0 × 69.0 × 58.8 m, viewport 0.39–0.61 × 0.37–0.67.
  - 131 renderers, 247,547 triángulos, 29 materiales (22 con textura), 0 magenta.
  - Compilación final: 0 errores / 0 warnings. Escena guardada (`isDirty: false`).

  **Estado VR:** rig y configuración OpenXR listos (HMD + tracking de mandos por
  `TrackedPoseDriver`, locomoción por sticks), **sin interacción** (no hay XRI) y
  **sin probar en visor físico**. Detalle y plan en `ESTADO_ACTUAL_Y_TAREAS.md` §3.

  **Documentación:** este documento se corrigió para eliminar afirmaciones falsas
  (XRI instalado, carpeta `Game/Scripts/Environment`, `MissionVR` en Build Settings)
  y se añadió `HANDOFF.md` como guía de continuación.

  **Reorganización (misma fecha):** los tres `.md` de contexto se movieron de
  `../Recursos/` a `Nova-Core-Orbita/contexto-proyecto/`, **dentro del repo**, y se añadió
  `README.md` con el índice y la tabla de fases. `../Recursos/` queda solo como fuente original.

  > 📋 Ver `contexto-proyecto/ESTADO_ACTUAL_Y_TAREAS.md` para la lista completa de tareas y troubleshooting.
