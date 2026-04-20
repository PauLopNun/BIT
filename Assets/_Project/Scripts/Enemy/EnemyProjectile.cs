using UnityEngine;

namespace BIT.Enemy
{
    public class EnemyProjectile : MonoBehaviour
    {
        public int damage = 12;

        void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                other.GetComponent<BIT.Player.PlayerController>()?.TakeDamage(damage);
                Destroy(gameObject);
            }
            else if (!other.CompareTag("Enemy") && !other.CompareTag("Projectile") && !other.isTrigger)
            {
                Destroy(gameObject);
            }
        }
    }
}
