using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BIT.Core;
using BIT.Audio;

// ============================================================================
// PAUSEMENUUI.CS - Menú de pausa del juego
// ============================================================================
// Este script controla el menú de pausa que aparece cuando el jugador
// pulsa Escape durante la partida.
//
// Incluye:
// - Reanudar partida
// - Reiniciar nivel
// - Volver al menú principal
// - Ajustes de volumen
// ============================================================================

namespace BIT.UI
{
    /// <summary>
    /// Controla la interfaz del menú de pausa.
    /// </summary>
    public class PauseMenuUI : MonoBehaviour
    {
        // ====================================================================
        // REFERENCIAS DE UI
        // ====================================================================

        [Header("=== PANEL DE PAUSA ===")]
        [Tooltip("Panel principal del menú de pausa")]
        [SerializeField] private GameObject _pausePanel;

        [Header("=== BOTONES ===")]
        [Tooltip("Botón para reanudar")]
        [SerializeField] private Button _resumeButton;

        [Tooltip("Botón para reiniciar")]
        [SerializeField] private Button _restartButton;

        [Tooltip("Botón para opciones")]
        [SerializeField] private Button _optionsButton;

        [Tooltip("Botón para volver al menú")]
        [SerializeField] private Button _mainMenuButton;

        [Header("=== PANEL DE OPCIONES ===")]
        [Tooltip("Panel de opciones dentro de la pausa")]
        [SerializeField] private GameObject _optionsPanel;

        [Tooltip("Slider de volumen de música")]
        [SerializeField] private Slider _musicVolumeSlider;

        [Tooltip("Slider de volumen de efectos")]
        [SerializeField] private Slider _sfxVolumeSlider;

        [Tooltip("Botón para volver de opciones")]
        [SerializeField] private Button _backFromOptionsButton;

        [Header("=== CONFIRMACIÓN ===")]
        [Tooltip("Panel de confirmación")]
        [SerializeField] private GameObject _confirmPanel;

        [Tooltip("Texto de confirmación")]
        [SerializeField] private TextMeshProUGUI _confirmText;

        [Tooltip("Botón de confirmar")]
        [SerializeField] private Button _confirmYesButton;

        [Tooltip("Botón de cancelar")]
        [SerializeField] private Button _confirmNoButton;

        // ====================================================================
        // VARIABLES PRIVADAS
        // ====================================================================

        private System.Action _pendingAction;
        private bool _isPaused = false;

        // ====================================================================
        // INICIALIZACIÓN
        // ====================================================================

        private void Awake()
        {
            // Ocultamos todos los paneles al inicio
            HideAllPanels();
        }

        private void Start()
        {
            // Configuramos botones
            SetupButtons();

            // Configuramos sliders
            SetupSliders();

            // Suscribimos a eventos del GameManager
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged += OnGameStateChanged;
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged -= OnGameStateChanged;
            }

            RemoveButtonListeners();
        }

        private void Update()
        {
            // Tecla de pausa (como backup si el GameManager no lo maneja)
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                TogglePause();
            }
        }

        // ====================================================================
        // CONFIGURACIÓN
        // ====================================================================

        private void SetupButtons()
        {
            _resumeButton?.onClick.AddListener(OnResumeClicked);
            _restartButton?.onClick.AddListener(OnRestartClicked);
            _optionsButton?.onClick.AddListener(OnOptionsClicked);
            _mainMenuButton?.onClick.AddListener(OnMainMenuClicked);
            _backFromOptionsButton?.onClick.AddListener(OnBackFromOptionsClicked);
            _confirmYesButton?.onClick.AddListener(OnConfirmYes);
            _confirmNoButton?.onClick.AddListener(OnConfirmNo);
        }

        private void RemoveButtonListeners()
        {
            _resumeButton?.onClick.RemoveAllListeners();
            _restartButton?.onClick.RemoveAllListeners();
            _optionsButton?.onClick.RemoveAllListeners();
            _mainMenuButton?.onClick.RemoveAllListeners();
            _backFromOptionsButton?.onClick.RemoveAllListeners();
            _confirmYesButton?.onClick.RemoveAllListeners();
            _confirmNoButton?.onClick.RemoveAllListeners();
        }

        private void SetupSliders()
        {
            if (_musicVolumeSlider != null)
            {
                _musicVolumeSlider.value = 0.7f; // Valor por defecto
                _musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            }

            if (_sfxVolumeSlider != null)
            {
                _sfxVolumeSlider.value = 1f;
                _sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
            }
        }

        // ====================================================================
        // CONTROL DE PAUSA
        // ====================================================================

        /// <summary>
        /// Alterna entre pausado y no pausado.
        /// </summary>
        public void TogglePause()
        {
            if (_isPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }

        /// <summary>
        /// Pausa el juego.
        /// </summary>
        public void Pause()
        {
            _isPaused = true;

            // Mostramos el menú de pausa
            ShowPausePanel();

            // Pausamos el tiempo (si el GameManager no lo hace)
            if (GameManager.Instance != null)
            {
                GameManager.Instance.PauseGame();
            }
            else
            {
                Time.timeScale = 0f;
            }

            Debug.Log("[PauseMenuUI] Juego pausado");
        }

        /// <summary>
        /// Reanuda el juego.
        /// </summary>
        public void Resume()
        {
            _isPaused = false;

            // Ocultamos todos los paneles
            HideAllPanels();

            // Reanudamos el tiempo
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ResumeGame();
            }
            else
            {
                Time.timeScale = 1f;
            }

            Debug.Log("[PauseMenuUI] Juego reanudado");
        }

        // ====================================================================
        // CALLBACKS DE BOTONES
        // ====================================================================

        private void OnResumeClicked()
        {
            AudioManager.Instance?.PlayButtonClick();
            Resume();
        }

        private void OnRestartClicked()
        {
            AudioManager.Instance?.PlayButtonClick();
            ShowConfirmation("¿Reiniciar la partida?", () =>
            {
                Time.timeScale = 1f;
                GameManager.Instance?.RestartGame();
            });
        }

        private void OnOptionsClicked()
        {
            AudioManager.Instance?.PlayButtonClick();
            ShowOptionsPanel();
        }

        private void OnMainMenuClicked()
        {
            AudioManager.Instance?.PlayButtonClick();
            ShowConfirmation("¿Volver al menú principal?\nPerderás el progreso no guardado.", () =>
            {
                Time.timeScale = 1f;
                GameManager.Instance?.GoToMainMenu();
            });
        }

        private void OnBackFromOptionsClicked()
        {
            AudioManager.Instance?.PlayButtonClick();
            ShowPausePanel();
        }

        // ====================================================================
        // CALLBACKS DE SLIDERS
        // ====================================================================

        private void OnMusicVolumeChanged(float value)
        {
            AudioManager.Instance?.SetMusicVolume(value);
        }

        private void OnSFXVolumeChanged(float value)
        {
            AudioManager.Instance?.SetSFXVolume(value);
        }

        // ====================================================================
        // CONFIRMACIÓN
        // ====================================================================

        private void ShowConfirmation(string message, System.Action onConfirm)
        {
            _pendingAction = onConfirm;

            if (_confirmText != null)
            {
                _confirmText.text = message;
            }

            SetPanelActive(_pausePanel, false);
            SetPanelActive(_optionsPanel, false);
            SetPanelActive(_confirmPanel, true);
        }

        private void OnConfirmYes()
        {
            AudioManager.Instance?.PlayButtonClick();
            HideAllPanels();
            _pendingAction?.Invoke();
            _pendingAction = null;
        }

        private void OnConfirmNo()
        {
            AudioManager.Instance?.PlayButtonClick();
            _pendingAction = null;
            ShowPausePanel();
        }

        // ====================================================================
        // GESTIÓN DE PANELES
        // ====================================================================

        private void HideAllPanels()
        {
            SetPanelActive(_pausePanel, false);
            SetPanelActive(_optionsPanel, false);
            SetPanelActive(_confirmPanel, false);
        }

        private void ShowPausePanel()
        {
            SetPanelActive(_pausePanel, true);
            SetPanelActive(_optionsPanel, false);
            SetPanelActive(_confirmPanel, false);
        }

        private void ShowOptionsPanel()
        {
            SetPanelActive(_pausePanel, false);
            SetPanelActive(_optionsPanel, true);
            SetPanelActive(_confirmPanel, false);
        }

        private void SetPanelActive(GameObject panel, bool active)
        {
            if (panel != null)
            {
                panel.SetActive(active);
            }
        }

        // ====================================================================
        // EVENTOS DEL GAMEMANAGER
        // ====================================================================

        private void OnGameStateChanged(GameState newState)
        {
            switch (newState)
            {
                case GameState.Paused:
                    _isPaused = true;
                    ShowPausePanel();
                    break;

                case GameState.Playing:
                    _isPaused = false;
                    HideAllPanels();
                    break;

                case GameState.GameOver:
                    _isPaused = false;
                    HideAllPanels();
                    break;
            }
        }
    }
}
