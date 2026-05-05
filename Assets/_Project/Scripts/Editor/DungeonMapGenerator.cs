#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine.SceneManagement;

// ============================================================================
// DUNGEONMAPGENERATOR.CS — Carga sampleMap.tmx del pack Kenney Tiny Dungeon
// ============================================================================
// Colisión acorde al mapa:
//   Tiles  1-42 → paredes (piedra + vallas) — TilemapCollider2D
//   Tiles 43+  → suelo arenoso — sin colisión
//   + 4 BoxCollider2D en los bordes donde el mapa está abierto
// Spawn points generados automáticamente en zonas 3×3 de suelo libre.
// ============================================================================

namespace BIT.Editor
{
    public class DungeonMapGenerator : EditorWindow
    {
        private const string KENNEY_PNG = "Assets/kenney_tiny-dungeon/Tilemap/tilemap_packed.png";
        private const string KENNEY_TMX = "kenney_tiny-dungeon/Tiled/sampleMap.tmx";

        private const int TILE_COLS   = 12;
        private const int TILE_ROWS   = 11;
        private const int TILE_PX     = 16;
        private const int PPU         = 16;
        // GIDs 1-36: bloques de piedra sólidos (bloquean). GIDs 37+: suelo y decoración (pisable).
        private const int WALL_ID_MAX = 36;

        [MenuItem("BIT/Regenerar Mapa Dungeon")]
        public static void Generate()
        {
            // ── 1. Verificar pack ────────────────────────────────────────────
            string tmxFull = Path.Combine(Application.dataPath,
                KENNEY_TMX.Replace('/', Path.DirectorySeparatorChar));

            if (!File.Exists(tmxFull))
            {
                EditorUtility.DisplayDialog("BIT – Pack no encontrado",
                    "No se encontró:\n  Assets/kenney_tiny-dungeon/Tiled/sampleMap.tmx",
                    "OK");
                return;
            }

            // ── 2. Tileset ───────────────────────────────────────────────────
            EditorUtility.DisplayProgressBar("BIT Dungeon", "Configurando tileset…", 0.05f);
            SetupKenneyTileset();
            AssetDatabase.Refresh();

            // ── 3. Sprites ───────────────────────────────────────────────────
            EditorUtility.DisplayProgressBar("BIT Dungeon", "Cargando sprites…", 0.15f);
            Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(KENNEY_PNG)
                .OfType<Sprite>()
                .OrderBy(s => {
                    int i = s.name.LastIndexOf('_');
                    return i >= 0 && int.TryParse(s.name.Substring(i + 1), out int n) ? n : 9999;
                }).ToArray();

            if (sprites.Length == 0)
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("BIT – Error",
                    "No se encontraron sprites en tilemap_packed.png.", "OK");
                return;
            }

            // ── 4. Parsear TMX ───────────────────────────────────────────────
            EditorUtility.DisplayProgressBar("BIT Dungeon", "Leyendo mapa…", 0.25f);
            if (!ParseTMX(tmxFull,
                    out uint[,] dungeon, out uint[,] _obj, out uint[,] _carts,
                    out int mapW, out int mapH))
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("BIT – Error TMX",
                    "No se pudo leer sampleMap.tmx.", "OK");
                return;
            }

            // ── 5. Escena ────────────────────────────────────────────────────
            EditorUtility.DisplayProgressBar("BIT Dungeon", "Preparando escena…", 0.38f);

            // Limpiar Grid anterior y SpawnPoints anteriores
            var oldGrid = GameObject.Find("Grid");
            if (oldGrid != null) UnityEngine.Object.DestroyImmediate(oldGrid);
            var oldSP = GameObject.Find("SpawnPoints");
            if (oldSP != null) UnityEngine.Object.DestroyImmediate(oldSP);

            var gridGO = new GameObject("Grid");
            gridGO.AddComponent<Grid>().cellSize = Vector3.one;

            Tilemap floorTM = MakeLayer(gridGO, "Floor", -2, false);
            // Paredes en sortOrder=-1 (detrás de personajes/items que van en 0).
            // Sin ySort para evitar el efecto de "hundirse" en paredes.
            Tilemap wallsTM = MakeLayer(gridGO, "Walls", -1, false);

            int ox = -mapW / 2;  // -16
            int oy = -mapH / 2;  // -10

            var cache = new Dictionary<uint, Tile>();

            // ── 6. Colocar tiles ─────────────────────────────────────────────
            EditorUtility.DisplayProgressBar("BIT Dungeon", "Colocando tiles…", 0.55f);
            PlaceDungeonLayer(dungeon, mapW, mapH, ox, oy, sprites, cache, floorTM, wallsTM);
            // Objects/Carts no se renderizan (personajitos estáticos del pack)

            // ── 7. Colisión en paredes ───────────────────────────────────────
            EditorUtility.DisplayProgressBar("BIT Dungeon", "Colisiones…", 0.70f);
            var rb = wallsTM.gameObject.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Static;
            var col = wallsTM.gameObject.AddComponent<TilemapCollider2D>();
            col.compositeOperation = Collider2D.CompositeOperation.Merge;
            // edgeRadius = 0: colisión exacta al tile visual.
            // Cualquier valor positivo crea espacio invisible que deja items inaccesibles en esquinas.
            wallsTM.gameObject.AddComponent<CompositeCollider2D>();

            // ── 8. Bordes invisibles (el mapa está abierto en algunos lados) ─
            CreateBoundaryWalls(gridGO, mapW, mapH, ox, oy);

            // ── 9. Spawn points seguros (escaneo automático del TMX) ─────────
            EditorUtility.DisplayProgressBar("BIT Dungeon", "Calculando spawn points…", 0.85f);
            var safePoints = FindSafeSpawnPoints(dungeon, mapW, mapH, ox, oy, 10);
            CreateSpawnPoints(safePoints);

            // ── 10. Guardar ──────────────────────────────────────────────────
            EditorUtility.DisplayProgressBar("BIT Dungeon", "Guardando…", 0.97f);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            EditorUtility.ClearProgressBar();

            EditorUtility.DisplayDialog("BIT – Mapa cargado",
                $"Mapa Kenney ({mapW}×{mapH} tiles)\n\n" +
                $"  Paredes: tiles 1-42 con TilemapCollider2D\n" +
                $"  Suelo: tiles 43+ sin colisión\n" +
                $"  Spawn points: {safePoints.Count} posiciones seguras\n\n" +
                "Ctrl+S para guardar. Luego Play.", "OK");

            Debug.Log($"[DungeonMap] {mapW}×{mapH}, {cache.Count} tile variants, {safePoints.Count} spawn points.");
        }

        // ====================================================================
        // TILESET SETUP
        // ====================================================================

        static void SetupKenneyTileset()
        {
            var imp = AssetImporter.GetAtPath(KENNEY_PNG) as TextureImporter;
            if (imp == null) return;

            imp.textureType         = TextureImporterType.Sprite;
            imp.spriteImportMode    = SpriteImportMode.Multiple;
            imp.spritePixelsPerUnit = PPU;
            imp.filterMode          = FilterMode.Point;
            imp.textureCompression  = TextureImporterCompression.Uncompressed;
            imp.isReadable          = false;
            var ts = new TextureImporterSettings();
            imp.ReadTextureSettings(ts);
            ts.spriteMeshType = SpriteMeshType.FullRect;
            imp.SetTextureSettings(ts);

            var factory = new SpriteDataProviderFactories();
            factory.Init();
            var dp = factory.GetSpriteEditorDataProviderFromObject(imp);
            dp.InitSpriteEditorDataProvider();

            var rects = new List<SpriteRect>();
            int idx = 0;
            for (int tRow = TILE_ROWS - 1; tRow >= 0; tRow--)
            for (int tCol = 0; tCol < TILE_COLS; tCol++)
            {
                rects.Add(new SpriteRect
                {
                    name      = $"tile_{idx}",
                    rect      = new Rect(tCol * TILE_PX, tRow * TILE_PX, TILE_PX, TILE_PX),
                    pivot     = new Vector2(0.5f, 0.5f),
                    alignment = SpriteAlignment.Center,
                    spriteID  = GUID.Generate()
                });
                idx++;
            }

            dp.SetSpriteRects(rects.ToArray());
            dp.Apply();
            (dp.targetObject as TextureImporter)?.SaveAndReimport();
        }

        // ====================================================================
        // TMX PARSER
        // ====================================================================

        static bool ParseTMX(string path,
            out uint[,] dungeon, out uint[,] objects, out uint[,] carts,
            out int width, out int height)
        {
            dungeon = objects = carts = null;
            width = height = 0;

            if (!File.Exists(path)) return false;

            try
            {
                var doc = new XmlDocument();
                doc.Load(path);
                var map = doc.DocumentElement;
                width  = int.Parse(map.Attributes["width"].Value);
                height = int.Parse(map.Attributes["height"].Value);

                dungeon = new uint[height, width];
                objects = new uint[height, width];
                carts   = new uint[height, width];

                foreach (XmlNode layer in map.SelectNodes("layer"))
                {
                    string name = layer.Attributes["name"].Value;
                    uint[,] target = name == "Dungeon" ? dungeon :
                                     name == "Objects" ? objects :
                                     name == "Carts"   ? carts   : null;
                    if (target == null) continue;

                    string csv = layer["data"].InnerText;
                    var tokens = csv.Split(new[] { ',', '\n', '\r', ' ' },
                        StringSplitOptions.RemoveEmptyEntries);

                    for (int i = 0; i < tokens.Length && i < width * height; i++)
                        if (uint.TryParse(tokens[i], out uint v))
                            target[i / width, i % width] = v;
                }

                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[DungeonMap] Error TMX: {e.Message}");
                return false;
            }
        }

        // ====================================================================
        // COLOCAR TILES
        // ====================================================================

        static void PlaceDungeonLayer(uint[,] data, int mapW, int mapH,
            int ox, int oy, Sprite[] sprites, Dictionary<uint, Tile> cache,
            Tilemap floorTM, Tilemap wallsTM)
        {
            for (int row = 0; row < mapH; row++)
            for (int col = 0; col < mapW; col++)
            {
                uint raw = data[row, col];
                if (raw == 0) continue;

                int gid = DecodeGid(raw, out bool fH, out bool fV, out bool fD);
                if (gid < 1 || gid > sprites.Length) continue;

                Tile tile = GetOrCreateTile(cache, raw, sprites[gid - 1], fH, fV, fD);
                var  pos  = new Vector3Int(ox + col, oy + (mapH - 1 - row), 0);

                bool isWall = gid <= WALL_ID_MAX;
                // Pilares aislados (pared sin ningún vecino de pared): solo visual,
                // sin colisión, para que el jugador pueda acercarse sin bloquearse.
                if (isWall && IsIsolatedWall(data, row, col, mapW, mapH))
                    isWall = false;

                (isWall ? wallsTM : floorTM).SetTile(pos, tile);
            }
        }

        // Un tile de pared es "aislado" si sus 4 vecinos cardinales son todos suelo.
        static bool IsIsolatedWall(uint[,] data, int row, int col, int mapW, int mapH)
        {
            int[] dr = { -1, 1,  0, 0 };
            int[] dc = {  0, 0, -1, 1 };
            for (int i = 0; i < 4; i++)
            {
                int nr = row + dr[i], nc = col + dc[i];
                if (nr < 0 || nr >= mapH || nc < 0 || nc >= mapW) continue;
                int nGid = (int)(data[nr, nc] & 0x1FFFFFFFu);
                if (nGid >= 1 && nGid <= WALL_ID_MAX) return false;
            }
            return true;
        }

        // ====================================================================
        // BORDES INVISIBLES
        // ====================================================================

        static void CreateBoundaryWalls(GameObject parent, int mapW, int mapH, int ox, int oy)
        {
            float l = ox, r = ox + mapW, b = oy, t = oy + mapH;
            float cx = (l + r) / 2f, cy = (b + t) / 2f;
            float w = r - l, h = t - b;
            const float K = 1f;

            var go = new GameObject("BoundaryWalls");
            go.transform.SetParent(parent.transform);
            Wall(go, "Top",    cx,       t + K/2f, w + K*2, K);
            Wall(go, "Bottom", cx,       b - K/2f, w + K*2, K);
            Wall(go, "Left",   l - K/2f, cy,       K,       h);
            Wall(go, "Right",  r + K/2f, cy,       K,       h);
        }

        static void Wall(GameObject parent, string name, float x, float y, float w, float h)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform);
            go.transform.position = new Vector3(x, y, 0f);
            go.AddComponent<BoxCollider2D>().size = new Vector2(w, h);
        }

        // ====================================================================
        // SPAWN POINTS SEGUROS (búsqueda automática)
        // ====================================================================

        // Busca spawn points que sean:
        //   1. Zona 3×3 de suelo (GID > WALL_ID_MAX)
        //   2. Alcanzables desde la posición del jugador mediante flood-fill
        // Esto garantiza que ni enemigos ni drops aparezcan en salas encerradas.
        static List<Vector3> FindSafeSpawnPoints(uint[,] dungeon,
            int mapW, int mapH, int ox, int oy, int targetCount = 10)
        {
            // Posición TMX del spawn del jugador: Unity(-7,-3) → col=9, row=12
            int playerCol = Mathf.Clamp(-7 - ox, 0, mapW - 1);
            int playerRow = Mathf.Clamp((mapH - 1) - (-3 - oy), 0, mapH - 1);

            // Flood-fill desde el jugador para saber qué celdas son alcanzables
            var reachable = new HashSet<(int, int)>();
            FloodFill(dungeon, mapW, mapH, playerCol, playerRow, reachable);

            if (reachable.Count == 0)
            {
                Debug.LogWarning("[DungeonMap] Flood-fill vacío — comprueba la posición de spawn del jugador.");
                reachable.Add((playerCol, playerRow));
            }

            // Candidatos: zona 3×3 de suelo dentro del área alcanzable
            var candidates = new List<(int col, int row)>();
            for (int row = 2; row < mapH - 2; row++)
            for (int col = 2; col < mapW - 2; col++)
            {
                if (!reachable.Contains((col, row))) continue;

                bool allFloor = true;
                for (int dr = -1; dr <= 1 && allFloor; dr++)
                for (int dc = -1; dc <= 1 && allFloor; dc++)
                {
                    uint raw = dungeon[row + dr, col + dc];
                    int gid = (int)(raw & 0x1FFFFFFFu);
                    if (gid >= 1 && gid <= WALL_ID_MAX) allFloor = false;
                }
                if (allFloor) candidates.Add((col, row));
            }

            if (candidates.Count == 0)
            {
                Debug.LogWarning("[DungeonMap] No hay spawn points alcanzables — usando posición del jugador.");
                return new List<Vector3> { new Vector3(-7f, -3f, 0f) };
            }

            var result = new List<Vector3>();
            float step = (float)candidates.Count / targetCount;
            for (int i = 0; i < targetCount; i++)
            {
                int idx = Mathf.Min((int)(i * step + step * 0.5f), candidates.Count - 1);
                var (c, r) = candidates[idx];
                result.Add(new Vector3(ox + c + 0.5f, oy + (mapH - 1 - r) + 0.5f, 0f));
            }

            Debug.Log($"[DungeonMap] {reachable.Count} celdas alcanzables, {candidates.Count} candidatos, {result.Count} spawn points.");
            return result;
        }

        // Flood-fill en 4 direcciones por celdas de suelo alcanzables.
        static void FloodFill(uint[,] dungeon, int mapW, int mapH,
            int startCol, int startRow, HashSet<(int, int)> visited)
        {
            var queue = new Queue<(int, int)>();
            if (!IsPassable(dungeon, startRow, startCol, mapW, mapH)) return;

            queue.Enqueue((startCol, startRow));
            visited.Add((startCol, startRow));

            int[] dc = { -1, 1,  0, 0 };
            int[] dr = {  0, 0, -1, 1 };

            while (queue.Count > 0)
            {
                var (c, r) = queue.Dequeue();
                for (int i = 0; i < 4; i++)
                {
                    int nc = c + dc[i], nr = r + dr[i];
                    if (nc < 0 || nc >= mapW || nr < 0 || nr >= mapH) continue;
                    if (visited.Contains((nc, nr))) continue;
                    if (!IsPassable(dungeon, nr, nc, mapW, mapH)) continue;
                    visited.Add((nc, nr));
                    queue.Enqueue((nc, nr));
                }
            }
        }

        // Celda pisable: suelo (GID > WALL_ID_MAX) o pared aislada (sin colisión).
        static bool IsPassable(uint[,] dungeon, int row, int col, int mapW, int mapH)
        {
            uint raw = dungeon[row, col];
            int gid = (int)(raw & 0x1FFFFFFFu);
            if (gid == 0) return false;
            if (gid > WALL_ID_MAX) return true;
            return IsIsolatedWall(dungeon, row, col, mapW, mapH);
        }

        // Crea GameObjects de spawn en la escena y los asigna al WaveManager.
        static void CreateSpawnPoints(List<Vector3> positions)
        {
            if (positions.Count == 0) return;

            var container = new GameObject("SpawnPoints");
            var transforms = new Transform[positions.Count];

            for (int i = 0; i < positions.Count; i++)
            {
                var sp = new GameObject($"SP_{i}");
                sp.transform.SetParent(container.transform);
                sp.transform.position = positions[i];
                transforms[i] = sp.transform;
            }

            var wm = UnityEngine.Object.FindFirstObjectByType<BIT.Core.WaveManager>();
            if (wm == null) return;

            var so = new SerializedObject(wm);
            var prop = so.FindProperty("_spawnPoints");
            if (prop == null) return;

            prop.ClearArray();
            for (int i = 0; i < transforms.Length; i++)
            {
                prop.InsertArrayElementAtIndex(i);
                prop.GetArrayElementAtIndex(i).objectReferenceValue = transforms[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ====================================================================
        // SYNC WAVEMANAGER
        // ====================================================================

        static void SyncWaveManagerSpawn(int mapW, int mapH)
        {
            var wm = UnityEngine.Object.FindFirstObjectByType<BIT.Core.WaveManager>();
            if (wm == null) return;
            var so = new SerializedObject(wm);
            var hw = so.FindProperty("_spawnAreaHalfWidth");
            var hh = so.FindProperty("_spawnAreaHalfHeight");
            if (hw != null) hw.floatValue = mapW / 2f - 3f;
            if (hh != null) hh.floatValue = mapH / 2f - 3f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ====================================================================
        // HELPERS
        // ====================================================================

        static int DecodeGid(uint raw, out bool flipH, out bool flipV, out bool flipD)
        {
            flipH = (raw & 0x80000000u) != 0;
            flipV = (raw & 0x40000000u) != 0;
            flipD = (raw & 0x20000000u) != 0;
            return (int)(raw & 0x1FFFFFFFu);
        }

        static Tile GetOrCreateTile(Dictionary<uint, Tile> cache, uint raw,
            Sprite sprite, bool fH, bool fV, bool fD)
        {
            if (cache.TryGetValue(raw, out Tile t)) return t;

            t = ScriptableObject.CreateInstance<Tile>();
            t.sprite       = sprite;
            t.color        = Color.white;
            t.colliderType = Tile.ColliderType.Sprite;
            t.flags        = TileFlags.LockTransform;
            t.transform    = BuildFlipMatrix(fH, fV, fD);

            cache[raw] = t;
            return t;
        }

        static Matrix4x4 BuildFlipMatrix(bool fH, bool fV, bool fD)
        {
            if (!fH && !fV && !fD) return Matrix4x4.identity;
            if (fD)
            {
                if ( fH &&  fV) return Matrix4x4.Rotate(Quaternion.Euler(0, 0, -90));
                if ( fH && !fV) return Matrix4x4.Rotate(Quaternion.Euler(0, 0,  90));
                if (!fH &&  fV) return Matrix4x4.Rotate(Quaternion.Euler(0, 0, -90))
                                       * Matrix4x4.Scale(new Vector3(-1, -1, 1));
                return             Matrix4x4.Rotate(Quaternion.Euler(0, 0,  90))
                                       * Matrix4x4.Scale(new Vector3(-1, -1, 1));
            }
            return Matrix4x4.Scale(new Vector3(fH ? -1f : 1f, fV ? -1f : 1f, 1f));
        }

        static Tilemap MakeLayer(GameObject parent, string name, int sortOrder, bool ySort)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform);
            var tm = go.AddComponent<Tilemap>();
            var tr = go.AddComponent<TilemapRenderer>();
            tr.sortingOrder = sortOrder;
            if (ySort) tr.mode = TilemapRenderer.Mode.Individual;
            return tm;
        }
    }
}
#endif
