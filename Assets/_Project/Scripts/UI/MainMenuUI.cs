using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BIT.Core;
using BIT.Audio;

// ============================================================================
// MAINMENUUI.CS - Menú principal del juego
// ============================================================================
// Este script controla la interfaz del menú principal, incluyendo:
// - Botón de Jugar
// - Entrada del nombre del jugador
// - Ver ranking
// - Opciones de volumen
// - Salir del juego
//
// Es importante para la primera impresión del juego y para el requisito
// bonus 3.2 donde se pide el nombre del jugador al inicio.
// ============================================================================

namespace BIT.UI
{
    /// <summary>
    /// Controla la interfaz del menú principal.
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        // ====================================================================
        // REFERENCIAS DE UI
        // ====================================================================

        [Header("=== BOTONES PRINCIPALES ===")]
        [Tooltip("Botón para iniciar el juego")]
        [SerializeField] private Button _playButton;

        [Tooltip("Botón para ver el ranking")]
        [SerializeField] private Button _rankingButton;

        [Tooltip("Botón para abrir opciones")]
        [SerializeField] private Button _optionsButton;

        [Tooltip("Botón para salir del juego")]
        [SerializeField] private Button _quitButton;

        [Header("=== ENTRADA DE NOMBRE ===")]
        [Tooltip("Campo para introducir el nombre")]
        [SerializeField] private TMP_InputField _playerNameInput;

        [Tooltip("Nombre por defecto si no se introduce ninguno")]
        [SerializeField] private string _defaultPlayerName = "Jugador";

        [Header("=== PANELES ===")]
        [Tooltip("Panel principal del menú")]
        [SerializeField] private GameObject _mainPanel;

        [Tooltip("Panel de opciones")]
        [SerializeField] private GameObject _optionsPanel;

        [Tooltip("Panel del ranking")]
        [SerializeField] private GameObject _rankingPanel;

        [Header("=== OPCIONES DE AUDIO ===")]
        [Tooltip("Slider de volumen de música")]
        [SerializeField] private Slider _musicVolumeSlider;

        [Tooltip("Slider de volumen de efectos")]
        [SerializeField] private Slider _sfxVolumeSlider;

        [Header("=== TEXTOS INFORMATIVOS ===")]
        [Tooltip("Texto del high score")]
        [SerializeField] private TextMeshProUGUI _highScoreText;

        [Tooltip("Texto de versión del juego")]
        [SerializeField] private TextMeshProUGUI _versionText;

        // ====================================================================
        // INICIALIZACIÓN
        // ====================================================================

        private void Start()
        {
            // Configuramos los botones
            SetupButtons();

            // Configuramos los sliders
            SetupSliders();

            // Cargamos datos guardados
            LoadSavedData();

            // Mostramos el panel principal
            ShowMainPanel();

            // Mostramos la versión
            if (_versionText != null)
            {
                _versionText.text = $"v{Application.version}";
            }
        }

        private void OnDestroy()
        {
            // Removemos los listeners
            RemoveButtonListeners();
        }

        // ====================================================================
        // CONFIGURACIÓN DE BOTONES
        // ====================================================================

        private void SetupButtons()
        {
            if (_playButton != null)
            {
                _playButton.onClick.AddListener(OnPlayClicked);
            }

            if (_rankingButton != null)
            {
                _rankingButton.onClick.AddListener(OnRankingClicked);
            }

            if (_optionsButton != null)
            {
                _optionsButton.onClick.AddListener(OnOptionsClicked);
            }

            if (_quitButton != null)
            {
                _quitButton.onClick.AddListener(OnQuitClicked);
            }
        }

        private void RemoveButtonListeners()
        {
            _playButton?.onClick.RemoveAllListeners();
            _rankingButton?.onClick.RemoveAllListeners();
            _optionsButton?.onClick.RemoveAllListeners();
            _quitButton?.onClick.RemoveAllListeners();
        }

        // ====================================================================
        // CONFIGURACIÓN DE SLIDERS
        // ====================================================================

        private void SetupSliders()
        {
            if (_musicVolumeSlider != null)
            {
                _musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            }

            if (_sfxVolumeSlider != null)
            {
                _sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
            }
        }

        // ====================================================================
        // CARGAR DATOS GUARDADOS
        // ====================================================================

        private void LoadSavedData()
        {
            // Cargamos el último nombre usado
            if (_playerNameInput != null && SaveSystem.Instance != null)
            {
                string lastName = SaveSystem.Instance.GetLastPlayerName();
                if (!string.IsNullOrEmpty(lastName))
                {
                    _playerNameInput.text = lastName;
                }
            }

            // Mostramos el high score
            if (_highScoreText != null && SaveSystem.Instance != null)
            {
                int highScore = SaveSystem.Instance.GetHighScore();
                _highScoreText.text = $"High Score: {highScore:N0}";
            }
        }

        // ====================================================================
        // CALLBACKS DE BOTONES
        // ====================================================================

        /// <summary>
        /// Se llama cuando el jugador pulsa "Jugar".
        /// </summary>
        private void OnPlayClicked()
        {
            // Reproducimos sonido de click
            AudioManager.Instance?.PlayButtonClick();

            // Obtenemos el nombre del jugador
            string playerName = _playerNameInput != null ? _playerNameInput.text.Trim() : "";

            if (string.IsNullOrEmpty(playerName))
            {
                playerName = _defaultPlayerName;
            }

            // Lo guardamos en el GameManager
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetPlayerName(playerName);
                GameManager.Instance.LoadGameScene();
            }
            else
            {
                // Si no hay GameManager, cargamos la escena directamente
                UnityEngine.SceneManagement.SceneManager.LoadScene("gamesetupscene");
            }

            Debug.Log($"[MainMenuUI] Iniciando juego con nombre: {playerName}");
        }

        /// <summary>
        /// Muestra el panel de ranking.
        /// </summary>
        private void OnRankingClicked()
        {
            AudioManager.Instance?.PlayButtonClick();
            ShowRankingPanel();
        }

        /// <summary>
        /// Muestra el panel de opciones.
        /// </summary>
        private void OnOptionsClicked()
        {
            AudioManager.Instance?.PlayButtonClick();
            ShowOptionsPanel();
        }

        /// <summary>
        /// Sale del juego.
        /// </summary>
        private void OnQuitClicked()
        {
            AudioManager.Instance?.PlayButtonClick();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.QuitGame();
            }
            else
            {
                #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
                #else
                Application.Quit();
                #endif
            }
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

            // Reproducimos un sonido de prueba
            AudioManager.Instance?.PlayButtonClick();
        }

        // ====================================================================
        // GESTIÓN DE PANELES
        // ====================================================================

        private void ShowMainPanel()
        {
            SetPanelActive(_mainPanel, true);
            SetPanelActive(_optionsPanel, false);
            SetPanelActive(_rankingPanel, false);
        }

        private void ShowOptionsPanel()
        {
            SetPanelActive(_mainPanel, false);
            SetPanelActive(_optionsPanel, true);
            SetPanelActive(_rankingPanel, false);
        }

        private void ShowRankingPanel()
        {
            SetPanelActive(_mainPanel, false);
            SetPanelActive(_optionsPanel, false);
            SetPanelActive(_rankingPanel, true);
        }

        private void SetPanelActive(GameObject panel, bool active)
        {
            if (panel != null)
            {
                panel.SetActive(active);
            }
        }

        /// <summary>
        /// Vuelve al panel principal (para botones "Atrás").
        /// </summary>
        public void BackToMainPanel()
        {
            AudioManager.Instance?.PlayButtonClick();
            ShowMainPanel();
        }
    }
}
