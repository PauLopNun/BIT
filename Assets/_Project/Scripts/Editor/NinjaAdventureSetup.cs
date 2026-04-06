#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.U2D.Sprites;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System.IO;

namespace BIT.Editor
{
    /// <summary>
    /// Script de Editor para configurar todos los assets de Ninja Adventure
    /// y crear una escena completa jugable.
    ///
    /// USO: Menu -> BIT -> Setup Ninja Adventure Scene
    /// </summary>
    public class NinjaAdventureSetup : EditorWindow
    {
        // =======================================================================
        // RUTAS DE ASSETS
        // =======================================================================
        private const string NINJA_PATH = "Assets/_Project/Sprites/Ninja Adventure";
        private const string PREFABS_PATH = "Assets/_Project/Prefabs";
        private const string ANIMS_PATH = "Assets/_Project/Animations";
        private const string AUDIO_PATH = "Assets/_Project/Sprites/Ninja Adventure/Audio";

        // Personajes
        private const string PLAYER_SHEET = "Actor/Character/NinjaBlue/SpriteSheet.png";

        // Enemigos (rutas correctas en Monster/)
        private const string ENEMY_SKULL = "Actor/Monster/Skull/SpriteSheet.png";
        private const string ENEMY_DRAGON = "Actor/Monster/Dragon/SpriteSheet.png";
        private const string ENEMY_CYCLOPE = "Actor/Monster/Cyclope/SpriteSheet.png";

        // Items
        private const string ITEM_COIN = "Items/Treasure/GoldCoin.png";
        private const string ITEM_HEART = "Items/Potion/Heart.png";
        private const string ITEM_SHURIKEN = "Items/Projectile/Shuriken.png";

        // Tilesets
        private const string TILESET_FLOOR   = "Backgrounds/Tilesets/TilesetFloor.png";
        private const string TILESET_NATURE  = "Backgrounds/Tilesets/TilesetNature.png";
        private const string TILESET_WATER   = "Backgrounds/Tilesets/TilesetWater.png";
        private const string TILESET_VILLAGE = "Backgrounds/Tilesets/TilesetVillageAbandoned.png";

        // FX
        private const string FX_SLASH = "FX/Attack/Slash";

        // =======================================================================
        // MENU PRINCIPAL
        // =======================================================================

        [MenuItem("BIT/Setup Ninja Adventure Scene")]
        public static void SetupScene()
        {
            Debug.Log("=== INICIANDO SETUP DE NINJA ADVENTURE ===");

            EditorUtility.DisplayProgressBar("Setup BIT", "Creando carpetas...", 0.05f);
            CreateFolders();

            EditorUtility.DisplayProgressBar("Setup BIT", "Configurando sprites del jugador...", 0.1f);
            ConfigurePlayerSprites();

            EditorUtility.DisplayProgressBar("Setup BIT", "Configurando sprites de enemigos...", 0.2f);
            ConfigureEnemySprites();

            EditorUtility.DisplayProgressBar("Setup BIT", "Configurando items...", 0.3f);
            ConfigureItemSprites();

            EditorUtility.DisplayProgressBar("Setup BIT", "Configurando tilesets...", 0.35f);
            ConfigureTilesets();

            EditorUtility.DisplayProgressBar("Setup BIT", "Creando animaciones del jugador...", 0.4f);
            CreatePlayerAnimator();

            EditorUtility.DisplayProgressBar("Setup BIT", "Creando animaciones de enemigos...", 0.5f);
            CreateEnemyAnimators();

            EditorUtility.DisplayProgressBar("Setup BIT", "Creando prefab del jugador...", 0.6f);
            CreatePlayerPrefab();

            EditorUtility.DisplayProgressBar("Setup BIT", "Creando prefabs de enemigos...", 0.7f);
            CreateEnemyPrefabs();

            EditorUtility.DisplayProgressBar("Setup BIT", "Creando prefabs de items...", 0.75f);
            CreateItemPrefabs();

            EditorUtility.DisplayProgressBar("Setup BIT", "Configurando escena...", 0.85f);
            SetupGameScene();

            EditorUtility.DisplayProgressBar("Setup BIT", "Guardando assets...", 0.95f);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.ClearProgressBar();

            EditorUtility.DisplayDialog("BIT - Setup Completo",
                "ESCENA CONFIGURADA CON NINJA ADVENTURE\n\n" +
                "CONTROLES:\n" +
                "- WASD / Flechas: Mover\n" +
                "- Espacio / Click: Lanzar Shuriken\n\n" +
                "CONTENIDO:\n" +
                "- Jugador: NinjaBlue con animaciones\n" +
                "- Enemigos: Esqueleto, Dragon, Ciclope\n" +
                "- Items: Monedas y Corazones\n" +
                "- Mapa: Suelo con tilesets reales\n\n" +
                "DALE A PLAY!", "OK");
        }

        // =======================================================================
        // CARPETAS
        // =======================================================================

        static void CreateFolders()
        {
            CreateFolderIfNeeded("Assets/_Project", "Prefabs");
            CreateFolderIfNeeded("Assets/_Project", "Animations");
            CreateFolderIfNeeded(PREFABS_PATH, "Player");
            CreateFolderIfNeeded(PREFABS_PATH, "Enemies");
            CreateFolderIfNeeded(PREFABS_PATH, "Pickups");
            CreateFolderIfNeeded(PREFABS_PATH, "Projectiles");
            CreateFolderIfNeeded(PREFABS_PATH, "Environment");
            CreateFolderIfNeeded(ANIMS_PATH, "Player");
            CreateFolderIfNeeded(ANIMS_PATH, "Enemy");
            Debug.Log("[Setup] Carpetas creadas");
        }

        static void CreateFolderIfNeeded(string parent, string name)
        {
            string path = $"{parent}/{name}";
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, name);
        }

        // =======================================================================
        // CONFIGURAR SPRITES
        // =======================================================================

        static void ConfigurePlayerSprites()
        {
            string path = $"{NINJA_PATH}/{PLAYER_SHEET}";
            ConfigureSpriteSheet(path, 16, 16);
            Debug.Log("[Setup] Sprites del jugador configurados");
        }

        static void ConfigureEnemySprites()
        {
            ConfigureSpriteSheet($"{NINJA_PATH}/{ENEMY_SKULL}", 16, 16);
            ConfigureSpriteSheet($"{NINJA_PATH}/{ENEMY_DRAGON}", 16, 16);
            ConfigureSpriteSheet($"{NINJA_PATH}/{ENEMY_CYCLOPE}", 16, 16);
            Debug.Log("[Setup] Sprites de enemigos configurados");
        }

        static void ConfigureItemSprites()
        {
            ConfigureSingleSprite($"{NINJA_PATH}/{ITEM_COIN}");
            ConfigureSingleSprite($"{NINJA_PATH}/{ITEM_HEART}");
            ConfigureSingleSprite($"{NINJA_PATH}/{ITEM_SHURIKEN}");
            Debug.Log("[Setup] Sprites de items configurados");
        }

        static void ConfigureTilesets()
        {
            ConfigureSpriteSheet($"{NINJA_PATH}/{TILESET_FLOOR}",   16, 16);
            ConfigureSpriteSheet($"{NINJA_PATH}/{TILESET_NATURE}",  16, 16);
            ConfigureSpriteSheet($"{NINJA_PATH}/{TILESET_WATER}",   16, 16);
            ConfigureSpriteSheet($"{NINJA_PATH}/{TILESET_VILLAGE}", 16, 16);
            Debug.Log("[Setup] Tilesets configurados (Floor, Nature, Water, VillageAbandoned)");
        }

        static void ConfigureSpriteSheet(string path, int spriteW, int spriteH)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogWarning($"[Setup] No se encontro: {path}");
                return;
            }

            // Configurar el importer base
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = 16;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.isReadable = true;  // necesario para muestrear colores
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();

            // Necesitamos la textura importada para saber dimensiones reales
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex == null) return;

            int cols = tex.width  / spriteW;
            int rows = tex.height / spriteH;

            // Usar ISpriteEditorDataProvider (API no obsoleta en Unity 2022+)
            var factory = new SpriteDataProviderFactories();
            factory.Init();
            var dataProvider = factory.GetSpriteEditorDataProviderFromObject(importer);
            dataProvider.InitSpriteEditorDataProvider();

            var spriteRects = new List<SpriteRect>();
            int idx = 0;

            // Orden: de arriba (fila rows-1 en coords Unity) hacia abajo, izquierda a derecha
            for (int row = rows - 1; row >= 0; row--)
            {
                for (int col = 0; col < cols; col++)
                {
                    spriteRects.Add(new SpriteRect
                    {
                        name      = $"sprite_{idx}",
                        rect      = new Rect(col * spriteW, row * spriteH, spriteW, spriteH),
                        pivot     = new Vector2(0.5f, 0.5f),
                        alignment = SpriteAlignment.Center,
                        spriteID  = GUID.Generate()
                    });
                    idx++;
                }
            }

            dataProvider.SetSpriteRects(spriteRects.ToArray());
            dataProvider.Apply();
            (dataProvider.targetObject as TextureImporter)?.SaveAndReimport();
        }

        static void ConfigureSingleSprite(string path)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogWarning($"[Setup] No se encontro: {path}");
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 16;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
        }

        // =======================================================================
        // CREAR ANIMACIONES
        // =======================================================================

        static void CreatePlayerAnimator()
        {
            string sheetPath = $"{NINJA_PATH}/{PLAYER_SHEET}";
            Sprite[] sprites = LoadAllSprites(sheetPath);

            if (sprites.Length == 0)
            {
                Debug.LogError("[Setup] No se encontraron sprites del jugador!");
                return;
            }

            Debug.Log($"[Setup] Cargados {sprites.Length} sprites del jugador");

            string controllerPath = $"{ANIMS_PATH}/Player/PlayerAnimator.controller";

            if (File.Exists(controllerPath))
                AssetDatabase.DeleteAsset(controllerPath);

            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("MoveX", AnimatorControllerParameterType.Float);
            controller.AddParameter("MoveY", AnimatorControllerParameterType.Float);
            controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);

            var rootStateMachine = controller.layers[0].stateMachine;

            var idleState = rootStateMachine.AddState("Idle");
            var walkState = rootStateMachine.AddState("Walk");

            // Spritesheet NinjaBlue: 4 COLUMNAS x 7 FILAS de 16x16px
            // Columna 0 = Down, Columna 1 = Up, Columna 2 = Left, Columna 3 = Right
            // Filas 0-3 = ciclo de walk (4 frames), Fila 4 = Attack, Fila 5 = Jump, Fila 6 = Dead
            const int SHEET_COLS = 4;
            const int WALK_ROWS  = 4;

            // Idle: 1 frame por dirección (fila 0 de cada columna)
            AnimationClip idleDown  = CreateAnimationClip("IdleDown",  new[] { GetSpriteAt(sprites, 0, SHEET_COLS) }, 1);
            AnimationClip idleUp    = CreateAnimationClip("IdleUp",    new[] { GetSpriteAt(sprites, 1, SHEET_COLS) }, 1);
            AnimationClip idleLeft  = CreateAnimationClip("IdleLeft",  new[] { GetSpriteAt(sprites, 2, SHEET_COLS) }, 1);
            AnimationClip idleRight = CreateAnimationClip("IdleRight", new[] { GetSpriteAt(sprites, 3, SHEET_COLS) }, 1);

            // Walk: 4 frames por dirección (columna completa, filas 0-3)
            AnimationClip walkDown  = CreateAnimationClip("WalkDown",  GetColumnSprites(sprites, 0, WALK_ROWS, SHEET_COLS), 6);
            AnimationClip walkUp    = CreateAnimationClip("WalkUp",    GetColumnSprites(sprites, 1, WALK_ROWS, SHEET_COLS), 6);
            AnimationClip walkLeft  = CreateAnimationClip("WalkLeft",  GetColumnSprites(sprites, 2, WALK_ROWS, SHEET_COLS), 6);
            AnimationClip walkRight = CreateAnimationClip("WalkRight", GetColumnSprites(sprites, 3, WALK_ROWS, SHEET_COLS), 6);

            SaveAnimationClip(idleDown,  "Player");
            SaveAnimationClip(idleUp,    "Player");
            SaveAnimationClip(idleLeft,  "Player");
            SaveAnimationClip(idleRight, "Player");
            SaveAnimationClip(walkDown,  "Player");
            SaveAnimationClip(walkUp,    "Player");
            SaveAnimationClip(walkLeft,  "Player");
            SaveAnimationClip(walkRight, "Player");

            // Idle Blend Tree (mantiene la dirección al pararse)
            BlendTree idleTree;
            controller.CreateBlendTreeInController("IdleBlend", out idleTree);
            idleTree.blendType = BlendTreeType.SimpleDirectional2D;
            idleTree.blendParameter = "MoveX";
            idleTree.blendParameterY = "MoveY";
            idleTree.AddChild(idleDown,  new Vector2(0, -1));
            idleTree.AddChild(idleUp,    new Vector2(0,  1));
            idleTree.AddChild(idleLeft,  new Vector2(-1, 0));
            idleTree.AddChild(idleRight, new Vector2( 1, 0));
            idleState.motion = idleTree;

            // Walk Blend Tree
            BlendTree walkTree;
            controller.CreateBlendTreeInController("WalkBlend", out walkTree);
            walkTree.blendType = BlendTreeType.SimpleDirectional2D;
            walkTree.blendParameter = "MoveX";
            walkTree.blendParameterY = "MoveY";
            walkTree.AddChild(walkDown,  new Vector2(0, -1));
            walkTree.AddChild(walkUp,    new Vector2(0,  1));
            walkTree.AddChild(walkLeft,  new Vector2(-1, 0));
            walkTree.AddChild(walkRight, new Vector2( 1, 0));
            walkState.motion = walkTree;

            var toWalk = idleState.AddTransition(walkState);
            toWalk.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
            toWalk.hasExitTime = false;
            toWalk.duration = 0.05f;

            var toIdle = walkState.AddTransition(idleState);
            toIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
            toIdle.hasExitTime = false;
            toIdle.duration = 0.05f;

            rootStateMachine.defaultState = idleState;

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();

            Debug.Log("[Setup] Animator jugador: IdleBlend + WalkBlend con sprites correctos por columna");
        }

        static void CreateEnemyAnimators()
        {
            CreateEnemyAnimator("Skull", ENEMY_SKULL, "Skeleton");
            CreateEnemyAnimator("Dragon", ENEMY_DRAGON, "Dragon");
            CreateEnemyAnimator("Cyclope", ENEMY_CYCLOPE, "Cyclope");
            Debug.Log("[Setup] Animators de enemigos creados");
        }

        static void CreateEnemyAnimator(string enemyName, string sheetPath, string prefix)
        {
            string fullPath = $"{NINJA_PATH}/{sheetPath}";
            Sprite[] sprites = LoadAllSprites(fullPath);

            if (sprites.Length == 0)
            {
                Debug.LogWarning($"[Setup] No sprites para {enemyName}");
                return;
            }

            string controllerPath = $"{ANIMS_PATH}/Enemy/{prefix}Animator.controller";

            if (File.Exists(controllerPath))
                AssetDatabase.DeleteAsset(controllerPath);

            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("MoveX", AnimatorControllerParameterType.Float);
            controller.AddParameter("MoveY", AnimatorControllerParameterType.Float);

            var rootStateMachine = controller.layers[0].stateMachine;

            var idleState = rootStateMachine.AddState("Idle");
            var walkState = rootStateMachine.AddState("Walk");

            // Spritesheet enemigos: misma estructura que el ninja (4 cols x 7 filas)
            const int ECOLS = 4;
            const int EROWS = 4;

            AnimationClip idleClip = CreateAnimationClip($"{prefix}Idle", new[] { GetSpriteAt(sprites, 0, ECOLS) }, 1);
            SaveAnimationClip(idleClip, "Enemy");

            AnimationClip walkDown  = CreateAnimationClip($"{prefix}WalkDown",  GetColumnSprites(sprites, 0, EROWS, ECOLS), 6);
            AnimationClip walkUp    = CreateAnimationClip($"{prefix}WalkUp",    GetColumnSprites(sprites, 1, EROWS, ECOLS), 6);
            AnimationClip walkLeft  = CreateAnimationClip($"{prefix}WalkLeft",  GetColumnSprites(sprites, 2, EROWS, ECOLS), 6);
            AnimationClip walkRight = CreateAnimationClip($"{prefix}WalkRight", GetColumnSprites(sprites, 3, EROWS, ECOLS), 6);

            SaveAnimationClip(walkDown, "Enemy");
            SaveAnimationClip(walkRight, "Enemy");
            SaveAnimationClip(walkUp, "Enemy");
            SaveAnimationClip(walkLeft, "Enemy");

            idleState.motion = idleClip;

            BlendTree walkTree;
            controller.CreateBlendTreeInController($"{prefix}WalkBlend", out walkTree);
            walkTree.blendType = BlendTreeType.SimpleDirectional2D;
            walkTree.blendParameter = "MoveX";
            walkTree.blendParameterY = "MoveY";

            walkTree.AddChild(walkDown, new Vector2(0, -1));
            walkTree.AddChild(walkUp, new Vector2(0, 1));
            walkTree.AddChild(walkLeft, new Vector2(-1, 0));
            walkTree.AddChild(walkRight, new Vector2(1, 0));

            walkState.motion = walkTree;

            var toWalk = idleState.AddTransition(walkState);
            toWalk.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
            toWalk.hasExitTime = false;
            toWalk.duration = 0.1f;

            var toIdle = walkState.AddTransition(idleState);
            toIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
            toIdle.hasExitTime = false;
            toIdle.duration = 0.1f;

            rootStateMachine.defaultState = idleState;

            EditorUtility.SetDirty(controller);
        }

        // =======================================================================
        // HELPERS DE ANIMACION
        // =======================================================================

        static AnimationClip CreateAnimationClip(string name, Sprite[] sprites, int fps)
        {
            AnimationClip clip = new AnimationClip();
            clip.name = name;
            clip.frameRate = fps;

            EditorCurveBinding binding = new EditorCurveBinding();
            binding.type = typeof(SpriteRenderer);
            binding.path = "";
            binding.propertyName = "m_Sprite";

            int frameCount = sprites.Length;
            ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[frameCount + 1];
            float frameTime = 1f / fps;

            for (int i = 0; i < frameCount; i++)
            {
                keyframes[i] = new ObjectReferenceKeyframe();
                keyframes[i].time = i * frameTime;
                keyframes[i].value = sprites[i];
            }

            // Loop
            keyframes[frameCount] = new ObjectReferenceKeyframe();
            keyframes[frameCount].time = frameCount * frameTime;
            keyframes[frameCount].value = sprites[0];

            AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);

            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            return clip;
        }

        static void SaveAnimationClip(AnimationClip clip, string subfolder)
        {
            string path = $"{ANIMS_PATH}/{subfolder}/{clip.name}.anim";
            if (AssetDatabase.LoadAssetAtPath<AnimationClip>(path) != null)
                AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(clip, path);
        }

        static Sprite[] GetSpriteRange(Sprite[] all, int start, int count)
        {
            List<Sprite> result = new List<Sprite>();
            for (int i = 0; i < count && (start + i) < all.Length; i++)
            {
                if (all[start + i] != null)
                    result.Add(all[start + i]);
            }

            // Si no hay suficientes sprites, duplicar el primero
            while (result.Count < count && result.Count > 0)
            {
                result.Add(result[0]);
            }

            return result.ToArray();
        }

        /// <summary>
        /// Obtiene los sprites de UNA COLUMNA recorriendo las filas.
        /// Spritesheet organizado: índice = fila * totalCols + col
        /// Ejemplo: col=0 (Down), totalCols=4, numRows=4 → sprites 0, 4, 8, 12
        /// </summary>
        static Sprite[] GetColumnSprites(Sprite[] all, int col, int numRows, int totalCols)
        {
            var result = new List<Sprite>();
            for (int row = 0; row < numRows; row++)
            {
                int idx = row * totalCols + col;
                if (idx < all.Length && all[idx] != null)
                    result.Add(all[idx]);
            }
            // Si algo falla, al menos devolver el primer sprite
            if (result.Count == 0 && all.Length > 0)
                result.Add(all[0]);
            return result.ToArray();
        }

        /// <summary>
        /// Obtiene un sprite en la posición (fila 0, columna col).
        /// </summary>
        static Sprite GetSpriteAt(Sprite[] all, int col, int totalCols)
        {
            int idx = col;  // fila 0: índice = col
            if (idx < all.Length && all[idx] != null) return all[idx];
            if (all.Length > 0) return all[0];
            return null;
        }

        static Sprite GetSpriteOrDefault(Sprite[] sprites, int index)
        {
            if (index < sprites.Length && sprites[index] != null)
                return sprites[index];
            if (sprites.Length > 0)
                return sprites[0];
            return null;
        }

        static Sprite[] LoadAllSprites(string path)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            List<Sprite> sprites = new List<Sprite>();

            foreach (var asset in assets)
            {
                if (asset is Sprite s)
                    sprites.Add(s);
            }

            // Ordenar por numero en el nombre
            sprites.Sort((a, b) => {
                int numA = ExtractNumber(a.name);
                int numB = ExtractNumber(b.name);
                return numA.CompareTo(numB);
            });

            return sprites.ToArray();
        }

        static int ExtractNumber(string name)
        {
            string num = "";
            foreach (char c in name)
            {
                if (char.IsDigit(c)) num += c;
            }
            return int.TryParse(num, out int result) ? result : 0;
        }

        // =======================================================================
        // CREAR PREFABS
        // =======================================================================

        static void CreatePlayerPrefab()
        {
            string sheetPath = $"{NINJA_PATH}/{PLAYER_SHEET}";
            Sprite[] sprites = LoadAllSprites(sheetPath);

            if (sprites.Length == 0)
            {
                Debug.LogError("[Setup] No sprites para Player!");
                return;
            }

            GameObject player = new GameObject("Player");
            player.tag = "Player";
            player.layer = LayerMask.NameToLayer("Default");

            SpriteRenderer sr = player.AddComponent<SpriteRenderer>();
            sr.sprite = sprites[0];
            sr.sortingOrder = 10;

            Rigidbody2D rb = player.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            CapsuleCollider2D col = player.AddComponent<CapsuleCollider2D>();
            col.size = new Vector2(0.5f, 0.7f);
            col.offset = new Vector2(0, -0.1f);

            // Animator
            Animator anim = player.AddComponent<Animator>();
            string controllerPath = $"{ANIMS_PATH}/Player/PlayerAnimator.controller";
            var animController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath);
            if (animController != null)
                anim.runtimeAnimatorController = animController;

            // PlayerInput
            PlayerInput input = player.AddComponent<PlayerInput>();
            string[] guids = AssetDatabase.FindAssets("PlayerInputActions t:InputActionAsset");
            if (guids.Length > 0)
            {
                var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(AssetDatabase.GUIDToAssetPath(guids[0]));
                if (asset != null)
                {
                    input.actions = asset;
                    input.defaultActionMap = "Player";
                }
            }

            // PlayerController
            player.AddComponent<BIT.Player.PlayerController>();

            SavePrefab(player, $"{PREFABS_PATH}/Player/Player.prefab");
            Debug.Log("[Setup] Prefab Player creado");
        }

        static void CreateEnemyPrefabs()
        {
            // Skeleton - Enemigo basico, velocidad media
            CreateEnemyPrefab("Enemy_Skeleton", ENEMY_SKULL, "Skeleton", 2f, 10, 50);

            // Dragon - Rapido y peligroso
            CreateEnemyPrefab("Enemy_Dragon", ENEMY_DRAGON, "Dragon", 3.5f, 20, 40);

            // Cyclope - Lento pero resistente
            CreateEnemyPrefab("Enemy_Cyclope", ENEMY_CYCLOPE, "Cyclope", 1.5f, 15, 80);

            Debug.Log("[Setup] Prefabs de enemigos creados (Skeleton, Dragon, Cyclope)");
        }

        static void CreateEnemyPrefab(string prefabName, string sheetPath, string animPrefix, float speed, int damage, int health)
        {
            string fullPath = $"{NINJA_PATH}/{sheetPath}";
            Sprite[] sprites = LoadAllSprites(fullPath);

            if (sprites.Length == 0)
            {
                Debug.LogWarning($"[Setup] No sprites para {prefabName}");
                return;
            }

            GameObject enemy = new GameObject(prefabName);
            enemy.tag = "Enemy";

            SpriteRenderer sr = enemy.AddComponent<SpriteRenderer>();
            sr.sprite = sprites[0];
            sr.sortingOrder = 9;

            Rigidbody2D rb = enemy.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;

            CircleCollider2D col = enemy.AddComponent<CircleCollider2D>();
            col.radius = 0.35f;

            // Animator
            Animator anim = enemy.AddComponent<Animator>();
            string controllerPath = $"{ANIMS_PATH}/Enemy/{animPrefix}Animator.controller";
            var animController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath);
            if (animController != null)
                anim.runtimeAnimatorController = animController;

            // SimpleEnemyAI con stats personalizados
            var ai = enemy.AddComponent<BIT.Core.SimpleEnemyAI>();
            ai.moveSpeed = speed;
            ai.damage = damage;
            ai.maxHealth = health;

            SavePrefab(enemy, $"{PREFABS_PATH}/Enemies/{prefabName}.prefab");
        }

        static void CreateItemPrefabs()
        {
            // Moneda
            CreatePickupPrefab("Coin", ITEM_COIN, "Coin", 0.35f);

            // Corazon
            CreatePickupPrefab("Heart", ITEM_HEART, "Health", 0.4f);

            // Shuriken (proyectil)
            CreateProjectilePrefab();

            Debug.Log("[Setup] Prefabs de items creados");
        }

        static void CreatePickupPrefab(string name, string spritePath, string tag, float radius)
        {
            string fullPath = $"{NINJA_PATH}/{spritePath}";
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(fullPath);

            if (sprite == null)
            {
                Debug.LogWarning($"[Setup] No sprite para {name}");
                return;
            }

            GameObject pickup = new GameObject(name);
            pickup.tag = tag;

            SpriteRenderer sr = pickup.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 5;

            CircleCollider2D col = pickup.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = radius;

            SavePrefab(pickup, $"{PREFABS_PATH}/Pickups/{name}.prefab");
        }

        static void CreateProjectilePrefab()
        {
            string fullPath = $"{NINJA_PATH}/{ITEM_SHURIKEN}";
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(fullPath);

            if (sprite == null)
            {
                Debug.LogWarning("[Setup] No sprite para Shuriken");
                return;
            }

            GameObject shuriken = new GameObject("Shuriken");
            shuriken.tag = "Projectile";

            SpriteRenderer sr = shuriken.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 15;

            CircleCollider2D col = shuriken.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.25f;

            Rigidbody2D rb = shuriken.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            shuriken.AddComponent<BIT.Player.Projectile>();

            SavePrefab(shuriken, $"{PREFABS_PATH}/Projectiles/Shuriken.prefab");
        }

        static void SavePrefab(GameObject obj, string path)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
                AssetDatabase.DeleteAsset(path);
            PrefabUtility.SaveAsPrefabAsset(obj, path);
            DestroyImmediate(obj);
        }

        // =======================================================================
        // CONFIGURAR ESCENA
        // =======================================================================

        static void SetupGameScene()
        {
            // Limpiar objetos (excepto camara)
            var allObjects = GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            foreach (var obj in allObjects)
            {
                if (obj.GetComponent<Camera>() == null && obj.transform.parent == null)
                    DestroyImmediate(obj);
            }

            // Asegurar que los tags existan
            EnsureTagsExist();

            SetupCamera();
            CreateTilemapFloor();
            CreateWalls();
            CreateDecorations(); // Nuevo: añade árboles y decoraciones
            SpawnPlayer();
            SpawnEnemies();
            SpawnPickups();
            CreateGameManager();

            Debug.Log("[Setup] Escena configurada");
        }

        static void EnsureTagsExist()
        {
            // Los tags se crean en TagSetup.cs, pero verificamos aquí
            string[] requiredTags = { "Player", "Enemy", "Coin", "Health", "Projectile" };
            foreach (var tag in requiredTags)
            {
                try
                {
                    // Intentar usar el tag para verificar si existe
                    var test = GameObject.FindGameObjectsWithTag(tag);
                }
                catch
                {
                    Debug.LogWarning($"[Setup] Tag '{tag}' no existe. Ejecuta BIT -> Setup Tags primero.");
                }
            }
        }

        static void CreateGameManager()
        {
            // Crear el RuntimeGameManager para UI y Audio
            GameObject managerGO = new GameObject("GameManager");
            managerGO.AddComponent<BIT.Core.RuntimeGameManager>();
            managerGO.AddComponent<BIT.Core.VFXManager>();
            Debug.Log("[Setup] RuntimeGameManager y VFXManager creados");
        }

        static void SetupCamera()
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            cam.orthographic = true;
            // Zoom x4 sobre tiles de 16px: 180px / 16 PPU = orthoSize 5.625
            cam.orthographicSize = 5.625f;
            cam.backgroundColor = new Color(0.08f, 0.10f, 0.08f);
            cam.transform.position = new Vector3(0, 0, -10);

            if (cam.GetComponent<BIT.Core.CameraFollow>() == null)
                cam.gameObject.AddComponent<BIT.Core.CameraFollow>();
        }

        // =======================================================================
        // MAPA CON TILESETS REALES (3 capas: Floor, FloorDetail, Walls)
        // Basado en la estructura del proyecto Godot de Ninja Adventure:
        //   - Floor (z=-2):       suelo continuo con variedad de tiles
        //   - FloorDetail (z=-1): detalles sobre el suelo (caminos, manchas)
        //   - Walls (z=0, Y-sort, collider): árboles y estructuras con colisión
        // Tamaño: 26x18 tiles visibles + 2 tiles de borde = 30x22 total
        // =======================================================================

        static void CreateTilemapFloor()
        {
            Sprite[] floorSprites  = LoadAllSprites($"{NINJA_PATH}/{TILESET_FLOOR}");
            Sprite[] natureSprites = LoadAllSprites($"{NINJA_PATH}/{TILESET_NATURE}");
            Sprite[] villageSprites= LoadAllSprites($"{NINJA_PATH}/Backgrounds/Tilesets/TilesetVillageAbandoned.png");

            if (floorSprites.Length == 0)
            {
                Debug.LogError("[Setup] No se encontraron sprites de TilesetFloor");
                return;
            }

            // ---- GRID ----
            GameObject gridGO = new GameObject("Grid");
            var grid = gridGO.AddComponent<Grid>();
            grid.cellSize = new Vector3(1, 1, 0);

            // ---- CAPA FLOOR (base, z=-2) ----
            var floorTM  = CreateTilemapLayer(gridGO, "Floor",       -2, false);
            // ---- CAPA FLOOR DETAIL (z=-1) ----
            var detailTM = CreateTilemapLayer(gridGO, "FloorDetail", -1, false);
            // ---- CAPA WALLS (z=0, Y-sort, con colisión) ----
            var wallTM   = CreateTilemapLayer(gridGO, "Walls",        0, true);

            // TilesetVillageAbandoned: 288px / 16 = 18 columnas, 160px / 16 = 10 filas
            // Estructura: fila 0-1 = muros/tejados, fila 2-3 = suelo piedra/madera, fila 4+ = tierra/caminos
            const int villageCols = 18;
            int natureCols = 24; // TilesetNature: 384px / 16 = 24 columnas

            // ---- SUELO: tiles de piedra/adoquín del Village (fila 2) ----
            Tile[] floorTiles = null;
            if (villageSprites.Length >= villageCols * 3)
            {
                // Fila 2 del Village = suelo de piedra/adoquín
                floorTiles = CreateTilesFromRow(villageSprites, villageCols * 2, villageCols, 4);
            }
            // Fallback: si no hay suficientes sprites de village, usar TilesetFloor
            if (floorTiles == null || floorTiles.Length == 0)
            {
                int floorCols = 22;
                int greenRow = FindGreenRow(LoadAllSprites($"{NINJA_PATH}/{TILESET_FLOOR}"), floorCols, targetGreen: true);
                floorTiles = CreateTilesFromRow(floorSprites, greenRow, floorCols, 3);
            }
            if (floorTiles == null || floorTiles.Length == 0)
                floorTiles = CreateTiles(villageSprites.Length > 0 ? villageSprites : floorSprites, new int[] { 0 });

            // ---- DETALLE: fila 3 del Village (tierra/caminos sobre piedra) ----
            Tile[] detailTiles = villageSprites.Length >= villageCols * 4
                ? CreateTilesFromRow(villageSprites, villageCols * 3, villageCols, 2)
                : null;

            // ---- BORDER: árboles verdes de TilesetNature (igual que antes) ----
            int greenNatureRow = FindGreenRow(natureSprites, natureCols, targetGreen: true, startCol: 4);
            Tile[] treeTiles   = CreateTilesFromRow(natureSprites, greenNatureRow, natureCols, 6);
            Tile[] bushTiles   = natureSprites.Length > greenNatureRow + natureCols * 3
                ? CreateTilesFromRow(natureSprites, greenNatureRow + natureCols * 2, natureCols, 4) : treeTiles;
            if (treeTiles == null || treeTiles.Length == 0)
                treeTiles = floorTiles;

            // ---- OBSTÁCULOS: muros/estructuras del Village (fila 0-1) ----
            Tile[] wallTiles = villageSprites.Length >= villageCols * 2
                ? CreateTilesFromRow(villageSprites, 0, villageCols, 4)
                : null;

            Debug.Log($"[Setup] Mapa Village: {floorTiles.Length} tiles de suelo, {treeTiles.Length} árboles de borde");

            // ==========================================
            // PINTADO DEL MAPA (30x22 tiles, esquina inferior izquierda en -15,-11)
            // Zona jugable: x [-13..13], y [-9..9]  (26x18 tiles)
            // Borde de árboles: x -14 y 14, y -10 y 10
            // ==========================================
            int mapMinX = -15, mapMaxX = 14;
            int mapMinY = -11, mapMaxY = 10;
            int innerMinX = -13, innerMaxX = 13;
            int innerMinY = -9,  innerMaxY = 9;

            var rng = new System.Random(42); // seed fija para reproducibilidad

            // 1. Pintar suelo base en toda la zona interior
            for (int x = innerMinX; x <= innerMaxX; x++)
            {
                for (int y = innerMinY; y <= innerMaxY; y++)
                {
                    // Variedad: alternar entre 2-3 tiles de hierba
                    Tile t = floorTiles[rng.Next(0, Mathf.Min(3, floorTiles.Length))];
                    floorTM.SetTile(new Vector3Int(x, y, 0), t);
                }
            }

            // 2. FloorDetail: camino diagonal/central con tiles distintos
            if (detailTiles != null && detailTiles.Length > 0)
            {
                // Camino horizontal en y=0
                for (int x = innerMinX + 2; x <= innerMaxX - 2; x++)
                {
                    if (rng.Next(0, 3) > 0) // 66% de probabilidad por tile
                        detailTM.SetTile(new Vector3Int(x, 0, 0), detailTiles[0]);
                }
                // Camino vertical en x=0
                for (int y = innerMinY + 2; y <= innerMaxY - 2; y++)
                {
                    if (rng.Next(0, 3) > 0)
                        detailTM.SetTile(new Vector3Int(0, y, 0), detailTiles.Length > 1 ? detailTiles[1] : detailTiles[0]);
                }
            }

            // 3. Borde de árboles (perimetro del mapa visible)
            Tile borderTile = (treeTiles != null && treeTiles.Length > 0) ? treeTiles[0]
                            : (wallTiles != null  && wallTiles.Length  > 0) ? wallTiles[0]
                            : floorTiles[0];
            Tile borderTile2 = (treeTiles != null && treeTiles.Length > 1) ? treeTiles[1] : borderTile;

            for (int x = mapMinX; x <= mapMaxX; x++)
            {
                for (int y = mapMinY; y <= mapMaxY; y++)
                {
                    bool isBorder = x <= innerMinX - 1 || x >= innerMaxX + 1
                                 || y <= innerMinY - 1 || y >= innerMaxY + 1;
                    if (!isBorder) continue;

                    Tile bt = (rng.Next(0, 4) == 0) ? borderTile2 : borderTile;
                    wallTM.SetTile(new Vector3Int(x, y, 0), bt);
                    // También poner suelo debajo del árbol
                    floorTM.SetTile(new Vector3Int(x, y, 0), floorTiles[0]);
                }
            }

            // 4. Obstáculos interiores (grupos de árboles/rocas)
            int[][] obstacleGroups = {
                new int[] { -8,  5 }, new int[] { -7,  5 }, new int[] { -8,  4 },  // grupo NW
                new int[] {  7,  6 }, new int[] {  8,  6 },                          // grupo NE
                new int[] { -9, -5 }, new int[] { -9, -6 },                          // grupo SW
                new int[] {  6, -5 }, new int[] {  7, -5 }, new int[] {  7, -6 },   // grupo SE
                new int[] { -3,  3 },                                                // obstáculo centro-izq
                new int[] {  4, -3 },                                                // obstáculo centro-der
            };

            // Obstáculos: usar muros de village si están disponibles, si no usar árboles
            Tile obstacleTile = (wallTiles != null && wallTiles.Length > 0) ? wallTiles[0]
                              : (bushTiles != null && bushTiles.Length > 0) ? bushTiles[0]
                              : borderTile;
            foreach (var pos in obstacleGroups)
            {
                wallTM.SetTile(new Vector3Int(pos[0], pos[1], 0), obstacleTile);
                floorTM.SetTile(new Vector3Int(pos[0], pos[1], 0), floorTiles[0]);
            }

            // 5. Añadir TilemapCollider2D + CompositeCollider a la capa Walls
            var wallCollider = wallTM.gameObject.AddComponent<TilemapCollider2D>();
            wallCollider.compositeOperation = Collider2D.CompositeOperation.Merge;
            wallTM.gameObject.AddComponent<CompositeCollider2D>();
            // CompositeCollider2D añade Rigidbody2D automáticamente
            wallTM.gameObject.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;

            Debug.Log("[Setup] Mapa creado: 3 capas (Floor/FloorDetail/Walls) con tiles reales");
        }

        static Tilemap CreateTilemapLayer(GameObject parent, string name, int sortOrder, bool ySort)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform);
            var tm = go.AddComponent<Tilemap>();
            var tr = go.AddComponent<TilemapRenderer>();
            tr.sortingOrder = sortOrder;
            if (ySort)
            {
                tr.mode = TilemapRenderer.Mode.Individual;
            }
            return tm;
        }

        /// <summary>
        /// Encuentra el índice del primer sprite de la primera fila donde el color
        /// dominante es verde (o no verde si targetGreen=false).
        /// startCol: columna desde la que empezar a muestrear (para saltar cols vacías).
        /// </summary>
        static int FindGreenRow(Sprite[] sprites, int totalCols, bool targetGreen, int startCol = 0)
        {
            if (sprites.Length == 0) return 0;
            int rows = sprites.Length / totalCols;
            for (int row = 0; row < rows; row++)
            {
                int idx = row * totalCols + startCol;
                if (idx >= sprites.Length) break;
                Sprite s = sprites[idx];
                if (s == null) continue;
                Texture2D tex = s.texture;
                if (tex == null || !tex.isReadable) continue;
                Rect r = s.textureRect;
                Color c = tex.GetPixel((int)(r.x + r.width * 0.5f), (int)(r.y + r.height * 0.5f));
                bool isGreen = c.g > c.r + 0.05f && c.g > c.b + 0.05f && c.a > 0.5f;
                if (isGreen == targetGreen)
                    return row * totalCols;
            }
            return 0;
        }

        /// <summary>
        /// Crea tiles a partir de un índice de inicio (primer tile de una fila),
        /// cogiendo 'count' tiles comenzando por 'startCol' de esa fila.
        /// </summary>
        static Tile[] CreateTilesFromRow(Sprite[] sprites, int rowStartIdx, int totalCols, int count)
        {
            var result = new List<Tile>();
            for (int i = 0; i < count; i++)
            {
                int idx = rowStartIdx + i;
                if (idx >= sprites.Length || sprites[idx] == null) break;
                var t = ScriptableObject.CreateInstance<Tile>();
                t.sprite = sprites[idx];
                result.Add(t);
            }
            return result.ToArray();
        }

        static Tile[] CreateTiles(Sprite[] sprites, int[] indices)
        {
            var tiles = new List<Tile>();
            foreach (int i in indices)
            {
                if (i >= sprites.Length) continue;
                var t = ScriptableObject.CreateInstance<Tile>();
                t.sprite = sprites[i];
                tiles.Add(t);
            }
            return tiles.ToArray();
        }

        static void CreateWalls()
        {
            // Las paredes ahora están en el Tilemap Walls (creado en CreateTilemapFloor)
            // Este método queda vacío intencionalmente
        }

        static void CreateDecorations()
        {
            // Las decoraciones ahora están integradas en el Tilemap (capas FloorDetail y Walls)
            // Este método queda vacío intencionalmente
        }


        static void SpawnPlayer()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFABS_PATH}/Player/Player.prefab");
            if (prefab == null)
            {
                Debug.LogError("[Setup] No se encontro prefab Player!");
                return;
            }

            GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            player.transform.position = new Vector3(0, -3, 0);

            // Asignar proyectil al PlayerController
            var controller = player.GetComponent<BIT.Player.PlayerController>();
            if (controller != null)
            {
                controller.projectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFABS_PATH}/Projectiles/Shuriken.prefab");
            }
        }

        static void SpawnEnemies()
        {
            // Spawn 4 esqueletos
            SpawnEnemy("Enemy_Skeleton", new Vector3(-8, 6, 0));
            SpawnEnemy("Enemy_Skeleton", new Vector3(8, 5, 0));

            // Spawn 2 dragones
            SpawnEnemy("Enemy_Dragon", new Vector3(7, -6, 0));
            SpawnEnemy("Enemy_Dragon", new Vector3(-6, -7, 0));

            // Spawn 2 ciclopes
            SpawnEnemy("Enemy_Cyclope", new Vector3(-9, -1, 0));
            SpawnEnemy("Enemy_Cyclope", new Vector3(9, 2, 0));
        }

        static void SpawnEnemy(string prefabName, Vector3 position)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFABS_PATH}/Enemies/{prefabName}.prefab");
            if (prefab == null) return;

            GameObject enemy = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            enemy.transform.position = position;
        }

        static void SpawnPickups()
        {
            // Monedas en varias posiciones
            Vector3[] coinPositions = {
                new Vector3(-4, 2, 0), new Vector3(4, 3, 0), new Vector3(-8, -3, 0),
                new Vector3(8, -4, 0), new Vector3(0, 6, 0), new Vector3(-3, -6, 0),
                new Vector3(5, 0, 0), new Vector3(-6, 1, 0)
            };

            GameObject coinPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFABS_PATH}/Pickups/Coin.prefab");
            if (coinPrefab != null)
            {
                foreach (var pos in coinPositions)
                {
                    GameObject coin = (GameObject)PrefabUtility.InstantiatePrefab(coinPrefab);
                    coin.transform.position = pos;
                }
            }

            // Corazones (menos, mas valiosos)
            Vector3[] heartPositions = {
                new Vector3(-9, 7, 0), new Vector3(9, -7, 0), new Vector3(0, 0, 0)
            };

            GameObject heartPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFABS_PATH}/Pickups/Heart.prefab");
            if (heartPrefab != null)
            {
                foreach (var pos in heartPositions)
                {
                    GameObject heart = (GameObject)PrefabUtility.InstantiatePrefab(heartPrefab);
                    heart.transform.position = pos;
                }
            }
        }
    }
}
#endif
