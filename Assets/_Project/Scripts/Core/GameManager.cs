using UnityEngine;
using UnityEngine.SceneManagement;
using BIT.Data;
using BIT.UI;
using BIT.Audio;

// ============================================================================
// GAMEMANAGER.CS - Gestor principal del juego
// ============================================================================
// Este script controla el flujo general del juego: inicio, pausa, game over,
// reinicio, etc. Es el "director" que coordina todos los demás sistemas.
//
// CONCEPTO CLAVE PARA DEFENSA ORAL:
// El GameManager usa el patrón SINGLETON para garantizar que solo existe
// una instancia. Todos los sistemas pueden acceder a él mediante
// GameManager.Instance para:
// - Pausar/reanudar el juego
// - Reiniciar la partida
// - Cambiar de escena
// - Acceder al estado global del juego
//
// DontDestroyOnLoad mantiene el GameManager entre cambios de escena.
// ============================================================================

namespace BIT.Core
{
    /// <summary>
    /// Estados posibles del juego.
    /// </summary>
    public enum GameState
    {
        MainMenu,       // En el menú principal
        Playing,        // Jugando activamente
        Paused,         // Juego pausado
        GameOver,       // Partida terminada (perdió)
        Victory         // Partida ganada
    }

    /// <summary>
    /// Gestor principal del juego.
    /// Controla el flujo y estado global.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        // ====================================================================
        // SECCIÓN 1: SINGLETON
        // ====================================================================

        public static GameManager Instance { get; private set; }

        // ====================================================================
        // SECCIÓN 2: CONFIGURACIÓN
        // ====================================================================

        [Header("=== REFERENCIAS ===")]
        [Tooltip("ScriptableObject con las estadísticas del jugador")]
        [SerializeField] private PlayerStatsSO _playerStats;

        [Header("=== ESCENAS ===")]
        [Tooltip("Nombre de la escena del menú principal")]
        [SerializeField] private string _mainMenuScene = "MainMenu";

        [Tooltip("Nombre de la escena del juego")]
        [SerializeField] private string _gameScene = "Game";

        [Header("=== CONFIGURACIÓN DE JUEGO ===")]
        [Tooltip("Tiempo de escala cuando está pausado (0 = congelado)")]
        [SerializeField] private float _pausedTimeScale = 0f;

        // ====================================================================
        // SECCIÓN 3: ESTADO DEL JUEGO
        // ====================================================================

        // Estado actual
        private GameState _currentState = GameState.MainMenu;

        // Evento para notificar cambios de estado
        public event System.Action<GameState> OnGameStateChanged;

        // Datos de la partida actual
        private string _currentPlayerName = "Jugador";
        private float _playTime = 0f;

        // ====================================================================
        // SECCIÓN 4: PROPIEDADES PÚBLICAS
        // ====================================================================

        /// <summary>
        /// Estado actual del juego (solo lectura).
        /// </summary>
        public GameState CurrentState => _currentState;

        /// <summary>
        /// Indica si el juego está en pausa.
        /// </summary>
        public bool IsPaused => _currentState == GameState.Paused;

        /// <summary>
        /// Indica si se está jugando activamente.
        /// </summary>
        public bool IsPlaying => _currentState == GameState.Playing;

        /// <summary>
        /// Nombre del jugador actual.
        /// </summary>
        public string CurrentPlayerName => _currentPlayerName;

        /// <summary>
        /// Tiempo de juego de la partida actual.
        /// </summary>
        public float PlayTime => _playTime;

        // ====================================================================
        // SECCIÓN 5: INICIALIZACIÓN
        // ====================================================================

        private void Awake()
        {
            // Singleton con persistencia entre escenas
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Nos suscribimos al evento de carga de escena
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void Start()
        {
            // Si tenemos PlayerStats, nos suscribimos al evento de muerte
            if (_playerStats != null)
            {
                _playerStats.OnPlayerDeath += HandlePlayerDeath;
            }
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;

            if (_playerStats != null)
            {
                _playerStats.OnPlayerDeath -= HandlePlayerDeath;
            }
        }

        /// <summary>
        /// Se llama cuando se carga una nueva escena.
        /// </summary>
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Debug.Log($"[GameManager] Escena cargada: {scene.name}");

            // Si es la escena de juego, iniciamos
            if (scene.name == _gameScene)
            {
                StartGame();
            }
            else if (scene.name == _mainMenuScene)
            {
                SetState(GameState.MainMenu);
            }
        }

        // ====================================================================
        // SECCIÓN 6: UPDATE
        // ====================================================================

        private void Update()
        {
            // Contamos el tiempo de juego solo mientras jugamos
            if (_currentState == GameState.Playing)
            {
                _playTime += Time.deltaTime;
            }

            // Tecla de pausa (Escape)
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (_currentState == GameState.Playing)
                {
                    PauseGame();
                }
                else if (_currentState == GameState.Paused)
                {
                    ResumeGame();
                }
            }
        }

        // ====================================================================
        // SECCIÓN 7: CONTROL DEL FLUJO DEL JUEGO
        // ====================================================================

        /// <summary>
        /// Inicia una nueva partida.
        /// </summary>
        public void StartGame()
        {
            Debug.Log("[GameManager] Iniciando partida");

            // Reseteamos el tiempo de juego
            _playTime = 0f;

            // Inicializamos las estadísticas del jugador
            if (_playerStats != null)
            {
                _playerStats.Initialize();
            }

            // Aseguramos que el tiempo corre normalmente
            Time.timeScale = 1f;

            // Cambiamos el estado
            SetState(GameState.Playing);
        }

        /// <summary>
        /// Pausa el juego.
        /// </summary>
        public void PauseGame()
        {
            if (_currentState != GameState.Playing) return;

            Debug.Log("[GameManager] Juego pausado");

            // Congelamos el tiempo
            Time.timeScale = _pausedTimeScale;

            SetState(GameState.Paused);
        }

        /// <summary>
        /// Reanuda el juego.
        /// </summary>
        public void ResumeGame()
        {
            if (_currentState != GameState.Paused) return;

            Debug.Log("[GameManager] Juego reanudado");

            // Restauramos el tiempo
            Time.timeScale = 1f;

            SetState(GameState.Playing);
        }

        /// <summary>
        /// Termina la partida (el jugador perdió).
        /// </summary>
        public void GameOver()
        {
            if (_currentState == GameState.GameOver) return;

            Debug.Log("[GameManager] Game Over");

            // Paramos el tiempo
            Time.timeScale = 0f;

            // Guardamos la puntuación en el ranking
            SaveScore();

            // Reproducimos música de game over
            AudioManager.Instance?.PlayGameOverMusic();

            SetState(GameState.GameOver);
        }

        /// <summary>
        /// El jugador ha ganado.
        /// </summary>
        public void Victory()
        {
            Debug.Log("[GameManager] Victoria!");

            // Guardamos la puntuación
            SaveScore();

            SetState(GameState.Victory);
        }

        /// <summary>
        /// Reinicia la partida actual.
        /// </summary>
        public void RestartGame()
        {
            Debug.Log("[GameManager] Reiniciando partida");

            // Restauramos el tiempo
            Time.timeScale = 1f;

            // Recargamos la escena actual
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        /// <summary>
        /// Vuelve al menú principal.
        /// </summary>
        public void GoToMainMenu()
        {
            Debug.Log("[GameManager] Volviendo al menú principal");

            Time.timeScale = 1f;
            SceneManager.LoadScene(_mainMenuScene);
        }

        /// <summary>
        /// Carga la escena de juego.
        /// </summary>
        public void LoadGameScene()
        {
            Debug.Log("[GameManager] Cargando escena de juego");

            SceneManager.LoadScene(_gameScene);
        }

        // ====================================================================
        // SECCIÓN 8: GESTIÓN DE ESTADO
        // ====================================================================

        /// <summary>
        /// Cambia el estado del juego y notifica a los listeners.
        /// </summary>
        private void SetState(GameState newState)
        {
            if (_currentState == newState) return;

            GameState previousState = _currentState;
            _currentState = newState;

            Debug.Log($"[GameManager] Estado: {previousState} -> {newState}");

            // Notificamos del cambio
            OnGameStateChanged?.Invoke(newState);
        }

        // ====================================================================
        // SECCIÓN 9: GUARDADO DE PUNTUACIÓN
        // ====================================================================

        /// <summary>
        /// Guarda la puntuación actual en el ranking.
        /// </summary>
        private void SaveScore()
        {
            if (_playerStats == null) return;

            int finalScore = _playerStats.CurrentScore;

            // Guardamos en el sistema de ranking
            if (SaveSystem.Instance != null)
            {
                int position = SaveSystem.Instance.AddRankingEntry(_currentPlayerName, finalScore);

                if (position > 0)
                {
                    Debug.Log($"[GameManager] Nueva entrada en ranking! Posición: {position}");
                }
            }
        }

        /// <summary>
        /// Establece el nombre del jugador (desde el menú).
        /// </summary>
        public void SetPlayerName(string name)
        {
            _currentPlayerName = string.IsNullOrEmpty(name) ? "Jugador" : name;

            // Guardamos como último jugador
            SaveSystem.Instance?.SetLastPlayerName(_currentPlayerName);

            Debug.Log($"[GameManager] Nombre de jugador: {_currentPlayerName}");
        }

        // ====================================================================
        // SECCIÓN 10: CALLBACKS DE EVENTOS
        // ====================================================================

        /// <summary>
        /// Se llama cuando el jugador muere.
        /// </summary>
        private void HandlePlayerDeath()
        {
            GameOver();
        }

        // ====================================================================
        // SECCIÓN 11: UTILIDADES
        // ====================================================================

        /// <summary>
        /// Sale del juego (cierra la aplicación).
        /// </summary>
        public void QuitGame()
        {
            Debug.Log("[GameManager] Saliendo del juego...");

            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }

        /// <summary>
        /// Obtiene las estadísticas del jugador.
        /// </summary>
        public PlayerStatsSO GetPlayerStats() => _playerStats;
    }
}
