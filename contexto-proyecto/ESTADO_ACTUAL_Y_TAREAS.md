# Nova Core Órbita — Estado actual y tareas pendientes

**Última actualización:** 2026-11-10 (sesión 4)
**Proyecto Unity:** `C:\Users\fdx_dracox\Documents\Projects\NovaCoreOrbita\Nova-Core-Orbita`
**Escena principal:** `Assets/NovaCore/Scenes/01_Orbit.unity`
**Estado general:** 🟢 Fase 1 (ambiente) y Fase 2 (ISS) **completas y verificadas en Play Mode**.
🟡 Fase 3 (VR) **implementada en código y configuración, sin probar todavía en visor físico**.

> Documento hermano con la visión, historia y decisiones: `contexto-proyecto/contexto_nova_core_orbita_vr.md`
> Guía rápida para retomar el proyecto (humano o agente IA): `contexto-proyecto/HANDOFF.md`
> Índice y fase actual: `contexto-proyecto/README.md`

---

## 1. TL;DR — qué hay y cómo se comprobó

| Cosa | Estado | Cómo se verificó |
|---|---|---|
| Compilación del proyecto | ✅ **0 errores, 0 warnings** | `script recompile_status` + `console status` (groundTruth) |
| Skybox deep space en la escena | ✅ Activo | `RenderSettings.skybox = M_Skybox_DeepSpace` en Play Mode; GUID del material guardado en el `.unity` |
| ISS visible y texturizada | ✅ 131 renderers, 29 mats (22 con textura, 7 color plano) | Inspección en Play Mode + estadística de píxeles del render |
| Encuadre del observador | ✅ ISS a 180.1 m, centrada (viewport 0.39–0.61 × 0.37–0.67) | `WorldToViewportPoint` de los 8 vértices del bounds |
| Post-proceso (Volume) | ✅ `NovaCore_OrbitProfile` conectado | GUID `5e8a57a9…` en el YAML de la escena |
| Rig VR (`XRPlayerRig`) | 🟡 Instalado en el Player, se construye en runtime | Código + jerarquía; **no probado con visor** |
| Reconocimiento de visor y mandos | 🟡 Configurado (OpenXR + perfiles), **sin validar en hardware** | Config del proyecto + código; ver §3 |
| Interacción con mandos (agarrar, apuntar, UI) | ❌ **No existe** | No hay XRI ni interactores en el proyecto |
| Escena `MissionVR` (legacy) | ⚠️ Existe, sigue siendo la estación procedural, **no está en Build Settings** | Inspección del `.unity` (910 GameObjects) |

---

## 2. Qué hacer para ver la demo (30 segundos)

1. Abrir Unity **6000.6.7f1** con el proyecto.
2. Abrir la escena `Assets/NovaCore/Scenes/01_Orbit.unity` (Unity suele abrir una escena "Untitled" al arrancar; hay que abrirla a mano).
3. **Play**.

Controles de escritorio (`OrbitFreeFly`):

| Entrada | Acción |
|---|---|
| W/S o ↑/↓ | Avanzar / retroceder |
| A/D o ←/→ | Lateral |
| E/Q o PageUp/PageDown | Subir / bajar |
| Click derecho + ratón (o cursor bloqueado) | Rotar cámara |
| Shift | Turbo (×4) |
| Rueda | Ajustar velocidad base (2–300 m/s) |
| ESC | Bloquear / soltar cursor |

La ISS debe verse a ~180 m, centrada, con estrellas y el Sol del skybox detrás.

---

## 3. VR: qué está implementado y qué no (estado real)

Esta es la parte que hay que tener clara antes de seguir. **Respuesta corta: el proyecto está
preparado para reconocer visor y mandos, pero eso no se ha confirmado con un visor puesto.**
Lo que sí está verificado es que compila, que la ruta de escritorio funciona y que la
configuración de OpenXR es coherente.

### 3.1 Visor (HMD)

**Implementado:**
- OpenXR activo como loader para **Standalone (PC)** y **Android**, con `InitManagerOnStart = 1`
  (`Assets/XR/XRGeneralSettingsPerBuildTarget.asset`).
- `XRPlayerRig` (en el objeto `Player`) detecta si hay XR activo
  (`XRGeneralSettings.Instance.Manager.activeLoader != null || XRSettings.isDeviceActive`).
- Cuando lo detecta, en runtime:
  - crea `Camera Offset` y cuelga ahí `Main Camera`;
  - añade un `TrackedPoseDriver` a la cámara con
    `<XRHMD>/centerEyePosition`, `<XRHMD>/centerEyeRotation`, `<XRHMD>/trackingState`;
  - intenta `Floor` tracking; si el runtime no lo soporta, usa altura de respaldo 1.6 m.
- La rotación de cabeza la hace el `TrackedPoseDriver`; el código solo mueve el cuerpo.

**No verificado:** no se ha ejecutado con un Quest conectado. En el Editor sin visor,
`XRSettings.isDeviceActive` es `false` y el juego corre en modo escritorio. El aviso
`XR: Error setting active audio output driver` que aparece al dar Play es la inicialización
de XR sin dispositivo, y es inofensivo.

### 3.2 Mandos

**Implementado:**
- `XRPlayerRig` crea en runtime dos objetos `Left Hand` y `Right Hand` con `TrackedPoseDriver`
  enlazados a `<XRController>{LeftHand}/devicePosition|deviceRotation` y lo mismo para la mano
  derecha. Es decir, **la posición y rotación de los mandos se rastrean**.
- `OrbitFreeFly` lee los mandos por la API de XR (`InputDevices` + `CommonUsages`):

| Entrada VR | Acción |
|---|---|
| Stick izquierdo X/Y | Lateral / avanzar-retroceder (relativo a la mirada) |
| Stick derecho X | Snap turn ±45° |
| Stick derecho Y | Subir / bajar |
| Gatillo izquierdo | Turbo |

**Limitaciones actuales:**
- **No hay modelos visuales de mandos** (los campos `leftControllerModelPrefab` /
  `rightControllerModelPrefab` de `XRPlayerRig` están vacíos). Los mandos se rastrean pero no se ven.
- No hay *hand tracking* real: `PalmPoseInteraction` y `HandCommonPosesInteraction` están
  desactivados en OpenXR. El perfil `HandInteractionProfile` sí está activado, pero es un perfil
  de entrada, no tracking de manos con mallas.
- Sin *haptics*, sin rebinding en runtime, sin simulación de XR en el Editor (Mock Runtime desactivado).

### 3.3 Interacción (agarrar, apuntar, UI, teletransporte)

**No existe.** Concretamente:
- `com.unity.xr.interaction.toolkit` **no está instalado** y no hay `XR Origin`, ni `XR Ray Interactor`,
  ni `XR Grab Interactable`, ni `XR UI Input Module`.
- `ModeToggleUI` es uGUI (Canvas + botón) pensado para ratón; **no está en `01_Orbit`** y no sería
  clicable en VR sin un `Tracked Device Graphic Raycaster` + `XR UI Input Module`.
- `PlayerSpawnBay` (puntos de spawn/teleport) existe pero **no está en la escena**.
- No hay ninguna acción de "agarrar" implementada.

### 3.4 Qué falta para decir "VR completo"

1. Primer build a Quest 2 (o Quest Link) y verificar HMD + mandos reales.
2. Modelos de mando (o manos) asignados en `XRPlayerRig`.
3. Decidir interacción: instalar XRI (implica rehacer el rig con `XR Origin`) **o** escribir
   interactores propios sobre `InputDevices` (más simple, coherente con el enfoque actual).
4. UI en VR (World Space Canvas + raycaster XR) si se quiere el cambio de modo dentro del visor.
5. Teletransporte / puntos de observación (ya existe la base en `PlayerSpawnBay`).

### 3.5 Cómo probarlo cuando llegue el visor

- **Rápido (PC):** Quest Link/Air Link → Play en el Editor. Si OpenXR inicializa bien,
  `XRSettings.isDeviceActive` pasa a `true` y `XRPlayerRig` construye el rig solo.
- **Real (Android):** `File → Build Settings → Android → Switch Platform → Build And Run`
  (módulo Android instalado ✅, IL2CPP + ARM64 ya configurados, minSdk 26).
- Comprobar en consola los logs `[XRPlayerRig] HMD: …` y `[XRPlayerRig] Mando izquierdo/derecho: …`
  que imprime al construir el rig. Si aparecen "no detectado", el problema es de runtime/perfiles,
  no del código.

---

## 4. Estado verificado de `01_Orbit.unity`

Jerarquía real (guardada en disco, `isDirty: false`):

```
01_Orbit
├── Sun                 Light direccional · intensidad 1.35 · Soft shadows · euler (11.9, 151.8, 0)
│                       RenderSettings.sun → este objeto
├── Player              posición (0, 1.6, -180) · rotación identidad
│   ├── OrbitFreeFly    vuelo libre (desktop + VR)
│   ├── XRPlayerRig     rig VR runtime (crea Camera Offset + mandos al detectar XR)
│   └── Main Camera     localPos (0,0,0) · FOV 75 · near 0.05 · far 5000
│                       Clear Flags = Skybox · Post Processing ON · FXAA
├── Global Volume       isGlobal · profile = NovaCore_OrbitProfile (guid 5e8a57a9…)
└── ISS                 posición (0, 0, 3.8) · static · bounds 112.0 × 69.0 × 58.8 m
    └── _root
        └── [módulos: Zvezda, Zarya, Destiny, Columbus, Kibo, Harmony, trusses, Canadarm2, DEXTRE, Cupola…]
```

Datos medidos en Play Mode:

| Métrica | Valor |
|---|---|
| Distancia cámara → centro de la ISS | **180.1 m** |
| ISS en pantalla (viewport) | x 0.39–0.61, y 0.37–0.67 → centrada |
| Renderers | 131 |
| Triángulos | 247,547 |
| Materiales | 29 (22 con textura, 7 con color plano del glTF) |
| Shaders | todos `Shader Graphs/glTF-pbrMetallicRoughness` (0 magenta) |
| Skybox | `M_Skybox_DeepSpace` (cubemap 2K, Cylindrical, 2048, mips) |
| Post-proceso | Tonemapping, Bloom, ColorAdjustments, Vignette |
| Build Settings | `01_Orbit` = índice 0 **habilitada**; `SampleScene` deshabilitada; `MissionVR` **no incluida** |

> ⚠️ El `Player` **no tiene `CharacterController`**. `SpaceStationPlayerController` lo requiere,
> así que no se puede añadir sin más (y además duplicaría el movimiento con `OrbitFreeFly`).

---

## 5. Configuración XR del proyecto (verificada)

| Ajuste | Valor | Dónde |
|---|---|---|
| Loader XR | OpenXR en Standalone y Android, init on start | `Assets/XR/XRGeneralSettingsPerBuildTarget.asset` |
| Render Mode | **Single Pass Instanced** (`m_renderMode: 1`) | `Assets/XR/Settings/OpenXR Package Settings.asset` |
| Perfiles de mando activos (Standalone) | Oculus Touch, Meta Quest Touch Plus, Meta Quest Touch Pro, Microsoft Motion, HP Reverb G2, HTC Vive, Valve Index, KHR Simple | idem |
| Perfiles activos (Android) | Oculus Touch + **Oculus Quest Support** | idem |
| Perfiles de mano | `HandInteractionProfile` ON · `PalmPose` OFF · `HandCommonPoses` OFF | idem |
| Mock Runtime (simulador) | **OFF** | idem |
| Input System | `activeInputHandler: 1` (solo Input System nuevo) | `ProjectSettings/ProjectSettings.asset` |
| Android | IL2CPP · ARM64 · minSdk 26 · targetSdk automático | idem |
| Calidad activa | `Mobile` | `ProjectSettings/QualitySettings.asset` |
| Módulo Android del Editor | Instalado | `…/6000.6.7f1/Editor/Data/PlaybackEngines/AndroidPlayer` |

---

## 6. Paquetes instalados (real, `Packages/manifest.json`)

```
com.unity.render-pipelines.universal   17.6.0
com.unity.inputsystem                  1.20.1
com.unity.xr.management                4.7.0
com.unity.xr.openxr                    1.18.0
com.unity.cloud.gltfast                6.20.0    ← importa el GLB
com.unity.ugui                         2.6.0
com.unity.pipeline                     0.10.0-exp.1  ← control del Editor por CLI
com.unity.ai.assistant                 2.21.0-pre.2  ← plugin externo (ver §11)
```

> ❌ **`com.unity.xr.interaction.toolkit` NO está instalado.** Tampoco existe
> `Assets/Samples/XR Interaction Toolkit/…`. Cualquier documento antiguo que lo afirme está obsoleto.
> El rig se escribió a propósito sin XRI.

---

## 6b. Assets y control de versiones (dónde está cada cosa)

**Todo lo que Unity usa está dentro del proyecto**, en `Assets/`:

| Recurso | Ruta |
|---|---|
| ISS (GLB, 42 MB) | `Assets/NovaCore/Art/ISS/ISS_stationary.glb` |
| Materiales ISS (29) | `Assets/NovaCore/Art/ISS/Materials/` |
| Skybox (2K PNG, 4K LDR, 4K HDR) | `Assets/NovaCore/Art/Skybox/` |
| Materiales skybox | `Assets/NovaCore/Art/Materials/` |
| Perfil de post-proceso | `Assets/NovaCore/Settings/NovaCore_OrbitProfile.asset` |
| Escena | `Assets/NovaCore/Scenes/01_Orbit.unity` |
| Config XR | `Assets/XR/` |

- Todos los archivos de `Assets/` tienen su `.meta` y están versionados (270 archivos rastreados).
- **No hay referencias a rutas fuera de `Assets/`** en escenas ni scripts. La carpeta hermana
  `../Recursos/` solo guarda fuentes originales (ZIP del skybox, USDZ descartado, GLB original).
- **Git:** el `.gitignore` es la plantilla oficial de Unity y **no** ignora `.glb`, `.gltf`, `.fbx`,
  `.obj`, `.blend`, `.png`, `.jpg`, `.mat` ni `.prefab`. Si un asset no aparece en `git add .` es
  porque **ya estaba commiteado** (solo se listan archivos nuevos o modificados). Comprobar con:
  ```bash
  git ls-files Assets/NovaCore/Art/ISS/ISS_stationary.glb
  git check-ignore -v Assets/NovaCore/Art/ISS/ISS_stationary.glb   # sin salida = no ignorado
  ```
- Se ignora lo que debe ignorarse: `Library/`, `Temp/`, `Logs/`, `UserSettings/`, `*.csproj`,
  `*.sln`, `*.slnx`, builds y `.unitypackage`.

---

## 7. Scripts — inventario y uso real

### Runtime

| Script | Ruta | ¿Se usa? | Notas |
|---|---|---|---|
| `OrbitFreeFly.cs` | `Assets/NovaCore/Scripts/Player/` | ✅ **Sí, en `01_Orbit`** | Movimiento desktop + VR. Alias `XRCommonUsages` para evitar CS0104 |
| `XRPlayerRig.cs` | `Assets/Game/Scripts/VR/` | ✅ **Sí, en `01_Orbit`** | Crea rig HMD + mandos en runtime. Sin XRI |
| `VRMovement.cs` | `Assets/NovaCore/Scripts/Player/` | ❌ No | Alternativa a `OrbitFreeFly`. **No añadirlo al Player**: duplicaría el movimiento |
| `SpaceStationPlayerController.cs` | `Assets/Game/Scripts/VR/` | ❌ No en `01_Orbit` | FPS con colisión; requiere `CharacterController`. Pensado para interiores |
| `PlayerModeSwitcher.cs` | `Assets/Game/Scripts/VR/` | ❌ No | Alterna espectador/interactivo; sin uso hasta que exista modo interior |
| `PlayerSpawnBay.cs` | `Assets/Game/Scripts/VR/` | ❌ No | Base para puntos de spawn/teleport |
| `ModeToggleUI.cs` | `Assets/Game/Scripts/UI/` | ❌ No | Botón uGUI; no clicable en VR sin módulo de UI XR |
| `OrbitalEnvironment.cs` | `Assets/NovaCore/Scripts/Environment/` | ❌ No | Tierra/Luna/Sol orbitales. **No hay ningún GameObject con este script en la escena** |
| `MissionManager` + Core | `Assets/Game/Scripts/Core/` | ⚠️ Solo en `MissionVR` | Sistema de misiones heredado |

### Editor (`Assets/Editor/`)

| Script | Menú | Para qué |
|---|---|---|
| `BuildOrbitScene.cs` | `Nova Core/BUILD 01_Orbit - Espacio + ISS + Player` | Reconstruye la escena completa (idempotente) |
| `SetupSpaceEnvironment.cs` | `Nova Core/Setup 01_Orbit (Espacio + ISS + Player)` | Variante/alternativa del anterior |
| `ConvertIssMaterials.cs` | `Nova Core/Convert ISS Materials (URP)` | Convierte materiales a URP si salen magenta |
| `SetupEnvironment.cs`, `PlaceIss.cs`, `FrameIss.cs`, `FixCamera.cs`, `PlaceCamera.cs` | (sin menú / varios) | Utilidades heredadas |

> Nota cosmética: los comentarios de `SpaceStationPlayerController.cs`, `VRMovement.cs` y
> `PlayerModeSwitcher.cs` tienen caracteres corruptos (mojibake) de una sesión anterior.
> **No afecta a la compilación**; solo a la legibilidad. Se puede arreglar cuando se toquen esos archivos.

---

## 8. Escena `MissionVR.unity` (heredada)

- Sigue conteniendo la **estación procedural SpaceStation5** (910 GameObjects, 7.26 MB).
- **No está en Build Settings**, así que no se compila ni se ve por accidente.
- No usa `SpaceStationPlayerController` ni `PlayerModeSwitcher` actuales (sus GUIDs no aparecen
  en la escena): la sustitución que se documentó en una sesión anterior **no se llegó a aplicar**
  (`Assets/Editor/SetupMissionVR.cs` no existe).
- Decisión pendiente: reconstruirla con la ISS oficial o descartarla cuando el modo interior
  se aborde en serio.

---

## 9. Cómo meter el nuevo modelo de ISS (cuando lo compres)

Checklist para no romper lo que ya funciona:

1. **Guardar el archivo** en `Assets/NovaCore/Art/ISS/` (GLB/GLTF se importan con glTFast;
   FBX necesita el importador nativo). Mantener también una copia del original en
   `../Recursos/` (fuera del proyecto; es solo fuente, Unity no la usa).
2. **Escala:** 1 unidad = 1 metro. El modelo actual mide ~112 × 69 × 59 m. Si el nuevo viene
   escalado, corregir el factor en el importador o en el `Transform` del root, no en la cámara.
3. **Pivote:** que el origen quede en el centro de la estación. Si no, ajustar la posición del
   root para que el centro caiga en `(0, 0, 3.8)` (como ahora) y el observador siga a ~180 m.
4. **Materiales:** si aparecen magenta → `Nova Core → Convert ISS Materials (URP)`.
   Revisar que los shaders queden en `Shader Graphs/glTF-pbrMetallicRoughness` o `Universal Render Pipeline/Lit`.
5. **Estático:** marcar el root y los hijos como `Static` para static batching.
6. **Encuadre:** mantener el observador a 150–200 m; a 100 m la estación ya llena el FOV.
   Verificar con `WorldToViewportPoint` que el bounds cae dentro de 0..1.
7. **Rendimiento:** contar renderers/triángulos/materiales y anotarlos en la tabla de §4.
   Presupuesto Quest 2: ≤250k tris visibles, ≤100–150 draw calls.
8. **Si el modelo trae interiores**, no mezclarlos con `01_Orbit`: crear una escena aparte
   (`MissionVR` o nueva) con `SpaceStationPlayerController` + `CharacterController`.
9. **Actualizar este documento** con las métricas nuevas y el nombre del archivo.

Consejo: conservar el modelo actual hasta validar el nuevo; renombrar el root nuevo a `ISS`
solo cuando el reemplazo esté verificado, para no romper referencias.

---

## 10. Tareas pendientes priorizadas

### 🔴 Bloqueante para VR
- [ ] **Primer build Android / Quest Link** y confirmar HMD + mandos reales (ver §3.5).
- [ ] Asignar modelos visuales de mando en `XRPlayerRig` (o crear manos simples).
- [ ] Revisar `Project Validation` de OpenXR (`Edit → Project Settings → XR Plug-in Management → OpenXR`).

### 🟡 Necesario para que la demo se vea bien en Quest
- [ ] Perfil URP móvil: sin HDR, MSAA 4×, post-proceso mínimo (`Mobile_RPAsset`).
- [ ] Texturas de la ISS en **ASTC 6×6, max 1024** para Android.
- [ ] Medir draw calls con Frame Debugger en dispositivo.
- [ ] Decidir interacción: XRI vs. interactores propios (§3.4).

### 🟢 Mejoras visuales
- [ ] Rotación lenta de paneles solares (`panel_*`, ~1°/s).
- [ ] Guardar la ISS como Prefab en `Assets/NovaCore/Prefabs/`.
- [ ] Puntos de observación alternativos (arriba, lateral, frontal) con `PlayerSpawnBay`.
- [ ] Tierra/Luna de fondo con `OrbitalEnvironment` (hoy no está en la escena).
- [ ] Sustituir la ISS por el modelo nuevo comprado (§9).

### ⚪ Fase posterior
- [ ] Interiores explorables (requiere modelado).
- [ ] Animación de Canadarm2 / DEXTRE (requiere separar geometría).
- [ ] UI/HUD en VR.
- [ ] Conectar misiones del Core a gameplay real.

---

## 11. Problemas conocidos y avisos que se pueden ignorar

| Aviso | Origen | Acción |
|---|---|---|
| `Account API did not become accessible within 30 seconds` | plugin externo `com.unity.ai.assistant` | Ignorar. No es del proyecto |
| `XR: Error setting active audio output driver. Falling back to default.` | XR Management al inicializar sin visor | Normal en el Editor sin HMD |
| `Editor is already in edit mode` | comando CLI redundante | Ignorar |
| Comentarios con `Ã³`, `�?` en 3 scripts VR | mojibake de una sesión anterior | Cosmético; arreglar al editar esos archivos |
| `Library/`, `Logs/`, `Temp/`, `UserSettings/` ignorados por git | `.gitignore` oficial de Unity | **Correcto**, no tocar: son carpetas regenerables |

Nada de esto genera errores de compilación: el proyecto está en **0 errores / 0 warnings**.

---

## 12. Cómo trabajar en este proyecto con un agente IA

El proyecto tiene instalado `com.unity.pipeline` (0.10.0-exp.1), que permite controlar el Editor
por CLI. Lo que hay que saber para no perder tiempo:

```powershell
cd C:\Users\fdx_dracox\Documents\Projects\NovaCoreOrbita\Nova-Core-Orbita
unity status                                   # ¿Editor abierto? puerto y pid
unity command scene open --path Assets/NovaCore/Scenes/01_Orbit.unity
unity command script recompile                 # compilar
unity command script recompile_status          # failed / errors
unity command console status                   # groundTruth: consoleErrors, consoleWarnings
unity command editor.playmode start | stop
unity command script run --file AgentScripts/X.cs --entry X.Run   # ejecuta C# sin tocar Assets
```

Trucos y trampas ya encontradas:

- El Editor arranca en una escena **Untitled**: hay que abrir `01_Orbit` explícitamente.
- Los scripts de diagnóstico van en `AgentScripts/` (**fuera de `Assets/`**) para no provocar
  reimports ni recargas de dominio.
- `script eval --code` se rompe con comillas en PowerShell; usar `--file` + `script run --entry`.
- Flags: `--enabled` (no `--enable`), `--output-path` (no `--save_path`). No existe `unity pipeline list`
  (usar `unity pipeline editors` / `unity status`).
- Para capturas: `capture editor_window` o render a PNG; **no se pueden "ver" imágenes**, así que
  conviene validar con estadísticas de píxeles (media RGB, píxeles magenta, luminancia del centro).
- Play Mode se corta solo al recompilar: comprobar `editor.playmode stop` antes de asumir que sigue activo.
- Cambios de escena/asset se guardan en disco pero **no se commitean** solos.

---

## 13. Registro de esta sesión (2026-11-10, sesión 4)

**Errores corregidos**
1. `CS0104` ×5 — `CommonUsages` ambiguo entre `UnityEngine.InputSystem` y `UnityEngine.XR`
   en `SpaceStationPlayerController.cs` → alias `using XRCommonUsages = UnityEngine.XR.CommonUsages;`
   (mismo patrón que `OrbitFreeFly.cs`).
2. `CS0618` ×2 — `FindObjectsByType<Light>(FindObjectsSortMode.None)` obsoleto en
   `OrbitalEnvironment.cs:177` → `FindObjectsByType<Light>(FindObjectsInactive.Exclude)`.

**Integración / verificación**
3. `Global Volume` tenía `sharedProfile` nulo → asignado `NovaCore_OrbitProfile`.
4. Skybox confirmado en `RenderSettings` y guardado en la escena (guid `b8007cf6…`).
5. `Sun` ajustado (1.35, Soft, euler 11.9/151.8/0); cámara con `Clear Flags = Skybox`,
   FOV 75, near 0.05, far 5000, post-proceso + FXAA.
6. `XRPlayerRig` añadido al `Player` (sin `VRMovement`, para no duplicar movimiento).
7. Play Mode: ISS visible, centrada, 22/29 materiales texturizados, 0 magenta, skybox correcto.
8. Compilación final: **0 errores / 0 warnings**; escena guardada (`isDirty: false`).

**Decisiones**
- No se instala XRI: el rig actual funciona con OpenXR + Input System y evita rehacer el Player.
- No se añade `VRMovement` ni `SpaceStationPlayerController` al `Player` de `01_Orbit`.

**Documentación y organización (cierre de sesión)**
9. Los tres `.md` de contexto se movieron de `../Recursos/` a `contexto-proyecto/`
   (dentro del repo) y se creó `contexto-proyecto/README.md` con índice + fases 0–5.
10. Verificado que **todos los assets que usa Unity están en `Assets/`** y ya estaban
    versionados en git (270 archivos rastreados; `.gitignore` oficial de Unity, no ignora
    modelos ni texturas). Ver §6b.

---

*Documento reescrito el 2026-11-10 tras verificar el estado real en el Editor.*
