using UnityEngine;
using System.Collections;

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
            // Los sprites se cargan desde el NinjaAdventureSetup
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
                // Crear efecto simple si no hay prefab
                StartCoroutine(SimpleSlashEffect(position, direction));
            }
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
