# BIT — 2D Top-Down Action Game

A 2D top-down action game inspired by classic Zelda, built with Unity.  
School project for the **Programación Multimedia y Dispositivos Móviles** module — DAM cycle.

## Gameplay

Survive endless waves of enemies in a dungeon arena. Kill enemies to earn score, collect power-ups to stay alive, and climb the leaderboard.

- **Move**: WASD / Arrow keys
- **Attack**: Space / Left click
- **Pause**: Escape
- **Restart after game over**: R

## Features

| Feature | Description |
|---|---|
| Wave system | Escalating enemy waves with 3 enemy types (basic, fast, tank) |
| Orbit weapon | Sword that rotates around the player and tracks the mouse cursor |
| Projectile attack | Optional ranged attack toward the mouse position |
| Enemy AI | Finite State Machine — Idle / Patrol / Chase / Attack / Return states |
| Pickups | Health potions, score coins, speed boosts |
| Hazards | Damage zones, slow traps, poison areas, pushable boxes |
| HUD | Heart display, score counter, wave indicator, enemy count |
| Ranking | Local JSON leaderboard (top 10) with name input |
| Audio | Centralised AudioManager with event-driven sound effects and BGM |
| New Input System | Keyboard, gamepad and mobile-ready input abstraction |

## Architecture

```
Assets/_Project/
├── Scripts/
│   ├── Core/          # GameManager, SaveSystem, WaveManager, CameraFollow, VFXManager
│   ├── Player/        # PlayerController, OrbitWeapon, Projectile
│   ├── Enemy/         # EnemyAI (FSM), SimpleEnemyAI
│   ├── Interactables/ # PickupBase, HazardBase, PushableObject
│   ├── UI/            # UIManager, MainMenuUI, PauseMenuUI, RankingUI
│   ├── Audio/         # AudioManager
│   ├── ScriptableObjects/ # PlayerStatsSO, GameEventSO
│   └── Editor/        # Setup tools
├── Prefabs/
├── SO_Data/
├── Animations/
├── Sprites/
└── Audio/
```

**Design patterns used:**
- **Singleton** — GameManager, AudioManager, UIManager, WaveManager
- **Observer** — GameEventSO system, PlayerStatsSO events, UI auto-updates
- **Finite State Machine** — EnemyAI (Idle → Patrol → Chase → Attack → Return)
- **Object Pool** — AudioManager round-robin AudioSource pool
- **Inheritance** — PickupBase / HazardBase abstract base classes

## Assets Used

| Asset | Author | Source | Licence |
|---|---|---|---|
| Ninja Adventure | Pixel-Boy & AAA | [itch.io](https://pixel-boy.itch.io/ninja-adventure-asset-pack) | CC0 |

## Academic Requirements Covered

### Minimum (2.1 – 2.12)
- [x] 2.1 Physics — Rigidbody2D, colliders, pushable boxes with Random force/colour
- [x] 2.2 Controllable character — WASD movement, collision with environment
- [x] 2.3 Animations — Animator Controller with Walk/Idle states per direction
- [x] 2.4 Independent part movement — Orbit weapon tracks the mouse independently
- [x] 2.5 Events — Attack key, projectile launch, item pickup events
- [x] 2.6 Score / state — HUD with hearts, numeric score, wave counter
- [x] 2.7 Positive objects — Health pickup, score coin, speed boost
- [x] 2.8 Negative objects — Damage hazard, score drain, slow trap, poison zone
- [x] 2.9 Sound — AudioManager with BGM and event-driven SFX
- [x] 2.10 Playable scene — Complete arena scene with camera and setup
- [x] 2.11 Architecture — Prefabs, organised folders, logic separated from scene objects
- [x] 2.12 Game testing — Tested for errors, physics coherence and performance

### Bonus (3.1 – 3.4)
- [x] 3.1 Mobile ready — New Input System supports keyboard, gamepad and touch
- [x] 3.2 Ranking — Local JSON leaderboard with player name input (top 10)
- [x] 3.3 Extra features — Enemy AI (FSM), wave system with difficulty scaling
- [x] 3.4 Originality — Ninja/anime Zelda aesthetic, wave survival mechanics
