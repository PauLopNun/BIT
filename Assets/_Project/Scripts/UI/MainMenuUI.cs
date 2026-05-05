using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem.UI;
using TMPro;
using BIT.Core;

namespace BIT.UI
{
    public class MainMenuUI : MonoBehaviour
    {
        [SerializeField] private string _characterSelectScene = "CharacterSelect";
        [SerializeField] private string _gameSceneName        = "gamesetupscene";

        private TMP_InputField _nameInput;

        void Start()
        {
            if (UnityEngine.EventSystems.EventSystem.current == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                es.AddComponent<InputSystemUIInputModule>();
            }

            BuildUI();
            LoadSavedName();
        }

        // ── UI construction ──────────────────────────────────────────────────

        void BuildUI()
        {
            var canvasGO = new GameObject("Canvas");
            var canvas   = canvasGO.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight  = 0.5f;

            canvasGO.AddComponent<GraphicRaycaster>();

            // Fondo
            MakeImage(canvasGO, "BG", new Color(0.04f, 0.03f, 0.07f)).GetComponent<RectTransform>()
                .SetAnchors(Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            // Franja decorativa central
            var stripe = MakeImage(canvasGO, "Stripe", new Color(0.10f, 0.07f, 0.18f, 0.6f));
            stripe.GetComponent<RectTransform>()
                .SetAnchors(new Vector2(0f, 0.14f), new Vector2(1f, 0.86f), Vector2.zero, Vector2.zero);

            // Título
            AddTMP(canvasGO, "Title", "BIT", 110, FontStyles.Bold,
                new Color(1f, 0.88f, 0.15f),
                new Vector2(0.15f, 0.72f), new Vector2(0.85f, 0.96f));

            // Subtítulo
            AddTMP(canvasGO, "Sub", "Ninja Survival — ¿Hasta dónde llegarás?", 26, FontStyles.Normal,
                new Color(0.65f, 0.65f, 0.75f),
                new Vector2(0.15f, 0.63f), new Vector2(0.85f, 0.73f));

            // High score
            string hsText = "High Score: 0";
            if (SaveSystem.Instance != null)
                hsText = $"High Score: {SaveSystem.Instance.GetHighScore():N0}";
            AddTMP(canvasGO, "HS", hsText, 22, FontStyles.Normal,
                new Color(0.55f, 0.55f, 0.65f),
                new Vector2(0.30f, 0.57f), new Vector2(0.70f, 0.64f));

            // Separador
            var sep = MakeImage(canvasGO, "Sep", new Color(0.35f, 0.25f, 0.60f, 0.5f));
            sep.GetComponent<RectTransform>()
                .SetAnchors(new Vector2(0.25f, 0.555f), new Vector2(0.75f, 0.560f), Vector2.zero, Vector2.zero);

            // Etiqueta nombre
            AddTMP(canvasGO, "NameLabel", "TU NOMBRE", 20, FontStyles.Bold,
                new Color(0.75f, 0.75f, 0.85f),
                new Vector2(0.30f, 0.495f), new Vector2(0.70f, 0.545f));

            // Input field
            _nameInput = MakeInputField(canvasGO,
                new Vector2(0.28f, 0.42f), new Vector2(0.72f, 0.49f));

            // Botón JUGAR
            MakeButton(canvasGO, "JUGAR", new Color(0.12f, 0.62f, 0.22f),
                new Color(1f, 1f, 1f), 42,
                new Vector2(0.30f, 0.29f), new Vector2(0.70f, 0.41f),
                OnPlayClicked);

            // Botón RANKING
            MakeButton(canvasGO, "RANKING", new Color(0.15f, 0.18f, 0.30f),
                new Color(0.85f, 0.85f, 0.95f), 28,
                new Vector2(0.30f, 0.17f), new Vector2(0.70f, 0.27f),
                OnRankingClicked);

            // Botón SALIR
            MakeButton(canvasGO, "SALIR", new Color(0.22f, 0.07f, 0.07f),
                new Color(0.90f, 0.70f, 0.70f), 28,
                new Vector2(0.30f, 0.05f), new Vector2(0.70f, 0.15f),
                OnQuitClicked);

            // Versión
            AddTMP(canvasGO, "Version", $"v{Application.version}", 16, FontStyles.Normal,
                new Color(0.35f, 0.35f, 0.40f),
                new Vector2(0.80f, 0.00f), new Vector2(1.00f, 0.05f));
        }

        // ── Acciones ─────────────────────────────────────────────────────────

        void LoadSavedName()
        {
            if (_nameInput == null) return;
            if (SaveSystem.Instance != null)
            {
                string last = SaveSystem.Instance.GetLastPlayerName();
                if (!string.IsNullOrEmpty(last)) _nameInput.text = last;
            }
        }

        void OnPlayClicked()
        {
            string name = _nameInput != null ? _nameInput.text.Trim() : "";
            if (string.IsNullOrEmpty(name)) name = "Jugador";

            // Guardar nombre en CharacterSelectManager para que llegue al juego
            if (CharacterSelectManager.Instance == null)
            {
                var go = new GameObject("CharacterSelectManager");
                go.AddComponent<CharacterSelectManager>();
            }
            CharacterSelectManager.Instance?.SetPlayerName(name);

            // Guardar nombre en SaveSystem si está disponible
            SaveSystem.Instance?.SetLastPlayerName(name);

            // Intentar cargar CharacterSelect; si no existe, ir directamente al juego
            try   { SceneManager.LoadScene(_characterSelectScene); }
            catch { SceneManager.LoadScene(_gameSceneName); }
        }

        void OnRankingClicked()
        {
            // Mostrar ranking in-place si existe RankingUI, o mostrar mensaje
            var ranking = FindFirstObjectByType<RankingUI>();
            if (ranking != null) ranking.ToggleRanking();
            else ShowSimpleRanking();
        }

        void OnQuitClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        void ShowSimpleRanking()
        {
            if (SaveSystem.Instance == null) return;
            var entries = SaveSystem.Instance.GetRanking();
            if (entries == null || entries.Count == 0)
            {
                Debug.Log("[MainMenu] No hay entradas en el ranking todavía.");
                return;
            }
            var sb = new System.Text.StringBuilder("=== RANKING ===\n");
            for (int i = 0; i < Mathf.Min(entries.Count, 5); i++)
                sb.AppendLine($"{i + 1}. {entries[i].playerName} — {entries[i].score:N0}");
            Debug.Log(sb.ToString());
        }

        // ── Helpers de UI ────────────────────────────────────────────────────

        static GameObject MakeImage(GameObject parent, string name, Color color)
        {
            var go  = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            go.AddComponent<Image>().color = color;
            return go;
        }

        static void AddTMP(GameObject parent, string name, string text,
            float size, FontStyles style, Color color,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            var go  = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text      = text;
            tmp.fontSize  = size;
            tmp.fontStyle = style;
            tmp.color     = color;
            tmp.alignment = TextAlignmentOptions.Center;
            go.GetComponent<RectTransform>()
              .SetAnchors(anchorMin, anchorMax, Vector2.zero, Vector2.zero);
        }

        static void MakeButton(GameObject parent, string label,
            Color bgColor, Color textColor, float fontSize,
            Vector2 anchorMin, Vector2 anchorMax,
            UnityEngine.Events.UnityAction action)
        {
            var go  = new GameObject(label + "_Btn");
            go.transform.SetParent(parent.transform, false);
            go.AddComponent<Image>().color = bgColor;
            var btn    = go.AddComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = new Color(
                Mathf.Min(bgColor.r + 0.15f, 1f),
                Mathf.Min(bgColor.g + 0.15f, 1f),
                Mathf.Min(bgColor.b + 0.15f, 1f));
            colors.pressedColor = new Color(
                Mathf.Min(bgColor.r + 0.30f, 1f),
                Mathf.Min(bgColor.g + 0.30f, 1f),
                Mathf.Min(bgColor.b + 0.30f, 1f));
            btn.colors = colors;
            btn.onClick.AddListener(action);

            go.GetComponent<RectTransform>()
              .SetAnchors(anchorMin, anchorMax, Vector2.zero, Vector2.zero);

            var textGO = new GameObject("Text");
            textGO.transform.SetParent(go.transform, false);
            var tmp       = textGO.AddComponent<TextMeshProUGUI>();
            tmp.text      = label;
            tmp.fontSize  = fontSize;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color     = textColor;
            tmp.alignment = TextAlignmentOptions.Center;
            textGO.GetComponent<RectTransform>()
                  .SetAnchors(Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        }

        TMP_InputField MakeInputField(GameObject parent, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go  = new GameObject("NameInput");
            go.transform.SetParent(parent.transform, false);
            go.AddComponent<Image>().color = new Color(0.14f, 0.12f, 0.22f);
            go.GetComponent<RectTransform>()
              .SetAnchors(anchorMin, anchorMax, Vector2.zero, Vector2.zero);

            var field = go.AddComponent<TMP_InputField>();

            // Text area
            var textAreaGO = new GameObject("Text Area");
            textAreaGO.transform.SetParent(go.transform, false);
            var mask = textAreaGO.AddComponent<RectMask2D>();
            var taRT = textAreaGO.GetComponent<RectTransform>();
            taRT.SetAnchors(Vector2.zero, Vector2.one, new Vector2(10, 6), new Vector2(-10, -6));

            // Text
            var textGO  = new GameObject("Text");
            textGO.transform.SetParent(textAreaGO.transform, false);
            var tmp     = textGO.AddComponent<TextMeshProUGUI>();
            tmp.fontSize  = 28;
            tmp.color     = Color.white;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            textGO.GetComponent<RectTransform>()
                  .SetAnchors(Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            // Placeholder
            var phGO  = new GameObject("Placeholder");
            phGO.transform.SetParent(textAreaGO.transform, false);
            var phTMP = phGO.AddComponent<TextMeshProUGUI>();
            phTMP.text      = "Escribe tu nombre…";
            phTMP.fontSize  = 28;
            phTMP.color     = new Color(0.5f, 0.5f, 0.6f, 0.8f);
            phTMP.fontStyle = FontStyles.Italic;
            phTMP.alignment = TextAlignmentOptions.MidlineLeft;
            phGO.GetComponent<RectTransform>()
                .SetAnchors(Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            field.textViewport   = taRT;
            field.textComponent  = tmp;
            field.placeholder    = phTMP;
            field.characterLimit = 20;
            field.text           = "";

            return field;
        }
    }

    // Extensión local para simplificar los SetAnchors repetitivos
    static class RectTransformExt
    {
        public static void SetAnchors(this RectTransform rt,
            Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax)
        {
            rt.anchorMin  = anchorMin;
            rt.anchorMax  = anchorMax;
            rt.offsetMin  = offsetMin;
            rt.offsetMax  = offsetMax;
        }
    }
}
