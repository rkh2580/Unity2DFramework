using System;
using System.Collections.Generic;
using KH.Framework2D.Data;
using UnityEngine;

namespace KH.Framework2D.Combat
{
    /// <summary>
    /// Runtime status effect instance.
    /// </summary>
    public class StatusEffect
    {
        public StatusEffectType Type { get; }
        public float Value { get; }
        public float Duration { get; }
        public float TickInterval { get; }
        public float RemainingTime { get; private set; }
        public float TickTimer { get; private set; }
        public int StackCount { get; private set; }
        public bool IsExpired => RemainingTime <= 0;
        
        public event Action OnTick;
        public event Action OnExpire;
        
        public StatusEffect(StatusEffectData data)
        {
            Type = data.effectType;
            Value = data.value;
            Duration = data.duration;
            TickInterval = data.tickInterval;
            RemainingTime = data.duration;
            TickTimer = 0f;
            StackCount = 1;
        }
        
        public StatusEffect(StatusEffectType type, float value, float duration, float tickInterval = 0f)
        {
            Type = type;
            Value = value;
            Duration = duration;
            TickInterval = tickInterval;
            RemainingTime = duration;
            TickTimer = 0f;
            StackCount = 1;
        }
        
        public void Update(float deltaTime)
        {
            RemainingTime -= deltaTime;
            
            // Handle tick-based effects (DoT, HoT)
            if (TickInterval > 0)
            {
                TickTimer += deltaTime;
                
                if (TickTimer >= TickInterval)
                {
                    TickTimer -= TickInterval;
                    OnTick?.Invoke();
                }
            }
            
            if (IsExpired)
            {
                OnExpire?.Invoke();
            }
        }
        
        public void Refresh()
        {
            RemainingTime = Duration;
        }
        
        public void AddStack(int count = 1)
        {
            StackCount += count;
        }
    }
    
    /// <summary>
    /// Manages status effects on a unit.
    /// </summary>
    public class StatusEffectManager
    {
        private readonly List<StatusEffect> _effects = new();
        private readonly Dictionary<StatusEffectType, StatusEffect> _effectLookup = new();
        
        public IReadOnlyList<StatusEffect> ActiveEffects => _effects;
        
        public event Action<StatusEffect> OnEffectAdded;
        public event Action<StatusEffect> OnEffectRemoved;
        public event Action<StatusEffectType, float> OnEffectTick; // Type, Value
        
        /// <summary>
        /// Add a status effect.
        /// </summary>
        public void AddEffect(StatusEffectData data)
        {
            // Roll apply chance
            if (data.applyChance < 1f && UnityEngine.Random.value > data.applyChance)
                return;
            
            AddEffect(new StatusEffect(data));
        }
        
        /// <summary>
        /// Add a status effect.
        /// </summary>
        public void AddEffect(StatusEffect effect)
        {
            // Check for existing effect of same type
            if (_effectLookup.TryGetValue(effect.Type, out var existing))
            {
                // Refresh duration
                existing.Refresh();
                existing.AddStack();
                return;
            }
            
            _effects.Add(effect);
            _effectLookup[effect.Type] = effect;
            
            effect.OnTick += () => OnEffectTick?.Invoke(effect.Type, effect.Value);
            effect.OnExpire += () => RemoveEffect(effect.Type);
            
            OnEffectAdded?.Invoke(effect);
        }
        
        /// <summary>
        /// Remove a status effect by type.
        /// </summary>
        public void RemoveEffect(StatusEffectType type)
        {
            if (_effectLookup.TryGetValue(type, out var effect))
            {
                _effects.Remove(effect);
                _effectLookup.Remove(type);
                OnEffectRemoved?.Invoke(effect);
            }
        }
        
        /// <summary>
        /// Check if has a specific effect.
        /// </summary>
        public bool HasEffect(StatusEffectType type)
        {
            return _effectLookup.ContainsKey(type);
        }
        
        /// <summary>
        /// Get effect of specific type.
        /// </summary>
        public StatusEffect GetEffect(StatusEffectType type)
        {
            return _effectLookup.TryGetValue(type, out var effect) ? effect : null;
        }
        
        /// <summary>
        /// Clear all effects.
        /// </summary>
        public void ClearAllEffects()
        {
            foreach (var effect in _effects.ToArray())
            {
                RemoveEffect(effect.Type);
            }
        }
        
        /// <summary>
        /// Clear all debuffs.
        /// </summary>
        public void ClearDebuffs()
        {
            foreach (var effect in _effects.ToArray())
            {
                if (IsDebuff(effect.Type))
                {
                    RemoveEffect(effect.Type);
                }
            }
        }
        
        /// <summary>
        /// Clear all buffs.
        /// </summary>
        public void ClearBuffs()
        {
            foreach (var effect in _effects.ToArray())
            {
                if (!IsDebuff(effect.Type))
                {
                    RemoveEffect(effect.Type);
                }
            }
        }
        
        /// <summary>
        /// Update all effects (call in Update).
        /// </summary>
        public void Update(float deltaTime)
        {
            for (int i = _effects.Count - 1; i >= 0; i--)
            {
                _effects[i].Update(deltaTime);
            }
        }
        
        #region Status Checks
        
        public bool IsStunned => HasEffect(StatusEffectType.Stun) || HasEffect(StatusEffectType.Freeze);
        public bool IsSilenced => HasEffect(StatusEffectType.Silence);
        public bool IsInvincible => HasEffect(StatusEffectType.Invincible);
        public bool IsSlowed => HasEffect(StatusEffectType.Slow);
        
        /// <summary>
        /// Get total movement speed modifier (multiplier).
        /// </summary>
        public float GetSpeedModifier()
        {
            float modifier = 1f;
            
            if (HasEffect(StatusEffectType.Slow))
            {
                modifier *= (1f - GetEffect(StatusEffectType.Slow).Value);
            }
            
            if (HasEffect(StatusEffectType.Haste))
            {
                modifier *= (1f + GetEffect(StatusEffectType.Haste).Value);
            }
            
            return modifier;
        }
        
        /// <summary>
        /// Get total attack modifier (flat).
        /// </summary>
        public int GetAttackModifier()
        {
            int modifier = 0;
            
            if (HasEffect(StatusEffectType.Strength))
            {
                modifier += Mathf.RoundToInt(GetEffect(StatusEffectType.Strength).Value);
            }
            
            if (HasEffect(StatusEffectType.Weakness))
            {
                modifier -= Mathf.RoundToInt(GetEffect(StatusEffectType.Weakness).Value);
            }
            
            return modifier;
        }
        
        /// <summary>
        /// Get damage taken modifier (multiplier).
        /// </summary>
        public float GetDamageTakenModifier()
        {
            float modifier = 1f;
            
            if (HasEffect(StatusEffectType.Vulnerable))
            {
                modifier *= (1f + GetEffect(StatusEffectType.Vulnerable).Value);
            }
            
            if (HasEffect(StatusEffectType.Shield))
            {
                modifier *= 0f; // Full damage block (or reduce by shield value)
            }
            
            return modifier;
        }
        
        private bool IsDebuff(StatusEffectType type)
        {
            return type switch
            {
                StatusEffectType.Stun => true,
                StatusEffectType.Slow => true,
                StatusEffectType.Silence => true,
                StatusEffectType.Burn => true,
                StatusEffectType.Poison => true,
                StatusEffectType.Bleed => true,
                StatusEffectType.Freeze => true,
                StatusEffectType.Weakness => true,
                StatusEffectType.Vulnerable => true,
                _ => false
            };
        }
        
        #endregion
    }
}
