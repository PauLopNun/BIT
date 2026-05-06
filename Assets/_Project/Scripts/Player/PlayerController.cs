using UnityEngine;
using UnityEngine.InputSystem;
using BIT.Core;

// ============================================================================
// PLAYERCONTROLLER.CS - Controlador principal del jugador
// ============================================================================
// Este script maneja el movimiento del personaje usando el NEW INPUT SYSTEM
// de Unity, que permite soporte para teclado, gamepad y móvil.
//
// REQUISITOS CUMPLIDOS:
// - 2.2: Personaje controlable con movimiento mediante teclado
// - 2.3: Animaciones de movimiento
// - 2.5: Lanzar objeto al pulsar tecla (Espacio para atacar)
// - 3.1: Preparado para móvil (New Input System)
//
// CONTROLES:
// - WASD o Flechas: Mover
// - Espacio o Click izquierdo: Atacar
// - E: Interactuar
// ============================================================================

namespace BIT.Player
{
    /// <summary>
    /// Controlador del personaje jugable.
    /// Usa el New Input System de Unity.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(PlayerInput))]
    public class PlayerController : MonoBehaviour
    {
        // ====================================================================
        // CONFIGURACIÓN EN EL INSPECTOR
        // ====================================================================

        [Header("=== MOVIMIENTO ===")]
        [Tooltip("Velocidad de movimiento del jugador")]
        public float moveSpeed = 5f;

        [Header("=== COMBATE MELEE ===")]
        [Tooltip("Daño del ataque melee")]
        public int meleeDamage = 25;

        [Tooltip("Radio del ataque melee")]
        public float meleeRange = 2.0f;

        [Tooltip("Tiempo entre ataques")]
        public float attackCooldown = 0.3f;

        [Header("=== PROYECTIL (Opcional) ===")]
        [Tooltip("Prefab del proyectil (opcional)")]
        public GameObject projectilePrefab;

        [Tooltip("Punto de disparo (opcional)")]
        public Transform firePoint;

        [Tooltip("Fuerza del proyectil")]
        public float projectileForce = 10f;

        [Header("=== ESTADÍSTICAS ===")]
        [Tooltip("Vida máxima")]
        public int maxHealth = 100;

        [Tooltip("Vida actual")]
        public int currentHealth = 100;

        [Tooltip("Puntuación")]
        public int score = 0;

        [Tooltip("Monedas acumuladas (se gastan en la tienda entre oleadas)")]
        public int coins = 0;

        [Header("=== SHURIKEN (Clic Derecho) ===")]
        [Tooltip("Daño del shuriken manual (sin carga)")]
        public int shurikenDamage = 30;
        [Tooltip("Velocidad del shuriken lanzado")]
        public float shurikenSpeed = 14f;
        [Tooltip("Cooldown entre shurikens manuales (segundos)")]
        public float shurikenCooldown = 0.45f;
        [Tooltip("Tiempo máximo de carga del shuriken (segundos)")]
        public float maxChargeTime = 1.5f;

        [Header("=== DASH (Shift) ===")]
        [Tooltip("Velocidad durante el dash")]
        public float dashSpeed = 18f;

        [Tooltip("Duración del dash en segundos")]
        public float dashDuration = 0.18f;

        [Tooltip("Cooldown del dash (segundos)")]
        public float dashCooldown = 3f;

        // ====================================================================
        // COMPONENTES (se obtienen automáticamente)
        // ====================================================================

        private Rigidbody2D rb;
        private Animator animator;
        private SpriteRenderer spriteRenderer;
        private PlayerInput playerInput;

        // Input Actions
        private InputAction moveAction;
        private InputAction attackAction;
        private InputAction interactAction;

        // ====================================================================
        // VARIABLES DE ESTADO
        // ====================================================================

        private Vector2 moveInput;
        private Vector2 lastMoveDirection = Vector2.down;
        private float lastAttackTime;
        private bool canMove = true;
        private bool _isDead = false;

        // Sprites de animación cargados desde los assets del personaje
        private Sprite[] _idleSprites;
        private Sprite[] _walkSprites;
        private Sprite[] _attackSprites;
        private float _animTimer;
        private int _animFrame;
        private bool _isAttackingAnim;
        private int _lastDirIndex = -1;
        private Color _baseColor = Color.white;

        // ====================================================================
        // INICIALIZACIÓN
        // ====================================================================

        void Awake()
        {
            // Obtenemos los componentes
            rb = GetComponent<Rigidbody2D>();
            animator = GetComponent<Animator>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            playerInput = GetComponent<PlayerInput>();

            // Configuramos el Rigidbody para juego top-down
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            // Aumentar el radio del collider para que el sprite no se meta visualmente en las paredes.
            // El sprite del ninja mide ~1 unidad; un radio de 0.42 lo cubre sin ser demasiado grande.
            var circ = GetComponent<CircleCollider2D>();
            if (circ != null) circ.radius = Mathf.Max(circ.radius, 0.42f);

            // Obtenemos las acciones del Input System
            if (playerInput != null && playerInput.actions != null)
            {
                moveAction = playerInput.actions["Move"];
                attackAction = playerInput.actions["Attack"];
                interactAction = playerInput.actions["Interact"];
            }
        }

        void OnEnable()
        {
            // Habilitamos las acciones
            if (moveAction != null) moveAction.Enable();
            if (attackAction != null)
            {
                attackAction.Enable();
                attackAction.performed += OnAttackPerformed;
            }
            if (interactAction != null)
            {
                interactAction.Enable();
                interactAction.performed += OnInteractPerformed;
            }
        }

        void OnDisable()
        {
            // Desuscribimos de los eventos de input
            if (attackAction != null)
            {
                attackAction.performed -= OnAttackPerformed;
            }
            if (interactAction != null)
            {
                interactAction.performed -= OnInteractPerformed;
            }
        }

        void Start()
        {
            // Inicializamos la vida
            currentHealth = maxHealth;

            // Debug para verificar el input
            if (playerInput == null)
                Debug.LogError("[Player] ERROR: No hay PlayerInput!");
            else if (playerInput.actions == null)
                Debug.LogError("[Player] ERROR: PlayerInput no tiene Actions asignadas!");
            else
            {
                Debug.Log("[Player] PlayerInput OK. Actions: " + playerInput.actions.name);
                if (moveAction == null)
                    Debug.LogError("[Player] ERROR: No se encontró la acción 'Move'!");
                else
                    Debug.Log("[Player] Acción Move encontrada: " + moveAction.name);
            }

            // Inicializar UI
            if (BIT.Core.RuntimeGameManager.Instance != null)
            {
                BIT.Core.RuntimeGameManager.Instance.SetHealth(currentHealth, maxHealth);
            }

            Debug.Log("[Player] Jugador inicializado con New Input System. Vida: " + currentHealth);

            // Aplicar stats del personaje seleccionado en la pantalla de selección
            ApplyCharacterData();
        }

        // ====================================================================
        // UPDATE - Lectura de Input y Animaciones
        // ====================================================================

        void Update()
        {
            // Si no podemos movernos, salimos
            if (!canMove) return;

            // --------------------------------------------------------
            // LECTURA DEL INPUT DE MOVIMIENTO (New Input System)
            // --------------------------------------------------------
            if (moveAction != null)
            {
                moveInput = moveAction.ReadValue<Vector2>();
            }

            // Normalizamos para que las diagonales no sean más rápidas
            if (moveInput.magnitude > 1f)
            {
                moveInput = moveInput.normalized;
            }

            // Guardamos la última dirección (para animaciones idle)
            if (moveInput.magnitude > 0.1f)
            {
                lastMoveDirection = moveInput.normalized;
            }

            // --------------------------------------------------------
            // ACTUALIZAR ANIMACIONES
            // --------------------------------------------------------
            UpdateAnimations();

            // --------------------------------------------------------
            // VOLTEAR SPRITE SEGÚN DIRECCIÓN
            // --------------------------------------------------------
            UpdateSpriteDirection();

            // --------------------------------------------------------
            // DASH ATTACK — solo Shift
            // --------------------------------------------------------
            if (_dashCooldownTimer  > 0f) _dashCooldownTimer  -= Time.deltaTime;
            if (_shurikenCooldownTimer > 0f) _shurikenCooldownTimer -= Time.deltaTime;

            if (!_isDashing && _dashCooldownTimer <= 0f && canMove
                && Keyboard.current != null && Keyboard.current.shiftKey.wasPressedThisFrame)
                StartCoroutine(DashAttack());

            // --------------------------------------------------------
            // SHURIKEN — Clic derecho (mantener para cargar, soltar para lanzar)
            // --------------------------------------------------------
            if (Mouse.current != null)
            {
                bool rmbPressed  = Mouse.current.rightButton.wasPressedThisFrame;
                bool rmbHeld     = Mouse.current.rightButton.isPressed;
                bool rmbReleased = Mouse.current.rightButton.wasReleasedThisFrame;

                // Iniciar carga al presionar
                if (rmbPressed && _shurikenCooldownTimer <= 0f && canMove && !_isDashing)
                {
                    _isChargingShuriken = true;
                    _chargeStartTime    = Time.time;
                    InitChargeIndicator();
                }

                // Actualizar indicador visual mientras se carga
                if (_isChargingShuriken && rmbHeld)
                {
                    float t = Mathf.Clamp01((Time.time - _chargeStartTime) / maxChargeTime);
                    UpdateChargeIndicator(t);
                }

                // Lanzar al soltar
                if (_isChargingShuriken && rmbReleased)
                {
                    float chargeT = Mathf.Clamp01((Time.time - _chargeStartTime) / maxChargeTime);
                    _isChargingShuriken = false;
                    DestroyChargeIndicator();
                    ThrowShuriken(chargeT);
                }

                // Cancelar carga si el jugador queda bloqueado
                if (_isChargingShuriken && (!canMove || _isDashing))
                {
                    _isChargingShuriken = false;
                    DestroyChargeIndicator();
                }
            }
        }

        // ====================================================================
        // INPUT CALLBACKS (New Input System)
        // ====================================================================

        private void OnAttackPerformed(InputAction.CallbackContext context)
        {
            if (canMove)
            {
                TryAttack();
            }
        }

        private void OnInteractPerformed(InputAction.CallbackContext context)
        {
            Debug.Log("[Player] Intentando interactuar...");
            // Aquí puedes añadir lógica de interacción
        }

        // ====================================================================
        // FIXED UPDATE - Movimiento con Físicas
        // ====================================================================

        void FixedUpdate()
        {
            if (!canMove) return;

            // Movemos usando el Rigidbody (respeta colisiones)
            Vector2 movement = moveInput * moveSpeed;
            rb.linearVelocity = movement;
        }

        // ====================================================================
        // ANIMACIONES
        // ====================================================================

        void UpdateAnimations()
        {
            if (animator != null)
            {
                try
                {
                    animator.SetFloat("Speed", moveInput.magnitude);
                    animator.SetFloat("MoveX", lastMoveDirection.x);
                    animator.SetFloat("MoveY", lastMoveDirection.y);
                }
                catch { }
            }

            if (spriteRenderer == null || _isAttackingAnim) return;

            bool moving = moveInput.magnitude > 0.1f;
            Sprite[] allFrames = moving ? _walkSprites : _idleSprites;
            if (allFrames == null || allFrames.Length == 0) return;

            Sprite[] frames = GetDirectionalFrames(allFrames);
            if (frames == null || frames.Length == 0) return;

            int dirIndex = CurrentDirIndex();
            if (dirIndex != _lastDirIndex)
            {
                _lastDirIndex = dirIndex;
                _animFrame = 0;
                _animTimer = 0f;
            }

            float fps = moving ? 10f : 4f;
            _animTimer += Time.deltaTime;
            if (_animTimer >= 1f / fps)
            {
                _animTimer = 0f;
                _animFrame = (_animFrame + 1) % frames.Length;
                spriteRenderer.sprite = frames[_animFrame];
            }
        }

        // NinjaAdventure: 4 rows (Down/Left/Right/Up) × N frames por fila
        int CurrentDirIndex()
        {
            float absX = Mathf.Abs(lastMoveDirection.x);
            float absY = Mathf.Abs(lastMoveDirection.y);
            if (absX > absY)
                return lastMoveDirection.x < 0f ? 1 : 2; // 1=Left, 2=Right
            return lastMoveDirection.y < 0f ? 0 : 3;     // 0=Down, 3=Up
        }

        Sprite[] GetDirectionalFrames(Sprite[] all)
        {
            if (all == null || all.Length < 4) return all;
            int framesPerDir = all.Length / 4;
            int start = CurrentDirIndex() * framesPerDir;
            var result = new Sprite[framesPerDir];
            for (int i = 0; i < framesPerDir; i++)
                result[i] = all[start + i];
            return result;
        }

        void UpdateSpriteDirection()
        {
            if (spriteRenderer == null) return;
            bool hasDirectionalSprites = (_walkSprites != null && _walkSprites.Length >= 4)
                                      || (_idleSprites != null && _idleSprites.Length >= 4);
            if (hasDirectionalSprites)
                spriteRenderer.flipX = false; // las filas del spritesheet ya tienen la dirección
            else if (Mathf.Abs(lastMoveDirection.x) > 0.1f)
                spriteRenderer.flipX = lastMoveDirection.x < 0f;
        }

        System.Collections.IEnumerator PlayAttackAnimation()
        {
            _isAttackingAnim = true;
            _animFrame = 0;

            if (_attackSprites != null && _attackSprites.Length > 0 && spriteRenderer != null)
            {
                // Play each attack frame, repeating the cycle once so it's visible
                int cycles = _attackSprites.Length <= 2 ? 2 : 1;
                for (int c = 0; c < cycles; c++)
                {
                    foreach (var frame in _attackSprites)
                    {
                        if (spriteRenderer == null) break;
                        spriteRenderer.sprite = frame;
                        yield return new WaitForSeconds(0.1f);
                    }
                }
            }
            else
            {
                yield return new WaitForSeconds(0.15f);
            }

            _isAttackingAnim = false;
        }

        // ====================================================================
        // COMBATE
        // ====================================================================

        void TryAttack()
        {
            // Verificamos el cooldown
            if (Time.time - lastAttackTime < attackCooldown) return;

            lastAttackTime = Time.time;

            // Animación de ataque (si existe el trigger)
            if (animator != null)
            {
                try { animator.SetTrigger("Attack"); } catch { }
            }

            // Sonido de ataque
            if (BIT.Core.RuntimeGameManager.Instance != null)
            {
                BIT.Core.RuntimeGameManager.Instance.PlayAttackSound();
            }

            // Posición del ataque (delante del jugador)
            Vector3 attackPos = transform.position + (Vector3)lastMoveDirection * 0.5f;

            // Animación de ataque del personaje (sprites Attack.png reales)
            StartCoroutine(PlayAttackAnimation());

            // Efecto visual de espada con Katana.png
            if (BIT.Core.VFXManager.Instance != null)
            {
                BIT.Core.VFXManager.Instance.SpawnMeleeSwordSwing(transform, lastMoveDirection);
            }

            // ATAQUE MELEE - Buscar enemigos cercanos y hacerles daño
            PerformMeleeAttack(attackPos);

            Debug.Log("[Player] ¡Ataque!");
        }

        /// <summary>
        /// Realiza un ataque melee, dañando a todos los enemigos en rango
        /// </summary>
        void PerformMeleeAttack(Vector3 attackPosition)
        {
            // Buscar todos los colliders en el rango de ataque
            Collider2D[] hitColliders = Physics2D.OverlapCircleAll(attackPosition, meleeRange);

            int enemiesHit = 0;
            foreach (Collider2D hit in hitColliders)
            {
                // Verificar si es un enemigo
                if (hit.CompareTag("Enemy"))
                {
                    var simpleEnemy = hit.GetComponent<BIT.Core.SimpleEnemyAI>();
                    if (simpleEnemy != null) { simpleEnemy.TakeDamage(meleeDamage); enemiesHit++; continue; }

                    var rangedEnemy = hit.GetComponent<BIT.Enemy.RangedEnemyAI>();
                    if (rangedEnemy != null) { rangedEnemy.TakeDamage(meleeDamage); enemiesHit++; continue; }

                    var enemyAI = hit.GetComponent<BIT.Enemy.EnemyAI>();
                    if (enemyAI != null) { enemyAI.TakeDamage(meleeDamage); enemiesHit++; continue; }

                    var damageable = hit.GetComponent<IDamageable>();
                    if (damageable != null) { damageable.TakeDamage(meleeDamage); enemiesHit++; }
                }
            }

            if (enemiesHit > 0)
            {
                Debug.Log($"[Player] Ataque melee impactó {enemiesHit} enemigo(s)!");
            }
        }

        void LaunchProjectile()
        {
            // Punto de spawn del proyectil
            Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;

            // Creamos el proyectil
            GameObject projectile = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);

            // Le damos velocidad
            Rigidbody2D projRb = projectile.GetComponent<Rigidbody2D>();
            if (projRb != null)
            {
                // Dirección hacia el ratón usando el New Input System
                Vector2 mouseScreenPos = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
                Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(new Vector3(mouseScreenPos.x, mouseScreenPos.y, 0));
                Vector2 direction = ((Vector2)mouseWorldPos - (Vector2)transform.position).normalized;

                projRb.linearVelocity = direction * projectileForce;

                // Rotamos el proyectil
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                projectile.transform.rotation = Quaternion.Euler(0, 0, angle);
            }

            // Auto-destrucción
            Destroy(projectile, 3f);
        }

        // ====================================================================
        // DAÑO Y CURACIÓN
        // ====================================================================

        /// <summary>
        /// Hace daño al jugador. Llamado por enemigos y trampas.
        /// </summary>
        public void TakeDamage(int damage)
        {
            if (_isDead) return;

            currentHealth -= damage;
            Debug.Log("[Player] Daño recibido: " + damage + ". Vida: " + currentHealth);

            // Efecto visual de daño
            StartCoroutine(DamageFlash());

            // Actualizar UI via RuntimeGameManager
            if (BIT.Core.RuntimeGameManager.Instance != null)
            {
                BIT.Core.RuntimeGameManager.Instance.SetHealth(currentHealth, maxHealth);
            }

            // Si muere
            if (currentHealth <= 0)
            {
                currentHealth = 0;
                Die();
            }
        }

        /// <summary>
        /// Cura al jugador.
        /// </summary>
        public void Heal(int amount)
        {
            currentHealth += amount;
            if (currentHealth > maxHealth) currentHealth = maxHealth;
            Debug.Log("[Player] Curado: +" + amount + ". Vida: " + currentHealth);

            // Actualizar UI
            if (BIT.Core.RuntimeGameManager.Instance != null)
            {
                BIT.Core.RuntimeGameManager.Instance.Heal(amount);
            }
        }

        /// <summary>
        /// Añade puntos a la puntuación.
        /// </summary>
        public void AddScore(int points)
        {
            score += points;
            Debug.Log("[Player] +" + points + " puntos. Total: " + score);

            // Actualizar UI
            if (BIT.Core.RuntimeGameManager.Instance != null)
            {
                BIT.Core.RuntimeGameManager.Instance.AddScore(points);
            }
        }

        System.Collections.IEnumerator DamageFlash()
        {
            if (spriteRenderer == null) yield break;
            _isAttackingAnim = false; // cancel attack tint if hit
            spriteRenderer.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            if (spriteRenderer != null) spriteRenderer.color = _baseColor;
        }

        void Die()
        {
            _isDead = true;
            Debug.Log("[Player] ¡Game Over!");
            canMove = false;
            rb.linearVelocity = Vector2.zero;

            // Aquí podrías cargar la pantalla de Game Over
            // SceneManager.LoadScene("GameOver");
        }

        // Las monedas y corazones los gestiona PickupBase — no hay sistema duplicado aquí.

        // ====================================================================
        // MODIFICADORES DE VELOCIDAD (Para power-ups y debuffs)
        // ====================================================================

        private float baseSpeed;
        private Coroutine speedCoroutine;

        // Dash state
        private bool _isDashing;
        private float _dashCooldownTimer;
        private float _shurikenCooldownTimer;

        // Kunai charge state
        private bool _isChargingShuriken;
        private float _chargeStartTime;
        private GameObject _chargeIndicatorGO;
        private SpriteRenderer _chargeIndicatorSR;

        /// <summary>
        /// Modifica la velocidad temporalmente (power-up o trampa de lentitud)
        /// </summary>
        public void ModifySpeed(float multiplier, float duration)
        {
            if (speedCoroutine != null) StopCoroutine(speedCoroutine);
            speedCoroutine = StartCoroutine(SpeedModifierRoutine(multiplier, duration));
        }

        System.Collections.IEnumerator SpeedModifierRoutine(float multiplier, float duration)
        {
            float originalSpeed = moveSpeed;
            moveSpeed = originalSpeed * multiplier;
            Debug.Log("[Player] Velocidad modificada: x" + multiplier);

            yield return new WaitForSeconds(duration);

            moveSpeed = originalSpeed;
            Debug.Log("[Player] Velocidad restaurada");
        }

        /// <summary>
        /// Devuelve este mismo controlador (compatibilidad con otros scripts)
        /// </summary>
        public PlayerController GetStats()
        {
            return this;
        }

        /// <summary>
        /// Quita puntos (para trampas)
        /// </summary>
        public void RemoveScore(int points)
        {
            score -= points;
            if (score < 0) score = 0;
            Debug.Log("[Player] -" + points + " puntos. Total: " + score);
        }

        // ====================================================================
        // SHURIKEN MANUAL — Clic derecho
        // ====================================================================

        private static Sprite _cachedShurikenSprite;

        void ThrowShuriken(float chargeT = 0f)
        {
            _shurikenCooldownTimer = shurikenCooldown;

            if (Mouse.current == null || Camera.main == null) return;

            // Multiplicadores según nivel de carga (0 = sin carga, 1 = carga máxima)
            int   finalDamage = Mathf.RoundToInt(shurikenDamage * (1f + chargeT * 3f));  // ×1 – ×4
            float finalSpeed  = shurikenSpeed  * (1f + chargeT * 0.5f);                 // ×1 – ×1.5
            float finalScale  = 1.0f           * (1f + chargeT * 0.3f);                 // 1.0 – 1.3

            Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
            Vector3 mouseWorldPos  = Camera.main.ScreenToWorldPoint(
                new Vector3(mouseScreenPos.x, mouseScreenPos.y, 0f));
            mouseWorldPos.z = 0f;
            Vector2 dir = ((Vector2)mouseWorldPos - (Vector2)transform.position).normalized;

            var bulletGO = new GameObject("Shuriken");
            bulletGO.transform.position = transform.position + (Vector3)dir * 0.5f;
            bulletGO.tag = "Projectile";

            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            bulletGO.transform.rotation = Quaternion.Euler(0f, 0f, angle);

            var sr = bulletGO.AddComponent<SpriteRenderer>();
            sr.color = Color.Lerp(new Color(0.85f, 0.9f, 1f), new Color(1f, 0.8f, 0.1f), chargeT);
            sr.sortingOrder = 20;
            bulletGO.transform.localScale = Vector3.one * finalScale;

            if (_cachedShurikenSprite == null)
            {
                // Prioridad: sprite individual limpio (no SpriteSheet) → frame 0 del SpriteSheet → BigShuriken
                string[] singles = {
                    "Assets/_Project/Sprites/Ninja Adventure/FX/Projectile/Shuriken.png",
                    "Assets/_Project/Sprites/Ninja Adventure/Items/Projectile/Shuriken.png",
                    "Assets/_Project/Sprites/Ninja Adventure/FX/Projectile/BigShuriken.png",
                };
                foreach (var p in singles)
                {
                    _cachedShurikenSprite = RuntimeAssetLoader.LoadFirstSprite(p);
                    if (_cachedShurikenSprite != null) break;
                }
                // Fallback: frame 0 del SpriteSheet (ordenado por índice para evitar frames transparentes)
                if (_cachedShurikenSprite == null)
                {
                    const string SHEET = "Assets/_Project/Sprites/Ninja Adventure/FX/Projectile/Shuriken/SpriteSheet.png";
                    _cachedShurikenSprite = RuntimeAssetLoader.LoadFirstSprite(SHEET);
                }
                Debug.Log($"[Player] Shuriken sprite: {(_cachedShurikenSprite != null ? _cachedShurikenSprite.name : "NULL — fallback estrella")}");
            }

            if (_cachedShurikenSprite != null)
            {
                sr.sprite = _cachedShurikenSprite;
            }
            else
            {
                // Fallback: estrella de 4 puntas
                var tex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
                var pixels = new Color[1024];
                var ctr = new UnityEngine.Vector2(15.5f, 15.5f);
                for (int i = 0; i < 1024; i++)
                {
                    int x = i % 32, y = i / 32;
                    float dx = x - ctr.x, dy = y - ctr.y;
                    float dist  = Mathf.Sqrt(dx * dx + dy * dy);
                    float ang   = Mathf.Atan2(Mathf.Abs(dy), Mathf.Abs(dx));
                    float rstar = 13f * (1f - 0.5f * Mathf.Sin(ang * 2f));
                    pixels[i] = dist <= rstar ? Color.white : Color.clear;
                }
                tex.SetPixels(pixels);
                tex.Apply();
                sr.sprite = Sprite.Create(tex, new Rect(0, 0, 32, 32), UnityEngine.Vector2.one * 0.5f, 32f);
            }

            var rb = bulletGO.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = false;
            rb.linearVelocity  = dir * finalSpeed;
            rb.angularVelocity = 480f + chargeT * 240f;

            var col = bulletGO.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius    = 0.5f;

            var bullet = bulletGO.AddComponent<PlayerBullet>();
            bullet.damage = finalDamage;

            Destroy(bulletGO, 4f);

            if (BIT.Core.RuntimeGameManager.Instance != null)
                BIT.Core.RuntimeGameManager.Instance.PlayAttackSound();
        }

        // ====================================================================
        // INDICADOR DE CARGA DEL SHURIKEN
        // ====================================================================

        void InitChargeIndicator()
        {
            _chargeIndicatorGO = new GameObject("ShurikenChargeIndicator");
            _chargeIndicatorGO.transform.SetParent(transform);
            _chargeIndicatorGO.transform.localPosition = Vector3.zero;
            _chargeIndicatorSR = _chargeIndicatorGO.AddComponent<SpriteRenderer>();
            _chargeIndicatorSR.sortingOrder = 15;
            _chargeIndicatorSR.sprite = CreateRingSprite();
            _chargeIndicatorGO.transform.localScale = Vector3.zero;
        }

        void UpdateChargeIndicator(float t)
        {
            if (_chargeIndicatorGO == null) return;
            float scale = Mathf.Lerp(0.4f, 2.0f, t);
            _chargeIndicatorGO.transform.localScale = Vector3.one * scale;
            // Amarillo → naranja → rojo según carga
            _chargeIndicatorSR.color = Color.Lerp(
                new Color(1f, 1f, 0.2f, 0.45f),
                new Color(1f, 0.2f, 0.1f, 0.85f), t);
        }

        void DestroyChargeIndicator()
        {
            if (_chargeIndicatorGO != null)
            {
                Destroy(_chargeIndicatorGO);
                _chargeIndicatorGO = null;
            }
        }

        static Sprite CreateRingSprite()
        {
            int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            float center = size * 0.5f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dist  = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(center, center));
                float inner = size * 0.32f;
                float outer = size * 0.48f;
                if (dist >= inner && dist <= outer)
                {
                    float alpha = 1f - Mathf.Abs(dist - (inner + outer) * 0.5f) / ((outer - inner) * 0.5f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
                else
                    tex.SetPixel(x, y, Color.clear);
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), Vector2.one * 0.5f, size);
        }

        // ====================================================================
        // DASH ATTACK — segundo tipo de ataque
        // ====================================================================
        // Shift: dash en la dirección de movimiento.
        // Durante el dash el jugador es invulnerable y daña al doble.
        // Cooldown: 3 segundos (configurable por personaje).

        System.Collections.IEnumerator DashAttack()
        {
            _isDashing = true;
            _dashCooldownTimer = dashCooldown;
            canMove = false;

            // Dirección: movimiento actual o última dirección guardada
            Vector2 dir = moveInput.sqrMagnitude > 0.1f ? moveInput.normalized : lastMoveDirection;

            // Flash cian para marcar el dash visualmente
            if (spriteRenderer != null) spriteRenderer.color = new Color(0.3f, 0.9f, 1f, 0.85f);

            // Sonido (reutiliza el de ataque; diferente pitch)
            if (BIT.Core.RuntimeGameManager.Instance != null)
                BIT.Core.RuntimeGameManager.Instance.PlayAttackSound();

            float elapsed = 0f;
            while (elapsed < dashDuration)
            {
                rb.linearVelocity = dir * dashSpeed;

                // Golpear enemigos en el área del dash
                var hits = Physics2D.OverlapCircleAll(transform.position, 0.7f);
                foreach (var hit in hits)
                {
                    if (!hit.CompareTag("Enemy")) continue;
                    int dashDmg = meleeDamage * 2;
                    var simpleAI = hit.GetComponent<BIT.Core.SimpleEnemyAI>();
                    if (simpleAI != null) { simpleAI.TakeDamage(dashDmg); continue; }
                    var rangedAI = hit.GetComponent<BIT.Enemy.RangedEnemyAI>();
                    if (rangedAI != null) { rangedAI.TakeDamage(dashDmg); continue; }
                    var enemyAI = hit.GetComponent<BIT.Enemy.EnemyAI>();
                    if (enemyAI != null) { enemyAI.TakeDamage(dashDmg); continue; }
                    var bossAI = hit.GetComponent<BIT.Enemy.BossEnemyAI>();
                    if (bossAI != null) bossAI.TakeDamage(dashDmg);
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            rb.linearVelocity = Vector2.zero;

            // Restaurar color del sprite
            if (spriteRenderer != null)
            {
                var csm = BIT.Core.CharacterSelectManager.Instance;
                spriteRenderer.color = csm?.SelectedCharacter != null
                    ? csm.SelectedCharacter.spriteColor
                    : Color.white;
            }

            // VFX del dash
            if (BIT.Core.VFXManager.Instance != null)
                BIT.Core.VFXManager.Instance.SpawnSlash(transform.position, dir);

            _isDashing = false;
            canMove = true;
        }

        // ====================================================================
        // APLICAR DATOS DEL PERSONAJE SELECCIONADO
        // ====================================================================

        void ApplyCharacterData()
        {
            var csm = BIT.Core.CharacterSelectManager.Instance;
            if (csm?.SelectedCharacter == null) return;

            var data = csm.SelectedCharacter;
            maxHealth       = data.maxHealth;
            currentHealth   = data.maxHealth;
            moveSpeed       = data.moveSpeed;
            meleeDamage     = data.meleeDamage;
            attackCooldown  = data.attackCooldown;
            meleeRange      = data.meleeRange;
            dashSpeed       = data.dashSpeed;
            dashDuration    = data.dashDuration;
            dashCooldown    = data.dashCooldown;

            if (spriteRenderer != null)
            {
                _baseColor = data.spriteColor;
                spriteRenderer.color = data.spriteColor;
                if (!string.IsNullOrEmpty(data.spritePath))
                {
                    Sprite sprite = RuntimeAssetLoader.LoadFirstSprite(data.spritePath);
                    if (sprite != null) spriteRenderer.sprite = sprite;
                }
            }

            // Actualizar UI con la nueva vida máxima
            if (BIT.Core.RuntimeGameManager.Instance != null)
                BIT.Core.RuntimeGameManager.Instance.SetHealth(currentHealth, maxHealth);

            // Cargar sprites de animación del personaje
            LoadCharacterSprites(data.spritePath);

            Debug.Log($"[Player] Personaje aplicado: {data.characterName} — HP:{maxHealth} SPD:{moveSpeed} DMG:{meleeDamage}");
        }

        // ====================================================================
        // CARGA DE SPRITES DE ANIMACIÓN
        // ====================================================================

        void LoadCharacterSprites(string idlePath)
        {
            if (string.IsNullOrEmpty(idlePath)) return;

            string dir = System.IO.Path.GetDirectoryName(idlePath).Replace("\\", "/");
            _idleSprites   = LoadSortedSprites(idlePath);
            _walkSprites   = LoadSortedSprites(dir + "/Walk.png");
            _attackSprites = LoadSortedSprites(dir + "/Attack.png");

            Debug.Log($"[Player] Sprites cargados — Idle:{_idleSprites?.Length} Walk:{_walkSprites?.Length} Attack:{_attackSprites?.Length}");
        }

        static Sprite[] LoadSortedSprites(string path)
        {
            var sprites = RuntimeAssetLoader.LoadSprites(path);
            return sprites != null && sprites.Length > 0 ? sprites : null;
        }
    }
}
