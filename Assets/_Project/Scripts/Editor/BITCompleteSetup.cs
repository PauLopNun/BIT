#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using System.IO;

// ============================================================================
// BITCOMPLETESETUP.CS - Paso 2 del Setup: WaveManager + UI
// ============================================================================
// USO:
//   1. Primero ejecuta: BIT → Setup Ninja Adventure Scene
//   2. Luego ejecuta:   BIT → Completar Setup (WaveManager + UI)
//
// Este script añade a la escena ya configurada:
//   - PlayerStatsSO (asset ScriptableObject)
//   - WaveManager con todos los prefabs conectados
//   - Canvas con HUD (TextMeshPro): ronda, enemigos, score, vida
//   - Panel de mensaje de oleada
//   - Panel de Game Over mejorado
//   - UIManager con todas las referencias conectadas
// ============================================================================

namespace BIT.Editor
{
    public class BITCompleteSetup
    {
        private const string PREFABS_PATH   = "Assets/_Project/Prefabs";
        private const string SO_PATH        = "Assets/_Project/SO_Data/Stats";
        private const string SO_ASSET_PATH  = "Assets/_Project/SO_Data/Stats/PlayerStats.asset";

        // ====================================================================
        // PUNTO DE ENTRADA
        // ====================================================================

        [MenuItem("BIT/Completar Setup (WaveManager + UI)")]
        public static void RunCompleteSetup()
        {
            // Verificar que la escena base ya fue configurada
            if (GameObject.Find("Grid") == null)
            {
                bool proceed = EditorUtility.DisplayDialog(
                    "BIT - Aviso",
                    "No se detectó el Grid del Tilemap.\n\n" +
                    "¿Ya ejecutaste 'BIT → Setup Ninja Adventure Scene'?\n\n" +
                    "Puedes continuar igualmente — este script solo añade el WaveManager y la UI.",
                    "Continuar",
                    "Cancelar"
                );
                if (!proceed) return;
            }

            EditorUtility.DisplayProgressBar("BIT - Setup Completo", "Creando carpetas...", 0.05f);
            CreateFolders();

            EditorUtility.DisplayProgressBar("BIT - Setup Completo", "Creando PlayerStatsSO...", 0.15f);
            var statsAsset = CreateOrLoadPlayerStats();

            EditorUtility.DisplayProgressBar("BIT - Setup Completo", "Configurando WaveManager...", 0.35f);
            SetupWaveManager(statsAsset);

            EditorUtility.DisplayProgressBar("BIT - Setup Completo", "Creando Canvas y UI...", 0.55f);
            var uiRefs = CreateCanvas();

            EditorUtility.DisplayProgressBar("BIT - Setup Completo", "Creando UIManager...", 0.80f);
            SetupUIManager(statsAsset, uiRefs);

            EditorUtility.DisplayProgressBar("BIT - Setup Completo", "Guardando escena...", 0.95f);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene()
            );
            AssetDatabase.SaveAssets();

            EditorUtility.ClearProgressBar();

            EditorUtility.DisplayDialog("BIT - Setup Completo",
                "¡SETUP COMPLETADO!\n\n" +
                "Lo que se ha creado:\n" +
                "✓ PlayerStats (ScriptableObject)\n" +
                "✓ WaveManager (con prefabs conectados)\n" +
                "✓ Canvas con HUD (ronda, enemigos, score, vida)\n" +
                "✓ Panel de mensaje de oleada\n" +
                "✓ Panel de Game Over\n" +
                "✓ UIManager (conectado a todo)\n\n" +
                "GUARDA LA ESCENA (Ctrl+S) y pulsa PLAY.",
                "OK");
        }

        // ====================================================================
        // CARPETAS
        // ====================================================================

        static void CreateFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Project/SO_Data"))
                AssetDatabase.CreateFolder("Assets/_Project", "SO_Data");

            if (!AssetDatabase.IsValidFolder("Assets/_Project/SO_Data/Stats"))
                AssetDatabase.CreateFolder("Assets/_Project/SO_Data", "Stats");

            if (!AssetDatabase.IsValidFolder(PREFABS_PATH + "/Enemies"))
                AssetDatabase.CreateFolder(PREFABS_PATH, "Enemies");
        }

        // ====================================================================
        // PLAYERSTATSSО
        // ====================================================================

        static BIT.Data.PlayerStatsSO CreateOrLoadPlayerStats()
        {
            var existing = AssetDatabase.LoadAssetAtPath<BIT.Data.PlayerStatsSO>(SO_ASSET_PATH);
            if (existing != null)
            {
                Debug.Log("[BITSetup] PlayerStats ya existía: " + SO_ASSET_PATH);
                return existing;
            }

            var so = ScriptableObject.CreateInstance<BIT.Data.PlayerStatsSO>();
            AssetDatabase.CreateAsset(so, SO_ASSET_PATH);
            AssetDatabase.SaveAssets();
            Debug.Log("[BITSetup] PlayerStats creado en: " + SO_ASSET_PATH);
            return so;
        }

        // ====================================================================
        // WAVEMANAGER
        // ====================================================================

        static void SetupWaveManager(BIT.Data.PlayerStatsSO stats)
        {
            // Buscar o crear el objeto WaveManager en la escena
            var existing = GameObject.Find("WaveManager");
            if (existing != null && existing.GetComponent<BIT.Core.WaveManager>() != null)
            {
                Debug.Log("[BITSetup] WaveManager ya existe en la escena, actualizando referencias...");
                WireWaveManager(existing.GetComponent<BIT.Core.WaveManager>(), stats);
                return;
            }

            // Crear nuevo WaveManager
            GameObject go = new GameObject("WaveManager");
            var wm = go.AddComponent<BIT.Core.WaveManager>();
            WireWaveManager(wm, stats);
            Debug.Log("[BITSetup] WaveManager creado en la escena");
        }

        static void WireWaveManager(BIT.Core.WaveManager wm, BIT.Data.PlayerStatsSO stats)
        {
            var so = new SerializedObject(wm);

            // Conectar prefabs
            var cyclope  = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFABS_PATH}/Enemies/Enemy_Cyclope.prefab");
            var dragon   = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFABS_PATH}/Enemies/Enemy_Dragon.prefab");
            var skeleton = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFABS_PATH}/Enemies/Enemy_Skeleton.prefab");

            if (cyclope  != null) so.FindProperty("_basicEnemyPrefab").objectReferenceValue = cyclope;
            if (dragon   != null) so.FindProperty("_fastEnemyPrefab").objectReferenceValue  = dragon;
            if (skeleton != null) so.FindProperty("_tankEnemyPrefab").objectReferenceValue  = skeleton;

            // Conectar PlayerStats
            so.FindProperty("_playerStats").objectReferenceValue = stats;

            so.ApplyModifiedProperties();

            if (cyclope == null)
                Debug.LogWarning("[BITSetup] No se encontró Enemy_Cyclope.prefab. Ejecuta primero 'Setup Ninja Adventure Scene'.");
            else
                Debug.Log("[BITSetup] WaveManager conectado: Cyclope=" + (cyclope != null) + " Dragon=" + (dragon != null) + " Skeleton=" + (skeleton != null));
        }

        // ====================================================================
        // CANVAS Y UI
        // ====================================================================

        struct UIReferences
        {
            public Slider      healthSlider;
            public Image       healthFillImage;
            public TMP_Text    scoreText;
            public TMP_Text    waveText;
            public TMP_Text    enemyCountText;
            public GameObject  waveMessagePanel;
            public TMP_Text    waveMessageText;
            public GameObject  gameOverPanel;
            public TMP_Text    finalScoreText;
        }

        static UIReferences CreateCanvas()
        {
            UIReferences refs = new UIReferences();

            // Si ya existe un Canvas de UIManager, borrarlo primero para evitar duplicados
            var oldCanvas = GameObject.Find("Canvas_BIT_UI");
            if (oldCanvas != null)
                GameObject.DestroyImmediate(oldCanvas);

            // ── CANVAS ──────────────────────────────────────────────────────
            GameObject canvasGO = new GameObject("Canvas_BIT_UI");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGO.AddComponent<GraphicRaycaster>();

            // ── HUD (panel transparente) ─────────────────────────────────────
            GameObject hud = CreatePanel("HUD", canvasGO.transform, new Color(0, 0, 0, 0));
            StretchFull(hud.GetComponent<RectTransform>());

            // ── TEXTO RONDA ─────────────────────────────────────────────────
            refs.waveText = CreateTMPText(
                "TextRonda", hud.transform,
                "Ronda 1",
                36, TextAlignmentOptions.Center, Color.white,
                new Vector2(0.3f, 1f), new Vector2(0.7f, 1f),
                new Vector2(0, -50), new Vector2(400, 50)
            );

            // ── TEXTO ENEMIGOS ───────────────────────────────────────────────
            refs.enemyCountText = CreateTMPText(
                "TextEnemigos", hud.transform,
                "Enemigos: 0",
                26, TextAlignmentOptions.Center, new Color(1f, 0.9f, 0.4f),
                new Vector2(0.3f, 1f), new Vector2(0.7f, 1f),
                new Vector2(0, -100), new Vector2(400, 40)
            );

            // ── TEXTO SCORE ──────────────────────────────────────────────────
            refs.scoreText = CreateTMPText(
                "TextScore", hud.transform,
                "Score: 0",
                30, TextAlignmentOptions.Right, Color.white,
                new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-20, -40), new Vector2(300, 50)
            );

            // ── BARRA DE VIDA ────────────────────────────────────────────────
            refs.healthSlider = CreateHealthSlider(hud.transform, out refs.healthFillImage);

            // ── PANEL MENSAJE DE OLEADA (desactivado por defecto) ────────────
            refs.waveMessagePanel = CreatePanel("PanelMensajeOleada", canvasGO.transform, new Color(0, 0, 0, 0.6f));
            var msgRect = refs.waveMessagePanel.GetComponent<RectTransform>();
            msgRect.anchorMin = new Vector2(0.5f, 0.5f);
            msgRect.anchorMax = new Vector2(0.5f, 0.5f);
            msgRect.pivot     = new Vector2(0.5f, 0.5f);
            msgRect.anchoredPosition = new Vector2(0, 80);
            msgRect.sizeDelta = new Vector2(700, 140);

            refs.waveMessageText = CreateTMPText(
                "TextMensaje", refs.waveMessagePanel.transform,
                "¡RONDA 1 SUPERADA!",
                52, TextAlignmentOptions.Center, Color.yellow,
                new Vector2(0, 0), new Vector2(1, 1),
                Vector2.zero, Vector2.zero
            );
            StretchFull(refs.waveMessageText.GetComponent<RectTransform>());

            refs.waveMessagePanel.SetActive(false);

            // ── PANEL GAME OVER (desactivado por defecto) ────────────────────
            refs.gameOverPanel = CreatePanel("PanelGameOver", canvasGO.transform, new Color(0, 0, 0, 0.85f));
            StretchFull(refs.gameOverPanel.GetComponent<RectTransform>());

            // Título GAME OVER
            var goTitle = CreateTMPText(
                "TextGameOver", refs.gameOverPanel.transform,
                "GAME OVER",
                80, TextAlignmentOptions.Center, new Color(0.9f, 0.2f, 0.2f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, 120), new Vector2(800, 100)
            );

            // Puntuación final
            refs.finalScoreText = CreateTMPText(
                "TextPuntuacionFinal", refs.gameOverPanel.transform,
                "Puntuación: 0",
                42, TextAlignmentOptions.Center, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, 20), new Vector2(600, 60)
            );

            // Instrucción reiniciar
            CreateTMPText(
                "TextReiniciar", refs.gameOverPanel.transform,
                "Pulsa R para reiniciar",
                32, TextAlignmentOptions.Center, new Color(0.7f, 0.7f, 0.7f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, -60), new Vector2(500, 50)
            );

            refs.gameOverPanel.SetActive(false);

            Debug.Log("[BITSetup] Canvas creado con HUD, PanelMensajeOleada y PanelGameOver");
            return refs;
        }

        // ── Helpers de creación de UI ────────────────────────────────────────

        static GameObject CreatePanel(string name, Transform parent, Color color)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            var img = go.AddComponent<Image>();
            img.color = color;
            return go;
        }

        static TMP_Text CreateTMPText(
            string name, Transform parent,
            string text, float fontSize,
            TextAlignmentOptions alignment, Color color,
            Vector2 anchorMin, Vector2 anchorMax,
            Vector2 anchoredPos, Vector2 sizeDelta)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = color;

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = new Vector2(
                (anchorMin.x + anchorMax.x) * 0.5f,
                (anchorMin.y + anchorMax.y) * 0.5f
            );
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;

            return tmp;
        }

        static Slider CreateHealthSlider(Transform parent, out Image fillImage)
        {
            // Contenedor
            GameObject sliderGO = new GameObject("BarraVida");
            sliderGO.transform.SetParent(parent, false);
            var slider = sliderGO.AddComponent<Slider>();

            var sliderRT = sliderGO.GetComponent<RectTransform>();
            sliderRT.anchorMin = new Vector2(0, 1);
            sliderRT.anchorMax = new Vector2(0, 1);
            sliderRT.pivot     = new Vector2(0, 1);
            sliderRT.anchoredPosition = new Vector2(20, -20);
            sliderRT.sizeDelta = new Vector2(280, 30);

            // Background
            GameObject bg = new GameObject("Background");
            bg.transform.SetParent(sliderGO.transform, false);
            var bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            StretchFull(bg.GetComponent<RectTransform>());

            // Fill Area
            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(sliderGO.transform, false);
            var fillAreaRT = fillArea.AddComponent<RectTransform>();
            fillAreaRT.anchorMin = Vector2.zero;
            fillAreaRT.anchorMax = Vector2.one;
            fillAreaRT.offsetMin = new Vector2(5, 5);
            fillAreaRT.offsetMax = new Vector2(-5, -5);

            // Fill
            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            fillImage = fill.AddComponent<Image>();
            fillImage.color = new Color(0.9f, 0.15f, 0.15f, 1f);
            var fillRT = fill.GetComponent<RectTransform>();
            fillRT.anchorMin = Vector2.zero;
            fillRT.anchorMax = Vector2.one;
            fillRT.sizeDelta = Vector2.zero;

            // Configurar el slider
            slider.fillRect = fillRT;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value    = 1f;
            slider.wholeNumbers = false;
            slider.direction = Slider.Direction.LeftToRight;

            // Etiqueta "HP"
            var label = CreateTMPText(
                "LabelHP", sliderGO.transform,
                "HP", 18, TextAlignmentOptions.Left, Color.white,
                new Vector2(0, 0.5f), new Vector2(0, 0.5f),
                new Vector2(-30, 0), new Vector2(40, 30)
            );

            return slider;
        }

        static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        // ====================================================================
        // UIMANAGER
        // ====================================================================

        static void SetupUIManager(BIT.Data.PlayerStatsSO stats, UIReferences ui)
        {
            // Buscar o crear objeto UIManager
            var existing = GameObject.Find("UIManager");
            GameObject uiManagerGO;

            if (existing != null && existing.GetComponent<BIT.UI.UIManager>() != null)
            {
                uiManagerGO = existing;
                Debug.Log("[BITSetup] UIManager ya existía, actualizando referencias...");
            }
            else
            {
                uiManagerGO = new GameObject("UIManager");
                uiManagerGO.AddComponent<BIT.UI.UIManager>();
                Debug.Log("[BITSetup] UIManager creado");
            }

            var uiManager = uiManagerGO.GetComponent<BIT.UI.UIManager>();
            var so = new SerializedObject(uiManager);

            // Estadísticas del jugador
            so.FindProperty("_playerStats").objectReferenceValue = stats;

            // UI de vida
            so.FindProperty("_healthSlider").objectReferenceValue = ui.healthSlider;
            so.FindProperty("_healthFillImage").objectReferenceValue = ui.healthFillImage;

            // UI de puntuación
            so.FindProperty("_scoreText").objectReferenceValue = ui.scoreText;

            // UI de rondas
            so.FindProperty("_waveText").objectReferenceValue = ui.waveText;
            so.FindProperty("_enemyCountText").objectReferenceValue = ui.enemyCountText;
            so.FindProperty("_waveMessagePanel").objectReferenceValue = ui.waveMessagePanel;
            so.FindProperty("_waveMessageText").objectReferenceValue = ui.waveMessageText;

            // Pantalla Game Over
            so.FindProperty("_gameOverPanel").objectReferenceValue = ui.gameOverPanel;
            so.FindProperty("_finalScoreText").objectReferenceValue = ui.finalScoreText;

            so.ApplyModifiedProperties();

            Debug.Log("[BITSetup] UIManager configurado con todas las referencias");
        }
    }
}
#endif
