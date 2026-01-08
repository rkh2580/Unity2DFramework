using UnityEngine;

namespace KH.Framework2D.Combat
{
    /// <summary>
    /// Combat calculation formulas.
    /// Centralized place for all damage, healing, and combat math.
    /// </summary>
    public static class CombatFormulas
    {
        #region Damage Calculation
        
        /// <summary>
        /// Calculate final damage after defense.
        /// Formula: Damage * (100 / (100 + Defense))
        /// </summary>
        public static int CalculateDamage(int rawDamage, int defense)
        {
            if (rawDamage <= 0) return 0;
            
            // Defense reduction formula (diminishing returns)
            float reduction = 100f / (100f + Mathf.Max(0, defense));
            int finalDamage = Mathf.RoundToInt(rawDamage * reduction);
            
            return Mathf.Max(1, finalDamage); // Minimum 1 damage
        }
        
        /// <summary>
        /// Calculate damage with critical hit check.
        /// </summary>
        public static DamageResult CalculateDamageWithCrit(
            int rawDamage, 
            int defense, 
            float critChance, 
            float critMultiplier)
        {
            bool isCritical = Random.value < critChance;
            int damage = rawDamage;
            
            if (isCritical)
            {
                damage = Mathf.RoundToInt(damage * critMultiplier);
            }
            
            int finalDamage = CalculateDamage(damage, defense);
            
            return new DamageResult
            {
                Damage = finalDamage,
                IsCritical = isCritical,
                WasDodged = false,
                WasBlocked = false
            };
        }
        
        /// <summary>
        /// Calculate damage with miss/dodge chance.
        /// </summary>
        public static DamageResult CalculateDamageWithDodge(
            int rawDamage,
            int defense,
            float critChance,
            float critMultiplier,
            float dodgeChance)
        {
            // Check dodge first
            if (Random.value < dodgeChance)
            {
                return new DamageResult
                {
                    Damage = 0,
                    IsCritical = false,
                    WasDodged = true,
                    WasBlocked = false
                };
            }
            
            return CalculateDamageWithCrit(rawDamage, defense, critChance, critMultiplier);
        }
        
        /// <summary>
        /// Calculate skill damage.
        /// </summary>
        public static int CalculateSkillDamage(
            float baseDamage,
            float scaling,
            int attackStat,
            int defense,
            bool isMagic = false)
        {
            int rawDamage = Mathf.RoundToInt(baseDamage + (attackStat * scaling));
            
            // Magic damage might use different defense (or no defense)
            int effectiveDefense = isMagic ? defense / 2 : defense;
            
            return CalculateDamage(rawDamage, effectiveDefense);
        }
        
        /// <summary>
        /// Calculate true damage (ignores defense).
        /// </summary>
        public static int CalculateTrueDamage(int rawDamage)
        {
            return Mathf.Max(1, rawDamage);
        }
        
        #endregion
        
        #region Healing
        
        /// <summary>
        /// Calculate heal amount.
        /// </summary>
        public static int CalculateHeal(float baseHeal, float scaling, int attackStat)
        {
            return Mathf.RoundToInt(baseHeal + (attackStat * scaling));
        }
        
        /// <summary>
        /// Calculate heal with overheal check.
        /// </summary>
        public static HealResult CalculateHealWithOverheal(int healAmount, int currentHp, int maxHp)
        {
            int actualHeal = Mathf.Min(healAmount, maxHp - currentHp);
            int overheal = healAmount - actualHeal;
            
            return new HealResult
            {
                HealAmount = actualHeal,
                Overheal = overheal,
                NewHp = currentHp + actualHeal
            };
        }
        
        #endregion
        
        #region Status Effects
        
        /// <summary>
        /// Calculate DoT (Damage over Time) tick damage.
        /// </summary>
        public static int CalculateDoTDamage(float damagePerTick, int defense, bool ignoreDefense = false)
        {
            if (ignoreDefense)
            {
                return Mathf.RoundToInt(damagePerTick);
            }
            
            return CalculateDamage(Mathf.RoundToInt(damagePerTick), defense);
        }
        
        /// <summary>
        /// Calculate slowed movement speed.
        /// </summary>
        public static float CalculateSlowedSpeed(float baseSpeed, float slowPercent)
        {
            float reduction = Mathf.Clamp01(slowPercent);
            return baseSpeed * (1f - reduction);
        }
        
        #endregion
        
        #region Utility
        
        /// <summary>
        /// Calculate attack interval from attack speed.
        /// </summary>
        public static float AttackSpeedToInterval(float attackSpeed)
        {
            return attackSpeed > 0 ? 1f / attackSpeed : float.MaxValue;
        }
        
        /// <summary>
        /// Check if target is in range.
        /// </summary>
        public static bool IsInRange(Vector3 from, Vector3 to, float range)
        {
            return Vector3.Distance(from, to) <= range;
        }
        
        /// <summary>
        /// Check if target is in range (2D).
        /// </summary>
        public static bool IsInRange2D(Vector2 from, Vector2 to, float range)
        {
            return Vector2.Distance(from, to) <= range;
        }
        
        /// <summary>
        /// Get direction towards target.
        /// </summary>
        public static Vector3 GetDirection(Vector3 from, Vector3 to)
        {
            return (to - from).normalized;
        }
        
        /// <summary>
        /// Calculate effective stat after buffs/debuffs.
        /// </summary>
        public static int ApplyStatModifier(int baseStat, float percentModifier, int flatModifier)
        {
            float modified = baseStat * (1f + percentModifier) + flatModifier;
            return Mathf.Max(0, Mathf.RoundToInt(modified));
        }
        
        /// <summary>
        /// Calculate level-based stat value.
        /// </summary>
        public static int CalculateLevelStat(int baseStat, float perLevel, int level)
        {
            return Mathf.RoundToInt(baseStat + (perLevel * (level - 1)));
        }
        
        /// <summary>
        /// Calculate experience required for next level.
        /// Formula: BaseXP * (Level ^ Exponent)
        /// </summary>
        public static int CalculateExpForLevel(int level, int baseExp = 100, float exponent = 1.5f)
        {
            return Mathf.RoundToInt(baseExp * Mathf.Pow(level, exponent));
        }
        
        #endregion
        
        #region Random Helpers
        
        /// <summary>
        /// Roll a chance (0-1).
        /// </summary>
        public static bool RollChance(float chance)
        {
            return Random.value < chance;
        }
        
        /// <summary>
        /// Get random value in range with variance.
        /// </summary>
        public static int RandomRange(int baseValue, float variance)
        {
            float min = baseValue * (1f - variance);
            float max = baseValue * (1f + variance);
            return Mathf.RoundToInt(Random.Range(min, max));
        }
        
        #endregion
    }
    
    /// <summary>
    /// Result of a damage calculation.
    /// </summary>
    public struct DamageResult
    {
        public int Damage;
        public bool IsCritical;
        public bool WasDodged;
        public bool WasBlocked;
        
        public bool WasHit => !WasDodged && !WasBlocked;
    }
    
    /// <summary>
    /// Result of a heal calculation.
    /// </summary>
    public struct HealResult
    {
        public int HealAmount;
        public int Overheal;
        public int NewHp;
    }
}
