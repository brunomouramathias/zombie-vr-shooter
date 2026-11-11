# Zombie VR Shooter

A small VR arcade shooter for Android, built with Unity and Google Cardboard.
The player stays in place, looks around with the phone's gyroscope and takes
down zombies by aiming at their heads. No controller is required.

![Gameplay](docs/images/gameplay.jpg)

| HUD | Enemy |
| --- | --- |
| ![HUD](docs/images/hud.jpg) | ![Enemy](docs/images/enemy.jpg) |

## Features

- Stereoscopic VR rendering with Google Cardboard (3DoF head tracking).
- Gaze-based shooting: the weapon fires automatically when the crosshair is on
  an enemy's head.
- Two enemy types with NavMesh pathfinding, idle/walk/attack/death animations
  and 3D audio.
- Wave spawner with configurable distance, delay and difficulty scaling.
- World-space HUD with score, best score and a three-heart health bar.
- Game over on enemy contact, with restart by gaze or Cardboard trigger.
- Optional waypoint navigation system for player movement.
- PlayMode tests covering the main gameplay flow.

## Tech stack

| Layer | Technology |
| --- | --- |
| Engine | Unity 6000.5.9f1 |
| Render pipeline | Universal Render Pipeline (URP) |
| Language | C# |
| Input | Unity Input System |
| VR | XR Plug-in Management + Google Cardboard XR Plugin 1.35.0 |
| AI | Unity AI Navigation (NavMesh) |
| UI | uGUI + TextMesh Pro |
| Target | Android (IL2CPP, arm64) |

## Controls

| Action | Unity Editor | Android / Cardboard |
| --- | --- | --- |
| Look around | Hold the right mouse button and drag | Move the phone / head |
| Shoot | Aim at an enemy's head | Aim at an enemy's head |
| Restart after game over | `R`, `Enter` or `Space`, or look at **REINICIAR** | Look at **REINICIAR** for 1.5 s or press the Cardboard trigger |

The editor controls are intended for quick testing. Stereo rendering and
gyroscope tracking only run on a supported Android device.

## Getting started

Requirements:

- Unity Hub and Unity **6000.5.9f1**.
- Android Build Support (SDK, NDK and OpenJDK) if you want to build the APK.

Steps:

1. Clone the repository.
2. In Unity Hub, choose **Add project from disk** and select the project folder.
3. Open `Assets/Scenes/Zombie.unity`.
4. Press **Play**.

The Cardboard plugin is included in `Packages/com.google.xr.cardboard`, so no
additional Git dependency is required. Unity will still restore the remaining
packages from the registry on first open.

## Building for Android

1. Open **File > Build Settings**.
2. Select **Android** and click **Switch Platform**.
3. Confirm that `Assets/Scenes/Zombie.unity` is the only enabled scene.
4. Click **Build** and choose an output path.

Alternatively, use **Tools > Zombie VR > Build APK**. The build is written to
`Builds/ZombieVR.apk` inside the project.

Build settings used by the project:

- Application ID: `com.brunomouramathias.zombievr`
- Minimum API level: 26 (Android 8.0)
- Architecture: ARM64
- Scripting backend: IL2CPP

## Project structure

```
Assets/
  Editor/          Editor tools (scene setup and APK build)
  Resources/       Enemy, bullet and audio prefabs loaded at runtime
  Scenes/          Zombie.unity (main scene)
  Scripts/         Gameplay, AI, UI and VR scripts
  Tests/PlayMode/  PlayMode tests
Packages/          Unity manifest and embedded Cardboard plugin
ProjectSettings/   Build, XR, graphics and input settings
docs/              Architecture notes and screenshots
```

## How it works

- `VrBootstrapper` makes sure XR starts on Android even if the XR management
  initialization is delayed.
- `PlayerController` handles gaze detection and the editor camera. The player
  root stays fixed, so the whole experience works while standing still.
- `WeaponController` raycasts from the camera and fires when the hit point is
  on an enemy's head. Shots deal damage and award score.
- `ZombieController` drives the NavMeshAgent, animation state and audio for
  each enemy. It also handles attack, knockback and death.
- `EnemySpawner` starts each round with a short delay, places enemies on valid
  NavMesh points away from the player and increases the wave size with score.
- `ScoreboardUI` renders the world-space HUD, updates the hearts and shows the
  game-over panel.
- `GameManager` keeps the game state, score, best score and lives.

More details are available in [docs/architecture.md](docs/architecture.md).

## Tests

The project includes PlayMode tests in
`Assets/Tests/PlayMode/ZombieVRPlayModeTests.cs`.

To run them:

1. Open **Window > General > Test Runner**.
2. Select **PlayMode**.
3. Click **Run All**.

## Assets and credits

- Google Cardboard XR Plugin for Unity, Apache License 2.0.
- Enemy, weapon and environment assets: see `Assets/Enemy/CREDITS.md`.
- Audio: see `Assets/Sons/CREDITS.md`.
- Third-party notices: [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).

## License

This project is released under the MIT License. See [LICENSE](LICENSE).
