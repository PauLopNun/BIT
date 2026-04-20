#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using BIT.Core;

// ============================================================================
// BITAUTOSETUP.CS — Automatiza TODO el setup de la escena BIT
// ============================================================================
// ORDEN DE USO (primera vez):
//   1. Menu → BIT → 1. Configurar Tilesets     (corta las PNG en tiles 16x16)
//   2. Menu → BIT → 2. Configurar Escena       (managers + mapa + jugador)
//
// Las siguientes veces solo hace falta el paso 2 para regenerar la escena.
// ============================================================================

namespace BIT.Editor
{
    public static class BITAutoSetup
    {
        // ====================================================================
        // PATHS
        // ====================================================================
        private const string INT_PATH  = "Assets/_Project/Sprites/Ninja Adventure/Backgrounds/Tilesets/Interior";
        private const string TILES_OUT = "Assets/_Project/Tiles/Interior";
        private const string PREFABS   = "Assets/_Project/Prefabs";

        // Tile assets generados (usados por DungeonMapGenerator)
        public const string TILE_FLOOR_0    = TILES_OUT + "/DungeonFloor_0.asset";
        public const string TILE_FLOOR_1    = TILES_OUT + "/DungeonFloor_1.asset";
        public const string TILE_FLOOR_2    = TILES_OUT + "/DungeonFloor_2.asset";
        public const string TILE_FLOOR_3    = TILES_OUT + "/DungeonFloor_3.asset";
        public const string TILE_FLOOR_4    = TILES_OUT + "/DungeonFloor_4.asset";
        public const string TILE_FLOOR_5    = TILES_OUT + "/DungeonFloor_5.asset";
        public const string TILE_WALL_0     = TILES_OUT + "/DungeonWall_0.asset";
        public const string TILE_WALL_1     = TILES_OUT + "/DungeonWall_1.asset";
        public const string TILE_WALLFACE_0 = TILES_OUT + "/DungeonWallFace_0.asset";
        public const string TILE_WALLFACE_1 = TILES_OUT + "/DungeonWallFace_1.asset";

        // ====================================================================
        // PASO 1: CORTAR TILESETS
        // ====================================================================

        [MenuItem("BIT/1. Configurar Tilesets (primera vez)")]
        public static void SetupTilesets()
        {
            EditorUtility.DisplayProgressBar("BIT – Tilesets", "Sliceando TilesetInteriorFloor.png…", 0.1f);
            SliceTileset(INT_PATH + "/TilesetInteriorFloor.png", "Floor");

            EditorUtility.DisplayProgressBar("BIT – Tilesets", "Sliceando TilesetWallSimple.png…", 0.35f);
            SliceTileset(INT_PATH + "/TilesetWallSimple.png", "Wall");

            EditorUtility.DisplayProgressBar("BIT – Tilesets", "Sliceando TilesetInterior.png…", 0.6f);
            SliceTileset(INT_PATH + "/TilesetInterior.png", "Interior");

            EditorUtility.DisplayProgressBar("BIT – Tilesets", "Creando tile assets…", 0.8f);
            CreateTileFolder();
            CreateTileAssets();

            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("BIT – Tilesets OK",
                "Tilesets cortados en tiles 16×16.\n\n" +
                "Assets creados en:\n  Assets/_Project/Tiles/Interior/\n\n" +
                "Ahora ejecuta: BIT → 2. Configurar Escena",
                "OK");
        }

        // ====================================================================
        // PASO 2: CONFIGURAR ESCENA
        // ====================================================================

        [MenuItem("BIT/2. Configurar Escena (Auto)")]
        public static void SetupScene()
        {
            // Auto-ejecutar paso 1 si los tiles no existen todavía
            if (!TilesReady())
            {
                bool run = EditorUtility.DisplayDialog("BIT – Tilesets necesarios",
                    "Los tile assets no están configurados.\n¿Ejecutar 'Configurar Tilesets' automáticamente?",
                    "Sí", "Cancelar");
                if (!run) return;
                SetupTilesets();
            }

            EditorUtility.DisplayProgressBar("BIT – Escena", "Configurando tags…", 0.05f);
            EnsureTags();

            EditorUtility.DisplayProgressBar("BIT – Escena", "Creando managers…", 0.15f);
            SetupManagers();

            EditorUtility.DisplayProgressBar("BIT – Escena", "Generando mapa dungeon…", 0.35f);
            DungeonMapGenerator.Generate();

            EditorUtility.DisplayProgressBar("BIT – Escena", "Colocando jugador…", 0.70f);
            PlacePlayer();

            EditorUtility.DisplayProgressBar("BIT – Escena", "Vinculando prefabs en WaveManager…", 0.85f);
            LinkPrefabsToWaveManager();

            EditorUtility.DisplayProgressBar("BIT – Escena", "Guardando…", 0.97f);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            EditorUtility.ClearProgressBar();

            EditorUtility.DisplayDialog("BIT – Escena Lista",
                "¡Escena configurada!\n\n" +
                "Managers añadidos:\n" +
                "  • RuntimeGameManager\n" +
                "  • WaveManager\n" +
                "  • ComboManager\n" +
                "  • LevelProgressionManager\n" +
                "  • WaveUpgradeSystem\n" +
                "  • VFXManager\n\n" +
                "Guarda la escena (Ctrl+S) y pulsa Play.",
                "OK");
        }

        // ====================================================================
        // TILESET SLICING
        // ====================================================================

        static void SliceTileset(string pngPath, string prefix)
        {
            const int TILE_W = 16;
            const int TILE_H = 16;

            var importer = AssetImporter.GetAtPath(pngPath) as TextureImporter;
            if (importer == null)
            {
                Debug.LogWarning($"[BITAutoSetup] No encontrado: {pngPath}");
                return;
            }

            importer.textureType            = TextureImporterType.Sprite;
            importer.spriteImportMode       = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit    = 16;
            importer.filterMode             = FilterMode.Point;
            importer.textureCompression     = TextureImporterCompression.Uncompressed;
            importer.isReadable             = true;
            importer.SaveAndReimport();

            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(pngPath);
            if (tex == null) return;

            int cols = tex.width  / TILE_W;
            int rows = tex.height / TILE_H;

            var factory = new SpriteDataProviderFactories();
            factory.Init();
            var dp = factory.GetSpriteEditorDataProviderFromObject(importer);
            dp.InitSpriteEditorDataProvider();

            var rects = new List<SpriteRect>();
            int idx = 0;
            // Orden: arriba→abajo, izquierda→derecha (Unity Y va de abajo hacia arriba)
            for (int row = rows - 1; row >= 0; row--)
            {
                for (int col = 0; col < cols; col++)
                {
                    rects.Add(new SpriteRect
                    {
                        name      = $"{prefix}_{idx}",
                        rect      = new Rect(col * TILE_W, row * TILE_H, TILE_W, TILE_H),
                        pivot     = new Vector2(0.5f, 0.5f),
                        alignment = SpriteAlignment.Center,
                        spriteID  = GUID.Generate()
                    });
                    idx++;
                }
            }

            dp.SetSpriteRects(rects.ToArray());
            dp.Apply();
            (dp.targetObject as TextureImporter)?.SaveAndReimport();

            Debug.Log($"[BITAutoSetup] {prefix}: {cols}×{rows} = {idx} tiles sliced de {pngPath}");
        }

        // ====================================================================
        // CREAR TILE ASSETS
        // ====================================================================

        static void CreateTileFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Tiles"))
                AssetDatabase.CreateFolder("Assets/_Project", "Tiles");
            if (!AssetDatabase.IsValidFolder(TILES_OUT))
                AssetDatabase.CreateFolder("Assets/_Project/Tiles", "Interior");
        }

        static void CreateTileAssets()
        {
            var floorSprites = SortedSprites(INT_PATH + "/TilesetInteriorFloor.png", "Floor");
            var wallSprites  = SortedSprites(INT_PATH + "/TilesetWallSimple.png",    "Wall");
            var faceSprites  = SortedSprites(INT_PATH + "/TilesetInterior.png",      "Interior");

            // 6 variantes de suelo (primera fila de TilesetInteriorFloor)
            for (int i = 0; i < 6; i++)
                MakeTileAsset(floorSprites, i, $"{TILES_OUT}/DungeonFloor_{i}.asset", Tile.ColliderType.None);

            // 2 variantes de pared (primera fila de TilesetWallSimple)
            for (int i = 0; i < 2; i++)
                MakeTileAsset(wallSprites, i, $"{TILES_OUT}/DungeonWall_{i}.asset", Tile.ColliderType.Sprite);

            // 2 variantes de cara de pared (primera fila de TilesetInterior)
            for (int i = 0; i < 2; i++)
                MakeTileAsset(faceSprites, i, $"{TILES_OUT}/DungeonWallFace_{i}.asset", Tile.ColliderType.None);
        }

        static Sprite[] SortedSprites(string pngPath, string expectedPrefix)
        {
            return AssetDatabase.LoadAllAssetsAtPath(pngPath)
                .OfType<Sprite>()
                .Where(s => s.name.StartsWith(expectedPrefix + "_"))
                .OrderBy(s =>
                {
                    var last = s.name.LastIndexOf('_');
                    return last >= 0 && int.TryParse(s.name.Substring(last + 1), out int n) ? n : 9999;
                })
                .ToArray();
        }

        static void MakeTileAsset(Sprite[] sprites, int index, string assetPath, Tile.ColliderType collider)
        {
            if (sprites == null || index >= sprites.Length || sprites[index] == null)
            {
                Debug.LogWarning($"[BITAutoSetup] Sprite index {index} no disponible para {assetPath}");
                return;
            }

            var existing = AssetDatabase.LoadAssetAtPath<Tile>(assetPath);
            if (existing != null)
            {
                existing.sprite       = sprites[index];
                existing.colliderType = collider;
                EditorUtility.SetDirty(existing);
                return;
            }

            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite       = sprites[index];
            tile.color        = Color.white;
            tile.colliderType = collider;
            AssetDatabase.CreateAsset(tile, assetPath);
        }

        // ====================================================================
        // SCENE MANAGERS
        // ====================================================================

        static void SetupManagers()
        {
            // WaveManager GO: agrupa todos los sistemas de oleadas
            var wmGO = GetOrCreate("WaveManager");
            EnsureComp<WaveManager>(wmGO);
            EnsureComp<ComboManager>(wmGO);
            EnsureComp<LevelProgressionManager>(wmGO);
            EnsureComp<WaveUpgradeSystem>(wmGO);

            // RuntimeGameManager (independiente, maneja UI y audio)
            var rgmGO = GetOrCreate("RuntimeGameManager");
            EnsureComp<RuntimeGameManager>(rgmGO);

            // VFXManager
            var vfxGO = GetOrCreate("VFXManager");
            EnsureComp<VFXManager>(vfxGO);
        }

        static void LinkPrefabsToWaveManager()
        {
            var wm = Object.FindFirstObjectByType<WaveManager>();
            if (wm == null) return;

            var so = new SerializedObject(wm);
            TrySetPrefab(so, "_basicEnemyPrefab", "Enemy_Skeleton");
            TrySetPrefab(so, "_fastEnemyPrefab",  "Enemy_Dragon");
            TrySetPrefab(so, "_tankEnemyPrefab",  "Enemy_Cyclope");
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void TrySetPrefab(SerializedObject so, string fieldName, string prefabSearchName)
        {
            var prop = so.FindProperty(fieldName);
            if (prop == null || prop.objectReferenceValue != null) return;

            var guids = AssetDatabase.FindAssets($"t:Prefab {prefabSearchName}", new[] { PREFABS });
            if (guids.Length == 0) return;

            prop.objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(
                AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        static void PlacePlayer()
        {
            if (GameObject.FindGameObjectWithTag("Player") != null) return;

            var guids = AssetDatabase.FindAssets("t:Prefab Player", new[] { PREFABS });
            if (guids.Length == 0) return;

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guids[0]));
            if (prefab == null) return;

            var go = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (go != null) go.transform.position = Vector3.zero;
        }

        // ====================================================================
        // TAGS
        // ====================================================================

        static void EnsureTags()
        {
            foreach (var tag in new[] { "Player", "Enemy", "Pickup", "Projectile" })
                AddTagIfMissing(tag);
        }

        static void AddTagIfMissing(string tag)
        {
            var so   = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var tags = so.FindProperty("tags");
            for (int i = 0; i < tags.arraySize; i++)
                if (tags.GetArrayElementAtIndex(i).stringValue == tag) return;
            tags.InsertArrayElementAtIndex(tags.arraySize);
            tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tag;
            so.ApplyModifiedProperties();
        }

        // ====================================================================
        // HELPERS
        // ====================================================================

        static bool TilesReady() =>
            AssetDatabase.LoadAssetAtPath<TileBase>(TILE_FLOOR_0) != null &&
            AssetDatabase.LoadAssetAtPath<TileBase>(TILE_WALL_0)  != null;

        static GameObject GetOrCreate(string name) =>
            GameObject.Find(name) ?? new GameObject(name);

        static T EnsureComp<T>(GameObject go) where T : Component =>
            go.GetComponent<T>() ?? go.AddComponent<T>();
    }
}
#endif
