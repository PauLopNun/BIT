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
            LoadSlashSprites();
            LoadKatanaSprites();
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

        // Cache de sprites de VFX de melee
        private Sprite[] _katanaSprites;
        private bool _katanaLoaded;
        private Sprite[] _circularSlashSprites;
        private bool _slashLoaded;

        /// <summary>
        /// Animación de espada usando sprites del asset pack (CircularSlash o Katana como fallback)
        /// </summary>
        public void SpawnMeleeSwordSwing(Vector3 playerPos, Vector2 direction)
        {
            if (!gameObject.activeInHierarchy) return;
            StartCoroutine(MeleeSlashEffect(playerPos, direction));
        }

        IEnumerator MeleeSlashEffect(Vector3 playerPos, Vector2 direction)
        {
            if (!_slashLoaded)  LoadSlashSprites();
            if (!_katanaLoaded) LoadKatanaSprites();

            float baseAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            // Prioridad: CircularSlash FX > Katana weapon > procedural
            Sprite[] sprites = null;
            float scale = 6f;

            if (_circularSlashSprites != null && _circularSlashSprites.Length > 0)
            {
                sprites = _circularSlashSprites;
                scale = 6f;   // FX sprites suelen tener PPU=16 → 1 unidad → escala visual grande
            }
            else if (_katanaSprites != null && _katanaSprites.Length > 0)
            {
                sprites = _katanaSprites;
                scale = 14f;  // weapon sprites tienen PPU=100 → necesitan más escala
            }

            if (sprites != null && sprites.Length > 0)
            {
                var go = new GameObject("MeleeSlashVFX");
                go.transform.position = playerPos + (Vector3)direction * 0.7f;
                go.transform.rotation = Quaternion.Euler(0f, 0f, baseAngle - 90f);
                go.transform.localScale = Vector3.one * scale;

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sortingOrder = 100;   // Por encima de tiles, player y UI del juego

                int frames = Mathf.Min(sprites.Length, 8);
                for (int i = 0; i < frames; i++)
                {
                    if (go == null) yield break;
                    sr.sprite = sprites[i];
                    float alpha = 1f - (float)i / frames;
                    sr.color = new Color(1f, 1f, 1f, alpha);
                    yield return new WaitForSeconds(0.04f);
                }
                if (go != null) Destroy(go);
            }
            else
            {
                yield return StartCoroutine(ProceduralSwordSwing(playerPos, baseAngle));
            }
        }

        void LoadSlashSprites()
        {
            _slashLoaded = true;
#if UNITY_EDITOR
            const string PATH = "Assets/_Project/Sprites/Ninja Adventure/FX/Attack/CircularSlash/SpriteSheet.png";
            _circularSlashSprites = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(PATH)
                .OfType<Sprite>()
                .OrderBy(s =>
                {
                    int i = s.name.LastIndexOf('_');
                    return i >= 0 && int.TryParse(s.name.Substring(i + 1), out int n) ? n : 9999;
                })
                .ToArray();
            if (_circularSlashSprites != null && _circularSlashSprites.Length > 0)
                Debug.Log($"[VFXManager] CircularSlash sprites cargados: {_circularSlashSprites.Length}");
            else
                Debug.LogWarning("[VFXManager] CircularSlash/SpriteSheet.png no encontrado — probando Katana");
#endif
        }

        void LoadKatanaSprites()
        {
            _katanaLoaded = true;
#if UNITY_EDITOR
            const string PATH = "Assets/_Project/Sprites/Ninja Adventure/Actor/CharacterAnimated/Weapon/Katana.png";
            _katanaSprites = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(PATH)
                .OfType<Sprite>()
                .OrderBy(s =>
                {
                    int i = s.name.LastIndexOf('_');
                    return i >= 0 && int.TryParse(s.name.Substring(i + 1), out int n) ? n : 9999;
                })
                .ToArray();
            if (_katanaSprites != null && _katanaSprites.Length > 0)
                Debug.Log($"[VFXManager] Katana sprites cargados: {_katanaSprites.Length}");
            else
                Debug.LogWarning("[VFXManager] Katana.png no encontrado — usando VFX procedural");
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
