#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

// ============================================================================
// DUNGEONMAPGENERATOR.CS — Genera el mapa dungeon con tiles 16×16 correctos
// ============================================================================
// PREREQUISITO: ejecutar primero "BIT → 1. Configurar Tilesets"
// USO DIRECTO:  "BIT → Regenerar Mapa Dungeon"  (también llamado desde BITAutoSetup)
//
// DISEÑO DEL MAPA (30×22 tiles en total):
//
//  ██████████████████████████████  ← paredes sólidas (y = 8, 9)
//  █▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓█  ← cara decorativa de pared norte (y = 7)
//  █░░░░░░░░░░░░░░░░░░░░░░░░░░░█  ← suelo borde
//  █░░░■■░░░░░░░░░░░░░░░░░░■■░░█  ← pilares esquina NW / NE
//  █░░░■░░░░░░░░░░░░░░░░░░░░■░░█
//  █░░░░░░░▒▒▒▒▒▒▒▒▒▒▒▒░░░░░░░█  ← zona central arena (suelo variado)
//  █░░░░░░░▒▒░░░░░░░░▒▒░░░░░░░█
//  █░░░░░░░▒▒░░■░░░■░░▒▒░░░░░░█  ← pilares centrales
//  █░░░░░░░▒▒░░░░░░░░▒▒░░░░░░░█
//  █░░░░░░░▒▒▒▒▒▒▒▒▒▒▒▒░░░░░░░█
//  █░░░■░░░░░░░░░░░░░░░░░░░░■░░█
//  █░░░■■░░░░░░░░░░░░░░░░░░■■░░█  ← pilares esquina SW / SE
//  █░░░░░░░░░░░░░░░░░░░░░░░░░░░█  ← suelo borde
//  █▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓█  ← cara decorativa pared sur (y = -10)
//  ██████████████████████████████  ← paredes sólidas (y = -10, -11)
//
// CAPAS (3):
//   Floor       (z=-2, sin colisión): suelo de piedra con variedad
//   FloorDetail (z=-1, sin colisión): caras de pared + antorchas/elementos
//   Walls       (z= 0, con colisión): muros exteriores + pilares interiores
// ============================================================================

namespace BIT.Editor
{
    public class DungeonMapGenerator : EditorWindow
    {
        // ====================================================================
        // PATHS DE TILE ASSETS (generados por BITAutoSetup)
        // ====================================================================
        private const string TILES = "Assets/_Project/Tiles/Interior";
        private const string INT   = "Assets/_Project/Sprites/Ninja Adventure/Backgrounds/Tilesets/Interior";

        // Suelo (6 variantes de piedra de TilesetInteriorFloor)
        private static readonly string[] FLOOR_TILES =
        {
            TILES + "/DungeonFloor_0.asset",
            TILES + "/DungeonFloor_1.asset",
            TILES + "/DungeonFloor_2.asset",
            TILES + "/DungeonFloor_3.asset",
            TILES + "/DungeonFloor_4.asset",
            TILES + "/DungeonFloor_5.asset",
        };

        // Pared sólida
        private const string WALL_TILE     = TILES + "/DungeonWall_0.asset";
        // Cara decorativa de pared
        private const string FACE_TILE     = TILES + "/DungeonWallFace_0.asset";
        private const string FACE_TILE_ALT = TILES + "/DungeonWallFace_1.asset";
        // Elementos decorativos (ya existían correctamente cortados)
        private const string ELEM_PATH = INT + "/Elements_{0}.asset";

        // ====================================================================
        // DIMENSIONES DEL MAPA
        // ====================================================================
        private const int PLAY_LEFT   = -14;
        private const int PLAY_RIGHT  =  13;
        private const int PLAY_BOTTOM =  -9;
        private const int PLAY_TOP    =   7;

        private const int WALL_LEFT   = PLAY_LEFT  - 1;   // -15
        private const int WALL_RIGHT  = PLAY_RIGHT + 1;   //  14
        private const int WALL_BOTTOM = PLAY_BOTTOM - 2;  // -11
        private const int WALL_TOP    = PLAY_TOP   + 2;   //   9

        // ====================================================================
        // MENÚ
        // ====================================================================

        [MenuItem("BIT/Regenerar Mapa Dungeon")]
        public static void Generate()
        {
            EditorUtility.DisplayProgressBar("BIT Dungeon", "Cargando tiles…", 0.05f);

            // ── cargar tiles (con fallback a assets existentes) ───────────
            TileBase fallbackFloor = AssetDatabase.LoadAssetAtPath<TileBase>(INT + "/TilesetInteriorFloor_0.asset");
            TileBase fallbackWall  = AssetDatabase.LoadAssetAtPath<TileBase>(INT + "/TilesetWallSimple_0.asset");
            TileBase fallbackFace  = AssetDatabase.LoadAssetAtPath<TileBase>(INT + "/TilesetInterior_0.asset");

            TileBase wall     = AssetDatabase.LoadAssetAtPath<TileBase>(WALL_TILE)     ?? fallbackWall;
            TileBase wallFace = AssetDatabase.LoadAssetAtPath<TileBase>(FACE_TILE)     ?? fallbackFace ?? fallbackWall;
            TileBase faceAlt  = AssetDatabase.LoadAssetAtPath<TileBase>(FACE_TILE_ALT) ?? wallFace;

            var floorTiles = new TileBase[FLOOR_TILES.Length];
            for (int i = 0; i < FLOOR_TILES.Length; i++)
                floorTiles[i] = AssetDatabase.LoadAssetAtPath<TileBase>(FLOOR_TILES[i]) ?? fallbackFloor;

            var elem = new TileBase[6];
            for (int i = 0; i < 6; i++)
                elem[i] = AssetDatabase.LoadAssetAtPath<TileBase>(string.Format(ELEM_PATH, i));

            // Si no hay tiles en ningún lado, ofrecer ejecutar el setup
            if (wall == null || floorTiles[0] == null)
            {
                EditorUtility.ClearProgressBar();
                bool run = EditorUtility.DisplayDialog("BIT – Tiles no encontrados",
                    "Los tile assets (16×16) no están generados.\n\n" +
                    "Ejecuta primero: BIT → 1. Configurar Tilesets\n\n" +
                    "¿Ejecutarlo ahora?",
                    "Sí, configurar", "Cancelar");
                if (run) BITAutoSetup.SetupTilesets();
                return;
            }

            bool usingFallback = AssetDatabase.LoadAssetAtPath<TileBase>(WALL_TILE) == null;
            if (usingFallback)
                Debug.Log("[DungeonMap] Usando tiles de fallback. Ejecuta 'BIT → 1. Configurar Tilesets' para tiles 16x16 óptimos.");

            // Aliases con fallback
            TileBase safeWall   = wall;
            TileBase safeFace   = wallFace ?? faceAlt ?? wall;
            TileBase f0 = floorTiles[0];
            TileBase f1 = floorTiles[1] ?? f0;
            TileBase f2 = floorTiles[2] ?? f0;
            TileBase f3 = floorTiles[3] ?? f0;
            TileBase f4 = floorTiles[4] ?? f0;
            TileBase f5 = floorTiles[5] ?? f0;

            // ── obtener / crear Grid ──────────────────────────────────────
            EditorUtility.DisplayProgressBar("BIT Dungeon", "Preparando escena…", 0.15f);

            GameObject gridGO = GameObject.Find("Grid");
            if (gridGO == null)
            {
                gridGO = new GameObject("Grid");
                gridGO.AddComponent<Grid>().cellSize = Vector3.one;
            }

            // Eliminar capas Tilemap previas
            for (int i = gridGO.transform.childCount - 1; i >= 0; i--)
            {
                var child = gridGO.transform.GetChild(i).gameObject;
                if (child.GetComponent<Tilemap>() != null)
                    Object.DestroyImmediate(child);
            }

            // ── crear capas ───────────────────────────────────────────────
            Tilemap floorTM  = MakeLayer(gridGO, "Floor",       -2, false);
            Tilemap detailTM = MakeLayer(gridGO, "FloorDetail", -1, false);
            Tilemap wallsTM  = MakeLayer(gridGO, "Walls",        0, true);

            // ================================================================
            // CAPA FLOOR
            // ================================================================
            EditorUtility.DisplayProgressBar("BIT Dungeon", "Pintando suelo…", 0.30f);

            // Base sólida bajo toda la zona (incluye bajo las paredes)
            for (int x = WALL_LEFT; x <= WALL_RIGHT; x++)
                for (int y = WALL_BOTTOM; y <= WALL_TOP; y++)
                    floorTM.SetTile(new Vector3Int(x, y, 0), f0);

            // Variedad determinista en la zona jugable
            for (int x = PLAY_LEFT; x <= PLAY_RIGHT; x++)
                for (int y = PLAY_BOTTOM; y <= PLAY_TOP; y++)
                    floorTM.SetTile(new Vector3Int(x, y, 0),
                        PickFloorTile(x, y, f0, f1, f2, f3, f4, f5));

            // ================================================================
            // CAPA FLOOR DETAIL
            // ================================================================
            EditorUtility.DisplayProgressBar("BIT Dungeon", "Pintando detalles…", 0.50f);

            // Cara de la pared NORTE (fila y = PLAY_TOP = 7, visible desde el área jugable)
            for (int x = PLAY_LEFT; x <= PLAY_RIGHT; x++)
                detailTM.SetTile(new Vector3Int(x, PLAY_TOP, 0), safeFace);

            // Cara de la pared SUR (fila y = PLAY_BOTTOM = -9, visible desde el área jugable)
            for (int x = PLAY_LEFT; x <= PLAY_RIGHT; x++)
                detailTM.SetTile(new Vector3Int(x, PLAY_BOTTOM, 0), safeFace);

            // Antorchas a lo largo de los cuatro muros interiores
            PlaceWallElements(detailTM, elem);

            // ================================================================
            // CAPA WALLS
            // ================================================================
            EditorUtility.DisplayProgressBar("BIT Dungeon", "Pintando paredes…", 0.68f);

            // Paredes NORTE (y = 8 y 9)
            for (int x = WALL_LEFT; x <= WALL_RIGHT; x++)
                for (int y = PLAY_TOP + 1; y <= WALL_TOP; y++)
                    wallsTM.SetTile(new Vector3Int(x, y, 0), safeWall);

            // Paredes SUR (y = -10 y -11)
            for (int x = WALL_LEFT; x <= WALL_RIGHT; x++)
                for (int y = WALL_BOTTOM; y <= PLAY_BOTTOM - 1; y++)
                    wallsTM.SetTile(new Vector3Int(x, y, 0), safeWall);

            // Pared OESTE (x = -15)
            for (int y = PLAY_BOTTOM; y <= PLAY_TOP; y++)
                wallsTM.SetTile(new Vector3Int(WALL_LEFT, y, 0), safeWall);

            // Pared ESTE (x = 14)
            for (int y = PLAY_BOTTOM; y <= PLAY_TOP; y++)
                wallsTM.SetTile(new Vector3Int(WALL_RIGHT, y, 0), safeWall);

            // Pilares interiores para cobertura en combate
            PlacePillars(wallsTM, safeWall);

            // ── Colisión compuesta ─────────────────────────────────────────
            EditorUtility.DisplayProgressBar("BIT Dungeon", "Configurando colisiones…", 0.85f);

            var rb = wallsTM.gameObject.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Static;
            var col = wallsTM.gameObject.AddComponent<TilemapCollider2D>();
            col.compositeOperation = Collider2D.CompositeOperation.Merge;
            wallsTM.gameObject.AddComponent<CompositeCollider2D>();

            // ── Guardar ───────────────────────────────────────────────────
            EditorUtility.DisplayProgressBar("BIT Dungeon", "Guardando…", 0.97f);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            EditorUtility.ClearProgressBar();

            EditorUtility.DisplayDialog("BIT – Mapa Dungeon",
                "¡Mapa dungeon generado con tiles correctos!\n\n" +
                "  • Floor        — 6 variantes de suelo 16×16\n" +
                "  • FloorDetail  — caras de pared + antorchas\n" +
                "  • Walls        — muros + 18 pilares de cobertura\n\n" +
                "Guarda la escena (Ctrl+S) antes de Play.",
                "OK");

            Debug.Log("[DungeonMap] Mapa generado. 30×22 tiles, tiles 16×16 correctos.");
        }

        // ====================================================================
        // SELECCIÓN DE TILE DE SUELO
        // ====================================================================

        static TileBase PickFloorTile(int x, int y,
            TileBase f0, TileBase f1, TileBase f2,
            TileBase f3, TileBase f4, TileBase f5)
        {
            // Borde de 1 tile pegado a las paredes: suelo base uniforme
            bool isBorder = x == PLAY_LEFT || x == PLAY_RIGHT ||
                            y == PLAY_TOP  || y == PLAY_BOTTOM;
            if (isBorder) return f0;

            // Zona central de la arena: tablero sutil de dos variantes
            bool inCenter = x >= -6 && x <= 5 && y >= -4 && y <= 3;
            if (inCenter) return ((x + y) & 1) == 0 ? f1 : f2;

            // Resto: variedad basada en hash de posición (rareza ~25%)
            int h = (x * 73856093 ^ y * 19349663) & 0x7FFFFFFF;
            return (h % 12) switch
            {
                0 => f3,
                1 => f4,
                2 => f5,
                _ => f0
            };
        }

        // ====================================================================
        // ELEMENTOS DECORATIVOS
        // ====================================================================

        static void PlaceWallElements(Tilemap tm, TileBase[] elem)
        {
            int count = 0;

            // Norte: a lo largo de y = PLAY_TOP, cada 4 tiles en X
            for (int x = PLAY_LEFT + 2; x <= PLAY_RIGHT - 2; x += 4)
                SetElem(tm, elem, x, PLAY_TOP, ref count);

            // Sur: a lo largo de y = PLAY_BOTTOM, cada 4 tiles en X
            for (int x = PLAY_LEFT + 2; x <= PLAY_RIGHT - 2; x += 4)
                SetElem(tm, elem, x, PLAY_BOTTOM, ref count);

            // Oeste: a lo largo de x = PLAY_LEFT, cada 4 tiles en Y
            for (int y = PLAY_BOTTOM + 2; y <= PLAY_TOP - 2; y += 4)
                SetElem(tm, elem, PLAY_LEFT, y, ref count);

            // Este: a lo largo de x = PLAY_RIGHT, cada 4 tiles en Y
            for (int y = PLAY_BOTTOM + 2; y <= PLAY_TOP - 2; y += 4)
                SetElem(tm, elem, PLAY_RIGHT, y, ref count);
        }

        static void SetElem(Tilemap tm, TileBase[] elem, int x, int y, ref int count)
        {
            for (int a = 0; a < elem.Length; a++)
            {
                var e = elem[(count + a) % elem.Length];
                if (e != null) { tm.SetTile(new Vector3Int(x, y, 0), e); break; }
            }
            count++;
        }

        // ====================================================================
        // PILARES INTERIORES
        // ====================================================================
        //
        //  Esquina NW (L): (-12,5),(-11,5),(-12,4)
        //  Esquina NE (L): (10,5),(11,5),(11,4)
        //  Esquina SW (L): (-12,-5),(-11,-5),(-12,-4)
        //  Esquina SE (L): (10,-5),(11,-5),(11,-4)
        //  Laterales:      (-12,0) y (11,0)
        //  Centrales:      (-5,±2) y (4,±2)   ← "puertas" de combate

        static void PlacePillars(Tilemap tm, TileBase tile)
        {
            var positions = new Vector3Int[]
            {
                // Esquinas en L
                new(-12,  5, 0), new(-11,  5, 0), new(-12,  4, 0),   // NW
                new( 10,  5, 0), new( 11,  5, 0), new( 11,  4, 0),   // NE
                new(-12, -5, 0), new(-11, -5, 0), new(-12, -4, 0),   // SW
                new( 10, -5, 0), new( 11, -5, 0), new( 11, -4, 0),   // SE

                // Pilares laterales (cobertura a mitad de las paredes O/E)
                new(-12,  0, 0),
                new( 11,  0, 0),

                // Pilares centrales (crean dos "puertas" en el eje vertical)
                new(-5,  2, 0), new( 4,  2, 0),
                new(-5, -2, 0), new( 4, -2, 0),
            };

            foreach (var pos in positions)
                tm.SetTile(pos, tile);
        }

        // ====================================================================
        // HELPERS
        // ====================================================================

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
