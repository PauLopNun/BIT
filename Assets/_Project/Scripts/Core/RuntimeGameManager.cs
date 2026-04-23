using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using BIT.Data;

// ============================================================================
// RUNTIMEGAMEMANAGER.CS - Gestiona UI y Audio en tiempo de ejecucion
// ============================================================================
// Este script crea automaticamente la UI y configura el audio sin necesidad
// de prefabs pre-configurados. Ideal para testing rapido.
//
// Se anade automaticamente a la escena desde NinjaAdventureSetup.
// ============================================================================

namespace BIT.Core
{
    public class RuntimeGameManager : MonoBehaviour
    {
        // ====================================================================
        // SINGLETON
        // ====================================================================
        public static RuntimeGameManager Instance { get; private set; }

        // ====================================================================
        // REFERENCIAS
        // ====================================================================
        private Canvas _canvas;
        private Text _healthText;
        private Text _scoreText;
        private Text _enemyCountText;
        private Image[] _hearts;
        private GameObject _gameOverPanel;
        private Text _gameOverText;
        private GameObject _victoryPanel;
        private Text _victoryText;

        private AudioSource _musicSource;
        private AudioSource _sfxSource;

        // ====================================================================
        // ESTADO DEL JUEGO
        // ====================================================================
        private int _currentHealth = 100;
        private int _maxHealth = 100;
        private int _score = 0;
        private bool _isGameOver = false;
        private bool _isVictory = false;
        private int _enemiesKilled = 0;
        private int _totalEnemies = 0;

        // Wave display
        private Text _waveMessageText;
        private GameObject _waveMessageGO;
        private Text _waveNumText;
        private Coroutine _waveMessageCoroutine;

        // Audio clips cargados
        private AudioClip _backgroundMusic;
        private AudioClip _hitSound;
        private AudioClip _coinSound;
        private AudioClip _healSound;
        private AudioClip _attackSound;
        private AudioClip _enemyDeathSound;

        // ====================================================================
        // CONFIGURACION
        // ====================================================================
        [Header("UI Settings")]
        public int maxHearts = 5;
        public Color heartFullColor = Color.red;
        public Color heartEmptyColor = new Color(0.3f, 0.1f, 0.1f);

        // ====================================================================
        // INICIALIZACION
        // ====================================================================

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        void Start()
        {
            CreateUI();
            SetupAudio();
            CountEnemies();
            UpdateUI();
            StartCoroutine(SubscribeToWaveManager());

            Debug.Log("[RuntimeGameManager] Sistema inicializado");
        }

        void OnDestroy()
        {
            if (WaveManager.Instance != null)
            {
                WaveManager.Instance.OnWaveStarted      -= HandleWaveStarted;
                WaveManager.Instance.OnWaveCleared      -= HandleWaveCleared;
                WaveManager.Instance.OnEnemyCountChanged -= HandleEnemyCountChanged;
            }
        }

        IEnumerator SubscribeToWaveManager()
        {
            yield return null; // Wait for all Start() to run
            if (WaveManager.Instance != null)
            {
                WaveManager.Instance.OnWaveStarted      += HandleWaveStarted;
                WaveManager.Instance.OnWaveCleared      += HandleWaveCleared;
                WaveManager.Instance.OnEnemyCountChanged += HandleEnemyCountChanged;
                // Sync current state
                if (_waveNumText != null)
                    _waveNumText.text = $"Ronda {WaveManager.Instance.CurrentWave}";
                if (_enemyCountText != null)
                    _enemyCountText.text = $"Enemigos: {WaveManager.Instance.AliveEnemyCount}";
            }
        }

        void HandleWaveStarted(int wave)
        {
            if (_waveNumText != null)
                _waveNumText.text = $"Ronda {wave}";
            ShowWaveMessage($"RONDA {wave}", Color.yellow);
        }

        void HandleWaveCleared(int wave)
        {
            ShowWaveMessage($"¡RONDA {wave} SUPERADA!", Color.green);
        }

        void HandleEnemyCountChanged(int count)
        {
            if (_enemyCountText != null)
                _enemyCountText.text = $"Enemigos: {count}";
        }

        void ShowWaveMessage(string msg, Color color)
        {
            if (_waveMessageGO == null) return;
            _waveMessageText.text = msg;
            _waveMessageText.color = color;
            _waveMessageGO.SetActive(true);
            if (_waveMessageCoroutine != null) StopCoroutine(_waveMessageCoroutine);
            _waveMessageCoroutine = StartCoroutine(HideWaveMessageDelay());
        }

        IEnumerator HideWaveMessageDelay()
        {
            yield return new WaitForSeconds(2.5f);
            if (_waveMessageGO != null)
                _waveMessageGO.SetActive(false);
        }

        void CountEnemies()
        {
            // Contar enemigos en la escena
            GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
            _totalEnemies = enemies.Length;
            _enemiesKilled = 0;
            Debug.Log($"[RuntimeGameManager] Enemigos en escena: {_totalEnemies}");
        }

        // ====================================================================
        // CREAR UI EN RUNTIME
        // ====================================================================

        void CreateUI()
        {
            // Crear Canvas
            GameObject canvasGO = new GameObject("GameCanvas");
            _canvas = canvasGO.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 100;

            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();

            // Panel superior para stats
            GameObject topPanel = CreatePanel("TopPanel", _canvas.transform);
            RectTransform topRect = topPanel.GetComponent<RectTransform>();
            topRect.anchorMin = new Vector2(0, 1);
            topRect.anchorMax = new Vector2(1, 1);
            topRect.pivot = new Vector2(0.5f, 1);
            topRect.anchoredPosition = Vector2.zero;
            topRect.sizeDelta = new Vector2(0, 80);

            // Corazones (vida)
            CreateHearts(topPanel.transform);

            // Texto de puntuacion
            _scoreText = CreateText("ScoreText", topPanel.transform, "Score: 0");
            RectTransform scoreRect = _scoreText.GetComponent<RectTransform>();
            scoreRect.anchorMin = new Vector2(1, 0.5f);
            scoreRect.anchorMax = new Vector2(1, 0.5f);
            scoreRect.pivot = new Vector2(1, 0.5f);
            scoreRect.anchoredPosition = new Vector2(-20, 0);
            scoreRect.sizeDelta = new Vector2(200, 40);
            _scoreText.alignment = TextAnchor.MiddleRight;
            _scoreText.fontSize = 28;

            // Texto de vida (numerico)
            _healthText = CreateText("HealthText", topPanel.transform, "100/100");
            RectTransform healthRect = _healthText.GetComponent<RectTransform>();
            healthRect.anchorMin = new Vector2(0, 0.5f);
            healthRect.anchorMax = new Vector2(0, 0.5f);
            healthRect.pivot = new Vector2(0, 0.5f);
            healthRect.anchoredPosition = new Vector2(20 + (maxHearts * 45), 0);
            healthRect.sizeDelta = new Vector2(120, 30);
            _healthText.fontSize = 20;

            // Contador de enemigos
            _enemyCountText = CreateText("EnemyCount", topPanel.transform, "Enemies: 0/0");
            RectTransform enemyRect = _enemyCountText.GetComponent<RectTransform>();
            enemyRect.anchorMin = new Vector2(0.5f, 0.5f);
            enemyRect.anchorMax = new Vector2(0.5f, 0.5f);
            enemyRect.pivot = new Vector2(0.5f, 0.5f);
            enemyRect.anchoredPosition = new Vector2(0, 0);
            enemyRect.sizeDelta = new Vector2(200, 40);
            _enemyCountText.alignment = TextAnchor.MiddleCenter;
            _enemyCountText.fontSize = 22;

            // Panel de Game Over (oculto inicialmente)
            CreateGameOverPanel();

            // Panel de Victoria (oculto inicialmente)
            CreateVictoryPanel();

            // Texto de ronda (esquina superior izquierda, debajo de corazones)
            _waveNumText = CreateText("WaveNumText", topPanel.transform, "Ronda 1");
            RectTransform waveNumRect = _waveNumText.GetComponent<RectTransform>();
            waveNumRect.anchorMin = new Vector2(0, 0.5f);
            waveNumRect.anchorMax = new Vector2(0, 0.5f);
            waveNumRect.pivot = new Vector2(0, 0.5f);
            waveNumRect.anchoredPosition = new Vector2(20 + (maxHearts * 45) + 130, 0);
            waveNumRect.sizeDelta = new Vector2(160, 30);
            _waveNumText.fontSize = 20;
            _waveNumText.color = new Color(0.9f, 0.9f, 0.4f);

            // Mensaje de oleada (centro de pantalla)
            _waveMessageGO = new GameObject("WaveMessage");
            _waveMessageGO.transform.SetParent(_canvas.transform, false);
            _waveMessageText = _waveMessageGO.AddComponent<Text>();
            _waveMessageText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _waveMessageText.fontSize = 52;
            _waveMessageText.color = Color.yellow;
            _waveMessageText.alignment = TextAnchor.MiddleCenter;
            _waveMessageText.fontStyle = FontStyle.Bold;
            Outline waveOutline = _waveMessageGO.AddComponent<Outline>();
            waveOutline.effectColor = Color.black;
            waveOutline.effectDistance = new Vector2(3, -3);
            RectTransform waveMsgRT = _waveMessageGO.GetComponent<RectTransform>();
            waveMsgRT.anchorMin = new Vector2(0f, 0.5f);
            waveMsgRT.anchorMax = new Vector2(1f, 0.5f);
            waveMsgRT.pivot = new Vector2(0.5f, 0.5f);
            waveMsgRT.anchoredPosition = new Vector2(0, 80);
            waveMsgRT.sizeDelta = new Vector2(0, 80);
            _waveMessageGO.SetActive(false);

            // Indicador del ninja elegido (debajo del top panel, izquierda)
            var csm = CharacterSelectManager.Instance;
            if (csm?.SelectedCharacter != null)
            {
                var cd = csm.SelectedCharacter;
                var ninjaGO = new GameObject("NinjaIndicator");
                ninjaGO.transform.SetParent(_canvas.transform, false);
                var ninjaText = ninjaGO.AddComponent<Text>();
                ninjaText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                ninjaText.text = $"[ {cd.characterName} ]";
                ninjaText.fontSize = 16;
                ninjaText.fontStyle = FontStyle.Bold;
                ninjaText.color = cd.spriteColor;
                ninjaText.alignment = TextAnchor.UpperLeft;
                var ninjaRT = ninjaGO.GetComponent<RectTransform>();
                ninjaRT.anchorMin = new Vector2(0f, 1f);
                ninjaRT.anchorMax = new Vector2(0f, 1f);
                ninjaRT.pivot = new Vector2(0f, 1f);
                ninjaRT.anchoredPosition = new Vector2(12f, -82f);
                ninjaRT.sizeDelta = new Vector2(200f, 22f);
            }

            // Panel de controles (esquina inferior izquierda)
            var ctrlGO = new GameObject("ControlsHint");
            ctrlGO.transform.SetParent(_canvas.transform, false);
            var ctrlText = ctrlGO.AddComponent<Text>();
            ctrlText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            ctrlText.fontSize = 14;
            ctrlText.color = new Color(0.85f, 0.85f, 0.85f, 0.75f);
            ctrlText.alignment = TextAnchor.LowerLeft;
            ctrlText.text = "WASD  Mover\nClick/Esp  Atacar\nShift/RMB  Dash (x2 dmg)\nE  Interactuar\nEsc  Pausa";
            var ctrlRT = ctrlGO.GetComponent<RectTransform>();
            ctrlRT.anchorMin = new Vector2(0f, 0f);
            ctrlRT.anchorMax = new Vector2(0f, 0f);
            ctrlRT.pivot = new Vector2(0f, 0f);
            ctrlRT.anchoredPosition = new Vector2(10f, 10f);
            ctrlRT.sizeDelta = new Vector2(200f, 90f);

            Debug.Log("[RuntimeGameManager] UI creada");
        }

        void CreateVictoryPanel()
        {
            _victoryPanel = CreatePanel("VictoryPanel", _canvas.transform);
            Image bgImg = _victoryPanel.GetComponent<Image>();
            bgImg.color = new Color(0, 0.2f, 0, 0.85f);

            RectTransform vRect = _victoryPanel.GetComponent<RectTransform>();
            vRect.anchorMin = Vector2.zero;
            vRect.anchorMax = Vector2.one;
            vRect.offsetMin = Vector2.zero;
            vRect.offsetMax = Vector2.zero;

            _victoryText = CreateText("VictoryText", _victoryPanel.transform, "VICTORY!\n\nScore: 0\n\nPress R to Play Again");
            _victoryText.fontSize = 48;
            _victoryText.alignment = TextAnchor.MiddleCenter;
            _victoryText.color = new Color(1f, 1f, 0.5f);

            RectTransform textRect = _victoryText.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            _victoryPanel.SetActive(false);
        }

        void CreateHearts(Transform parent)
        {
            _hearts = new Image[maxHearts];

            GameObject heartsContainer = new GameObject("HeartsContainer");
            heartsContainer.transform.SetParent(parent, false);
            RectTransform containerRect = heartsContainer.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0, 0.5f);
            containerRect.anchorMax = new Vector2(0, 0.5f);
            containerRect.pivot = new Vector2(0, 0.5f);
            containerRect.anchoredPosition = new Vector2(15, 0);
            containerRect.sizeDelta = new Vector2(maxHearts * 45, 40);

            HorizontalLayoutGroup layout = heartsContainer.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 5;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = false;

            for (int i = 0; i < maxHearts; i++)
            {
                GameObject heartGO = new GameObject($"Heart_{i}");
                heartGO.transform.SetParent(heartsContainer.transform, false);

                Image heartImg = heartGO.AddComponent<Image>();
                heartImg.color = heartFullColor;

                // Crear sprite de corazon simple
                heartImg.sprite = CreateHeartSprite();

                RectTransform heartRect = heartGO.GetComponent<RectTransform>();
                heartRect.sizeDelta = new Vector2(35, 35);

                _hearts[i] = heartImg;
            }
        }

        Sprite CreateHeartSprite()
        {
            // Crear un corazon pixelado simple
            int size = 16;
            Texture2D tex = new Texture2D(size, size);
            tex.filterMode = FilterMode.Point;

            // Patron de corazon simple
            int[,] pattern = {
                {0,0,1,1,0,0,1,1,0,0,0,0,1,1,0,0},
                {0,1,1,1,1,1,1,1,1,0,0,1,1,1,1,0},
                {1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1},
                {1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1},
                {1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1},
                {1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1},
                {0,1,1,1,1,1,1,1,1,1,1,1,1,1,1,0},
                {0,1,1,1,1,1,1,1,1,1,1,1,1,1,1,0},
                {0,0,1,1,1,1,1,1,1,1,1,1,1,1,0,0},
                {0,0,1,1,1,1,1,1,1,1,1,1,1,1,0,0},
                {0,0,0,1,1,1,1,1,1,1,1,1,1,0,0,0},
                {0,0,0,0,1,1,1,1,1,1,1,1,0,0,0,0},
                {0,0,0,0,0,1,1,1,1,1,1,0,0,0,0,0},
                {0,0,0,0,0,0,1,1,1,1,0,0,0,0,0,0},
                {0,0,0,0,0,0,0,1,1,0,0,0,0,0,0,0},
                {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0}
            };

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int py = size - 1 - y;
                    Color c = pattern[py, x] == 1 ? Color.white : Color.clear;
                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();

            return Sprite.Create(tex, new Rect(0, 0, size, size), Vector2.one * 0.5f, size);
        }

        void CreateGameOverPanel()
        {
            _gameOverPanel = CreatePanel("GameOverPanel", _canvas.transform);
            Image bgImg = _gameOverPanel.GetComponent<Image>();
            bgImg.color = new Color(0, 0, 0, 0.8f);

            RectTransform goRect = _gameOverPanel.GetComponent<RectTransform>();
            goRect.anchorMin = Vector2.zero;
            goRect.anchorMax = Vector2.one;
            goRect.offsetMin = Vector2.zero;
            goRect.offsetMax = Vector2.zero;

            // Texto Game Over
            _gameOverText = CreateText("GameOverText", _gameOverPanel.transform, "GAME OVER\n\nScore: 0\n\nPress R to Restart");
            _gameOverText.fontSize = 48;
            _gameOverText.alignment = TextAnchor.MiddleCenter;
            _gameOverText.color = Color.white;

            RectTransform textRect = _gameOverText.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            _gameOverPanel.SetActive(false);
        }

        GameObject CreatePanel(string name, Transform parent)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent, false);

            Image img = panel.AddComponent<Image>();
            img.color = new Color(0, 0, 0, 0.5f);

            return panel;
        }

        Text CreateText(string name, Transform parent, string content)
        {
            GameObject textGO = new GameObject(name);
            textGO.transform.SetParent(parent, false);

            Text text = textGO.AddComponent<Text>();
            text.text = content;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 24;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleLeft;

            // Outline para mejor legibilidad
            Outline outline = textGO.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(2, -2);

            RectTransform rect = textGO.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(200, 40);

            return text;
        }

        // ====================================================================
        // SETUP AUDIO
        // ====================================================================

        void SetupAudio()
        {
            // Crear AudioSources
            _musicSource = gameObject.AddComponent<AudioSource>();
            _musicSource.loop = true;
            _musicSource.volume = 0.4f;
            _musicSource.playOnAwake = false;

            _sfxSource = gameObject.AddComponent<AudioSource>();
            _sfxSource.playOnAwake = false;
            _sfxSource.volume = 0.7f;

            // Cargar audio del pack Ninja Adventure
            StartCoroutine(LoadAudioClips());
        }

        IEnumerator LoadAudioClips()
        {
            yield return null;

#if UNITY_EDITOR
            const string BASE = "Assets/_Project/Sprites/Ninja Adventure/Audio/";
            string[] musicCandidates = {
                "Musics/17 - Fight.ogg",
                "Musics/1 - Adventure Begin.ogg",
                "Musics/10 - Dark Castle.ogg"
            };
            foreach (var m in musicCandidates)
            {
                _backgroundMusic = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(BASE + m);
                if (_backgroundMusic != null) break;
            }
            _hitSound        = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(BASE + "Sounds/Hit & Impact/Hit1.wav");
            _coinSound       = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(BASE + "Sounds/Bonus/Coin.wav");
            _healSound       = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(BASE + "Sounds/Magic & Skill/Heal.wav");
            _attackSound     = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(BASE + "Sounds/Whoosh & Slash/Slash.wav");
            _enemyDeathSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(BASE + "Sounds/Hit & Impact/Hit2.wav");
#else
            // Para builds: copia los audios a Assets/Resources/Ninja Adventure/Audio/
            string mPath = "Ninja Adventure/Audio/Musics/";
            foreach (var f in new[] { "17 - Fight", "1 - Adventure Begin", "10 - Dark Castle" })
            {
                _backgroundMusic = Resources.Load<AudioClip>(mPath + f);
                if (_backgroundMusic != null) break;
            }
            string sPath = "Ninja Adventure/Audio/Sounds/";
            _hitSound        = Resources.Load<AudioClip>(sPath + "Hit & Impact/Hit1");
            _coinSound       = Resources.Load<AudioClip>(sPath + "Bonus/Coin");
            _healSound       = Resources.Load<AudioClip>(sPath + "Magic & Skill/Heal");
            _attackSound     = Resources.Load<AudioClip>(sPath + "Whoosh & Slash/Slash");
            _enemyDeathSound = Resources.Load<AudioClip>(sPath + "Hit & Impact/Hit2");
#endif

            if (_backgroundMusic != null)
            {
                _musicSource.clip = _backgroundMusic;
                _musicSource.Play();
                Debug.Log("[RuntimeGameManager] Musica cargada y reproduciendose");
            }
            else
            {
                Debug.LogWarning("[RuntimeGameManager] No se encontro musica. En builds, coloca audios en Assets/Resources/Ninja Adventure/Audio/");
            }

            int sfxCount = (_hitSound != null ? 1 : 0) + (_coinSound != null ? 1 : 0) +
                           (_healSound != null ? 1 : 0) + (_attackSound != null ? 1 : 0) +
                           (_enemyDeathSound != null ? 1 : 0);
            Debug.Log($"[RuntimeGameManager] {sfxCount}/5 SFX cargados");
        }

        // ====================================================================
        // ACTUALIZAR UI
        // ====================================================================

        void UpdateUI()
        {
            if (_hearts == null) return;
            // Actualizar corazones
            float healthPerHeart = (float)_maxHealth / maxHearts;
            for (int i = 0; i < _hearts.Length; i++)
            {
                float heartThreshold = (i + 1) * healthPerHeart;
                if (_currentHealth >= heartThreshold)
                {
                    _hearts[i].color = heartFullColor;
                }
                else if (_currentHealth > i * healthPerHeart)
                {
                    // Corazon parcial - mezclamos colores
                    float fillAmount = (_currentHealth - i * healthPerHeart) / healthPerHeart;
                    _hearts[i].color = Color.Lerp(heartEmptyColor, heartFullColor, fillAmount);
                }
                else
                {
                    _hearts[i].color = heartEmptyColor;
                }
            }

            // Actualizar textos
            if (_healthText != null)
                _healthText.text = $"{_currentHealth}/{_maxHealth}";

            if (_scoreText != null)
                _scoreText.text = $"Score: {_score}";

            if (_enemyCountText != null)
            {
                if (WaveManager.Instance != null)
                    _enemyCountText.text = $"Enemigos: {WaveManager.Instance.AliveEnemyCount}";
                else
                    _enemyCountText.text = $"Enemies: {_enemiesKilled}/{_totalEnemies}";
            }
        }

        // ====================================================================
        // METODOS PUBLICOS - Llamados desde otros scripts
        // ====================================================================

        public void SetHealth(int current, int max)
        {
            _currentHealth = current;
            _maxHealth = max;
            UpdateUI();

            if (_currentHealth <= 0 && !_isGameOver)
            {
                GameOver();
            }
        }

        public void TakeDamage(int damage)
        {
            _currentHealth -= damage;
            if (_currentHealth < 0) _currentHealth = 0;
            UpdateUI();
            PlaySFX(_hitSound);
            StartCoroutine(DamageFlash());

            if (_currentHealth <= 0 && !_isGameOver)
            {
                GameOver();
            }
        }

        public void Heal(int amount)
        {
            _currentHealth += amount;
            if (_currentHealth > _maxHealth) _currentHealth = _maxHealth;
            UpdateUI();
            PlaySFX(_healSound);
        }

        public void AddScore(int points)
        {
            _score += points;
            UpdateUI();
            PlaySFX(_coinSound);
            StartCoroutine(ScorePop());
        }

        public void PlayAttackSound()
        {
            PlaySFX(_attackSound);
        }

        public void PlayEnemyDeathSound()
        {
            PlaySFX(_enemyDeathSound);
        }

        /// <summary>
        /// Registra un nuevo enemigo (llamado desde Start de cada enemigo)
        /// </summary>
        public void RegisterEnemy()
        {
            _totalEnemies++;
            UpdateUI();
        }

        public void OnEnemyKilled()
        {
            _enemiesKilled++;
            UpdateUI();

            // Si WaveManager está activo, él gestiona las rondas — no mostrar Victoria aquí
            if (WaveManager.Instance != null) return;

            // Sin WaveManager: victoria cuando mueren todos los enemigos iniciales
            if (_enemiesKilled >= _totalEnemies && !_isVictory && !_isGameOver)
            {
                Victory();
            }
        }

        // ====================================================================
        // EFECTOS
        // ====================================================================

        IEnumerator DamageFlash()
        {
            var flashGO = new GameObject("DamageFlash");
            flashGO.transform.SetParent(_canvas.transform, false);
            var flash = flashGO.AddComponent<Image>();
            var rect = flashGO.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;

            float duration = 0.40f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                flash.color = new Color(1f, 0f, 0f, Mathf.Lerp(0.45f, 0f, elapsed / duration));
                elapsed += Time.deltaTime;
                yield return null;
            }
            Destroy(flashGO);
        }

        IEnumerator ScorePop()
        {
            if (_scoreText == null) yield break;

            Vector3 originalScale = _scoreText.transform.localScale;
            _scoreText.transform.localScale = originalScale * 1.3f;
            yield return new WaitForSeconds(0.1f);
            _scoreText.transform.localScale = originalScale;
        }

        // ====================================================================
        // GAME OVER
        // ====================================================================

        void GameOver()
        {
            _isGameOver = true;
            _gameOverPanel.SetActive(true);
            _gameOverText.text = $"GAME OVER\n\nScore: {_score}\n\nVolviendo al menú...";

            SaveScoreToRanking();

            if (_musicSource != null)
                _musicSource.Pause();

            Debug.Log("[RuntimeGameManager] Game Over!");
            StartCoroutine(ReturnToMenuAfterDelay(3f));
        }

        System.Collections.IEnumerator ReturnToMenuAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }

        void Victory()
        {
            _isVictory = true;
            _victoryPanel.SetActive(true);
            _victoryText.text = $"VICTORY!\n\nAll enemies defeated!\n\nScore: {_score}\n\nPress R to Play Again";

            // Guardar puntuacion en el ranking
            SaveScoreToRanking();

            Debug.Log("[RuntimeGameManager] Victory!");
        }

        void SaveScoreToRanking()
        {
            if (SaveSystem.Instance == null) return;

            string playerName = GameManager.Instance != null
                ? GameManager.Instance.CurrentPlayerName
                : (SaveSystem.Instance.GetLastPlayerName() is string n && n.Length > 0 ? n : "Jugador");

            SaveSystem.Instance.AddRankingEntry(playerName, _score);
            Debug.Log($"[RuntimeGameManager] Puntuacion guardada: {playerName} - {_score}");
        }

        void Update()
        {
            if ((_isGameOver || _isVictory) && UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.rKey.wasPressedThisFrame)
            {
                RestartGame();
            }
        }

        void RestartGame()
        {
            _isGameOver = false;
            _isVictory = false;
            _currentHealth = _maxHealth;
            _score = 0;
            _enemiesKilled = 0;

            if (_gameOverPanel != null) _gameOverPanel.SetActive(false);
            if (_victoryPanel != null) _victoryPanel.SetActive(false);

            UpdateUI();

            if (_musicSource != null)
                _musicSource.UnPause();

            // Recargar escena
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
            );
        }

        // ====================================================================
        // AUDIO
        // ====================================================================

        void PlaySFX(AudioClip clip)
        {
            if (clip != null && _sfxSource != null)
            {
                _sfxSource.pitch = Random.Range(0.9f, 1.1f);
                _sfxSource.PlayOneShot(clip);
            }
        }

        public void PlaySFXClip(AudioClip clip)
        {
            PlaySFX(clip);
        }

        // ====================================================================
        // BIG MESSAGE (nivel, boss, upgrade)
        // ====================================================================

        private Text _bigMessageText;
        private GameObject _bigMessageGO;
        private Coroutine _bigMessageCoroutine;

        // Lazy-create the big message element and show it
        public void ShowBigMessage(string msg, Color color)
        {
            EnsureBigMessage();
            if (_bigMessageGO == null) return;

            _bigMessageText.text = msg;
            _bigMessageText.color = color;
            _bigMessageGO.SetActive(true);

            if (_bigMessageCoroutine != null) StopCoroutine(_bigMessageCoroutine);
            _bigMessageCoroutine = StartCoroutine(HideBigMessageDelay());
        }

        void EnsureBigMessage()
        {
            if (_bigMessageGO != null) return;
            if (_canvas == null) return;

            _bigMessageGO = new GameObject("BigMessage");
            _bigMessageGO.transform.SetParent(_canvas.transform, false);

            _bigMessageText = _bigMessageGO.AddComponent<Text>();
            _bigMessageText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _bigMessageText.fontSize = 58;
            _bigMessageText.fontStyle = FontStyle.Bold;
            _bigMessageText.alignment = TextAnchor.MiddleCenter;
            _bigMessageText.color = Color.white;

            Outline outline = _bigMessageGO.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(4, -4);

            Shadow shadow = _bigMessageGO.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.6f);
            shadow.effectDistance = new Vector2(5, -5);

            RectTransform rt = _bigMessageGO.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0.35f);
            rt.anchorMax = new Vector2(1f, 0.65f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            _bigMessageGO.SetActive(false);
        }

        IEnumerator HideBigMessageDelay()
        {
            yield return new WaitForSeconds(2.8f);
            if (_bigMessageGO != null) _bigMessageGO.SetActive(false);
        }

        // ====================================================================
        // GETTERS
        // ====================================================================

        public int CurrentHealth => _currentHealth;
        public int MaxHealth => _maxHealth;
        public int Score => _score;
        public bool IsGameOver => _isGameOver;
    }
}
