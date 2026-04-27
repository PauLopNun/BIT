# BIT — Memoria del Proyecto
## Desarrollo de un Videojuego con Unity

---

| | |
|---|---|
| **Título del videojuego** | BIT |
| **Nombre del alumno/a** | *(escribe tu nombre aquí)* |
| **Módulo** | Programación Multimedia y Dispositivos Móviles |
| **Ciclo** | Desarrollo de Aplicaciones Multiplataforma (DAM) |
| **Profesora** | Isabel Martí Romeu |
| **Curso académico** | 2025 – 2026 |

---

## 1. Descripción del videojuego

**BIT** es un videojuego de acción 2D con perspectiva top-down (vista cenital) inspirado en clásicos como Zelda y en el género "Vampire Survivors". El jugador debe sobrevivir oleadas interminables de enemigos en una mazmorra generada proceduralmente.

### Tipo de juego
Acción / Supervivencia por oleadas (wave survival roguelite)

### Objetivo del jugador
Resistir el mayor número de oleadas posible, acumular la mayor puntuación y entrar en el ranking local de los 10 mejores.

### Mecánicas principales

| Mecánica | Descripción |
|---|---|
| **Movimiento** | WASD / teclas de dirección |
| **Ataque melee** | Clic izquierdo — golpe de espada con animación de katana |
| **Shuriken** | Clic derecho — lanza un shuriken apuntando al ratón |
| **Disparo automático** | Sistema estilo Vampire Survivors que dispara al enemigo más cercano cada ~1,8 s sin input del jugador |
| **Dash** | Shift — lanzamiento en la dirección de movimiento con daño ×2 |
| **Selección de personaje** | 3 ninjas con estadísticas distintas antes de comenzar la partida |
| **Oleadas** | Las rondas escalan en dificultad; hordas cada 5 rondas, oleada de boss cada 10 |
| **Mejoras entre oleadas** | Al terminar cada ronda el jugador elige 1 de 3 mejoras aleatorias |
| **Combo** | Las kills consecutivas aplican multiplicadores de puntuación (×1,5 / ×2 / ×3) |
| **Recogida de objetos** | Los enemigos sueltan Corazones (+25 HP) y Monedas (+puntuación) |

---

## 2. Arquitectura del juego

### 2D o 3D
El juego es completamente **2D**. Usa sprites pixel-art (16×16 px) con cámara ortográfica.

### Motor y versión
**Unity 6** (6000.3.7f1) con el **New Input System** de Unity para la gestión de controles.

### Elementos principales del motor utilizados

| Elemento | Uso |
|---|---|
| **Scenes** | 4 escenas (MainMenu, CharacterSelect, gamesetupscene, TestScene) |
| **GameObjects** | Jugador, enemigos, pickups, managers, cámara, mapa |
| **Components** | Rigidbody2D, Collider2D, SpriteRenderer, AudioSource, Tilemap |
| **Scripts (C#)** | 43 scripts organizados en 8 namespaces |
| **ScriptableObjects** | CharacterData (stats de personaje), PlayerStatsSO (stats base del jugador), GameEventSO (sistema de eventos) |
| **Tilemap** | Generación procedural del mapa de mazmorras mediante editor personalizado |
| **New Input System** | PlayerInput + InputAction para soporte de teclado, ratón y gamepad |

### Patrones de diseño aplicados

- **Singleton** — `RuntimeGameManager`, `WaveManager`, `VFXManager`, `ComboManager`, `CharacterSelectManager`
- **Observer** — `GameEventSO` / `GameEventListenerComponent` para comunicación desacoplada entre sistemas
- **Finite State Machine (FSM)** — IA de enemigos con estados Idle → Patrol → Chase → Attack → Return
- **Strategy** — Hazards (trampas) implementan comportamientos intercambiables (daño, ralentización, veneno, drenaje de puntos)
- **Object Pool** — `AudioManager` con pool de AudioSources para reproducir SFX sin allocations

### Estructura de namespaces

```
BIT.Core       → Managers principales (WaveManager, RuntimeGameManager, VFXManager…)
BIT.Player     → Controlador del jugador y sistema de disparo automático
BIT.Enemy      → IA de enemigos y sistema de drops
BIT.UI         → Pantallas (menú, selección de personaje, pausa, ranking)
BIT.Data       → ScriptableObjects de datos
BIT.Interactables → Pickups y trampas
BIT.Audio      → Pool de audio
BIT.Editor     → Herramientas de setup del proyecto (solo en editor)
```

---

## 3. Objetos del juego

### Personajes jugables

| Personaje | Color | HP | Velocidad | Daño | Cooldown | Estilo |
|---|---|---|---|---|---|---|
| Ninja Azul | Azul | 100 | 6 | 22 | 0,3 s | Equilibrado |
| Ninja Rojo | Rojo | 80 | 5 | 40 | 0,5 s | Guerrero (alto daño) |
| Ninja Verde | Verde | 140 | 8,5 | 15 | 0,22 s | Explorador (rápido y resistente) |

Todos los sprites de los ninjas provienen del pack **Ninja Adventure** (CC0).

### Enemigos

| Enemigo | Tipo | Prefab | Descripción |
|---|---|---|---|
| Esqueleto | Melee básico | Enemy_Skeleton.prefab | Disponible desde la oleada 1. IA con FSM completa |
| Dragón | Melee rápido | Enemy_Dragon.prefab | Disponible desde la oleada 3. Mayor velocidad |
| Cíclope | Melee tanque | Enemy_Cyclope.prefab | Disponible desde la oleada 5. Mayor HP y daño |
| Enemigo a distancia | Ranged | Generado en runtime | Esqueleto con tinte azul + `RangedEnemyAI`. Disponible desde la oleada 2. Mantiene distancia y dispara proyectiles cada ~2,8 s |
| Boss | Tanque × 8 | Cíclope escalado | Aparece en las oleadas 10, 20… con HP, daño y tamaño multiplicados |

Las estadísticas se escalan automáticamente a razón de +15% por oleada (`ScaleEnemyStats`).

### Objetos interactivos / pickups

| Objeto | Prefab | Efecto |
|---|---|---|
| Corazón | Heart.prefab | Restaura 25 HP al jugador |
| Moneda | Coin.prefab | Añade puntuación |
| Trampa de daño | DamageHazard | Resta HP al tocar |
| Trampa lenta | SlowHazard | Reduce velocidad temporalmente |
| Bloque empujable | PushableObject | Interactivo con física |

### Escenario
El mapa es una **mazmorra 2D generada proceduralmente** mediante el editor personalizado `DungeonMapGenerator`. Usa tiles del pack Ninja Adventure (suelo, paredes, caras de pared) organizados en capas de Tilemap separadas.

### Licencias de los objetos

Ver sección **11. Recursos utilizados**.

---

## 4. Escenas

### Escenas del proyecto

| Escena | Función |
|---|---|
| **MainMenu** | Menú principal con botones Jugar, Ranking y Salir. UI generada en runtime por `MainMenuUI` |
| **CharacterSelect** | Pantalla de selección de los 3 ninjas. Muestra estadísticas, retrato facial (`Faceset.png`) y permite elegir antes de la partida. UI generada en runtime por `CharacterSelectUI` |
| **gamesetupscene** | Escena principal del juego. Contiene el jugador, el mapa, todos los managers y el sistema de oleadas |
| **TestScene** | Escena de pruebas de desarrollo |

### Distribución de objetos en la escena principal (`gamesetupscene`)

```
Escena
├── WaveManager         (WaveManager + ComboManager + LevelProgressionManager + WaveUpgradeSystem)
├── RuntimeGameManager  (RuntimeGameManager — HUD, audio, flujo de juego)
├── VFXManager          (VFXManager — efectos visuales)
├── Player              (PlayerController + AutoShooter + Rigidbody2D + Collider2D)
├── Camera              (Camera + CameraFollow)
└── Tilemap Grid        (varios Tilemap layers: Floor, Walls, WallFace)
```

Los enemigos y pickups se crean en runtime por el `WaveManager` y `EnemyDropper` respectivamente.

---

## 5. Física y colisiones

### Rigidbodies utilizados
Todos los actores dinámicos tienen **Rigidbody2D** con `gravityScale = 0` (juego top-down, sin gravedad) y `freezeRotation = true` para evitar rotaciones por colisión.

| Actor | Tipo RB | Detección |
|---|---|---|
| Jugador | Rigidbody2D | `ContinuousDetection` |
| Enemigos melee | Rigidbody2D | Discreto |
| Proyectiles (shurikens) | Rigidbody2D | Discreto |
| Enemigo a distancia | Rigidbody2D | Discreto |

### Colliders utilizados

| Collider | Actor | Uso |
|---|---|---|
| `CapsuleCollider2D` | Jugador | Colisión con el mapa y recogida de pickups |
| `CircleCollider2D` | Enemigos | Colisión entre actores |
| `CircleCollider2D (trigger)` | Proyectiles | Detección de impacto sin física (trigger) |
| `TilemapCollider2D` | Paredes | Bloqueo del movimiento en el mapa |
| `CompositeCollider2D` | Paredes | Optimización de la geometría del tilemap |

### Interacción entre objetos

- **Ataque melee**: usa `Physics2D.OverlapCircleAll()` centrado 0,5 unidades delante del jugador con radio 1,2 u para detectar todos los `Collider2D` con tag `"Enemy"` en el área de golpe.
- **Proyectiles**: usan `OnTriggerEnter2D` para detectar impactos. Si golpean un enemigo, llaman a `TakeDamage()`. Si golpean una pared (TilemapCollider2D), se destruyen.
- **Pickups**: el jugador los recoge mediante `OnTriggerEnter2D`, comprobando los tags `"Health"` y `"Coin"`.
- **Dash**: durante el dash, `OverlapCircleAll()` comprueba continuamente el radio del jugador para golpear enemigos con daño doble.

---

## 6. Animaciones

### Sistema de animación
El proyecto **no usa Animator Controller** de Unity. Las animaciones se gestionan íntegramente **por código** en `PlayerController.cs` para maximizar el control y evitar problemas con el New Input System.

El método `UpdateAnimations()` cicla manualmente por arrays de sprites cargados desde los assets:

```csharp
bool moving = moveInput.magnitude > 0.1f;
Sprite[] frames = moving ? _walkSprites : _idleSprites;
float fps = moving ? 10f : 4f;
_animTimer += Time.deltaTime;
if (_animTimer >= 1f / fps)
{
    _animFrame = (_animFrame + 1) % frames.Length;
    spriteRenderer.sprite = frames[_animFrame];
}
```

### Sprites de animación utilizados (por personaje)

Todos provienen de las carpetas `SeparateAnim/` del pack Ninja Adventure:

| Archivo | Dimensiones | Frames | Uso |
|---|---|---|---|
| `Idle.png` | 64×16 px | 4 | Animación de reposo (mismo para todos) |
| `Walk.png` | 64×64 px | 10 sprites | Animación de caminar (4 direcciones) |
| `Attack.png` | 64×16 px | 4 | Animación de ataque (flash naranja) |

### Animación del ataque (melee)

Al atacar, `PlayAttackAnimation()` recorre los frames de `Attack.png` con un tinte naranja-rojizo para indicar visualmente el golpe, repitiéndolos 2 veces si la animación tiene ≤ 2 frames.

### Efectos visuales animados (VFXManager)

El `VFXManager` genera efectos animados mediante código:

| Efecto | Descripción |
|---|---|
| **Katana overlay** | Crea un GameObject temporal con los sprites de `Katana.png` (16 frames), girado hacia la dirección de ataque, escalado ×4 y con fade de opacidad |
| **Slash arc** | Arco procedural (textura generada en código) que aparece, escala y desvanece en 0,25 s |
| **Partículas de impacto** | 6 partículas circulares doradas que salen en direcciones aleatorias y se desvanecen |
| **Partículas de muerte** | 8 partículas grises (humo) con velocidad y fade |
| **Efecto de pickup** | 5 partículas de color suben hacia arriba y se desvanecen |

---

## 7. Interacciones y eventos

### Controles del jugador (New Input System)

El `PlayerController` usa el **New Input System** de Unity con un `PlayerInput` component y un `InputActionAsset`:

| Input | Acción registrada | Resultado |
|---|---|---|
| WASD / Flechas | `Move` (InputAction) | `rb.linearVelocity = moveInput × moveSpeed` |
| Clic izquierdo / Espacio | `Attack` (performed callback) | `TryAttack()` → melee + animación |
| Clic derecho | `Mouse.rightButton` (Update) | `ThrowShuriken()` hacia el cursor |
| Shift | `Keyboard.shiftKey` (Update) | `DashAttack()` coroutine |
| E | `Interact` (performed callback) | Interacción con objetos |
| Escape | `PauseMenuUI` | Pausa el juego (`Time.timeScale = 0`) |

### Lanzamiento de objetos

- **Shuriken manual (RMB)**: se crea un `GameObject` en código con `Rigidbody2D`, velocidad lineal calculada desde la posición del jugador hacia la posición del ratón (`Camera.main.ScreenToWorldPoint`), rotación `angularVelocity = 480°/s` y `CircleCollider2D` trigger. Se auto-destruye tras 4 s.
- **AutoShooter**: cada 1,8 s busca el enemigo más cercano en radio 12 u y lanza un proyectil amarillo automáticamente sin input del jugador.
- **Proyectil del enemigo ranged**: `RangedEnemyAI` crea `EnemyProjectile` apuntando al jugador.

### Eventos del juego (sistema Observer)

El `WaveManager` expone eventos a los que se suscriben la UI y otros managers:

```csharp
public event Action<int> OnWaveStarted;    // → UIManager muestra "RONDA X"
public event Action<int> OnWaveCleared;    // → LevelProgressionManager comprueba level up
public event Action<int> OnEnemyCountChanged; // → HUD actualiza contador de enemigos
```

El `GameEventSO` permite comunicación desacoplada entre GameObjects mediante `ScriptableObjects` como canales de eventos.

---

## 8. Sistema de puntuación y estado

### Cómo funciona

| Fuente de puntos | Cantidad |
|---|---|
| Matar enemigo | Puntos base del `EnemyAI` |
| Multiplicador de combo | ×1,5 (3+ kills) / ×2 (6+ kills) / ×3 (10+ kills) |
| Bonus de oleada superada | 100 + (n_oleada × 50) |

El combo se reinicia si pasan más de 2,5 s sin matar ningún enemigo.

### Cómo se actualiza

```
EnemyAI.Die()
    → ComboManager.RegisterKill(baseScore)  // aplica multiplicador
        → PlayerStatsSO.AddScore(finalScore)
            → RuntimeGameManager.AddScore(finalScore)  // actualiza HUD
```

El `ComboManager` también muestra un widget de texto en la esquina inferior derecha con el multiplicador activo (amarillo / naranja / rojo).

### Leaderboard

Al morir o ganar, `SaveSystem` guarda la puntuación en un archivo JSON local (`ranking.json`). La pantalla de ranking (`RankingUI`) carga las 10 mejores puntuaciones y las muestra con nombre y puntuación.

### Estado del jugador

El `RuntimeGameManager` mantiene y actualiza en el HUD:
- Barra de corazones (vida actual / máxima)
- Puntuación actual
- Número de oleada
- Contador de enemigos vivos
- Nivel actual (`LevelProgressionManager`)

---

## 9. Sonido

### AudioManager

El `AudioManager` implementa un **pool de 8 AudioSources** (round-robin) para reproducir múltiples SFX simultáneamente sin cortes.

El `RuntimeGameManager` gestiona la música de fondo en un `AudioSource` separado con `loop = true`.

### Clips de audio utilizados

Todos los clips provienen del pack **Ninja Adventure** (CC0):

| Clip | Archivo | Evento que lo activa |
|---|---|---|
| Música de fondo | `17 - Fight.ogg` | Al iniciar la escena de juego |
| Música (alternativa 1) | `1 - Adventure Begin.ogg` | Cargado como alternativa |
| Música (alternativa 2) | `10 - Dark Castle.ogg` | Cargado como alternativa |
| Sonido de golpe 1 | `Hit1.wav` | Enemigo recibe daño |
| Sonido de golpe 2 | `Hit2.wav` | Jugador recibe daño |
| Sonido de moneda | `Coin.wav` | Recoger moneda |
| Sonido de curación | `Heal.wav` | Recoger corazón / level up |
| Sonido de ataque | `Slash.wav` | Ataque melee del jugador |

---

## 10. Recursos utilizados

| Asset | Autor | Web | Licencia |
|---|---|---|---|
| Ninja Adventure (personajes, enemigos, tiles, audio, efectos) | Pixel-Boy & AAA | https://pixel-boy.itch.io/ninja-adventure-asset-pack | CC0 1.0 Universal (dominio público) |
| Unity Engine 6 | Unity Technologies | https://unity.com | Unity Personal / Student License |
| TextMeshPro | Unity Technologies | Incluido en Unity | Unity Companion License |
| New Input System | Unity Technologies | Incluido en Unity | Unity Companion License |
| Fuentes de texto | Unity (fuente Legacy Runtime) | Incluido en Unity | Unity Companion License |

> **Nota:** El pack Ninja Adventure incluye CC0 1.0 Universal License (LICENSE.txt en `Assets/_Project/Sprites/Ninja Adventure/`), lo que significa dominio público completo: uso libre, comercial y sin atribución obligatoria.

---

## 11. Dificultades encontradas

### 1. Configuración del New Input System en Unity 6
**Problema:** La acción `Attack` del `InputActionAsset` no detectaba el clic izquierdo porque el proyecto tenía configurado solo el teclado.
**Solución:** Se añadió el binding `<Mouse>/leftButton` al `InputAction "Attack"` y se configuró `InputSystemBootstrap` para inicializar el sistema antes de cualquier escena.

### 2. API de Unity 6 vs documentación antigua
**Problema:** Varias APIs de `PrefabUtility` usadas en tutoriales online (`EditPrefabContentsScope`) no existen en Unity 6.
**Solución:** Se reemplazaron por la secuencia correcta: `LoadPrefabContents()` → modificar → `SaveAsPrefabAsset()` → `UnloadPrefabContents()`.

### 3. Sprites de enemigos aparecían como puntos
**Problema:** Las hojas de sprites de los enemigos generados en runtime (SkullBlue) tenían `spriteMode: Multiple` pero no estaban cortadas, por lo que `LoadAllAssetsAtPath` no devolvía `Sprite` sub-assets.
**Solución:** En `CreateRangedEnemyAtRuntime()` se abandonó la carga manual de sprites y se pasó a instanciar el prefab del esqueleto existente (con sprites ya configurados), aplicando un tinte azul-morado y sustituyendo su IA melee por `RangedEnemyAI`.

### 4. Click izquierdo disparaba shurikens además del ataque melee
**Problema:** `TryAttack()` incluía una llamada a `LaunchProjectile()` si `projectilePrefab != null` en el inspector, haciendo que el LMB ejecutara simultáneamente el melee y el shuriken.
**Solución:** Se eliminó el bloque `LaunchProjectile()` de `TryAttack()`. El shuriken manual queda exclusivamente en `ThrowShuriken()`, llamado desde `Update()` al detectar `rightButton`.

### 5. Animación de espada invisible
**Problema:** El overlay de Katana se mostraba a escala 1,4 sobre sprites de 16 px a 100 PPU = 0,224 unidades de mundo total. El jugador ocupa el mismo espacio, por lo que era indistinguible.
**Solución:** Se aumentó la escala del overlay a ×4 (0,64 u) y el offset a 0,6 u delante del jugador para que sobresalga visiblemente.

### 6. Generación procedural del mapa
**Problema:** El generador de mazmorras necesitaba tile assets cortados de los PNG de tilesets antes de poder crear el mapa.
**Solución:** Se creó `BITAutoSetup` con el menú `BIT → 1. Configurar Tilesets` que corta automáticamente los PNG en tiles 16×16 usando `SpriteDataProviderFactories` y genera los `.asset` de tiles necesarios para el `Tilemap`.

---

## 12. Funcionalidades extra

| Funcionalidad | Descripción |
|---|---|
| **Sistema de combo** | Kills consecutivas con ventana de 2,5 s activan multiplicadores de puntuación (×1,5 / ×2 / ×3) con widget visual de color |
| **Progresión de niveles** | Cada 5 oleadas superadas el jugador sube de nivel, se cura 25 HP y se muestra mensaje en pantalla |
| **Sistema de mejoras (upgrades)** | Entre cada oleada el jugador elige 1 de 3 mejoras aleatorias (velocidad, daño, vida máxima…) |
| **Dash con daño** | El dash causa daño doble a todos los enemigos que toca durante su duración y tiene su propio cooldown e invulnerabilidad |
| **AutoShooter (Vampire Survivors style)** | Arma automática independiente del input del jugador que busca y dispara al enemigo más cercano dentro del rango |
| **Boss cada 10 oleadas** | Oleada de boss con escalado de stats ×8 y tamaño ×1,8; se anuncia con mensaje especial |
| **Horda cada 5 oleadas** | Oleadas de horda con el doble de enemigos normales |
| **Leaderboard local** | Top 10 puntuaciones guardadas en JSON (`ranking.json`) con pantalla de ranking |
| **Generación procedural del mapa** | Editor personalizado (`DungeonMapGenerator`) que genera el mapa de la mazmorra con tiles del asset pack |
| **Menú de pausa** | Tecla Escape congela el juego (`Time.timeScale = 0`) y muestra opciones de reanudar / salir |
| **Herramientas de editor** | Menú BIT en Unity Editor para configurar tilesets, escena y prefabs automáticamente en 2 pasos |
| **Sistema de eventos desacoplado** | `GameEventSO` + `GameEventListenerComponent` implementan el patrón Observer sin dependencias directas entre scripts |

---

## 13. Enlaces a OneDrive

| Recurso | Enlace |
|---|---|
| **Proyecto Unity** | *(pega aquí el enlace de OneDrive al proyecto)* |
| **Ejecutable del juego** | *(pega aquí el enlace de OneDrive al ejecutable)* |

---

*Documento generado para la defensa del proyecto BIT — Módulo Programación Multimedia y Dispositivos Móviles, DAM.*
