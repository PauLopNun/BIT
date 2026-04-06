using UnityEngine;

// ============================================================================
// ANIMATIONHELPER.CS - Guía para crear animaciones en Unity
// ============================================================================
// Este archivo contiene instrucciones y código de ayuda para configurar
// las animaciones del personaje y enemigos usando el pack Ninja Adventure.
//
// NO ES UN SCRIPT EJECUTABLE - Es una guía de referencia.
// ============================================================================

namespace BIT.Utils
{
    /// <summary>
    /// Clase de utilidad con constantes para animaciones.
    /// Úsala como referencia para los nombres de parámetros del Animator.
    /// </summary>
    public static class AnimationHelper
    {
        // ====================================================================
        // PARÁMETROS DEL ANIMATOR
        // ====================================================================
        // Estos son los nombres que debes usar en tu Animator Controller

        /// <summary>Velocidad de movimiento (float, 0 = idle, >0 = walk)</summary>
        public const string PARAM_SPEED = "Speed";

        /// <summary>Dirección horizontal (-1 izquierda, 1 derecha)</summary>
        public const string PARAM_MOVE_X = "MoveX";

        /// <summary>Dirección vertical (-1 abajo, 1 arriba)</summary>
        public const string PARAM_MOVE_Y = "MoveY";

        /// <summary>Trigger de ataque</summary>
        public const string PARAM_ATTACK = "Attack";

        /// <summary>Trigger de daño recibido</summary>
        public const string PARAM_HURT = "Hurt";

        /// <summary>Trigger de muerte</summary>
        public const string PARAM_DIE = "Die";

        /// <summary>Bool de si está vivo</summary>
        public const string PARAM_IS_ALIVE = "IsAlive";

        // ====================================================================
        // CÓMO CREAR UN ANIMATOR CONTROLLER PARA EL JUGADOR
        // ====================================================================
        /*
        PASO 1: Crear el Animator Controller
        -------------------------------------
        1. Click derecho en Assets/_Project/Animations/
        2. Create > Animator Controller
        3. Nómbralo "PlayerAnimator"
        4. Doble click para abrirlo

        PASO 2: Crear los parámetros
        ----------------------------
        En la pestaña "Parameters" (izquierda del Animator):
        1. Click "+" > Float > Nombra "Speed"
        2. Click "+" > Float > Nombra "MoveX"
        3. Click "+" > Float > Nombra "MoveY"
        4. Click "+" > Trigger > Nombra "Attack"
        5. Click "+" > Trigger > Nombra "Hurt"
        6. Click "+" > Bool > Nombra "IsAlive" (marca como true por defecto)

        PASO 3: Crear las animaciones
        -----------------------------
        Para cada sprite sheet (Idle, Walk, Attack):
        1. Selecciona el sprite sheet en Project
        2. Abre el Sprite Editor y haz Slice (Grid 16x16)
        3. Selecciona todos los sprites individuales
        4. Arrástralos al panel Animator
        5. Unity creará la animación automáticamente
        6. Guárdala en Assets/_Project/Animations/Player/

        PASO 4: Configurar el Blend Tree (para 4 direcciones)
        ----------------------------------------------------
        1. Click derecho en Animator > Create State > From New Blend Tree
        2. Doble click en el Blend Tree
        3. En Inspector: Blend Type = 2D Simple Directional
        4. Parameters: X = MoveX, Y = MoveY
        5. Añade las 4 animaciones de caminar (arriba, abajo, izq, der)
        6. Configura sus posiciones: (0,1), (0,-1), (-1,0), (1,0)

        PASO 5: Configurar transiciones
        -------------------------------
        - Idle -> Walk: Condición "Speed > 0.1"
        - Walk -> Idle: Condición "Speed < 0.1"
        - Any State -> Attack: Trigger "Attack"
        - Attack -> Idle: Al terminar animación (Has Exit Time)
        - Any State -> Hurt: Trigger "Hurt"
        - Any State -> Die: IsAlive = false
        */

        // ====================================================================
        // FRAMES PER SECOND RECOMENDADOS
        // ====================================================================

        /// <summary>FPS para animación de Idle (lenta, relajada)</summary>
        public const int FPS_IDLE = 6;

        /// <summary>FPS para animación de Walk (normal)</summary>
        public const int FPS_WALK = 10;

        /// <summary>FPS para animación de Attack (rápida)</summary>
        public const int FPS_ATTACK = 12;

        /// <summary>FPS para animación de Hurt (muy rápida)</summary>
        public const int FPS_HURT = 15;

        // ====================================================================
        // MÉTODO DE AYUDA PARA DEBUG
        // ====================================================================

        /// <summary>
        /// Imprime información de debug sobre el estado actual del Animator.
        /// Útil para depurar problemas de animación.
        /// </summary>
        public static void DebugAnimatorState(Animator animator)
        {
            if (animator == null)
            {
                Debug.LogWarning("[AnimationHelper] No hay Animator asignado");
                return;
            }

            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            Debug.Log($"[AnimationHelper] Estado actual: {stateInfo.fullPathHash}");
            Debug.Log($"[AnimationHelper] Speed: {animator.GetFloat(PARAM_SPEED)}");
            Debug.Log($"[AnimationHelper] MoveX: {animator.GetFloat(PARAM_MOVE_X)}");
            Debug.Log($"[AnimationHelper] MoveY: {animator.GetFloat(PARAM_MOVE_Y)}");
        }
    }
}
