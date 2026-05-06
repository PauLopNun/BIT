using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BIT.Data;
using BIT.Enemy;
using BIT.UI;

// ============================================================================
// WAVEMANAGER.CS - Sistema de oleadas (Rounds)
// ============================================================================
// Controla el spawn de enemigos por oleadas. Cada ronda spawnea más enemigos
// y a partir de ciertos umbrales añade tipos más fuertes.
//
// FLUJO:
//   StartWave() → spawna N enemigos → espera a que todos mueran
//   → muestra mensaje "Ronda X superada!" → pausa breve → siguiente ronda
//
// ESCALADO DE DIFICULTAD:
//   Oleada 1+: mezcla enemigo basico, rapido, fuerte y a distancia
//   Oleadas siguientes: sube el numero de enemigos y escalan sus stats
//   Cada N rondas: oleada especial de boss u horda
// ============================================================================

namespace BIT.Core
{
    public class WaveManager : MonoBehaviour
    {
        // ====================================================================
        // SINGLETON
        // ====================================================================
        public static WaveManager Instance { get; private set; }

        // ====================================================================
        // CONFIGURACIÓN
        // ====================================================================

        [Header("=== PREFABS DE ENEMIGOS ===")]
        [Tooltip("Enemigo básico (siempre disponible desde ronda 1)")]
        [SerializeField] private GameObject _basicEnemyPrefab;

        [Tooltip("Enemigo rápido (disponible desde ronda 1)")]
        [SerializeField] private GameObject _fastEnemyPrefab;

        [Tooltip("Enemigo fuerte (disponible desde ronda 1)")]
        [SerializeField] private GameObject _tankEnemyPrefab;

        [Tooltip("Enemigo a distancia (si está vacío se crea en runtime desde ronda 1)")]
        [SerializeField] private GameObject _rangedEnemyPrefab;

        [Tooltip("Ronda a partir de la cual aparecen enemigos a distancia")]
        [SerializeField] private int _rangedEnemyUnlockWave = 1;

        [Header("=== BOSS ===")]
        [Tooltip("Prefab del boss (si está vacío se usa el enemigo fuerte con stats escalados x8)")]
        [SerializeField] private GameObject _bossPrefab;

        [Tooltip("Cada cuántas rondas aparece el boss")]
        [SerializeField] private int _bossEveryNWaves = 3;

        [Header("=== DIFICULTAD ===")]
        [Tooltip("Enemigos en la primera oleada")]
        [SerializeField] private int _baseEnemyCount = 2;

        [Tooltip("Enemigos extra añadidos por cada ronda")]
        [SerializeField] private int _enemiesPerRoundIncrease = 1;

        [Tooltip("Ronda a partir de la cual aparecen enemigos rápidos")]
        [SerializeField] private int _fastEnemyUnlockWave = 1;

        [Tooltip("Ronda a partir de la cual aparecen enemigos fuertes")]
        [SerializeField] private int _tankEnemyUnlockWave = 1;

        [Tooltip("Cada cuántas rondas hay una 'Horda' (x1.5 enemigos). 0 = desactivado")]
        [SerializeField] private int _hordeEveryNWaves = 8;

        [Header("=== SPAWNING ===")]
        [Tooltip("Puntos de spawn (si están vacíos, se usan posiciones aleatorias)")]
        [SerializeField] private Transform[] _spawnPoints;

        [Tooltip("Distancia mínima al jugador para spawnear")]
        [SerializeField] private float _minSpawnDistanceFromPlayer = 3f;

        [Header("=== TIEMPOS ===")]
        [Tooltip("Segundos de espera entre rondas")]
        [SerializeField] private float _timeBetweenWaves = 3f;

        [Tooltip("Tiempo entre spawns individuales de la misma oleada")]
        [SerializeField] private float _timeBetweenSpawns = 0.4f;

        [Header("=== PUNTUACIÓN ===")]
        [Tooltip("Puntos bonus por completar una ronda")]
        [SerializeField] private int _waveClearBonusScore = 100;

        [Tooltip("Multiplicador de bonus por cada ronda completada")]
        [SerializeField] private int _waveBonusMultiplier = 50;

        [Header("=== REFERENCIAS ===")]
        [SerializeField] private PlayerStatsSO _playerStats;

        // ====================================================================
        // ESTADO
        // ====================================================================

        private int _currentWave = 0;
        private bool _waveActive = false;
        private List<GameObject> _activeEnemies = new List<GameObject>();
        private Transform _playerTransform;

        // ====================================================================
        // PROPIEDADES PÚBLICAS
        // ====================================================================
        public int CurrentWave => _currentWave;
        public bool WaveActive => _waveActive;
        public int AliveEnemyCount => CountAliveEnemies();
        public bool LastWaveWasBoss => _bossEveryNWaves > 0 && _currentWave % _bossEveryNWaves == 0;

        // Evento para que la UI escuche
        public event System.Action<int> OnWaveStarted;
        public event System.Action<int> OnWaveCleared;
        public event System.Action<int> OnEnemyCountChanged;

        // ====================================================================
        // INICIALIZACIÓN
        // ====================================================================

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            EnsureEarlyEnemyVariety();
        }

        private void EnsureEarlyEnemyVariety()
        {
            _fastEnemyUnlockWave = 1;
            _tankEnemyUnlockWave = 1;
            _rangedEnemyUnlockWave = 1;
        }

        private void Start()
        {
            // Buscamos al jugador
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                _playerTransform = player.transform;

            // Si los spawn points no están asignados en el inspector, buscarlos en la escena
            if (_spawnPoints == null || _spawnPoints.Length == 0)
            {
                var container = GameObject.Find("SpawnPoints");
                if (container != null)
                {
                    _spawnPoints = new Transform[container.transform.childCount];
                    for (int i = 0; i < container.transform.childCount; i++)
                        _spawnPoints[i] = container.transform.GetChild(i);
                }
            }

            // Si no hay GameManager, inicializamos el PlayerStatsSO directamente
            if (GameManager.Instance == null && _playerStats != null)
                _playerStats.Initialize();

            // Escuchamos el GameManager para saber cuándo empieza el juego
            if (GameManager.Instance != null)
                GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;

            // Si ya estamos en Playing, empezamos directamente
            if (GameManager.Instance == null || GameManager.Instance.IsPlaying)
                StartNextWave();
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
        }

        private void HandleGameStateChanged(GameState state)
        {
            if (state == GameState.Playing && _currentWave == 0)
                StartNextWave();
        }

        // ====================================================================
        // CONTROL DE OLEADAS
        // ====================================================================

        /// <summary>Inicia la siguiente oleada.</summary>
        public void StartNextWave()
        {
            _currentWave++;
            _waveActive = true;

            Debug.Log($"[WaveManager] === OLEADA {_currentWave} ===");

            // Notificamos a la UI
            OnWaveStarted?.Invoke(_currentWave);
            UIManager.Instance?.ShowWaveMessage($"RONDA {_currentWave}", isStart: true);

            StartCoroutine(SpawnWave());
        }

        /// <summary>Corrutina que spawna todos los enemigos de la oleada.</summary>
        private IEnumerator SpawnWave()
        {
            _activeEnemies.Clear();

            bool isBossWave = _bossEveryNWaves > 0 && _currentWave % _bossEveryNWaves == 0;

            if (isBossWave)
            {
                yield return StartCoroutine(SpawnBossWave());
            }
            else
            {
                int enemiesToSpawn = CalculateEnemyCount();
                Debug.Log($"[WaveManager] Spawneando {enemiesToSpawn} enemigos");

                for (int i = 0; i < enemiesToSpawn; i++)
                {
                    if (GameManager.Instance != null && !GameManager.Instance.IsPlaying)
                        yield break;

                    GameObject enemy = SpawnEnemy(i);
                    if (enemy != null)
                        _activeEnemies.Add(enemy);

                    OnEnemyCountChanged?.Invoke(CountAliveEnemies());

                    yield return new WaitForSeconds(_timeBetweenSpawns);
                }
            }

            yield return StartCoroutine(WaitForWaveClear());
        }

        /// <summary>Spawna la oleada del boss.</summary>
        private IEnumerator SpawnBossWave()
        {
            Debug.Log($"[WaveManager] ¡¡OLEADA DE BOSS!! Ronda {_currentWave}");

            UIManager.Instance?.ShowWaveMessage($"¡¡OLEADA JEFE!! RONDA {_currentWave}", isStart: true);
            RuntimeGameManager.Instance?.ShowBigMessage("¡¡BOSS INCOMING!!", Color.red);

            yield return new WaitForSeconds(1.5f);

            // Usar _bossPrefab si está asignado; si no, usar _tankEnemyPrefab con stats x8
            GameObject bossPrefab = _bossPrefab != null ? _bossPrefab : _tankEnemyPrefab;
            if (bossPrefab == null) bossPrefab = _basicEnemyPrefab;
            if (bossPrefab == null)
            {
                Debug.LogError("[WaveManager] No hay prefab de boss ni de enemigos asignado.");
                yield break;
            }

            Vector3 spawnPos = GetSpawnPosition();
            GameObject boss = Instantiate(bossPrefab, spawnPos, Quaternion.identity);

            // Si tiene BossEnemyAI, aplicar escalado de boss
            var bossAI = boss.GetComponent<BossEnemyAI>();
            if (bossAI != null)
            {
                float bossScale = 1f + (_currentWave / _bossEveryNWaves - 1) * 0.3f;
                bossAI.ScaleStats(Mathf.Max(1f, bossScale));
            }
            else
            {
                ScaleEnemyStats(boss, 3f);
                boss.transform.localScale = Vector3.one * 1.4f;
            }

            // Intentar aplicar sprite único de boss (TRex/Skull, no aparecen en oleadas normales)
            ApplyBossVisual(boss);

            _activeEnemies.Add(boss);
            OnEnemyCountChanged?.Invoke(CountAliveEnemies());
        }

        /// <summary>Espera hasta que no queden enemigos vivos.</summary>
        private IEnumerator WaitForWaveClear()
        {
            while (CountAliveEnemies() > 0)
            {
                OnEnemyCountChanged?.Invoke(CountAliveEnemies());
                yield return new WaitForSeconds(0.5f);
            }

            // Oleada superada
            WaveCleared();
        }

        /// <summary>Lógica de victoria de oleada.</summary>
        private void WaveCleared()
        {
            _waveActive = false;
            Debug.Log($"[WaveManager] Oleada {_currentWave} superada!");

            // Bonus de puntuación
            int bonus = _waveClearBonusScore + (_currentWave * _waveBonusMultiplier);
            _playerStats?.AddScore(bonus);

            OnWaveCleared?.Invoke(_currentWave);
            UIManager.Instance?.ShowWaveMessage($"¡RONDA {_currentWave} SUPERADA! +{bonus}", isStart: false);

            // Curamos un poco al jugador entre rondas
            int healAmount = 10 + (_currentWave * 2);
            _playerStats?.Heal(healAmount);

            // Iniciamos la siguiente oleada tras un delay
            StartCoroutine(NextWaveDelay());
        }

        private IEnumerator NextWaveDelay()
        {
            yield return new WaitForSeconds(_timeBetweenWaves);

            if (GameManager.Instance == null || GameManager.Instance.IsPlaying)
                StartNextWave();
        }

        // ====================================================================
        // CÁLCULO DE DIFICULTAD
        // ====================================================================

        /// <summary>Calcula cuántos enemigos spawnear en la oleada actual.</summary>
        private int CalculateEnemyCount()
        {
            // Curva no lineal: arranca con fuerza y crece más despacio
            // wave 1=5, 2=8, 4=10, 5=11, 7=12, 8=13, 10+=14 (cap)
            int configuredCount = Mathf.Max(1,
                _baseEnemyCount + ((_currentWave - 1) * Mathf.Max(0, _enemiesPerRoundIncrease)));
            int curveCount;
            if (_currentWave == 1)      curveCount = 5;
            else if (_currentWave == 2) curveCount = 8;
            else                        curveCount = Mathf.Min(14, 8 + (_currentWave - 2));

            int count = Mathf.Max(configuredCount, curveCount);

            bool isBossWave = _bossEveryNWaves > 0 && _currentWave % _bossEveryNWaves == 0;
            if (!isBossWave && _hordeEveryNWaves > 0 && _currentWave % _hordeEveryNWaves == 0)
            {
                count = Mathf.RoundToInt(count * 1.5f);
                Debug.Log($"[WaveManager] ¡OLEADA HORDA! Enemigos: {count}");
                UIManager.Instance?.ShowWaveMessage($"¡¡HORDA!! RONDA {_currentWave}", isStart: true);
            }

            return count;
        }

        /// <summary>Elige el tipo de enemigo a spawnear según la ronda.</summary>
        private GameObject ChooseEnemyPrefab()
        {
            List<GameObject> available = new List<GameObject>();

            if (_basicEnemyPrefab != null)
                available.Add(_basicEnemyPrefab);

            if (_fastEnemyPrefab != null && _currentWave >= _fastEnemyUnlockWave)
                available.Add(_fastEnemyPrefab);

            if (_tankEnemyPrefab != null && _currentWave >= _tankEnemyUnlockWave)
                available.Add(_tankEnemyPrefab);

            if (available.Count == 0)
            {
                Debug.LogError("[WaveManager] No hay prefabs de enemigos asignados!");
                return null;
            }

            // Más probabilidad de enemigos básicos en oleadas tempranas
            // En oleadas avanzadas, más variedad
            if (_currentWave <= 3 || available.Count == 1)
                return available[Random.Range(0, available.Count)];

            // Pesos: básico 40%, rápido 35%, fuerte 25%
            float roll = Random.value;
            if (roll < 0.4f && available.Contains(_basicEnemyPrefab))
                return _basicEnemyPrefab;
            if (roll < 0.75f && available.Contains(_fastEnemyPrefab))
                return _fastEnemyPrefab;
            if (available.Contains(_tankEnemyPrefab))
                return _tankEnemyPrefab;

            return available[Random.Range(0, available.Count)];
        }

        // ====================================================================
        // SPAWNING
        // ====================================================================

        /// <summary>Spawna un enemigo en una posición válida.</summary>
        private GameObject SpawnEnemy(int spawnIndex)
        {
            Vector3 spawnPos = GetSpawnPosition();
            GameObject enemy = TrySpawnGuaranteedVarietyEnemy(spawnPos, spawnIndex);

            if (enemy == null)
            {
                float roll = Random.value;
                // Enemigo pesado (Cyclope): desde ronda 1, probabilidad crece con las oleadas
                float heavyChance = _currentWave >= 5 ? 0.22f : _currentWave >= 3 ? 0.15f : 0.08f;
                if (roll < heavyChance)
                {
                    enemy = CreateHeavyEnemyAtRuntime(spawnPos);
                }
                // Enemigo a distancia desde rondas tempranas para aportar variedad visual.
                else if (_currentWave >= _rangedEnemyUnlockWave && roll < 0.40f)
                {
                    enemy = _rangedEnemyPrefab != null
                        ? Instantiate(_rangedEnemyPrefab, spawnPos, Quaternion.identity)
                        : CreateRangedEnemyAtRuntime(spawnPos);
                }
                else
                {
                    GameObject prefab = ChooseEnemyPrefab();
                    if (prefab == null) return null;
                    enemy = Instantiate(prefab, spawnPos, Quaternion.identity);
                }
            }

            ScaleEnemyStats(enemy);
            MakeEnemyIgnoreWalls(enemy);

            // Ensure prefab-based enemies have item drops configured
            var dropper = enemy.GetComponent<BIT.Enemy.EnemyDropper>();
            if (dropper == null)
            {
                dropper = enemy.AddComponent<BIT.Enemy.EnemyDropper>();
                var heartPrefab = LoadPickupPrefab("Heart");
                var coinPrefab  = LoadPickupPrefab("Coin");
                if (heartPrefab != null) dropper.AddDrop(heartPrefab, 0.40f);
                if (coinPrefab  != null) dropper.AddDrop(coinPrefab,  0.70f);
            }

            return enemy;
        }

        private GameObject TrySpawnGuaranteedVarietyEnemy(Vector3 spawnPos, int spawnIndex)
        {
            if (spawnIndex == 0 && _basicEnemyPrefab != null)
                return Instantiate(_basicEnemyPrefab, spawnPos, Quaternion.identity);

            if (spawnIndex == 1 && _currentWave >= _fastEnemyUnlockWave && _fastEnemyPrefab != null)
                return Instantiate(_fastEnemyPrefab, spawnPos, Quaternion.identity);

            if (spawnIndex == 2 && _currentWave >= _tankEnemyUnlockWave && _tankEnemyPrefab != null)
                return Instantiate(_tankEnemyPrefab, spawnPos, Quaternion.identity);

            if (spawnIndex == 3 && _currentWave >= _rangedEnemyUnlockWave)
            {
                return _rangedEnemyPrefab != null
                    ? Instantiate(_rangedEnemyPrefab, spawnPos, Quaternion.identity)
                    : CreateRangedEnemyAtRuntime(spawnPos);
            }

            return null;
        }

        private GameObject CreateRangedEnemyAtRuntime(Vector3 pos)
        {
            // Use the skeleton prefab as visual base — guaranteed to have real sprites
            GameObject basePrefab = _basicEnemyPrefab ?? _fastEnemyPrefab ?? _tankEnemyPrefab;

            GameObject go;
            if (basePrefab != null)
            {
                go = Instantiate(basePrefab, pos, Quaternion.identity);
                go.name = "Enemy_Ranged";

                // Sprite de Flam (calavera de fuego) para diferenciarlo visualmente
                var sr = go.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    Sprite flamSprite = LoadFirstAvailableSprite(
                        "Assets/_Project/Sprites/Ninja Adventure/Actor/Monster/Flam/SpriteSheet.png",
                        "Assets/_Project/Sprites/Ninja Adventure/Actor/Monster/Flam2/SpriteSheet.png");
                    if (flamSprite != null) { sr.sprite = flamSprite; sr.color = Color.white; }
                    else sr.color = new Color(1f, 0.5f, 0.1f); // naranja como fallback
                }

                // Remove existing melee AI components and add ranged AI
                var existingEnemyAI = go.GetComponent<BIT.Enemy.EnemyAI>();
                if (existingEnemyAI != null) Destroy(existingEnemyAI);
                var existingSimpleAI = go.GetComponent<SimpleEnemyAI>();
                if (existingSimpleAI != null) Destroy(existingSimpleAI);

                if (go.GetComponent<BIT.Enemy.RangedEnemyAI>() == null)
                    go.AddComponent<BIT.Enemy.RangedEnemyAI>();
            }
            else
            {
                // Fallback if no prefab is assigned yet
                go = new GameObject("Enemy_Ranged");
                go.transform.position = pos;
                go.tag = "Enemy";

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sortingOrder = 2;
                Sprite flamFallback = LoadFirstAvailableSprite(
                    "Assets/_Project/Sprites/Ninja Adventure/Actor/Monster/Flam/SpriteSheet.png",
                    "Assets/_Project/Sprites/Ninja Adventure/Actor/Monster/Flam2/SpriteSheet.png");
                if (flamFallback != null) { sr.sprite = flamFallback; sr.color = Color.white; }
                else sr.color = new Color(1f, 0.5f, 0.1f);

                var rb = go.AddComponent<Rigidbody2D>();
                rb.gravityScale = 0f;
                rb.freezeRotation = true;

                var col = go.AddComponent<CircleCollider2D>();
                col.radius = 0.4f;

                go.AddComponent<BIT.Enemy.RangedEnemyAI>();
            }

            // Item drops so ranged enemies feel rewarding to kill
            var dropper = go.GetComponent<BIT.Enemy.EnemyDropper>();
            if (dropper == null)
            {
                dropper = go.AddComponent<BIT.Enemy.EnemyDropper>();
                var heartPrefab = LoadPickupPrefab("Heart");
                var coinPrefab  = LoadPickupPrefab("Coin");
                if (heartPrefab != null) dropper.AddDrop(heartPrefab, 0.40f);
                if (coinPrefab  != null) dropper.AddDrop(coinPrefab,  0.70f);
            }

            return go;
        }

        private GameObject CreateHeavyEnemyAtRuntime(Vector3 pos)
        {
            GameObject basePrefab = _tankEnemyPrefab ?? _basicEnemyPrefab;
            GameObject go = basePrefab != null
                ? Instantiate(basePrefab, pos, Quaternion.identity)
                : new GameObject("Enemy_Heavy") { tag = "Enemy" };

            go.name = "Enemy_Heavy";
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * 1.3f;

            var sr = go.GetComponent<SpriteRenderer>() ?? go.AddComponent<SpriteRenderer>();
            Sprite heavySprite = LoadFirstAvailableSprite(
                "Assets/_Project/Sprites/Ninja Adventure/Actor/Monster/Cyclope/SpriteSheet.png",
                "Assets/_Project/Sprites/Ninja Adventure/Actor/Monster/Cyclope2/SpriteSheet.png",
                "Assets/_Project/Sprites/Ninja Adventure/Actor/Monster/Bear/SpriteSheet.png");
            if (heavySprite != null) { sr.sprite = heavySprite; sr.color = Color.white; }
            else sr.color = new Color(0.6f, 0.3f, 1f);

            // Stats: más vida y daño, más lento
            var simpleAI = go.GetComponent<SimpleEnemyAI>();
            if (simpleAI != null)
            {
                simpleAI.ScaleStats(2.0f);
                simpleAI.moveSpeed = Mathf.Max(0.8f, simpleAI.moveSpeed * 0.65f);
            }

            var dropper = go.GetComponent<BIT.Enemy.EnemyDropper>() ?? go.AddComponent<BIT.Enemy.EnemyDropper>();
            var heartPrefab = LoadPickupPrefab("Heart");
            var coinPrefab  = LoadPickupPrefab("Coin");
            if (heartPrefab != null) dropper.AddDrop(heartPrefab, 0.55f);
            if (coinPrefab  != null) dropper.AddDrop(coinPrefab,  0.80f);

            return go;
        }

        static GameObject LoadPickupPrefab(string name)
        {
            return RuntimeAssetLoader.LoadPickupPrefab(name);
        }

        static Sprite LoadFirstAvailableSprite(params string[] paths)
        {
            return RuntimeAssetLoader.LoadFirstAvailableSprite(paths);
        }

        /// <summary>Escala las estadísticas del enemigo según la ronda.</summary>
        private void ScaleEnemyStats(GameObject enemy)
        {
            if (_currentWave <= 1) return;
            float scaleFactor = 1f + (_currentWave - 1) * 0.08f; // +8% por ronda (más gradual)
            ScaleEnemyStats(enemy, scaleFactor);
        }

        private void ScaleEnemyStats(GameObject enemy, float scaleFactor)
        {
            var ai = enemy.GetComponent<EnemyAI>();
            if (ai != null) { ai.ScaleStats(scaleFactor); return; }

            var simpleAI = enemy.GetComponent<SimpleEnemyAI>();
            if (simpleAI != null) { simpleAI.ScaleStats(scaleFactor); return; }

            var rangedAI = enemy.GetComponent<BIT.Enemy.RangedEnemyAI>();
            if (rangedAI != null) { rangedAI.ScaleStats(scaleFactor); return; }

            var bossAI = enemy.GetComponent<BossEnemyAI>();
            if (bossAI != null) bossAI.ScaleStats(scaleFactor);
        }

        // Límites del mapa cacheados para no recalcularlos cada spawn
        private Bounds _mapBounds;
        private bool _mapBoundsCached;

        /// <summary>Obtiene una posición de spawn válida alrededor del jugador y dentro del mapa.</summary>
        private Vector3 GetSpawnPosition()
        {
            if (_playerTransform == null) return Vector3.zero;

            // Primero: usar spawn points pre-calculados por el generador de mapa (son seguros)
            if (_spawnPoints != null && _spawnPoints.Length > 0)
                return _spawnPoints[Random.Range(0, _spawnPoints.Length)].position;

            // Cachear los límites del mapa a partir del Tilemap
            if (!_mapBoundsCached) CacheMapBounds();

            for (int i = 0; i < 40; i++)
            {
                float angle    = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float distance = Random.Range(_minSpawnDistanceFromPlayer, _minSpawnDistanceFromPlayer + 5f);
                Vector2 candidate = (Vector2)_playerTransform.position
                                  + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;

                // Acotar al área interior del mapa (margen de 2 unidades respecto al borde)
                if (_mapBounds.size != Vector3.zero)
                {
                    candidate.x = Mathf.Clamp(candidate.x, _mapBounds.min.x + 2f, _mapBounds.max.x - 2f);
                    candidate.y = Mathf.Clamp(candidate.y, _mapBounds.min.y + 2f, _mapBounds.max.y - 2f);
                }

                bool blocked = false;
                foreach (var h in Physics2D.OverlapCircleAll(candidate, 0.35f))
                    if (!h.isTrigger) { blocked = true; break; }

                if (!blocked) return candidate;
            }

            // Fallback: junto al jugador
            return _playerTransform.position + new Vector3(_minSpawnDistanceFromPlayer + 1f, 0f, 0f);
        }

        private void CacheMapBounds()
        {
            _mapBoundsCached = true;
            var grid = GameObject.Find("Grid");
            if (grid == null) return;

            var tilemaps = grid.GetComponentsInChildren<UnityEngine.Tilemaps.Tilemap>();
            if (tilemaps.Length == 0) return;

            _mapBounds = new Bounds(tilemaps[0].transform.TransformPoint(tilemaps[0].localBounds.center), Vector3.zero);
            foreach (var tm in tilemaps)
            {
                var lb = tm.localBounds;
                _mapBounds.Encapsulate(tm.transform.TransformPoint(lb.min));
                _mapBounds.Encapsulate(tm.transform.TransformPoint(lb.max));
            }
        }

        // ====================================================================
        // CONTEO DE ENEMIGOS
        // ====================================================================

        private int CountAliveEnemies()
        {
            // Limpiamos referencias nulas (enemigos destruidos)
            _activeEnemies.RemoveAll(e => e == null);
            return _activeEnemies.Count;
        }

        // ====================================================================
        // API PÚBLICA (para otros sistemas)
        // ====================================================================

        /// <summary>
        /// Notifica al WaveManager que un enemigo ha muerto.
        /// Llamar desde EnemyAI.Die() o usar FindObjects.
        /// </summary>
        public void NotifyEnemyDied(GameObject enemy)
        {
            _activeEnemies.Remove(enemy);
            OnEnemyCountChanged?.Invoke(CountAliveEnemies());
            Debug.Log($"[WaveManager] Enemigo muerto. Quedan: {CountAliveEnemies()}");
        }

        /// <summary>
        /// Registra un minion invocado por el boss en la lista de enemigos activos.
        /// Así la oleada no termina hasta que también muera el minion.
        /// </summary>
        /// <summary>Devuelve un spawn point aleatorio seguro (usado por EnemyDropper para drops).</summary>
        public Vector3 GetRandomSpawnPoint()
        {
            if (_spawnPoints != null && _spawnPoints.Length > 0)
                return _spawnPoints[Random.Range(0, _spawnPoints.Length)].position;
            // Fallback a posición aleatoria dentro del área si no hay spawn points
            return GetSpawnPosition();
        }

        public void RegisterBossMinion(GameObject minion)
        {
            if (minion != null && !_activeEnemies.Contains(minion))
            {
                _activeEnemies.Add(minion);
                OnEnemyCountChanged?.Invoke(CountAliveEnemies());
            }
        }

        /// <summary>
        /// Hace que el enemigo ignore colisiones con los tiles de pared (CompositeCollider2D)
        /// pero siga chocando con los bordes invisibles del mapa (BoxCollider2D).
        /// </summary>
        private static void MakeEnemyIgnoreWalls(GameObject enemy)
        {
            // El tilemap de paredes se llama "Walls" y tiene CompositeCollider2D
            var wallsGO = GameObject.Find("Walls");
            if (wallsGO == null) return;
            var wallComposite = wallsGO.GetComponent<CompositeCollider2D>();
            if (wallComposite == null) return;

            foreach (var col in enemy.GetComponentsInChildren<Collider2D>(true))
                Physics2D.IgnoreCollision(col, wallComposite, true);
        }

        /// <summary>Aplica sprite y tinte únicos al boss para que se diferencie de los enemigos normales.</summary>
        // Pool de sprites de boss — cada oleada de boss usa uno distinto en orden
        private static readonly string[] _bossSpritePool = {
            "Assets/_Project/Sprites/Ninja Adventure/Actor/Monster/TRex/SpriteSheet.png",
            "Assets/_Project/Sprites/Ninja Adventure/Actor/Monster/Dragon/SpriteSheet.png",
            "Assets/_Project/Sprites/Ninja Adventure/Actor/Monster/Grey Trex/SpriteSheet.png",
            "Assets/_Project/Sprites/Ninja Adventure/Actor/Monster/DragonYellow/SpriteSheet.png",
            "Assets/_Project/Sprites/Ninja Adventure/Actor/Monster/Cyclope/SpriteSheet.png",
            "Assets/_Project/Sprites/Ninja Adventure/Actor/Monster/GoldRacoon/SpriteSheet.png",
        };

        private void ApplyBossVisual(GameObject boss)
        {
            var sr = boss.GetComponent<SpriteRenderer>();
            if (sr == null) return;

            // Cada boss usa un sprite diferente según el número de oleada de boss
            int bossIndex = (_bossEveryNWaves > 0 ? _currentWave / _bossEveryNWaves : 1) - 1;
            string path = _bossSpritePool[bossIndex % _bossSpritePool.Length];

            Sprite bossSprite = LoadFirstAvailableSprite(path,
                "Assets/_Project/Sprites/Ninja Adventure/Actor/Monster/TRex/SpriteSheet.png");

            if (bossSprite != null)
            {
                sr.sprite = bossSprite;
                sr.color = Color.white;
            }
            else
            {
                sr.color = new Color(1f, 0.35f, 0.1f);
            }
        }

        /// <summary>Fuerza el inicio de la siguiente oleada (útil para debug).</summary>
        [ContextMenu("Forzar siguiente oleada")]
        public void ForceNextWave()
        {
            StopAllCoroutines();
            foreach (var e in _activeEnemies)
                if (e != null) Destroy(e);
            _activeEnemies.Clear();

            _waveActive = false;
            StartNextWave();
        }
    }
}
