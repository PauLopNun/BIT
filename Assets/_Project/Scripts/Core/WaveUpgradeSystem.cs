using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using BIT.Player;

namespace BIT.Core
{
    // Shows an upgrade selection panel every N waves.
    // Pauses the game (timeScale 0) while the player picks one of 3 random upgrades.
    // The panel and cards are created entirely at runtime — no scene setup needed.
    public class WaveUpgradeSystem : MonoBehaviour
    {
        public static WaveUpgradeSystem Instance { get; private set; }

        [Header("=== CONFIG ===")]
        [Tooltip("Mostrar mejoras cada N oleadas")]
        [SerializeField] private int _upgradeEveryNWaves = 3;
        [Tooltip("Número de opciones que se ofrecen al jugador")]
        [SerializeField] private int _optionCount = 3;

        private GameObject _upgradePanel;
        private bool _waitingForChoice = false;
        private PlayerController _player;

        struct UpgradeOption
        {
            public string name;
            public string description;
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
            CreateUpgradePanel();
        }

        void OnDestroy()
        {
            if (WaveManager.Instance != null)
                WaveManager.Instance.OnWaveCleared -= OnWaveCleared;
        }

        void OnWaveCleared(int wave)
        {
            if (wave % _upgradeEveryNWaves == 0)
                StartCoroutine(ShowUpgradesDelayed());
        }

        IEnumerator ShowUpgradesDelayed()
        {
            yield return new WaitForSeconds(2f);
            ShowUpgrades();
        }

        List<UpgradeOption> AllUpgrades()
        {
            return new List<UpgradeOption>
            {
                new UpgradeOption
                {
                    name = "+ Vida Máxima",
                    description = "+30 Vida Máx\n+30 Curación",
                    apply = p => { p.maxHealth += 30; p.Heal(30); }
                },
                new UpgradeOption
                {
                    name = "+ Velocidad",
                    description = "Movimiento 20%\nmás rápido",
                    apply = p => p.moveSpeed *= 1.20f
                },
                new UpgradeOption
                {
                    name = "+ Daño Melee",
                    description = "+10 de daño\nen cuerpo a cuerpo",
                    apply = p => p.meleeDamage += 10
                },
                new UpgradeOption
                {
                    name = "Curación Total",
                    description = "Recupera 60\npuntos de vida",
                    apply = p => p.Heal(60)
                },
                new UpgradeOption
                {
                    name = "+ Velocidad Ataque",
                    description = "Cooldown de ataque\n25% más corto",
                    apply = p => p.attackCooldown = Mathf.Max(0.1f, p.attackCooldown * 0.75f)
                },
                new UpgradeOption
                {
                    name = "+ Área de Ataque",
                    description = "Rango melee\n30% mayor",
                    apply = p => p.meleeRange *= 1.30f
                },
                new UpgradeOption
                {
                    name = "Defensa",
                    description = "+50 Vida Máx\nsin curación",
                    apply = p => p.maxHealth += 50
                },
                new UpgradeOption
                {
                    name = "Ataque Veloz",
                    description = "+5 daño\ny +10% velocidad",
                    apply = p => { p.meleeDamage += 5; p.moveSpeed *= 1.10f; }
                },
            };
        }

        void ShowUpgrades()
        {
            if (_player == null) _player = FindFirstObjectByType<PlayerController>();
            if (_player == null || _upgradePanel == null) return;

            _waitingForChoice = true;
            Time.timeScale = 0f;

            // Pick random options
            var all = AllUpgrades();
            var chosen = new List<UpgradeOption>();
            int count = Mathf.Min(_optionCount, all.Count);
            while (chosen.Count < count)
            {
                int idx = Random.Range(0, all.Count);
                chosen.Add(all[idx]);
                all.RemoveAt(idx);
            }

            // Rebuild cards
            foreach (Transform child in _upgradePanel.transform)
                Destroy(child.gameObject);

            // Title
            CreateLabel("¡ELIGE UNA MEJORA!", 36, new Color(1f, 0.9f, 0.3f),
                new Vector2(0f, 0.82f), new Vector2(1f, 1f));

            // Sub-hint
            CreateLabel("La elección es permanente para esta partida", 18, new Color(0.8f, 0.8f, 0.8f),
                new Vector2(0f, 0.74f), new Vector2(1f, 0.82f));

            // Cards
            float totalSpacing = 1f - chosen.Count * 0.28f;
            float gap = totalSpacing / (chosen.Count + 1);
            for (int i = 0; i < chosen.Count; i++)
            {
                float xMin = gap + i * (0.28f + gap);
                CreateCard(chosen[i], xMin, xMin + 0.28f);
            }

            _upgradePanel.SetActive(true);
        }

        void CreateLabel(string text, int fontSize, Color color, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(_upgradePanel.transform, false);
            var t = go.AddComponent<Text>();
            t.text = text;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = fontSize;
            t.fontStyle = FontStyle.Bold;
            t.color = color;
            t.alignment = TextAnchor.MiddleCenter;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        void CreateCard(UpgradeOption option, float xMin, float xMax)
        {
            var card = new GameObject("Card");
            card.transform.SetParent(_upgradePanel.transform, false);

            var bg = card.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.10f, 0.18f, 0.97f);

            var btn = card.AddComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = new Color(0.2f, 0.3f, 0.5f);
            colors.pressedColor = new Color(0.3f, 0.5f, 0.8f);
            btn.colors = colors;

            var cardRT = card.GetComponent<RectTransform>();
            cardRT.anchorMin = new Vector2(xMin, 0.12f);
            cardRT.anchorMax = new Vector2(xMax, 0.72f);
            cardRT.offsetMin = new Vector2(6, 6);
            cardRT.offsetMax = new Vector2(-6, -6);

            var outline = card.AddComponent<Outline>();
            outline.effectColor = new Color(0.4f, 0.6f, 1f, 0.7f);
            outline.effectDistance = new Vector2(3, -3);

            // Name label
            var nameGO = new GameObject("Name");
            nameGO.transform.SetParent(card.transform, false);
            var nameText = nameGO.AddComponent<Text>();
            nameText.text = option.name;
            nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            nameText.fontSize = 20;
            nameText.fontStyle = FontStyle.Bold;
            nameText.color = new Color(1f, 0.85f, 0.3f);
            nameText.alignment = TextAnchor.MiddleCenter;
            var nameRT = nameGO.GetComponent<RectTransform>();
            nameRT.anchorMin = new Vector2(0f, 0.62f);
            nameRT.anchorMax = new Vector2(1f, 1f);
            nameRT.offsetMin = new Vector2(5, 5);
            nameRT.offsetMax = new Vector2(-5, -5);

            // Desc label
            var descGO = new GameObject("Desc");
            descGO.transform.SetParent(card.transform, false);
            var descText = descGO.AddComponent<Text>();
            descText.text = option.description;
            descText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            descText.fontSize = 17;
            descText.color = Color.white;
            descText.alignment = TextAnchor.MiddleCenter;
            var descRT = descGO.GetComponent<RectTransform>();
            descRT.anchorMin = new Vector2(0f, 0.05f);
            descRT.anchorMax = new Vector2(1f, 0.62f);
            descRT.offsetMin = new Vector2(5, 5);
            descRT.offsetMax = new Vector2(-5, -5);

            var captured = option;
            btn.onClick.AddListener(() => SelectUpgrade(captured));
        }

        void SelectUpgrade(UpgradeOption option)
        {
            if (!_waitingForChoice) return;
            _waitingForChoice = false;

            option.apply(_player);
            _upgradePanel.SetActive(false);
            Time.timeScale = 1f;

            RuntimeGameManager.Instance?.ShowBigMessage(
                $"¡{option.name}!", new Color(0.5f, 1f, 0.5f));

            Debug.Log($"[UpgradeSystem] Mejora aplicada: {option.name}");
        }

        void CreateUpgradePanel()
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) return;

            _upgradePanel = new GameObject("UpgradePanel");
            _upgradePanel.transform.SetParent(canvas.transform, false);

            var bg = _upgradePanel.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.90f);

            var rt = _upgradePanel.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            _upgradePanel.SetActive(false);
        }
    }
}
