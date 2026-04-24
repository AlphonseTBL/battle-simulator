using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace BattleSim.Config
{
    public static class BattleConfigLoader
    {
        public const string DefaultFileName = "battle_config.json";

        public static bool TryLoad(out BattleConfig config, out string error)
        {
            string path = Path.Combine(Application.streamingAssetsPath, DefaultFileName);
            config = null;
            error = string.Empty;

            if (!File.Exists(path))
            {
                error = $"Config file not found: {path}";
                return false;
            }

            try
            {
                string json = File.ReadAllText(path);
                config = JsonUtility.FromJson<BattleConfig>(json);
                if (config == null)
                {
                    error = "Config JSON produced null BattleConfig.";
                    return false;
                }

                if (!Validate(config, out error))
                {
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                error = $"Failed to read config: {ex.Message}";
                return false;
            }
        }

        private static bool Validate(BattleConfig config, out string error)
        {
            error = string.Empty;

            if (config.rules == null)
            {
                config.rules = new BattleRulesConfig();
            }

            if (config.arena == null)
            {
                error = "Arena config is missing.";
                return false;
            }

            if (config.arena.width <= 1f || config.arena.height <= 1f)
            {
                error = "Arena width and height must be greater than 1.";
                return false;
            }

            if (config.arena.sizeScale <= 0f)
            {
                error = "Arena sizeScale must be greater than 0.";
                return false;
            }

            if (config.arena.wallBounciness < 0f || config.arena.marbleBounciness < 0f)
            {
                error = "Arena bounciness values cannot be negative.";
                return false;
            }

            if (config.marbles == null || config.marbles.Length < 2)
            {
                error = "At least two marbles are required.";
                return false;
            }

            if (config.rules.maxBattleSeconds < 5f)
            {
                error = "rules.maxBattleSeconds must be at least 5.";
                return false;
            }

            if (config.rules.restartDelaySeconds < 0f)
            {
                error = "rules.restartDelaySeconds cannot be negative.";
                return false;
            }

            if (config.rules.speedBoostCriticalSeconds <= 0f)
            {
                error = "rules.speedBoostCriticalSeconds must be greater than 0.";
                return false;
            }

            if (config.rules.maxExtraSpeedPercentAtCritical < 0f)
            {
                error = "rules.maxExtraSpeedPercentAtCritical cannot be negative.";
                return false;
            }

            HashSet<string> marbleIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < config.marbles.Length; i++)
            {
                MarbleConfig marble = config.marbles[i];
                if (string.IsNullOrWhiteSpace(marble.id))
                {
                    error = $"Marble[{i}] has an empty id.";
                    return false;
                }

                if (!marbleIds.Add(marble.id))
                {
                    error = $"Duplicate marble id detected: '{marble.id}'.";
                    return false;
                }

                if (marble.maxHealth <= 0f)
                {
                    error = $"Marble[{i}] has invalid maxHealth.";
                    return false;
                }

                if (marble.speed <= 0f)
                {
                    error = $"Marble[{i}] has invalid speed.";
                    return false;
                }

                if (marble.turnRateDegreesPerSecond < 0f)
                {
                    error = $"Marble[{i}] turnRateDegreesPerSecond cannot be negative.";
                    return false;
                }

                if (marble.turnRateJitter < 0f)
                {
                    error = $"Marble[{i}] turnRateJitter cannot be negative.";
                    return false;
                }

                if (marble.turnRateJitterInterval < 0.1f)
                {
                    error = $"Marble[{i}] turnRateJitterInterval must be at least 0.1.";
                    return false;
                }

                if (marble.radius <= 0.05f)
                {
                    error = $"Marble[{i}] has invalid radius.";
                    return false;
                }

                if (marble.missingHealthToBonusPercentPerPoint < 0f)
                {
                    error = $"Marble[{i}] missingHealthToBonusPercentPerPoint cannot be negative.";
                    return false;
                }

                if (marble.abilities == null || marble.abilities.Length == 0)
                {
                    error = $"Marble[{i}] has no abilities.";
                    return false;
                }

                for (int j = 0; j < marble.abilities.Length; j++)
                {
                    AbilityConfig ability = marble.abilities[j];
                    if (ability.randomJitter < 0f)
                    {
                        error = $"Marble[{i}] Ability[{j}] randomJitter cannot be negative.";
                        return false;
                    }

                    if (string.IsNullOrWhiteSpace(ability.type))
                    {
                        error = $"Marble[{i}] Ability[{j}] has an empty type.";
                        return false;
                    }

                    switch (ability.type)
                    {
                        case "SideRectangles":
                            if (ability.power <= 0f)
                            {
                                error = $"Marble[{i}] Ability[{j}] SideRectangles requires power > 0.";
                                return false;
                            }

                            if (ability.sizeX <= 0f || ability.sizeY <= 0f)
                            {
                                error = $"Marble[{i}] Ability[{j}] SideRectangles requires sizeX/sizeY > 0.";
                                return false;
                            }
                            break;

                        case "TrailCircles":
                            if (ability.baseCooldown <= 0f)
                            {
                                error = $"Marble[{i}] Ability[{j}] TrailCircles requires baseCooldown > 0.";
                                return false;
                            }

                            if (ability.power <= 0f)
                            {
                                error = $"Marble[{i}] Ability[{j}] TrailCircles requires power > 0.";
                                return false;
                            }

                            if (ability.trailRadius <= 0f)
                            {
                                error = $"Marble[{i}] Ability[{j}] TrailCircles requires trailRadius > 0.";
                                return false;
                            }

                            if (ability.trailLifetime < 0f)
                            {
                                error = $"Marble[{i}] Ability[{j}] TrailCircles trailLifetime cannot be negative.";
                                return false;
                            }

                            if (ability.maxActiveTrailCircles < 0)
                            {
                                error = $"Marble[{i}] Ability[{j}] TrailCircles maxActiveTrailCircles cannot be negative.";
                                return false;
                            }
                            break;

                        case "LongRangeRectangleShot":
                            if (ability.baseCooldown <= 0f)
                            {
                                error = $"Marble[{i}] Ability[{j}] LongRangeRectangleShot requires baseCooldown > 0.";
                                return false;
                            }

                            if (ability.power <= 0f)
                            {
                                error = $"Marble[{i}] Ability[{j}] LongRangeRectangleShot requires power > 0.";
                                return false;
                            }

                            if (ability.projectileSpeed <= 0f)
                            {
                                error = $"Marble[{i}] Ability[{j}] LongRangeRectangleShot requires projectileSpeed > 0.";
                                return false;
                            }

                            if (ability.projectileLifetime <= 0f)
                            {
                                error = $"Marble[{i}] Ability[{j}] LongRangeRectangleShot requires projectileLifetime > 0.";
                                return false;
                            }

                            if (ability.projectileLength <= 0f || ability.projectileWidth <= 0f)
                            {
                                error = $"Marble[{i}] Ability[{j}] LongRangeRectangleShot requires projectileLength/projectileWidth > 0.";
                                return false;
                            }

                            if (ability.distanceDamageBonusPercentPerUnit < 0f)
                            {
                                error = $"Marble[{i}] Ability[{j}] LongRangeRectangleShot distanceDamageBonusPercentPerUnit cannot be negative.";
                                return false;
                            }

                            if (ability.projectileMaxDamageMultiplier < 1f)
                            {
                                error = $"Marble[{i}] Ability[{j}] LongRangeRectangleShot projectileMaxDamageMultiplier must be >= 1.";
                                return false;
                            }
                            break;

                        default:
                            error = $"Marble[{i}] Ability[{j}] has unsupported type '{ability.type}'.";
                            return false;
                    }
                }
            }

            return true;
        }
    }
}
