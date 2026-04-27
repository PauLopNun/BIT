using UnityEngine;
using System.Linq;
using BIT.Player;

namespace BIT.Player
{
    // Arma automática estilo Vampire Survivors: dispara al enemigo más cercano sin input del jugador.
    public class AutoShooter : MonoBehaviour
    {
        [Header("=== CONFIG ===")]
        [Tooltip("Segundos entre disparos")]
        public float fireInterval = 1.8f;

        [Tooltip("Daño por bala")]
        public int bulletDamage = 12;

        [Tooltip("Velocidad de la bala")]
        public float bulletSpeed = 9f;

        [Tooltip("Rango máximo de detección de enemigos")]
        public float detectionRange = 12f;

        private float _nextFireTime;
        private static Sprite _bulletSprite;

        void Update()
        {
            if (Time.time < _nextFireTime) return;

            Transform target = FindNearestEnemy();
            if (target == null) return;

            _nextFireTime = Time.time + fireInterval;
            Fire(target.position);
        }

        Transform FindNearestEnemy()
        {
            Transform nearest = null;
            float best = detectionRange * detectionRange;

            foreach (var enemy in FindObjectsByType<BIT.Core.SimpleEnemyAI>(FindObjectsSortMode.None))
            {
                float sqDist = ((Vector2)enemy.transform.position - (Vector2)transform.position).sqrMagnitude;
                if (sqDist < best) { best = sqDist; nearest = enemy.transform; }
            }

            foreach (var enemy in FindObjectsByType<BIT.Enemy.RangedEnemyAI>(FindObjectsSortMode.None))
            {
                float sqDist = ((Vector2)enemy.transform.position - (Vector2)transform.position).sqrMagnitude;
                if (sqDist < best) { best = sqDist; nearest = enemy.transform; }
            }

            foreach (var enemy in FindObjectsByType<BIT.Enemy.EnemyAI>(FindObjectsSortMode.None))
            {
                float sqDist = ((Vector2)enemy.transform.position - (Vector2)transform.position).sqrMagnitude;
                if (sqDist < best) { best = sqDist; nearest = enemy.transform; }
            }

            return nearest;
        }

        void Fire(Vector3 targetPos)
        {
            Vector2 dir = ((Vector2)targetPos - (Vector2)transform.position).normalized;

            var bulletGO = new GameObject("PlayerBullet");
            bulletGO.transform.position = transform.position + (Vector3)dir * 0.4f;
            bulletGO.tag = "Projectile";

            // Rotar hacia el objetivo
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            bulletGO.transform.rotation = Quaternion.Euler(0f, 0f, angle);

            var sr = bulletGO.AddComponent<SpriteRenderer>();
            sr.sprite = GetBulletSprite();
            sr.color = Color.white;
            sr.sortingOrder = 4;
            bulletGO.transform.localScale = Vector3.one * 0.45f;

            var rb = bulletGO.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.linearVelocity = dir * bulletSpeed;

            var col = bulletGO.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.5f;

            var bullet = bulletGO.AddComponent<PlayerBullet>();
            bullet.damage = bulletDamage;

            Destroy(bulletGO, 4f);
        }

        static Sprite GetBulletSprite()
        {
            if (_bulletSprite != null) return _bulletSprite;

            // Shuriken: sprite 15×15, forma de estrella correcta
#if UNITY_EDITOR
            const string SHURIKEN_PATH = "Assets/_Project/Sprites/Ninja Adventure/FX/Projectile/Shuriken.png";
            var loaded = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(SHURIKEN_PATH).OfType<Sprite>().FirstOrDefault();
            if (loaded != null) { _bulletSprite = loaded; return _bulletSprite; }

            const string KUNAI_PATH = "Assets/_Project/Sprites/Ninja Adventure/FX/Projectile/Kunai.png";
            loaded = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(KUNAI_PATH).OfType<Sprite>().FirstOrDefault();
            if (loaded != null) { _bulletSprite = loaded; return _bulletSprite; }

            const string KUNAI_SHEET = "Assets/_Project/Sprites/Ninja Adventure/FX/Projectile/Kunai/SpriteSheet.png";
            loaded = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(KUNAI_SHEET).OfType<Sprite>().FirstOrDefault();
            if (loaded != null) { _bulletSprite = loaded; return _bulletSprite; }
#endif
            // Fallback procedural: estrella de 4 puntas
            var tex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            var pixels = new Color[1024];
            var center = new Vector2(15.5f, 15.5f);
            for (int i = 0; i < 1024; i++)
            {
                int x = i % 32, y = i / 32;
                float dx = x - center.x, dy = y - center.y;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float angle = Mathf.Atan2(Mathf.Abs(dy), Mathf.Abs(dx));
                // Forma de estrella / rombo
                float starRadius = 13f * (1f - 0.5f * Mathf.Sin(angle * 2f));
                pixels[i] = dist <= starRadius ? Color.white : Color.clear;
            }
            tex.SetPixels(pixels);
            tex.Apply();
            _bulletSprite = Sprite.Create(tex, new Rect(0, 0, 32, 32), Vector2.one * 0.5f, 32f);
            return _bulletSprite;
        }
    }

    public class PlayerBullet : MonoBehaviour
    {
        public int damage = 12;
        private bool _hit;

        void OnTriggerEnter2D(Collider2D other)
        {
            if (_hit) return;
            if (other.CompareTag("Player") || other.CompareTag("Projectile")) return;

            var simpleEnemy = other.GetComponent<BIT.Core.SimpleEnemyAI>();
            if (simpleEnemy != null) { _hit = true; simpleEnemy.TakeDamage(damage); Destroy(gameObject); return; }

            var rangedEnemy = other.GetComponent<BIT.Enemy.RangedEnemyAI>();
            if (rangedEnemy != null) { _hit = true; rangedEnemy.TakeDamage(damage); Destroy(gameObject); return; }

            var damageable = other.GetComponent<IDamageable>();
            if (damageable != null) { _hit = true; damageable.TakeDamage(damage); Destroy(gameObject); return; }

            // Hit non-trigger collider (wall/floor)
            if (!other.isTrigger) { _hit = true; Destroy(gameObject); }
        }
    }
}
