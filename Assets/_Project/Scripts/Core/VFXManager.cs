using UnityEngine;
using System.Collections;
using System.Linq;

// ============================================================================
// VFXMANAGER.CS - Sistema de efectos visuales
// ============================================================================
// Gestiona todos los efectos visuales del juego: slashes, particulas,
// efectos de impacto, etc.
// ============================================================================

namespace BIT.Core
{
    public class VFXManager : MonoBehaviour
    {
        // ====================================================================
        // SINGLETON
        // ====================================================================
        public static VFXManager Instance { get; private set; }

        // ====================================================================
        // PREFABS DE EFECTOS
        // ====================================================================
        [Header("Prefabs de Efectos")]
        public GameObject slashEffectPrefab;
        public GameObject hitEffectPrefab;
        public GameObject deathEffectPrefab;
        public GameObject pickupEffectPrefab;

        // ====================================================================
        // SPRITES CARGADOS EN RUNTIME
        // ====================================================================
        private Sprite[] _slashSprites;
        private Sprite[] _hitSprites;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        void Start()
        {
            LoadVFXSprites();
        }

        void LoadVFXSprites()
        {
            LoadWeaponSprite();
            Debug.Log("[VFXManager] Sistema VFX inicializado");
        }

        // ====================================================================
        // METODOS PUBLICOS
        // ====================================================================

        /// <summary>
        /// Spawn efecto de slash en la direccion dada
        /// </summary>
        public void SpawnSlash(Vector3 position, Vector2 direction)
        {
            if (slashEffectPrefab != null)
            {
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                GameObject slash = Instantiate(slashEffectPrefab, position, Quaternion.Euler(0, 0, angle));
                Destroy(slash, 0.3f);
            }
            else
            {
                StartCoroutine(SimpleSlashEffect(position, direction));
            }
        }

        private Sprite _weaponInHandSprite;
        private bool   _weaponLoaded;

        public void SpawnMeleeSwordSwing(Transform playerTransform, Vector2 direction)
        {
            if (!gameObject.activeInHierarchy) return;
            StartCoroutine(MeleeSlashEffect(playerTransform, direction));
        }

        IEnumerator MeleeSlashEffect(Transform playerTransform, Vector2 direction)
        {
            if (!_weaponLoaded) LoadWeaponSprite();

            float baseAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            if (_weaponInHandSprite != null)
                yield return StartCoroutine(SwingWeaponSprite(playerTransform, baseAngle));
            else
                yield return StartCoroutine(ProceduralSwordSwing(playerTransform.position, baseAngle));
        }

        // La espada sigue al jugador cada frame para no flotar al moverse
        IEnumerator SwingWeaponSprite(Transform playerTransform, float baseAngle)
        {
            var go = new GameObject("WeaponSwingVFX");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _weaponInHandSprite;
            sr.sortingOrder = 101;

            float duration    = 0.26f;
            float orbitRadius = 1.2f;
            float swingArc    = 130f;

            float startAngle = baseAngle + swingArc * 0.5f;
            float endAngle   = baseAngle - swingArc * 0.5f;

            float trailEvery = 0.035f;
            float lastTrail  = -1f;

            float elapsed = 0f;
            while (elapsed < duration && go != null && playerTransform != null)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float easedT = 1f - Mathf.Pow(1f - t, 2.5f);

                float currentAngle = Mathf.Lerp(startAngle, endAngle, easedT);
                float rad = currentAngle * Mathf.Deg2Rad;

                Vector3 playerPos = playerTransform.position;
                go.transform.position = playerPos + new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * orbitRadius;
                go.transform.rotation = Quaternion.Euler(0f, 0f, currentAngle - 90f);

                float scalePulse = 1f + Mathf.Sin(t * Mathf.PI) * 0.3f;
                go.transform.localScale = Vector3.one * 6.5f * scalePulse;

                sr.color = new Color(1f, 1f, 1f, t < 0.6f ? 1f : 1f - (t - 0.6f) / 0.4f);

                // solo trail durante la fase activa, no en el fade-out final
                if (t < 0.6f && elapsed - lastTrail >= trailEvery)
                {
                    lastTrail = elapsed;
                    StartCoroutine(SwordTrailGhost(
                        go.transform.position, go.transform.rotation,
                        go.transform.localScale, _weaponInHandSprite));
                }

                yield return null;
            }
            if (go != null) Destroy(go);
        }

        IEnumerator SwordTrailGhost(Vector3 pos, Quaternion rot, Vector3 scale, Sprite sprite)
        {
            var ghost = new GameObject("SwordTrail");
            ghost.transform.SetPositionAndRotation(pos, rot);
            ghost.transform.localScale = scale;

            var sr = ghost.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 99;

            float duration = 0.14f;
            float elapsed  = 0f;
            while (elapsed < duration && ghost != null)
            {
                elapsed += Time.deltaTime;
                float alpha = (1f - Mathf.Clamp01(elapsed / duration)) * 0.45f;
                sr.color = new Color(0.85f, 0.95f, 1f, alpha);
                yield return null;
            }
            if (ghost != null) Destroy(ghost);
        }

        void LoadWeaponSprite()
        {
            _weaponLoaded = true;
#if UNITY_EDITOR
            string[] candidates = {
                "Assets/_Project/Sprites/Ninja Adventure/Items/Weapons/BigSword/Sprite.png",
                "Assets/_Project/Sprites/Ninja Adventure/Items/Weapons/Sword/Sprite.png",
                "Assets/_Project/Sprites/Ninja Adventure/Items/Weapons/Sword2/Sprite.png",
                "Assets/_Project/Sprites/Ninja Adventure/Items/Weapons/BigSword/SpriteInHand.png",
            };
            foreach (var path in candidates)
            {
                _weaponInHandSprite = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path)
                    .OfType<Sprite>().FirstOrDefault();
                if (_weaponInHandSprite != null)
                {
                    Debug.Log($"[VFXManager] Arma melee cargada: {System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(path))}");
                    return;
                }
            }
            Debug.LogWarning("[VFXManager] Arma melee no encontrada — ejecuta BIT > 1b. Reconfigurar Sprites");
#endif
        }

        IEnumerator ProceduralSwordSwing(Vector3 playerPos, float baseAngle)
        {
            float[] angleOffsets = { -40f, 0f, 40f };
            float[] scales       = { 1.4f, 2.0f, 1.4f };

            for (int i = 0; i < angleOffsets.Length; i++)
            {
                float rad = (baseAngle + angleOffsets[i]) * Mathf.Deg2Rad;
                Vector3 pos = playerPos + new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * 0.7f;

                var go = new GameObject("SwordSlash");
                go.transform.position = pos;
                go.transform.rotation = Quaternion.Euler(0f, 0f, baseAngle + angleOffsets[i] - 90f);
                go.transform.localScale = Vector3.one * scales[i];

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = CreateSlashSprite();
                sr.color = new Color(1f, 0.95f, 0.45f);
                sr.sortingOrder = 25;

                StartCoroutine(AnimateSlashFade(go, 0.25f));
                yield return new WaitForSeconds(0.045f);
            }
        }

        IEnumerator AnimateSlashFade(GameObject go, float duration)
        {
            if (go == null) yield break;
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr == null) { Destroy(go); yield break; }

            Vector3 startScale = go.transform.localScale;
            float elapsed = 0f;
            while (elapsed < duration && go != null)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                if (go != null && sr != null)
                {
                    go.transform.localScale = startScale * (1f + t * 0.9f);
                    sr.color = new Color(1f, 0.95f, 0.45f, 1f - t);
                }
                yield return null;
            }
            if (go != null) Destroy(go);
        }

        /// <summary>
        /// Spawn efecto de impacto
        /// </summary>
        public void SpawnHitEffect(Vector3 position)
        {
            if (hitEffectPrefab != null)
            {
                GameObject hit = Instantiate(hitEffectPrefab, position, Quaternion.identity);
                Destroy(hit, 0.5f);
            }
            else
            {
                StartCoroutine(SimpleHitEffect(position));
            }
        }

        /// <summary>
        /// Spawn efecto de muerte de enemigo
        /// </summary>
        public void SpawnDeathEffect(Vector3 position)
        {
            if (deathEffectPrefab != null)
            {
                GameObject death = Instantiate(deathEffectPrefab, position, Quaternion.identity);
                Destroy(death, 1f);
            }
            else
            {
                StartCoroutine(SimpleDeathEffect(position));
            }
        }

        /// <summary>
        /// Spawn efecto de recoger item
        /// </summary>
        public void SpawnPickupEffect(Vector3 position, Color color)
        {
            StartCoroutine(SimplePickupEffect(position, color));
        }

        // ====================================================================
        // EFECTOS SIMPLES (Fallback sin prefabs)
        // ====================================================================

        IEnumerator SimpleSlashEffect(Vector3 position, Vector2 direction)
        {
            // Crear sprite de slash simple
            GameObject slashGO = new GameObject("SlashEffect");
            slashGO.transform.position = position;

            SpriteRenderer sr = slashGO.AddComponent<SpriteRenderer>();
            sr.sprite = CreateSlashSprite();
            sr.color = new Color(1f, 1f, 0.8f, 0.9f);
            sr.sortingOrder = 20;

            // Rotar hacia la direccion
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            slashGO.transform.rotation = Quaternion.Euler(0, 0, angle - 90);

            // Animacion de escala y fade
            float duration = 0.2f;
            float elapsed = 0f;

            Vector3 startScale = Vector3.one * 0.5f;
            Vector3 endScale = Vector3.one * 1.5f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                slashGO.transform.localScale = Vector3.Lerp(startScale, endScale, t);
                sr.color = new Color(1f, 1f, 0.8f, 1f - t);

                yield return null;
            }

            Destroy(slashGO);
        }

        IEnumerator SimpleHitEffect(Vector3 position)
        {
            // Crear particulas de impacto
            int particleCount = 6;

            for (int i = 0; i < particleCount; i++)
            {
                GameObject particle = new GameObject($"HitParticle_{i}");
                particle.transform.position = position;

                SpriteRenderer sr = particle.AddComponent<SpriteRenderer>();
                sr.sprite = CreateCircleSprite();
                sr.color = new Color(1f, 0.8f, 0.2f);
                sr.sortingOrder = 20;

                particle.transform.localScale = Vector3.one * Random.Range(0.1f, 0.3f);

                // Direccion aleatoria
                Vector2 dir = Random.insideUnitCircle.normalized;
                StartCoroutine(MoveAndFadeParticle(particle, dir, 0.3f));
            }

            yield return null;
        }

        IEnumerator SimpleDeathEffect(Vector3 position)
        {
            // Efecto de humo/explosion
            int particleCount = 8;

            for (int i = 0; i < particleCount; i++)
            {
                GameObject particle = new GameObject($"DeathParticle_{i}");
                particle.transform.position = position;

                SpriteRenderer sr = particle.AddComponent<SpriteRenderer>();
                sr.sprite = CreateCircleSprite();
                sr.color = new Color(0.5f, 0.5f, 0.5f, 0.8f);
                sr.sortingOrder = 20;

                particle.transform.localScale = Vector3.one * Random.Range(0.3f, 0.6f);

                Vector2 dir = Random.insideUnitCircle.normalized;
                StartCoroutine(MoveAndFadeParticle(particle, dir * 0.5f, 0.5f));
            }

            yield return null;
        }

        IEnumerator SimplePickupEffect(Vector3 position, Color color)
        {
            // Particulas brillantes hacia arriba
            int particleCount = 5;

            for (int i = 0; i < particleCount; i++)
            {
                GameObject particle = new GameObject($"PickupParticle_{i}");
                particle.transform.position = position + new Vector3(Random.Range(-0.3f, 0.3f), 0, 0);

                SpriteRenderer sr = particle.AddComponent<SpriteRenderer>();
                sr.sprite = CreateCircleSprite();
                sr.color = color;
                sr.sortingOrder = 20;

                particle.transform.localScale = Vector3.one * Random.Range(0.1f, 0.2f);

                Vector2 dir = Vector2.up + new Vector2(Random.Range(-0.3f, 0.3f), 0);
                StartCoroutine(MoveAndFadeParticle(particle, dir, 0.4f));
            }

            yield return null;
        }

        IEnumerator MoveAndFadeParticle(GameObject particle, Vector2 direction, float duration)
        {
            if (particle == null) yield break;

            SpriteRenderer sr = particle.GetComponent<SpriteRenderer>();
            Color startColor = sr.color;
            Vector3 startPos = particle.transform.position;
            float speed = 3f;
            float elapsed = 0f;

            while (elapsed < duration && particle != null)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                particle.transform.position = startPos + (Vector3)(direction * speed * t);
                sr.color = new Color(startColor.r, startColor.g, startColor.b, startColor.a * (1f - t));

                yield return null;
            }

            if (particle != null)
                Destroy(particle);
        }

        // ====================================================================
        // CREAR SPRITES PROCEDURALES
        // ====================================================================

        Sprite CreateSlashSprite()
        {
            int size = 32;
            Texture2D tex = new Texture2D(size, size);
            tex.filterMode = FilterMode.Point;

            // Crear forma de slash (arco)
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - size / 2f;
                    float dy = y - size / 2f;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float angle = Mathf.Atan2(dy, dx);

                    // Arco de slash
                    bool inArc = dist > 8 && dist < 14 && angle > -0.5f && angle < 2f;

                    if (inArc)
                    {
                        float alpha = 1f - (Mathf.Abs(dist - 11) / 3f);
                        tex.SetPixel(x, y, new Color(1, 1, 1, alpha));
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), Vector2.one * 0.5f, size);
        }

        Sprite CreateCircleSprite()
        {
            int size = 8;
            Texture2D tex = new Texture2D(size, size);
            tex.filterMode = FilterMode.Point;

            float center = size / 2f;
            float radius = size / 2f - 1;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center + 0.5f;
                    float dy = y - center + 0.5f;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    if (dist <= radius)
                    {
                        tex.SetPixel(x, y, Color.white);
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), Vector2.one * 0.5f, size);
        }
    }
}
