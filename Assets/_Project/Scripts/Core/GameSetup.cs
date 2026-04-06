using UnityEngine;
using UnityEngine.InputSystem;

// ============================================================================
// GAMESETUP.CS - Configuración rápida de escena para pruebas
// ============================================================================
// Este script crea una escena jugable básica con sprites generados por código.
// Úsalo para probar el juego mientras descargas los assets de arte.
//
// USO: Crea un GameObject vacío y añade este script. Al dar Play, genera todo.
// ============================================================================

namespace BIT.Core
{
    public class GameSetup : MonoBehaviour
    {
        [Header("=== CONFIGURACIÓN ===")]
        public bool createPlayerOnStart = true;
        public bool createFloorOnStart = true;
        public bool createEnemyOnStart = true;
        public bool createPickupsOnStart = true;

        [Header("=== COLORES ===")]
        public Color playerColor = new Color(0.2f, 0.6f, 1f);     // Azul
        public Color floorColor = new Color(0.3f, 0.5f, 0.3f);    // Verde oscuro
        public Color wallColor = new Color(0.4f, 0.3f, 0.2f);     // Marrón
        public Color enemyColor = new Color(1f, 0.3f, 0.3f);      // Rojo
        public Color coinColor = new Color(1f, 0.9f, 0.2f);       // Amarillo
        public Color healthColor = new Color(1f, 0.4f, 0.6f);     // Rosa

        void Start()
        {
            Debug.Log("[GameSetup] Configurando escena de prueba...");

            // Configurar cámara
            SetupCamera();

            // Crear suelo
            if (createFloorOnStart)
            {
                CreateFloor();
            }

            // Crear jugador
            if (createPlayerOnStart)
            {
                CreatePlayer();
            }

            // Crear enemigo de prueba
            if (createEnemyOnStart)
            {
                CreateEnemy();
            }

            // Crear pickups
            if (createPickupsOnStart)
            {
                CreatePickups();
            }

            Debug.Log("[GameSetup] Escena lista. Usa WASD para moverte, Espacio/Click para atacar.");
        }

        void SetupCamera()
        {
            Camera cam = Camera.main;
            if (cam != null)
            {
                cam.orthographic = true;
                cam.orthographicSize = 6f;
                cam.backgroundColor = new Color(0.15f, 0.15f, 0.2f); // Fondo oscuro
                cam.transform.position = new Vector3(0, 0, -10);
            }
        }

        void CreateFloor()
        {
            // Crear suelo grande
            GameObject floor = CreateSprite("Floor", floorColor, new Vector3(0, 0, 0), new Vector3(20, 20, 1));
            floor.GetComponent<SpriteRenderer>().sortingOrder = -10;

            // Crear paredes
            CreateWall("WallTop", new Vector3(0, 10, 0), new Vector3(22, 2, 1));
            CreateWall("WallBottom", new Vector3(0, -10, 0), new Vector3(22, 2, 1));
            CreateWall("WallLeft", new Vector3(-10, 0, 0), new Vector3(2, 22, 1));
            CreateWall("WallRight", new Vector3(10, 0, 0), new Vector3(2, 22, 1));

            // Crear algunos obstáculos
            CreateWall("Obstacle1", new Vector3(-3, 3, 0), new Vector3(2, 2, 1));
            CreateWall("Obstacle2", new Vector3(4, -2, 0), new Vector3(3, 1, 1));
            CreateWall("Obstacle3", new Vector3(2, 4, 0), new Vector3(1, 3, 1));
        }

        void CreateWall(string name, Vector3 position, Vector3 scale)
        {
            GameObject wall = CreateSprite(name, wallColor, position, scale);
            BoxCollider2D col = wall.AddComponent<BoxCollider2D>();
            wall.GetComponent<SpriteRenderer>().sortingOrder = -5;
        }

        void CreatePlayer()
        {
            // Verificar si ya existe un jugador
            if (FindFirstObjectByType<BIT.Player.PlayerController>() != null)
            {
                Debug.Log("[GameSetup] Ya existe un jugador en la escena.");
                return;
            }

            // Crear jugador
            GameObject player = CreateSprite("Player", playerColor, new Vector3(0, -3, 0), new Vector3(1, 1.5f, 1));
            player.tag = "Player";
            player.layer = LayerMask.NameToLayer("Default");

            // Añadir componentes
            Rigidbody2D rb = player.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            CapsuleCollider2D col = player.AddComponent<CapsuleCollider2D>();
            col.size = new Vector2(0.8f, 1.2f);
            col.offset = new Vector2(0, 0.1f);

            // Añadir PlayerInput
            PlayerInput playerInput = player.AddComponent<PlayerInput>();

            // Buscar el InputActionAsset
            #if UNITY_EDITOR
            string[] guids = UnityEditor.AssetDatabase.FindAssets("PlayerInputActions t:InputActionAsset");
            if (guids.Length > 0)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                var inputAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);
                if (inputAsset != null)
                {
                    playerInput.actions = inputAsset;
                    playerInput.defaultActionMap = "Player";
                    Debug.Log("[GameSetup] PlayerInput configurado con InputActionAsset.");
                }
            }
            else
            {
                Debug.LogWarning("[GameSetup] No se encontró PlayerInputActions. Asigna el InputActionAsset manualmente al PlayerInput.");
            }
            #else
            Debug.LogWarning("[GameSetup] En build, asigna el InputActionAsset manualmente.");
            #endif

            // Añadir PlayerController
            player.AddComponent<BIT.Player.PlayerController>();

            // Hacer que la cámara siga al jugador (añadir CameraFollow a la cámara)
            if (Camera.main != null && Camera.main.GetComponent<CameraFollow>() == null)
            {
                Camera.main.gameObject.AddComponent<CameraFollow>();
            }

            player.GetComponent<SpriteRenderer>().sortingOrder = 5;
            Debug.Log("[GameSetup] Jugador creado.");
        }

        void CreateEnemy()
        {
            // Enemigo lejos del jugador para que no mate al inicio
            GameObject enemy = CreateSprite("Enemy", enemyColor, new Vector3(-7, 6, 0), new Vector3(1, 1.2f, 1));
            enemy.tag = "Enemy";

            Rigidbody2D rb = enemy.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;

            CircleCollider2D col = enemy.AddComponent<CircleCollider2D>();
            col.radius = 0.5f;

            // Añadir IA básica
            enemy.AddComponent<SimpleEnemyAI>();

            enemy.GetComponent<SpriteRenderer>().sortingOrder = 4;
            Debug.Log("[GameSetup] Enemigo creado.");
        }

        void CreatePickups()
        {
            // Monedas
            CreatePickup("Coin1", coinColor, new Vector3(-5, 2, 0), "Coin");
            CreatePickup("Coin2", coinColor, new Vector3(-4, -4, 0), "Coin");
            CreatePickup("Coin3", coinColor, new Vector3(6, -3, 0), "Coin");

            // Corazones
            CreatePickup("Health1", healthColor, new Vector3(-6, -2, 0), "Health");
            CreatePickup("Health2", healthColor, new Vector3(7, 5, 0), "Health");

            Debug.Log("[GameSetup] Pickups creados.");
        }

        void CreatePickup(string name, Color color, Vector3 position, string tag)
        {
            GameObject pickup = CreateSprite(name, color, position, new Vector3(0.6f, 0.6f, 1));
            pickup.tag = tag;

            CircleCollider2D col = pickup.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.3f;

            pickup.GetComponent<SpriteRenderer>().sortingOrder = 3;
        }

        GameObject CreateSprite(string name, Color color, Vector3 position, Vector3 scale)
        {
            GameObject obj = new GameObject(name);
            obj.transform.position = position;
            obj.transform.localScale = scale;

            SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = CreateColoredSprite(color);

            return obj;
        }

        Sprite CreateColoredSprite(Color color)
        {
            // Crear una textura de 32x32 píxeles
            Texture2D tex = new Texture2D(32, 32);
            tex.filterMode = FilterMode.Point;

            Color[] pixels = new Color[32 * 32];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }
            tex.SetPixels(pixels);
            tex.Apply();

            // Crear sprite desde la textura
            return Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32);
        }
    }

    // SimpleEnemyAI ahora está en Scripts/Enemy/SimpleEnemyAI.cs
}
