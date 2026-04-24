using System;

namespace BattleSim.Config
{
    [Serializable]
    public class BattleConfig
    {
        public ArenaConfig arena = new ArenaConfig();
        public BattleRulesConfig rules = new BattleRulesConfig();
        public MarbleConfig[] marbles = Array.Empty<MarbleConfig>();
    }

    [Serializable]
    public class BattleRulesConfig
    {
        public float maxBattleSeconds = 90f;
        public float restartDelaySeconds = 3f;
        public bool autoRestart = false;
        public bool showDebugHud = true;
        public float speedBoostCriticalSeconds = 15f;
        public float maxExtraSpeedPercentAtCritical = 200f;
    }

    [Serializable]
    public class ArenaConfig
    {
        public float width = 10f;
        public float height = 10f;
        public bool forceSquare = true;
        public float sizeScale = 0.75f;
        public float wallThickness = 0.5f;
        public float wallBounciness = 1.2f;
        public float marbleBounciness = 1f;
    }

    [Serializable]
    public class MarbleConfig
    {
        public string id = "marble";
        public string displayName = "Marble";
        public string colorHex = "#FFFFFF";
        public float radius = 0.4f;
        public float maxHealth = 100f;
        public float speed = 4f;
        public float turnRateDegreesPerSecond = 35f;
        public float turnRateJitter = 12f;
        public float turnRateJitterInterval = 1.25f;
        public float collisionDamage = 4f;
        public float missingHealthToBonusPercentPerPoint = 1f;
        public float hasteMultiplier = 1.8f;
        public float hasteDuration = 1.2f;
        public AbilityConfig[] abilities = Array.Empty<AbilityConfig>();
    }

    [Serializable]
    public class AbilityConfig
    {
        public string type = "SideRectangles";
        public string name = "Ability";
        public float power = 10f;
        public float baseCooldown = 2f;
        public float randomJitter = 0.5f;
        public float range = 999f;

        public float sizeX = 0.18f;
        public float sizeY = 1.3f;
        public float sideOffset = 0f;
        public float trailRadius = 0.14f;
        public float trailLifetime = 8f;
        public int maxActiveTrailCircles = 0;

        public float projectileSpeed = 9f;
        public float projectileLifetime = 2.2f;
        public float projectileLength = 1.8f;
        public float projectileWidth = 0.22f;
        public float distanceDamageBonusPercentPerUnit = 12f;
        public float projectileMaxDamageMultiplier = 3f;
    }
}
