using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem.UI;
using TMPro;
using BIT.Data;
using BIT.Core;

// ============================================================================
// CHARACTERSELECTUI.CS — Pantalla de selección de personaje
// ============================================================================
// Crea toda la UI en runtime (no requiere prefabs).
// Muestra 3 personajes jugables con estadísticas y permite elegir antes
// de empezar la partida.
//
// ESCENA: la usa CharacterSelect.unity (creada por BIT → 3. Crear Escenas)
// ============================================================================

namespace BIT.UI
{
    public class CharacterSelectUI : MonoBehaviour
    {
        // ====================================================================
        // NOMBRE DE LA ESCENA DEL JUEGO
        // ====================================================================
        [SerializeField] private string _gameSceneName = "gamesetupscene";
        [SerializeField] private string _mainMenuSceneName = "MainMenu";

        // ====================================================================
        // ESTADO INTERNO
        // ====================================================================
        private CharacterData[] _characters;
        private int _selectedIndex = -1;
        private readonly List<GameObject> _cards = new();
        private Button _playButton;

        // ====================================================================
        // INICIALIZACIÓN
        // ====================================================================

        void Awake()
        {
            // Asegurar CharacterSelectManager en escena
            if (CharacterSelectManager.Instance == null)
            {
                var go = new GameObject("CharacterSelectManager");
                go.AddComponent<CharacterSelectManager>();
            }
        }

        void Start()
        {
            _characters = BuildDefaultCharacters();
            BuildUI();
        }

        // ====================================================================
        // PERSONAJES POR DEFECTO (sin necesitar Resources/)
        // ====================================================================

        static CharacterData[] BuildDefaultCharacters()
        {
            return new CharacterData[]
            {
                MakeCharacter(
                    "Ninja Azul", "Equilibrado — Bueno en todo.",
                    new Color(0.35f, 0.65f, 1f),
                    maxHealth: 100, speed: 6f, damage: 22, cooldown: 0.3f,
                    dashSpeed: 18f, dashDur: 0.18f, dashCD: 3f,
                    spritePath: "Assets/_Project/Sprites/Ninja Adventure/Actor/Character/NinjaBlue/SeparateAnim/Idle.png"),

                MakeCharacter(
                    "Ninja Rojo", "Guerrero — Alto daño, más lento.",
                    new Color(1f, 0.25f, 0.25f),
                    maxHealth: 80, speed: 5f, damage: 40, cooldown: 0.5f,
                    dashSpeed: 22f, dashDur: 0.22f, dashCD: 4f,
                    spritePath: "Assets/_Project/Sprites/Ninja Adventure/Actor/Character/NinjaRed/SeparateAnim/Idle.png"),

                MakeCharacter(
                    "Ninja Verde", "Explorador — Rápido, más resistente.",
                    new Color(0.2f, 0.9f, 0.4f),
                    maxHealth: 140, speed: 8.5f, damage: 15, cooldown: 0.22f,
                    dashSpeed: 24f, dashDur: 0.15f, dashCD: 2f,
                    spritePath: "Assets/_Project/Sprites/Ninja Adventure/Actor/Character/NinjaGreen/SeparateAnim/Idle.png"),
            };
        }

        static CharacterData MakeCharacter(
            string name, string desc, Color color,
            int maxHealth, float speed, int damage, float cooldown,
            float dashSpeed, float dashDur, float dashCD,
            string spritePath = "")
        {
            var d = ScriptableObject.CreateInstance<CharacterData>();
            d.characterName = name;
            d.description   = desc;
            d.spriteColor   = color;
            d.maxHealth     = maxHealth;
            d.moveSpeed     = speed;
            d.meleeDamage   = damage;
            d.attackCooldown = cooldown;
            d.meleeRange    = 1.2f;
            d.dashSpeed     = dashSpeed;
            d.dashDuration  = dashDur;
            d.dashCooldown  = dashCD;
            d.spritePath    = spritePath;
            return d;
        }

        // ====================================================================
        // CONSTRUCCIÓN DE LA UI
        // ====================================================================

        void BuildUI()
        {
            // ── Canvas ────────────────────────────────────────────────────
            var canvasGO = new GameObject("Canvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGO.AddComponent<GraphicRaycaster>();

            if (UnityEngine.EventSystems.EventSystem.current == null)
            {
                var evSystem = new GameObject("EventSystem");
                evSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
                evSystem.AddComponent<InputSystemUIInputModule>();
            }

            // ── Fondo oscuro ─────────────────────────────────────────────
            var bg = MakeImage(canvasGO, "Background", new Color(0.05f, 0.04f, 0.08f));
            StretchFull(bg.GetComponent<RectTransform>());

            // ── Título ────────────────────────────────────────────────────
            var titleGO = new GameObject("Title");
            titleGO.transform.SetParent(canvasGO.transform, false);
            var titleTMP = titleGO.AddComponent<TextMeshProUGUI>();
            titleTMP.text = "SELECCIONA TU NINJA";
            titleTMP.fontSize = 56;
            titleTMP.fontStyle = FontStyles.Bold;
            titleTMP.alignment = TextAlignmentOptions.Center;
            titleTMP.color = new Color(1f, 0.85f, 0.3f);
            var titleRT = titleGO.GetComponent<RectTransform>();
            titleRT.anchorMin = new Vector2(0f, 0.82f);
            titleRT.anchorMax = new Vector2(1f, 0.98f);
            titleRT.offsetMin = titleRT.offsetMax = Vector2.zero;

            // ── Subtítulo ─────────────────────────────────────────────────
            var subGO = new GameObject("Subtitle");
            subGO.transform.SetParent(canvasGO.transform, false);
            var subTMP = subGO.AddComponent<TextMeshProUGUI>();
            subTMP.text = "Cada ninja tiene habilidades únicas. ¡Elige sabiamente!";
            subTMP.fontSize = 24;
            subTMP.alignment = TextAlignmentOptions.Center;
            subTMP.color = new Color(0.7f, 0.7f, 0.7f);
            var subRT = subGO.GetComponent<RectTransform>();
            subRT.anchorMin = new Vector2(0.1f, 0.76f);
            subRT.anchorMax = new Vector2(0.9f, 0.84f);
            subRT.offsetMin = subRT.offsetMax = Vector2.zero;

            // ── 3 tarjetas de personaje ───────────────────────────────────
            float cardW = 0.26f;
            float[] cardXs = { 0.06f, 0.37f, 0.68f };

            for (int i = 0; i < _characters.Length; i++)
                BuildCard(canvasGO, _characters[i], i, cardXs[i], cardW);

            // ── Botón JUGAR ───────────────────────────────────────────────
            var playGO = BuildButton(canvasGO, "JUGAR", new Color(0.15f, 0.75f, 0.25f),
                new Vector2(0.35f, 0.06f), new Vector2(0.65f, 0.18f));
            _playButton = playGO.GetComponent<Button>();
            _playButton.interactable = false;
            _playButton.onClick.AddListener(StartGame);
            SetButtonTextColor(playGO, Color.white, 36);

            // ── Botón VOLVER ──────────────────────────────────────────────
            var backGO = BuildButton(canvasGO, "← VOLVER", new Color(0.25f, 0.25f, 0.3f),
                new Vector2(0.02f, 0.02f), new Vector2(0.22f, 0.12f));
            backGO.GetComponent<Button>().onClick.AddListener(GoBack);
            SetButtonTextColor(backGO, new Color(0.9f, 0.9f, 0.9f), 24);
        }

        // ====================================================================
        // TARJETA DE PERSONAJE
        // ====================================================================

        void BuildCard(GameObject parent, CharacterData data, int index, float xMin, float cardW)
        {
            float xMax = xMin + cardW;

            // Fondo de la tarjeta
            var cardGO = MakeImage(parent, $"Card_{index}", new Color(0.12f, 0.10f, 0.18f));
            var cardRT = cardGO.GetComponent<RectTransform>();
            cardRT.anchorMin = new Vector2(xMin, 0.22f);
            cardRT.anchorMax = new Vector2(xMax, 0.78f);
            cardRT.offsetMin = cardRT.offsetMax = Vector2.zero;
            _cards.Add(cardGO);

            // Marco de color del personaje (borde)
            var borderGO = MakeImage(cardGO, "Border", new Color(data.spriteColor.r * 0.5f, data.spriteColor.g * 0.5f, data.spriteColor.b * 0.5f));
            var borderRT = borderGO.GetComponent<RectTransform>();
            borderRT.anchorMin = Vector2.zero;
            borderRT.anchorMax = Vector2.one;
            borderRT.offsetMin = Vector2.zero;
            borderRT.offsetMax = Vector2.zero;
            borderGO.transform.SetAsFirstSibling();

            // Capa interior (encima del borde)
            var innerGO = MakeImage(cardGO, "Inner", new Color(0.10f, 0.08f, 0.15f));
            var innerRT = innerGO.GetComponent<RectTransform>();
            innerRT.anchorMin = Vector2.zero;
            innerRT.anchorMax = Vector2.one;
            innerRT.offsetMin = new Vector2(3, 3);
            innerRT.offsetMax = new Vector2(-3, -3);

            // "Avatar": sprite real del personaje o fallback de color
            var avatarGO = MakeImage(innerGO, "Avatar", new Color(0.08f, 0.06f, 0.12f));
            var avatarRT = avatarGO.GetComponent<RectTransform>();
            avatarRT.anchorMin = new Vector2(0.15f, 0.65f);
            avatarRT.anchorMax = new Vector2(0.85f, 0.97f);
            avatarRT.offsetMin = avatarRT.offsetMax = Vector2.zero;

            Sprite characterSprite = TryLoadSprite(data.spritePath);
            if (characterSprite != null)
            {
                var sprImg = avatarGO.GetComponent<Image>();
                sprImg.sprite = characterSprite;
                sprImg.color = data.spriteColor;
                sprImg.preserveAspect = true;
            }
            else
            {
                avatarGO.GetComponent<Image>().color = data.spriteColor;
                var initGO = new GameObject("Initial");
                initGO.transform.SetParent(avatarGO.transform, false);
                var initTMP = initGO.AddComponent<TextMeshProUGUI>();
                initTMP.text = data.characterName[0].ToString();
                initTMP.fontSize = 72;
                initTMP.fontStyle = FontStyles.Bold;
                initTMP.alignment = TextAlignmentOptions.Center;
                initTMP.color = new Color(0f, 0f, 0f, 0.5f);
                StretchFull(initGO.GetComponent<RectTransform>());
            }

            // Nombre del personaje
            AddText(innerGO, "Name", data.characterName, 22, FontStyles.Bold, data.spriteColor,
                new Vector2(0f, 0.55f), new Vector2(1f, 0.64f));

            // Descripción
            AddText(innerGO, "Desc", data.description, 14, FontStyles.Normal, new Color(0.75f, 0.75f, 0.75f),
                new Vector2(0.05f, 0.46f), new Vector2(0.95f, 0.55f));

            // Barras de estadísticas
            float barY = 0.375f;
            AddStatBar(innerGO, "HP",   NormalizeHealth(data.maxHealth),   data.spriteColor, ref barY);
            AddStatBar(innerGO, "SPD",  NormalizeSpeed(data.moveSpeed),    data.spriteColor, ref barY);
            AddStatBar(innerGO, "DMG",  NormalizeDamage(data.meleeDamage), data.spriteColor, ref barY);
            AddStatBar(innerGO, "DASH", NormalizeDash(data.dashCooldown),  data.spriteColor, ref barY);

            // Botón ELEGIR
            var selGO = BuildButton(innerGO, "ELEGIR", new Color(0.2f, 0.2f, 0.3f),
                new Vector2(0.05f, 0.01f), new Vector2(0.95f, 0.11f));
            int capturedIndex = index;
            selGO.GetComponent<Button>().onClick.AddListener(() => SelectCharacter(capturedIndex));
            SetButtonTextColor(selGO, data.spriteColor, 20);
        }

        void AddStatBar(GameObject parent, string label, float normalizedValue, Color barColor, ref float yMin)
        {
            float yMax = yMin + 0.07f;

            // Etiqueta
            AddText(parent, label + "_Label", label, 13, FontStyles.Normal, new Color(0.8f, 0.8f, 0.8f),
                new Vector2(0.03f, yMin), new Vector2(0.38f, yMax));

            // Fondo de la barra
            var bgBar = MakeImage(parent, label + "_BG", new Color(0.08f, 0.08f, 0.12f));
            var bgRT = bgBar.GetComponent<RectTransform>();
            bgRT.anchorMin = new Vector2(0.40f, yMin + 0.01f);
            bgRT.anchorMax = new Vector2(0.97f, yMax - 0.01f);
            bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;

            // Fill de la barra
            var fillBar = MakeImage(bgBar, label + "_Fill", barColor);
            var fillRT = fillBar.GetComponent<RectTransform>();
            fillRT.anchorMin = Vector2.zero;
            fillRT.anchorMax = new Vector2(Mathf.Clamp01(normalizedValue), 1f);
            fillRT.offsetMin = fillRT.offsetMax = Vector2.zero;

            yMin -= 0.085f;
        }

        // ====================================================================
        // NORMALIZACIÓN DE ESTADÍSTICAS (0→1)
        // ====================================================================
        static float NormalizeHealth(int hp)   => Mathf.InverseLerp(50, 200, hp);
        static float NormalizeSpeed(float s)   => Mathf.InverseLerp(2f, 9f, s);
        static float NormalizeDamage(int dmg)  => Mathf.InverseLerp(5, 35, dmg);
        static float NormalizeDash(float cd)   => Mathf.InverseLerp(5f, 1f, cd); // menos CD = mejor

        // ====================================================================
        // LÓGICA DE SELECCIÓN
        // ====================================================================

        void SelectCharacter(int index)
        {
            _selectedIndex = index;

            // Actualizar apariencia visual de las tarjetas
            for (int i = 0; i < _cards.Count; i++)
            {
                var img = _cards[i].GetComponent<Image>();
                img.color = i == index
                    ? new Color(0.18f, 0.15f, 0.28f)
                    : new Color(0.12f, 0.10f, 0.18f);

                var border = _cards[i].transform.Find("Border")?.GetComponent<Image>();
                if (border != null)
                {
                    var c = _characters[i].spriteColor;
                    border.color = i == index
                        ? new Color(c.r, c.g, c.b, 1f)
                        : new Color(c.r * 0.5f, c.g * 0.5f, c.b * 0.5f, 1f);
                }
            }

            // Notificar al manager
            CharacterSelectManager.Instance?.SelectCharacter(_characters[index]);

            // Habilitar botón de jugar
            if (_playButton != null)
            {
                _playButton.interactable = true;
                var img = _playButton.GetComponent<Image>();
                if (img != null) img.color = new Color(0.15f, 0.75f, 0.25f);
            }

            StartCoroutine(PulseCard(index));
        }

        IEnumerator PulseCard(int index)
        {
            if (index >= _cards.Count) yield break;
            var rt = _cards[index].GetComponent<RectTransform>();
            float t = 0;
            while (t < 0.2f)
            {
                float s = 1f + Mathf.Sin(t / 0.2f * Mathf.PI) * 0.03f;
                rt.localScale = new Vector3(s, s, 1f);
                t += Time.deltaTime;
                yield return null;
            }
            rt.localScale = Vector3.one;
        }

        void StartGame()
        {
            if (_selectedIndex < 0) return;
            SceneManager.LoadScene(_gameSceneName);
        }

        void GoBack()
        {
            SceneManager.LoadScene(_mainMenuSceneName);
        }

        // ====================================================================
        // HELPERS DE UI
        // ====================================================================

        static GameObject MakeImage(GameObject parent, string name, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var img = go.AddComponent<Image>();
            img.color = color;
            return go;
        }

        static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        static void AddText(GameObject parent, string name, string text, float fontSize,
            FontStyles style, Color color, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = color;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        static GameObject BuildButton(GameObject parent, string label, Color bgColor,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = MakeImage(parent, label + "_Btn", bgColor);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            go.AddComponent<Button>();

            var textGO = new GameObject("Text");
            textGO.transform.SetParent(go.transform, false);
            var tmp = textGO.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            StretchFull(textGO.GetComponent<RectTransform>());

            return go;
        }

        static void SetButtonTextColor(GameObject btnGO, Color color, float size)
        {
            var tmp = btnGO.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null) { tmp.color = color; tmp.fontSize = size; }
        }

        static Sprite TryLoadSprite(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
#if UNITY_EDITOR
            var sprites = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var a in sprites)
                if (a is Sprite s) return s;
            var tex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex != null) return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
#endif
            return null;
        }
    }
}
