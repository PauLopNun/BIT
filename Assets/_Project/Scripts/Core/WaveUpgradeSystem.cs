using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using BIT.Player;

namespace BIT.Core
{
    // Tienda entre oleadas: el jugador gasta monedas para comprar mejoras.
    // Se abre tras cada oleada superada. Pausa el juego mientras está abierta.
    // Permite comprar varios ítems hasta quedarse sin monedas o pulsar "Siguiente Ronda".
    public class WaveUpgradeSystem : MonoBehaviour
    {
        public static WaveUpgradeSystem Instance { get; private set; }

        [Header("=== CONFIG ===")]
        [Tooltip("Número de opciones que se ofrecen al jugador")]
        [SerializeField] private int _optionCount = 3;

        private GameObject _shopPanel;
        private Text _coinBalanceText;
        private List<CardEntry> _cards = new List<CardEntry>();
        private PlayerController _player;

        class CardEntry
        {
            public GameObject go;
            public Button btn;
            public Text priceText;
            public int cost;
            public string name;
            public bool purchased;
            public System.Action<PlayerController> apply;
        }

        struct UpgradeOption
        {
            public string name, description;
            public int cost;
            public System.Action<PlayerController> apply;
        }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        void Start() => StartCoroutine(Init());

        IEnumerator Init()
        {
            yield return null;
            _player = FindFirstObjectByType<PlayerController>();
            if (WaveManager.Instance != null)
                WaveManager.Instance.OnWaveCleared += OnWaveCleared;
            yield return new WaitForSeconds(0.35f);
            CreateShopPanel();
        }

        void OnDestroy()
        {
            if (WaveManager.Instance != null)
                WaveManager.Instance.OnWaveCleared -= OnWaveCleared;
        }

        void OnWaveCleared(int wave)
        {
            bool wasBoss = WaveManager.Instance != null && WaveManager.Instance.LastWaveWasBoss;
            StartCoroutine(wasBoss ? ShowBossRewardDelayed() : ShowShopDelayed());
        }

        IEnumerator ShowShopDelayed()
        {
            yield return new WaitForSeconds(2f);
            ShowShop();
        }

        IEnumerator ShowBossRewardDelayed()
        {
            yield return new WaitForSeconds(2.5f);
            ShowBossReward();
        }

        void ShowBossReward()
        {
            if (_player == null) _player = FindFirstObjectByType<PlayerController>();
            if (_player == null || _shopPanel == null) return;

            Time.timeScale = 0f;

            var all = AllUpgrades();
            var chosen = new List<UpgradeOption>();
            while (chosen.Count < 2 && all.Count > 0)
            {
                int idx = Random.Range(0, all.Count);
                chosen.Add(all[idx]);
                all.RemoveAt(idx);
            }

            foreach (Transform child in _shopPanel.transform)
                Destroy(child.gameObject);
            _cards.Clear();

            int wave = WaveManager.Instance != null ? WaveManager.Instance.CurrentWave : 1;
            CreateLabel("¡RECOMPENSA DE BOSS!", 38, new Color(1f, 0.75f, 0f), new Vector2(0f, 0.87f), new Vector2(1f, 1f));
            CreateLabel("¡Elige UNA mejora GRATIS por derrotar al boss!", 18,
                new Color(0.85f, 0.85f, 0.85f), new Vector2(0f, 0.79f), new Vector2(1f, 0.87f));

            float totalSpacing = 1f - chosen.Count * 0.30f;
            float gap = totalSpacing / (chosen.Count + 1);
            for (int i = 0; i < chosen.Count; i++)
            {
                float xMin = gap + i * (0.30f + gap);
                CreateBossRewardCard(chosen[i], xMin, xMin + 0.30f);
            }

            CreateNextRoundButton();
            _shopPanel.SetActive(true);
        }

        void CreateBossRewardCard(UpgradeOption option, float xMin, float xMax)
        {
            var card = new GameObject("BossRewardCard");
            card.transform.SetParent(_shopPanel.transform, false);

            var bg = card.AddComponent<Image>();
            bg.color = new Color(0.10f, 0.08f, 0.04f, 0.97f);

            var btn = card.AddComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = new Color(0.35f, 0.28f, 0.08f);
            colors.pressedColor     = new Color(0.55f, 0.45f, 0.10f);
            btn.colors = colors;

            var cardRT = card.GetComponent<RectTransform>();
            cardRT.anchorMin = new Vector2(xMin, 0.13f);
            cardRT.anchorMax = new Vector2(xMax, 0.72f);
            cardRT.offsetMin = new Vector2(6, 6);
            cardRT.offsetMax = new Vector2(-6, -6);

            var outline = card.AddComponent<Outline>();
            outline.effectColor    = new Color(1f, 0.75f, 0f, 0.8f);
            outline.effectDistance = new Vector2(3, -3);

            AddCardText(card, option.name,        20, FontStyles.Bold,   new Color(1f, 0.88f, 0.2f), new Vector2(0f, 0.68f), new Vector2(1f, 1f));
            AddCardText(card, option.description, 16, FontStyles.Normal, Color.white,                new Vector2(0f, 0.22f), new Vector2(1f, 0.68f));
            AddCardText(card, "GRATIS",           22, FontStyles.Bold,   new Color(0.3f, 1f, 0.4f), new Vector2(0f, 0f),    new Vector2(1f, 0.22f));

            var entry = new CardEntry
            {
                go = card, btn = btn, priceText = null,
                cost = 0, name = option.name, purchased = false, apply = option.apply,
            };
            _cards.Add(entry);

            var captured = entry;
            btn.onClick.AddListener(() =>
            {
                if (captured.purchased) return;
                captured.purchased = true;
                captured.apply(_player);
                var b = captured.go.GetComponent<Image>();
                if (b != null) b.color = new Color(0.04f, 0.10f, 0.04f, 0.97f);
                captured.btn.interactable = false;
                // desactivar las otras cartas (solo una gratis)
                foreach (var e in _cards)
                    if (e != captured) e.btn.interactable = false;
                RuntimeGameManager.Instance?.ShowBigMessage($"¡{captured.name}!", new Color(1f, 0.85f, 0.2f));
            });
        }

        static void AddCardText(GameObject parent, string text, int size, FontStyles style,
            Color color, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject("T");
            go.transform.SetParent(parent.transform, false);
            var tmp = go.AddComponent<TMPro.TextMeshProUGUI>();
            tmp.text = text; tmp.fontSize = size; tmp.fontStyle = style;
            tmp.color = color; tmp.alignment = TMPro.TextAlignmentOptions.Center;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
            rt.offsetMin = new Vector2(5, 4); rt.offsetMax = new Vector2(-5, -4);
        }

        List<UpgradeOption> AllUpgrades() => new List<UpgradeOption>
        {
            new UpgradeOption { name = "+ Vida Máxima",   description = "+30 Vida Máx\n+30 Curación",       cost = 5, apply = p => { p.maxHealth += 30; p.Heal(30); } },
            new UpgradeOption { name = "+ Velocidad",      description = "Movimiento 20%\nmás rápido",        cost = 3, apply = p => p.moveSpeed *= 1.20f },
            new UpgradeOption { name = "+ Daño Melee",     description = "+10 daño\ncuerpo a cuerpo",         cost = 4, apply = p => p.meleeDamage += 10 },
            new UpgradeOption { name = "Curación Total",   description = "Recupera 60\npuntos de vida",       cost = 4, apply = p => p.Heal(60) },
            new UpgradeOption { name = "+ Vel. Ataque",    description = "Cooldown melee\n25% más corto",     cost = 3, apply = p => p.attackCooldown = Mathf.Max(0.1f, p.attackCooldown * 0.75f) },
            new UpgradeOption { name = "+ Área Ataque",    description = "Rango melee\n30% mayor",            cost = 3, apply = p => p.meleeRange *= 1.30f },
            new UpgradeOption { name = "Escudo",           description = "+50 Vida Máx\nsin curación",        cost = 6, apply = p => p.maxHealth += 50 },
            new UpgradeOption { name = "Ataque Veloz",     description = "+5 daño\n+10% velocidad",           cost = 5, apply = p => { p.meleeDamage += 5; p.moveSpeed *= 1.10f; } },
            new UpgradeOption { name = "Shuriken Veloz",   description = "Cooldown shuriken\n35% más corto",   cost = 4, apply = p => p.shurikenCooldown = Mathf.Max(0.15f, p.shurikenCooldown * 0.65f) },
            new UpgradeOption { name = "Shuriken Pesado",  description = "+14 daño\nen shuriken",              cost = 4, apply = p => p.shurikenDamage += 14 },
            new UpgradeOption { name = "Velocidad Ninja",  description = "Movimiento 30%\nmás rápido",         cost = 5, apply = p => p.moveSpeed *= 1.30f },
            new UpgradeOption { name = "Furia Total",      description = "+15 daño melee\n+10 shuriken",       cost = 8, apply = p => { p.meleeDamage += 15; p.shurikenDamage += 10; } },
            new UpgradeOption { name = "Dash Mejorado",    description = "Vel. de dash\n+30%",                cost = 3, apply = p => p.dashSpeed *= 1.30f },
            new UpgradeOption { name = "+ Daño Shuriken",  description = "+10 daño\nen shuriken",             cost = 4, apply = p => p.shurikenDamage += 10 },
        };

        void ShowShop()
        {
            if (_player == null) _player = FindFirstObjectByType<PlayerController>();
            if (_player == null || _shopPanel == null) return;

            Time.timeScale = 0f;

            var all = AllUpgrades();
            var chosen = new List<UpgradeOption>();
            int count = Mathf.Min(_optionCount, all.Count);
            while (chosen.Count < count)
            {
                int idx = Random.Range(0, all.Count);
                chosen.Add(all[idx]);
                all.RemoveAt(idx);
            }

            foreach (Transform child in _shopPanel.transform)
                Destroy(child.gameObject);
            _cards.Clear();

            int wave = WaveManager.Instance != null ? WaveManager.Instance.CurrentWave : 1;

            // Descuento en rondas tempranas: hay menos enemigos y por tanto menos monedas
            float priceMultiplier = wave <= 2 ? 0.5f : wave <= 4 ? 0.75f : 1.0f;
            for (int i = 0; i < chosen.Count; i++)
            {
                var opt = chosen[i];
                opt.cost = Mathf.Max(1, Mathf.RoundToInt(opt.cost * priceMultiplier));
                chosen[i] = opt;
            }

            string discountLabel = wave <= 2 ? " — ¡50% descuento!" : wave <= 4 ? " — ¡25% descuento!" : "";
            CreateLabel("¡TIENDA!", 42, new Color(1f, 0.88f, 0.15f), new Vector2(0f, 0.87f), new Vector2(1f, 1f));
            CreateLabel($"Oleada {wave} superada — gasta tus monedas{discountLabel}", 18,
                new Color(0.75f, 0.75f, 0.75f), new Vector2(0f, 0.79f), new Vector2(1f, 0.87f));

            // Balance de monedas
            var balGO = new GameObject("CoinBalance");
            balGO.transform.SetParent(_shopPanel.transform, false);
            _coinBalanceText = balGO.AddComponent<Text>();
            _coinBalanceText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _coinBalanceText.fontSize = 26;
            _coinBalanceText.fontStyle = FontStyle.Bold;
            _coinBalanceText.color = new Color(1f, 0.85f, 0.1f);
            _coinBalanceText.alignment = TextAnchor.MiddleCenter;
            var balRT = balGO.GetComponent<RectTransform>();
            balRT.anchorMin = new Vector2(0.25f, 0.71f);
            balRT.anchorMax = new Vector2(0.75f, 0.80f);
            balRT.offsetMin = balRT.offsetMax = Vector2.zero;
            RefreshBalanceText();

            // Cartas de mejora
            float totalSpacing = 1f - chosen.Count * 0.28f;
            float gap = totalSpacing / (chosen.Count + 1);
            for (int i = 0; i < chosen.Count; i++)
            {
                float xMin = gap + i * (0.28f + gap);
                CreateShopCard(chosen[i], xMin, xMin + 0.28f);
            }

            CreateNextRoundButton();
            _shopPanel.SetActive(true);
            RefreshCardAffordability();
        }

        void RefreshBalanceText()
        {
            if (_coinBalanceText == null) return;
            int coins = RuntimeGameManager.Instance != null ? RuntimeGameManager.Instance.Coins : 0;
            _coinBalanceText.text = $"$ Monedas disponibles: {coins}";
        }

        void CreateShopCard(UpgradeOption option, float xMin, float xMax)
        {
            var card = new GameObject("ShopCard");
            card.transform.SetParent(_shopPanel.transform, false);

            var bg = card.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.10f, 0.18f, 0.97f);

            var btn = card.AddComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = new Color(0.18f, 0.28f, 0.48f);
            colors.pressedColor     = new Color(0.28f, 0.48f, 0.78f);
            btn.colors = colors;

            var cardRT = card.GetComponent<RectTransform>();
            cardRT.anchorMin = new Vector2(xMin, 0.13f);
            cardRT.anchorMax = new Vector2(xMax, 0.70f);
            cardRT.offsetMin = new Vector2(6, 6);
            cardRT.offsetMax = new Vector2(-6, -6);

            var outline = card.AddComponent<Outline>();
            outline.effectColor    = new Color(0.35f, 0.55f, 1f, 0.65f);
            outline.effectDistance = new Vector2(3, -3);

            // Nombre
            var nameGO = new GameObject("Name");
            nameGO.transform.SetParent(card.transform, false);
            var nameText = nameGO.AddComponent<Text>();
            nameText.text      = option.name;
            nameText.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            nameText.fontSize  = 20;
            nameText.fontStyle = FontStyle.Bold;
            nameText.color     = new Color(1f, 0.85f, 0.28f);
            nameText.alignment = TextAnchor.MiddleCenter;
            var nameRT = nameGO.GetComponent<RectTransform>();
            nameRT.anchorMin = new Vector2(0f, 0.68f);
            nameRT.anchorMax = new Vector2(1f, 1f);
            nameRT.offsetMin = new Vector2(5, 4);
            nameRT.offsetMax = new Vector2(-5, -4);

            // Descripción
            var descGO = new GameObject("Desc");
            descGO.transform.SetParent(card.transform, false);
            var descText = descGO.AddComponent<Text>();
            descText.text      = option.description;
            descText.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            descText.fontSize  = 16;
            descText.color     = Color.white;
            descText.alignment = TextAnchor.MiddleCenter;
            var descRT = descGO.GetComponent<RectTransform>();
            descRT.anchorMin = new Vector2(0f, 0.28f);
            descRT.anchorMax = new Vector2(1f, 0.68f);
            descRT.offsetMin = new Vector2(5, 4);
            descRT.offsetMax = new Vector2(-5, -4);

            // Precio
            var priceGO = new GameObject("Price");
            priceGO.transform.SetParent(card.transform, false);
            var priceText = priceGO.AddComponent<Text>();
            priceText.text      = $"$ {option.cost} monedas";
            priceText.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            priceText.fontSize  = 20;
            priceText.fontStyle = FontStyle.Bold;
            priceText.color     = new Color(1f, 0.85f, 0.1f);
            priceText.alignment = TextAnchor.MiddleCenter;
            var priceRT = priceGO.GetComponent<RectTransform>();
            priceRT.anchorMin = new Vector2(0f, 0.02f);
            priceRT.anchorMax = new Vector2(1f, 0.28f);
            priceRT.offsetMin = new Vector2(5, 4);
            priceRT.offsetMax = new Vector2(-5, -4);

            var entry = new CardEntry
            {
                go        = card,
                btn       = btn,
                priceText = priceText,
                cost      = option.cost,
                name      = option.name,
                purchased = false,
                apply     = option.apply,
            };
            _cards.Add(entry);

            var captured = entry;
            btn.onClick.AddListener(() => BuyUpgrade(captured));
        }

        void BuyUpgrade(CardEntry entry)
        {
            if (entry.purchased) return;
            if (RuntimeGameManager.Instance == null) return;
            if (!RuntimeGameManager.Instance.SpendCoins(entry.cost)) return;

            entry.purchased = true;
            entry.apply(_player);

            // Visual: marcar como comprado
            var bg = entry.go.GetComponent<Image>();
            if (bg != null) bg.color = new Color(0.04f, 0.10f, 0.04f, 0.97f);
            entry.priceText.text  = "✓ Comprado";
            entry.priceText.color = new Color(0.3f, 1f, 0.3f);
            entry.btn.interactable = false;

            RefreshBalanceText();
            RefreshCardAffordability();

            RuntimeGameManager.Instance.ShowBigMessage($"¡{entry.name}!", new Color(0.4f, 1f, 0.4f));
            Debug.Log($"[ShopSystem] Comprado: {entry.name} por {entry.cost} monedas");
        }

        void RefreshCardAffordability()
        {
            int coins = RuntimeGameManager.Instance != null ? RuntimeGameManager.Instance.Coins : 0;
            foreach (var entry in _cards)
            {
                if (entry.purchased) continue;
                bool canAfford = coins >= entry.cost;
                entry.btn.interactable = canAfford;
                var bg = entry.go.GetComponent<Image>();
                if (bg != null)
                    bg.color = canAfford
                        ? new Color(0.08f, 0.10f, 0.18f, 0.97f)
                        : new Color(0.05f, 0.05f, 0.09f, 0.97f);
                entry.priceText.color = canAfford
                    ? new Color(1f, 0.85f, 0.1f)
                    : new Color(0.45f, 0.45f, 0.28f);
            }
        }

        void CreateNextRoundButton()
        {
            var btnGO = new GameObject("NextRoundBtn");
            btnGO.transform.SetParent(_shopPanel.transform, false);

            var bg = btnGO.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.32f, 0.08f, 0.97f);

            var btn = btnGO.AddComponent<Button>();
            var btnColors = btn.colors;
            btnColors.highlightedColor = new Color(0.15f, 0.50f, 0.15f);
            btnColors.pressedColor     = new Color(0.28f, 0.68f, 0.28f);
            btn.colors = btnColors;

            var btnRT = btnGO.GetComponent<RectTransform>();
            btnRT.anchorMin = new Vector2(0.32f, 0.01f);
            btnRT.anchorMax = new Vector2(0.68f, 0.11f);
            btnRT.offsetMin = new Vector2(4, 4);
            btnRT.offsetMax = new Vector2(-4, -4);

            var outl = btnGO.AddComponent<Outline>();
            outl.effectColor    = new Color(0.3f, 1f, 0.3f, 0.6f);
            outl.effectDistance = new Vector2(2, -2);

            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(btnGO.transform, false);
            var labelText = labelGO.AddComponent<Text>();
            labelText.text      = "SIGUIENTE RONDA  →";
            labelText.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            labelText.fontSize  = 22;
            labelText.fontStyle = FontStyle.Bold;
            labelText.color     = Color.white;
            labelText.alignment = TextAnchor.MiddleCenter;
            var labelRT = labelGO.GetComponent<RectTransform>();
            labelRT.anchorMin = Vector2.zero;
            labelRT.anchorMax = Vector2.one;
            labelRT.offsetMin = labelRT.offsetMax = Vector2.zero;

            btn.onClick.AddListener(CloseShop);
        }

        void CloseShop()
        {
            _shopPanel.SetActive(false);
            Time.timeScale = 1f;
        }

        void CreateLabel(string text, int fontSize, Color color, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(_shopPanel.transform, false);
            var t = go.AddComponent<Text>();
            t.text      = text;
            t.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize  = fontSize;
            t.fontStyle = FontStyle.Bold;
            t.color     = color;
            t.alignment = TextAnchor.MiddleCenter;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        void CreateShopPanel()
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) return;

            _shopPanel = new GameObject("ShopPanel");
            _shopPanel.transform.SetParent(canvas.transform, false);

            var bg = _shopPanel.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.93f);

            var rt = _shopPanel.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            _shopPanel.SetActive(false);
        }
    }
}
