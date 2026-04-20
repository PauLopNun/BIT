using UnityEngine;

// ============================================================================
// CAMERAFOLLOW.CS - Cámara que sigue al jugador (Requisito 2.10)
// ============================================================================
// Este script hace que la cámara siga suavemente al jugador.
// Es esencial para cualquier juego 2D donde el jugador se mueve.
//
// CONCEPTO CLAVE PARA DEFENSA ORAL:
// Usamos Vector3.Lerp para crear un movimiento SUAVE de la cámara.
// Lerp significa "Linear Interpolation" - calcula un punto intermedio
// entre dos posiciones. Esto evita que la cámara se mueva bruscamente.
//
// La cámara también tiene límites (bounds) para no mostrar áreas fuera
// del mapa del juego.
// ============================================================================

namespace BIT.Core
{
    /// <summary>
    /// Hace que la cámara siga al jugador con movimiento suave.
    /// Debe estar en el GameObject de la Main Camera.
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        // ====================================================================
        // CONFIGURACIÓN
        // ====================================================================

        [Header("=== OBJETIVO A SEGUIR ===")]
        [Tooltip("El transform del jugador que la cámara seguirá")]
        [SerializeField] private Transform _target;

        [Tooltip("Si es true, busca al jugador automáticamente por tag")]
        [SerializeField] private bool _findPlayerAutomatically = true;

        [Header("=== COMPORTAMIENTO DE SEGUIMIENTO ===")]
        [Tooltip("Velocidad con la que la cámara sigue al objetivo (mayor = más rápido)")]
        [SerializeField] private float _smoothSpeed = 5f;

        [Tooltip("Offset de la cámara respecto al jugador (útil si quieres centrar diferente)")]
        [SerializeField] private Vector3 _offset = new Vector3(0f, 0f, -10f);

        [Header("=== LÍMITES DEL MAPA (Opcional) ===")]
        [Tooltip("Si es true, la cámara no saldrá de los límites definidos")]
        [SerializeField] private bool _useBounds = false;

        [Tooltip("Límite mínimo X del mapa")]
        [SerializeField] private float _minX = -10f;

        [Tooltip("Límite máximo X del mapa")]
        [SerializeField] private float _maxX = 10f;

        [Tooltip("Límite mínimo Y del mapa")]
        [SerializeField] private float _minY = -10f;

        [Tooltip("Límite máximo Y del mapa")]
        [SerializeField] private float _maxY = 10f;

        [Header("=== LOOK AHEAD (Avanzado) ===")]
        [Tooltip("Si es true, la cámara se adelanta en la dirección del movimiento")]
        [SerializeField] private bool _useLookAhead = false;

        [Tooltip("Distancia que la cámara se adelanta")]
        [SerializeField] private float _lookAheadDistance = 2f;

        [Tooltip("Velocidad del look ahead")]
        [SerializeField] private float _lookAheadSpeed = 3f;

        // ====================================================================
        // VARIABLES PRIVADAS
        // ====================================================================

        private Vector3 _currentVelocity;
        private Vector3 _lookAheadOffset;
        private Rigidbody2D _targetRigidbody;

        // Screen shake
        public static CameraFollow Instance { get; private set; }
        private float _shakeDuration;
        private float _shakeMagnitude;

        // ====================================================================
        // INICIALIZACIÓN
        // ====================================================================

        private void Awake() { Instance = this; }

        private void Start()
        {
            if (_findPlayerAutomatically && _target == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    _target = player.transform;
                    _targetRigidbody = player.GetComponent<Rigidbody2D>();
                    Debug.Log("[CameraFollow] Jugador encontrado automáticamente");
                }
                else
                {
                    Debug.LogWarning("[CameraFollow] No se encontró ningún objeto con tag 'Player'");
                }
            }

            // Si ya tenemos target, obtenemos su Rigidbody
            if (_target != null && _targetRigidbody == null)
            {
                _targetRigidbody = _target.GetComponent<Rigidbody2D>();
            }
        }

        // ====================================================================
        // ACTUALIZACIÓN (LateUpdate)
        // ====================================================================

        /// <summary>
        /// Usamos LateUpdate para que la cámara se mueva DESPUÉS de que
        /// el jugador se haya movido. Esto evita que la cámara "tiemble".
        /// </summary>
        private void LateUpdate()
        {
            if (_target == null) return;

            // Calculamos la posición objetivo
            Vector3 targetPosition = _target.position + _offset;

            // Si usamos Look Ahead, calculamos el offset adicional
            if (_useLookAhead && _targetRigidbody != null)
            {
                Vector2 velocity = _targetRigidbody.linearVelocity;
                if (velocity.magnitude > 0.1f)
                {
                    Vector3 lookAheadTarget = velocity.normalized * _lookAheadDistance;
                    _lookAheadOffset = Vector3.Lerp(
                        _lookAheadOffset,
                        lookAheadTarget,
                        _lookAheadSpeed * Time.deltaTime
                    );
                }
                else
                {
                    // Volvemos al centro suavemente cuando no nos movemos
                    _lookAheadOffset = Vector3.Lerp(
                        _lookAheadOffset,
                        Vector3.zero,
                        _lookAheadSpeed * Time.deltaTime
                    );
                }

                targetPosition += _lookAheadOffset;
            }

            // Aplicamos los límites si están activados
            if (_useBounds)
            {
                targetPosition.x = Mathf.Clamp(targetPosition.x, _minX, _maxX);
                targetPosition.y = Mathf.Clamp(targetPosition.y, _minY, _maxY);
            }

            // Movimiento suave usando Lerp
            Vector3 smoothedPosition = Vector3.Lerp(
                transform.position,
                targetPosition,
                _smoothSpeed * Time.deltaTime
            );

            // Mantenemos la Z fija
            smoothedPosition.z = _offset.z;

            // Screen shake
            if (_shakeDuration > 0f)
            {
                smoothedPosition += (Vector3)Random.insideUnitCircle * _shakeMagnitude;
                _shakeDuration -= Time.deltaTime;
            }

            transform.position = smoothedPosition;
        }

        public void Shake(float duration = 0.15f, float magnitude = 0.12f)
        {
            _shakeDuration = duration;
            _shakeMagnitude = magnitude;
        }

        // ====================================================================
        // MÉTODOS PÚBLICOS
        // ====================================================================

        /// <summary>
        /// Cambia el objetivo de la cámara.
        /// </summary>
        public void SetTarget(Transform newTarget)
        {
            _target = newTarget;
            _targetRigidbody = newTarget?.GetComponent<Rigidbody2D>();
        }

        /// <summary>
        /// Establece los límites de la cámara.
        /// </summary>
        public void SetBounds(float minX, float maxX, float minY, float maxY)
        {
            _minX = minX;
            _maxX = maxX;
            _minY = minY;
            _maxY = maxY;
            _useBounds = true;
        }

        /// <summary>
        /// Mueve la cámara instantáneamente al objetivo.
        /// Útil al cambiar de escena o teletransportar.
        /// </summary>
        public void SnapToTarget()
        {
            if (_target == null) return;

            Vector3 targetPosition = _target.position + _offset;

            if (_useBounds)
            {
                targetPosition.x = Mathf.Clamp(targetPosition.x, _minX, _maxX);
                targetPosition.y = Mathf.Clamp(targetPosition.y, _minY, _maxY);
            }

            transform.position = targetPosition;
        }

        // ====================================================================
        // GIZMOS (Visualización en Editor)
        // ====================================================================

        private void OnDrawGizmosSelected()
        {
            if (!_useBounds) return;

            // Dibujamos los límites de la cámara
            Gizmos.color = Color.cyan;

            Vector3 bottomLeft = new Vector3(_minX, _minY, 0);
            Vector3 topLeft = new Vector3(_minX, _maxY, 0);
            Vector3 topRight = new Vector3(_maxX, _maxY, 0);
            Vector3 bottomRight = new Vector3(_maxX, _minY, 0);

            Gizmos.DrawLine(bottomLeft, topLeft);
            Gizmos.DrawLine(topLeft, topRight);
            Gizmos.DrawLine(topRight, bottomRight);
            Gizmos.DrawLine(bottomRight, bottomLeft);
        }
    }
}
