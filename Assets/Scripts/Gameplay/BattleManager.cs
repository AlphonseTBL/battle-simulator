using System;
using System.Collections.Generic;
using BattleSim.Config;
using BattleSim.Core;
using UnityEngine;

namespace BattleSim.Gameplay
{
    public class BattleManager : MonoBehaviour
    {
        private enum BattleFlowState
        {
            Menu,
            InBattle,
            Finished
        }

        private readonly List<MarbleAgent> _marbles = new List<MarbleAgent>();

        private BattleConfig _config;
        private Sprite _circleSprite;
        private PhysicsMaterial2D _wallPhysicsMaterial;
        private PhysicsMaterial2D _marblePhysicsMaterial;
        private GameObject _arenaRoot;
        private float _arenaSide;
        private bool _battleFinished;
        private float _battleStartTime;
        private string _battleResultText = string.Empty;
        private BattleFlowState _flowState = BattleFlowState.Menu;
        private int _leftSelectionIndex;
        private int _rightSelectionIndex;

        private GUIStyle _hudStyle;
        private GUIStyle _resultStyle;
        private GUIStyle _panelNameLeftStyle;
        private GUIStyle _panelNameRightStyle;
        private GUIStyle _panelTextLeftStyle;
        private GUIStyle _panelTextRightStyle;
        private GUIStyle _panelBarTextStyle;
        private GUIStyle _menuTitleStyle;
        private GUIStyle _menuCardTitleStyle;
        private GUIStyle _menuCardTextStyle;
        private GUIStyle _menuHintStyle;

        private void Start()
        {
            if (!BattleConfigLoader.TryLoad(out _config, out string error))
            {
                Debug.LogError($"BattleManager could not start. {error}");
                enabled = false;
                return;
            }

            Time.timeScale = 1f;

            _circleSprite = SimpleSpriteFactory.CreateCircle(96);
            _wallPhysicsMaterial = new PhysicsMaterial2D("WallRuntimeMaterial")
            {
                friction = 0f,
                bounciness = Mathf.Clamp01(_config.arena.wallBounciness)
            };

            _marblePhysicsMaterial = new PhysicsMaterial2D("MarbleRuntimeMaterial")
            {
                friction = 0f,
                bounciness = Mathf.Clamp01(_config.arena.marbleBounciness)
            };

            _leftSelectionIndex = 0;
            _rightSelectionIndex = _config.marbles.Length > 1 ? 1 : 0;
            _flowState = BattleFlowState.Menu;

            Debug.Log("Battle ready. Waiting for team selection in main menu.");
        }

        private void OnDisable()
        {
            Time.timeScale = 1f;
        }

        private void Update()
        {
            if (_flowState != BattleFlowState.InBattle)
            {
                return;
            }

            float elapsed = Time.time - _battleStartTime;
            if (_config.rules.maxBattleSeconds > 0f && elapsed >= _config.rules.maxBattleSeconds)
            {
                EndBattleWithTimeout();
                return;
            }

            int aliveCount = 0;
            MarbleAgent winner = null;

            for (int i = 0; i < _marbles.Count; i++)
            {
                MarbleAgent marble = _marbles[i];
                if (marble != null && marble.IsAlive)
                {
                    aliveCount++;
                    winner = marble;
                }
            }

            if (aliveCount <= 1)
            {
                if (winner == null)
                {
                    FinishBattle("Draw", "Battle ended in draw.");
                }
                else
                {
                    FinishBattle($"Winner: {winner.DisplayName}", $"Winner: {winner.DisplayName}");
                }
            }
        }

        public float GetGlobalSpeedMultiplier()
        {
            if (_config == null || _flowState != BattleFlowState.InBattle)
            {
                return 1f;
            }

            float maxTime = _config.rules.maxBattleSeconds;
            if (maxTime <= 0f)
            {
                return 1f;
            }

            float criticalSeconds = Mathf.Max(0.1f, _config.rules.speedBoostCriticalSeconds);
            float maxExtraPercent = Mathf.Max(0f, _config.rules.maxExtraSpeedPercentAtCritical);

            float elapsed = Time.time - _battleStartTime;
            float timeToCritical = Mathf.Max(0.01f, maxTime - criticalSeconds);
            float rawProgress = Mathf.Clamp01(elapsed / timeToCritical);
            float smoothProgress = rawProgress * rawProgress * (3f - 2f * rawProgress);
            float extraPercent = smoothProgress * maxExtraPercent;
            return 1f + extraPercent * 0.01f;
        }

        private void OnGUI()
        {
            if (_config == null)
            {
                return;
            }

            EnsureStyles();

            if (_flowState == BattleFlowState.Menu)
            {
                DrawMainMenu();
                return;
            }

            if (_config.rules.showDebugHud)
            {
                int alive = GetAliveCount();
                float elapsed = Time.time - _battleStartTime;
                float remaining = Mathf.Max(0f, _config.rules.maxBattleSeconds - elapsed);

                string hudText = $"Alive: {alive}/{_marbles.Count}    Time Left: {remaining:0.0}s";
                GUI.Label(new Rect(16f, 16f, 560f, 32f), hudText, _hudStyle);
                DrawBottomPanels();
            }

            if (_flowState == BattleFlowState.Finished)
            {
                DrawFinishOverlay();
            }
        }

        public MarbleAgent GetClosestEnemy(MarbleAgent requester, float range)
        {
            if (requester == null)
            {
                return null;
            }

            float maxDistance = range <= 0f ? float.MaxValue : range;
            float bestDistanceSqr = maxDistance * maxDistance;
            MarbleAgent best = null;

            Vector3 requesterPos = requester.transform.position;

            for (int i = 0; i < _marbles.Count; i++)
            {
                MarbleAgent candidate = _marbles[i];
                if (candidate == null || candidate == requester || !candidate.IsAlive)
                {
                    continue;
                }

                float distanceSqr = (candidate.transform.position - requesterPos).sqrMagnitude;
                if (distanceSqr < bestDistanceSqr)
                {
                    bestDistanceSqr = distanceSqr;
                    best = candidate;
                }
            }

            return best;
        }

        public void NotifyMarbleDeath(MarbleAgent dead, MarbleAgent attacker)
        {
            if (dead == null)
            {
                return;
            }

            string attackerName = attacker == null ? "unknown" : attacker.DisplayName;
            Debug.Log($"{dead.DisplayName} has been defeated by {attackerName}.");
        }

        private void EndBattleWithTimeout()
        {
            MarbleAgent winner = ResolveWinnerByRemainingHealth();
            if (winner == null)
            {
                FinishBattle("Draw (Time Limit)", "Battle ended by timeout: draw.");
            }
            else
            {
                FinishBattle($"Winner by Time: {winner.DisplayName}", $"Battle ended by timeout. Winner by health: {winner.DisplayName}");
            }
        }

        private void FinishBattle(string resultText, string logText)
        {
            if (_battleFinished)
            {
                return;
            }

            _battleFinished = true;
            _flowState = BattleFlowState.Finished;
            _battleResultText = resultText;
            MarbleAbilities.ClearRuntimePlaceables();
            Time.timeScale = 0f;
            Debug.Log(logText);
        }

        private MarbleAgent ResolveWinnerByRemainingHealth()
        {
            MarbleAgent best = null;
            float bestHealth = float.MinValue;

            for (int i = 0; i < _marbles.Count; i++)
            {
                MarbleAgent marble = _marbles[i];
                if (marble == null || !marble.IsAlive)
                {
                    continue;
                }

                float currentHealth = marble.Health.CurrentHealth;
                if (currentHealth > bestHealth)
                {
                    bestHealth = currentHealth;
                    best = marble;
                }
            }

            return best;
        }

        private int GetAliveCount()
        {
            int alive = 0;
            for (int i = 0; i < _marbles.Count; i++)
            {
                MarbleAgent marble = _marbles[i];
                if (marble != null && marble.IsAlive)
                {
                    alive++;
                }
            }

            return alive;
        }

        private void EnsureStyles()
        {
            if (_hudStyle == null)
            {
                _hudStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 16,
                    normal = { textColor = Color.white }
                };
            }

            if (_resultStyle == null)
            {
                _resultStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 22,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = Color.white }
                };
            }

            if (_panelNameLeftStyle == null)
            {
                _panelNameLeftStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 20,
                    alignment = TextAnchor.UpperLeft,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = Color.white }
                };
            }

            if (_panelNameRightStyle == null)
            {
                _panelNameRightStyle = new GUIStyle(_panelNameLeftStyle)
                {
                    alignment = TextAnchor.UpperRight
                };
            }

            if (_panelTextLeftStyle == null)
            {
                _panelTextLeftStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 14,
                    alignment = TextAnchor.UpperLeft,
                    normal = { textColor = Color.white }
                };
            }

            if (_panelTextRightStyle == null)
            {
                _panelTextRightStyle = new GUIStyle(_panelTextLeftStyle)
                {
                    alignment = TextAnchor.UpperRight
                };
            }

            if (_panelBarTextStyle == null)
            {
                _panelBarTextStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 12,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = Color.white }
                };
            }

            if (_menuTitleStyle == null)
            {
                _menuTitleStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 30,
                    alignment = TextAnchor.UpperCenter,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = Color.white }
                };
            }

            if (_menuCardTitleStyle == null)
            {
                _menuCardTitleStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 18,
                    alignment = TextAnchor.UpperCenter,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = Color.white }
                };
            }

            if (_menuCardTextStyle == null)
            {
                _menuCardTextStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 15,
                    alignment = TextAnchor.UpperCenter,
                    normal = { textColor = Color.white }
                };
            }

            if (_menuHintStyle == null)
            {
                _menuHintStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 14,
                    alignment = TextAnchor.UpperCenter,
                    normal = { textColor = new Color(0.9f, 0.9f, 0.9f, 1f) }
                };
            }
        }

        private void DrawMainMenu()
        {
            float boxWidth = 700f;
            float boxHeight = 390f;
            Rect box = new Rect((Screen.width - boxWidth) * 0.5f, (Screen.height - boxHeight) * 0.5f, boxWidth, boxHeight);

            GUI.Box(box, GUIContent.none);
            GUI.Label(new Rect(box.x, box.y + 18f, box.width, 44f), "Marble Battle Simulator", _menuTitleStyle);
            GUI.Label(new Rect(box.x + 20f, box.y + 64f, box.width - 40f, 24f), "Selecciona una canica por lado. Las habilidades son fijas por canica.", _menuHintStyle);

            float cardWidth = (box.width - 60f) * 0.5f;
            Rect leftCard = new Rect(box.x + 20f, box.y + 96f, cardWidth, 200f);
            Rect rightCard = new Rect(box.x + 40f + cardWidth, box.y + 96f, cardWidth, 200f);

            DrawSelectorCard(leftCard, "Lado Izquierdo", ref _leftSelectionIndex);
            DrawSelectorCard(rightCard, "Lado Derecho", ref _rightSelectionIndex);

            GUI.Label(new Rect(box.x + 20f, box.y + 304f, box.width - 40f, 24f),
                "Azure conserva Twin Side Blades y Ruby conserva Hazard Trail.", _menuHintStyle);

            Rect startButton = new Rect(box.x + (box.width - 220f) * 0.5f, box.y + 332f, 220f, 40f);
            if (GUI.Button(startButton, "Iniciar Combate"))
            {
                StartBattleFromSelection();
            }
        }

        private void DrawSelectorCard(Rect rect, string sideLabel, ref int selectedIndex)
        {
            DrawRect(rect, new Color(0f, 0f, 0f, 0.45f));
            GUI.Label(new Rect(rect.x, rect.y + 10f, rect.width, 28f), sideLabel, _menuCardTitleStyle);

            if (_config.marbles == null || _config.marbles.Length == 0)
            {
                GUI.Label(new Rect(rect.x, rect.y + 70f, rect.width, 26f), "Sin canicas", _menuCardTextStyle);
                return;
            }

            if (selectedIndex < 0 || selectedIndex >= _config.marbles.Length)
            {
                selectedIndex = 0;
            }

            Rect leftArrow = new Rect(rect.x + 14f, rect.y + 80f, 32f, 32f);
            Rect rightArrow = new Rect(rect.x + rect.width - 46f, rect.y + 80f, 32f, 32f);

            if (GUI.Button(leftArrow, "<"))
            {
                selectedIndex = (selectedIndex - 1 + _config.marbles.Length) % _config.marbles.Length;
            }

            if (GUI.Button(rightArrow, ">"))
            {
                selectedIndex = (selectedIndex + 1) % _config.marbles.Length;
            }

            MarbleConfig selected = _config.marbles[selectedIndex];
            Color prev = GUI.color;
            GUI.color = ParseColor(selected.colorHex);
            GUI.Label(new Rect(rect.x, rect.y + 76f, rect.width, 34f), selected.displayName, _menuCardTitleStyle);
            GUI.color = prev;

            GUI.Label(new Rect(rect.x + 16f, rect.y + 122f, rect.width - 32f, 22f), $"Habilidad: {GetPrimaryAbilityName(selected)}", _menuCardTextStyle);
            GUI.Label(new Rect(rect.x + 16f, rect.y + 146f, rect.width - 32f, 22f), $"Vida: {selected.maxHealth:0}  Velocidad: {selected.speed:0.0}", _menuCardTextStyle);
        }

        private string GetPrimaryAbilityName(MarbleConfig config)
        {
            if (config == null || config.abilities == null || config.abilities.Length == 0 || config.abilities[0] == null)
            {
                return "N/A";
            }

            return string.IsNullOrWhiteSpace(config.abilities[0].name) ? config.abilities[0].type : config.abilities[0].name;
        }

        private void DrawFinishOverlay()
        {
            float boxWidth = 460f;
            float boxHeight = 140f;
            Rect box = new Rect((Screen.width - boxWidth) * 0.5f, (Screen.height - boxHeight) * 0.5f, boxWidth, boxHeight);

            GUI.Box(box, GUIContent.none);
            GUI.Label(new Rect(box.x + 20f, box.y + 18f, box.width - 40f, 32f), _battleResultText, _resultStyle);

            Rect rematchButton = new Rect(box.x + 40f, box.y + 78f, 170f, 36f);
            Rect menuButton = new Rect(box.x + box.width - 210f, box.y + 78f, 170f, 36f);

            if (GUI.Button(rematchButton, "Revancha"))
            {
                StartBattleFromSelection();
            }

            if (GUI.Button(menuButton, "Menu Principal"))
            {
                ReturnToMainMenu();
            }
        }

        private void StartBattleFromSelection()
        {
            if (_config == null || _config.marbles == null || _config.marbles.Length == 0)
            {
                return;
            }

            Time.timeScale = 1f;
            CleanupBattleObjects();

            MarbleConfig leftTemplate = _config.marbles[Mathf.Clamp(_leftSelectionIndex, 0, _config.marbles.Length - 1)];
            MarbleConfig rightTemplate = _config.marbles[Mathf.Clamp(_rightSelectionIndex, 0, _config.marbles.Length - 1)];

            MarbleConfig[] battleMarbles =
            {
                CloneForSide(leftTemplate, "left"),
                CloneForSide(rightTemplate, "right")
            };

            _arenaSide = BuildArena(_config.arena);
            ConfigureCamera(_arenaSide);
            SpawnMarbles(battleMarbles);

            _battleStartTime = Time.time;
            _battleFinished = false;
            _battleResultText = string.Empty;
            _flowState = BattleFlowState.InBattle;

            Debug.Log($"Battle started with {_marbles.Count} marbles.");
        }

        private void ReturnToMainMenu()
        {
            Time.timeScale = 1f;
            CleanupBattleObjects();
            _battleFinished = false;
            _battleResultText = string.Empty;
            _flowState = BattleFlowState.Menu;
        }

        private void CleanupBattleObjects()
        {
            MarbleAbilities.ClearRuntimePlaceables();

            for (int i = 0; i < _marbles.Count; i++)
            {
                MarbleAgent marble = _marbles[i];
                if (marble != null)
                {
                    Destroy(marble.gameObject);
                }
            }

            _marbles.Clear();

            if (_arenaRoot != null)
            {
                Destroy(_arenaRoot);
                _arenaRoot = null;
            }
        }

        private static MarbleConfig CloneForSide(MarbleConfig source, string side)
        {
            MarbleConfig clone = new MarbleConfig
            {
                id = $"{source.id}_{side}",
                displayName = source.displayName,
                colorHex = source.colorHex,
                radius = source.radius,
                maxHealth = source.maxHealth,
                speed = source.speed,
                turnRateDegreesPerSecond = source.turnRateDegreesPerSecond,
                turnRateJitter = source.turnRateJitter,
                turnRateJitterInterval = source.turnRateJitterInterval,
                collisionDamage = source.collisionDamage,
                missingHealthToBonusPercentPerPoint = source.missingHealthToBonusPercentPerPoint,
                hasteMultiplier = source.hasteMultiplier,
                hasteDuration = source.hasteDuration,
                abilities = CloneAbilities(source.abilities)
            };

            return clone;
        }

        private static AbilityConfig[] CloneAbilities(AbilityConfig[] source)
        {
            if (source == null)
            {
                return Array.Empty<AbilityConfig>();
            }

            AbilityConfig[] cloned = new AbilityConfig[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                AbilityConfig ability = source[i];
                if (ability == null)
                {
                    continue;
                }

                cloned[i] = new AbilityConfig
                {
                    type = ability.type,
                    name = ability.name,
                    power = ability.power,
                    baseCooldown = ability.baseCooldown,
                    randomJitter = ability.randomJitter,
                    range = ability.range,
                    sizeX = ability.sizeX,
                    sizeY = ability.sizeY,
                    sideOffset = ability.sideOffset,
                    trailRadius = ability.trailRadius,
                    trailLifetime = ability.trailLifetime,
                    maxActiveTrailCircles = ability.maxActiveTrailCircles,
                    projectileSpeed = ability.projectileSpeed,
                    projectileLifetime = ability.projectileLifetime,
                    projectileLength = ability.projectileLength,
                    projectileWidth = ability.projectileWidth,
                    distanceDamageBonusPercentPerUnit = ability.distanceDamageBonusPercentPerUnit,
                    projectileMaxDamageMultiplier = ability.projectileMaxDamageMultiplier
                };
            }

            return cloned;
        }

        private float BuildArena(ArenaConfig arena)
        {
            if (_arenaRoot != null)
            {
                Destroy(_arenaRoot);
            }

            _arenaRoot = new GameObject("Arena_Runtime");

            float side = arena.forceSquare ? Mathf.Max(arena.width, arena.height) : Mathf.Min(arena.width, arena.height);
            side *= Mathf.Max(0.1f, arena.sizeScale);
            float halfWidth = side * 0.5f;
            float halfHeight = side * 0.5f;
            float t = arena.wallThickness;

            CreateWall(_arenaRoot.transform, "Wall_Top", new Vector2(0f, halfHeight + t * 0.5f), new Vector2(side + 2f * t, t));
            CreateWall(_arenaRoot.transform, "Wall_Bottom", new Vector2(0f, -halfHeight - t * 0.5f), new Vector2(side + 2f * t, t));
            CreateWall(_arenaRoot.transform, "Wall_Left", new Vector2(-halfWidth - t * 0.5f, 0f), new Vector2(t, side));
            CreateWall(_arenaRoot.transform, "Wall_Right", new Vector2(halfWidth + t * 0.5f, 0f), new Vector2(t, side));

            return side;
        }

        private void CreateWall(Transform parent, string wallName, Vector2 position, Vector2 size)
        {
            GameObject wall = new GameObject(wallName);
            wall.transform.SetParent(parent, false);
            wall.transform.position = position;

            SpriteRenderer renderer = wall.AddComponent<SpriteRenderer>();
            renderer.sprite = SimpleSpriteFactory.GetWhitePixel();
            renderer.color = new Color(0.1f, 0.1f, 0.1f, 1f);
            renderer.sortingOrder = 2;
            wall.transform.localScale = new Vector3(size.x, size.y, 1f);

            BoxCollider2D collider = wall.AddComponent<BoxCollider2D>();
            collider.size = Vector2.one;
            collider.sharedMaterial = _wallPhysicsMaterial;

            Rigidbody2D body = wall.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Static;
        }

        private void SpawnMarbles(MarbleConfig[] marbleConfigs)
        {
            if (marbleConfigs == null || marbleConfigs.Length == 0)
            {
                return;
            }

            float spawnRadius = _arenaSide * 0.32f;
            float step = Mathf.PI * 2f / marbleConfigs.Length;

            for (int i = 0; i < marbleConfigs.Length; i++)
            {
                MarbleConfig cfg = marbleConfigs[i];
                Vector2 position = new Vector2(Mathf.Cos(step * i), Mathf.Sin(step * i)) * spawnRadius;

                GameObject marbleObject = new GameObject($"Marble_{cfg.id}");
                MarbleAgent agent = marbleObject.AddComponent<MarbleAgent>();
                agent.Initialize(cfg, this, _circleSprite, _marblePhysicsMaterial, position);
                _marbles.Add(agent);
            }
        }

        private void ConfigureCamera(float arenaSide)
        {
            Camera cam = Camera.main;
            if (cam == null || !cam.orthographic)
            {
                return;
            }

            float margin = 1f;
            float targetHalf = arenaSide * 0.5f + margin;
            float aspect = Mathf.Max(0.1f, (float)Screen.width / Screen.height);
            cam.orthographicSize = Mathf.Max(targetHalf, targetHalf / aspect);
            cam.transform.position = new Vector3(0f, 0f, cam.transform.position.z);
        }

        private void DrawBottomPanels()
        {
            float margin = 16f;
            float panelWidth = 360f;
            float panelHeight = 136f;
            float panelY = Screen.height - panelHeight - margin;

            if (_marbles.Count > 0)
            {
                DrawMarblePanel(_marbles[0], new Rect(margin, panelY, panelWidth, panelHeight), true);
            }

            if (_marbles.Count > 1)
            {
                DrawMarblePanel(_marbles[1], new Rect(Screen.width - panelWidth - margin, panelY, panelWidth, panelHeight), false);
            }
        }

        private void DrawMarblePanel(MarbleAgent marble, Rect panelRect, bool leftAlign)
        {
            DrawRect(panelRect, new Color(0f, 0f, 0f, 0.48f));

            if (marble == null)
            {
                return;
            }

            Color teamColor = marble.TeamColor;
            float hpRatio = marble.Health == null || marble.Health.MaxHealth <= 0f
                ? 0f
                : Mathf.Clamp01(marble.Health.CurrentHealth / marble.Health.MaxHealth);

            GUIStyle nameStyle = leftAlign ? _panelNameLeftStyle : _panelNameRightStyle;
            GUIStyle textStyle = leftAlign ? _panelTextLeftStyle : _panelTextRightStyle;
            nameStyle.normal.textColor = teamColor;

            GUI.Label(new Rect(panelRect.x + 12f, panelRect.y + 8f, panelRect.width - 24f, 26f), marble.DisplayName, nameStyle);

            string hpText = $"Vida: {marble.Health.CurrentHealth:0}/{marble.Health.MaxHealth:0} ({hpRatio * 100f:0}%)";
            GUI.Label(new Rect(panelRect.x + 12f, panelRect.y + 36f, panelRect.width - 24f, 22f), hpText, textStyle);

            Rect barBg = new Rect(panelRect.x + 12f, panelRect.y + 60f, panelRect.width - 24f, 18f);
            DrawRect(barBg, new Color(0.14f, 0.14f, 0.14f, 0.95f));

            Rect barFill = new Rect(barBg.x + 1f, barBg.y + 1f, (barBg.width - 2f) * hpRatio, barBg.height - 2f);
            DrawRect(barFill, new Color(teamColor.r, teamColor.g, teamColor.b, 0.95f));

            GUI.Label(barBg, $"{hpRatio * 100f:0}%", _panelBarTextStyle);

            GetWeaponDamageInfo(marble, out float baseDamage, out float currentDamage, out float damageMultiplier);
            GUI.Label(new Rect(panelRect.x + 12f, panelRect.y + 82f, panelRect.width - 24f, 22f), $"Daño del arma: {currentDamage:0.0} (base {baseDamage:0.0})", textStyle);
            GUI.Label(new Rect(panelRect.x + 12f, panelRect.y + 104f, panelRect.width - 24f, 22f),
                $"Escalado por vida faltante: x{damageMultiplier:0.00} ({(damageMultiplier - 1f) * 100f:+0.0;-0.0;0.0}%)",
                textStyle);
        }

        private static void DrawRect(Rect rect, Color color)
        {
            Color prev = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = prev;
        }

        private static void GetWeaponDamageInfo(MarbleAgent marble, out float baseDamage, out float currentDamage, out float damageMultiplier)
        {
            baseDamage = 0f;
            currentDamage = 0f;
            damageMultiplier = 1f;

            if (marble == null || marble.Config == null || marble.Config.abilities == null)
            {
                return;
            }

            for (int i = 0; i < marble.Config.abilities.Length; i++)
            {
                AbilityConfig ability = marble.Config.abilities[i];
                if (ability == null)
                {
                    continue;
                }

                if (ability.type == "SideRectangles" || ability.type == "TrailCircles" || ability.type == "LongRangeRectangleShot")
                {
                    baseDamage = Mathf.Max(baseDamage, ability.power);
                }
            }

            damageMultiplier = marble.GetDamageMultiplierFromMissingHealth();
            currentDamage = baseDamage * damageMultiplier;
        }

        private static Color ParseColor(string hex)
        {
            if (!ColorUtility.TryParseHtmlString(hex, out Color color))
            {
                return Color.white;
            }

            return color;
        }
    }
}
