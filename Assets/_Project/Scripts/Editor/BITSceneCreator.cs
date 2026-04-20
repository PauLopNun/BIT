#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using BIT.Core;
using BIT.Events;
using BIT.UI;
using BIT.Audio;

// ============================================================================
// BITSCENECREATOR.CS — Crea las escenas MainMenu y CharacterSelect automáticamente
// ============================================================================
// USO: Menu → BIT → 3. Crear Escenas (MainMenu + CharacterSelect)
//
// Crea:
//   Assets/_Project/Scenes/MainMenu.unity        — menú principal
//   Assets/_Project/Scenes/CharacterSelect.unity — selector de personaje
//   Actualiza Build Settings (orden: MainMenu → CharacterSelect → gamesetupscene)
//   Conecta los clips de audio del pack Ninja Adventure al AudioManager
// ============================================================================

namespace BIT.Editor
{
    public static class BITSceneCreator
    {
        private const string SCENES_PATH = "Assets/_Project/Scenes";
        private const string AUDIO_ROOT  = "Assets/_Project/Sprites/Ninja Adventure/Audio";

        // ====================================================================
        // MENÚ
        // ====================================================================

        [MenuItem("BIT/3. Crear Escenas (MainMenu + CharacterSelect)")]
        public static void CreateScenes()
        {
            // Guardar escena actual para no perder trabajo
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

            EditorUtility.DisplayProgressBar("BIT – Escenas", "Creando MainMenu.unity…", 0.1f);
            CreateMainMenuScene();

            EditorUtility.DisplayProgressBar("BIT – Escenas", "Creando CharacterSelect.unity…", 0.4f);
            CreateCharacterSelectScene();

            EditorUtility.DisplayProgressBar("BIT – Escenas", "Actualizando Build Settings…", 0.7f);
            UpdateBuildSettings();

            EditorUtility.DisplayProgressBar("BIT – Escenas", "Conectando audio…", 0.85f);
            WireAudio();

            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("BIT – Escenas creadas",
                "¡Escenas creadas correctamente!\n\n" +
                "Build Settings:\n" +
                "  0 → MainMenu\n" +
                "  1 → CharacterSelect\n" +
                "  2 → gamesetupscene\n\n" +
                "Audio del pack Ninja Adventure conectado al AudioManager.\n\n" +
                "FLUJO: MainMenu → CharacterSelect → Juego",
                "OK");
        }

        // ====================================================================
        // ESCENA: MAIN MENU
        // ====================================================================

        static void CreateMainMenuScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // ── Cámara ────────────────────────────────────────────────────
            var camGO = new GameObject("Main Camera");
            var cam = camGO.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.04f, 0.03f, 0.06f);
            camGO.tag = "MainCamera";
            camGO.transform.position = new Vector3(0, 0, -10);

            // ── Managers ──────────────────────────────────────────────────
            new GameObject("GameManager").AddComponent<GameManager>();
            new GameObject("AudioManager").AddComponent<AudioManager>();
            new GameObject("SaveSystem").AddComponent<BIT.Core.SaveSystem>();

            // ── EventSystem ───────────────────────────────────────────────
            var evGO = new GameObject("EventSystem");
            evGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            evGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

            // ── Canvas ────────────────────────────────────────────────────
            var canvasGO = new GameObject("Canvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGO.AddComponent<GraphicRaycaster>();

            // ── Fondo ─────────────────────────────────────────────────────
            var bgGO = new GameObject("Background");
            bgGO.transform.SetParent(canvasGO.transform, false);
            bgGO.AddComponent<Image>().color = new Color(0.04f, 0.03f, 0.06f);
            StretchRT(bgGO.GetComponent<RectTransform>());

            // ── MainMenuUI manager ────────────────────────────────────────
            var menuMgrGO = new GameObject("MainMenuUI");
            menuMgrGO.transform.SetParent(canvasGO.transform, false);
            var mainMenuUI = menuMgrGO.AddComponent<BIT.UI.MainMenuUI>();

            // ================================================================
            // PANEL PRINCIPAL
            // ================================================================
            var mainPanel = new GameObject("MainPanel");
            mainPanel.transform.SetParent(canvasGO.transform, false);
            mainPanel.AddComponent<Image>().color = new Color(0, 0, 0, 0);
            StretchRT(mainPanel.GetComponent<RectTransform>());

            // Título
            var titleGO = new GameObject("GameTitle");
            titleGO.transform.SetParent(mainPanel.transform, false);
            var titleTMP = titleGO.AddComponent<TextMeshProUGUI>();
            titleTMP.text = "BIT";
            titleTMP.fontSize = 120;
            titleTMP.fontStyle = FontStyles.Bold;
            titleTMP.alignment = TextAlignmentOptions.Center;
            titleTMP.color = new Color(1f, 0.85f, 0.2f);
            SetAnchor(titleGO, 0.3f, 0.65f, 0.7f, 0.95f);

            // Subtítulo
            var subGO = new GameObject("Subtitle");
            subGO.transform.SetParent(mainPanel.transform, false);
            var subTMP = subGO.AddComponent<TextMeshProUGUI>();
            subTMP.text = "Acción y supervivencia Ninja";
            subTMP.fontSize = 24;
            subTMP.alignment = TextAlignmentOptions.Center;
            subTMP.color = new Color(0.65f, 0.65f, 0.65f);
            SetAnchor(subGO, 0.2f, 0.60f, 0.8f, 0.68f);

            // Campo de nombre del jugador
            var nameInputGO = new GameObject("NameInput");
            nameInputGO.transform.SetParent(mainPanel.transform, false);
            nameInputGO.AddComponent<Image>().color = new Color(0.12f, 0.1f, 0.18f);
            SetAnchor(nameInputGO, 0.3f, 0.57f, 0.7f, 0.63f);
            var nameInput = nameInputGO.AddComponent<TMPro.TMP_InputField>();
            var nameTextAreaGO = new GameObject("Text Area");
            nameTextAreaGO.transform.SetParent(nameInputGO.transform, false);
            var nameTextAreaRT = nameTextAreaGO.AddComponent<RectTransform>();
            StretchRT(nameTextAreaRT);
            var nameTMP = nameTextAreaGO.AddComponent<TextMeshProUGUI>();
            nameTMP.fontSize = 22;
            nameTMP.alignment = TextAlignmentOptions.Center;
            nameTMP.color = Color.white;
            nameInput.textComponent = nameTMP;
            var namePlaceholderGO = new GameObject("Placeholder");
            namePlaceholderGO.transform.SetParent(nameInputGO.transform, false);
            var namePlaceholderRT = namePlaceholderGO.AddComponent<RectTransform>();
            StretchRT(namePlaceholderRT);
            var placeholderTMP = namePlaceholderGO.AddComponent<TextMeshProUGUI>();
            placeholderTMP.text = "Introduce tu nombre…";
            placeholderTMP.fontSize = 22;
            placeholderTMP.alignment = TextAlignmentOptions.Center;
            placeholderTMP.color = new Color(0.5f, 0.5f, 0.5f);
            nameInput.placeholder = placeholderTMP;

            // Botón JUGAR → CharacterSelect
            var playBtn = MakeButton(mainPanel, "JUGAR",
                new Color(0.15f, 0.65f, 0.25f), Color.white, 32,
                new Vector2(0.35f, 0.45f), new Vector2(0.65f, 0.57f));
            playBtn.GetComponent<Button>().onClick.AddListener(() =>
                SceneManager.LoadScene("CharacterSelect"));

            // Botón RANKING — se conecta vía MainMenuUI
            var rankBtn = MakeButton(mainPanel, "RANKING",
                new Color(0.15f, 0.30f, 0.55f), new Color(0.9f, 0.9f, 1f), 28,
                new Vector2(0.35f, 0.32f), new Vector2(0.65f, 0.43f));

            // Botón SALIR
            var quitBtn = MakeButton(mainPanel, "SALIR",
                new Color(0.25f, 0.10f, 0.10f), new Color(1f, 0.6f, 0.6f), 24,
                new Vector2(0.40f, 0.20f), new Vector2(0.60f, 0.30f));
            quitBtn.GetComponent<Button>().onClick.AddListener(() => Application.Quit());

            // High score
            var hsGO = new GameObject("HighScore");
            hsGO.transform.SetParent(mainPanel.transform, false);
            var hsTMP = hsGO.AddComponent<TextMeshProUGUI>();
            hsTMP.text = "High Score: 0";
            hsTMP.fontSize = 20;
            hsTMP.alignment = TextAlignmentOptions.Center;
            hsTMP.color = new Color(1f, 0.85f, 0.2f);
            SetAnchor(hsGO, 0.2f, 0.12f, 0.8f, 0.20f);

            // Versión
            var verGO = new GameObject("Version");
            verGO.transform.SetParent(mainPanel.transform, false);
            var verTMP = verGO.AddComponent<TextMeshProUGUI>();
            verTMP.text = "v1.0 — Ninja Adventure Pack (CC0)";
            verTMP.fontSize = 16;
            verTMP.alignment = TextAlignmentOptions.Right;
            verTMP.color = new Color(0.35f, 0.35f, 0.35f);
            SetAnchor(verGO, 0.5f, 0.01f, 0.99f, 0.07f);

            // ================================================================
            // PANEL RANKING (con RankingUI)
            // ================================================================
            var rankingPanel = new GameObject("RankingPanel");
            rankingPanel.transform.SetParent(canvasGO.transform, false);
            rankingPanel.AddComponent<Image>().color = new Color(0.04f, 0.03f, 0.08f, 0.97f);
            StretchRT(rankingPanel.GetComponent<RectTransform>());
            rankingPanel.SetActive(false);

            var rankUI = rankingPanel.AddComponent<BIT.UI.RankingUI>();

            var rankTitleGO = new GameObject("RankingTitle");
            rankTitleGO.transform.SetParent(rankingPanel.transform, false);
            var rankTitleTMP = rankTitleGO.AddComponent<TextMeshProUGUI>();
            rankTitleTMP.text = "RANKING";
            rankTitleTMP.fontSize = 60;
            rankTitleTMP.fontStyle = FontStyles.Bold;
            rankTitleTMP.alignment = TextAlignmentOptions.Center;
            rankTitleTMP.color = new Color(1f, 0.85f, 0.2f);
            SetAnchor(rankTitleGO, 0.1f, 0.82f, 0.9f, 0.97f);

            var noEntriesGO = new GameObject("NoEntries");
            noEntriesGO.transform.SetParent(rankingPanel.transform, false);
            var noEntriesTMP = noEntriesGO.AddComponent<TextMeshProUGUI>();
            noEntriesTMP.text = "Aún no hay puntuaciones. ¡Sé el primero!";
            noEntriesTMP.fontSize = 28;
            noEntriesTMP.alignment = TextAlignmentOptions.Center;
            noEntriesTMP.color = new Color(0.6f, 0.6f, 0.6f);
            SetAnchor(noEntriesGO, 0.1f, 0.45f, 0.9f, 0.58f);

            var listContainerGO = new GameObject("RankingList");
            listContainerGO.transform.SetParent(rankingPanel.transform, false);
            listContainerGO.AddComponent<Image>().color = new Color(0, 0, 0, 0);
            var listRT = listContainerGO.GetComponent<RectTransform>();
            listRT.anchorMin = new Vector2(0.15f, 0.12f);
            listRT.anchorMax = new Vector2(0.85f, 0.80f);
            listRT.offsetMin = listRT.offsetMax = Vector2.zero;
            var vertLayout = listContainerGO.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
            vertLayout.spacing = 6;
            vertLayout.childForceExpandWidth = true;
            vertLayout.childForceExpandHeight = false;

            var rankBackBtn = MakeButton(rankingPanel, "← VOLVER",
                new Color(0.2f, 0.2f, 0.3f), Color.white, 24,
                new Vector2(0.35f, 0.02f), new Vector2(0.65f, 0.10f));
            rankBackBtn.GetComponent<Button>().onClick.AddListener(() =>
            {
                rankingPanel.SetActive(false);
                mainPanel.SetActive(true);
            });

            // ── Wire MainMenuUI references ────────────────────────────────
            var soMenu = new SerializedObject(mainMenuUI);
            soMenu.FindProperty("_playButton").objectReferenceValue      = playBtn.GetComponent<Button>();
            soMenu.FindProperty("_rankingButton").objectReferenceValue   = rankBtn.GetComponent<Button>();
            soMenu.FindProperty("_quitButton").objectReferenceValue      = quitBtn.GetComponent<Button>();
            soMenu.FindProperty("_playerNameInput").objectReferenceValue = nameInput;
            soMenu.FindProperty("_mainPanel").objectReferenceValue       = mainPanel;
            soMenu.FindProperty("_rankingPanel").objectReferenceValue    = rankingPanel;
            soMenu.FindProperty("_highScoreText").objectReferenceValue   = hsTMP;
            soMenu.FindProperty("_versionText").objectReferenceValue     = verTMP;
            soMenu.ApplyModifiedProperties();

            // ── Wire RankingUI references ─────────────────────────────────
            var rankingEntryPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Project/Prefabs/UI/RankingEntry.prefab");
            var soRank = new SerializedObject(rankUI);
            soRank.FindProperty("_rankingListContainer").objectReferenceValue = listContainerGO.transform;
            soRank.FindProperty("_rankingPanel").objectReferenceValue         = rankingPanel;
            soRank.FindProperty("_rankingTitleText").objectReferenceValue     = rankTitleTMP;
            soRank.FindProperty("_noEntriesText").objectReferenceValue        = noEntriesTMP;
            if (rankingEntryPrefab != null)
                soRank.FindProperty("_rankingEntryPrefab").objectReferenceValue = rankingEntryPrefab;
            soRank.ApplyModifiedProperties();

            // Guardar escena
            string path = SCENES_PATH + "/MainMenu.unity";
            EditorSceneManager.SaveScene(scene, path);
            Debug.Log($"[BITSceneCreator] Escena MainMenu guardada en {path}");
        }

        // ====================================================================
        // ESCENA: CHARACTER SELECT
        // ====================================================================

        static void CreateCharacterSelectScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Cámara
            var camGO = new GameObject("Main Camera");
            var cam = camGO.AddComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.05f, 0.04f, 0.08f);
            camGO.tag = "MainCamera";
            camGO.transform.position = new Vector3(0, 0, -10);

            // CharacterSelectManager (persiste hasta que carga la escena de juego)
            var csmGO = new GameObject("CharacterSelectManager");
            csmGO.AddComponent<CharacterSelectManager>();

            // EventSystem
            var evGO = new GameObject("EventSystem");
            evGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            evGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

            // CharacterSelectUI (crea toda la UI en runtime)
            var uiGO = new GameObject("CharacterSelectUI");
            uiGO.AddComponent<CharacterSelectUI>();

            string path = SCENES_PATH + "/CharacterSelect.unity";
            EditorSceneManager.SaveScene(scene, path);
            Debug.Log($"[BITSceneCreator] Escena CharacterSelect guardada en {path}");
        }

        // ====================================================================
        // BUILD SETTINGS
        // ====================================================================

        static void UpdateBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>
            {
                new(SCENES_PATH + "/MainMenu.unity",       true),
                new(SCENES_PATH + "/CharacterSelect.unity",true),
                new(SCENES_PATH + "/gamesetupscene.unity", true),
            };

            // Añadir cualquier otra escena que ya esté en Build Settings y no esté en la lista
            foreach (var existing in EditorBuildSettings.scenes)
            {
                bool already = false;
                foreach (var s in scenes)
                    if (s.path == existing.path) { already = true; break; }
                if (!already)
                    scenes.Add(new EditorBuildSettingsScene(existing.path, existing.enabled));
            }

            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log("[BITSceneCreator] Build Settings actualizados: 3 escenas principales.");
        }

        // ====================================================================
        // CONECTAR AUDIO
        // ====================================================================

        [MenuItem("BIT/4. Conectar Audio (Ninja Adventure)")]
        public static void WireAudio()
        {
            // Buscar AudioManager en CUALQUIER escena abierta
            var audioMgr = Object.FindFirstObjectByType<AudioManager>();
            if (audioMgr == null)
            {
                // Intentar cargar desde la escena de juego
                string gamePath = SCENES_PATH + "/gamesetupscene.unity";
                if (!System.IO.File.Exists(gamePath))
                {
                    Debug.LogWarning("[BITSceneCreator] AudioManager no encontrado. Abre la escena de juego primero.");
                    return;
                }
            }

            // Rutas de audio (Ninja Adventure pack)
            string sfx = AUDIO_ROOT + "/Sounds";
            string mus = AUDIO_ROOT + "/Musics";
            string jng = AUDIO_ROOT + "/Jingles";

            var clips = new Dictionary<string, string>
            {
                { "_playerAttackSound",  sfx + "/Whoosh & Slash/Slash.wav"              },
                { "_playerHurtSound",    sfx + "/Hit & Impact/Hit1.wav"                 },
                { "_playerDeathSound",   sfx + "/Hit & Impact/Impact.wav"               },
                { "_footstepSound",      sfx + "/Jump & Bounce/Bounce.wav"              },
                { "_pickupSound",        sfx + "/Bonus/Bonus.wav"                       },
                { "_coinSound",          sfx + "/Bonus/Coin.wav"                        },
                { "_pushSound",          sfx + "/Hit & Impact/Impact2.wav"              },
                { "_buttonClickSound",   sfx + "/Menu/Accept.wav"                       },
                { "_menuSound",          sfx + "/Menu/Accept2.wav"                      },
                { "_backgroundMusic",    mus + "/21 - Dungeon.ogg"                      },
                { "_gameOverMusic",      jng + "/GameOver.wav"                          },
            };

            int wired = 0;
            if (audioMgr != null)
            {
                var so = new SerializedObject(audioMgr);
                foreach (var kv in clips)
                {
                    var prop = so.FindProperty(kv.Key);
                    if (prop == null) continue;

                    var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(kv.Value);
                    if (clip == null)
                    {
                        Debug.LogWarning($"[BITSceneCreator] Audio no encontrado: {kv.Value}");
                        continue;
                    }

                    prop.objectReferenceValue = clip;
                    wired++;
                }
                // Conectar GameEventSO si existen los assets
                var gameEvents = new Dictionary<string, string>
                {
                    { "_onPlayerDamageEvent", "Assets/_Project/SO_Data/Events/OnPlayerDamage.asset" },
                    { "_onPlayerAttackEvent", "Assets/_Project/SO_Data/Events/OnPlayerAttack.asset" },
                    { "_onPickupEvent",       "Assets/_Project/SO_Data/Events/OnPickup.asset"       },
                };
                foreach (var kv in gameEvents)
                {
                    var prop = so.FindProperty(kv.Key);
                    if (prop == null || prop.objectReferenceValue != null) continue;
                    var asset = AssetDatabase.LoadAssetAtPath<BIT.Events.GameEventSO>(kv.Value);
                    if (asset != null) { prop.objectReferenceValue = asset; wired++; }
                }

                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(audioMgr);
                Debug.Log($"[BITSceneCreator] {wired} clips/eventos conectados al AudioManager.");
            }
            else
            {
                Debug.LogWarning("[BITSceneCreator] AudioManager no está en la escena activa. " +
                    "Abre la escena de juego y ejecuta 'BIT → 4. Conectar Audio' de nuevo.");
            }
        }

        // ====================================================================
        // HELPERS DE UI
        // ====================================================================

        static void StretchRT(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        static void SetAnchor(GameObject go, float xMin, float yMin, float xMax, float yMax)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(xMin, yMin);
            rt.anchorMax = new Vector2(xMax, yMax);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        static GameObject MakeButton(GameObject parent, string label, Color bgColor,
            Color textColor, float fontSize, Vector2 aMin, Vector2 aMax)
        {
            var go = new GameObject(label + "_Button");
            go.transform.SetParent(parent.transform, false);

            var img = go.AddComponent<Image>();
            img.color = bgColor;

            go.AddComponent<Button>();

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = aMin;
            rt.anchorMax = aMax;
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            var textGO = new GameObject("Label");
            textGO.transform.SetParent(go.transform, false);
            var tmp = textGO.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = fontSize;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = textColor;
            StretchRT(textGO.GetComponent<RectTransform>());

            return go;
        }
    }
}
#endif
