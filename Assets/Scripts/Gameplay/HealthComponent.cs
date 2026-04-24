using System;
using UnityEngine;

namespace BattleSim.Gameplay
{
    public class HealthComponent : MonoBehaviour
    {
        public event Action<float, float> OnHealthChanged;
        public event Action<HealthComponent, MarbleAgent> OnDeath;

        public float CurrentHealth { get; private set; }
        public float MaxHealth { get; private set; }
        public bool IsAlive => CurrentHealth > 0f;

        private MarbleAgent _owner;

        public void Initialize(float maxHealth, MarbleAgent owner)
        {
            MaxHealth = Mathf.Max(1f, maxHealth);
            CurrentHealth = MaxHealth;
            _owner = owner;
            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
        }

        public void ApplyDamage(float amount, MarbleAgent attacker)
        {
            if (!IsAlive || amount <= 0f)
            {
                return;
            }

            CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);

            if (CurrentHealth <= 0f)
            {
                OnDeath?.Invoke(this, attacker);
            }
        }

        public void Heal(float amount)
        {
            if (!IsAlive || amount <= 0f)
            {
                return;
            }

            CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + amount);
            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
        }

        public MarbleAgent GetOwner()
        {
            return _owner;
        }
    }
}
