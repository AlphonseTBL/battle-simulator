using UnityEngine;

namespace BattleSim.Gameplay
{
    public class AbilityDamageZone : MonoBehaviour
    {
        private MarbleAgent _owner;
        private float _damage;
        private bool _consumeOnHit;

        public void Initialize(MarbleAgent owner, float damage, bool consumeOnHit)
        {
            _owner = owner;
            _damage = Mathf.Max(0f, damage);
            _consumeOnHit = consumeOnHit;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_owner == null || !_owner.IsAlive || _damage <= 0f)
            {
                return;
            }

            MarbleAgent target = other.GetComponent<MarbleAgent>();
            if (target == null || target == _owner || !target.IsAlive)
            {
                return;
            }

            float scaledDamage = _damage * _owner.GetDamageMultiplierFromMissingHealth();
            target.Health.ApplyDamage(scaledDamage, _owner);

            if (_consumeOnHit)
            {
                Destroy(gameObject);
            }
        }
    }
}
