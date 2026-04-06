using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BIT.Core;
using System.Collections.Generic;

// ============================================================================
// RANKINGUI.CS - Interfaz del ranking de jugadores (Requisito 3.2)
// ============================================================================
// Este script gestiona la visualización del ranking de puntuaciones.
// Muestra el nombre del jugador y su puntuación de forma ordenada.
//
// Requisito 3.2: "Ranking de jugadores visible con nombre + puntuación"
// ============================================================================

namespace BIT.UI
{
    /// <summary>
    /// Gestiona la interfaz del ranking de puntuaciones.
    /// </summary>
    public class RankingUI : MonoBehaviour
    {
        // ====================================================================
        // CONFIGURACIÓN
        // ====================================================================

        [Header("=== ENTRADA DE NOMBRE ===")]
        [Tooltip("Campo de texto para introducir el nombre")]
        [SerializeField] private TMP_InputField _nameInputField;

        [Tooltip("Botón para confirmar el nombre")]
        [SerializeField] private Button _confirmNameButton;

        [Tooltip("Panel que contiene la entrada de nombre")]
        [SerializeField] private GameObject _nameInputPanel;

        [Header("=== LISTA DEL RANKING ===")]
        [Tooltip("Contenedor donde se instancian las entradas del ranking")]
        [SerializeField] private Transform _rankingListContainer;

        [Tooltip("Prefab de cada entrada del ranking")]
        [SerializeField] private GameObject _rankingEntryPrefab;

        [Tooltip("Panel que contiene el ranking")]
        [SerializeField] private GameObject _rankingPanel;

        [Header("=== TEXTOS ===")]
        [Tooltip("Título del ranking")]
        [SerializeField] private TextMeshProUGUI _rankingTitleText;

        [Tooltip("Mensaje cuando no hay entradas")]
        [SerializeField] private TextMeshProUGUI _noEntriesText;

        // ====================================================================
        // INICIALIZACIÓN
        // ====================================================================

        private void Start()
        {
            // Configuramos el botón
            if (_confirmNameButton != null)
            {
                _confirmNameButton.onClick.AddListener(OnConfirmName);
            }

            // Cargamos el último nombre usado
            LoadLastPlayerName();

            // Mostramos el ranking
            RefreshRankingList();
        }

        private void OnDestroy()
        {
            if (_confirmNameButton != null)
            {
                _confirmNameButton.onClick.RemoveListener(OnConfirmName);
            }
        }

        // ====================================================================
        // ENTRADA DE NOMBRE
        // ====================================================================

        /// <summary>
        /// Carga el último nombre del jugador usado.
        /// </summary>
        private void LoadLastPlayerName()
        {
            if (_nameInputField == null) return;

            string lastName = SaveSystem.Instance?.GetLastPlayerName();

            if (!string.IsNullOrEmpty(lastName))
            {
                _nameInputField.text = lastName;
            }
        }

        /// <summary>
        /// Se llama cuando el jugador confirma su nombre.
        /// </summary>
        private void OnConfirmName()
        {
            if (_nameInputField == null) return;

            string playerName = _nameInputField.text.Trim();

            // Validamos el nombre
            if (string.IsNullOrEmpty(playerName))
            {
                playerName = "Jugador";
            }

            // Lo guardamos en el GameManager
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetPlayerName(playerName);
            }

            // Ocultamos el panel de entrada
            if (_nameInputPanel != null)
            {
                _nameInputPanel.SetActive(false);
            }

            Debug.Log($"[RankingUI] Nombre confirmado: {playerName}");
        }

        /// <summary>
        /// Muestra el panel de entrada de nombre.
        /// </summary>
        public void ShowNameInput()
        {
            if (_nameInputPanel != null)
            {
                _nameInputPanel.SetActive(true);
            }

            // Enfocamos el campo de texto
            if (_nameInputField != null)
            {
                _nameInputField.Select();
                _nameInputField.ActivateInputField();
            }
        }

        // ====================================================================
        // VISUALIZACIÓN DEL RANKING
        // ====================================================================

        /// <summary>
        /// Actualiza la lista del ranking.
        /// </summary>
        public void RefreshRankingList()
        {
            if (_rankingListContainer == null) return;

            // Limpiamos las entradas existentes
            ClearRankingList();

            // Obtenemos el ranking
            List<RankingEntry> ranking = SaveSystem.Instance?.GetRanking();

            if (ranking == null || ranking.Count == 0)
            {
                // Mostramos mensaje de "sin entradas"
                if (_noEntriesText != null)
                {
                    _noEntriesText.gameObject.SetActive(true);
                }
                return;
            }

            // Ocultamos el mensaje de "sin entradas"
            if (_noEntriesText != null)
            {
                _noEntriesText.gameObject.SetActive(false);
            }

            // Creamos una entrada por cada registro
            for (int i = 0; i < ranking.Count; i++)
            {
                CreateRankingEntry(i + 1, ranking[i]);
            }
        }

        /// <summary>
        /// Limpia todas las entradas del ranking.
        /// </summary>
        private void ClearRankingList()
        {
            if (_rankingListContainer == null) return;

            foreach (Transform child in _rankingListContainer)
            {
                Destroy(child.gameObject);
            }
        }

        /// <summary>
        /// Crea una entrada visual en el ranking.
        /// </summary>
        private void CreateRankingEntry(int position, RankingEntry entry)
        {
            if (_rankingEntryPrefab == null || _rankingListContainer == null) return;

            // Instanciamos el prefab
            GameObject entryObj = Instantiate(_rankingEntryPrefab, _rankingListContainer);

            // Configuramos los textos (asumimos que el prefab tiene estos componentes)
            TextMeshProUGUI[] texts = entryObj.GetComponentsInChildren<TextMeshProUGUI>();

            if (texts.Length >= 3)
            {
                // Posición
                texts[0].text = $"#{position}";

                // Nombre
                texts[1].text = entry.playerName;

                // Puntuación
                texts[2].text = entry.score.ToString("N0");
            }
            else
            {
                // Si solo hay un texto, mostramos todo junto
                TextMeshProUGUI text = entryObj.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null)
                {
                    text.text = $"#{position} - {entry.playerName}: {entry.score}";
                }
            }

            // Color especial para el top 3
            Image bg = entryObj.GetComponent<Image>();
            if (bg != null)
            {
                switch (position)
                {
                    case 1:
                        bg.color = new Color(1f, 0.84f, 0f, 0.3f); // Oro
                        break;
                    case 2:
                        bg.color = new Color(0.75f, 0.75f, 0.75f, 0.3f); // Plata
                        break;
                    case 3:
                        bg.color = new Color(0.8f, 0.5f, 0.2f, 0.3f); // Bronce
                        break;
                    default:
                        bg.color = new Color(1f, 1f, 1f, 0.1f); // Normal
                        break;
                }
            }
        }

        // ====================================================================
        // CONTROL DE VISIBILIDAD
        // ====================================================================

        /// <summary>
        /// Muestra el panel del ranking.
        /// </summary>
        public void ShowRanking()
        {
            if (_rankingPanel != null)
            {
                _rankingPanel.SetActive(true);
            }

            RefreshRankingList();
        }

        /// <summary>
        /// Oculta el panel del ranking.
        /// </summary>
        public void HideRanking()
        {
            if (_rankingPanel != null)
            {
                _rankingPanel.SetActive(false);
            }
        }

        /// <summary>
        /// Alterna la visibilidad del ranking.
        /// </summary>
        public void ToggleRanking()
        {
            if (_rankingPanel != null)
            {
                bool isActive = !_rankingPanel.activeSelf;
                _rankingPanel.SetActive(isActive);

                if (isActive)
                {
                    RefreshRankingList();
                }
            }
        }

        // ====================================================================
        // UTILIDADES
        // ====================================================================

        /// <summary>
        /// Limpia todo el ranking (para testing).
        /// </summary>
        public void ClearAllRankings()
        {
            SaveSystem.Instance?.ClearRanking();
            RefreshRankingList();
        }
    }
}
