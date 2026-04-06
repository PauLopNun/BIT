using UnityEngine;

// ============================================================================
// PUSHABLEOBJECT.CS - Objeto que puede ser empujado (Requisito 2.1)
// ============================================================================
// Este script implementa el requisito 2.1: "Interacción física entre objetos
// mediante Rigidbody y Collider" y "Cambio de color usando Random".
//
// CONCEPTO CLAVE PARA DEFENSA ORAL:
// Las cajas empujables son un elemento clásico en juegos tipo Zelda.
// Usamos Rigidbody2D para que Unity maneje las físicas automáticamente.
// Cuando el jugador empuja la caja, Unity detecta la colisión y aplica
// las fuerzas correspondientes.
//
// El uso de Random.ColorHSV() cumple el requisito de "cambio de color
// utilizando Random". Cada vez que se empuja la caja, cambia de color.
// ============================================================================

namespace BIT.Interactables
{
    /// <summary>
    /// Objeto físico que puede ser empujado por el jugador.
    /// Requiere: Rigidbody2D, Collider2D
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(BoxCollider2D))]
    public class PushableObject : MonoBehaviour
    {
        // ====================================================================
        // SECCIÓN 1: CONFIGURACIÓN EN INSPECTOR
        // ====================================================================

        [Header("=== CONFIGURACIÓN DE EMPUJE ===")]
        [Tooltip("Resistencia al empuje. Valores altos = más difícil de mover")]
        [SerializeField] private float _pushResistance = 2f;

        [Tooltip("Velocidad máxima a la que puede moverse el objeto")]
        [SerializeField] private float _maxSpeed = 3f;

        [Header("=== EFECTOS VISUALES ===")]
        [Tooltip("Si es true, el objeto cambia de color al ser empujado")]
        [SerializeField] private bool _changeColorOnPush = true;

        [Tooltip("Duración del cambio de color en segundos")]
        [SerializeField] private float _colorChangeDuration = 0.5f;

        [Header("=== EFECTOS DE SONIDO ===")]
        [Tooltip("Sonido al empujar (opcional)")]
        [SerializeField] private AudioClip _pushSound;

        // ====================================================================
        // SECCIÓN 2: VARIABLES PRIVADAS
        // ====================================================================

        private Rigidbody2D _rb;
        private SpriteRenderer _spriteRenderer;
        private Color _originalColor;
        private bool _isBeingPushed = false;
        private AudioSource _audioSource;

        // ====================================================================
        // SECCIÓN 3: INICIALIZACIÓN
        // ====================================================================

        private void Awake()
        {
            // Obtenemos componentes
            _rb = GetComponent<Rigidbody2D>();
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _audioSource = GetComponent<AudioSource>();

            // Guardamos el color original
            if (_spriteRenderer != null)
            {
                _originalColor = _spriteRenderer.color;
            }

            // Configuramos el Rigidbody
            ConfigureRigidbody();
        }

        /// <summary>
        /// Configura el Rigidbody2D para comportamiento de objeto empujable.
        /// </summary>
        private void ConfigureRigidbody()
        {
            // Tipo dinámico para que responda a fuerzas
            _rb.bodyType = RigidbodyType2D.Dynamic;

            // Sin gravedad (es top-down)
            _rb.gravityScale = 0f;

            // Congelamos rotación para que no gire al empujarlo
            _rb.freezeRotation = true;

            // Drag alto para que se detenga al dejar de empujar
            // Esto crea un movimiento más "pesado" y controlado
            _rb.linearDamping = _pushResistance;

            // Masa para resistencia al empuje
            _rb.mass = _pushResistance;
        }

        // ====================================================================
        // SECCIÓN 4: ACTUALIZACIÓN
        // ====================================================================

        private void FixedUpdate()
        {
            // Limitamos la velocidad máxima
            if (_rb.linearVelocity.magnitude > _maxSpeed)
            {
                _rb.linearVelocity = _rb.linearVelocity.normalized * _maxSpeed;
            }
        }

        // ====================================================================
        // SECCIÓN 5: DETECCIÓN DE COLISIONES
        // ====================================================================

        /// <summary>
        /// Se llama cuando otro objeto empieza a tocar este.
        /// Usamos OnCollisionEnter2D (no Trigger) porque queremos físicas reales.
        /// </summary>
        private void OnCollisionEnter2D(Collision2D collision)
        {
            // Verificamos si es el jugador quien nos empuja
            if (collision.gameObject.CompareTag("Player"))
            {
                _isBeingPushed = true;

                // Efecto visual: cambio de color aleatorio
                if (_changeColorOnPush)
                {
                    ApplyRandomColorChange();
                }

                // Efecto de sonido
                PlayPushSound();

                Debug.Log($"[PushableObject] {gameObject.name} está siendo empujado");
            }
        }

        /// <summary>
        /// Se llama cuando el otro objeto deja de tocar este.
        /// </summary>
        private void OnCollisionExit2D(Collision2D collision)
        {
            if (collision.gameObject.CompareTag("Player"))
            {
                _isBeingPushed = false;
            }
        }

        // ====================================================================
        // SECCIÓN 6: EFECTOS VISUALES (Requisito 2.1 - Random)
        // ====================================================================

        /// <summary>
        /// Aplica un cambio de color aleatorio al objeto.
        /// Esto cumple el requisito 2.1: "Cambio de color utilizando Random"
        ///
        /// CONCEPTO PARA DEFENSA ORAL:
        /// Random.ColorHSV() genera un color aleatorio en el espacio HSV
        /// (Hue, Saturation, Value). Es más útil que Random RGB porque
        /// puedes controlar la saturación y brillo para evitar colores feos.
        /// </summary>
        private void ApplyRandomColorChange()
        {
            if (_spriteRenderer == null) return;

            // Generamos un color aleatorio con saturación y brillo controlados
            // Los parámetros son: (hueMin, hueMax, satMin, satMax, valMin, valMax)
            Color randomColor = Random.ColorHSV(
                0f, 1f,     // Cualquier tono (0-360 grados en el círculo de color)
                0.5f, 1f,   // Saturación media-alta (evita grises)
                0.7f, 1f    // Brillo alto (evita colores oscuros)
            );

            // Aplicamos el color
            _spriteRenderer.color = randomColor;

            // Programamos restaurar el color original
            StartCoroutine(RestoreColorAfterDelay());

            Debug.Log($"[PushableObject] Color cambiado a: {randomColor}");
        }

        /// <summary>
        /// Restaura el color original después de un tiempo.
        /// </summary>
        private System.Collections.IEnumerator RestoreColorAfterDelay()
        {
            yield return new WaitForSeconds(_colorChangeDuration);

            // Restauramos gradualmente (efecto de fade)
            float elapsed = 0f;
            float fadeDuration = 0.3f;
            Color currentColor = _spriteRenderer.color;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeDuration;
                _spriteRenderer.color = Color.Lerp(currentColor, _originalColor, t);
                yield return null;
            }

            _spriteRenderer.color = _originalColor;
        }

        // ====================================================================
        // SECCIÓN 7: SONIDO
        // ====================================================================

        /// <summary>
        /// Reproduce el sonido de empuje si está configurado.
        /// </summary>
        private void PlayPushSound()
        {
            if (_pushSound == null) return;

            // Si no hay AudioSource, lo creamos temporalmente
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
                _audioSource.playOnAwake = false;
            }

            // Evitamos reproducir si ya está sonando
            if (!_audioSource.isPlaying)
            {
                _audioSource.PlayOneShot(_pushSound);
            }
        }

        // ====================================================================
        // SECCIÓN 8: MÉTODOS PÚBLICOS
        // ====================================================================

        /// <summary>
        /// Aplica una fuerza externa al objeto.
        /// Útil para explosiones, ataques, etc.
        /// </summary>
        /// <param name="force">Vector de fuerza a aplicar</param>
        public void ApplyForce(Vector2 force)
        {
            _rb.AddForce(force, ForceMode2D.Impulse);

            // También cambiamos el color al recibir fuerza
            if (_changeColorOnPush)
            {
                ApplyRandomColorChange();
            }
        }

        /// <summary>
        /// Aplica una fuerza aleatoria al objeto.
        /// Cumple específicamente el requisito de "fuerza aleatoria".
        /// </summary>
        public void ApplyRandomForce()
        {
            // Dirección aleatoria
            Vector2 randomDirection = Random.insideUnitCircle.normalized;

            // Magnitud aleatoria
            float randomMagnitude = Random.Range(3f, 8f);

            // Aplicamos la fuerza
            ApplyForce(randomDirection * randomMagnitude);

            Debug.Log($"[PushableObject] Fuerza aleatoria: {randomDirection * randomMagnitude}");
        }

        /// <summary>
        /// Resetea el objeto a su posición inicial.
        /// </summary>
        public void ResetPosition(Vector3 originalPosition)
        {
            _rb.linearVelocity = Vector2.zero;
            transform.position = originalPosition;
            _spriteRenderer.color = _originalColor;
        }
    }
}
