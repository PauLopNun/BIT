using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using BIT.Player;
using BIT.Core;

// ============================================================================
// BOSSENEMYAI.CS - Enemigo jefe con fases de combate
// ============================================================================
// Spawneado por WaveManager cada N oleadas (configurable, por defecto cada 10).
//
// FASES:
//   Normal (100-50% vida): persigue al jugador, ataca por contacto
//   Enraged (50-25% vida): más rápido, carga hacia el jugador
//   Summoning (<25% vida): invoca minions y continúa en modo Enraged
//
// Muestra una barra de vida en el HUD (creada en runtime).
// ============================================================================

namespace BIT.Enemy
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class BossEnemyAI : MonoBehaviour, IDamageable
    {
        public enum BossPhase { Normal, Enraged, Summoning }

        [Header("=== STATS ===")]
        [SerializeField] private int _maxHealth = 500;
        [SerializeField] private int _attackDamage = 20;
        [SerializeField] private float _normalSpeed = 2.5f;
        [SerializeField] private float _enragedSpeed = 5f;
        [SerializeField] private float _attackRadius = 1.2f;
        [SerializeField] private float _attackCooldown = 1.5f;
        [SerializeField] private int _scoreReward = 1000;

        [Header("=== FASES ===")]
        [Tooltip("Vida (%) en la que entra en modo Enraged")]
        [SerializeField] private float _enrageAt = 0.5f;
        [Tooltip("Vida (%) en la que invoca minions")]
        [SerializeField] private float _summonAt = 0.25f;

        [Header("=== CARGA ===")]
        [SerializeField] private float _chargeSpeed = 14f;
        [SerializeField] private float _chargeCooldown = 5f;
        [SerializeField] private float _chargeDuration = 0.45f;

        [Header("=== MINIONS ===")]
        [Tooltip("Prefab de enemigo que invoca (puede dejarse vacío)")]
        [SerializeField] private GameObject _minionPrefab;
        [SerializeField] private int _minionsToSummon = 3;

        [Header("=== VISUAL ===")]
        [SerializeField] private Color _normalColor = new Color(0.5f, 0f, 0.5f);
        [SerializeField] private Color _enragedColor = new Color(1f, 0.2f, 0f);

        private int _currentHealth;
        private Rigidbody2D _rb;
        private SpriteRenderer _spriteRenderer;
        private Animator _animator;
        private Transform _player;
        private BossPhase _phase = BossPhase.Normal;
        private float _lastAttackTime;
        private float _lastChargeTime = -99f;
        private bool _isDead = false;
        private bool _isCharging = false;
        private bool _hasSummoned = false;

        // HP bar refs
        private GameObject _hpBarRoot;
        private Image _hpFill;

        static readonly int ANIM_SPEED = Animator.StringToHash("Speed");

        void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _animator = GetComponent<Animator>();

            _rb.gravityScale = 0f;
            _rb.freezeRotation = true;
            _rb.linearDamping = 2f;

            _currentHealth = _maxHealth;
        }

        void Start()
        {
            _player = GameObject.FindGameObjectWithTag("Player")?.transform;

            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = _normalColor;
            }
            transform.localScale = Vector3.one * 2f;

            StartCoroutine(CreateHPBarDelayed());
            StartCoroutine(EntranceEffect());
        }

        IEnumerator EntranceEffect()
        {
            RuntimeGameManager.Instance?.ShowBigMessage("¡¡BOSS INCOMING!!", Color.red);
            for (int i = 0; i < 8; i++)
            {
                if (_spriteRenderer != null)
                    _spriteRenderer.color = i % 2 == 0 ? Color.white : _normalColor;
                yield return new WaitForSeconds(0.18f);
            }
            if (_spriteRenderer != null) _spriteRenderer.color = _normalColor;
        }

        IEnumerator CreateHPBarDelayed()
        {
            yield return new WaitForSeconds(0.2f);
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas != null) CreateHPBar(canvas.transform);
        }

        void CreateHPBar(Transform parent)
        {
            _hpBarRoot = new GameObject("BossHPBar");
            _hpBarRoot.transform.SetParent(parent, false);

            var bg = _hpBarRoot.AddComponent<Image>();
            bg.color = new Color(0.12f, 0.04f, 0.04f);

            var bgRT = _hpBarRoot.GetComponent<RectTransform>();
            bgRT.anchorMin = new Vector2(0.15f, 0f);
            bgRT.anchorMax = new Vector2(0.85f, 0f);
            bgRT.pivot = new Vector2(0.5f, 0f);
            bgRT.anchoredPosition = new Vector2(0f, 16f);
            bgRT.sizeDelta = new Vector2(0f, 26f);

            // Fill
            var fillGO = new GameObject("Fill");
            fillGO.transform.SetParent(_hpBarRoot.transform, false);
            _hpFill = fillGO.AddComponent<Image>();
            _hpFill.color = new Color(0.85f, 0.1f, 0.1f);
            var fillRT = fillGO.GetComponent<RectTransform>();
            fillRT.anchorMin = Vector2.zero;
            fillRT.anchorMax = Vector2.one;
            fillRT.offsetMin = new Vector2(2, 2);
            fillRT.offsetMax = new Vector2(-2, -2);

            // Label
            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(_hpBarRoot.transform, false);
            var label = labelGO.AddComponent<Text>();
            label.text = "BOSS";
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 16;
            label.fontStyle = FontStyle.Bold;
            label.color = Color.white;
            label.alignment = TextAnchor.MiddleCenter;
            var labelRT = labelGO.GetComponent<RectTransform>();
            labelRT.anchorMin = Vector2.zero;
            labelRT.anchorMax = Vector2.one;
            labelRT.offsetMin = Vector2.zero;
            labelRT.offsetMax = Vector2.zero;
        }

        void Update()
        {
            if (_isDead) return;
            UpdateHPBar();
            UpdateAnimations();
        }

        void FixedUpdate()
        {
            if (_isDead || _player == null || _isCharging) return;

            float dist = Vector2.Distance(transform.position, _player.position);
            float speed = _phase == BossPhase.Normal ? _normalSpeed : _enragedSpeed;

            if (dist > _attackRadius)
            {
                Vector2 dir = ((Vector2)_player.position - (Vector2)transform.position).normalized;
                _rb.linearVelocity = dir * speed;
                if (_spriteRenderer != null) _spriteRenderer.flipX = dir.x < 0;
            }
            else
            {
                _rb.linearVelocity = Vector2.zero;
                TryMeleeAttack();
            }

            CheckPhaseTransitions();
        }

        void CheckPhaseTransitions()
        {
            float pct = (float)_currentHealth / _maxHealth;

            if (pct <= _enrageAt && _phase == BossPhase.Normal)
                EnterEnrage();

            if (pct <= _summonAt && !_hasSummoned)
                SummonMinions();

            if (_phase == BossPhase.Enraged && !_isCharging && Time.time - _lastChargeTime > _chargeCooldown)
                StartCoroutine(ChargeRoutine());
        }

        void EnterEnrage()
        {
            _phase = BossPhase.Enraged;
            if (_spriteRenderer != null) _spriteRenderer.color = _enragedColor;
            RuntimeGameManager.Instance?.ShowBigMessage("¡EL BOSS ENFURECE!", new Color(1f, 0.3f, 0f));
            Debug.Log("[Boss] Fase ENRAGED");
        }

        void TryMeleeAttack()
        {
            if (Time.time - _lastAttackTime < _attackCooldown) return;
            _lastAttackTime = Time.time;

            var pc = _player?.GetComponent<PlayerController>();
            if (pc != null)
            {
                pc.TakeDamage(_attackDamage);
                VFXManager.Instance?.SpawnHitEffect(_player.position);
            }
        }

        IEnumerator ChargeRoutine()
        {
            if (_isDead || _player == null) yield break;
            _lastChargeTime = Time.time;
            _isCharging = true;

            // Telegraph
            if (_spriteRenderer != null) _spriteRenderer.color = Color.white;
            _rb.linearVelocity = Vector2.zero;
            yield return new WaitForSeconds(0.35f);
            if (_spriteRenderer != null) _spriteRenderer.color = _enragedColor;

            // Charge
            if (_player != null && !_isDead)
            {
                Vector2 dir = ((Vector2)_player.position - (Vector2)transform.position).normalized;
                _rb.linearVelocity = dir * _chargeSpeed;
            }

            yield return new WaitForSeconds(_chargeDuration);
            if (!_isDead) _rb.linearVelocity = Vector2.zero;
            _isCharging = false;
        }

        void SummonMinions()
        {
            _hasSummoned = true;
            RuntimeGameManager.Instance?.ShowBigMessage("¡INVOCANDO REFUERZOS!", new Color(0.7f, 0.3f, 1f));

            if (_minionPrefab == null)
            {
                Debug.Log("[Boss] Sin prefab de minion asignado. Asigna _minionPrefab en el Inspector.");
                return;
            }

            for (int i = 0; i < _minionsToSummon; i++)
            {
                Vector2 offset = Random.insideUnitCircle.normalized * 2.5f;
                var minion = Instantiate(_minionPrefab, transform.position + (Vector3)offset, Quaternion.identity);
                WaveManager.Instance?.RegisterBossMinion(minion);
            }
        }

        public void TakeDamage(int damage, Vector2 knockbackDir = default)
        {
            if (_isDead) return;
            _currentHealth -= damage;
            StartCoroutine(DamageFlash());
            if (knockbackDir != Vector2.zero && _rb != null)
                _rb.linearVelocity = knockbackDir.normalized * 2.5f; // boss knockback reducido
            if (_currentHealth <= 0) Die();
        }

        IEnumerator DamageFlash()
        {
            if (_spriteRenderer == null) yield break;
            Color orig = _spriteRenderer.color;
            _spriteRenderer.color = Color.white;
            yield return new WaitForSeconds(0.08f);
            if (_spriteRenderer != null && !_isDead)
                _spriteRenderer.color = orig;
        }

        void Die()
        {
            if (_isDead) return;
            _isDead = true;

            Debug.Log("[Boss] ¡Boss derrotado!");
            WaveManager.Instance?.NotifyEnemyDied(gameObject);

            // Score with combo multiplier
            int finalScore = ComboManager.Instance != null
                ? ComboManager.Instance.RegisterKill(_scoreReward)
                : _scoreReward;
            _player?.GetComponent<PlayerController>()?.AddScore(finalScore);

            RuntimeGameManager.Instance?.ShowBigMessage("¡¡BOSS DERROTADO!!", new Color(1f, 0.85f, 0f));
            VFXManager.Instance?.SpawnDeathEffect(transform.position);

            // Double drop for boss
            var dropper = GetComponent<EnemyDropper>();
            dropper?.Drop();
            dropper?.ForceDrop();

            _rb.linearVelocity = Vector2.zero;
            if (_hpBarRoot != null) Destroy(_hpBarRoot);
            var col = GetComponent<Collider2D>();
            if (col != null) col.enabled = false;
            Destroy(gameObject, 0.5f);
        }

        void UpdateHPBar()
        {
            if (_hpFill == null) return;
            float pct = Mathf.Clamp01((float)_currentHealth / _maxHealth);
            // Shrink anchor to simulate fill
            _hpFill.rectTransform.anchorMax = new Vector2(pct, 1f);
            _hpFill.color = Color.Lerp(
                new Color(0.85f, 0.1f, 0.1f),
                new Color(0.1f, 0.85f, 0.1f),
                pct);
        }

        void UpdateAnimations()
        {
            if (_animator == null) return;
            try { _animator.SetFloat(ANIM_SPEED, _rb.linearVelocity.magnitude); }
            catch { }
        }

        // Called by WaveManager to scale this boss relative to wave number
        public void ScaleStats(float factor)
        {
            _maxHealth = Mathf.RoundToInt(_maxHealth * factor);
            _currentHealth = _maxHealth;
            _attackDamage = Mathf.RoundToInt(_attackDamage * factor);
            _scoreReward = Mathf.RoundToInt(_scoreReward * factor);
        }

        void OnDestroy()
        {
            if (_hpBarRoot != null) Destroy(_hpBarRoot);
        }
    }
}
