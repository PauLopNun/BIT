using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace BIT.Core
{
    // Tracks "levels": every N waves = level up.
    // Heals the player and shows a big level-up message.
    // Shows the current level in the bottom-left HUD.
    public class LevelProgressionManager : MonoBehaviour
    {
        public static LevelProgressionManager Instance { get; private set; }

        [Header("=== CONFIG ===")]
        [Tooltip("Cada cuántas oleadas sube de nivel")]
        [SerializeField] private int _wavesPerLevel = 5;
        [Tooltip("Curación que recibe el jugador al subir de nivel")]
        [SerializeField] private int _healPerLevel = 25;

        private int _currentLevel = 1;
        public int CurrentLevel => _currentLevel;

        private Text _levelText;
        private GameObject _levelGO;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        void Start() => StartCoroutine(Init());

        IEnumerator Init()
        {
            yield return null;
            if (WaveManager.Instance != null)
                WaveManager.Instance.OnWaveCleared += OnWaveCleared;

            yield return new WaitForSeconds(0.3f);
            CreateLevelUI();
        }

        void OnDestroy()
        {
            if (WaveManager.Instance != null)
                WaveManager.Instance.OnWaveCleared -= OnWaveCleared;
        }

        void CreateLevelUI()
        {
            // Texto de nivel eliminado del HUD — la curación sigue funcionando internamente.
        }

        void OnWaveCleared(int wave)
        {
            if (wave % _wavesPerLevel == 0)
                StartCoroutine(LevelUpRoutine());
        }

        IEnumerator LevelUpRoutine()
        {
            // Small delay so wave-cleared message shows first
            yield return new WaitForSeconds(1f);
            _currentLevel++;

            if (_levelText != null)
                _levelText.text = $"Nivel {_currentLevel}";

            var player = FindFirstObjectByType<BIT.Player.PlayerController>();
            player?.Heal(_healPerLevel);

            RuntimeGameManager.Instance?.ShowBigMessage(
                $"¡NIVEL {_currentLevel}!\n+{_healPerLevel} vida",
                new Color(0.4f, 1f, 0.4f));

            Debug.Log($"[LevelProgression] ¡Nivel {_currentLevel}! Curación: +{_healPerLevel}");
        }
    }
}
