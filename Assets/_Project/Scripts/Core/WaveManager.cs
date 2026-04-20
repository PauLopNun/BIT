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
//   Oleada 1:  3 enemigos básicos
//   Oleada 3+: aparecen enemigos rápidos
//   Oleada 5+: aparecen enemigos fuertes
//   Cada 5 rondas: oleada con el doble de enemigos ("Horda")
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

        [Tooltip("Enemigo rápido (disponible desde ronda 3)")]
        [SerializeField] private GameObject _fastEnemyPrefab;

        [Tooltip("Enemigo fuerte (disponible desde ronda 5)")]
        [SerializeField] private GameObject _tankEnemyPrefab;

        [Tooltip("Enemigo a distancia (si está vacío se crea en runtime a partir de ronda 4)")]
        [SerializeField] private GameObject _rangedEnemyPrefab;

        [Tooltip("Ronda a partir de la cual aparecen enemigos a distancia")]
        [SerializeField] private int _rangedEnemyUnlockWave = 2;

        [Header("=== BOSS ===")]
        [Tooltip("Prefab del boss (si está vacío se usa el enemigo fuerte con stats escalados x8)")]
        [SerializeField] private GameObject _bossPrefab;

        [Tooltip("Cada cuántas rondas aparece el boss")]
        [SerializeField] private int _bossEveryNWaves = 10;

        [Header("=== DIFICULTAD ===")]
        [Tooltip("Enemigos en la primera oleada")]
        [SerializeField] private int _baseEnemyCount = 3;

        [Tooltip("Enemigos extra añadidos por cada ronda")]
        [SerializeField] private int _enemiesPerRoundIncrease = 2;

        [Tooltip("Ronda a partir de la cual aparecen enemigos rápidos")]
        [SerializeField] private int _fastEnemyUnlockWave = 3;

        [Tooltip("Ronda a partir de la cual aparecen enemigos fuertes")]
        [SerializeField] private int _tankEnemyUnlockWave = 5;

        [Tooltip("Cada cuántas rondas hay una 'Horda' (doble de enemigos)")]
        [SerializeField] private int _hordeEveryNWaves = 5;

        [Header("=== SPAWNING ===")]
        [Tooltip("Puntos de spawn (si están vacíos, se usan posiciones aleatorias)")]
        [SerializeField] private Transform[] _spawnPoints;

        [Tooltip("Área de spawn aleatorio: mitad del ancho (si no hay spawn points)")]
        [SerializeField] private float _spawnAreaHalfWidth = 7f;

        [Tooltip("Área de spawn aleatorio: mitad del alto")]
        [SerializeField] private float _spawnAreaHalfHeight = 4f;

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
        }

        private void Start()
        {
            // Buscamos al jugador
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                _playerTransform = player.transform;

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

                    GameObject enemy = SpawnEnemy();
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
                float bossScale = 1f + (_currentWave / _bossEveryNWaves - 1) * 0.5f;
                bossAI.ScaleStats(Mathf.Max(1f, bossScale));
            }
            else
            {
                // Sin BossEnemyAI: escalar el enemigo normal x8 para que actúe de boss
                ScaleEnemyStats(boss, 8f);
                boss.transform.localScale = Vector3.one * 1.8f;
            }

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
            int count = _baseEnemyCount + (_currentWave - 1) * _enemiesPerRoundIncrease;

            // Las oleadas de boss tienen prioridad — no son horda
            bool isBossWave = _bossEveryNWaves > 0 && _currentWave % _bossEveryNWaves == 0;
            if (!isBossWave && _currentWave % _hordeEveryNWaves == 0)
            {
                count *= 2;
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
        private GameObject SpawnEnemy()
        {
            Vector3 spawnPos = GetSpawnPosition();
            GameObject enemy;

            // 25% chance of spawning a ranged enemy once unlocked
            if (_currentWave >= _rangedEnemyUnlockWave && Random.value < 0.25f)
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

            ScaleEnemyStats(enemy);
            return enemy;
        }

        private GameObject CreateRangedEnemyAtRuntime(Vector3 pos)
        {
            var go = new GameObject("Enemy_Ranged");
            go.transform.position = pos;
            go.tag = "Enemy";

            var sr = go.AddComponent<SpriteRenderer>();
            sr.color = new Color(0.5f, 0.2f, 1f); // purple to distinguish from melee
            sr.sortingOrder = 2;

            // Try to load SkullBlue sprite
            var sprite = LoadFirstAvailableSprite(
                "Assets/_Project/Sprites/Ninja Adventure/Actor/Monster/SkullBlue/SpriteSheet.png",
                "Assets/_Project/Sprites/Ninja Adventure/Actor/Monster/Skull/SpriteSheet.png");
            if (sprite != null) sr.sprite = sprite;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;

            var col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.4f;

            go.AddComponent<BIT.Enemy.RangedEnemyAI>();
            return go;
        }

        static Sprite LoadFirstAvailableSprite(params string[] paths)
        {
#if UNITY_EDITOR
            foreach (var path in paths)
            {
                var sp = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sp != null) return sp;
                foreach (var a in UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path))
                    if (a is Sprite s) return s;
            }
#endif
            return null;
        }

        /// <summary>Escala las estadísticas del enemigo según la ronda.</summary>
        private void ScaleEnemyStats(GameObject enemy)
        {
            if (_currentWave <= 1) return;
            float scaleFactor = 1f + (_currentWave - 1) * 0.15f; // +15% por ronda
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

        /// <summary>Obtiene una posición de spawn válida (alejada del jugador).</summary>
        private Vector3 GetSpawnPosition()
        {
            // Si hay puntos de spawn definidos, los usamos
            if (_spawnPoints != null && _spawnPoints.Length > 0)
            {
                Transform point = _spawnPoints[Random.Range(0, _spawnPoints.Length)];
                return point.position;
            }

            // Si no, posición aleatoria dentro del área
            Vector3 pos;
            int maxAttempts = 20;

            do
            {
                float x = Random.Range(-_spawnAreaHalfWidth, _spawnAreaHalfWidth);
                float y = Random.Range(-_spawnAreaHalfHeight, _spawnAreaHalfHeight);
                pos = new Vector3(x, y, 0f);
                maxAttempts--;
            }
            while (_playerTransform != null
                   && Vector2.Distance(pos, _playerTransform.position) < _minSpawnDistanceFromPlayer
                   && maxAttempts > 0);

            return pos;
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
        public void RegisterBossMinion(GameObject minion)
        {
            if (minion != null && !_activeEnemies.Contains(minion))
            {
                _activeEnemies.Add(minion);
                OnEnemyCountChanged?.Invoke(CountAliveEnemies());
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
