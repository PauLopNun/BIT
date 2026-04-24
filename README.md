# BIT — 2D Top-Down Action Game

A 2D top-down dungeon survival game inspired by classic Zelda, built with Unity 6.  
School project for the **Programación Multimedia y Dispositivos Móviles** module — DAM cycle.

---

## Gameplay

Survive endless waves of enemies in a procedurally generated dungeon arena.  
Choose your ninja, fight through escalating rounds, collect drops to stay alive and beat the leaderboard.

### Controls

| Input | Action |
|---|---|
| WASD / Arrow keys | Move |
| Left click / Space | Melee attack (sword swing with Katana animation) |
| Right click | Throw shuriken manually (aimed at mouse cursor) |
| Shift | Dash attack — 2× damage, brief sprint |
| *(automatic)* | AutoShooter fires shurikens at nearest enemy every ~1.8 s |
| E | Interact |
| Escape | Pause |
| R | Return to character selection (game over / victory screen) |

---

## Features

| Feature | Description |
|---|---|
| Character selection | 3 playable ninjas with different stats (balanced / warrior / explorer) |
| Wave system | Escalating enemy rounds; horde waves every 5th, boss wave every 10th |
| Melee attack | LMB sword swing — plays real `Attack.png` frames + Katana overlay animation |
| Shuriken (manual) | RMB throw toward cursor using real `Shuriken/SpriteSheet.png` asset |
| AutoShooter | Automatic weapon (Vampire Survivors style) targets nearest enemy |
| Dash attack | Shift — dash in movement direction with 2× melee damage |
| Sprite animations | Walk / Idle / Attack using real asset-pack sprites per character |
| Enemy types | Melee (skull), Ranged (skull that shoots), Tank, Fast, Boss |
| Enemy AI | Finite State Machine (Idle → Patrol → Chase → Attack → Return) |
| Ranged enemy AI | Maintains preferred distance, strafes, fires projectiles every ~2.8 s |
| Boss AI | Scaled stats × 8, spawns at horde wave 10, 20, … |
| Combo system | Consecutive kills boost score: ×1.5 (3+), ×2 (6+), ×3 (10+) |
| Wave upgrades | Between waves: choose 1 of 3 random upgrades (speed, damage, health, …) |
| Level progression | Every 5 waves: level-up message + healing bonus |
| Item drops | Enemies drop Heart (~12–18%) and Coin (~45–50%) on death |
| Pickups | Heart restores 25 HP · Coin adds score |
| HUD | Heart bar, numeric HP, score, wave counter, enemy count |
| Dungeon map | Procedurally generated tilemap (floor, walls, wall faces) |
| VFX | Hit sparks, death particles, slash arcs, pickup bursts |
| Audio | BGM + 5 SFX (hit, coin, heal, attack, enemy death) from Ninja Adventure pack |
| Ranking | Local JSON leaderboard (top 10) with player name input |
| Game Over flow | On death → saves score → returns to ninja selection screen |
| Pause menu | Escape freezes game, shows resume / quit options |

---

## Architecture

```
Assets/_Project/
├── Scripts/
│   ├── Core/          # RuntimeGameManager, WaveManager, GameManager, VFXManager,
│   │                  # ComboManager, LevelProgressionManager, WaveUpgradeSystem,
│   │                  # SaveSystem, CharacterSelectManager, CameraFollow
│   ├── Player/        # PlayerController, AutoShooter (PlayerBullet), OrbitWeapon, Projectile
│   ├── Enemy/         # EnemyAI (FSM), SimpleEnemyAI, RangedEnemyAI, BossEnemyAI,
│   │                  # EnemyDropper, EnemyProjectile
│   ├── Interactables/ # PickupBase (HealthPickup, ScorePickup, SpeedPickup),
│   │                  # HazardBase, PushableObject
│   ├── UI/            # CharacterSelectUI, MainMenuUI, PauseMenuUI, RankingUI, UIManager
│   ├── Audio/         # AudioManager
│   ├── ScriptableObjects/ # CharacterData, PlayerStatsSO, GameEventSO
│   └── Editor/        # BITAutoSetup, BITFullSetup, DungeonMapGenerator, NinjaAdventureSetup
├── Prefabs/
│   ├── Player/        # Player.prefab
│   ├── Enemies/       # Enemy_Skeleton, Enemy_Dragon, Enemy_Cyclope
│   ├── Pickups/       # Heart.prefab (tag: Health), Coin.prefab (tag: Coin)
│   └── Projectiles/   # Shuriken.prefab
├── Scenes/
│   └── gamesetupscene.unity   # Main playable scene
└── Sprites/
    └── Ninja Adventure/       # CC0 asset pack (characters, monsters, FX, tilesets, audio)
```

**Design patterns:**

| Pattern | Where |
|---|---|
| Singleton | RuntimeGameManager, WaveManager, VFXManager, ComboManager, SaveSystem, … |
| Observer | `OnWaveStarted / OnWaveCleared / OnEnemyCountChanged` events; `GameEventSO` system |
| FSM | `EnemyAI` — Idle → Patrol → Chase → Attack → Return |
| Strategy | `PickupBase` / `HazardBase` abstract base classes with overridable `ApplyEffect()` |
| Object Pool | `AudioManager` round-robin `AudioSource` pool |

---

## First-time Setup (Unity Editor)

1. Open the project in **Unity 6.x**.
2. Run **`BIT → 1. Configurar Tilesets`** once — slices the PNG tilesets into 16×16 tiles.
3. Run **`BIT → 2. Configurar Escena`** — generates the dungeon, places managers and player.
4. *(Optional)* Run **`BIT → BIT Full Setup`** — adds OrbitWeapon child to Player prefab and configures remaining prefab components.
5. Press **Play** in `gamesetupscene`.

---

## Assets Used

| Asset | Author | Source | Licence |
|---|---|---|---|
| Ninja Adventure | Pixel-Boy & AAA | [itch.io](https://pixel-boy.itch.io/ninja-adventure-asset-pack) | CC0 |

Sprites used from the pack:
- `Actor/Character/Ninja{Blue,Red,Green}/SeparateAnim/` — Walk, Idle, Attack, Dead
- `Actor/CharacterAnimated/Weapon/Katana.png` — melee swing overlay (16 frames)
- `FX/Projectile/Shuriken/SpriteSheet.png` — shuriken projectile (42 frames)
- `Actor/Monster/Skull*/SpriteSheet.png` — ranged enemy visual
- `Actor/Monster/Cyclope, Dragon, Skeleton/` — melee enemy prefabs
- `Backgrounds/Tilesets/Interior/` — dungeon floor and wall tiles
- `Audio/Musics/` + `Audio/Sounds/` — BGM and SFX

---

## Academic Requirements

### Minimum (2.1 – 2.12)

- [x] **2.1 Physics** — Rigidbody2D + CircleCollider2D on all characters; CompositeCollider2D on tilemap walls; `PushableObject.cs` implements push mechanics with randomised force and colour
- [x] **2.2 Controllable character** — WASD movement via New Input System, pixel-perfect collision with dungeon walls
- [x] **2.3 Animations** — Walk / Idle / Attack sprite cycles loaded from real asset-pack PNGs per character; flipX for horizontal direction; Animator params forwarded if controller is assigned
- [x] **2.4 Independent part movement** — `OrbitWeapon` child object rotates around the player and tracks the mouse cursor independently of the parent transform *(requires BIT Full Setup to add to Player prefab)*
- [x] **2.5 Events** — LMB attack event, RMB shuriken throw, item pickup collision events, wave start/clear events via C# Actions
- [x] **2.6 Score / state** — HUD with heart bar, numeric HP, score counter, wave number, enemy count; combo multiplier display
- [x] **2.7 Positive objects** — Heart pickup (+25 HP), Coin pickup (+score), SpeedPickup (temporary ×1.5 speed); dropped by enemies and collectible by the player
- [x] **2.8 Negative objects** — `HazardBase` abstract class with `DamageHazard`, `SlowTrap`, `PoisonZone` concrete implementations; `PushableObject` with random colour/force on push
- [x] **2.9 Sound** — BGM from Ninja Adventure audio pack; 5 SFX (hit, coin, heal, attack slash, enemy death) loaded via `AssetDatabase` at runtime; `AudioManager` with pitch randomisation
- [x] **2.10 Playable scene** — `gamesetupscene` with procedural dungeon, player, enemies, camera follow, full HUD and wave logic
- [x] **2.11 Architecture** — Singleton managers, ScriptableObject events, prefabs with separated logic, Editor setup tools, organised folder structure
- [x] **2.12 Game testing** — Null guards throughout, `#if UNITY_EDITOR` for editor-only APIs, graceful fallbacks for missing sprites/audio, continuous collision detection

### Bonus (3.1 – 3.4)

- [x] **3.1 Mobile ready** — New Input System abstracts keyboard, gamepad and touch; controls auto-adapt
- [x] **3.2 Ranking** — `SaveSystem` writes/reads JSON leaderboard; `RankingUI` shows top 10 with player name entry
- [x] **3.3 Extra AI / systems** — FSM enemy (`EnemyAI`), simplified chase AI (`SimpleEnemyAI`), ranged AI (`RangedEnemyAI`) with preferred-range strafing, `BossEnemyAI` with stat scaling; `ComboManager`, `WaveUpgradeSystem`, `LevelProgressionManager`
- [x] **3.4 Originality** — Ninja/dungeon aesthetic using CC0 asset pack; Vampire Survivors–style AutoShooter; procedural dungeon map; character selection with 3 stat-differentiated ninjas
