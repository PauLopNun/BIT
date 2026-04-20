#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BIT.Core;
using BIT.Events;
using BIT.Interactables;
using BIT.Player;

// ============================================================================
// BITFULLSETUP.CS — UN SOLO CLIC para tener el juego funcionando al 100%
// ============================================================================
// USO: Menu → BIT → 0. SETUP COMPLETO (un clic)
//
// Ejecuta en orden:
//   1. Corta los tilesets PNG en sprites 16×16
//   2. Configura managers y el mapa dungeon en gamesetupscene
//   3. Crea MainMenu.unity y CharacterSelect.unity
//   4. Conecta clips de audio al AudioManager
//   5. Crea prefabs de Hazards (DamageHazard, SlowHazard, PoisonHazard, ScoreDrainHazard)
//   6. Crea el prefab de entrada del ranking (RankingEntry)
//   7. Corrige el nombre de escena en GameManager
//   8. Conecta sprites FX al VFXManager si está en escena
// ============================================================================

namespace BIT.Editor
{
    public static class BITFullSetup
    {
        private const string PREFABS    = "Assets/_Project/Prefabs";
        private const string AUDIO_ROOT = "Assets/_Project/Sprites/Ninja Adventure/Audio";
        private const string FX_ROOT    = "Assets/_Project/Sprites/Ninja Adventure/Actor/FX";
        private const string SCENES_PATH = "Assets/_Project/Scenes";

        // ====================================================================
        // MENÚ PRINCIPAL
        // ====================================================================

        [MenuItem("BIT/0. SETUP COMPLETO (un clic)", priority = 0)]
        public static void RunFullSetup()
        {
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

            int step = 0;
            int total = 11;

            Progress(ref step, total, "Cortando tilesets PNG (16×16)…");
            BITAutoSetup.SetupTilesets();

            Progress(ref step, total, "Configurando escena de juego…");
            BITAutoSetup.SetupScene();
            // gamesetupscene activa aquí — AudioManager y VFXManager presentes

            Progress(ref step, total, "Creando menú de pausa en la escena de juego…");
            CreatePauseMenuInScene();

            Progress(ref step, total, "Conectando audio y VFX (mientras gamesetupscene está activa)…");
            BITSceneCreator.WireAudio();
            WireVFXSprites();
            EditorSceneManager.SaveOpenScenes();

            Progress(ref step, total, "Creando prefabs de Hazards y Ranking…");
            CreateHazardPrefabs();
            CreateRankingEntryPrefab();

            Progress(ref step, total, "Creando GameEventSO assets…");
            CreateGameEvents();

            Progress(ref step, total, "Creando escenas MainMenu y CharacterSelect…");
            BITSceneCreator.CreateScenes();

            Progress(ref step, total, "Arreglando prefabs (OrbitWeapon, Pickups, Shuriken)…");
            FixPrefabs();

            Progress(ref step, total, "Finalizando…");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.ClearProgressBar();

            EditorUtility.DisplayDialog("BIT – Setup Completo",
                "¡El juego está configurado al 100%!\n\n" +
                "✓ Tilesets cortados en sprites 16×16\n" +
                "✓ Escena de juego configurada (gamesetupscene)\n" +
                "✓ Menú de pausa (Escape) añadido a la escena\n" +
                "✓ MainMenu.unity con Ranking funcional y CharacterSelect.unity creadas\n" +
                "✓ Build Settings: MainMenu(0) → CharacterSelect(1) → gamesetupscene(2)\n" +
                "✓ Prefabs de hazards y SpeedBoost creados\n" +
                "✓ RankingEntry.prefab creado y conectado\n" +
                "✓ GameEventSO assets creados\n" +
                "✓ OrbitWeapon + Shuriken añadidos al Player (req. 2.4 + 2.5)\n" +
                "✓ HealthPickup/ScorePickup/SpeedPickup en prefabs (req. 2.7)\n" +
                "✓ Audio y GameEvents conectados\n" +
                "✓ Sprites FX conectados\n\n" +
                "Pulsa PLAY en MainMenu para el flujo completo:\n" +
                "MainMenu → CharacterSelect → Juego",
                "¡Perfecto!");
        }

        static void Progress(ref int step, int total, string msg)
        {
            step++;
            EditorUtility.DisplayProgressBar("BIT – Setup Completo", msg, (float)step / total);
        }

        // ====================================================================
        // MENÚ DE PAUSA — se añade a gamesetupscene en tiempo de edición
        // ====================================================================

        [MenuItem("BIT/7. Añadir Menú de Pausa (gamesetupscene)", priority = 57)]
        public static void CreatePauseMenuInScene()
        {
            // Si ya existe, no duplicar
            if (Object.FindFirstObjectByType<BIT.UI.PauseMenuUI>() != null)
            {
                Debug.Log("[BITFullSetup] PauseMenuUI ya existe en la escena.");
                return;
            }

            // Canvas dedicado al pause (sortingOrder alto para aparecer encima)
            var canvasGO = new GameObject("PauseMenuCanvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGO.AddComponent<GraphicRaycaster>();

            var pauseUI = canvasGO.AddComponent<BIT.UI.PauseMenuUI>();

            // ── PANEL PRINCIPAL DE PAUSA ──────────────────────────────────
            var pausePanel = MakePausePanel(canvasGO, "PausePanel",
                new Color(0f, 0f, 0f, 0.82f));

            // Título
            MakePauseTMP(pausePanel, "PauseTitle", "PAUSA",
                new Color(1f, 0.85f, 0.2f), 72, FontStyles.Bold,
                new Vector2(0.1f, 0.72f), new Vector2(0.9f, 0.92f));

            // Botones del panel principal
            var resumeBtn   = MakePauseButton(pausePanel, "Reanudar",
                new Color(0.15f, 0.65f, 0.25f), new Vector2(0.3f, 0.58f), new Vector2(0.7f, 0.70f));
            var restartBtn  = MakePauseButton(pausePanel, "Reiniciar",
                new Color(0.20f, 0.35f, 0.65f), new Vector2(0.3f, 0.44f), new Vector2(0.7f, 0.56f));
            var optionsBtn  = MakePauseButton(pausePanel, "Opciones",
                new Color(0.30f, 0.25f, 0.45f), new Vector2(0.3f, 0.30f), new Vector2(0.7f, 0.42f));
            var menuBtn     = MakePauseButton(pausePanel, "Menú Principal",
                new Color(0.45f, 0.15f, 0.15f), new Vector2(0.3f, 0.14f), new Vector2(0.7f, 0.27f));

            pausePanel.SetActive(false);

            // ── PANEL DE OPCIONES ─────────────────────────────────────────
            var optionsPanel = MakePausePanel(canvasGO, "OptionsPanel",
                new Color(0.05f, 0.04f, 0.10f, 0.95f));

            MakePauseTMP(optionsPanel, "OptionsTitle", "OPCIONES",
                new Color(1f, 0.85f, 0.2f), 56, FontStyles.Bold,
                new Vector2(0.1f, 0.78f), new Vector2(0.9f, 0.93f));

            MakePauseTMP(optionsPanel, "MusicLabel", "Música",
                Color.white, 28, FontStyles.Normal,
                new Vector2(0.15f, 0.60f), new Vector2(0.45f, 0.70f));
            var musicSlider = MakeSlider(optionsPanel, "MusicSlider",
                new Vector2(0.15f, 0.50f), new Vector2(0.85f, 0.60f), 0.7f);

            MakePauseTMP(optionsPanel, "SFXLabel", "Efectos",
                Color.white, 28, FontStyles.Normal,
                new Vector2(0.15f, 0.37f), new Vector2(0.45f, 0.47f));
            var sfxSlider = MakeSlider(optionsPanel, "SFXSlider",
                new Vector2(0.15f, 0.27f), new Vector2(0.85f, 0.37f), 1f);

            var backBtn = MakePauseButton(optionsPanel, "← Volver",
                new Color(0.25f, 0.25f, 0.35f), new Vector2(0.3f, 0.10f), new Vector2(0.7f, 0.22f));

            optionsPanel.SetActive(false);

            // ── PANEL DE CONFIRMACIÓN ─────────────────────────────────────
            var confirmPanel = MakePausePanel(canvasGO, "ConfirmPanel",
                new Color(0.04f, 0.03f, 0.08f, 0.97f));

            var confirmTMP = MakePauseTMP(confirmPanel, "ConfirmText",
                "¿Estás seguro?", Color.white, 32, FontStyles.Normal,
                new Vector2(0.1f, 0.55f), new Vector2(0.9f, 0.80f));

            var confirmYes = MakePauseButton(confirmPanel, "Sí",
                new Color(0.15f, 0.65f, 0.25f), new Vector2(0.2f, 0.30f), new Vector2(0.46f, 0.50f));
            var confirmNo  = MakePauseButton(confirmPanel, "No",
                new Color(0.55f, 0.15f, 0.15f), new Vector2(0.54f, 0.30f), new Vector2(0.80f, 0.50f));

            confirmPanel.SetActive(false);

            // ── Wire PauseMenuUI ──────────────────────────────────────────
            var so = new SerializedObject(pauseUI);
            so.FindProperty("_pausePanel").objectReferenceValue         = pausePanel;
            so.FindProperty("_resumeButton").objectReferenceValue       = resumeBtn.GetComponent<Button>();
            so.FindProperty("_restartButton").objectReferenceValue      = restartBtn.GetComponent<Button>();
            so.FindProperty("_optionsButton").objectReferenceValue      = optionsBtn.GetComponent<Button>();
            so.FindProperty("_mainMenuButton").objectReferenceValue     = menuBtn.GetComponent<Button>();
            so.FindProperty("_optionsPanel").objectReferenceValue       = optionsPanel;
            so.FindProperty("_musicVolumeSlider").objectReferenceValue  = musicSlider;
            so.FindProperty("_sfxVolumeSlider").objectReferenceValue    = sfxSlider;
            so.FindProperty("_backFromOptionsButton").objectReferenceValue = backBtn.GetComponent<Button>();
            so.FindProperty("_confirmPanel").objectReferenceValue       = confirmPanel;
            so.FindProperty("_confirmText").objectReferenceValue        = confirmTMP;
            so.FindProperty("_confirmYesButton").objectReferenceValue   = confirmYes.GetComponent<Button>();
            so.FindProperty("_confirmNoButton").objectReferenceValue    = confirmNo.GetComponent<Button>();
            so.ApplyModifiedProperties();

            EditorUtility.SetDirty(canvasGO);
            Debug.Log("[BITFullSetup] Menú de pausa creado y cableado en la escena.");
        }

        // ── Helpers de UI para el menú de pausa ──────────────────────────

        static GameObject MakePausePanel(GameObject parent, string name, Color bgColor)
        {
            var go  = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var img = go.AddComponent<Image>();
            img.color = bgColor;
            var rt  = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return go;
        }

        static TextMeshProUGUI MakePauseTMP(GameObject parent, string name, string text,
            Color color, float size, FontStyles style, Vector2 aMin, Vector2 aMax)
        {
            var go  = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text      = text;
            tmp.fontSize  = size;
            tmp.fontStyle = style;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color     = color;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = aMin; rt.anchorMax = aMax;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return tmp;
        }

        static GameObject MakePauseButton(GameObject parent, string label,
            Color bgColor, Vector2 aMin, Vector2 aMax)
        {
            var go  = new GameObject(label + "_Btn");
            go.transform.SetParent(parent.transform, false);
            go.AddComponent<Image>().color = bgColor;
            go.AddComponent<Button>();
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = aMin; rt.anchorMax = aMax;
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            var txtGO = new GameObject("Label");
            txtGO.transform.SetParent(go.transform, false);
            var tmp       = txtGO.AddComponent<TextMeshProUGUI>();
            tmp.text      = label;
            tmp.fontSize  = 26;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color     = Color.white;
            var trt = txtGO.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = trt.offsetMax = Vector2.zero;
            return go;
        }

        static Slider MakeSlider(GameObject parent, string name,
            Vector2 aMin, Vector2 aMax, float defaultVal)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = aMin; rt.anchorMax = aMax;
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            var bg = new GameObject("Background");
            bg.transform.SetParent(go.transform, false);
            var bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0.1f, 0.1f, 0.15f);
            var bgRT = bg.GetComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;

            var fill = new GameObject("Fill");
            fill.transform.SetParent(go.transform, false);
            var fillImg = fill.AddComponent<Image>();
            fillImg.color = new Color(0.3f, 0.7f, 1f);
            var fillRT = fill.GetComponent<RectTransform>();
            fillRT.anchorMin = Vector2.zero; fillRT.anchorMax = new Vector2(defaultVal, 1f);
            fillRT.offsetMin = fillRT.offsetMax = Vector2.zero;

            var handle = new GameObject("Handle");
            handle.transform.SetParent(go.transform, false);
            handle.AddComponent<Image>().color = Color.white;
            var handleRT = handle.GetComponent<RectTransform>();
            handleRT.sizeDelta = new Vector2(20, 0);

            var slider            = go.AddComponent<Slider>();
            slider.fillRect       = fillRT;
            slider.handleRect     = handleRT;
            slider.minValue       = 0f;
            slider.maxValue       = 1f;
            slider.value          = defaultVal;
            return slider;
        }

        // ====================================================================
        // HAZARD PREFABS
        // ====================================================================

        static void CreateHazardPrefabs()
        {
            string hazardDir = PREFABS + "/Hazards";
            if (!Directory.Exists(hazardDir))
                Directory.CreateDirectory(hazardDir);

            // DamageHazard — pinchos rojos
            CreateHazardPrefab<DamageHazard>(
                hazardDir + "/Hazard_Damage.prefab",
                "Hazard_Damage",
                new Color(0.85f, 0.15f, 0.15f),
                configure: go =>
                {
                    var h = go.GetComponent<DamageHazard>();
                    var so = new SerializedObject(h);
                    so.FindProperty("_damageAmount").intValue = 10;
                    so.FindProperty("_damageCooldown").floatValue = 1f;
                    so.FindProperty("_destroyOnHit").boolValue = false;
                    so.ApplyModifiedProperties();
                }
            );

            // SlowHazard — barro gris-azul
            CreateHazardPrefab<SlowHazard>(
                hazardDir + "/Hazard_Slow.prefab",
                "Hazard_Slow",
                new Color(0.35f, 0.55f, 0.75f),
                configure: go =>
                {
                    var h = go.GetComponent<SlowHazard>();
                    var so = new SerializedObject(h);
                    so.FindProperty("_slowMultiplier").floatValue = 0.4f;
                    so.FindProperty("_effectDuration").floatValue = 2f;
                    so.FindProperty("_damageCooldown").floatValue = 0.5f;
                    so.ApplyModifiedProperties();
                }
            );

            // PoisonHazard — verde venenoso
            CreateHazardPrefab<PoisonHazard>(
                hazardDir + "/Hazard_Poison.prefab",
                "Hazard_Poison",
                new Color(0.2f, 0.75f, 0.2f),
                configure: go =>
                {
                    var h = go.GetComponent<PoisonHazard>();
                    var so = new SerializedObject(h);
                    so.FindProperty("_damagePerTick").intValue = 5;
                    so.FindProperty("_slowAmount").floatValue = 0.6f;
                    so.FindProperty("_damageCooldown").floatValue = 1.5f;
                    so.ApplyModifiedProperties();
                }
            );

            // ScoreDrainHazard — dorado oscuro
            CreateHazardPrefab<ScoreDrainHazard>(
                hazardDir + "/Hazard_ScoreDrain.prefab",
                "Hazard_ScoreDrain",
                new Color(0.75f, 0.55f, 0.1f),
                configure: go =>
                {
                    var h = go.GetComponent<ScoreDrainHazard>();
                    var so = new SerializedObject(h);
                    so.FindProperty("_pointsToRemove").intValue = 50;
                    so.FindProperty("_damageCooldown").floatValue = 2f;
                    so.ApplyModifiedProperties();
                }
            );

            AssetDatabase.SaveAssets();
            Debug.Log("[BITFullSetup] 4 prefabs de hazard creados en " + hazardDir);
        }

        static void CreateHazardPrefab<T>(string path, string goName, Color color,
            System.Action<GameObject> configure) where T : MonoBehaviour
        {
            if (File.Exists(path)) return;

            var go = new GameObject(goName);

            // Sprite visual
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = GetPrimitiveSprite();
            sr.color = color;
            sr.sortingOrder = 1;

            // Collider trigger
            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.4f;

            // Hazard component
            go.AddComponent<T>();

            configure?.Invoke(go);

            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
        }

        static Sprite GetPrimitiveSprite()
        {
            // Intentar cargar el sprite de un hazard FX del pack
            string[] candidates = {
                FX_ROOT + "/CircularSlash/SpriteSheet.png",
                FX_ROOT + "/Explosion/SpriteSheet.png",
                FX_ROOT + "/Claw/SpriteSheet.png",
            };
            foreach (var c in candidates)
            {
                var sp = AssetDatabase.LoadAssetAtPath<Sprite>(c);
                if (sp != null) return sp;
            }
            return null;
        }

        // ====================================================================
        // RANKING ENTRY PREFAB
        // ====================================================================

        static void CreateRankingEntryPrefab()
        {
            string uiDir = PREFABS + "/UI";
            string path = uiDir + "/RankingEntry.prefab";

            if (!Directory.Exists(uiDir))
                Directory.CreateDirectory(uiDir);

            if (File.Exists(path)) return;

            // Root
            var root = new GameObject("RankingEntry");
            var rootImg = root.AddComponent<Image>();
            rootImg.color = new Color(0.1f, 0.09f, 0.14f, 0.9f);
            var rootRT = root.GetComponent<RectTransform>();
            rootRT.sizeDelta = new Vector2(600, 50);

            // Rank number (#1, #2, …)
            var rankGO = new GameObject("RankText");
            rankGO.transform.SetParent(root.transform, false);
            var rankTMP = rankGO.AddComponent<TextMeshProUGUI>();
            rankTMP.text = "#1";
            rankTMP.fontSize = 20;
            rankTMP.fontStyle = FontStyles.Bold;
            rankTMP.alignment = TextAlignmentOptions.Center;
            rankTMP.color = new Color(1f, 0.85f, 0.2f);
            var rankRT = rankGO.GetComponent<RectTransform>();
            rankRT.anchorMin = new Vector2(0f, 0f);
            rankRT.anchorMax = new Vector2(0.15f, 1f);
            rankRT.offsetMin = rankRT.offsetMax = Vector2.zero;

            // Player name
            var nameGO = new GameObject("NameText");
            nameGO.transform.SetParent(root.transform, false);
            var nameTMP = nameGO.AddComponent<TextMeshProUGUI>();
            nameTMP.text = "Jugador";
            nameTMP.fontSize = 18;
            nameTMP.alignment = TextAlignmentOptions.Left;
            nameTMP.color = Color.white;
            var nameRT = nameGO.GetComponent<RectTransform>();
            nameRT.anchorMin = new Vector2(0.17f, 0f);
            nameRT.anchorMax = new Vector2(0.65f, 1f);
            nameRT.offsetMin = nameRT.offsetMax = Vector2.zero;

            // Score
            var scoreGO = new GameObject("ScoreText");
            scoreGO.transform.SetParent(root.transform, false);
            var scoreTMP = scoreGO.AddComponent<TextMeshProUGUI>();
            scoreTMP.text = "9999";
            scoreTMP.fontSize = 18;
            scoreTMP.fontStyle = FontStyles.Bold;
            scoreTMP.alignment = TextAlignmentOptions.Right;
            scoreTMP.color = new Color(0.3f, 1f, 0.5f);
            var scoreRT = scoreGO.GetComponent<RectTransform>();
            scoreRT.anchorMin = new Vector2(0.67f, 0f);
            scoreRT.anchorMax = new Vector2(0.98f, 1f);
            scoreRT.offsetMin = scoreRT.offsetMax = Vector2.zero;

            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);

            AssetDatabase.SaveAssets();
            Debug.Log("[BITFullSetup] RankingEntry prefab creado en " + path);
        }

        // ====================================================================
        // WIRE VFX SPRITES
        // ====================================================================

        static void WireVFXSprites()
        {
            var vfx = Object.FindFirstObjectByType<VFXManager>();
            if (vfx == null)
            {
                Debug.LogWarning("[BITFullSetup] VFXManager no está en la escena activa. Abre gamesetupscene.");
                return;
            }

            var so = new SerializedObject(vfx);

            TrySetPrefabFromFX(so, "slashEffectPrefab",  PREFABS + "/VFX/Slash.prefab",  FX_ROOT + "/CircularSlash/SpriteSheet.png");
            TrySetPrefabFromFX(so, "hitEffectPrefab",    PREFABS + "/VFX/Hit.prefab",     FX_ROOT + "/Claw/SpriteSheet.png");
            TrySetPrefabFromFX(so, "deathEffectPrefab",  PREFABS + "/VFX/Death.prefab",   FX_ROOT + "/Explosion/SpriteSheet.png");
            TrySetPrefabFromFX(so, "pickupEffectPrefab", PREFABS + "/VFX/Pickup.prefab",  FX_ROOT + "/Thunder/SpriteSheet.png");

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(vfx);
        }

        static void TrySetPrefabFromFX(SerializedObject so, string propName, string prefabPath, string spritePath)
        {
            var prop = so.FindProperty(propName);
            if (prop == null || prop.objectReferenceValue != null) return;

            // Buscar o crear prefab VFX
            var prefabGO = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefabGO == null)
                prefabGO = CreateVFXPrefab(prefabPath, spritePath);

            if (prefabGO != null)
                prop.objectReferenceValue = prefabGO;
        }

        static GameObject CreateVFXPrefab(string prefabPath, string spritePath)
        {
            string vfxDir = PREFABS + "/VFX";
            if (!Directory.Exists(vfxDir))
                Directory.CreateDirectory(vfxDir);

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (sprite == null)
            {
                var sprites = AssetDatabase.LoadAllAssetsAtPath(spritePath);
                foreach (var a in sprites)
                    if (a is Sprite s) { sprite = s; break; }
            }

            var go = new GameObject(Path.GetFileNameWithoutExtension(prefabPath));
            var sr = go.AddComponent<SpriteRenderer>();
            if (sprite != null) sr.sprite = sprite;
            sr.sortingOrder = 10;

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Object.DestroyImmediate(go);
            return prefab;
        }

        // ====================================================================
        // FIX PREFABS — OrbitWeapon en Player, HealthPickup/ScorePickup en pickups
        // ====================================================================

        static void FixPrefabs()
        {
            FixPlayerPrefab();
            FixPickupPrefab<HealthPickup>(PREFABS + "/Pickups/Heart.prefab",   "Heart",   new Color(1f, 0.2f, 0.2f));
            FixPickupPrefab<ScorePickup> (PREFABS + "/Pickups/Coin.prefab",    "Coin",    new Color(1f, 0.85f, 0.1f));
            CreateSpeedPickupPrefab();
        }

        static void FixPlayerPrefab()
        {
            string path = PREFABS + "/Player/Player.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) { Debug.LogWarning("[BITFullSetup] Player.prefab no encontrado."); return; }

            using (var scope = new PrefabUtility.EditPrefabContentsScope(path))
            {
                var root = scope.prefabContentsRoot;
                var pc   = root.GetComponent<BIT.Player.PlayerController>();

                // ── OrbitWeapon ───────────────────────────────────────────
                if (root.GetComponentInChildren<OrbitWeapon>() == null)
                {
                    var weaponGO = new GameObject("Weapon");
                    weaponGO.transform.SetParent(root.transform, false);
                    weaponGO.transform.localPosition = new Vector3(0.8f, 0f, 0f);

                    var sr = weaponGO.AddComponent<SpriteRenderer>();
                    sr.sortingOrder = 5;
                    var swordSprite = LoadFirstSprite(
                        "Assets/_Project/Sprites/Ninja Adventure/Actor/Weapon/Sword/SpriteSheet.png");
                    if (swordSprite != null) sr.sprite = swordSprite;

                    var col = weaponGO.AddComponent<CircleCollider2D>();
                    col.isTrigger = true;
                    col.radius = 0.25f;

                    var ow   = weaponGO.AddComponent<OrbitWeapon>();
                    var soOW = new SerializedObject(ow);
                    soOW.FindProperty("_orbitRadius").floatValue    = 0.8f;
                    soOW.FindProperty("_rotationSpeed").floatValue  = 15f;
                    soOW.FindProperty("_damage").intValue           = 10;
                    soOW.FindProperty("_weaponSprite").objectReferenceValue = sr;
                    soOW.ApplyModifiedProperties();

                    Debug.Log("[BITFullSetup] OrbitWeapon añadido al Player.prefab.");
                }

                // ── FirePoint + Projectile ────────────────────────────────
                if (pc != null && pc.projectilePrefab == null)
                {
                    var shurikenPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                        PREFABS + "/Projectiles/Shuriken.prefab");
                    if (shurikenPrefab != null)
                    {
                        // Crear FirePoint hijo
                        var fpGO = root.transform.Find("FirePoint")?.gameObject;
                        if (fpGO == null)
                        {
                            fpGO = new GameObject("FirePoint");
                            fpGO.transform.SetParent(root.transform, false);
                            fpGO.transform.localPosition = new Vector3(0.5f, 0f, 0f);
                        }

                        var soPC = new SerializedObject(pc);
                        soPC.FindProperty("projectilePrefab").objectReferenceValue = shurikenPrefab;
                        soPC.FindProperty("firePoint").objectReferenceValue        = fpGO.transform;
                        soPC.ApplyModifiedProperties();

                        Debug.Log("[BITFullSetup] Shuriken + FirePoint conectados al Player.prefab.");
                    }
                }
            }
        }

        static Sprite LoadFirstSprite(string path)
        {
            var sp = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sp != null) return sp;
            foreach (var a in AssetDatabase.LoadAllAssetsAtPath(path))
                if (a is Sprite s) return s;
            return null;
        }

        static void CreateSpeedPickupPrefab()
        {
            string path = PREFABS + "/Pickups/SpeedBoost.prefab";
            if (File.Exists(path)) return;

            var go = new GameObject("SpeedBoost");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.color = new Color(0.3f, 0.8f, 1f);
            sr.sortingOrder = 1;

            // Intentar usar sprite del pack (lightning/FX)
            var sp = LoadFirstSprite(FX_ROOT + "/Thunder/SpriteSheet.png");
            if (sp != null) sr.sprite = sp;

            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.3f;

            var pickup = go.AddComponent<SpeedPickup>();
            var so = new SerializedObject(pickup);
            so.FindProperty("_speedMultiplier").floatValue = 1.5f;
            so.FindProperty("_duration").floatValue        = 5f;
            so.FindProperty("_floatAnimation").boolValue   = true;
            so.ApplyModifiedProperties();

            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);

            Debug.Log("[BITFullSetup] SpeedBoost.prefab creado.");
        }

        static void FixPickupPrefab<T>(string path, string name, Color color)
            where T : MonoBehaviour
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) { Debug.LogWarning($"[BITFullSetup] {name}.prefab no encontrado: {path}"); return; }

            // Ya tiene el componente?
            if (prefab.GetComponent<T>() != null) return;

            using (var scope = new PrefabUtility.EditPrefabContentsScope(path))
            {
                var root = scope.prefabContentsRoot;
                if (root.GetComponent<T>() != null) return;

                root.AddComponent<T>();

                // Asegurar SpriteRenderer visible
                var sr = root.GetComponent<SpriteRenderer>();
                if (sr != null) sr.color = color;
            }

            Debug.Log($"[BITFullSetup] {typeof(T).Name} añadido a {name}.prefab.");
        }

        // ====================================================================
        // CREAR GAMEEVENTSO ASSETS
        // ====================================================================

        static void CreateGameEvents()
        {
            string eventsDir = "Assets/_Project/SO_Data/Events";
            if (!Directory.Exists(eventsDir))
                Directory.CreateDirectory(eventsDir);

            CreateEventAsset(eventsDir + "/OnPlayerDamage.asset");
            CreateEventAsset(eventsDir + "/OnPlayerAttack.asset");
            CreateEventAsset(eventsDir + "/OnPickup.asset");
            CreateEventAsset(eventsDir + "/OnCoin.asset");
            CreateEventAsset(eventsDir + "/OnPlayerDeath.asset");

            AssetDatabase.SaveAssets();
            Debug.Log("[BITFullSetup] GameEventSO assets creados en " + eventsDir);
        }

        static void CreateEventAsset(string path)
        {
            if (File.Exists(path)) return;
            var asset = ScriptableObject.CreateInstance<BIT.Events.GameEventSO>();
            AssetDatabase.CreateAsset(asset, path);
        }

        // ====================================================================
        // MENU ITEM INDIVIDUAL: SOLO HAZARDS
        // ====================================================================

        [MenuItem("BIT/5. Crear Prefabs de Hazards", priority = 55)]
        public static void CreateHazardsOnly()
        {
            CreateHazardPrefabs();
            CreateRankingEntryPrefab();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("BIT – Hazards",
                "Prefabs creados:\n" +
                "  Prefabs/Hazards/Hazard_Damage.prefab\n" +
                "  Prefabs/Hazards/Hazard_Slow.prefab\n" +
                "  Prefabs/Hazards/Hazard_Poison.prefab\n" +
                "  Prefabs/Hazards/Hazard_ScoreDrain.prefab\n" +
                "  Prefabs/UI/RankingEntry.prefab",
                "OK");
        }

        // ====================================================================
        // MENU ITEM: COLOCAR HAZARDS EN LA ESCENA ACTUAL
        // ====================================================================

        [MenuItem("BIT/6. Colocar Hazards en Escena", priority = 56)]
        public static void PlaceHazardsInScene()
        {
            string[] hazardPaths = {
                PREFABS + "/Hazards/Hazard_Damage.prefab",
                PREFABS + "/Hazards/Hazard_Slow.prefab",
                PREFABS + "/Hazards/Hazard_Poison.prefab",
                PREFABS + "/Hazards/Hazard_ScoreDrain.prefab",
            };

            // Posiciones fijas dentro del dungeon
            Vector2[] positions = {
                new(-8f,  4f),   // arriba-izquierda
                new( 8f,  4f),   // arriba-derecha
                new(-8f, -4f),   // abajo-izquierda
                new( 8f, -4f),   // abajo-derecha
                new(-4f,  0f),   // centro-izquierda
                new( 4f,  0f),   // centro-derecha
                new( 0f,  2f),   // centro-arriba
                new( 0f, -2f),   // centro-abajo
            };

            int placed = 0;
            for (int i = 0; i < Mathf.Min(positions.Length, hazardPaths.Length * 2); i++)
            {
                string p = hazardPaths[i % hazardPaths.Length];
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(p);
                if (prefab == null) continue;

                var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                inst.transform.position = new Vector3(positions[i].x, positions[i].y, 0);
                inst.name = prefab.name + "_" + i;
                placed++;
            }

            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log($"[BITFullSetup] {placed} hazards colocados en la escena.");
            EditorUtility.DisplayDialog("BIT – Hazards Colocados",
                $"{placed} hazards colocados en el mapa.\n\nGuarda la escena (Ctrl+S).",
                "OK");
        }
    }
}
#endif
