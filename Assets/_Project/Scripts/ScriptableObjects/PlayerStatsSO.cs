using UnityEngine;

// ============================================================================
// PLAYERSTATSS0.CS - ScriptableObject para estadísticas del jugador
// ============================================================================
// Este ScriptableObject actúa como un "contenedor de datos" que vive fuera
// de las escenas. Permite que múltiples sistemas (UI, Audio, Lógica) accedan
// a los mismos datos sin necesidad de referencias directas entre ellos.
//
// CONCEPTO CLAVE PARA DEFENSA ORAL:
// Los ScriptableObjects son assets que almacenan datos. A diferencia de los
// MonoBehaviours, no necesitan estar en un GameObject. Esto permite:
// 1. Compartir datos entre escenas sin usar Singletons
// 2. Modificar valores en el Inspector sin tocar código
// 3. Crear múltiples configuraciones (ej: dificultad fácil/difícil)
// ============================================================================

namespace BIT.Data
{
    /// <summary>
    /// ScriptableObject que contiene todas las estadísticas del jugador.
    /// Se crea desde el menú: Assets > Create > BIT > Player Stats
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerStats", menuName = "BIT/Player Stats", order = 0)]
    public class PlayerStatsSO : ScriptableObject
    {
        // ====================================================================
        // SECCIÓN 1: VARIABLES DE CONFIGURACIÓN (Valores iniciales)
        // ====================================================================
        // Estas variables definen los valores BASE del jugador.
        // Se configuran en el Inspector y NO cambian durante el juego.

        [Header("=== CONFIGURACIÓN INICIAL ===")]
        [Tooltip("Vida máxima que puede tener el jugador")]
        [SerializeField] private int _maxHealth = 100;

        [Tooltip("Velocidad base de movimiento del jugador")]
        [SerializeField] private float _baseSpeed = 5f;

        [Tooltip("Puntuación inicial al comenzar el juego")]
        [SerializeField] private int _startingScore = 0;

        // ====================================================================
        // SECCIÓN 2: VARIABLES DE ESTADO (Valores durante el juego)
        // ====================================================================
        // Estas variables cambian durante la partida.
        // Usamos [System.NonSerialized] para que Unity no las guarde entre sesiones.
        // Así, cada vez que iniciamos el juego, empezamos con los valores iniciales.

        [System.NonSerialized] private int _currentHealth;
        [System.NonSerialized] private int _currentScore;
        [System.NonSerialized] private float _currentSpeed;
        [System.NonSerialized] private bool _isInitialized = false;

        // ====================================================================
        // SECCIÓN 3: EVENTOS (Patrón Observer)
        // ====================================================================
        // Los eventos permiten que otros sistemas "escuchen" cuando algo cambia.
        // Por ejemplo, la UI se suscribe a OnHealthChanged para actualizar
        // la barra de vida SIN que este script conozca la existencia de la UI.
        //
        // CONCEPTO CLAVE PARA DEFENSA ORAL:
        // Esto es el "Patrón Observer". Los suscriptores (UI, Audio) se registran
        // y reciben notificaciones automáticamente cuando el valor cambia.
        // Esto DESACOPLA los sistemas: PlayerStats no sabe quién escucha.

        /// <summary>
        /// Se dispara cuando la vida cambia. Envía (vidaActual, vidaMáxima)
        /// </summary>
        public event System.Action<int, int> OnHealthChanged;

        /// <summary>
        /// Se dispara cuando la puntuación cambia. Envía la nueva puntuación.
        /// </summary>
        public event System.Action<int> OnScoreChanged;

        /// <summary>
        /// Se dispara cuando la velocidad cambia. Envía la nueva velocidad.
        /// </summary>
        public event System.Action<float> OnSpeedChanged;

        /// <summary>
        /// Se dispara cuando el jugador muere (vida llega a 0)
        /// </summary>
        public event System.Action OnPlayerDeath;

        // ====================================================================
        // SECCIÓN 4: PROPIEDADES PÚBLICAS (Getters)
        // ====================================================================
        // Exponemos los valores mediante propiedades de solo lectura.
        // Esto encapsula los datos: otros scripts pueden LEER pero no ESCRIBIR
        // directamente. Para modificar, deben usar los métodos públicos.

        /// <summary>
        /// Vida actual del jugador (solo lectura)
        /// </summary>
        public int CurrentHealth => _currentHealth;

        /// <summary>
        /// Vida máxima configurada (solo lectura)
        /// </summary>
        public int MaxHealth => _maxHealth;

        /// <summary>
        /// Puntuación actual (solo lectura)
        /// </summary>
        public int CurrentScore => _currentScore;

        /// <summary>
        /// Velocidad actual de movimiento (solo lectura)
        /// </summary>
        public float CurrentSpeed => _currentSpeed;

        /// <summary>
        /// Indica si el jugador está vivo
        /// </summary>
        public bool IsAlive => _currentHealth > 0;

        /// <summary>
        /// Porcentaje de vida actual (útil para barras de vida)
        /// </summary>
        public float HealthPercentage => (float)_currentHealth / _maxHealth;

        // ====================================================================
        // SECCIÓN 5: MÉTODOS DE INICIALIZACIÓN
        // ====================================================================

        /// <summary>
        /// Inicializa las estadísticas a sus valores base.
        /// DEBE llamarse al inicio del juego o al reiniciar.
        /// </summary>
        public void Initialize()
        {
            _currentHealth = _maxHealth;
            _currentScore = _startingScore;
            _currentSpeed = _baseSpeed;
            _isInitialized = true;

            // Notificamos a todos los suscriptores del estado inicial
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
            OnScoreChanged?.Invoke(_currentScore);
            OnSpeedChanged?.Invoke(_currentSpeed);

            Debug.Log($"[PlayerStats] Inicializado - Vida: {_currentHealth}/{_maxHealth}, " +
                     $"Velocidad: {_currentSpeed}, Score: {_currentScore}");
        }

        /// <summary>
        /// Se llama automáticamente cuando el ScriptableObject se activa.
        /// Útil para resetear en el Editor cuando se inicia Play Mode.
        /// </summary>
        private void OnEnable()
        {
            // En el Editor, reseteamos al entrar en Play Mode
            #if UNITY_EDITOR
            _isInitialized = false;
            #endif
        }

        // ====================================================================
        // SECCIÓN 6: MÉTODOS PARA MODIFICAR VIDA (Requisitos 2.7, 2.8)
        // ====================================================================

        /// <summary>
        /// Aplica daño al jugador (efecto negativo - requisito 2.8)
        /// </summary>
        /// <param name="amount">Cantidad de daño a aplicar</param>
        public void TakeDamage(int amount)
        {
            if (!_isInitialized)
            {
                Debug.LogWarning("[PlayerStats] No inicializado. Llama a Initialize() primero.");
                return;
            }

            if (amount <= 0) return;

            // Restamos el daño, asegurándonos de no bajar de 0
            _currentHealth = Mathf.Max(0, _currentHealth - amount);

            Debug.Log($"[PlayerStats] Daño recibido: {amount}. Vida actual: {_currentHealth}/{_maxHealth}");

            // Notificamos del cambio de vida
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);

            // Si la vida llega a 0, el jugador muere
            if (_currentHealth <= 0)
            {
                Debug.Log("[PlayerStats] ¡El jugador ha muerto!");
                OnPlayerDeath?.Invoke();
            }
        }

        /// <summary>
        /// Cura al jugador (efecto positivo - requisito 2.7)
        /// </summary>
        /// <param name="amount">Cantidad de vida a recuperar</param>
        public void Heal(int amount)
        {
            if (!_isInitialized) return;
            if (amount <= 0) return;

            // Sumamos la curación, sin superar el máximo
            int previousHealth = _currentHealth;
            _currentHealth = Mathf.Min(_maxHealth, _currentHealth + amount);

            int actualHealing = _currentHealth - previousHealth;
            Debug.Log($"[PlayerStats] Curación: +{actualHealing}. Vida actual: {_currentHealth}/{_maxHealth}");

            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
        }

        // ====================================================================
        // SECCIÓN 7: MÉTODOS PARA MODIFICAR PUNTUACIÓN (Requisito 2.6)
        // ====================================================================

        /// <summary>
        /// Añade puntos a la puntuación actual (efecto positivo)
        /// </summary>
        /// <param name="points">Puntos a añadir</param>
        public void AddScore(int points)
        {
            if (!_isInitialized) return;
            if (points <= 0) return;

            _currentScore += points;

            Debug.Log($"[PlayerStats] +{points} puntos. Total: {_currentScore}");

            OnScoreChanged?.Invoke(_currentScore);
        }

        /// <summary>
        /// Resta puntos de la puntuación actual (efecto negativo)
        /// </summary>
        /// <param name="points">Puntos a restar</param>
        public void RemoveScore(int points)
        {
            if (!_isInitialized) return;
            if (points <= 0) return;

            _currentScore = Mathf.Max(0, _currentScore - points);

            Debug.Log($"[PlayerStats] -{points} puntos. Total: {_currentScore}");

            OnScoreChanged?.Invoke(_currentScore);
        }

        // ====================================================================
        // SECCIÓN 8: MÉTODOS PARA MODIFICAR VELOCIDAD
        // ====================================================================

        /// <summary>
        /// Aplica un modificador de velocidad (multiplicador)
        /// Útil para power-ups de velocidad o efectos de lentitud
        /// </summary>
        /// <param name="multiplier">Multiplicador (1.5 = 50% más rápido, 0.5 = 50% más lento)</param>
        /// <param name="duration">Duración en segundos (0 = permanente)</param>
        public void ModifySpeed(float multiplier)
        {
            if (!_isInitialized) return;

            _currentSpeed = _baseSpeed * multiplier;

            Debug.Log($"[PlayerStats] Velocidad modificada: {_currentSpeed} (x{multiplier})");

            OnSpeedChanged?.Invoke(_currentSpeed);
        }

        /// <summary>
        /// Restaura la velocidad a su valor base
        /// </summary>
        public void ResetSpeed()
        {
            if (!_isInitialized) return;

            _currentSpeed = _baseSpeed;

            Debug.Log($"[PlayerStats] Velocidad restaurada: {_currentSpeed}");

            OnSpeedChanged?.Invoke(_currentSpeed);
        }
    }
}
