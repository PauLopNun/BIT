using UnityEngine;
using BIT.Events;
using BIT.Player;

// ============================================================================
// HAZARDBASE.CS - Base para objetos con efectos negativos (Requisito 2.8)
// ============================================================================
// Esta clase implementa objetos que perjudican al jugador: trampas, pinchos,
// enemigos estáticos, zonas de daño, etc.
//
// CONCEPTO CLAVE PARA DEFENSA ORAL:
// Al igual que con PickupBase, usamos herencia. HazardBase contiene la lógica
// común (detección, feedback, cooldown de daño) y las clases hijas definen
// el tipo específico de perjuicio.
//
// Requisitos cumplidos:
// - 2.8: Objetos con efectos negativos (quitar vida, puntos, reducir velocidad)
// - El efecto se refleja en el marcador
// ============================================================================

namespace BIT.Interactables
{
    /// <summary>
    /// Clase base para todos los hazards (objetos peligrosos).
    /// </summary>
    public abstract class HazardBase : MonoBehaviour
    {
        // ====================================================================
        // SECCIÓN 1: CONFIGURACIÓN COMÚN
        // ====================================================================

        [Header("=== CONFIGURACIÓN GENERAL ===")]
        [Tooltip("Tag del objeto que puede ser afectado")]
        [SerializeField] protected string _targetTag = "Player";

        [Tooltip("Tiempo de espera entre cada aplicación de daño (evita spam)")]
        [SerializeField] protected float _damageCooldown = 1f;

        [Tooltip("Si es true, el hazard se destruye al hacer daño una vez")]
        [SerializeField] protected bool _destroyOnHit = false;

        [Header("=== EVENTOS ===")]
        [Tooltip("Evento que se dispara al hacer daño")]
        [SerializeField] protected GameEventSO _onHazardHitEvent;

        [Header("=== FEEDBACK VISUAL ===")]
        [Tooltip("Prefab de partículas al hacer daño")]
        [SerializeField] protected GameObject _hitParticlesPrefab;

        [Tooltip("Color del flash de daño")]
        [SerializeField] protected Color _damageFlashColor = Color.red;

        [Header("=== SONIDO ===")]
        [Tooltip("Sonido al hacer daño")]
        [SerializeField] protected AudioClip _hitSound;

        // ====================================================================
        // SECCIÓN 2: VARIABLES PRIVADAS
        // ====================================================================

        protected float _lastDamageTime;
        protected bool _playerInside = false;
        protected Collider2D _targetCollider;

        // ====================================================================
        // SECCIÓN 3: DETECCIÓN DE COLISIÓN
        // ====================================================================

        /// <summary>
        /// Se llama cuando el jugador entra en la zona de peligro.
        /// </summary>
        protected virtual void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag(_targetTag))
            {
                _playerInside = true;
                _targetCollider = other;

                // Aplicamos daño inmediatamente al entrar
                TryApplyEffect(other);
            }
        }

        /// <summary>
        /// Se llama mientras el jugador permanece en la zona.
        /// Permite aplicar daño continuo.
        /// </summary>
        protected virtual void OnTriggerStay2D(Collider2D other)
        {
            if (other.CompareTag(_targetTag) && _playerInside)
            {
                TryApplyEffect(other);
            }
        }

        /// <summary>
        /// Se llama cuando el jugador sale de la zona.
        /// </summary>
        protected virtual void OnTriggerExit2D(Collider2D other)
        {
            if (other.CompareTag(_targetTag))
            {
                _playerInside = false;
                _targetCollider = null;
            }
        }

        // ====================================================================
        // SECCIÓN 4: APLICACIÓN DE EFECTO
        // ====================================================================

        /// <summary>
        /// Intenta aplicar el efecto si ha pasado el cooldown.
        /// </summary>
        protected void TryApplyEffect(Collider2D target)
        {
            // Verificamos el cooldown
            if (Time.time - _lastDamageTime < _damageCooldown) return;

            _lastDamageTime = Time.time;

            // Obtenemos el PlayerController
            PlayerController player = target.GetComponent<PlayerController>();

            if (player != null)
            {
                // Aplicamos el efecto específico
                ApplyEffect(player);

                // Feedback
                PlayFeedback(target.transform.position);

                // Evento
                _onHazardHitEvent?.Raise();
            }

            // Destruimos si está configurado
            if (_destroyOnHit)
            {
                Destroy(gameObject, 0.1f);
            }
        }

        /// <summary>
        /// Método abstracto que las clases hijas implementan.
        /// </summary>
        protected abstract void ApplyEffect(PlayerController player);

        // ====================================================================
        // SECCIÓN 5: FEEDBACK
        // ====================================================================

        protected virtual void PlayFeedback(Vector3 position)
        {
            // Partículas
            if (_hitParticlesPrefab != null)
            {
                GameObject particles = Instantiate(_hitParticlesPrefab, position, Quaternion.identity);
                Destroy(particles, 2f);
            }

            // Sonido
            if (_hitSound != null)
            {
                AudioSource.PlayClipAtPoint(_hitSound, position);
            }
        }
    }

    // ========================================================================
    // CLASES CONCRETAS DE HAZARDS
    // ========================================================================

    /// <summary>
    /// Hazard que quita vida al jugador (pinchos, fuego, etc.)
    /// </summary>
    public class DamageHazard : HazardBase
    {
        [Header("=== CONFIGURACIÓN DE DAÑO ===")]
        [Tooltip("Cantidad de daño que inflige")]
        [SerializeField] private int _damageAmount = 10;

        protected override void ApplyEffect(PlayerController player)
        {
            player.TakeDamage(_damageAmount);
            Debug.Log($"[DamageHazard] Daño aplicado: {_damageAmount}");
        }
    }

    /// <summary>
    /// Hazard que quita puntos al jugador
    /// </summary>
    public class ScoreDrainHazard : HazardBase
    {
        [Header("=== CONFIGURACIÓN DE PUNTOS ===")]
        [Tooltip("Puntos que quita")]
        [SerializeField] private int _pointsToRemove = 50;

        protected override void ApplyEffect(PlayerController player)
        {
            player.RemoveScore(_pointsToRemove);
            Debug.Log($"[ScoreDrainHazard] Puntos removidos: {_pointsToRemove}");
        }
    }

    /// <summary>
    /// Hazard que ralentiza al jugador (barro, hielo, telarañas)
    /// </summary>
    public class SlowHazard : HazardBase
    {
        [Header("=== CONFIGURACIÓN DE LENTITUD ===")]
        [Tooltip("Multiplicador de velocidad (0.5 = 50% más lento)")]
        [SerializeField] private float _slowMultiplier = 0.5f;

        [Tooltip("Duración del efecto después de salir de la zona")]
        [SerializeField] private float _effectDuration = 2f;

        protected override void ApplyEffect(PlayerController player)
        {
            player.ModifySpeed(_slowMultiplier, _effectDuration);
            Debug.Log($"[SlowHazard] Velocidad reducida a x{_slowMultiplier}");
        }
    }

    /// <summary>
    /// Hazard combinado: hace daño Y ralentiza (veneno, ácido)
    /// </summary>
    public class PoisonHazard : HazardBase
    {
        [Header("=== CONFIGURACIÓN DE VENENO ===")]
        [Tooltip("Daño por tick")]
        [SerializeField] private int _damagePerTick = 5;

        [Tooltip("Reducción de velocidad")]
        [SerializeField] private float _slowAmount = 0.7f;

        protected override void ApplyEffect(PlayerController player)
        {
            // Daño
            player.TakeDamage(_damagePerTick);

            // Lentitud temporal
            player.ModifySpeed(_slowAmount, _damageCooldown);

            Debug.Log($"[PoisonHazard] Veneno: {_damagePerTick} daño, velocidad x{_slowAmount}");
        }
    }
}
