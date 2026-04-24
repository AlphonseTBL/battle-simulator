using System.Collections.Generic;
using BattleSim.Config;
using BattleSim.Core;
using UnityEngine;

namespace BattleSim.Gameplay
{
    public static class MarbleAbilities
    {
        private static Sprite _trailSprite;
        private static readonly Dictionary<MarbleAgent, Queue<GameObject>> TrailCirclesByCaster = new Dictionary<MarbleAgent, Queue<GameObject>>();
        private static readonly HashSet<GameObject> RuntimePlaceables = new HashSet<GameObject>();

        public static bool IsPassive(string abilityType)
        {
            return abilityType == "SideRectangles";
        }

        public static void ClearRuntimePlaceables()
        {
            foreach (GameObject placeable in RuntimePlaceables)
            {
                if (placeable != null)
                {
                    Object.Destroy(placeable);
                }
            }

            RuntimePlaceables.Clear();
            TrailCirclesByCaster.Clear();
        }

        public static void Execute(MarbleAgent caster, MarbleAgent target, AbilityConfig ability)
        {
            if (caster == null || !caster.IsAlive || ability == null)
            {
                return;
            }

            switch (ability.type)
            {
                case "SideRectangles":
                    EnsureSideRectangles(caster, ability);
                    break;

                case "TrailCircles":
                    SpawnTrailCircle(caster, ability);
                    break;

                case "LongRangeRectangleShot":
                    SpawnLongRangeRectangleShot(caster, target, ability);
                    break;

                default:
                    Debug.LogWarning($"Unknown ability type '{ability.type}' on marble '{caster.DisplayName}'.");
                    break;
            }
        }

        private static void EnsureSideRectangles(MarbleAgent caster, AbilityConfig ability)
        {
            Transform existing = caster.transform.Find("SideRectangles");
            if (existing != null)
            {
                return;
            }

            float rawWidth = Mathf.Max(0.05f, ability.sizeX);
            float rawHeight = Mathf.Max(0.2f, ability.sizeY);
            float width = Mathf.Min(rawWidth, rawHeight);
            float height = Mathf.Max(rawWidth, rawHeight);
            float offset = ability.sideOffset > 0f ? ability.sideOffset : caster.Config.radius + width * 0.5f;
            float damage = Mathf.Max(0f, ability.power);
            Color teamColor = caster.TeamColor;

            GameObject root = new GameObject("SideRectangles");
            root.transform.SetParent(caster.transform, false);

            CreateSideRectangle(root.transform, caster, "SideRect_Left", -offset, width, height, damage, teamColor);
            CreateSideRectangle(root.transform, caster, "SideRect_Right", offset, width, height, damage, teamColor);
        }

        private static void CreateSideRectangle(
            Transform parent,
            MarbleAgent caster,
            string name,
            float localX,
            float width,
            float height,
            float damage,
            Color teamColor)
        {
            GameObject rectangle = new GameObject(name);
            rectangle.transform.SetParent(parent, false);
            rectangle.transform.localPosition = new Vector3(localX, 0f, 0f);
            rectangle.transform.localScale = new Vector3(width, height, 1f);

            SpriteRenderer renderer = rectangle.AddComponent<SpriteRenderer>();
            renderer.sprite = SimpleSpriteFactory.GetWhitePixel();
            renderer.color = new Color(teamColor.r, teamColor.g, teamColor.b, 0.55f);
            renderer.sortingOrder = 4;

            BoxCollider2D collider = rectangle.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = Vector2.one;

            AbilityDamageZone zone = rectangle.AddComponent<AbilityDamageZone>();
            zone.Initialize(caster, damage, false);
        }

        private static void SpawnTrailCircle(MarbleAgent caster, AbilityConfig ability)
        {
            if (!TrailCirclesByCaster.TryGetValue(caster, out Queue<GameObject> activeCircles))
            {
                activeCircles = new Queue<GameObject>();
                TrailCirclesByCaster[caster] = activeCircles;
            }

            TrimDestroyed(activeCircles);

            int maxActive = ability.maxActiveTrailCircles;
            if (maxActive > 0)
            {
                while (activeCircles.Count >= maxActive)
                {
                    GameObject oldest = activeCircles.Dequeue();
                    if (oldest != null)
                    {
                        Object.Destroy(oldest);
                    }
                }
            }

            GameObject trailCircle = new GameObject($"TrailCircle_{caster.DisplayName}");
            trailCircle.transform.position = caster.transform.position;

            if (_trailSprite == null)
            {
                _trailSprite = SimpleSpriteFactory.CreateCircle(40);
            }

            float radius = Mathf.Max(0.05f, ability.trailRadius);
            Color teamColor = caster.TeamColor;

            SpriteRenderer renderer = trailCircle.AddComponent<SpriteRenderer>();
            renderer.sprite = _trailSprite;
            renderer.color = new Color(teamColor.r, teamColor.g, teamColor.b, 0.65f);
            renderer.sortingOrder = 1;

            trailCircle.transform.localScale = new Vector3(radius * 2f, radius * 2f, 1f);

            CircleCollider2D collider = trailCircle.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.5f;

            AbilityDamageZone zone = trailCircle.AddComponent<AbilityDamageZone>();
            zone.Initialize(caster, Mathf.Max(0f, ability.power), true);

            if (ability.trailLifetime > 0f)
            {
                Object.Destroy(trailCircle, ability.trailLifetime);
            }

            activeCircles.Enqueue(trailCircle);
            RuntimePlaceables.Add(trailCircle);
        }

        private static void SpawnLongRangeRectangleShot(MarbleAgent caster, MarbleAgent target, AbilityConfig ability)
        {
            Vector2 direction = ResolveShotDirection(caster, target);

            GameObject shot = new GameObject($"LongRangeShot_{caster.DisplayName}");
            shot.transform.position = caster.transform.position;

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            shot.transform.rotation = Quaternion.Euler(0f, 0f, angle);

            float length = Mathf.Max(0.1f, ability.projectileLength);
            float width = Mathf.Max(0.05f, ability.projectileWidth);
            shot.transform.localScale = new Vector3(length, width, 1f);

            SpriteRenderer renderer = shot.AddComponent<SpriteRenderer>();
            renderer.sprite = SimpleSpriteFactory.GetWhitePixel();
            renderer.color = new Color(caster.TeamColor.r, caster.TeamColor.g, caster.TeamColor.b, 0.78f);
            renderer.sortingOrder = 3;

            BoxCollider2D collider = shot.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = Vector2.one;

            DistanceScaledProjectile projectile = shot.AddComponent<DistanceScaledProjectile>();
            projectile.Initialize(
                caster,
                direction,
                ability.projectileSpeed,
                ability.power,
                ability.distanceDamageBonusPercentPerUnit,
                ability.projectileMaxDamageMultiplier);

            Object.Destroy(shot, Mathf.Max(0.1f, ability.projectileLifetime));
            RuntimePlaceables.Add(shot);
        }

        private static Vector2 ResolveShotDirection(MarbleAgent caster, MarbleAgent target)
        {
            if (target != null && target.IsAlive)
            {
                Vector2 towardTarget = target.transform.position - caster.transform.position;
                if (towardTarget.sqrMagnitude > 0.0001f)
                {
                    return towardTarget.normalized;
                }
            }

            Rigidbody2D rb = caster.GetComponent<Rigidbody2D>();
            if (rb != null && rb.linearVelocity.sqrMagnitude > 0.0001f)
            {
                return rb.linearVelocity.normalized;
            }

            Vector2 fallback = Random.insideUnitCircle;
            if (fallback.sqrMagnitude <= 0.0001f)
            {
                fallback = Vector2.right;
            }

            return fallback.normalized;
        }

        private static void TrimDestroyed(Queue<GameObject> queue)
        {
            while (queue.Count > 0 && queue.Peek() == null)
            {
                queue.Dequeue();
            }
        }
    }
}
