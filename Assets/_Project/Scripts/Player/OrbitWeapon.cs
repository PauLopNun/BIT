using UnityEngine;
using UnityEngine.InputSystem;

// ============================================================================
// ORBITWEAPON.CS - Arma orbital que sigue al ratón (Requisito 2.4)
// ============================================================================
// Este script cumple el requisito 2.4: "Partes del personaje con movimiento
// independiente". El arma (espada, varita, etc.) rota alrededor del jugador
// y SIEMPRE apunta hacia donde está el cursor del ratón.
//
// CONCEPTO CLAVE PARA DEFENSA ORAL:
// El arma es un objeto HIJO del jugador en la jerarquía de Unity.
// Sin embargo, su rotación es INDEPENDIENTE del padre. Esto se logra porque
// en Unity, puedes modificar la rotación de un hijo sin afectar al padre.
//
// La técnica usada es:
// 1. Obtenemos la posición del ratón en coordenadas del mundo (ScreenToWorldPoint)
// 2. Calculamos el ángulo entre el jugador y el ratón (Mathf.Atan2)
// 3. Aplicamos ese ángulo como rotación del arma
// 4. Posicionamos el arma en un círculo alrededor del jugador
//
// Esto crea el efecto de una espada que "orbita" y siempre apunta al ratón.
// ============================================================================

namespace BIT.Player
{
    /// <summary>
    /// Controla un arma que orbita alrededor del jugador y apunta al ratón.
    /// Debe ser hijo del objeto Player en la jerarquía.
    /// </summary>
    public class OrbitWeapon : MonoBehaviour
    {
        // ====================================================================
        // SECCIÓN 1: CONFIGURACIÓN EN INSPECTOR
        // ====================================================================

        [Header("=== CONFIGURACIÓN DE ÓRBITA ===")]
        [Tooltip("Distancia del arma al centro del jugador")]
        [SerializeField] private float _orbitRadius = 0.8f;

        [Tooltip("Velocidad de rotación suave hacia el objetivo")]
        [SerializeField] private float _rotationSpeed = 15f;

        [Tooltip("Si es true, el arma gira instantáneamente. Si es false, gira suavemente.")]
        [SerializeField] private bool _instantRotation = false;

        [Header("=== CONFIGURACIÓN VISUAL ===")]
        [Tooltip("Si es true, el sprite se voltea según la dirección")]
        [SerializeField] private bool _flipSprite = true;

        [Tooltip("El SpriteRenderer del arma (para voltear)")]
        [SerializeField] private SpriteRenderer _weaponSprite;

        [Header("=== CONFIGURACIÓN DE ATAQUE ===")]
        [Tooltip("Daño que hace el arma al contacto")]
        [SerializeField] private int _damage = 10;

        [Tooltip("Tag de los objetos que pueden recibir daño")]
        [SerializeField] private string _enemyTag = "Enemy";

        // ====================================================================
        // SECCIÓN 2: VARIABLES PRIVADAS
        // ====================================================================

        // Referencia al transform del jugador (padre)
        private Transform _playerTransform;

        // Ángulo actual y objetivo (para interpolación suave)
        private float _currentAngle;
        private float _targetAngle;

        // Cámara principal (para convertir posición del ratón)
        private Camera _mainCamera;

        // ====================================================================
        // SECCIÓN 3: INICIALIZACIÓN
        // ====================================================================

        /// <summary>
        /// Se llama antes que Start.
        /// Obtenemos referencias necesarias.
        /// </summary>
        private void Awake()
        {
            // El jugador es nuestro padre en la jerarquía
            _playerTransform = transform.parent;

            if (_playerTransform == null)
            {
                Debug.LogError("[OrbitWeapon] Este objeto debe ser hijo del Player!");
            }

            // Guardamos referencia a la cámara principal
            _mainCamera = Camera.main;

            // Si no asignaron el SpriteRenderer, intentamos obtenerlo
            if (_weaponSprite == null)
            {
                _weaponSprite = GetComponent<SpriteRenderer>();
            }
        }

        // ====================================================================
        // SECCIÓN 4: ACTUALIZACIÓN (Cada frame)
        // ====================================================================

        /// <summary>
        /// Update se llama cada frame.
        /// Calculamos el ángulo hacia el ratón y posicionamos el arma.
        /// </summary>
        private void Update()
        {
            if (_playerTransform == null || _mainCamera == null) return;

            // 1. Obtenemos la posición del ratón en el mundo
            Vector3 mouseScreenPos = Mouse.current.position.ReadValue();
            mouseScreenPos.z = _mainCamera.nearClipPlane;
            Vector3 mouseWorldPos = _mainCamera.ScreenToWorldPoint(mouseScreenPos);

            // 2. Calculamos la dirección desde el jugador hacia el ratón
            Vector2 direction = (mouseWorldPos - _playerTransform.position).normalized;

            // 3. Calculamos el ángulo en grados
            // Atan2 devuelve el ángulo en radianes, lo convertimos a grados
            _targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            // 4. Aplicamos rotación (instantánea o suave)
            if (_instantRotation)
            {
                _currentAngle = _targetAngle;
            }
            else
            {
                // Lerp angular para movimiento suave
                _currentAngle = Mathf.LerpAngle(_currentAngle, _targetAngle, _rotationSpeed * Time.deltaTime);
            }

            // 5. Posicionamos el arma en el círculo orbital
            PositionWeaponOnOrbit();

            // 6. Rotamos el arma para que apunte hacia afuera
            RotateWeapon();

            // 7. Volteamos el sprite si es necesario
            FlipSpriteIfNeeded();
        }

        // ====================================================================
        // SECCIÓN 5: POSICIONAMIENTO EN ÓRBITA
        // ====================================================================

        /// <summary>
        /// Posiciona el arma en un punto del círculo orbital.
        /// Usa trigonometría básica: x = cos(ángulo) * radio, y = sin(ángulo) * radio
        ///
        /// CONCEPTO PARA DEFENSA ORAL:
        /// Imagina un círculo alrededor del jugador. El arma siempre está
        /// en el borde de ese círculo, en la dirección donde apunta el ratón.
        /// </summary>
        private void PositionWeaponOnOrbit()
        {
            // Convertimos el ángulo de grados a radianes para las funciones trigonométricas
            float angleInRadians = _currentAngle * Mathf.Deg2Rad;

            // Calculamos la posición en el círculo usando trigonometría
            float x = Mathf.Cos(angleInRadians) * _orbitRadius;
            float y = Mathf.Sin(angleInRadians) * _orbitRadius;

            // Posicionamos relativo al jugador
            // Usamos localPosition porque somos hijos del jugador
            transform.localPosition = new Vector3(x, y, 0f);
        }

        /// <summary>
        /// Rota el arma para que apunte hacia afuera (en dirección al ratón).
        /// </summary>
        private void RotateWeapon()
        {
            // Aplicamos la rotación en el eje Z (rotación 2D)
            transform.rotation = Quaternion.Euler(0f, 0f, _currentAngle);
        }

        // ====================================================================
        // SECCIÓN 6: VOLTEO DEL SPRITE
        // ====================================================================

        /// <summary>
        /// Voltea el sprite del arma para que siempre se vea correctamente.
        /// Si el ratón está a la izquierda, el sprite se voltea verticalmente.
        /// </summary>
        private void FlipSpriteIfNeeded()
        {
            if (!_flipSprite || _weaponSprite == null) return;

            // Si el ángulo está en el lado izquierdo (entre 90 y -90 grados),
            // volteamos el sprite para que no se vea "al revés"
            bool shouldFlip = Mathf.Abs(_currentAngle) > 90f;
            _weaponSprite.flipY = shouldFlip;
        }

        // ====================================================================
        // SECCIÓN 7: DETECCIÓN DE COLISIONES (Daño a enemigos)
        // ====================================================================

        /// <summary>
        /// Se llama cuando el arma entra en contacto con otro collider.
        /// Usamos OnTriggerEnter2D porque el arma debe ser un Trigger.
        /// </summary>
        /// <param name="other">El collider con el que colisionamos</param>
        private void OnTriggerEnter2D(Collider2D other)
        {
            // Verificamos si es un enemigo
            if (other.CompareTag(_enemyTag))
            {
                // Intentamos obtener el componente de daño del enemigo
                var enemyHealth = other.GetComponent<IDamageable>();

                if (enemyHealth != null)
                {
                    enemyHealth.TakeDamage(_damage);
                    Debug.Log($"[OrbitWeapon] Daño aplicado a {other.name}: {_damage}");
                }

                // Efecto visual de impacto (usando Random - requisito 2.1)
                ApplyRandomImpactForce(other);
            }
        }

        /// <summary>
        /// Aplica una fuerza aleatoria al objeto golpeado.
        /// Cumple el requisito 2.1: "Cambio de posición, velocidad o fuerza
        /// de objetos utilizando el Random."
        /// </summary>
        private void ApplyRandomImpactForce(Collider2D other)
        {
            Rigidbody2D rb = other.GetComponent<Rigidbody2D>();

            if (rb != null)
            {
                // Generamos una fuerza aleatoria
                float randomForce = Random.Range(3f, 8f);

                // Dirección del impacto (desde el arma hacia el enemigo)
                Vector2 impactDirection = (other.transform.position - transform.position).normalized;

                // Aplicamos la fuerza
                rb.AddForce(impactDirection * randomForce, ForceMode2D.Impulse);

                Debug.Log($"[OrbitWeapon] Fuerza aleatoria aplicada: {randomForce}");
            }
        }

        // ====================================================================
        // SECCIÓN 8: MÉTODOS PÚBLICOS
        // ====================================================================

        /// <summary>
        /// Cambia el radio de órbita del arma.
        /// Útil para power-ups que aumentan el alcance.
        /// </summary>
        public void SetOrbitRadius(float newRadius)
        {
            _orbitRadius = Mathf.Max(0.1f, newRadius);
        }

        /// <summary>
        /// Obtiene el ángulo actual del arma.
        /// </summary>
        public float GetCurrentAngle() => _currentAngle;

        /// <summary>
        /// Obtiene la dirección en la que apunta el arma.
        /// </summary>
        public Vector2 GetAimDirection()
        {
            float angleInRadians = _currentAngle * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(angleInRadians), Mathf.Sin(angleInRadians));
        }
    }

    // ========================================================================
    // INTERFAZ IDamageable
    // ========================================================================
    // Interfaz que deben implementar todos los objetos que pueden recibir daño.
    // Esto permite que el arma haga daño a cualquier cosa sin saber qué es.

    /// <summary>
    /// Interfaz para objetos que pueden recibir daño.
    /// </summary>
    public interface IDamageable
    {
        void TakeDamage(int damage);
    }
}
