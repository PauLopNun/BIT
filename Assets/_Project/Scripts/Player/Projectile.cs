using UnityEngine;
using BIT.Player;

// ============================================================================
// PROJECTILE.CS - Proyectil lanzado por el jugador (Requisito 2.5)
// ============================================================================
// Este script controla el comportamiento de los proyectiles que el jugador
// puede lanzar. Cumple el requisito 2.5: "Lanzar un objeto al pulsar una tecla"
//
// El proyectil:
// - Se mueve en la dirección en que fue lanzado
// - Hace daño a los enemigos con los que colisiona
// - Se destruye al impactar o después de un tiempo
// ============================================================================

namespace BIT.Player
{
    /// <summary>
    /// Comportamiento del proyectil lanzado por el jugador.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class Projectile : MonoBehaviour
    {
        // ====================================================================
        // CONFIGURACIÓN
        // ====================================================================

        [Header("=== CONFIGURACIÓN ===")]
        [Tooltip("Daño que hace el proyectil")]
        [SerializeField] private int _damage = 15;

        [Tooltip("Tiempo de vida del proyectil en segundos")]
        [SerializeField] private float _lifetime = 3f;

        [Tooltip("Si es true, el proyectil se destruye al impactar")]
        [SerializeField] private bool _destroyOnImpact = true;

        [Tooltip("Tag de los objetos que pueden ser dañados")]
        [SerializeField] private string _targetTag = "Enemy";

        [Header("=== EFECTOS ===")]
        [Tooltip("Prefab de partículas al impactar")]
        [SerializeField] private GameObject _impactParticlesPrefab;

        [Tooltip("Sonido de impacto")]
        [SerializeField] private AudioClip _impactSound;

        // ====================================================================
        // VARIABLES
        // ====================================================================

        private Rigidbody2D _rb;
        private bool _hasHit = false;

        // ====================================================================
        // INICIALIZACIÓN
        // ====================================================================

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();

            // Configuramos el Rigidbody
            _rb.gravityScale = 0f;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        private void Start()
        {
            // Auto-destrucción después del tiempo de vida
            Destroy(gameObject, _lifetime);
        }

        // ====================================================================
        // COLISIONES
        // ====================================================================

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_hasHit) return;

            // Verificamos si es un objetivo válido
            if (other.CompareTag(_targetTag))
            {
                _hasHit = true;

                // Intentamos hacer daño - primero probamos SimpleEnemyAI
                var enemy = other.GetComponent<BIT.Core.SimpleEnemyAI>();
                if (enemy != null)
                {
                    enemy.TakeDamage(_damage);
                    Debug.Log($"[Projectile] Impacto en {other.name}, daño: {_damage}");
                }
                else
                {
                    // Fallback a IDamageable si existe
                    IDamageable damageable = other.GetComponent<IDamageable>();
                    if (damageable != null)
                    {
                        damageable.TakeDamage(_damage);
                        Debug.Log($"[Projectile] Impacto en {other.name}, daño: {_damage}");
                    }
                }

                // Efectos de impacto
                SpawnImpactEffects();

                // Destruimos el proyectil
                if (_destroyOnImpact)
                {
                    Destroy(gameObject);
                }
            }
            // Si colisiona con algo sólido (no el jugador)
            else if (!other.CompareTag("Player") && !other.isTrigger)
            {
                _hasHit = true;
                SpawnImpactEffects();

                if (_destroyOnImpact)
                {
                    Destroy(gameObject);
                }
            }
        }

        // ====================================================================
        // EFECTOS
        // ====================================================================

        private void SpawnImpactEffects()
        {
            // Partículas
            if (_impactParticlesPrefab != null)
            {
                GameObject particles = Instantiate(_impactParticlesPrefab, transform.position, Quaternion.identity);
                Destroy(particles, 2f);
            }

            // Sonido
            if (_impactSound != null)
            {
                AudioSource.PlayClipAtPoint(_impactSound, transform.position);
            }
        }

        // ====================================================================
        // CONFIGURACIÓN EXTERNA
        // ====================================================================

        /// <summary>
        /// Establece el daño del proyectil (útil para power-ups).
        /// </summary>
        public void SetDamage(int damage)
        {
            _damage = damage;
        }
    }
}
