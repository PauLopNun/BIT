using UnityEngine;
using UnityEngine.Events;

// ============================================================================
// GAMEEVENTLISTENERCOMPONENT.CS - Componente para escuchar eventos en Inspector
// ============================================================================
// Este componente permite configurar respuestas a eventos directamente
// desde el Inspector de Unity, sin necesidad de escribir código.
//
// CONCEPTO CLAVE PARA DEFENSA ORAL:
// Este es un "puente" entre el sistema de eventos por código y el diseñador
// que quiere configurar comportamientos sin programar. Usa UnityEvents,
// que son los mismos que ves en los botones de UI.
//
// CÓMO SE USA:
// 1. Añadir este componente a cualquier GameObject
// 2. Asignar el GameEventSO que quieres escuchar
// 3. En "Response", añadir las funciones a ejecutar (como en un botón)
// ============================================================================

namespace BIT.Events
{
    /// <summary>
    /// Componente que escucha un GameEventSO y ejecuta UnityEvents como respuesta.
    /// Permite configurar comportamientos desde el Inspector sin código.
    /// </summary>
    public class GameEventListenerComponent : MonoBehaviour, IGameEventListener
    {
        // ====================================================================
        // VARIABLES CONFIGURABLES EN INSPECTOR
        // ====================================================================

        [Header("=== CONFIGURACIÓN DEL LISTENER ===")]
        [Tooltip("El evento que este componente va a escuchar")]
        [SerializeField] private GameEventSO _gameEvent;

        [Tooltip("Acciones a ejecutar cuando el evento se dispare. " +
                "Configura como un botón de UI.")]
        [SerializeField] private UnityEvent _response;

        // ====================================================================
        // CICLO DE VIDA - Suscripción y Desuscripción
        // ====================================================================

        /// <summary>
        /// Se llama cuando el objeto se activa.
        /// Nos suscribimos al evento para empezar a escuchar.
        /// </summary>
        private void OnEnable()
        {
            // Nos registramos en el evento para recibir notificaciones
            if (_gameEvent != null)
            {
                _gameEvent.RegisterListener(this);
            }
            else
            {
                Debug.LogWarning($"[GameEventListener] {gameObject.name}: No hay evento asignado.");
            }
        }

        /// <summary>
        /// Se llama cuando el objeto se desactiva.
        /// IMPORTANTE: Siempre desuscribirse para evitar errores y memory leaks.
        /// </summary>
        private void OnDisable()
        {
            if (_gameEvent != null)
            {
                _gameEvent.UnregisterListener(this);
            }
        }

        // ====================================================================
        // IMPLEMENTACIÓN DE IGameEventListener
        // ====================================================================

        /// <summary>
        /// Este método es llamado automáticamente cuando el evento se dispara.
        /// Ejecuta todas las acciones configuradas en el Inspector.
        /// </summary>
        public void OnEventRaised()
        {
            // Invocamos el UnityEvent, que ejecutará todas las funciones
            // que hayamos configurado en el Inspector
            _response?.Invoke();

            #if UNITY_EDITOR
            Debug.Log($"[GameEventListener] {gameObject.name}: Evento '{_gameEvent.name}' recibido.");
            #endif
        }

        // ====================================================================
        // MÉTODOS PÚBLICOS AUXILIARES
        // ====================================================================

        /// <summary>
        /// Permite cambiar el evento a escuchar en tiempo de ejecución.
        /// </summary>
        public void SetGameEvent(GameEventSO newEvent)
        {
            // Primero nos desuscribimos del evento actual
            if (_gameEvent != null)
            {
                _gameEvent.UnregisterListener(this);
            }

            // Asignamos el nuevo evento
            _gameEvent = newEvent;

            // Nos suscribimos al nuevo evento
            if (_gameEvent != null && gameObject.activeInHierarchy)
            {
                _gameEvent.RegisterListener(this);
            }
        }
    }
}
