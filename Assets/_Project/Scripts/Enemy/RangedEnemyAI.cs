using UnityEngine;

namespace BIT.Enemy
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class RangedEnemyAI : MonoBehaviour
    {
        [Header("=== MOVIMIENTO ===")]
        public float moveSpeed = 2.5f;
        public float preferredRange = 6f;
        public float minDistance = 3f;
        public float detectionRange = 14f;

        [Header("=== DISPARO ===")]
        public float fireRate = 2.8f;
        public int projectileDamage = 12;
        public float projectileSpeed = 7f;

        [Header("=== STATS ===")]
        public int maxHealth = 35;
        public int scoreValue = 150;

        private Transform _player;
        private Rigidbody2D _rb;
        private SpriteRenderer _sr;
        private Animator _anim;
        private int _currentHealth;
        private float _lastFireTime;
        private bool _isDead;

        static readonly int ANIM_SPEED = Animator.StringToHash("Speed");

        static Sprite _bulletSprite;

        void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _sr = GetComponent<SpriteRenderer>();
            _anim = GetComponent<Animator>();
            _rb.gravityScale = 0f;
            _rb.freezeRotation = true;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        void Start()
        {
            _currentHealth = maxHealth;
            var playerGO = GameObject.FindGameObjectWithTag("Player");
            if (playerGO != null) _player = playerGO.transform;
        }

        void FixedUpdate()
        {
            if (_isDead || _player == null) return;

            float dist = Vector2.Distance(transform.position, _player.position);

            if (dist > detectionRange)
            {
                _rb.linearVelocity = Vector2.zero;
                return;
            }

            Vector2 toPlayer = ((Vector2)_player.position - (Vector2)transform.position).normalized;

            if (dist < minDistance)
            {
                _rb.linearVelocity = -toPlayer * moveSpeed;
            }
            else if (dist > preferredRange)
            {
                _rb.linearVelocity = toPlayer * (moveSpeed * 0.65f);
            }
            else
            {
                // In range: strafe sideways
                Vector2 perp = new Vector2(-toPlayer.y, toPlayer.x);
                float strafe = Mathf.Sin(Time.time * 1.3f);
                _rb.linearVelocity = perp * (strafe * moveSpeed * 0.55f);
            }

            if (_sr != null && Mathf.Abs(_rb.linearVelocity.x) > 0.05f)
                _sr.flipX = _rb.linearVelocity.x < 0;

            if (_anim != null)
            {
                try { _anim.SetFloat(ANIM_SPEED, _rb.linearVelocity.magnitude); } catch { }
            }

            if (dist <= detectionRange && Time.time - _lastFireTime >= fireRate)
            {
                _lastFireTime = Time.time;
                FireAt(_player.position);
            }
        }

        void FireAt(Vector3 target)
        {
            Vector2 dir = ((Vector2)target - (Vector2)transform.position).normalized;

            var projGO = new GameObject("EnemyBullet");
            projGO.transform.position = transform.position + (Vector3)dir * 0.6f;
            projGO.tag = "Projectile";

            var sr = projGO.AddComponent<SpriteRenderer>();
            sr.sprite = GetBulletSprite();
            sr.color = new Color(1f, 0.35f, 0.1f);
            sr.sortingOrder = 3;
            projGO.transform.localScale = Vector3.one * 0.3f;

            var rb = projGO.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.linearVelocity = dir * projectileSpeed;

            var col = projGO.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.5f;

            var proj = projGO.AddComponent<EnemyProjectile>();
            proj.damage = projectileDamage;

            Destroy(projGO, 5f);
        }

        static Sprite GetBulletSprite()
        {
            if (_bulletSprite != null) return _bulletSprite;

            var tex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            var pixels = new Color[1024];
            var center = new Vector2(15.5f, 15.5f);
            for (int i = 0; i < 1024; i++)
            {
                int x = i % 32, y = i / 32;
                float d = Vector2.Distance(new Vector2(x, y), center);
                pixels[i] = d <= 15f ? Color.white : Color.clear;
            }
            tex.SetPixels(pixels);
            tex.Apply();
            _bulletSprite = Sprite.Create(tex, new Rect(0, 0, 32, 32), Vector2.one * 0.5f, 32f);
            return _bulletSprite;
        }

        public void TakeDamage(int amount)
        {
            if (_isDead) return;
            _currentHealth -= amount;
            Debug.Log($"[RangedEnemyAI] {gameObject.name} recibió {amount}. Vida: {_currentHealth}/{maxHealth}");
            StartCoroutine(DamageFlash());
            BIT.Core.VFXManager.Instance?.SpawnHitEffect(transform.position);
            if (_currentHealth <= 0) Die();
        }

        System.Collections.IEnumerator DamageFlash()
        {
            if (_sr == null) yield break;
            var orig = _sr.color;
            _sr.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            if (_sr != null) _sr.color = orig;
        }

        void Die()
        {
            if (_isDead) return;
            _isDead = true;

            int finalScore = BIT.Core.ComboManager.Instance != null
                ? BIT.Core.ComboManager.Instance.RegisterKill(scoreValue)
                : scoreValue;

            _player?.GetComponent<BIT.Player.PlayerController>()?.AddScore(finalScore);
            GetComponent<EnemyDropper>()?.Drop();
            BIT.Core.RuntimeGameManager.Instance?.PlayEnemyDeathSound();
            BIT.Core.RuntimeGameManager.Instance?.OnEnemyKilled();
            BIT.Core.VFXManager.Instance?.SpawnDeathEffect(transform.position);
            BIT.Core.WaveManager.Instance?.NotifyEnemyDied(gameObject);

            var col = GetComponent<Collider2D>();
            if (col) col.enabled = false;
            _rb.linearVelocity = Vector2.zero;

            Destroy(gameObject, 0.5f);
        }

        public void ScaleStats(float factor)
        {
            maxHealth = Mathf.RoundToInt(maxHealth * factor);
            _currentHealth = maxHealth;
            projectileDamage = Mathf.RoundToInt(projectileDamage * factor);
            moveSpeed = Mathf.Min(moveSpeed * (1f + (factor - 1f) * 0.2f), 6f);
            fireRate = Mathf.Max(0.8f, fireRate - (factor - 1f) * 0.25f);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, preferredRange);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, minDistance);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);
        }
    }
}
