using UnityEngine;
using BIT.Player;
using BIT.Core;

// ============================================================================
// ENEMYAI.CS - Inteligencia Artificial de enemigo (Requisito Bonus 3.3)
// ============================================================================
// Este script implementa una IA básica con máquina de estados finitos (FSM).
// El enemigo puede PATRULLAR entre puntos y PERSEGUIR al jugador cuando
// este entra en su rango de detección.
//
// CONCEPTO CLAVE PARA DEFENSA ORAL:
// Una MÁQUINA DE ESTADOS FINITOS (FSM - Finite State Machine) es un patrón
// donde el objeto solo puede estar en UN estado a la vez, y hay reglas claras
// para cambiar de un estado a otro (transiciones).
//
// Estados de este enemigo:
// - IDLE: Esperando, no hace nada
// - PATROL: Caminando entre puntos de patrulla
// - CHASE: Persiguiendo al jugador
// - ATTACK: Atacando al jugador (cuando está muy cerca)
// - RETURN: Volviendo al punto de patrulla (si el jugador escapa)
//
// Transiciones:
// - IDLE -> PATROL: Cuando se activa
// - PATROL -> CHASE: Cuando detecta al jugador
// - CHASE -> ATTACK: Cuando está lo suficientemente cerca
// - CHASE -> RETURN: Cuando pierde al jugador de vista
// - RETURN -> PATROL: Cuando llega al punto de patrulla
// ============================================================================

namespace BIT.Enemy
{
    /// <summary>
    /// Estados posibles del enemigo.
    /// </summary>
    public enum EnemyState
    {
        Idle,       // Quieto, sin hacer nada
        Patrol,     // Patrullando entre waypoints
        Chase,      // Persiguiendo al jugador
        Attack,     // Atacando
        Return      // Volviendo al punto de patrulla
    }

    /// <summary>
    /// IA de enemigo con máquina de estados.
    /// Requiere: Rigidbody2D, Collider2D
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class EnemyAI : MonoBehaviour, IDamageable
    {
        // ====================================================================
        // SECCIÓN 1: CONFIGURACIÓN GENERAL
        // ====================================================================

        [Header("=== ESTADÍSTICAS ===")]
        [Tooltip("Vida máxima del enemigo")]
        [SerializeField] private int _maxHealth = 50;

        [Tooltip("Daño que hace al jugador")]
        [SerializeField] private int _attackDamage = 10;

        [Tooltip("Velocidad de movimiento al patrullar")]
        [SerializeField] private float _patrolSpeed = 2f;

        [Tooltip("Velocidad de movimiento al perseguir")]
        [SerializeField] private float _chaseSpeed = 4f;

        [Header("=== DETECCIÓN ===")]
        [Tooltip("Radio de detección del jugador")]
        [SerializeField] private float _detectionRadius = 5f;

        [Tooltip("Radio para atacar")]
        [SerializeField] private float _attackRadius = 1f;

        [Tooltip("Radio para perder al jugador de vista")]
        [SerializeField] private float _loseTargetRadius = 8f;

        [Tooltip("Layer del jugador")]
        [SerializeField] private LayerMask _playerLayer;

        [Header("=== PATRULLA ===")]
        [Tooltip("Puntos de patrulla (vacío = estático)")]
        [SerializeField] private Transform[] _patrolPoints;

        [Tooltip("Tiempo de espera en cada punto")]
        [SerializeField] private float _waitTimeAtPoint = 1f;

        [Header("=== ATAQUE ===")]
        [Tooltip("Cooldown entre ataques")]
        [SerializeField] private float _attackCooldown = 1.5f;

        // ====================================================================
        // SECCIÓN 2: VARIABLES PRIVADAS
        // ====================================================================

        // Componentes
        private Rigidbody2D _rb;
        private Animator _animator;
        private SpriteRenderer _spriteRenderer;

        // Estado actual
        private EnemyState _currentState = EnemyState.Idle;

        // Estadísticas actuales
        private int _currentHealth;

        // Patrulla
        private int _currentPatrolIndex = 0;
        private float _waitTimer = 0f;
        private Vector3 _lastPatrolPosition;

        // Persecución
        private Transform _targetPlayer;
        private float _lastAttackTime;

        // Animador
        private static readonly int ANIM_SPEED = Animator.StringToHash("Speed");
        private static readonly int ANIM_ATTACK = Animator.StringToHash("Attack");
        private static readonly int ANIM_HURT = Animator.StringToHash("Hurt");
        private static readonly int ANIM_DIE = Animator.StringToHash("Die");

        // ====================================================================
        // SECCIÓN 3: INICIALIZACIÓN
        // ====================================================================

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _animator = GetComponent<Animator>();
            _spriteRenderer = GetComponent<SpriteRenderer>();

            // Configuramos el Rigidbody
            ConfigureRigidbody();

            // Inicializamos salud
            _currentHealth = _maxHealth;

            // Guardamos posición inicial para volver
            _lastPatrolPosition = transform.position;
        }

        private void Start()
        {
            // Si hay puntos de patrulla, empezamos a patrullar
            if (_patrolPoints != null && _patrolPoints.Length > 0)
            {
                ChangeState(EnemyState.Patrol);
            }
            else
            {
                ChangeState(EnemyState.Idle);
            }
        }

        private void ConfigureRigidbody()
        {
            _rb.bodyType = RigidbodyType2D.Dynamic;
            _rb.gravityScale = 0f;
            _rb.freezeRotation = true;
            _rb.linearDamping = 3f;
        }

        // ====================================================================
        // SECCIÓN 4: UPDATE - MÁQUINA DE ESTADOS
        // ====================================================================

        private void Update()
        {
            // Ejecutamos la lógica del estado actual
            switch (_currentState)
            {
                case EnemyState.Idle:
                    UpdateIdle();
                    break;

                case EnemyState.Patrol:
                    UpdatePatrol();
                    break;

                case EnemyState.Chase:
                    UpdateChase();
                    break;

                case EnemyState.Attack:
                    UpdateAttack();
                    break;

                case EnemyState.Return:
                    UpdateReturn();
                    break;
            }

            // Actualizamos animaciones
            UpdateAnimations();
        }

        // ====================================================================
        // SECCIÓN 5: LÓGICA DE CADA ESTADO
        // ====================================================================

        /// <summary>
        /// Estado IDLE: El enemigo está quieto.
        /// Busca al jugador y transiciona a CHASE si lo encuentra.
        /// </summary>
        private void UpdateIdle()
        {
            // Intentamos detectar al jugador
            if (TryDetectPlayer())
            {
                ChangeState(EnemyState.Chase);
            }
        }

        /// <summary>
        /// Estado PATROL: El enemigo se mueve entre waypoints.
        /// Transiciona a CHASE si detecta al jugador.
        /// </summary>
        private void UpdatePatrol()
        {
            // Primero, verificamos si hay jugador cerca
            if (TryDetectPlayer())
            {
                _lastPatrolPosition = transform.position;
                ChangeState(EnemyState.Chase);
                return;
            }

            // Si no hay puntos de patrulla, pasamos a idle
            if (_patrolPoints == null || _patrolPoints.Length == 0)
            {
                ChangeState(EnemyState.Idle);
                return;
            }

            // Obtenemos el punto de patrulla actual
            Transform targetPoint = _patrolPoints[_currentPatrolIndex];

            // Calculamos distancia al punto
            float distanceToPoint = Vector2.Distance(transform.position, targetPoint.position);

            // Si llegamos al punto
            if (distanceToPoint < 0.3f)
            {
                // Esperamos un tiempo
                _waitTimer += Time.deltaTime;

                if (_waitTimer >= _waitTimeAtPoint)
                {
                    // Avanzamos al siguiente punto
                    _currentPatrolIndex = (_currentPatrolIndex + 1) % _patrolPoints.Length;
                    _waitTimer = 0f;
                }
            }
            else
            {
                // Nos movemos hacia el punto
                MoveTowards(targetPoint.position, _patrolSpeed);
            }
        }

        /// <summary>
        /// Estado CHASE: El enemigo persigue al jugador.
        /// Transiciona a ATTACK si está lo suficientemente cerca.
        /// Transiciona a RETURN si pierde al jugador.
        /// </summary>
        private void UpdateChase()
        {
            // Si no tenemos target, volvemos
            if (_targetPlayer == null)
            {
                ChangeState(EnemyState.Return);
                return;
            }

            float distanceToPlayer = Vector2.Distance(transform.position, _targetPlayer.position);

            // Si el jugador está muy lejos, lo perdemos
            if (distanceToPlayer > _loseTargetRadius)
            {
                _targetPlayer = null;
                ChangeState(EnemyState.Return);
                return;
            }

            // Si estamos lo suficientemente cerca, atacamos
            if (distanceToPlayer <= _attackRadius)
            {
                ChangeState(EnemyState.Attack);
                return;
            }

            // Perseguimos al jugador
            MoveTowards(_targetPlayer.position, _chaseSpeed);
        }

        /// <summary>
        /// Estado ATTACK: El enemigo ataca al jugador.
        /// Transiciona a CHASE si el jugador se aleja.
        /// </summary>
        private void UpdateAttack()
        {
            if (_targetPlayer == null)
            {
                ChangeState(EnemyState.Return);
                return;
            }

            float distanceToPlayer = Vector2.Distance(transform.position, _targetPlayer.position);

            // Si el jugador se aleja, volvemos a perseguir
            if (distanceToPlayer > _attackRadius * 1.5f)
            {
                ChangeState(EnemyState.Chase);
                return;
            }

            // Intentamos atacar
            TryAttack();
        }

        /// <summary>
        /// Estado RETURN: El enemigo vuelve a su punto de patrulla.
        /// Transiciona a PATROL al llegar.
        /// </summary>
        private void UpdateReturn()
        {
            // Si detectamos al jugador mientras volvemos, lo perseguimos
            if (TryDetectPlayer())
            {
                ChangeState(EnemyState.Chase);
                return;
            }

            float distanceToPoint = Vector2.Distance(transform.position, _lastPatrolPosition);

            // Si llegamos, volvemos a patrullar
            if (distanceToPoint < 0.3f)
            {
                ChangeState(EnemyState.Patrol);
                return;
            }

            // Nos movemos de vuelta
            MoveTowards(_lastPatrolPosition, _patrolSpeed);
        }

        // ====================================================================
        // SECCIÓN 6: DETECCIÓN DEL JUGADOR
        // ====================================================================

        /// <summary>
        /// Intenta detectar al jugador dentro del radio de detección.
        /// Usa OverlapCircle para buscar colliders en el área.
        /// </summary>
        private bool TryDetectPlayer()
        {
            // Buscamos colliders del jugador en el radio de detección
            Collider2D playerCollider = Physics2D.OverlapCircle(
                transform.position,
                _detectionRadius,
                _playerLayer
            );

            if (playerCollider != null)
            {
                _targetPlayer = playerCollider.transform;
                Debug.Log($"[EnemyAI] Jugador detectado a distancia: {Vector2.Distance(transform.position, _targetPlayer.position)}");
                return true;
            }

            return false;
        }

        // ====================================================================
        // SECCIÓN 7: MOVIMIENTO
        // ====================================================================

        /// <summary>
        /// Mueve al enemigo hacia una posición objetivo.
        /// </summary>
        private void MoveTowards(Vector3 targetPosition, float speed)
        {
            // Calculamos dirección
            Vector2 direction = (targetPosition - transform.position).normalized;

            // Aplicamos velocidad
            _rb.linearVelocity = direction * speed;

            // Volteamos el sprite según la dirección
            FlipSprite(direction.x);
        }

        /// <summary>
        /// Voltea el sprite según la dirección de movimiento.
        /// </summary>
        private void FlipSprite(float horizontalDirection)
        {
            if (_spriteRenderer == null) return;

            if (horizontalDirection > 0.1f)
            {
                _spriteRenderer.flipX = false;
            }
            else if (horizontalDirection < -0.1f)
            {
                _spriteRenderer.flipX = true;
            }
        }

        // ====================================================================
        // SECCIÓN 8: ATAQUE
        // ====================================================================

        /// <summary>
        /// Intenta atacar si ha pasado el cooldown.
        /// </summary>
        private void TryAttack()
        {
            if (Time.time - _lastAttackTime < _attackCooldown) return;

            _lastAttackTime = Time.time;

            // Animación de ataque
            _animator?.SetTrigger(ANIM_ATTACK);

            // Hacemos daño al jugador
            if (_targetPlayer != null)
            {
                PlayerController player = _targetPlayer.GetComponent<PlayerController>();
                if (player != null)
                {
                    player.TakeDamage(_attackDamage);
                    Debug.Log($"[EnemyAI] Ataque! Daño: {_attackDamage}");
                }
            }
        }

        // ====================================================================
        // SECCIÓN 9: RECIBIR DAÑO (Implementa IDamageable)
        // ====================================================================

        /// <summary>
        /// El enemigo recibe daño (llamado desde el arma del jugador).
        /// </summary>
        public void TakeDamage(int damage)
        {
            _currentHealth -= damage;

            // Animación de daño
            _animator?.SetTrigger(ANIM_HURT);

            // Efecto visual
            StartCoroutine(DamageFlash());

            Debug.Log($"[EnemyAI] Daño recibido: {damage}. Vida: {_currentHealth}/{_maxHealth}");

            // Si muere
            if (_currentHealth <= 0)
            {
                Die();
            }
        }

        /// <summary>
        /// Efecto de flash rojo al recibir daño.
        /// </summary>
        private System.Collections.IEnumerator DamageFlash()
        {
            if (_spriteRenderer == null) yield break;

            Color originalColor = _spriteRenderer.color;
            _spriteRenderer.color = Color.red;

            yield return new WaitForSeconds(0.1f);

            _spriteRenderer.color = originalColor;
        }

        /// <summary>
        /// El enemigo muere.
        /// </summary>
        private void Die()
        {
            Debug.Log($"[EnemyAI] {gameObject.name} ha muerto");

            // Notificamos al WaveManager antes de destruirnos
            BIT.Core.WaveManager.Instance?.NotifyEnemyDied(gameObject);

            // Score con multiplicador de combo
            int baseScore = 50 + (_maxHealth / 2);
            int finalScore = BIT.Core.ComboManager.Instance != null
                ? BIT.Core.ComboManager.Instance.RegisterKill(baseScore)
                : baseScore;

            // Sumamos puntos al jugador
            var player = FindFirstObjectByType<PlayerController>();
            player?.AddScore(finalScore);

            // Drop de items aleatorio
            GetComponent<EnemyDropper>()?.Drop();

            // Animación de muerte
            _animator?.SetTrigger(ANIM_DIE);

            _rb.linearVelocity = Vector2.zero;
            var col = GetComponent<Collider2D>();
            if (col != null) col.enabled = false;

            Destroy(gameObject, 1f);
        }

        // ====================================================================
        // SECCIÓN 10: ANIMACIONES
        // ====================================================================

        private void UpdateAnimations()
        {
            if (_animator == null) return;

            // Velocidad para el animator
            float speed = _rb.linearVelocity.magnitude;
            _animator.SetFloat(ANIM_SPEED, speed);
        }

        // ====================================================================
        // SECCIÓN 11: CAMBIO DE ESTADO
        // ====================================================================

        /// <summary>
        /// Cambia el estado actual del enemigo.
        /// </summary>
        private void ChangeState(EnemyState newState)
        {
            // Log de transición (útil para debug)
            Debug.Log($"[EnemyAI] {gameObject.name}: {_currentState} -> {newState}");

            // Salimos del estado anterior
            ExitState(_currentState);

            // Entramos al nuevo estado
            _currentState = newState;
            EnterState(newState);
        }

        /// <summary>
        /// Acciones al salir de un estado.
        /// </summary>
        private void ExitState(EnemyState state)
        {
            switch (state)
            {
                case EnemyState.Chase:
                case EnemyState.Attack:
                    // Paramos el movimiento al salir de persecución
                    _rb.linearVelocity = Vector2.zero;
                    break;
            }
        }

        /// <summary>
        /// Acciones al entrar a un estado.
        /// </summary>
        private void EnterState(EnemyState state)
        {
            switch (state)
            {
                case EnemyState.Patrol:
                    _waitTimer = 0f;
                    break;

                case EnemyState.Return:
                    // Si no tenemos posición guardada, usamos el primer waypoint
                    if (_patrolPoints != null && _patrolPoints.Length > 0)
                    {
                        _lastPatrolPosition = _patrolPoints[0].position;
                    }
                    break;
            }
        }

        // ====================================================================
        // SECCIÓN 12: GIZMOS (Visualización en Editor)
        // ====================================================================

        private void OnDrawGizmosSelected()
        {
            // Radio de detección (amarillo)
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _detectionRadius);

            // Radio de ataque (rojo)
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, _attackRadius);

            // Radio de pérdida de objetivo (cian)
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, _loseTargetRadius);

            // Líneas a los puntos de patrulla (verde)
            if (_patrolPoints != null && _patrolPoints.Length > 0)
            {
                Gizmos.color = Color.green;
                for (int i = 0; i < _patrolPoints.Length; i++)
                {
                    if (_patrolPoints[i] != null)
                    {
                        // Punto
                        Gizmos.DrawSphere(_patrolPoints[i].position, 0.2f);

                        // Línea al siguiente
                        int nextIndex = (i + 1) % _patrolPoints.Length;
                        if (_patrolPoints[nextIndex] != null)
                        {
                            Gizmos.DrawLine(_patrolPoints[i].position, _patrolPoints[nextIndex].position);
                        }
                    }
                }
            }
        }

        // ====================================================================
        // SECCIÓN 13: PROPIEDADES PÚBLICAS
        // ====================================================================

        public EnemyState CurrentState => _currentState;
        public int CurrentHealth => _currentHealth;
        public int MaxHealth => _maxHealth;

        // ====================================================================
        // SECCIÓN 14: ESCALADO (llamado desde WaveManager)
        // ====================================================================

        public void ScaleStats(float factor)
        {
            _maxHealth = Mathf.RoundToInt(_maxHealth * factor);
            _currentHealth = _maxHealth;
            _attackDamage = Mathf.RoundToInt(_attackDamage * factor);
            // Escalar velocidad suavemente para no hacer enemigos imposibles
            _chaseSpeed = Mathf.Min(_chaseSpeed * (1f + (factor - 1f) * 0.4f), 10f);
            _patrolSpeed = Mathf.Min(_patrolSpeed * (1f + (factor - 1f) * 0.25f), 5f);
        }
    }
}
