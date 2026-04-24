using UnityEngine;

namespace BattleSim.Gameplay
{
    public class DistanceScaledProjectile : MonoBehaviour
    {
        private MarbleAgent _owner;
        private Vector2 _direction;
        private float _speed;
        private float _baseDamage;
        private float _distanceBonusPercentPerUnit;
        private float _maxDamageMultiplier;
        private float _travelDistance;

        public void Initialize(
            MarbleAgent owner,
            Vector2 direction,
            float speed,
            float baseDamage,
            float distanceBonusPercentPerUnit,
            float maxDamageMultiplier)
        {
            _owner = owner;
            _direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
            _speed = Mathf.Max(0.1f, speed);
            _baseDamage = Mathf.Max(0f, baseDamage);
            _distanceBonusPercentPerUnit = Mathf.Max(0f, distanceBonusPercentPerUnit);
            _maxDamageMultiplier = Mathf.Max(1f, maxDamageMultiplier);
        }

        private void Update()
        {
            if (_owner == null || !_owner.IsAlive)
            {
                Destroy(gameObject);
                return;
            }

            float step = _speed * Time.deltaTime;
            transform.position += (Vector3)(_direction * step);
            _travelDistance += step;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_owner == null || !_owner.IsAlive || _baseDamage <= 0f)
            {
                return;
            }

            MarbleAgent target = other.GetComponent<MarbleAgent>();
            if (target == null || target == _owner || !target.IsAlive)
            {
                return;
            }

            float distanceMultiplier = 1f + _travelDistance * _distanceBonusPercentPerUnit * 0.01f;
            distanceMultiplier = Mathf.Min(distanceMultiplier, _maxDamageMultiplier);

            float ownerDamageMultiplier = _owner.GetDamageMultiplierFromMissingHealth();
            float finalDamage = _baseDamage * distanceMultiplier * ownerDamageMultiplier;
            target.Health.ApplyDamage(finalDamage, _owner);

            Destroy(gameObject);
        }
    }
}
