using UnityEngine;

// ============================================================================
// CHARACTERDATA.CS — ScriptableObject con los datos de cada personaje jugable
// ============================================================================
// Crea assets: menú Unity → Assets → Create → BIT → Character Data
// O se generan automáticamente mediante BIT → 3. Crear Escenas
// ============================================================================

namespace BIT.Data
{
    [CreateAssetMenu(fileName = "CharacterData", menuName = "BIT/Character Data")]
    public class CharacterData : ScriptableObject
    {
        [Header("=== IDENTIDAD ===")]
        public string characterName = "Ninja";

        [TextArea(2, 4)]
        public string description = "Personaje equilibrado.";

        [Tooltip("Color que se aplica al SpriteRenderer del jugador")]
        public Color spriteColor = Color.white;

        [Header("=== COMBATE ===")]
        [Tooltip("Vida máxima")]
        public int maxHealth = 100;

        [Tooltip("Velocidad de movimiento")]
        public float moveSpeed = 5f;

        [Tooltip("Daño del ataque melee")]
        public int meleeDamage = 15;

        [Tooltip("Tiempo entre ataques normales")]
        public float attackCooldown = 0.3f;

        [Tooltip("Radio del ataque melee")]
        public float meleeRange = 1.2f;

        [Header("=== DASH ===")]
        [Tooltip("Velocidad durante el dash")]
        public float dashSpeed = 18f;

        [Tooltip("Duración del dash en segundos")]
        public float dashDuration = 0.18f;

        [Tooltip("Tiempo de recarga del dash (segundos)")]
        public float dashCooldown = 3f;

        // ====================================================================
        // HELPERS DE DISPLAY
        // ====================================================================

        public string HealthLabel   => $"{maxHealth} HP";
        public string SpeedLabel    => moveSpeed >= 6f ? "Alta" : moveSpeed <= 4f ? "Baja" : "Media";
        public string DamageLabel   => meleeDamage >= 20 ? "Alto" : meleeDamage <= 12 ? "Bajo" : "Medio";
        public string CooldownLabel => attackCooldown <= 0.25f ? "Rápido" : attackCooldown >= 0.45f ? "Lento" : "Normal";
    }
}
