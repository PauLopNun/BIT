using UnityEngine;
using BIT.Events;
using BIT.Player;

// ============================================================================
// PICKUPBASE.CS - Base para objetos con efectos positivos (Requisito 2.7)
// ============================================================================
// Esta clase base implementa el comportamiento común de todos los objetos
// que benefician al jugador: monedas, pociones, power-ups, etc.
//
// CONCEPTO CLAVE PARA DEFENSA ORAL:
// Usamos HERENCIA para evitar duplicar código. PickupBase contiene la lógica
// común (detección de colisión, feedback visual, sonido) y las clases hijas
// (HealthPickup, ScorePickup, SpeedPickup) solo definen el efecto específico.
//
// Requisitos cumplidos:
// - 2.7: Objetos con efectos positivos (aumentar puntuación, vida, velocidad)
// - El efecto se refleja en la interfaz
// - El efecto se visualiza en el juego (partículas, escala)
// ============================================================================

namespace BIT.Interactables
{
    /// <summary>
    /// Clase base abstracta para todos los pickups (objetos recogibles).
    /// Las clases hijas deben implementar ApplyEffect().
    /// </summary>
    public abstract class PickupBase : MonoBehaviour
    {
        // ====================================================================
        // SECCIÓN 1: CONFIGURACIÓN COMÚN
        // ====================================================================

        [Header("=== CONFIGURACIÓN GENERAL ===")]
        [Tooltip("Tag del objeto que puede recoger este pickup")]
        [SerializeField] protected string _targetTag = "Player";

        [Tooltip("Si es true, el pickup se destruye al recogerlo")]
        [SerializeField] protected bool _destroyOnPickup = true;

        [Header("=== EVENTOS ===")]
        [Tooltip("Evento que se dispara al recoger el objeto")]
        [SerializeField] protected GameEventSO _onPickupEvent;

        [Header("=== FEEDBACK VISUAL ===")]
        [Tooltip("Prefab de partículas al recoger")]
        [SerializeField] protected GameObject _pickupParticlesPrefab;

        [Tooltip("Escala del efecto de partículas")]
        [SerializeField] protected float _particleScale = 1f;

        [Header("=== ANIMACIÓN IDLE ===")]
        [Tooltip("Si es true, el objeto se balancea arriba/abajo")]
        [SerializeField] protected bool _floatAnimation = true;

        [Tooltip("Velocidad del balanceo")]
        [SerializeField] protected float _floatSpeed = 2f;

        [Tooltip("Amplitud del balanceo")]
        [SerializeField] protected float _floatAmplitude = 0.1f;

        [Header("=== SONIDO ===")]
        [Tooltip("Sonido al recoger el objeto")]
        [SerializeField] protected AudioClip _pickupSound;

        // ====================================================================
        // SECCIÓN 2: VARIABLES PRIVADAS
        // ====================================================================

        protected Vector3 _startPosition;
        protected SpriteRenderer _spriteRenderer;
        protected bool _hasBeenPickedUp = false;

        // ====================================================================
        // SECCIÓN 3: INICIALIZACIÓN
        // ====================================================================

        protected virtual void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _startPosition = transform.position;
        }

        // ====================================================================
        // SECCIÓN 4: ANIMACIÓN IDLE (Balanceo)
        // ====================================================================

        protected virtual void Update()
        {
            if (_floatAnimation && !_hasBeenPickedUp)
            {
                // Movimiento sinusoidal arriba/abajo
                float newY = _startPosition.y + Mathf.Sin(Time.time * _floatSpeed) * _floatAmplitude;
                transform.position = new Vector3(_startPosition.x, newY, _startPosition.z);
            }
        }

        // ====================================================================
        // SECCIÓN 5: DETECCIÓN DE COLISIÓN
        // ====================================================================

        /// <summary>
        /// Se llama cuando el jugador entra en contacto con el pickup.
        /// El Collider del pickup debe estar marcado como "Is Trigger".
        /// </summary>
        protected virtual void OnTriggerEnter2D(Collider2D other)
        {
            // Evitamos recoger dos veces
            if (_hasBeenPickedUp) return;

            // Verificamos que sea el jugador
            if (other.CompareTag(_targetTag))
            {
                _hasBeenPickedUp = true;

                // Obtenemos referencia al PlayerController
                PlayerController player = other.GetComponent<PlayerController>();

                if (player != null)
                {
                    // Aplicamos el efecto específico (implementado en las clases hijas)
                    ApplyEffect(player);

                    // Feedback visual y sonoro
                    PlayFeedback();

                    // Disparamos evento (para UI, logros, etc.)
                    _onPickupEvent?.Raise();
                }

                // Destruimos o desactivamos el objeto
                if (_destroyOnPickup)
                {
                    Destroy(gameObject, 0.1f); // Pequeño delay para que se vean las partículas
                }
                else
                {
                    gameObject.SetActive(false);
                }
            }
        }

        // ====================================================================
        // SECCIÓN 6: MÉTODO ABSTRACTO (Las clases hijas lo implementan)
        // ====================================================================

        /// <summary>
        /// Aplica el efecto específico del pickup al jugador.
        /// DEBE ser implementado por las clases hijas.
        /// </summary>
        /// <param name="player">Referencia al PlayerController</param>
        protected abstract void ApplyEffect(PlayerController player);

        // ====================================================================
        // SECCIÓN 7: FEEDBACK (Visual y Sonoro)
        // ====================================================================

        /// <summary>
        /// Reproduce el feedback visual y sonoro al recoger el objeto.
        /// </summary>
        protected virtual void PlayFeedback()
        {
            // Partículas
            SpawnParticles();

            // Sonido
            PlaySound();

            // Efecto de escala (el objeto "explota" un poco antes de desaparecer)
            StartCoroutine(ScalePopEffect());
        }

        /// <summary>
        /// Instancia las partículas de recogida.
        /// </summary>
        protected void SpawnParticles()
        {
            if (_pickupParticlesPrefab == null) return;

            GameObject particles = Instantiate(
                _pickupParticlesPrefab,
                transform.position,
                Quaternion.identity
            );

            particles.transform.localScale = Vector3.one * _particleScale;

            // Destruimos las partículas después de 2 segundos
            Destroy(particles, 2f);
        }

        /// <summary>
        /// Reproduce el sonido de recogida.
        /// </summary>
        protected void PlaySound()
        {
            if (_pickupSound == null) return;

            // Usamos PlayClipAtPoint para que el sonido siga aunque destruyamos el objeto
            AudioSource.PlayClipAtPoint(_pickupSound, transform.position);
        }

        /// <summary>
        /// Efecto de "pop" en la escala del objeto.
        /// </summary>
        protected System.Collections.IEnumerator ScalePopEffect()
        {
            float duration = 0.1f;
            float elapsed = 0f;
            Vector3 originalScale = transform.localScale;
            Vector3 targetScale = originalScale * 1.3f;

            // Expandimos
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                transform.localScale = Vector3.Lerp(originalScale, targetScale, t);
                yield return null;
            }

            // Contraemos
            elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                transform.localScale = Vector3.Lerp(targetScale, Vector3.zero, t);
                yield return null;
            }
        }
    }

    // ========================================================================
    // CLASES CONCRETAS DE PICKUPS
    // ========================================================================

    /// <summary>
    /// Pickup que restaura vida al jugador.
    /// </summary>
    public class HealthPickup : PickupBase
    {
        [Header("=== CONFIGURACIÓN DE CURACIÓN ===")]
        [Tooltip("Cantidad de vida que restaura")]
        [SerializeField] private int _healAmount = 25;

        protected override void ApplyEffect(PlayerController player)
        {
            player.Heal(_healAmount);
            Debug.Log($"[HealthPickup] Jugador curado: +{_healAmount} vida");
        }
    }

    /// <summary>
    /// Pickup que añade puntos a la puntuación.
    /// </summary>
    public class ScorePickup : PickupBase
    {
        [Header("=== CONFIGURACIÓN DE PUNTOS ===")]
        [Tooltip("Puntos que otorga al recogerlo")]
        [SerializeField] private int _scoreAmount = 100;

        protected override void ApplyEffect(PlayerController player)
        {
            player.AddScore(_scoreAmount);
            Debug.Log($"[ScorePickup] Puntos añadidos: +{_scoreAmount}");
        }
    }

    /// <summary>
    /// Pickup que aumenta temporalmente la velocidad del jugador.
    /// </summary>
    public class SpeedPickup : PickupBase
    {
        [Header("=== CONFIGURACIÓN DE VELOCIDAD ===")]
        [Tooltip("Multiplicador de velocidad (1.5 = 50% más rápido)")]
        [SerializeField] private float _speedMultiplier = 1.5f;

        [Tooltip("Duración del efecto en segundos")]
        [SerializeField] private float _duration = 5f;

        protected override void ApplyEffect(PlayerController player)
        {
            player.ModifySpeed(_speedMultiplier, _duration);
            Debug.Log($"[SpeedPickup] Velocidad x{_speedMultiplier} durante {_duration}s");
        }
    }
}
