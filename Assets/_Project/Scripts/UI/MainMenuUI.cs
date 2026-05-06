using System.Collections.Generic;
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
        private GameObject     _rankingOverlay;
        private Canvas         _runtimeCanvas;

        void Start()
        {
            EnsureSaveSystem();

            if (UnityEngine.EventSystems.EventSystem.current == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                es.AddComponent<InputSystemUIInputModule>();
            }
            // Añadir AudioListener si no hay ninguno en la escena
            if (FindFirstObjectByType<AudioListener>() == null)
                new GameObject("AudioListener").AddComponent<AudioListener>();

            PrepareSceneMenuCanvases();
            BuildUI();
            LoadSavedName();
        }

        // ── Build ─────────────────────────────────────────────────────────────

        void BuildUI()
        {
            var existing = GameObject.Find("MainMenuRuntimeCanvas");
            if (existing != null) Destroy(existing);

            var canvasGO = new GameObject("MainMenuRuntimeCanvas");
            var canvas   = canvasGO.AddComponent<Canvas>();
            _runtimeCanvas = canvas;
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 500;
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight  = 0.5f;
            canvasGO.AddComponent<GraphicRaycaster>();

            // Fondo
            Img(canvasGO, "BG", new Color(0.04f, 0.03f, 0.07f))
                .Anc(V(0,0), V(1,1));

            // Título
            TMP(canvasGO, "Title", "BIT", 108, FontStyles.Bold,
                new Color(1f, 0.88f, 0.15f), V(0.15f, 0.82f), V(0.85f, 0.97f));

            // Subtítulo
            TMP(canvasGO, "Sub", "Ninja Survival - Accion y Supervivencia", 26, FontStyles.Normal,
                new Color(0.62f, 0.62f, 0.72f), V(0.15f, 0.73f), V(0.85f, 0.81f));

            // High score
            string hs = SaveSystem.Instance != null
                ? $"Mejor puntuación: {SaveSystem.Instance.GetHighScore():N0}"
                : "Mejor puntuación: -";
            TMP(canvasGO, "HS", hs, 22, FontStyles.Normal,
                new Color(0.55f, 0.55f, 0.65f), V(0.25f, 0.65f), V(0.75f, 0.72f));

            // Etiqueta nombre
            TMP(canvasGO, "NameLbl", "TU NOMBRE", 20, FontStyles.Bold,
                new Color(0.75f, 0.75f, 0.85f), V(0.28f, 0.58f), V(0.72f, 0.64f));

            // Input
            _nameInput = MakeInput(canvasGO, V(0.28f, 0.48f), V(0.72f, 0.57f));

            // JUGAR  (gap ~0.09 con el input)
            Btn(canvasGO, "JUGAR", new Color(0.12f, 0.60f, 0.20f), Color.white, 42,
                V(0.32f, 0.315f), V(0.68f, 0.415f), OnPlayClicked);

            // SCOREBOARD
            Btn(canvasGO, "SCOREBOARD", new Color(0.14f, 0.18f, 0.30f),
                new Color(0.85f, 0.85f, 0.95f), 28,
                V(0.32f, 0.18f), V(0.68f, 0.28f), OnRankingClicked);

            // SALIR
            Btn(canvasGO, "SALIR", new Color(0.22f, 0.07f, 0.07f),
                new Color(0.90f, 0.70f, 0.70f), 28,
                V(0.32f, 0.045f), V(0.68f, 0.145f), OnQuitClicked);

            // Versión
            TMP(canvasGO, "Ver", $"v{Application.version}", 15, FontStyles.Normal,
                new Color(0.30f, 0.30f, 0.35f), V(0.80f, 0.00f), V(1.00f, 0.04f));

            // Panel ranking (oculto)
            _rankingOverlay = BuildRankingOverlay(canvasGO);
        }

        GameObject BuildRankingOverlay(GameObject canvas)
        {
            var panel = new GameObject("RankingOverlay");
            panel.transform.SetParent(canvas.transform, false);
            panel.AddComponent<Image>().color = new Color(0.04f, 0.03f, 0.07f, 1f);
            panel.GetComponent<RectTransform>().SetAnch(V(0,0), V(1,1));

            TMP(panel, "Title", "SCOREBOARD", 72, FontStyles.Bold,
                new Color(1f, 0.85f, 0.15f), V(0.1f, 0.82f), V(0.9f, 0.97f));

            // Entradas
            float y = 0.74f;
            List<RankingEntry> entries = null;
            if (SaveSystem.Instance != null)
                entries = SaveSystem.Instance.GetRanking();

            if (entries == null || entries.Count == 0)
            {
                TMP(panel, "Empty", "Aun no hay puntuaciones.\nJuega una partida para aparecer aqui.",
                    28, FontStyles.Normal, new Color(0.55f, 0.55f, 0.65f),
                    V(0.15f, 0.35f), V(0.85f, 0.75f));
            }
            else
            {
                Color[] rowColors = {
                    new Color(1f, 0.82f, 0.10f),   // oro
                    new Color(0.80f, 0.80f, 0.82f), // plata
                    new Color(0.80f, 0.55f, 0.25f), // bronce
                };
                int show = Mathf.Min(entries.Count, 8);
                for (int i = 0; i < show; i++)
                {
                    Color c = i < 3 ? rowColors[i] : new Color(0.75f, 0.75f, 0.80f);
                    string medal = $"{i + 1}.";
                    string line  = $"{medal}  {entries[i].playerName}  -  {entries[i].score:N0}";
                    float h = 0.065f;
                    TMP(panel, $"E{i}", line, 26, FontStyles.Normal, c,
                        V(0.10f, y - h), V(0.90f, y));
                    y -= h + 0.008f;
                }
            }

            // Botón volver
            Btn(panel, "< VOLVER", new Color(0.18f, 0.18f, 0.28f),
                new Color(0.85f, 0.85f, 0.95f), 28,
                V(0.35f, 0.03f), V(0.65f, 0.12f),
                () => panel.SetActive(false));

            panel.SetActive(false);
            return panel;
        }

        // ── Actions ───────────────────────────────────────────────────────────

        void LoadSavedName()
        {
            if (_nameInput == null || SaveSystem.Instance == null) return;
            string last = SaveSystem.Instance.GetLastPlayerName();
            if (!string.IsNullOrEmpty(last)) _nameInput.text = last;
        }

        void OnPlayClicked()
        {
            string name = _nameInput != null ? _nameInput.text.Trim() : "";
            if (string.IsNullOrEmpty(name)) name = "Jugador";

            Time.timeScale = 1f;

            if (CharacterSelectManager.Instance == null)
                new GameObject("CharacterSelectManager").AddComponent<CharacterSelectManager>();
            CharacterSelectManager.Instance?.SetPlayerName(name);
            GameManager.Instance?.SetPlayerName(name);
            SaveSystem.Instance?.SetLastPlayerName(name);

            // Cargar CharacterSelect; si no existe, ir al juego directamente
            if (Application.CanStreamedLevelBeLoaded(_characterSelectScene))
                SceneManager.LoadScene(_characterSelectScene);
            else
                SceneManager.LoadScene(_gameSceneName);
        }

        void OnRankingClicked()
        {
            // Reconstruir entradas cada vez que se abre (puntuaciones pueden haber cambiado)
            if (_rankingOverlay != null)
            {
                _rankingOverlay.transform.SetAsLastSibling();
                _rankingOverlay.SetActive(true);
                RefreshRankingOverlay();
            }
        }

        void RefreshRankingOverlay()
        {
            if (_rankingOverlay == null) return;
            // Destruir entradas anteriores (todos los hijos excepto Title, Empty y VOLVER)
            foreach (Transform child in _rankingOverlay.transform)
            {
                string n = child.name;
                if (n.StartsWith("E") || n == "Empty")
                    Destroy(child.gameObject);
            }

            List<RankingEntry> entries = SaveSystem.Instance?.GetRanking();
            float y = 0.74f;
            if (entries == null || entries.Count == 0)
            {
                TMP(_rankingOverlay, "Empty",
                    "Aun no hay puntuaciones.\nJuega una partida para aparecer aqui.",
                    28, FontStyles.Normal, new Color(0.55f, 0.55f, 0.65f),
                    V(0.15f, 0.35f), V(0.85f, 0.75f));
            }
            else
            {
                Color[] rowColors = {
                    new Color(1f, 0.82f, 0.10f),
                    new Color(0.80f, 0.80f, 0.82f),
                    new Color(0.80f, 0.55f, 0.25f),
                };
                int show = Mathf.Min(entries.Count, 8);
                for (int i = 0; i < show; i++)
                {
                    Color c = i < 3 ? rowColors[i] : new Color(0.75f, 0.75f, 0.80f);
                    string medal = $"{i + 1}.";
                    string line  = $"{medal}  {entries[i].playerName}  -  {entries[i].score:N0}";
                    float h = 0.065f;
                    TMP(_rankingOverlay, $"E{i}", line, 26, FontStyles.Normal, c,
                        V(0.10f, y - h), V(0.90f, y));
                    y -= h + 0.008f;
                }
            }
        }

        void OnQuitClicked()
        {
            Time.timeScale = 1f;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        static void EnsureSaveSystem()
        {
            if (SaveSystem.Instance != null) return;
            new GameObject("SaveSystem").AddComponent<SaveSystem>();
        }

        void PrepareSceneMenuCanvases()
        {
            foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (canvas == null) continue;
                if (canvas == _runtimeCanvas) continue;
                if (canvas.gameObject.name == "MainMenuRuntimeCanvas") continue;

                canvas.sortingOrder = -100;
                var raycaster = canvas.GetComponent<GraphicRaycaster>();
                if (raycaster != null) raycaster.enabled = false;
            }
        }

        // ── UI helpers ────────────────────────────────────────────────────────

        static Vector2 V(float x, float y) => new Vector2(x, y);

        static RectTransformHelper Img(GameObject parent, string name, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            go.AddComponent<Image>().color = color;
            return new RectTransformHelper(go.GetComponent<RectTransform>());
        }

        static void TMP(GameObject parent, string name, string text,
            float size, FontStyles style, Color color, Vector2 aMin, Vector2 aMax)
        {
            var go  = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var t   = go.AddComponent<TextMeshProUGUI>();
            t.text = text; t.fontSize = size; t.fontStyle = style;
            t.color = color; t.alignment = TextAlignmentOptions.Center;
            go.GetComponent<RectTransform>().SetAnch(aMin, aMax);
        }

        static void Btn(GameObject parent, string label,
            Color bg, Color fg, float size, Vector2 aMin, Vector2 aMax,
            UnityEngine.Events.UnityAction action)
        {
            var go  = new GameObject(label + "_Btn");
            go.transform.SetParent(parent.transform, false);
            go.AddComponent<Image>().color = bg;
            var btn    = go.AddComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = Brighter(bg, 0.15f);
            colors.pressedColor     = Brighter(bg, 0.30f);
            btn.colors = colors;
            btn.onClick.AddListener(action);
            go.GetComponent<RectTransform>().SetAnch(aMin, aMax);

            var tGO = new GameObject("T");
            tGO.transform.SetParent(go.transform, false);
            var t   = tGO.AddComponent<TextMeshProUGUI>();
            t.text = label; t.fontSize = size; t.fontStyle = FontStyles.Bold;
            t.color = fg; t.alignment = TextAlignmentOptions.Center;
            tGO.GetComponent<RectTransform>().SetAnch(V(0,0), V(1,1));
        }

        TMP_InputField MakeInput(GameObject parent, Vector2 aMin, Vector2 aMax)
        {
            var go = new GameObject("NameInput");
            go.transform.SetParent(parent.transform, false);
            go.AddComponent<Image>().color = new Color(0.12f, 0.10f, 0.20f);
            go.GetComponent<RectTransform>().SetAnch(aMin, aMax);

            var field = go.AddComponent<TMP_InputField>();

            var taGO = new GameObject("TextArea");
            taGO.transform.SetParent(go.transform, false);
            taGO.AddComponent<RectMask2D>();
            taGO.GetComponent<RectTransform>().SetAnch(V(0,0), V(1,1),
                new Vector2(10,6), new Vector2(-10,-6));

            var txtGO = new GameObject("Text");
            txtGO.transform.SetParent(taGO.transform, false);
            var txt = txtGO.AddComponent<TextMeshProUGUI>();
            txt.fontSize = 28; txt.color = Color.white;
            txt.alignment = TextAlignmentOptions.MidlineLeft;
            txtGO.GetComponent<RectTransform>().SetAnch(V(0,0), V(1,1));

            var phGO = new GameObject("Placeholder");
            phGO.transform.SetParent(taGO.transform, false);
            var ph = phGO.AddComponent<TextMeshProUGUI>();
            ph.text = "Escribe tu nombre..."; ph.fontSize = 28;
            ph.color = new Color(0.5f, 0.5f, 0.6f, 0.8f);
            ph.fontStyle = FontStyles.Italic;
            ph.alignment = TextAlignmentOptions.MidlineLeft;
            phGO.GetComponent<RectTransform>().SetAnch(V(0,0), V(1,1));

            field.textViewport  = taGO.GetComponent<RectTransform>();
            field.textComponent = txt;
            field.placeholder   = ph;
            field.characterLimit = 20;
            return field;
        }

        static Color Brighter(Color c, float amt) =>
            new Color(Mathf.Min(c.r + amt,1), Mathf.Min(c.g + amt,1), Mathf.Min(c.b + amt,1));

        // ── Mini helpers ──────────────────────────────────────────────────────

        struct RectTransformHelper
        {
            readonly RectTransform _rt;
            public RectTransformHelper(RectTransform rt) { _rt = rt; }
            public void Anc(Vector2 min, Vector2 max)
            {
                _rt.anchorMin = min; _rt.anchorMax = max;
                _rt.offsetMin = _rt.offsetMax = Vector2.zero;
            }
        }
    }

    static class RTExt
    {
        public static void SetAnch(this RectTransform rt, Vector2 min, Vector2 max,
            Vector2 offMin = default, Vector2 offMax = default)
        {
            rt.anchorMin = min; rt.anchorMax = max;
            rt.offsetMin = offMin; rt.offsetMax = offMax;
        }
    }
}
