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
        private Text _gameOverSubtitleText;
        private Text _gameOverScoreText;
        private Text _gameOverReturnText;
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
        private int _coins = 0;
        private bool _isGameOver = false;
        private bool _isVictory = false;
        private int _enemiesKilled = 0;
        private int _totalEnemies = 0;

        // Wave display
        private Text _waveMessageText;
        private GameObject _waveMessageGO;
        private Text _waveNumText;
        private Text _coinText;
        private Coroutine _waveMessageCoroutine;

        private Vector3 _scoreTextOriginalScale = Vector3.one;

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

            // Awake se ejecuta antes que cualquier física/trigger.
            // Limpiamos pickups Y enemigos pre-colocados en la escena por el setup script.
            // Solo el WaveManager debe crear enemigos; los pickups solo existen como drops.
            CleanPreplacedObjects();
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

        static void CleanPreplacedObjects()
        {
            int pickups = 0, enemies = 0;

            // Por componente (tiene PickupBase)
            foreach (var p in FindObjectsByType<BIT.Interactables.PickupBase>(FindObjectsSortMode.None))
            { DestroyImmediate(p.gameObject); pickups++; }

            // Por tag (GameObjects con tag Coin/Health pero sin PickupBase —
            // objetos sueltos que quedan de runs anteriores del setup)
            foreach (var go in GameObject.FindGameObjectsWithTag("Coin"))
            { DestroyImmediate(go); pickups++; }
            foreach (var go in GameObject.FindGameObjectsWithTag("Health"))
            { DestroyImmediate(go); pickups++; }
            foreach (var go in GameObject.FindGameObjectsWithTag("Pickup"))
            { DestroyImmediate(go); pickups++; }

            // Enemigos pre-colocados
            foreach (var go in GameObject.FindGameObjectsWithTag("Enemy"))
            { DestroyImmediate(go); enemies++; }

            if (pickups > 0 || enemies > 0)
                Debug.Log($"[RuntimeGameManager] Limpieza: {pickups} pickups y {enemies} enemigos pre-colocados eliminados.");
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
            topRect.sizeDelta = new Vector2(0, 96);

            // Corazones (vida)
            CreateHearts(topPanel.transform);

            // Puntuacion alineada con el combo, justo encima del multiplicador.
            _scoreText = CreateText("ScoreText", _canvas.transform, "SCORE: 0");
            RectTransform scoreRect = _scoreText.GetComponent<RectTransform>();
            scoreRect.anchorMin = new Vector2(1f, 0f);
            scoreRect.anchorMax = new Vector2(1f, 0f);
            scoreRect.pivot     = new Vector2(1f, 0f);
            scoreRect.anchoredPosition = new Vector2(-20f, 94f);
            scoreRect.sizeDelta = new Vector2(320f, 50f);
            _scoreText.alignment = TextAnchor.MiddleCenter;
            _scoreText.fontSize  = 38;
            _scoreText.fontStyle = FontStyle.BoldAndItalic;
            _scoreText.color     = new Color(0.03f, 0.02f, 0.01f);

            Outline scoreOutline = _scoreText.GetComponent<Outline>();
            if (scoreOutline != null)
            {
                scoreOutline.effectColor = new Color(1f, 0.78f, 0.05f);
                scoreOutline.effectDistance = new Vector2(3f, -3f);
            }

            Shadow scoreShadow = _scoreText.gameObject.AddComponent<Shadow>();
            scoreShadow.effectColor = new Color(1f, 1f, 0.75f, 0.75f);
            scoreShadow.effectDistance = new Vector2(-2f, 2f);

            _scoreTextOriginalScale = _scoreText.transform.localScale;

            // Texto de vida (numerico)
            _healthText = CreateText("HealthText", topPanel.transform, "100/100");
            RectTransform healthRect = _healthText.GetComponent<RectTransform>();
            healthRect.anchorMin = new Vector2(0, 0.5f);
            healthRect.anchorMax = new Vector2(0, 0.5f);
            healthRect.pivot = new Vector2(0, 0.5f);
            healthRect.anchoredPosition = new Vector2(20 + (maxHearts * 45), 0);
            healthRect.sizeDelta = new Vector2(120, 34);
            _healthText.fontSize = 24;

            // Contador de enemigos
            _enemyCountText = CreateText("EnemyCount", topPanel.transform, "Enemies: 0/0");
            RectTransform enemyRect = _enemyCountText.GetComponent<RectTransform>();
            enemyRect.anchorMin = new Vector2(0.5f, 0.5f);
            enemyRect.anchorMax = new Vector2(0.5f, 0.5f);
            enemyRect.pivot = new Vector2(0.5f, 0.5f);
            enemyRect.anchoredPosition = new Vector2(0, 0);
            enemyRect.sizeDelta = new Vector2(220, 44);
            _enemyCountText.alignment = TextAnchor.MiddleCenter;
            _enemyCountText.fontSize = 26;

            // Panel de Game Over (oculto inicialmente)
            CreateGameOverPanel();

            // Panel de Victoria (oculto inicialmente)
            CreateVictoryPanel();

            // Texto de ronda — centro inferior de la pantalla
            _waveNumText = CreateText("WaveNumText", _canvas.transform, "Ronda 1");
            RectTransform waveNumRect = _waveNumText.GetComponent<RectTransform>();
            waveNumRect.anchorMin = new Vector2(0.5f, 0f);
            waveNumRect.anchorMax = new Vector2(0.5f, 0f);
            waveNumRect.pivot     = new Vector2(0.5f, 0f);
            waveNumRect.anchoredPosition = new Vector2(0f, 14f);
            waveNumRect.sizeDelta = new Vector2(260f, 36f);
            _waveNumText.alignment = TextAnchor.MiddleCenter;
            _waveNumText.fontSize  = 26;
            _waveNumText.color     = new Color(0.9f, 0.9f, 0.4f);

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

            // Monedas — arriba a la derecha del todo
            _coinText = CreateText("CoinText", _canvas.transform, "$ 0");
            RectTransform coinRect = _coinText.GetComponent<RectTransform>();
            coinRect.anchorMin = new Vector2(1f, 1f);
            coinRect.anchorMax = new Vector2(1f, 1f);
            coinRect.pivot     = new Vector2(1f, 1f);
            coinRect.anchoredPosition = new Vector2(-10f, -8f);
            coinRect.sizeDelta = new Vector2(130f, 36f);
            _coinText.alignment = TextAnchor.UpperRight;
            _coinText.fontSize  = 28;
            _coinText.color     = new Color(1f, 0.85f, 0.1f);

            // Indicador del ninja elegido — dentro del top panel, izquierda tras los corazones
            var csm = CharacterSelectManager.Instance;
            if (csm?.SelectedCharacter != null)
            {
                var cd = csm.SelectedCharacter;
                var ninjaGO = new GameObject("NinjaIndicator");
                ninjaGO.transform.SetParent(topPanel.transform, false);
                var ninjaText = ninjaGO.AddComponent<Text>();
                ninjaText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                ninjaText.text = $"[ {cd.characterName} ]";
                ninjaText.fontSize = 20;
                ninjaText.fontStyle = FontStyle.Bold;
                ninjaText.color = cd.spriteColor;
                ninjaText.alignment = TextAnchor.MiddleLeft;
                var ninjaOutline = ninjaGO.AddComponent<Outline>();
                ninjaOutline.effectColor = Color.black;
                ninjaOutline.effectDistance = new Vector2(2, -2);
                var ninjaRT = ninjaGO.GetComponent<RectTransform>();
                ninjaRT.anchorMin = new Vector2(0f, 0f);
                ninjaRT.anchorMax = new Vector2(0f, 0f);
                ninjaRT.pivot     = new Vector2(0f, 0f);
                // Pegado debajo de los corazones, parte inferior del HUD
                ninjaRT.anchoredPosition = new Vector2(15f, 4f);
                ninjaRT.sizeDelta = new Vector2(maxHearts * 45f + 20f, 22f);
            }

            // Panel de controles — esquina inferior izquierda con fondo oscuro para legibilidad
            var ctrlBgGO = new GameObject("ControlsBg");
            ctrlBgGO.transform.SetParent(_canvas.transform, false);
            var ctrlBg = ctrlBgGO.AddComponent<Image>();
            ctrlBg.color = new Color(0f, 0f, 0f, 0.60f);
            var ctrlBgRT = ctrlBgGO.GetComponent<RectTransform>();
            ctrlBgRT.anchorMin = new Vector2(0f, 0f);
            ctrlBgRT.anchorMax = new Vector2(0f, 0f);
            ctrlBgRT.pivot = new Vector2(0f, 0f);
            ctrlBgRT.anchoredPosition = new Vector2(8f, 8f);
            ctrlBgRT.sizeDelta = new Vector2(192f, 108f);

            var ctrlGO = new GameObject("ControlsHint");
            ctrlGO.transform.SetParent(_canvas.transform, false);
            var ctrlText = ctrlGO.AddComponent<Text>();
            ctrlText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            ctrlText.fontSize = 16;
            ctrlText.color = Color.white;
            ctrlText.alignment = TextAnchor.LowerLeft;
            ctrlText.text = "WASD  Mover\nLMB  Melee\nRMB  Shuriken\nShift  Dash\nE  Interactuar";
            var ctrlRT = ctrlGO.GetComponent<RectTransform>();
            ctrlRT.anchorMin = new Vector2(0f, 0f);
            ctrlRT.anchorMax = new Vector2(0f, 0f);
            ctrlRT.pivot = new Vector2(0f, 0f);
            ctrlRT.anchoredPosition = new Vector2(16f, 14f);
            ctrlRT.sizeDelta = new Vector2(176f, 100f);

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
            bgImg.color = new Color(0.025f, 0f, 0.005f, 0.90f);

            RectTransform goRect = _gameOverPanel.GetComponent<RectTransform>();
            goRect.anchorMin = Vector2.zero;
            goRect.anchorMax = Vector2.one;
            goRect.offsetMin = Vector2.zero;
            goRect.offsetMax = Vector2.zero;

            GameObject content = CreatePanel("GameOverContent", _gameOverPanel.transform);
            Image contentImg = content.GetComponent<Image>();
            contentImg.color = new Color(0.08f, 0.01f, 0.018f, 0.92f);
            Outline contentOutline = content.AddComponent<Outline>();
            contentOutline.effectColor = new Color(0.70f, 0.02f, 0.02f, 0.80f);
            contentOutline.effectDistance = new Vector2(4f, -4f);

            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.5f, 0.5f);
            contentRect.anchorMax = new Vector2(0.5f, 0.5f);
            contentRect.pivot = new Vector2(0.5f, 0.5f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(660f, 380f);

            GameObject accent = new GameObject("GameOverAccent");
            accent.transform.SetParent(content.transform, false);
            Image accentImg = accent.AddComponent<Image>();
            accentImg.color = new Color(0.95f, 0.08f, 0.05f, 1f);
            RectTransform accentRect = accent.GetComponent<RectTransform>();
            accentRect.anchorMin = new Vector2(0.5f, 0.5f);
            accentRect.anchorMax = new Vector2(0.5f, 0.5f);
            accentRect.pivot = new Vector2(0.5f, 0.5f);
            accentRect.anchoredPosition = new Vector2(0f, 68f);
            accentRect.sizeDelta = new Vector2(460f, 4f);

            _gameOverText = CreateText("GameOverTitle", content.transform, "HAS MUERTO");
            _gameOverText.fontSize = 68;
            _gameOverText.fontStyle = FontStyle.Bold;
            _gameOverText.alignment = TextAnchor.MiddleCenter;
            _gameOverText.color = new Color(1f, 0.12f, 0.08f);

            Outline titleOutline = _gameOverText.GetComponent<Outline>();
            if (titleOutline != null)
            {
                titleOutline.effectColor = new Color(0f, 0f, 0f, 0.95f);
                titleOutline.effectDistance = new Vector2(4f, -4f);
            }

            Shadow titleGlow = _gameOverText.gameObject.AddComponent<Shadow>();
            titleGlow.effectColor = new Color(1f, 0.45f, 0.18f, 0.70f);
            titleGlow.effectDistance = new Vector2(-3f, 3f);

            RectTransform titleRect = _gameOverText.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 0.5f);
            titleRect.anchorMax = new Vector2(0.5f, 0.5f);
            titleRect.pivot = new Vector2(0.5f, 0.5f);
            titleRect.anchoredPosition = new Vector2(0f, 122f);
            titleRect.sizeDelta = new Vector2(620f, 82f);

            _gameOverSubtitleText = CreateText("GameOverSubtitle", content.transform, "Tu aventura termina aquí.");
            _gameOverSubtitleText.fontSize = 28;
            _gameOverSubtitleText.fontStyle = FontStyle.Italic;
            _gameOverSubtitleText.alignment = TextAnchor.MiddleCenter;
            _gameOverSubtitleText.color = new Color(0.96f, 0.84f, 0.75f);

            RectTransform subtitleRect = _gameOverSubtitleText.GetComponent<RectTransform>();
            subtitleRect.anchorMin = new Vector2(0.5f, 0.5f);
            subtitleRect.anchorMax = new Vector2(0.5f, 0.5f);
            subtitleRect.pivot = new Vector2(0.5f, 0.5f);
            subtitleRect.anchoredPosition = new Vector2(0f, 28f);
            subtitleRect.sizeDelta = new Vector2(560f, 40f);

            _gameOverScoreText = CreateText("GameOverScore", content.transform, "Puntuación final: 0");
            _gameOverScoreText.fontSize = 36;
            _gameOverScoreText.fontStyle = FontStyle.Bold;
            _gameOverScoreText.alignment = TextAnchor.MiddleCenter;
            _gameOverScoreText.color = new Color(1f, 0.82f, 0.22f);

            RectTransform scoreRect = _gameOverScoreText.GetComponent<RectTransform>();
            scoreRect.anchorMin = new Vector2(0.5f, 0.5f);
            scoreRect.anchorMax = new Vector2(0.5f, 0.5f);
            scoreRect.pivot = new Vector2(0.5f, 0.5f);
            scoreRect.anchoredPosition = new Vector2(0f, -48f);
            scoreRect.sizeDelta = new Vector2(560f, 50f);

            _gameOverReturnText = CreateText("GameOverReturn", content.transform, "Volviendo al menú principal...");
            _gameOverReturnText.fontSize = 26;
            _gameOverReturnText.alignment = TextAnchor.MiddleCenter;
            _gameOverReturnText.color = new Color(0.86f, 0.86f, 0.86f);

            RectTransform returnRect = _gameOverReturnText.GetComponent<RectTransform>();
            returnRect.anchorMin = new Vector2(0.5f, 0.5f);
            returnRect.anchorMax = new Vector2(0.5f, 0.5f);
            returnRect.pivot = new Vector2(0.5f, 0.5f);
            returnRect.anchoredPosition = new Vector2(0f, -126f);
            returnRect.sizeDelta = new Vector2(560f, 42f);

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

            const string BASE = "Assets/_Project/Sprites/Ninja Adventure/Audio/";
            string[] musicCandidates = {
                "Musics/17 - Fight.ogg",
                "Musics/1 - Adventure Begin.ogg",
                "Musics/10 - Dark Castle.ogg"
            };
            foreach (var m in musicCandidates)
            {
                _backgroundMusic = RuntimeAssetLoader.LoadAsset<AudioClip>(BASE + m);
                if (_backgroundMusic != null) break;
            }
            _hitSound        = RuntimeAssetLoader.LoadAsset<AudioClip>(BASE + "Sounds/Hit & Impact/Hit1.wav");
            _coinSound       = RuntimeAssetLoader.LoadAsset<AudioClip>(BASE + "Sounds/Bonus/Coin.wav");
            _healSound       = RuntimeAssetLoader.LoadAsset<AudioClip>(BASE + "Sounds/Magic & Skill/Heal.wav");
            _attackSound     = RuntimeAssetLoader.LoadAsset<AudioClip>(BASE + "Sounds/Whoosh & Slash/Slash.wav");
            _enemyDeathSound = RuntimeAssetLoader.LoadAsset<AudioClip>(BASE + "Sounds/Hit & Impact/Hit2.wav");

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
                _scoreText.text = $"SCORE: {_score:N0}";

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

        public void AddCoins(int amount)
        {
            _coins += amount;
            if (_coinText != null) _coinText.text = $"$ {_coins}";
        }

        public bool SpendCoins(int amount)
        {
            if (_coins < amount) return false;
            _coins -= amount;
            if (_coinText != null) _coinText.text = $"$ {_coins}";
            return true;
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

            _scoreText.transform.localScale = _scoreTextOriginalScale * 1.2f;
            yield return new WaitForSeconds(0.1f);
            _scoreText.transform.localScale = _scoreTextOriginalScale;
        }

        // ====================================================================
        // GAME OVER
        // ====================================================================

        void GameOver()
        {
            _isGameOver = true;
            _gameOverPanel.SetActive(true);
            _gameOverText.text = "HAS MUERTO";
            if (_gameOverSubtitleText != null)
                _gameOverSubtitleText.text = "Tu aventura termina aquí.";
            if (_gameOverScoreText != null)
                _gameOverScoreText.text = $"Puntuación final: {_score}";
            if (_gameOverReturnText != null)
                _gameOverReturnText.text = "Volviendo al menú principal...";

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
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
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
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene(_isGameOver ? "MainMenu" : "CharacterSelect");
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
        public int Coins => _coins;
        public bool IsGameOver => _isGameOver;
    }
}
