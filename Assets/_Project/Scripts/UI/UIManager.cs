using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BIT.Data;

// ============================================================================
// UIMANAGER.CS - Gestor de la interfaz de usuario (Requisito 2.6)
// ============================================================================
// Este script maneja toda la UI del juego: barras de vida, puntuación,
// mensajes en pantalla, etc.
//
// CONCEPTO CLAVE PARA DEFENSA ORAL:
// El UIManager se SUSCRIBE a los eventos del PlayerStatsSO.
// Cuando el jugador recibe daño, el PlayerStats dispara OnHealthChanged,
// y el UIManager lo escucha y actualiza la barra de vida automáticamente.
//
// Esto es el PATRÓN OBSERVER en acción:
// - PlayerStats NO conoce la existencia del UIManager
// - UIManager simplemente "escucha" los cambios
// - Si mañana añadimos otro sistema (logros), también puede escuchar
//
// Requisito 2.6: "Sistema de puntuación o estado del jugador mostrado en UI"
// ============================================================================

namespace BIT.UI
{
    /// <summary>
    /// Gestiona la interfaz de usuario del juego.
    /// Patrón Singleton para acceso global.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        // ====================================================================
        // SECCIÓN 1: SINGLETON
        // ====================================================================

        /// <summary>
        /// Instancia única del UIManager (Patrón Singleton)
        /// </summary>
        public static UIManager Instance { get; private set; }

        // ====================================================================
        // SECCIÓN 2: REFERENCIAS A UI
        // ====================================================================

        [Header("=== REFERENCIAS DE DATOS ===")]
        [Tooltip("ScriptableObject con las estadísticas del jugador")]
        [SerializeField] private PlayerStatsSO _playerStats;

        [Header("=== BARRA DE VIDA ===")]
        [Tooltip("Slider o Image que muestra la vida")]
        [SerializeField] private Slider _healthSlider;

        [Tooltip("Imagen de relleno de la barra de vida (para cambiar color)")]
        [SerializeField] private Image _healthFillImage;

        [Tooltip("Texto que muestra la vida numéricamente (ej: 80/100)")]
        [SerializeField] private TextMeshProUGUI _healthText;

        [Header("=== PUNTUACIÓN ===")]
        [Tooltip("Texto que muestra la puntuación")]
        [SerializeField] private TextMeshProUGUI _scoreText;

        [Tooltip("Prefijo antes del número (ej: 'Score: ')")]
        [SerializeField] private string _scorePrefix = "Score: ";

        [Header("=== EFECTOS VISUALES ===")]
        [Tooltip("Color de la barra cuando la vida está alta")]
        [SerializeField] private Color _healthHighColor = Color.green;

        [Tooltip("Color de la barra cuando la vida está media")]
        [SerializeField] private Color _healthMediumColor = Color.yellow;

        [Tooltip("Color de la barra cuando la vida está baja")]
        [SerializeField] private Color _healthLowColor = Color.red;

        [Tooltip("Umbral de vida media (porcentaje)")]
        [SerializeField] private float _mediumHealthThreshold = 0.5f;

        [Tooltip("Umbral de vida baja (porcentaje)")]
        [SerializeField] private float _lowHealthThreshold = 0.25f;

        [Header("=== RONDAS ===")]
        [Tooltip("Texto que muestra la ronda actual (ej: 'Ronda 3')")]
        [SerializeField] private TextMeshProUGUI _waveText;

        [Tooltip("Texto que muestra cuántos enemigos quedan")]
        [SerializeField] private TextMeshProUGUI _enemyCountText;

        [Tooltip("Panel del mensaje de oleada ('¡RONDA 2 SUPERADA!')")]
        [SerializeField] private GameObject _waveMessagePanel;

        [Tooltip("Texto dentro del panel de mensaje de oleada")]
        [SerializeField] private TextMeshProUGUI _waveMessageText;

        [Tooltip("Segundos que se muestra el mensaje de oleada")]
        [SerializeField] private float _waveMessageDuration = 2.5f;

        [Header("=== PANTALLAS ===")]
        [Tooltip("Panel de Game Over")]
        [SerializeField] private GameObject _gameOverPanel;

        [Tooltip("Texto de puntuación final en Game Over")]
        [SerializeField] private TextMeshProUGUI _finalScoreText;

        // ====================================================================
        // SECCIÓN 3: INICIALIZACIÓN
        // ====================================================================

        private void Awake()
        {
            // Implementación del Singleton
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            // Opcional: mantener entre escenas
            // DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            // Ocultamos el panel de Game Over al inicio
            if (_gameOverPanel != null)
                _gameOverPanel.SetActive(false);

            if (_waveMessagePanel != null)
                _waveMessagePanel.SetActive(false);

            // Suscribimos a los eventos de PlayerStats
            SubscribeToEvents();

            // Actualizamos la UI con los valores iniciales
            if (_playerStats != null)
            {
                UpdateHealthUI(_playerStats.CurrentHealth, _playerStats.MaxHealth);
                UpdateScoreUI(_playerStats.CurrentScore);
            }

            // Suscribimos a WaveManager si existe
            if (BIT.Core.WaveManager.Instance != null)
            {
                BIT.Core.WaveManager.Instance.OnWaveStarted   += UpdateWaveUI;
                BIT.Core.WaveManager.Instance.OnEnemyCountChanged += UpdateEnemyCountUI;
            }
        }

        private void OnDestroy()
        {
            // MUY IMPORTANTE: Desuscribirse para evitar memory leaks
            UnsubscribeFromEvents();

            if (BIT.Core.WaveManager.Instance != null)
            {
                BIT.Core.WaveManager.Instance.OnWaveStarted      -= UpdateWaveUI;
                BIT.Core.WaveManager.Instance.OnEnemyCountChanged -= UpdateEnemyCountUI;
            }
        }

        // ====================================================================
        // SECCIÓN 4: SUSCRIPCIÓN A EVENTOS (Patrón Observer)
        // ====================================================================

        /// <summary>
        /// Suscribe el UIManager a los eventos del PlayerStats.
        /// Esto es el corazón del patrón Observer.
        /// </summary>
        private void SubscribeToEvents()
        {
            if (_playerStats == null)
            {
                Debug.LogError("[UIManager] No hay PlayerStatsSO asignado!");
                return;
            }

            // Nos suscribimos a cada evento
            // Cuando PlayerStats cambie, nuestros métodos serán llamados automáticamente
            _playerStats.OnHealthChanged += UpdateHealthUI;
            _playerStats.OnScoreChanged += UpdateScoreUI;
            _playerStats.OnPlayerDeath += ShowGameOver;

            Debug.Log("[UIManager] Suscrito a eventos de PlayerStats");
        }

        /// <summary>
        /// Elimina las suscripciones a los eventos.
        /// </summary>
        private void UnsubscribeFromEvents()
        {
            if (_playerStats == null) return;

            _playerStats.OnHealthChanged -= UpdateHealthUI;
            _playerStats.OnScoreChanged -= UpdateScoreUI;
            _playerStats.OnPlayerDeath -= ShowGameOver;
        }

        // ====================================================================
        // SECCIÓN 5: ACTUALIZACIÓN DE UI (Callbacks de eventos)
        // ====================================================================

        /// <summary>
        /// Actualiza la barra y texto de vida.
        /// Este método es llamado AUTOMÁTICAMENTE cuando la vida cambia.
        /// </summary>
        /// <param name="currentHealth">Vida actual</param>
        /// <param name="maxHealth">Vida máxima</param>
        private void UpdateHealthUI(int currentHealth, int maxHealth)
        {
            // Calculamos el porcentaje
            float healthPercent = (float)currentHealth / maxHealth;

            // Actualizamos el slider
            if (_healthSlider != null)
            {
                _healthSlider.value = healthPercent;
            }

            // Actualizamos el texto
            if (_healthText != null)
            {
                _healthText.text = $"{currentHealth}/{maxHealth}";
            }

            // Cambiamos el color según el nivel de vida
            UpdateHealthColor(healthPercent);

            // Efecto visual de sacudida cuando recibimos daño
            if (healthPercent < 1f)
            {
                StartCoroutine(ShakeHealthBar());
            }

            Debug.Log($"[UIManager] Vida actualizada: {currentHealth}/{maxHealth} ({healthPercent:P0})");
        }

        /// <summary>
        /// Cambia el color de la barra de vida según el porcentaje.
        /// </summary>
        private void UpdateHealthColor(float healthPercent)
        {
            if (_healthFillImage == null) return;

            if (healthPercent <= _lowHealthThreshold)
            {
                _healthFillImage.color = _healthLowColor;
            }
            else if (healthPercent <= _mediumHealthThreshold)
            {
                _healthFillImage.color = _healthMediumColor;
            }
            else
            {
                _healthFillImage.color = _healthHighColor;
            }
        }

        /// <summary>
        /// Actualiza el texto de puntuación.
        /// </summary>
        /// <param name="newScore">Nueva puntuación</param>
        private void UpdateScoreUI(int newScore)
        {
            if (_scoreText == null) return;

            // Animación de "pop" al ganar puntos
            StartCoroutine(ScorePopAnimation(newScore));
        }

        /// <summary>
        /// Animación de pop cuando cambia la puntuación.
        /// </summary>
        private System.Collections.IEnumerator ScorePopAnimation(int newScore)
        {
            if (_scoreText == null) yield break;

            // Guardamos la escala original
            Vector3 originalScale = _scoreText.transform.localScale;

            // Agrandamos un poco
            _scoreText.transform.localScale = originalScale * 1.2f;

            // Actualizamos el texto
            _scoreText.text = $"{_scorePrefix}{newScore}";

            // Esperamos un frame
            yield return new WaitForSeconds(0.1f);

            // Volvemos a la escala original
            _scoreText.transform.localScale = originalScale;
        }

        // ====================================================================
        // SECCIÓN 6: EFECTOS DE UI
        // ====================================================================

        /// <summary>
        /// Efecto de sacudida en la barra de vida al recibir daño.
        /// </summary>
        private System.Collections.IEnumerator ShakeHealthBar()
        {
            if (_healthSlider == null) yield break;

            RectTransform rt = _healthSlider.GetComponent<RectTransform>();
            if (rt == null) yield break;

            Vector3 originalPos = rt.localPosition;
            float shakeDuration = 0.2f;
            float shakeIntensity = 5f;
            float elapsed = 0f;

            while (elapsed < shakeDuration)
            {
                elapsed += Time.deltaTime;

                // Movimiento aleatorio
                float x = originalPos.x + Random.Range(-shakeIntensity, shakeIntensity);
                float y = originalPos.y + Random.Range(-shakeIntensity, shakeIntensity);

                rt.localPosition = new Vector3(x, y, originalPos.z);

                yield return null;
            }

            // Restauramos posición original
            rt.localPosition = originalPos;
        }

        // ====================================================================
        // SECCIÓN 7: PANTALLAS ESPECIALES
        // ====================================================================

        /// <summary>
        /// Muestra la pantalla de Game Over.
        /// </summary>
        private void ShowGameOver()
        {
            if (_gameOverPanel != null)
            {
                _gameOverPanel.SetActive(true);
            }

            if (_finalScoreText != null && _playerStats != null)
            {
                _finalScoreText.text = $"Puntuación Final: {_playerStats.CurrentScore}";
            }

            Debug.Log("[UIManager] Game Over mostrado");
        }

        /// <summary>
        /// Oculta la pantalla de Game Over (para reiniciar).
        /// </summary>
        public void HideGameOver()
        {
            if (_gameOverPanel != null)
            {
                _gameOverPanel.SetActive(false);
            }
        }

        // ====================================================================
        // SECCIÓN 8: MÉTODOS PÚBLICOS (Para otros sistemas)
        // ====================================================================

        /// <summary>
        /// Muestra un mensaje temporal en pantalla.
        /// </summary>
        public void ShowMessage(string message, float duration = 2f)
        {
            Debug.Log($"[UIManager] Mensaje: {message}");
        }

        /// <summary>
        /// Actualiza la velocidad mostrada (si existe UI para ello).
        /// </summary>
        public void UpdateSpeedUI(float currentSpeed)
        {
            // Implementar si se necesita mostrar velocidad
        }

        // ====================================================================
        // SECCIÓN 9: UI DE RONDAS
        // ====================================================================

        /// <summary>
        /// Actualiza el texto con el número de ronda.
        /// </summary>
        private void UpdateWaveUI(int wave)
        {
            if (_waveText != null)
                _waveText.text = $"Ronda {wave}";
        }

        /// <summary>
        /// Actualiza el texto con el número de enemigos vivos.
        /// </summary>
        private void UpdateEnemyCountUI(int count)
        {
            if (_enemyCountText != null)
                _enemyCountText.text = $"Enemigos: {count}";
        }

        /// <summary>
        /// Muestra el panel de mensaje de oleada (inicio o fin de ronda).
        /// Se oculta automáticamente después de <c>_waveMessageDuration</c> segundos.
        /// </summary>
        public void ShowWaveMessage(string message, bool isStart)
        {
            if (_waveMessagePanel == null || _waveMessageText == null) return;

            _waveMessageText.text = message;
            _waveMessageText.color = isStart ? Color.yellow : Color.green;
            _waveMessagePanel.SetActive(true);

            StopCoroutine(nameof(HideWaveMessageAfterDelay));
            StartCoroutine(HideWaveMessageAfterDelay());
        }

        private System.Collections.IEnumerator HideWaveMessageAfterDelay()
        {
            yield return new WaitForSeconds(_waveMessageDuration);
            if (_waveMessagePanel != null)
                _waveMessagePanel.SetActive(false);
        }
    }
}

