#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

// ============================================================================
// DUNGEONMAPGENERATOR.CS - Genera el mapa dungeon con los tiles del pack
// ============================================================================
// USAR: Menu -> BIT -> Regenerar Mapa Dungeon
//
// DISEÑO DEL MAPA (30x22 tiles totales):
//
//   ██████████████████████████████  ← paredes sólidas (2 filas)
//   ██░░░░░░░░░░░░░░░░░░░░░░░░░░██  ← cara interior de la pared
//   ██░f f f f f f f f f f f f░██  ← suelo interior
//   ██░f ■■    f f f f    ■■ f░██  ← pilares esquina arriba
//   ██░f f f f f f f f f f f f░██
//   ██░f f  ■  f f f f  ■  f f░██  ← pilares centro
//   ██░f f f f v v v v f f f f░██  ← suelo variado en el centro
//   ██░f f  ■  f f f f  ■  f f░██  ← pilares centro
//   ██░f ■■    f f f f    ■■ f░██  ← pilares esquina abajo
//   ██░f f f f f f f f f f f f░██
//   ██████████████████████████████  ← paredes sólidas (2 filas)
//
// CAPAS (3):
//   Floor      (z=-2, sin colisión): suelo de piedra + variedad
//   FloorDetail(z=-1, sin colisión): cara de pared + antorchas/elementos
//   Walls      (z= 0, con colisión): muros exteriores + pilares internos
// ============================================================================

namespace BIT.Editor
{
    public class DungeonMapGenerator : EditorWindow
    {
        // ====================================================================
        // PATHS DE LOS TILE ASSETS YA IMPORTADOS
        // ====================================================================
        private const string INT_PATH = "Assets/_Project/Sprites/Ninja Adventure/Backgrounds/Tilesets/Interior";
        private const string MAP_PATH = "Assets/_Project/Sprites/Ninja Adventure/map";

        // Tiles de suelo
        private const string T_FLOOR      = INT_PATH + "/TilesetInteriorFloor_0.asset";
        private const string T_WALL       = INT_PATH + "/TilesetWallSimple_0.asset";
        private const string T_WALL_FACE  = INT_PATH + "/TilesetInterior_0.asset";

        // Tiles de village para variedad de suelo (9 tiles: _0 a _8)
        private static string VillageT(int i) => $"{INT_PATH}/tileset_village_abandoned_{i}.asset";

        // Elementos decorativos (6 tiles: _0 a _5)
        private static string ElemT(int i) => $"{INT_PATH}/Elements_{i}.asset";

        // Tiles del mapa (importados en /map)
        private const string T_FLOOR_MINI   = MAP_PATH + "/tileset_interior_floor_0.asset";
        private const string T_FLOOR_SKINNY = MAP_PATH + "/tileskinny_0.asset";

        // ====================================================================
        // DIMENSIONES DEL MAPA
        // ====================================================================

        // Zona interior jugable
        private const int PLAY_LEFT   = -14;
        private const int PLAY_RIGHT  =  13;
        private const int PLAY_BOTTOM = -9;
        private const int PLAY_TOP    =  7;

        // Zona de paredes (2 filas arriba/abajo, 1 columna a los lados)
        private const int WALL_LEFT   = PLAY_LEFT  - 1;  // -15
        private const int WALL_RIGHT  = PLAY_RIGHT + 1;  //  14
        private const int WALL_BOTTOM = PLAY_BOTTOM - 2; // -11
        private const int WALL_TOP    = PLAY_TOP   + 2;  //   9

        // ====================================================================
        // MENÚ
        // ====================================================================

        [MenuItem("BIT/Regenerar Mapa Dungeon")]
        public static void Generate()
        {
            EditorUtility.DisplayProgressBar("BIT Dungeon", "Cargando tiles...", 0.1f);

            // ── cargar tiles ──────────────────────────────────────────────
            TileBase floor     = Load(T_FLOOR);
            TileBase wall      = Load(T_WALL);
            TileBase wallFace  = Load(T_WALL_FACE);
            TileBase floorMini = Load(T_FLOOR_MINI);
            TileBase floorSkin = Load(T_FLOOR_SKINNY);

            TileBase[] village = new TileBase[9];
            for (int i = 0; i < 9; i++) village[i] = Load(VillageT(i));

            TileBase[] elem = new TileBase[6];
            for (int i = 0; i < 6; i++) elem[i] = Load(ElemT(i));

            // Tile de suelo garantizado (fallback)
            TileBase safeFloor = floor ?? village[0];
            TileBase safeWall  = wall  ?? village[1];

            if (safeFloor == null)
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("Error", "No se encontró ningún tile de suelo.\nEjecuta primero BIT > Setup Ninja Adventure Scene.", "OK");
                return;
            }

            EditorUtility.DisplayProgressBar("BIT Dungeon", "Preparando escena...", 0.2f);

            // ── obtener / crear Grid ──────────────────────────────────────
            GameObject gridGO = GameObject.Find("Grid");
            if (gridGO == null)
            {
                gridGO = new GameObject("Grid");
                gridGO.AddComponent<Grid>().cellSize = Vector3.one;
            }

            // Eliminar tilemaps viejos (Floor, FloorDetail, Walls)
            for (int i = gridGO.transform.childCount - 1; i >= 0; i--)
            {
                var child = gridGO.transform.GetChild(i).gameObject;
                if (child.GetComponent<Tilemap>() != null)
                    DestroyImmediate(child);
            }

            // ── crear capas ───────────────────────────────────────────────
            Tilemap floorTM  = MakeLayer(gridGO, "Floor",       -2, false);
            Tilemap detailTM = MakeLayer(gridGO, "FloorDetail", -1, false);
            Tilemap wallsTM  = MakeLayer(gridGO, "Walls",        0, true);

            EditorUtility.DisplayProgressBar("BIT Dungeon", "Pintando suelo...", 0.4f);

            // ================================================================
            // CAPA FLOOR — suelo piedra en toda la zona jugable + bajo paredes
            // ================================================================

            // Suelo bajo toda la zona (incluyendo paredes para que no haya huecos)
            for (int x = WALL_LEFT; x <= WALL_RIGHT; x++)
                for (int y = WALL_BOTTOM; y <= WALL_TOP; y++)
                    floorTM.SetTile(new Vector3Int(x, y, 0), safeFloor);

            // Variedad en el suelo interior: patrón determinista basado en posición
            for (int x = PLAY_LEFT; x <= PLAY_RIGHT; x++)
            {
                for (int y = PLAY_BOTTOM; y <= PLAY_TOP; y++)
                {
                    TileBase t = PickFloorTile(x, y, safeFloor, floorMini, floorSkin, village);
                    floorTM.SetTile(new Vector3Int(x, y, 0), t);
                }
            }

            EditorUtility.DisplayProgressBar("BIT Dungeon", "Pintando detalles...", 0.55f);

            // ================================================================
            // CAPA FLOOR DETAIL — cara de pared superior + decoraciones
            // ================================================================

            // Cara de la pared norte (fila y=8, sobre el suelo jugable)
            if (wallFace != null)
            {
                for (int x = PLAY_LEFT; x <= PLAY_RIGHT; x++)
                    detailTM.SetTile(new Vector3Int(x, PLAY_TOP + 1, 0), wallFace);
            }

            // Antorchas y elementos a lo largo de los muros interiores
            // (se colocan en la fila del suelo junto a la pared, lado norte)
            PlaceElements(detailTM, elem, new Vector3Int[]
            {
                // Pared norte — antorchas simétricas
                new(-10, PLAY_TOP, 0),
                new( -5, PLAY_TOP, 0),
                new(  0, PLAY_TOP, 0),
                new(  4, PLAY_TOP, 0),
                new(  9, PLAY_TOP, 0),

                // Pared sur — espejadas
                new(-10, PLAY_BOTTOM, 0),
                new( -5, PLAY_BOTTOM, 0),
                new(  0, PLAY_BOTTOM, 0),
                new(  4, PLAY_BOTTOM, 0),
                new(  9, PLAY_BOTTOM, 0),

                // Pared oeste — lateral
                new(PLAY_LEFT, PLAY_TOP - 3, 0),
                new(PLAY_LEFT, 0,            0),
                new(PLAY_LEFT, PLAY_BOTTOM + 3, 0),

                // Pared este — lateral
                new(PLAY_RIGHT, PLAY_TOP - 3, 0),
                new(PLAY_RIGHT, 0,             0),
                new(PLAY_RIGHT, PLAY_BOTTOM + 3, 0),
            });

            EditorUtility.DisplayProgressBar("BIT Dungeon", "Pintando paredes...", 0.7f);

            // ================================================================
            // CAPA WALLS — paredes exteriores (colisión) + pilares interiores
            // ================================================================

            // Paredes norte (2 filas sólidas: y=8 y y=9)
            for (int x = WALL_LEFT; x <= WALL_RIGHT; x++)
                for (int y = PLAY_TOP + 1; y <= WALL_TOP; y++)
                    wallsTM.SetTile(new Vector3Int(x, y, 0), safeWall);

            // Paredes sur (2 filas sólidas: y=-10 y y=-11)
            for (int x = WALL_LEFT; x <= WALL_RIGHT; x++)
                for (int y = WALL_BOTTOM; y <= PLAY_BOTTOM - 1; y++)
                    wallsTM.SetTile(new Vector3Int(x, y, 0), safeWall);

            // Pared oeste (1 columna: x=-15)
            for (int y = PLAY_BOTTOM; y <= PLAY_TOP; y++)
                wallsTM.SetTile(new Vector3Int(WALL_LEFT, y, 0), safeWall);

            // Pared este (1 columna: x=14)
            for (int y = PLAY_BOTTOM; y <= PLAY_TOP; y++)
                wallsTM.SetTile(new Vector3Int(WALL_RIGHT, y, 0), safeWall);

            // ── Pilares interiores (cobertura durante el combate) ────────
            // 4 grupos en esquinas internas + 4 pilares centrales
            PlacePillars(wallsTM, safeWall, new Vector3Int[]
            {
                // Esquina interior noroeste (cluster 2x2)
                new(-11, 5, 0), new(-10, 5, 0),
                new(-11, 4, 0),

                // Esquina interior noreste
                new( 10, 5, 0), new( 9, 5, 0),
                new( 10, 4, 0),

                // Esquina interior suroeste
                new(-11, -5, 0), new(-10, -5, 0),
                new(-11, -4, 0),

                // Esquina interior sureste
                new( 10, -5, 0), new(  9, -5, 0),
                new( 10, -4, 0),

                // Pilares centrales (para cover a mitad de mapa)
                new(-4,  2, 0),
                new( 3,  2, 0),
                new(-4, -2, 0),
                new( 3, -2, 0),
            });

            // ── Colisión compuesta en la capa Walls ─────────────────────
            var col = wallsTM.gameObject.AddComponent<TilemapCollider2D>();
            col.compositeOperation = Collider2D.CompositeOperation.Merge;
            wallsTM.gameObject.AddComponent<CompositeCollider2D>();
            var rb = wallsTM.gameObject.GetComponent<Rigidbody2D>();
            if (rb != null) rb.bodyType = RigidbodyType2D.Static;

            EditorUtility.DisplayProgressBar("BIT Dungeon", "Guardando...", 0.95f);

            // Marcar escena como modificada
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();

            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog("BIT - Mapa Dungeon",
                "¡Mapa generado con éxito!\n\n" +
                "3 capas creadas:\n" +
                "  • Floor      (suelo piedra interior)\n" +
                "  • FloorDetail(cara de pared + antorchas)\n" +
                "  • Walls      (paredes + pilares, con colisión)\n\n" +
                "Guarda la escena con Ctrl+S antes de darle a Play.",
                "OK");

            Debug.Log("[DungeonMap] ¡Mapa dungeon generado! 30x22 tiles. 3 capas.");
        }

        // ====================================================================
        // HELPERS
        // ====================================================================

        /// Carga un TileBase asset por path. Devuelve null sin error fatal si falta.
        static TileBase Load(string path)
        {
            var t = AssetDatabase.LoadAssetAtPath<TileBase>(path);
            if (t == null)
                Debug.LogWarning($"[DungeonMap] Tile no encontrado: {path}");
            return t;
        }

        /// Crea una capa Tilemap como hijo del Grid.
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

        /// Selecciona el tile de suelo según una función hash de la posición
        /// para conseguir variedad sin aleatoriedad incontrolada.
        static TileBase PickFloorTile(int x, int y,
            TileBase floor, TileBase mini, TileBase skinny, TileBase[] village)
        {
            // Hash determinista
            int h = (x * 31 + y * 17 + x * y) & 0x7FFFFFFF;

            // Zona central: patrón de village para distinguirla visualmente
            bool inCenter = x >= -6 && x <= 5 && y >= -4 && y <= 3;

            if (inCenter)
            {
                // Tablero de ajedrez de 2 tiles para el centro de la arena
                int vIdx = ((x + y) % 2 == 0) ? 4 : 5;
                if (village[vIdx] != null) return village[vIdx];
            }

            // Fuera del centro: 80% suelo base, 10% mini, 10% village variado
            int r = h % 10;
            if (r == 0 && mini != null)   return mini;
            if (r == 1 && village[2] != null) return village[2];
            if (r == 2 && village[3] != null) return village[3];
            return floor;
        }

        /// Coloca elementos decorativos en la capa Detail, alternando entre los 6 tiles.
        static void PlaceElements(Tilemap tm, TileBase[] elem, Vector3Int[] positions)
        {
            int count = 0;
            foreach (var pos in positions)
            {
                TileBase e = null;
                // Buscar el primer elem disponible empezando por el índice actual
                for (int attempt = 0; attempt < elem.Length; attempt++)
                {
                    e = elem[(count + attempt) % elem.Length];
                    if (e != null) break;
                }
                if (e != null)
                    tm.SetTile(pos, e);
                count++;
            }
        }

        /// Coloca pilares sólidos en la capa Walls.
        static void PlacePillars(Tilemap tm, TileBase pillarTile, Vector3Int[] positions)
        {
            foreach (var pos in positions)
                tm.SetTile(pos, pillarTile);
        }
    }
}
#endif
