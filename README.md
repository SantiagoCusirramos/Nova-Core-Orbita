# Nova Core Órbita

> A virtual reality experience for **Meta Quest 2** set aboard the International Space Station (ISS).
> Developed for the **NASA International Space Apps Challenge 2026**.

A power failure puts the station at risk. The player must move through the ISS modules, replace a power module using the robotic arm, and go outside on a spacewalk (EVA) with a thruster backpack to repair a damaged solar panel.

---

## Table of Contents

- [Features](#features)
- [Requirements](#requirements)
- [Installation](#installation)
- [How to Run the Game](#how-to-run-the-game)
- [Controls](#controls)
- [Game Flow](#game-flow)
- [Project Structure](#project-structure)
- [Editor Menus (Nova Core)](#editor-menus-nova-core)
- [Troubleshooting](#troubleshooting)
- [Team](#team)
- [Credits and Resources](#credits-and-resources)

---

## Features

- **Training room** with an interactive tutorial (can be skipped).
- **Intro video** (lore) when crossing the exit hatch, with a **SKIP INTRO** button.
- **5 interior modules** as separate scenes: Unity Node 1, Destiny, Zvezda, Quest (airlock) and Cupola.
- **Real-scale exterior (EVA)** with collisions on the ISS model.
- **Two missions**:
  1. **Power Module Replacement**: find the spare module, carry it by hand and install it with the robotic arm.
  2. **Solar Panel Repair (EVA)**: suit up, depressurize the airlock, fly with thrusters and repair the panel with tools.
- **Zero-gravity physics**: objects float, spin, can be thrown and bounce off walls.
- **Gloved hands** that follow the controllers. They press buttons and push levers on contact.
- **Objective guidance** with animated arrows, a compass and automatic hints.
- **Wrist mission tablet**: objectives, power level, timer and Mission Control messages.
- **Gamification**: points, speed bonus, final rank (S/A/B/C) and hidden mission patches.
- Ambient music, sound effects, sparks, Earth and the Moon.

---

## Requirements

### Software

| Tool | Version | Notes |
|---|---|---|
| **Unity** | `6000.6.7f1` | Install through Unity Hub |
| **Android Build Support** module | — | Includes OpenJDK and Android SDK & NDK (required for Quest) |
| **Git** | 2.x or newer | — |
| **Git LFS** | 3.x or newer | **Required**: 3D models, textures and video are stored in LFS |
| **Meta Horizon Link** (Windows) | Latest | Only to test VR from the editor |

### Hardware

- Windows 10/11 PC with a VR-capable GPU (for Link).
- **Meta Quest 2** (or newer), with developer mode enabled to install the APK.
- USB-C data cable or Air Link. A **cable** is recommended, since public Wi-Fi networks are often unstable.

### Unity packages (installed automatically when opening the project)

- Universal Render Pipeline (URP) 17.x
- OpenXR Plugin and XR Plug-in Management
- Input System
- glTFast (`.glb` model import)
- uGUI / TextMeshPro

---

## Installation

### 1. Install Git LFS (once per machine)

```bash
git lfs install
```

### 2. Clone the repository

```bash
git clone git@github.com:SantiagoCusirramos/Nova-Core-Orbita.git
cd Nova-Core-Orbita
git lfs pull
```

> **Important:** if you clone without Git LFS, the models (`.glb`, `.obj`), textures and video become tiny text files and will not load in Unity.
> To check: `git lfs ls-files` must list the models, and `ISS_stationary.glb` should be about 44 MB.

### 3. Open in Unity

1. Open **Unity Hub**, click **Add → Add project from disk** and select the `Nova-Core-Orbita` folder.
2. Open the project with **Unity 6000.6.7f1**. The first import takes several minutes.
3. Wait for compilation to finish. The console must show **no red errors**.

### 4. Initial setup (once)

From the **Nova Core** top menu:

1. **Importar TMP Essentials**: required for the mission tablet text.
2. **Configurar proyecto (Quest 2 + Build Settings)**: applies the Android settings and registers the scenes.
3. **BUILD Interiores ISS (5 módulos + EVA)**: generates or updates all game scenes.

---

## How to Run the Game

### Option A: In the editor, without a headset (keyboard and mouse)

1. Open the scene `Assets/Game/Scenes/ISS/00_Tutorial.unity`.
2. Press **Play**.
3. Click the *Game* view to capture the mouse. **Esc** releases it.

### Option B: In the editor, with a headset (Meta Quest + Link)

1. Open **Meta Horizon Link** on the PC and connect the Quest (cable or Air Link).
2. On the Quest, enable **Quest Link**.
3. In Unity, check that **Edit → Project Settings → XR Plug-in Management → Windows** has **OpenXR** enabled with the **Oculus Touch / Meta Quest** interaction profile.
4. Open `00_Tutorial` and press **Play**.

### Option C: Install on the Quest (standalone APK)

1. Go to **File → Build Profiles** (or *Build Settings*), select **Android** and click **Switch Platform**.
2. Make sure the scenes are in this order:

   ```
   00_Tutorial
   01_Orbit
   02_Nodo
   03_Destiny
   04_Zvezda
   05_Quest
   06_Cupula
   07_EVA
   ```

3. Connect the Quest over USB, with developer mode enabled.
4. Click **Build And Run**. The game is installed under *Library → Unknown Sources*.

> **Testing a single scene:** you can open any scene in `Assets/Game/Scenes/ISS/` and press Play. Mission progress is kept when moving between modules.

---

## Controls

### Headset (Meta Quest)

| Action | Control |
|---|---|
| Walk (interior) | Left stick |
| Turn | Right stick (snap turn) |
| Grab / release (throw) | **Grip** (side button). Reach for the object or point with the laser |
| Use buttons, consoles and tools | **Trigger** (index finger) |
| Press buttons / move levers | Touch them directly with your hand |
| Show path to objective | **Y** |
| Mission tablet | Turn your left wrist. **X** switches tabs |
| EVA thrusters: push | Left stick |
| EVA thrusters: up / down | **A** / **B** |
| EVA thrusters: stabilize (brake) | Left stick click |
| Skip intro | **Hold A or X** for 1 second, or hold the trigger on *SKIP INTRO* |

### Keyboard and mouse (editor)

| Action | Key |
|---|---|
| Move | `W` `A` `S` `D` |
| Look | Mouse |
| Run | `Shift` |
| Use | Left click (hold) |
| Grab / release | `F` |
| Throw object | `R` |
| Show path | `G` |
| Mission tablet | `Tab` (tabs with `1` `2` `3`) |
| EVA: up / down | `Space` / `Ctrl` |
| EVA: stabilize | `X` |
| Skip intro | `Space` / `Enter` / `Esc` |
| Release mouse | `Esc` |

**Test shortcuts (editor only):** in `00_Tutorial`, `F8` opens the exit hatch and `F9` places the player inside it.

---

## Game Flow

```
00_Tutorial (training room)
   │  5-step tutorial (optional)
   │  Cross the hatch → intro video (skippable)
   ▼
02_Nodo ── Mission 1 starts automatically
   ├── 03_Destiny  → pick up the power module (carried in your hand)
   ├── 04_Zvezda   → place it in the hatch, activate the console, steer the robotic arm
   ├── 05_Quest    → Mission 2: suit, systems check, depressurization lever, MMU backpack
   │      └── 07_EVA → fly to the damaged solar panel and repair it with tools
   └── 06_Cupula   → confirm station power → END (score and rank)

01_Orbit: exterior view of the ISS; enter any module by pointing at its sign.
```

---

## Project Structure

```
Assets/
├── Editor/                     # Scene builders and "Nova Core" menus
│   ├── BuildIssScenes.cs       # Generates the modules, tutorial and EVA
│   ├── BuildMissionTablet.cs   # Generates the mission tablet prefab
│   └── NovaProjectSetup.cs     # Quest 2 settings and Build Settings
├── Game/
│   ├── Art/                    # Models, textures, intro video, icons
│   ├── Audio/                  # Ambient music
│   ├── Data/Missions/          # Mission definitions (ScriptableObjects)
│   ├── Prefabs/UI/             # Mission tablet
│   ├── Scenes/ISS/             # 00_Tutorial … 07_EVA
│   └── Scripts/
│       ├── Core/               # MissionManager (mission state)
│       ├── Missions/           # Buttons, zones, guidance, HUD, scoring…
│       ├── Interaction/        # VR/desktop interactors, hands, tools
│       ├── Physics/            # Zero gravity, levers, physical hands
│       ├── Scenes/             # Module transitions, hatches
│       ├── Tutorial/           # Training room
│       ├── Intro/              # Intro video
│       ├── UI/                 # Mission tablet and telemetry
│       └── VR/                 # XR rig
└── NovaCore/                   # Exterior scene, ISS model and skybox
contexto-proyecto/              # Team documentation (status, handoff, official PDF)
recursos/                       # Original source files (video, models, images)
```

---

## Editor Menus (Nova Core)

| Menu | What it does |
|---|---|
| **BUILD Interiores ISS (5 módulos + EVA)** | Regenerates all game scenes. Run it after changing the builder code |
| **BUILD 01_Orbit — Espacio + ISS + Player** | Regenerates the exterior scene |
| **Configurar proyecto (Quest 2 + Build Settings)** | Android settings and scene order |
| **Importar TMP Essentials** | TextMeshPro fonts for the tablet |
| **Crear Tablet de Mision** | Regenerates the tablet prefab |
| **Diagnóstico → Listar partes de la ISS** | Prints the model parts and their positions to the console |

> **Note:** the BUILD replaces the generated scenes. Manual changes made inside those scenes are lost; adjustments must be made in `BuildIssScenes.cs`.

---

## Troubleshooting

| Problem | Likely cause | Fix |
|---|---|---|
| Invisible models or "invalid file" | Repo cloned without Git LFS | `git lfs install`, then `git lfs pull` |
| `This cannot be used during play mode` | A BUILD was run while in Play mode | Stop Play and run the BUILD again |
| Code changes don't show up | Unity didn't reload the files | **Assets → Refresh** (`Ctrl+R`) |
| Empty tablet text | TMP fonts missing | **Nova Core → Importar TMP Essentials** |
| `Token Exchange failed` / `Account API` in console | Unity account session | Sign out and back in to Unity Hub (does not affect the game) |
| Headset not detected in Play | Link not active or OpenXR not configured | Enable Quest Link and check *XR Plug-in Management → OpenXR* |
| Controllers "not detected" at startup | Controllers take a moment to connect | Move the controllers; the hands appear once detected |
| `git push` rejected (*fetch first*) | A teammate pushed changes | `git pull origin main`, then `git push origin main` |

---

## Team

| Member | Area | Responsibility |
|---|---|---|
| **Luis Arizaga** | Core | Mission and objective system |
| **Santiago Cusirramos** | Virtual Reality | VR rig, locomotion and comfort |
| **Yoshua Huillca** | Interactions | Repairs, consoles and minigames |
| **Diogo Castro** | 3D Environment | Station, lighting and atmosphere |
| **Camila Lizarazo** | UI and Narrative | Interface, voice and guidance |

---

## Credits and Resources

- International Space Station 3D model: _[add source]_.
- Moon photograph: **Apollo 11** mission, NASA.
- Tool models (pistol grip tool, hammer, ratchet and grease gun): _[add source]_.
- Astronaut model: _[add source]_.
- Intro video *"Última Luz"*: original production by the team.
- Ambient music and sound effects: generated for the project.

> In-game procedures are simplified for educational and gameplay purposes. **They do not represent real NASA training or procedures.**

---

**NASA International Space Apps Challenge 2026** · Arequipa, Peru