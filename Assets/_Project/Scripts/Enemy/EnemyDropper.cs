using UnityEngine;
using System.Collections.Generic;

namespace BIT.Enemy
{
    // Attach to any enemy prefab. Call Drop() from Die() to spawn random pickups.
    public class EnemyDropper : MonoBehaviour
    {
        [System.Serializable]
        public class DropEntry
        {
            [Tooltip("Prefab del pickup (Coin, Heart, etc.)")]
            public GameObject prefab;
            [Tooltip("Probabilidad de drop 0-1")]
            [Range(0f, 1f)]
            public float probability = 0.3f;
        }

        [Header("=== DROPS ===")]
        [SerializeField] private List<DropEntry> _drops = new List<DropEntry>();
        public void Drop()
        {
            Vector3 pos = GetDropPosition();

            // Barajar para no dar siempre prioridad al mismo tipo de drop
            var order = new List<DropEntry>(_drops);
            for (int i = order.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                var tmp = order[i]; order[i] = order[j]; order[j] = tmp;
            }

            // Solo cae un item por muerte
            foreach (var entry in order)
            {
                if (entry.prefab == null) continue;
                if (Random.value <= entry.probability)
                {
                    EnsurePickupBehavior(Instantiate(entry.prefab, pos, Quaternion.identity));
                    break;
                }
            }
        }

        public void ForceDrop()
        {
            Vector3 pos = GetDropPosition();
            foreach (var entry in _drops)
            {
                if (entry.prefab == null) continue;
                EnsurePickupBehavior(Instantiate(entry.prefab, pos, Quaternion.identity));
            }
        }

        // Prefabs de Heart y Coin no tienen script adjunto: lo añadimos aquí.
        static void EnsurePickupBehavior(GameObject go)
        {
            if (go.GetComponent<BIT.Interactables.PickupBase>() != null) return;
            string n = go.name.ToLower();
            if (n.Contains("heart"))      go.AddComponent<BIT.Interactables.HealthPickup>();
            else if (n.Contains("coin"))  go.AddComponent<BIT.Interactables.ScorePickup>();
        }

        // Spawnea cerca del jugador en suelo libre (sin paredes).
        static Vector3 GetDropPosition()
        {
            var player = FindFirstObjectByType<BIT.Player.PlayerController>();
            if (player == null) return Vector3.zero;

            Vector2 playerPos = player.transform.position;

            for (int attempt = 0; attempt < 30; attempt++)
            {
                float angle = Random.Range(0f, Mathf.PI * 2f);
                // Distancia pequeña: cuanto más cerca del jugador, menos probable acabar en pared
                float dist  = Random.Range(0.3f, 0.9f);
                Vector2 candidate = playerPos + new Vector2(Mathf.Cos(angle) * dist, Mathf.Sin(angle) * dist);

                // Radio generoso (0.4) para detectar bordes de pared con margen
                bool blocked = false;
                foreach (var h in Physics2D.OverlapCircleAll(candidate, 0.4f))
                    if (!h.isTrigger) { blocked = true; break; }

                if (!blocked) return candidate;
            }

            // El jugador siempre está en suelo válido
            return playerPos;
        }

        // Configure drops at runtime (used by WaveManager for spawned enemies)
        public void AddDrop(GameObject prefab, float probability)
        {
            if (prefab == null) return;
            _drops.Add(new DropEntry { prefab = prefab, probability = probability });
        }
    }
}
