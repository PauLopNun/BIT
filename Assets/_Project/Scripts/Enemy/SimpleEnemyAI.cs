using UnityEngine;

// ============================================================================
// SIMPLEENEMYAI.CS - IA simplificada de enemigo
// ============================================================================
// Sistema de IA que persigue al jugador automáticamente sin necesidad de
// configurar waypoints o layers en el Inspector. Ideal para prototipos.
//
// El enemigo:
// - Busca al jugador por tag "Player"
// - Lo persigue cuando está en rango de detección
// - Le hace daño por contacto
// - Puede recibir daño y morir
// ============================================================================

namespace BIT.Core
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class SimpleEnemyAI : MonoBehaviour
    {
        // ====================================================================
        // CONFIGURACIÓN
        // ====================================================================

        [Header("=== MOVIMIENTO ===")]
        [Tooltip("Velocidad de movimiento")]
        public float moveSpeed = 2f;

        [Tooltip("Rango de detección del jugador")]
        public float detectionRange = 8f;

        [Tooltip("Distancia mínima al jugador (para no pegarse)")]
        public float stoppingDistance = 0.8f;

        [Header("=== COMBATE ===")]
        [Tooltip("Daño que hace al jugador por contacto")]
        public int damage = 10;

        [Tooltip("Cooldown entre ataques (segundos)")]
        public float attackCooldown = 1f;

        [Tooltip("Vida máxima del enemigo")]
        public int maxHealth = 50;

        [Header("=== PUNTUACIÓN ===")]
        [Tooltip("Puntos que da al morir")]
        public int scoreValue = 100;

        // ====================================================================
        // VARIABLES PRIVADAS
        // ====================================================================

        private Transform _player;
        private Rigidbody2D _rb;
        private Animator _animator;
        private SpriteRenderer _spriteRenderer;
        private int _currentHealth;
        private float _lastAttackTime;
        private Vector2 _moveDirection;
        private bool _isDead = false;

        private float _strafeTimer;
        private float _strafeFrequency;
        private float _strafeAmplitude;
        private float _speedVariance;

        // Hash de parámetros del animator para optimización
        private static readonly int ANIM_SPEED = Animator.StringToHash("Speed");
        private static readonly int ANIM_MOVEX = Animator.StringToHash("MoveX");
        private static readonly int ANIM_MOVEY = Animator.StringToHash("MoveY");

        // ====================================================================
        // INICIALIZACIÓN
        // ====================================================================

        void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _animator = GetComponent<Animator>();
            _spriteRenderer = GetComponent<SpriteRenderer>();

            // Configurar Rigidbody
            _rb.gravityScale = 0f;
            _rb.freezeRotation = true;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        void Start()
        {
            _currentHealth = maxHealth;
            FindPlayer();

            // Each enemy gets a unique strafe pattern so they don't all move identically
            _strafeTimer = Random.Range(0f, Mathf.PI * 2f);
            _strafeFrequency = Random.Range(0.6f, 1.4f);
            _strafeAmplitude = Random.Range(0.25f, 0.55f);
            _speedVariance   = Random.Range(0.85f, 1.15f);

            // El RuntimeGameManager cuenta enemigos automáticamente en CountEnemies()
            // No necesitamos registrar manualmente
        }

        void FindPlayer()
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                _player = playerObj.transform;
            }
            else
            {
                Debug.LogWarning($"[SimpleEnemyAI] {gameObject.name}: No se encontró jugador con tag 'Player'");
            }
        }

        // ====================================================================
        // UPDATE - Animaciones
        // ====================================================================

        void Update()
        {
            if (_isDead) return;

            // Si perdimos la referencia al jugador, intentar encontrarlo
            if (_player == null)
            {
                FindPlayer();
                return;
            }

            // Actualizar animaciones
            UpdateAnimations();
        }

        void UpdateAnimations()
        {
            if (_animator == null) return;

            float speed = _rb.linearVelocity.magnitude;

            // Intentamos setear los parámetros (algunos animators pueden no tenerlos)
            try
            {
                _animator.SetFloat(ANIM_SPEED, speed);

                if (_moveDirection.magnitude > 0.1f)
                {
                    _animator.SetFloat(ANIM_MOVEX, _moveDirection.x);
                    _animator.SetFloat(ANIM_MOVEY, _moveDirection.y);
                }
            }
            catch { }

            // Voltear sprite según dirección
            if (_spriteRenderer != null && Mathf.Abs(_moveDirection.x) > 0.1f)
            {
                _spriteRenderer.flipX = _moveDirection.x < 0;
            }
        }

        // ====================================================================
        // FIXED UPDATE - Movimiento con Físicas
        // ====================================================================

        void FixedUpdate()
        {
            if (_isDead || _player == null) return;

            float distance = Vector2.Distance(transform.position, _player.position);

            // Si el jugador está en rango de detección y no demasiado cerca
            if (distance < detectionRange && distance > stoppingDistance)
            {
                _moveDirection = ((Vector2)_player.position - (Vector2)transform.position).normalized;

                // Perpendicular zigzag so enemies don't all rush in a straight line
                _strafeTimer += Time.fixedDeltaTime;
                Vector2 perp = new Vector2(-_moveDirection.y, _moveDirection.x);
                float strafe = Mathf.Sin(_strafeTimer * _strafeFrequency * Mathf.PI * 2f) * _strafeAmplitude;
                Vector2 finalDir = (_moveDirection + perp * strafe).normalized;

                _rb.linearVelocity = finalDir * (moveSpeed * _speedVariance);
            }
            else
            {
                // Fuera de rango o demasiado cerca - detenerse
                _rb.linearVelocity = Vector2.zero;
                _moveDirection = Vector2.zero;
            }

            // Daño por proximidad: si el jugador está muy cerca aunque no haya colisión física
            if (distance < stoppingDistance + 0.4f)
            {
                TryDamagePlayer(_player.gameObject);
            }
        }

        // ====================================================================
        // COLISIONES - Daño al jugador
        // ====================================================================

        void OnCollisionEnter2D(Collision2D collision)
        {
            TryDamagePlayer(collision.gameObject);
        }

        void OnCollisionStay2D(Collision2D collision)
        {
            TryDamagePlayer(collision.gameObject);
        }

        void TryDamagePlayer(GameObject other)
        {
            if (_isDead) return;

            if (other.CompareTag("Player"))
            {
                // Verificar cooldown de ataque
                if (Time.time - _lastAttackTime >= attackCooldown)
                {
                    _lastAttackTime = Time.time;

                    // Hacer daño al jugador
                    var playerController = other.GetComponent<BIT.Player.PlayerController>();
                    if (playerController != null)
                    {
                        playerController.TakeDamage(damage);
                        Debug.Log($"[SimpleEnemyAI] {gameObject.name} hizo {damage} de daño al jugador");
                    }
                }
            }
        }

        // ====================================================================
        // RECIBIR DAÑO
        // ====================================================================

        /// <summary>
        /// El enemigo recibe daño (llamado desde proyectiles o ataque melee)
        /// </summary>
        public void TakeDamage(int amount)
        {
            if (_isDead) return;

            _currentHealth -= amount;
            Debug.Log($"[SimpleEnemyAI] {gameObject.name} recibió {amount} de daño. Vida: {_currentHealth}/{maxHealth}");

            // Efecto visual de daño
            StartCoroutine(DamageFlash());

            // Efecto VFX
            if (VFXManager.Instance != null)
            {
                VFXManager.Instance.SpawnHitEffect(transform.position);
            }

            // Verificar muerte
            if (_currentHealth <= 0)
            {
                Die();
            }
        }

        System.Collections.IEnumerator DamageFlash()
        {
            if (_spriteRenderer == null) yield break;

            Color originalColor = _spriteRenderer.color;
            _spriteRenderer.color = Color.red;
            yield return new WaitForSeconds(0.1f);

            if (_spriteRenderer != null)
                _spriteRenderer.color = originalColor;
        }

        // ====================================================================
        // MUERTE
        // ====================================================================

        void Die()
        {
            if (_isDead) return;
            _isDead = true;

            Debug.Log($"[SimpleEnemyAI] {gameObject.name} ha muerto!");

            // Score con multiplicador de combo
            int finalScore = ComboManager.Instance != null
                ? ComboManager.Instance.RegisterKill(scoreValue)
                : scoreValue;

            if (_player != null)
                _player.GetComponent<BIT.Player.PlayerController>()?.AddScore(finalScore);

            // Drop de items al morir
            GetComponent<BIT.Enemy.EnemyDropper>()?.Drop();

            RuntimeGameManager.Instance?.PlayEnemyDeathSound();
            RuntimeGameManager.Instance?.OnEnemyKilled();
            VFXManager.Instance?.SpawnDeathEffect(transform.position);
            WaveManager.Instance?.NotifyEnemyDied(gameObject);

            var col = GetComponent<Collider2D>();
            if (col != null) col.enabled = false;
            _rb.linearVelocity = Vector2.zero;

            Destroy(gameObject, 0.5f);
        }

        // ====================================================================
        // ESCALADO (llamado desde WaveManager)
        // ====================================================================

        public void ScaleStats(float factor)
        {
            maxHealth = Mathf.RoundToInt(maxHealth * factor);
            _currentHealth = maxHealth;
            damage = Mathf.RoundToInt(damage * factor);
            moveSpeed = Mathf.Min(moveSpeed * (1f + (factor - 1f) * 0.35f), 8f);
        }

        // ====================================================================
        // GIZMOS
        // ====================================================================

        void OnDrawGizmosSelected()
        {
            // Rango de detección (amarillo)
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);

            // Distancia de parada (rojo)
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, stoppingDistance);
        }
    }
}
