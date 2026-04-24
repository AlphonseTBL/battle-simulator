using System;
using System.Collections.Generic;
using BattleSim.Config;
using UnityEngine;

namespace BattleSim.Gameplay
{
    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D), typeof(HealthComponent))]
    [RequireComponent(typeof(HealthBarView), typeof(SpriteRenderer))]
    public class MarbleAgent : MonoBehaviour
    {
        private const float MinBounceJitterDegrees = 5f;
        private const float MaxBounceJitterDegrees = 15f;

        [Serializable]
        private class AbilityRuntime
        {
            public AbilityConfig config;
            public float nextActivationTime;
        }

        public string DisplayName => Config.displayName;
        public MarbleConfig Config { get; private set; }
        public HealthComponent Health { get; private set; }
        public bool IsAlive => Health != null && Health.IsAlive;
        public Color TeamColor => GetComponent<SpriteRenderer>().color;

        private BattleManager _battleManager;
        private Rigidbody2D _rigidbody2D;
        private CircleCollider2D _collider;
        private AbilityRuntime[] _abilities;

        private float _hasteMultiplier = 1f;
        private float _hasteEndTime;
        private float _visualSpinRateDeg;

        public void Initialize(
            MarbleConfig config,
            BattleManager battleManager,
            Sprite sprite,
            PhysicsMaterial2D physicsMaterial,
            Vector2 startPosition)
        {
            Config = config;
            _battleManager = battleManager;

            transform.position = startPosition;
            transform.name = $"Marble_{config.id}";

            SpriteRenderer renderer = GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = ParseColor(config.colorHex);
            renderer.sortingOrder = 5;

            _collider = GetComponent<CircleCollider2D>();
            _collider.radius = config.radius;
            _collider.sharedMaterial = physicsMaterial;

            _rigidbody2D = GetComponent<Rigidbody2D>();
            _rigidbody2D.gravityScale = 0f;
            _rigidbody2D.linearDamping = 0f;
            _rigidbody2D.angularDamping = 0f;
            _rigidbody2D.freezeRotation = true;
            _rigidbody2D.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            _rigidbody2D.sleepMode = RigidbodySleepMode2D.NeverSleep;
            _rigidbody2D.sharedMaterial = physicsMaterial;

            Health = GetComponent<HealthComponent>();
            Health.Initialize(config.maxHealth, this);
            Health.OnDeath += HandleDeath;

            HealthBarView barView = GetComponent<HealthBarView>();
            barView.Initialize(Health, 1.1f, 0.14f, config.radius + 0.4f);

            BuildAbilityRuntime(config);
            InitializeSpinRate();
            LaunchWithRandomDirection();
        }

        private void Update()
        {
            if (!IsAlive)
            {
                return;
            }

            if (_abilities == null)
            {
                return;
            }

            float now = Time.time;
            for (int i = 0; i < _abilities.Length; i++)
            {
                AbilityRuntime ability = _abilities[i];
                if (now < ability.nextActivationTime)
                {
                    continue;
                }

                MarbleAgent target = _battleManager.GetClosestEnemy(this, ability.config.range);
                MarbleAbilities.Execute(this, target, ability.config);
                ability.nextActivationTime = now + GetNextCooldown(ability.config);
            }

            // Visual rotation makes the marble and child weapons rotate together.
            transform.Rotate(0f, 0f, _visualSpinRateDeg * Time.deltaTime, Space.Self);
        }

        private void FixedUpdate()
        {
            if (!IsAlive)
            {
                _rigidbody2D.linearVelocity = Vector2.zero;
                return;
            }

            float activeHaste = Time.time < _hasteEndTime ? _hasteMultiplier : 1f;
            float battleSpeed = _battleManager == null ? 1f : _battleManager.GetGlobalSpeedMultiplier();
            float targetSpeed = Config.speed * activeHaste * battleSpeed;

            Vector2 velocity = _rigidbody2D.linearVelocity;
            if (velocity.sqrMagnitude < 0.0001f)
            {
                velocity = UnityEngine.Random.insideUnitCircle.normalized;
            }

            velocity = velocity.normalized * targetSpeed;
            _rigidbody2D.linearVelocity = velocity;
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (!IsAlive)
            {
                return;
            }

            ApplyBounceDirectionJitter(collision);

            MarbleAgent other = collision.collider.GetComponent<MarbleAgent>();
            if (other == null || !other.IsAlive)
            {
                return;
            }

            float hasteDamageMultiplier = Time.time < _hasteEndTime ? _hasteMultiplier : 1f;
            float damage = Config.collisionDamage * hasteDamageMultiplier * GetDamageMultiplierFromMissingHealth();
            other.Health.ApplyDamage(damage, this);
        }

        private void ApplyBounceDirectionJitter(Collision2D collision)
        {
            Vector2 velocity = _rigidbody2D.linearVelocity;
            if (velocity.sqrMagnitude < 0.0001f)
            {
                if (collision.contactCount > 0)
                {
                    Vector2 normal = collision.GetContact(0).normal;
                    velocity = Vector2.Perpendicular(normal).normalized * Mathf.Max(0.1f, Config.speed);
                }
                else
                {
                    velocity = UnityEngine.Random.insideUnitCircle.normalized * Mathf.Max(0.1f, Config.speed);
                }
            }

            float jitterAngle = UnityEngine.Random.Range(MinBounceJitterDegrees, MaxBounceJitterDegrees);
            if (UnityEngine.Random.value < 0.5f)
            {
                jitterAngle = -jitterAngle;
            }

            Vector2 jitteredDirection = (Quaternion.Euler(0f, 0f, jitterAngle) * velocity.normalized);
            _rigidbody2D.linearVelocity = jitteredDirection * velocity.magnitude;
        }

        public void ApplyHaste(float multiplier, float duration)
        {
            _hasteMultiplier = Mathf.Max(1f, multiplier);
            _hasteEndTime = Mathf.Max(_hasteEndTime, Time.time + Mathf.Max(0.1f, duration));
        }

        public float GetDamageMultiplierFromMissingHealth()
        {
            if (Health == null || Health.MaxHealth <= 0f)
            {
                return 1f;
            }

            float missingHealth = Mathf.Max(0f, Health.MaxHealth - Health.CurrentHealth);
            float bonusPercent = missingHealth * Mathf.Max(0f, Config.missingHealthToBonusPercentPerPoint);
            return 1f + bonusPercent * 0.01f;
        }

        private void HandleDeath(HealthComponent _, MarbleAgent attacker)
        {
            _rigidbody2D.linearVelocity = Vector2.zero;
            _rigidbody2D.simulated = false;
            GetComponent<Collider2D>().enabled = false;

            SpriteRenderer renderer = GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.enabled = false;
            }

            _battleManager.NotifyMarbleDeath(this, attacker);
            Destroy(gameObject);
        }

        private void BuildAbilityRuntime(MarbleConfig config)
        {
            List<AbilityRuntime> activeAbilities = new List<AbilityRuntime>();

            for (int i = 0; i < config.abilities.Length; i++)
            {
                AbilityConfig ability = config.abilities[i];

                if (MarbleAbilities.IsPassive(ability.type))
                {
                    MarbleAbilities.Execute(this, null, ability);
                    continue;
                }

                activeAbilities.Add(new AbilityRuntime
                {
                    config = ability,
                    nextActivationTime = Time.time + GetNextCooldown(ability)
                });
            }

            _abilities = activeAbilities.ToArray();
        }

        private static float GetNextCooldown(AbilityConfig ability)
        {
            float jitter = UnityEngine.Random.Range(-ability.randomJitter, ability.randomJitter);
            return Mathf.Max(0.15f, ability.baseCooldown + jitter);
        }

        private void LaunchWithRandomDirection()
        {
            Vector2 direction = UnityEngine.Random.insideUnitCircle.normalized;
            if (direction.sqrMagnitude < 0.0001f)
            {
                direction = Vector2.right;
            }

            _rigidbody2D.linearVelocity = direction * Config.speed;
        }

        private void InitializeSpinRate()
        {
            float baseRate = Mathf.Max(0f, Config.turnRateDegreesPerSecond);
            float jitter = UnityEngine.Random.Range(-Config.turnRateJitter, Config.turnRateJitter);
            float magnitude = Mathf.Max(0f, baseRate + jitter);
            float directionSign = UnityEngine.Random.value < 0.5f ? -1f : 1f;
            _visualSpinRateDeg = magnitude * directionSign;
        }

        private static Color ParseColor(string hex)
        {
            if (!ColorUtility.TryParseHtmlString(hex, out Color color))
            {
                color = Color.white;
            }

            return color;
        }
    }
}
