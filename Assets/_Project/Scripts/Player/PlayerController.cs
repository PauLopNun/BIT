using UnityEngine;
using UnityEngine.InputSystem;

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
        public int meleeDamage = 15;

        [Tooltip("Radio del ataque melee")]
        public float meleeRange = 1.2f;

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
            rb.gravityScale = 0f;           // Sin gravedad (es top-down)
            rb.freezeRotation = true;        // No rotar al chocar
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

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
            // Si no hay Animator, salimos
            if (animator == null) return;

            // Velocidad de movimiento (para saber si está caminando o idle)
            float speed = moveInput.magnitude;

            // Intentamos setear los parámetros del Animator
            // Si no existen, no pasa nada (el try-catch previene errores)
            try
            {
                animator.SetFloat("Speed", speed);
                animator.SetFloat("MoveX", lastMoveDirection.x);
                animator.SetFloat("MoveY", lastMoveDirection.y);
            }
            catch { }
        }

        void UpdateSpriteDirection()
        {
            // Las animaciones de Walk/Idle ya tienen sprites por dirección (Left/Right/Up/Down)
            // No se necesita flipX
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

            // Efecto visual de slash
            if (BIT.Core.VFXManager.Instance != null)
            {
                BIT.Core.VFXManager.Instance.SpawnSlash(attackPos, lastMoveDirection);
            }

            // ATAQUE MELEE - Buscar enemigos cercanos y hacerles daño
            PerformMeleeAttack(attackPos);

            // Si tenemos proyectil configurado, lo lanzamos
            if (projectilePrefab != null)
            {
                LaunchProjectile();
            }

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
                    // Intentar hacer daño - primero con SimpleEnemyAI
                    var simpleEnemy = hit.GetComponent<BIT.Core.SimpleEnemyAI>();
                    if (simpleEnemy != null)
                    {
                        simpleEnemy.TakeDamage(meleeDamage);
                        enemiesHit++;
                        continue;
                    }

                    // Fallback a EnemyAI
                    var enemyAI = hit.GetComponent<BIT.Enemy.EnemyAI>();
                    if (enemyAI != null)
                    {
                        enemyAI.TakeDamage(meleeDamage);
                        enemiesHit++;
                        continue;
                    }

                    // Fallback a IDamageable
                    var damageable = hit.GetComponent<IDamageable>();
                    if (damageable != null)
                    {
                        damageable.TakeDamage(meleeDamage);
                        enemiesHit++;
                    }
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

            Color originalColor = spriteRenderer.color;
            spriteRenderer.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            spriteRenderer.color = originalColor;
        }

        void Die()
        {
            Debug.Log("[Player] ¡Game Over!");
            canMove = false;
            rb.linearVelocity = Vector2.zero;

            // Aquí podrías cargar la pantalla de Game Over
            // SceneManager.LoadScene("GameOver");
        }

        // ====================================================================
        // COLISIONES (Para recoger objetos)
        // ====================================================================

        void OnTriggerEnter2D(Collider2D other)
        {
            // Ejemplo: recoger moneda
            if (other.CompareTag("Coin"))
            {
                AddScore(100);

                // Efecto de particulas doradas
                if (BIT.Core.VFXManager.Instance != null)
                {
                    BIT.Core.VFXManager.Instance.SpawnPickupEffect(other.transform.position, new Color(1f, 0.9f, 0.2f));
                }

                Destroy(other.gameObject);
            }

            // Ejemplo: recoger corazón
            if (other.CompareTag("Health"))
            {
                Heal(25);

                // Efecto de particulas rosadas
                if (BIT.Core.VFXManager.Instance != null)
                {
                    BIT.Core.VFXManager.Instance.SpawnPickupEffect(other.transform.position, new Color(1f, 0.4f, 0.6f));
                }

                Destroy(other.gameObject);
            }
        }

        // ====================================================================
        // MODIFICADORES DE VELOCIDAD (Para power-ups y debuffs)
        // ====================================================================

        private float baseSpeed;
        private Coroutine speedCoroutine;

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
    }
}
