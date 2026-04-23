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
        [Tooltip("Radio de dispersión aleatorio al dropear")]
        [SerializeField] private float _spawnRadius = 0.5f;

        public void Drop()
        {
            foreach (var entry in _drops)
            {
                if (entry.prefab == null) continue;
                if (Random.value <= entry.probability)
                {
                    Vector2 offset = Random.insideUnitCircle * _spawnRadius;
                    Instantiate(entry.prefab, transform.position + (Vector3)offset, Quaternion.identity);
                }
            }
        }

        // Guaranteed drop (used by boss for double drop)
        public void ForceDrop()
        {
            foreach (var entry in _drops)
            {
                if (entry.prefab == null) continue;
                Vector2 offset = Random.insideUnitCircle * _spawnRadius;
                Instantiate(entry.prefab, transform.position + (Vector3)offset, Quaternion.identity);
            }
        }

        // Configure drops at runtime (used by WaveManager for spawned enemies)
        public void AddDrop(GameObject prefab, float probability)
        {
            if (prefab == null) return;
            _drops.Add(new DropEntry { prefab = prefab, probability = probability });
        }
    }
}
