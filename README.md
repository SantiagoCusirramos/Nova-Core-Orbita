\# Nova Core Órbita



> Experiencia de realidad virtual para \*\*Meta Quest 2\*\* ambientada en la Estación Espacial Internacional (ISS).

> Proyecto desarrollado para el \*\*NASA International Space Apps Challenge 2026\*\*.



Una falla eléctrica pone en riesgo la estación. El jugador debe recorrer los módulos de la ISS, reemplazar un módulo de energía con el brazo robótico y salir al exterior (EVA) con una mochila de propulsores para reparar un panel solar dañado.



\---



\## Índice



\- \[Características](#características)

\- \[Requisitos](#requisitos)

\- \[Instalación](#instalación)

\- \[Cómo ejecutar el juego](#cómo-ejecutar-el-juego)

\- \[Controles](#controles)

\- \[Flujo del juego](#flujo-del-juego)

\- \[Estructura del proyecto](#estructura-del-proyecto)

\- \[Menús del editor (Nova Core)](#menús-del-editor-nova-core)

\- \[Solución de problemas](#solución-de-problemas)

\- \[Equipo](#equipo)

\- \[Créditos y recursos](#créditos-y-recursos)



\---



\## Características



\- \*\*Sala de entrenamiento\*\* con tutorial interactivo, que se puede saltar.

\- \*\*Video de introducción\*\* (lore) al cruzar la escotilla, con botón \*\*OMITIR INTRO\*\*.

\- \*\*5 módulos interiores\*\* en escenas separadas: Nodo 1 Unity, Destiny, Zvezda, Quest (esclusa) y Cúpula.

\- \*\*Exterior a escala real (EVA)\*\*, con colisiones sobre el modelo de la ISS.

\- \*\*Dos misiones\*\*:

&#x20; 1. \*\*Sustitución del módulo energético\*\*: buscar el repuesto, llevarlo en la mano y usar el brazo robótico.

&#x20; 2. \*\*Reparación del panel solar (EVA)\*\*: equiparse el traje, despresurizar la esclusa, volar con propulsores y reparar el panel con herramientas.

\- \*\*Física de gravedad cero\*\*: los objetos flotan, giran, se pueden lanzar y rebotan contra las paredes.

\- \*\*Manos con guante\*\* que siguen al mando. Con ellas se pulsan botones y palancas por contacto.

\- \*\*Guía al objetivo\*\* con flechas animadas, brújula y pistas automáticas.

\- \*\*Tablet de misión en la muñeca\*\*: objetivos, energía, tiempo y mensajes del Centro de Control.

\- \*\*Gamificación\*\*: puntos, bonus por rapidez, rango final (S/A/B/C) e insignias escondidas.

\- Música ambiental, efectos de sonido, chispas, la Tierra y la Luna.



\---



\## Requisitos



\### Software



| Herramienta | Versión | Notas |

|---|---|---|

| \*\*Unity\*\* | `6000.6.7f1` | Instalar desde Unity Hub |

| Módulo \*\*Android Build Support\*\* | — | Incluye OpenJDK y Android SDK \& NDK (necesario para el Quest) |

| \*\*Git\*\* | 2.x o superior | — |

| \*\*Git LFS\*\* | 3.x o superior | \*\*Obligatorio\*\*: modelos 3D, texturas y video están en LFS |

| \*\*Meta Horizon Link\*\* (Windows) | Última | Solo para probar en VR desde el editor |



\### Hardware



\- PC con Windows 10/11 y GPU compatible con VR (para Link).

\- \*\*Meta Quest 2\*\* (o superior), con el modo desarrollador activado para instalar el APK.

\- Cable USB-C de datos o Air Link. Recomendado: \*\*cable\*\*, porque las redes WiFi públicas suelen ser inestables.



\### Paquetes de Unity (se instalan solos al abrir el proyecto)



\- Universal Render Pipeline (URP) 17.x

\- OpenXR Plugin y XR Plug-in Management

\- Input System

\- glTFast (importación de modelos `.glb`)

\- uGUI / TextMeshPro



\---



\## Instalación



\### 1. Instalar Git LFS (una sola vez por equipo)



```bash

git lfs install

```



\### 2. Clonar el repositorio



```bash

git clone git@github.com:SantiagoCusirramos/Nova-Core-Orbita.git

cd Nova-Core-Orbita

git lfs pull

```



> \*\*Importante:\*\* si clonas sin Git LFS, los modelos (`.glb`, `.obj`), las texturas y el video quedan como archivos de texto de pocos bytes y no cargan en Unity.

> Para comprobarlo, `git lfs ls-files` debe listar los modelos y `ISS\_stationary.glb` debe pesar unos 44 MB.



\### 3. Abrir en Unity



1\. Abre \*\*Unity Hub\*\*, pulsa \*\*Add → Add project from disk\*\* y selecciona la carpeta `Nova-Core-Orbita`.

2\. Abre el proyecto con \*\*Unity 6000.6.7f1\*\*. La primera importación tarda varios minutos.

3\. Espera a que termine de compilar. En la consola \*\*no debe haber errores rojos\*\*.



\### 4. Configuración inicial (una sola vez)



Desde el menú superior \*\*Nova Core\*\*:



1\. \*\*Importar TMP Essentials\*\*: necesario para los textos de la tablet de misión.

2\. \*\*Configurar proyecto (Quest 2 + Build Settings)\*\*: aplica los ajustes de Android y registra las escenas.

3\. \*\*BUILD Interiores ISS (5 módulos + EVA)\*\*: genera o actualiza todas las escenas del juego.



\---



\## Cómo ejecutar el juego



\### Opción A: En el editor, sin visor (teclado y ratón)



1\. Abre la escena `Assets/Game/Scenes/ISS/00\_Tutorial.unity`.

2\. Pulsa \*\*Play\*\*.

3\. Haz clic en la vista \*Game\* para capturar el ratón. \*\*Esc\*\* lo libera.



\### Opción B: En el editor, con visor (Meta Quest + Link)



1\. Abre \*\*Meta Horizon Link\*\* en el PC y conecta el Quest (cable o Air Link).

2\. En el Quest, activa \*\*Quest Link\*\*.

3\. En Unity, comprueba que en \*\*Edit → Project Settings → XR Plug-in Management → Windows\*\* esté activo \*\*OpenXR\*\* con el perfil de \*\*Oculus Touch / Meta Quest\*\*.

4\. Abre `00\_Tutorial` y pulsa \*\*Play\*\*.



\### Opción C: Instalar en el Quest (APK independiente)



1\. \*\*File → Build Profiles\*\* (o \*Build Settings\*), elige \*\*Android\*\* y pulsa \*\*Switch Platform\*\*.

2\. Verifica que las escenas estén en este orden:



&#x20;  ```

&#x20;  00\_Tutorial

&#x20;  01\_Orbit

&#x20;  02\_Nodo

&#x20;  03\_Destiny

&#x20;  04\_Zvezda

&#x20;  05\_Quest

&#x20;  06\_Cupula

&#x20;  07\_EVA

&#x20;  ```



3\. Conecta el Quest por USB, con el modo desarrollador activo.

4\. Pulsa \*\*Build And Run\*\*. El juego queda instalado en \*Biblioteca → Orígenes desconocidos\*.



> \*\*Probar una escena suelta:\*\* puedes abrir cualquier escena de `Assets/Game/Scenes/ISS/` y darle a Play. La misión y el progreso se conservan al cambiar de módulo.



\---



\## Controles



\### Visor (Meta Quest)



| Acción | Control |

|---|---|

| Caminar (interior) | Stick izquierdo |

| Girar | Stick derecho (giro por pasos) |

| Agarrar / soltar (lanzar) | \*\*Grip\*\* (botón lateral). Acerca la mano o apunta con el láser |

| Usar botones, consolas y herramientas | \*\*Gatillo\*\* (índice) |

| Pulsar botones / mover palancas | Tocarlos directamente con la mano |

| Mostrar el camino al objetivo | \*\*Y\*\* |

| Tablet de misión | Girar la muñeca izquierda. \*\*X\*\* cambia de pestaña |

| Propulsores EVA: empujar | Stick izquierdo |

| Propulsores EVA: subir / bajar | \*\*A\*\* / \*\*B\*\* |

| Propulsores EVA: estabilizar (frenar) | Clic del stick izquierdo |

| Omitir intro | \*\*A\*\* o \*\*X\*\*, o el botón \*OMITIR INTRO\* |



\### Teclado y ratón (editor)



| Acción | Tecla |

|---|---|

| Moverse | `W` `A` `S` `D` |

| Mirar | Ratón |

| Correr | `Shift` |

| Usar | Clic izquierdo (mantener) |

| Agarrar / soltar | `F` |

| Lanzar objeto | `R` |

| Mostrar camino | `G` |

| Tablet de misión | `Tab` (pestañas con `1` `2` `3`) |

| EVA: subir / bajar | `Espacio` / `Ctrl` |

| EVA: estabilizar | `X` |

| Omitir intro | `Espacio` / `Enter` / `Esc` |

| Liberar el ratón | `Esc` |



\*\*Atajos de prueba (solo en el editor):\*\* en `00\_Tutorial`, `F8` abre la escotilla de salida y `F9` coloca al jugador dentro de ella.



\---



\## Flujo del juego



```

00\_Tutorial (sala de entrenamiento)

&#x20;  │  Tutorial de 5 pasos (opcional)

&#x20;  │  Cruzar la escotilla → video de introducción (se puede omitir)

&#x20;  ▼

02\_Nodo ── Misión 1 inicia automáticamente

&#x20;  ├── 03\_Destiny  → recoger el módulo energético (se lleva en la mano)

&#x20;  ├── 04\_Zvezda   → colocarlo en la compuerta, activar la consola, mover el brazo robótico

&#x20;  ├── 05\_Quest    → Misión 2: traje, verificación, palanca de despresurización, mochila MMU

&#x20;  │      └── 07\_EVA → volar al panel solar dañado y repararlo con herramientas

&#x20;  └── 06\_Cupula   → confirmar la energía de la estación → FIN (puntuación y rango)



01\_Orbit: vista exterior de la ISS; se entra a cada módulo apuntando a su cartel.

```



\---



\## Estructura del proyecto



```

Assets/

├── Editor/                     # Constructores de escenas y menús "Nova Core"

│   ├── BuildIssScenes.cs       # Genera los módulos, el tutorial y la EVA

│   ├── BuildMissionTablet.cs   # Genera el prefab de la tablet de misión

│   └── NovaProjectSetup.cs     # Ajustes de Quest 2 y Build Settings

├── Game/

│   ├── Art/                    # Modelos, texturas, video de intro, iconos

│   ├── Audio/                  # Música ambiental

│   ├── Data/Missions/          # Definición de misiones (ScriptableObjects)

│   ├── Prefabs/UI/             # Tablet de misión

│   ├── Scenes/ISS/             # 00\_Tutorial … 07\_EVA

│   └── Scripts/

│       ├── Core/               # MissionManager (estado de misiones)

│       ├── Missions/           # Botones, zonas, guía, HUD, puntuación…

│       ├── Interaction/        # Interactores VR/escritorio, manos, herramientas

│       ├── Physics/            # Gravedad cero, palancas, manos físicas

│       ├── Scenes/             # Transiciones entre módulos, escotillas

│       ├── Tutorial/           # Sala de entrenamiento

│       ├── Intro/              # Video de introducción

│       ├── UI/                 # Tablet de misión y telemetría

│       └── VR/                 # Rig XR

└── NovaCore/                   # Escena exterior, modelo de la ISS y skybox

contexto-proyecto/              # Documentación del equipo (estado, handoff, PDF oficial)

recursos/                       # Archivos fuente originales (video, modelos, imágenes)

```



\---



\## Menús del editor (Nova Core)



| Menú | Qué hace |

|---|---|

| \*\*BUILD Interiores ISS (5 módulos + EVA)\*\* | Regenera todas las escenas del juego. Úsalo después de cambiar código del constructor |

| \*\*BUILD 01\_Orbit — Espacio + ISS + Player\*\* | Regenera la escena exterior |

| \*\*Configurar proyecto (Quest 2 + Build Settings)\*\* | Ajustes de Android y orden de escenas |

| \*\*Importar TMP Essentials\*\* | Fuentes de TextMeshPro para la tablet |

| \*\*Crear Tablet de Misión\*\* | Regenera el prefab de la tablet |

| \*\*Diagnóstico → Listar partes de la ISS\*\* | Muestra en consola las partes del modelo y sus posiciones |



> \*\*Nota:\*\* el BUILD reemplaza las escenas generadas. Los cambios manuales hechos dentro de esas escenas se pierden; los ajustes deben hacerse en `BuildIssScenes.cs`.



\---



\## Solución de problemas



| Problema | Causa probable | Solución |

|---|---|---|

| Modelos invisibles o "archivo no válido" | El repo se clonó sin Git LFS | `git lfs install` y luego `git lfs pull` |

| `This cannot be used during play mode` | Se ejecutó un BUILD con el juego en Play | Detén el Play y vuelve a ejecutar el BUILD |

| Los cambios de código no aparecen | Unity no recargó los archivos | \*\*Assets → Refresh\*\* (`Ctrl+R`) |

| Textos de la tablet vacíos | Faltan las fuentes de TMP | \*\*Nova Core → Importar TMP Essentials\*\* |

| `Token Exchange failed` / `Account API` en consola | Sesión de la cuenta de Unity | Cerrar sesión y volver a entrar en Unity Hub (no afecta al juego) |

| El visor no se detecta en Play | Link no activo u OpenXR no configurado | Activa Quest Link y revisa \*XR Plug-in Management → OpenXR\* |

| Mandos "no detectados" en consola al iniciar | Los mandos tardan en conectarse | Mueve los mandos; las manos aparecen al detectarlos |

| `git push` rechazado (\*fetch first\*) | Otro integrante subió cambios | `git pull origin main` y luego `git push origin main` |



\---



\## Equipo



| Integrante | Rol |

|---|---|

| Santiago Cusirramos | Liderazgo, integración y desarrollo VR |

| Camila Lizarazo | UI (tablet de misión), Centro de Control, narrativa y pruebas |

| \_Nombre\_ | \_Rol\_ |

| \_Nombre\_ | \_Rol\_ |

| \_Nombre\_ | \_Rol\_ |



\---



\## Créditos y recursos



\- Modelo 3D de la Estación Espacial Internacional: \_\[indicar la fuente]\_.

\- Fotografía de la Luna: misión \*\*Apollo 11\*\*, NASA.

\- Modelos de herramientas (taladro de empuñadura, martillo, carraca y pistola de sellador): \_\[indicar la fuente]\_.

\- Modelo del astronauta: \_\[indicar la fuente]\_.

\- Video de introducción \*"Última Luz"\*: producción original del equipo.

\- Música ambiental y efectos de sonido: generados para el proyecto.



> Los procedimientos del juego están simplificados con fines educativos y de jugabilidad. \*\*No representan entrenamiento ni procedimientos reales de la NASA.\*\*



\---



\*\*NASA International Space Apps Challenge 2026\*\* · Arequipa, Perú

