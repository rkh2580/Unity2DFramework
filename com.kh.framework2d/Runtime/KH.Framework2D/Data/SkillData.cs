using System;
using KH.Framework2D.Data.Pipeline;

namespace KH.Framework2D.Data
{
    /// <summary>
    /// Skill data loaded from XML via data pipeline.
    /// Assets (effects, sounds) are bound via AssetRegistry.
    /// 
    /// This is the DATA-ONLY version for the data pipeline.
    /// For ScriptableObject version, see SkillDataSO.
    /// </summary>
    [Serializable]
    public class SkillData : IGameData, ILocalizable
    {
        // Identity
        public string Id { get; set; }
        public string NameKey { get; set; }
        public string DescriptionKey { get; set; }
        public string IconId { get; set; }
        
        // Type
        public SkillType SkillType { get; set; }
        public TargetType TargetType { get; set; }
        public DamageType DamageType { get; set; }
        
        // Damage / Effect
        public float BaseDamage { get; set; }
        public float DamageScaling { get; set; } = 1f;
        public float HealAmount { get; set; }
        public float HealScaling { get; set; }
        
        // Cooldown & Cost
        public float Cooldown { get; set; } = 5f;
        public int ManaCost { get; set; }
        public float CastTime { get; set; }
        
        // Range & Area
        public float Range { get; set; } = 5f;
        public bool IsAoE { get; set; }
        public float AoERadius { get; set; } = 2f;
        public int MaxTargets { get; set; } = 1;
        
        // Projectile
        public bool HasProjectile { get; set; }
        public string ProjectilePrefabId { get; set; }
        public float ProjectileSpeed { get; set; } = 10f;
        
        // Status Effects (comma-separated IDs)
        public string AppliedEffectIds { get; set; }
        
        // Visual & Audio (AssetRegistry IDs)
        public string CastEffectId { get; set; }
        public string HitEffectId { get; set; }
        public string AnimationTrigger { get; set; }
        public string CastSoundId { get; set; }
        public string HitSoundId { get; set; }
        
        // ILocalizable
        string ILocalizable.NameKey => NameKey;
        string ILocalizable.DescriptionKey => DescriptionKey;
        
        /// <summary>
        /// Calculate final damage based on caster's attack stat.
        /// </summary>
        public int CalculateDamage(int attackStat, bool isCritical = false, float criticalMultiplier = 1.5f)
        {
            float damage = BaseDamage + (attackStat * DamageScaling);
            
            if (isCritical)
                damage *= criticalMultiplier;
            
            return (int)Math.Round(damage);
        }
        
        /// <summary>
        /// Calculate heal amount.
        /// </summary>
        public int CalculateHeal(int attackStat)
        {
            return (int)Math.Round(HealAmount + (attackStat * HealScaling));
        }
    }
    
    // Keep enum definitions for SkillType, TargetType, DamageType (already in SkillDataSO.cs)
    // If those enums are in the renamed file, we need to reference them here or define them separately
}
