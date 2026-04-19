using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace BIT.Core
{
    // Tracks consecutive kills within a time window and applies score multipliers.
    // Enemies call ComboManager.Instance.RegisterKill(baseScore) in Die().
    // Creates its own UI element at runtime.
    public class ComboManager : MonoBehaviour
    {
        public static ComboManager Instance { get; private set; }

        [Header("=== COMBO CONFIG ===")]
        [Tooltip("Tiempo máximo entre kills para mantener el combo")]
        [SerializeField] private float _comboTimeWindow = 2.5f;
        [Tooltip("Kills para multiplicador x1.5")]
        [SerializeField] private int _killsForDouble = 3;
        [Tooltip("Kills para multiplicador x2")]
        [SerializeField] private int _killsForTriple = 6;
        [Tooltip("Kills para multiplicador x3")]
        [SerializeField] private int _killsForUltra = 10;

        private int _currentCombo = 0;
        private Coroutine _resetCoroutine;

        // UI
        private Text _comboText;
        private GameObject _comboGO;
        private Coroutine _hideCoroutine;

        public int CurrentCombo => _currentCombo;

        public float CurrentMultiplier =>
            _currentCombo >= _killsForUltra ? 3f :
            _currentCombo >= _killsForTriple ? 2f :
            _currentCombo >= _killsForDouble ? 1.5f : 1f;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        void Start() => StartCoroutine(CreateUIDelayed());

        IEnumerator CreateUIDelayed()
        {
            yield return new WaitForSeconds(0.25f);
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas != null) BuildWidget(canvas.transform);
        }

        void BuildWidget(Transform parent)
        {
            _comboGO = new GameObject("ComboDisplay");
            _comboGO.transform.SetParent(parent, false);

            _comboText = _comboGO.AddComponent<Text>();
            _comboText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _comboText.fontSize = 44;
            _comboText.fontStyle = FontStyle.Bold;
            _comboText.alignment = TextAnchor.MiddleCenter;
            _comboText.color = new Color(1f, 0.85f, 0f);

            Outline outline = _comboGO.AddComponent<Outline>();
            outline.effectColor = new Color(0.4f, 0.15f, 0f);
            outline.effectDistance = new Vector2(3, -3);

            RectTransform rt = _comboGO.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(1f, 0f);
            rt.anchoredPosition = new Vector2(-20f, 20f);
            rt.sizeDelta = new Vector2(280f, 70f);

            _comboGO.SetActive(false);
        }

        // Call from enemy Die(). Returns score after applying multiplier.
        public int RegisterKill(int baseScore)
        {
            _currentCombo++;

            if (_resetCoroutine != null) StopCoroutine(_resetCoroutine);
            _resetCoroutine = StartCoroutine(ResetAfterDelay());

            int finalScore = Mathf.RoundToInt(baseScore * CurrentMultiplier);
            RefreshComboUI();
            return finalScore;
        }

        void RefreshComboUI()
        {
            if (_comboGO == null) return;

            string label = null;
            Color color = new Color(1f, 0.85f, 0f);

            if (_currentCombo >= _killsForUltra)
            {
                label = $"ULTRA COMBO x3!\n{_currentCombo} kills";
                color = new Color(1f, 0.3f, 0f);
            }
            else if (_currentCombo >= _killsForTriple)
            {
                label = $"TRIPLE x2!\n{_currentCombo} kills";
                color = new Color(1f, 0.55f, 0f);
            }
            else if (_currentCombo >= _killsForDouble)
            {
                label = $"DOBLE x1.5!\n{_currentCombo} kills";
                color = new Color(1f, 0.85f, 0f);
            }
            else if (_currentCombo > 1)
            {
                label = $"COMBO x{_currentCombo}";
                color = Color.white;
            }

            if (label != null)
            {
                _comboText.text = label;
                _comboText.color = color;
                _comboGO.SetActive(true);

                if (_hideCoroutine != null) StopCoroutine(_hideCoroutine);
                _hideCoroutine = StartCoroutine(HideAfterDelay());
            }
        }

        IEnumerator HideAfterDelay()
        {
            yield return new WaitForSeconds(1.8f);
            if (_comboGO != null) _comboGO.SetActive(false);
        }

        IEnumerator ResetAfterDelay()
        {
            yield return new WaitForSeconds(_comboTimeWindow);
            _currentCombo = 0;
        }

        public void ResetCombo()
        {
            _currentCombo = 0;
            if (_resetCoroutine != null) StopCoroutine(_resetCoroutine);
        }
    }
}
