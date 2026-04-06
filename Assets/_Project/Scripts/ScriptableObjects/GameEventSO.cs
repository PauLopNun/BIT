using UnityEngine;
using System.Collections.Generic;

// ============================================================================
// GAMEEVENTSO.CS - Sistema de Eventos con ScriptableObjects
// ============================================================================
// Este sistema implementa el PATRÓN OBSERVER usando ScriptableObjects.
// Permite que diferentes partes del juego se comuniquen SIN conocerse
// directamente. Esto es fundamental para una arquitectura limpia y modular.
//
// CONCEPTO CLAVE PARA DEFENSA ORAL:
// Imagina que el jugador recoge una moneda. Sin eventos, tendrías que:
// - Desde la moneda, buscar la UI y actualizarla
// - Desde la moneda, buscar el AudioManager y reproducir sonido
// - Desde la moneda, buscar el sistema de partículas y activarlo
// Esto crea DEPENDENCIAS FUERTES y código espagueti.
//
// Con este sistema de eventos:
// - La moneda simplemente dispara un evento "OnCoinCollected"
// - La UI, Audio y Partículas ESCUCHAN ese evento independientemente
// - La moneda NO SABE quién está escuchando. DESACOPLAMIENTO total.
//
// CÓMO SE USA:
// 1. Crear un GameEventSO en el proyecto (Assets > Create > BIT > Game Event)
// 2. El emisor (moneda) llama a: coinEvent.Raise()
// 3. Los receptores (UI, Audio) se suscriben: coinEvent.RegisterListener(this)
// ============================================================================

namespace BIT.Events
{
    /// <summary>
    /// ScriptableObject que representa un evento del juego.
    /// Se crea desde: Assets > Create > BIT > Game Event
    /// </summary>
    [CreateAssetMenu(fileName = "NewGameEvent", menuName = "BIT/Game Event", order = 1)]
    public class GameEventSO : ScriptableObject
    {
        // ====================================================================
        // SECCIÓN 1: LISTA DE LISTENERS (Suscriptores)
        // ====================================================================
        // Almacenamos todos los objetos que quieren ser notificados.
        // Usamos una List porque los listeners pueden añadirse/quitarse
        // dinámicamente durante el juego.

        /// <summary>
        /// Lista de todos los listeners suscritos a este evento.
        /// Cada listener implementa la interfaz IGameEventListener.
        /// </summary>
        private readonly List<IGameEventListener> _listeners = new List<IGameEventListener>();

        // También permitimos suscribirse con System.Action (más flexible)
        private readonly List<System.Action> _actionListeners = new List<System.Action>();

        // ====================================================================
        // SECCIÓN 2: MÉTODOS PARA DISPARAR EL EVENTO
        // ====================================================================

        /// <summary>
        /// Dispara el evento, notificando a TODOS los listeners suscritos.
        /// Se llama desde el código que quiere emitir el evento.
        ///
        /// Ejemplo de uso:
        /// public GameEventSO onCoinCollected;
        /// void OnTriggerEnter2D(Collider2D other) {
        ///     if (other.CompareTag("Player")) {
        ///         onCoinCollected.Raise(); // Dispara el evento
        ///     }
        /// }
        /// </summary>
        public void Raise()
        {
            // Notificamos a todos los listeners de interfaz
            // Iteramos hacia atrás por si algún listener se desuscribe durante el evento
            for (int i = _listeners.Count - 1; i >= 0; i--)
            {
                if (_listeners[i] != null)
                {
                    _listeners[i].OnEventRaised();
                }
            }

            // Notificamos a todos los listeners de Action
            for (int i = _actionListeners.Count - 1; i >= 0; i--)
            {
                _actionListeners[i]?.Invoke();
            }

            #if UNITY_EDITOR
            Debug.Log($"[GameEvent] '{name}' disparado. Listeners notificados: " +
                     $"{_listeners.Count + _actionListeners.Count}");
            #endif
        }

        // ====================================================================
        // SECCIÓN 3: MÉTODOS DE SUSCRIPCIÓN (Register/Unregister)
        // ====================================================================

        /// <summary>
        /// Registra un listener para recibir notificaciones de este evento.
        /// Se llama típicamente en OnEnable() del MonoBehaviour.
        /// </summary>
        /// <param name="listener">Objeto que implementa IGameEventListener</param>
        public void RegisterListener(IGameEventListener listener)
        {
            if (listener == null) return;

            // Evitamos duplicados
            if (!_listeners.Contains(listener))
            {
                _listeners.Add(listener);
                #if UNITY_EDITOR
                Debug.Log($"[GameEvent] '{name}': Listener registrado. Total: {_listeners.Count}");
                #endif
            }
        }

        /// <summary>
        /// Elimina un listener de la lista de notificaciones.
        /// Se llama típicamente en OnDisable() del MonoBehaviour.
        /// IMPORTANTE: Siempre desuscribirse para evitar memory leaks.
        /// </summary>
        /// <param name="listener">Listener a eliminar</param>
        public void UnregisterListener(IGameEventListener listener)
        {
            if (listener == null) return;

            if (_listeners.Contains(listener))
            {
                _listeners.Remove(listener);
                #if UNITY_EDITOR
                Debug.Log($"[GameEvent] '{name}': Listener eliminado. Total: {_listeners.Count}");
                #endif
            }
        }

        /// <summary>
        /// Versión con System.Action para suscripciones más simples.
        /// Útil cuando no quieres implementar una interfaz completa.
        ///
        /// Ejemplo:
        /// void OnEnable() {
        ///     myEvent.RegisterListener(() => Debug.Log("Evento recibido!"));
        /// }
        /// </summary>
        public void RegisterListener(System.Action callback)
        {
            if (callback == null) return;

            if (!_actionListeners.Contains(callback))
            {
                _actionListeners.Add(callback);
            }
        }

        /// <summary>
        /// Elimina un callback Action de la lista.
        /// </summary>
        public void UnregisterListener(System.Action callback)
        {
            if (callback == null) return;

            if (_actionListeners.Contains(callback))
            {
                _actionListeners.Remove(callback);
            }
        }

        /// <summary>
        /// Limpia todos los listeners. Útil al cambiar de escena.
        /// </summary>
        public void ClearAllListeners()
        {
            _listeners.Clear();
            _actionListeners.Clear();

            Debug.Log($"[GameEvent] '{name}': Todos los listeners eliminados.");
        }
    }

    // ========================================================================
    // INTERFAZ IGameEventListener
    // ========================================================================
    // Los MonoBehaviours que quieran escuchar eventos deben implementar
    // esta interfaz. Es un "contrato" que garantiza que tienen el método
    // OnEventRaised() para recibir la notificación.

    /// <summary>
    /// Interfaz que deben implementar los objetos que quieren
    /// escuchar eventos de GameEventSO.
    /// </summary>
    public interface IGameEventListener
    {
        /// <summary>
        /// Método llamado cuando el evento es disparado.
        /// Aquí va la lógica de respuesta al evento.
        /// </summary>
        void OnEventRaised();
    }
}
