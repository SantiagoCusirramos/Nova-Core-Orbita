# HANDOFF — Cómo retomar Nova Core Órbita

**Para:** cualquier persona o agente IA que continúe el proyecto.
**Última actualización:** 2026-11-10 (sesión 4)

> Lee en este orden (todo dentro del proyecto, en `contexto-proyecto/`):
> 1. este archivo (5 min),
> 2. `contexto-proyecto/README.md` (índice + fase actual),
> 3. `contexto-proyecto/ESTADO_ACTUAL_Y_TAREAS.md` (estado real + tareas),
> 4. `contexto-proyecto/contexto_nova_core_orbita_vr.md` (visión, historia, decisiones).

---

## 1. Datos del proyecto

| Dato | Valor |
|---|---|
| Ruta del proyecto Unity | `C:\Users\fdx_dracox\Documents\Projects\NovaCoreOrbita\Nova-Core-Orbita` |
| Documentación del proyecto | `…\Nova-Core-Orbita\contexto-proyecto\` (dentro del repo) |
| Fuentes originales (no usadas por Unity) | `…\NovaCoreOrbita\Recursos\` (carpeta hermana) |
| Versión de Unity | **6000.6.7f1** (URP 17.6.0) |
| Escena principal | `Assets/NovaCore/Scenes/01_Orbit.unity` (build index 0, única habilitada) |
| Escena legacy | `Assets/Game/Scenes/MissionVR.unity` (estación procedural, **no** en Build Settings) |
| Target | Meta Quest 2 (Android, IL2CPP, ARM64) + escritorio |
| Paquete de control | `com.unity.pipeline` 0.10.0-exp.1 (CLI `unity`) |

---

## 2. Estado en una tabla

| Área | Estado |
|---|---|
| Compilación | ✅ **0 errores / 0 warnings** |
| Skybox deep space | ✅ integrado y guardado en la escena |
| ISS (`ISS_stationary.glb`) | ✅ integrada, texturizada, 0 magenta |
| Encuadre | ✅ ISS a 180.1 m, centrada |
| Post-proceso | ✅ `NovaCore_OrbitProfile` conectado |
| Movimiento escritorio | ✅ probado (`OrbitFreeFly`) |
| Movimiento VR | 🟡 código + config listos, **sin probar en visor** |
| Tracking de mandos | 🟡 posición/rotación sí; **sin modelos visuales** |
| Interacción (grab/ray/UI/teleport) | ❌ no existe |
| Build Android | ⚠️ configurado, nunca ejecutado en dispositivo |

---

## 3. Cómo abrirlo y probarlo

1. Abrir Unity **6000.6.7f1** con el proyecto.
2. Abrir `Assets/NovaCore/Scenes/01_Orbit.unity` (el Editor suele arrancar en una escena "Untitled").
3. Play.
4. Controles: WASD/flechas, E/Q subir-bajar, click derecho o cursor bloqueado para mirar,
   Shift turbo, rueda para velocidad, ESC para soltar cursor.

Si algo se ve mal, **no reconstruyas la escena a ciegas**: primero mira la consola.
Si hace falta, el menú `Nova Core → BUILD 01_Orbit - Espacio + ISS + Player` la reconstruye
(es idempotente, pero sobrescribe ajustes manuales).

---

## 4. Cómo continuar con un agente IA

### 4.1. Prompt sugerido (copiar y pegar)

```
Proyecto Unity en C:\Users\fdx_dracox\Documents\Projects\NovaCoreOrbita\Nova-Core-Orbita
(Unity 6000.6.7f1). Antes de tocar nada, lee:
- C:\Users\fdx_dracox\Documents\Projects\NovaCoreOrbita\Nova-Core-Orbita\contexto-proyecto\HANDOFF.md
- C:\Users\fdx_dracox\Documents\Projects\NovaCoreOrbita\Nova-Core-Orbita\contexto-proyecto\README.md
- C:\Users\fdx_dracox\Documents\Projects\NovaCoreOrbita\Nova-Core-Orbita\contexto-proyecto\ESTADO_ACTUAL_Y_TAREAS.md
- C:\Users\fdx_dracox\Documents\Projects\NovaCoreOrbita\Nova-Core-Orbita\contexto-proyecto\contexto_nova_core_orbita_vr.md
El proyecto ya compila con 0 errores y la escena 01_Orbit está verificada.
No instales XR Interaction Toolkit ni añadas VRMovement al Player (decisión tomada).
Objetivo de esta sesión: <escribe aquí lo que quieres hacer>.
Al terminar, actualiza ESTADO_ACTUAL_Y_TAREAS.md y el registro de cambios.
```

### 4.2. Comandos útiles (CLI `unity`)

```powershell
cd C:\Users\fdx_dracox\Documents\Projects\NovaCoreOrbita\Nova-Core-Orbita

unity status                                    # editor abierto, puerto, pid
unity command scene open --path Assets/NovaCore/Scenes/01_Orbit.unity
unity command script recompile                  # forzar compilación
unity command script recompile_status           # { failed, errors }
unity command console status                    # groundTruth: consoleErrors/Warnings
unity command console get --level warn --tail 20
unity command editor.playmode start
unity command editor.playmode stop
unity command script run --file AgentScripts/Verify.cs --entry Verify.All
```

### 4.3. Trampas ya encontradas (no repetirlas)

- El Editor arranca en escena **Untitled**; hay que abrir `01_Orbit` explícitamente.
- **Play Mode se detiene solo al recompilar.** No asumas que sigue activo.
- Scripts de diagnóstico → `AgentScripts/` (**fuera de `Assets/`**) para no disparar reimports.
- `script eval --code` falla con comillas en PowerShell → usar `--file` + `script run --entry`.
- Flags reales: `--enabled` (no `--enable`), `--output-path` (no `--save_path`).
  No existe `unity pipeline list`; usar `unity status` / `unity pipeline editors`.
- No se pueden "ver" imágenes: valida con estadísticas de píxeles (media RGB, magenta, luminancia).
- `console status` puede mostrar contadores históricos; el bloque `groundTruth` es el fiable.
- Los cambios se guardan en disco pero **no se commitean** solos.

---

## 5. Mapa de archivos clave

```
Assets/
├─ NovaCore/
│  ├─ Scenes/01_Orbit.unity                  ← escena de la demo
│  ├─ Art/ISS/ISS_stationary.glb             ← modelo actual (42 MB, 247.5k tris)
│  ├─ Art/ISS/Materials/                     ← materiales URP de la ISS
│  ├─ Art/Skybox/                            ← cubemaps (2K/4K LDR/4K HDR) + shader
│  ├─ Art/Materials/M_Skybox_DeepSpace.mat   ← skybox activo
│  ├─ Settings/NovaCore_OrbitProfile.asset   ← post-proceso
│  └─ Scripts/Player/OrbitFreeFly.cs         ← locomoción (desktop + VR)
├─ Game/Scripts/VR/XRPlayerRig.cs            ← rig VR runtime (HMD + mandos)
├─ Game/Scripts/VR/…                         ← otros scripts VR (sin uso actual)
├─ Editor/BuildOrbitScene.cs                 ← reconstruye la escena
└─ XR/                                       ← OpenXR loader + settings

contexto-proyecto/                            ← DENTRO del repo: documentación del proyecto
├─ README.md                                 ← índice + fase actual
├─ HANDOFF.md                                ← este archivo
├─ ESTADO_ACTUAL_Y_TAREAS.md                 ← estado y tareas
└─ contexto_nova_core_orbita_vr.md           ← visión e historial

../Recursos/                                  ← FUERA del repo: solo fuentes originales
├─ deep-space-skybox-1.0.zip                 ← ZIP original del skybox
├─ ISS_stationary.glb / .usdz                ← modelos originales
└─ NovaCoreSpacePanorama.shader              ← copia de referencia
```

---

## 6. Próximos 3 pasos concretos

1. **Probar en el visor**: Quest Link → Play en Editor. Buscar en consola
   `[XRPlayerRig] HMD: …`, `[XRPlayerRig] Mando izquierdo: …`, `[XRPlayerRig] Mando derecho: …`.
2. **Si funciona**: build Android (`File → Build Settings → Android → Build And Run`)
   y medir FPS en el dispositivo.
3. **Si no funciona**: revisar `Project Settings → XR Plug-in Management → OpenXR`
   (loader activo en PC/Android, perfiles Oculus Touch, `Project Validation`).

Después: decidir interacción (XRI vs. interactores propios) y asignar modelos de mando.

---

## 7. Decisiones cerradas (no reabrir sin motivo)

- **No se usa XR Interaction Toolkit.** El rig funciona con OpenXR + Input System.
  Instalarlo obliga a rehacer el Player con `XR Origin`.
- **`VRMovement` no se añade al Player**: `OrbitFreeFly` ya cubre VR y escritorio.
  Añadir ambos duplica el movimiento.
- **`SpaceStationPlayerController` no se usa en `01_Orbit`**: requiere `CharacterController`
  y es para interiores.
- **`01_Orbit` es la escena de la demo.** `MissionVR` es legacy y no está en el build.
- **Escala real**: 1 unidad = 1 metro, observador a 150–200 m. No encoger la ISS.
- **Skybox 2K** como base; el HDR 4K queda como alternativa para PC.

---

## 8. Preguntas abiertas

- ¿Se compra un modelo de ISS más completo? → ver checklist §9 de
  `ESTADO_ACTUAL_Y_TAREAS.md` antes de reemplazarlo.
- ¿Interacción con piezas de la estación? Requiere decidir el sistema (§3.4 del estado).
- ¿Se recupera `MissionVR` con la ISS oficial o se descarta?
- ¿Interiores? Requiere modelado aparte (el GLB actual es solo exterior).
