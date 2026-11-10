# Contexto del proyecto — Nova Core Órbita

Esta carpeta vive **dentro del proyecto Unity** para que el contexto viaje con el repo:
si cambias de máquina, de sesión o de agente IA, aquí está todo lo necesario para retomar.

**Última actualización:** 2026-11-10

---

## 1. Índice de documentos

| Documento | Para qué sirve | Cuándo leerlo |
|---|---|---|
| `HANDOFF.md` | Cómo abrir, probar y continuar el proyecto. Comandos, trampas, prompt para un agente IA | **Primero**, siempre |
| `ESTADO_ACTUAL_Y_TAREAS.md` | Estado real verificado, configuración XR, inventario de scripts, tareas priorizadas, checklist para cambiar la ISS | Segundo |
| `contexto_nova_core_orbita_vr.md` | Visión, objetivos, historia del proyecto, decisiones y registro de cambios | Tercero / consulta |

Orden recomendado de lectura: **HANDOFF → ESTADO → contexto**.

---

## 2. Fase actual del proyecto

| Fase | Contenido | Estado |
|---|---|---|
| **0. Proyecto y XR** | Unity 6000.6.7f1 + URP, OpenXR + XR Management (PC y Android), Input System, glTFast | ✅ Completa |
| **1. Ambiente espacial** | Skybox deep space (cubemap 2K), Sol alineado, post-proceso (bloom, tonemapping, vignette) | ✅ Completa y verificada |
| **2. ISS** | `ISS_stationary.glb` integrada, materiales URP, encuadre a 180 m | ✅ Completa y verificada |
| **3. VR y locomoción** | Rig OpenXR (`XRPlayerRig`), tracking HMD y mandos, locomoción con sticks + snap turn | 🟡 Implementada, **sin probar en visor físico** |
| **4. Build Quest 2 + optimización** | Build Android, ASTC, draw calls, 72 Hz | ⏳ No iniciada |
| **5. Ampliaciones** | Interacción, nuevo modelo de ISS, interiores, misiones | ⏳ Pendiente |

**Resumen en una frase:** la demo de escritorio está terminada y verificada; falta validar VR en el
dispositivo y decidir el sistema de interacción.

Detalle completo, métricas y tareas: `ESTADO_ACTUAL_Y_TAREAS.md`.

---

## 3. Dónde está cada cosa (assets)

**Todo lo que Unity usa está dentro de `Assets/`** (no hay dependencias fuera del proyecto):

| Recurso | Ruta dentro del proyecto |
|---|---|
| Modelo de la ISS (GLB, 42 MB) | `Assets/NovaCore/Art/ISS/ISS_stationary.glb` |
| Materiales de la ISS (29) | `Assets/NovaCore/Art/ISS/Materials/` |
| Skybox (2K PNG, 4K LDR, 4K HDR) | `Assets/NovaCore/Art/Skybox/` |
| Materiales del skybox | `Assets/NovaCore/Art/Materials/` |
| Perfil de post-proceso | `Assets/NovaCore/Settings/NovaCore_OrbitProfile.asset` |
| Escena principal | `Assets/NovaCore/Scenes/01_Orbit.unity` |
| Configuración XR | `Assets/XR/` |
| Escenas y ajustes de render | `Assets/Scenes/`, `Assets/Settings/` |

Fuera del proyecto (`../Recursos/`, carpeta hermana) solo quedan **fuentes originales** que Unity
no necesita: el ZIP del skybox, el USDZ descartado, el GLB original y copias de referencia del
shader y de `OrbitalEnvironment.cs`. No se referencian desde ningún script ni escena.

> Si en el futuro se compra un modelo nuevo de ISS, va en `Assets/NovaCore/Art/ISS/`.
> Ver checklist en `ESTADO_ACTUAL_Y_TAREAS.md` §9.

---

## 4. Notas sobre Git

- El `.gitignore` es la plantilla oficial de Unity: **no ignora** `.glb`, `.gltf`, `.fbx`, `.obj`,
  `.blend`, `.png`, `.jpg`, `.mat` ni `.prefab`.
- Los assets **ya están versionados** desde commits anteriores. Por eso `git add .` no los vuelve
  a mostrar: solo aparecen los archivos nuevos o modificados.
- Comprobación rápida de que un asset está en el repo:
  ```bash
  git ls-files Assets/NovaCore/Art/ISS/ISS_stationary.glb
  git check-ignore -v Assets/NovaCore/Art/ISS/ISS_stationary.glb   # sin salida = no ignorado
  ```
- Lo que **sí** se ignora (correcto): `Library/`, `Temp/`, `Logs/`, `UserSettings/`, `*.csproj`,
  `*.sln`, `*.slnx`, `*.apk`, `*.unitypackage`.
- Los archivos `.meta` son obligatorios y están todos versionados; no borrar ni regenerar.

---

## 5. Convención de trabajo

1. Al terminar una sesión, actualizar `ESTADO_ACTUAL_Y_TAREAS.md` (estado + registro) y, si cambia
   el alcance, la tabla de fases de este README.
2. Mantener este orden: **una fase a la vez**; no abrir interacción/interiores antes de validar VR.
3. Cualquier decisión que se cierre (por ejemplo "no usamos XR Interaction Toolkit") se anota en
   `HANDOFF.md` §7 para no reabrirla por accidente.
