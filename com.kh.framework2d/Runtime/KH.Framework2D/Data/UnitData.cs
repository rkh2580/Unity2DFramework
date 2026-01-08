using System;
using KH.Framework2D.Data.Pipeline;
using UnityEngine;

namespace KH.Framework2D.Data
{
    /// <summary>
    /// Unit data loaded from XML via data pipeline.
    /// Assets (sprites, prefabs, audio) are bound via AssetRegistry.
    /// 
    /// This is the DATA-ONLY version for the data pipeline.
    /// For ScriptableObject version, see UnitDataSO.
    /// </summary>
    [Serializable]
    public class UnitData : IGameData, ILocalizable
    {
        // Identity
        public string Id { get; set; }
        public string NameKey { get; set; }
        public string DescriptionKey { get; set; }
        public UnitType UnitType { get; set; }
        public UnitRarity Rarity { get; set; }
        
        // Asset references (IDs for AssetRegistry lookup)
        public string IconId { get; set; }
        public string SpriteId { get; set; }
        public string AnimatorId { get; set; }
        public string PrefabId { get; set; }
        
        // Base Stats
        public int MaxHp { get; set; } = 100;
        public int Attack { get; set; } = 10;
        public int Defense { get; set; } = 5;
        public float AttackSpeed { get; set; } = 1f;
        public float MoveSpeed { get; set; } = 3f;
        public float AttackRange { get; set; } = 1.5f;
        public float CriticalChance { get; set; } = 0.1f;
        public float CriticalDamage { get; set; } = 1.5f;
        
        // Skills (IDs, resolved at runtime)
        public string BasicSkillId { get; set; }
        public string UltimateSkillId { get; set; }
        public string PassiveSkillIds { get; set; }  // Comma-separated
        
        // Level Scaling
        public float HpPerLevel { get; set; } = 10f;
        public float AttackPerLevel { get; set; } = 2f;
        public float DefensePerLevel { get; set; } = 1f;
        
        // Audio (IDs for AssetRegistry lookup)
        public string AttackSoundId { get; set; }
        public string HurtSoundId { get; set; }
        public string DeathSoundId { get; set; }
        public string SkillSoundId { get; set; }
        
        // ILocalizable implementation
        string ILocalizable.NameKey => NameKey;
        string ILocalizable.DescriptionKey => DescriptionKey;
        
        /// <summary>
        /// Get stats scaled for a specific level.
        /// </summary>
        public UnitStats GetStatsForLevel(int level)
        {
            int bonusLevels = level - 1;
            
            return new UnitStats
            {
                MaxHp = (int)(MaxHp + (HpPerLevel * bonusLevels)),
                Attack = (int)(Attack + (AttackPerLevel * bonusLevels)),
                Defense = (int)(Defense + (DefensePerLevel * bonusLevels)),
                AttackSpeed = AttackSpeed,
                MoveSpeed = MoveSpeed,
                AttackRange = AttackRange,
                CriticalChance = CriticalChance,
                CriticalDamage = CriticalDamage
            };
        }
        
        /// <summary>
        /// Get asset references from AssetRegistry.
        /// </summary>
        public UnitAssets GetAssets()
        {
            var registry = AssetRegistry.Instance;
            if (registry == null)
                return default;
            
            return new UnitAssets
            {
                Icon = registry.GetSprite(IconId),
                Sprite = registry.GetSprite(SpriteId),
                Animator = registry.GetAnimator(AnimatorId),
                Prefab = registry.GetPrefab(PrefabId),
                AttackSound = registry.GetAudioClip(AttackSoundId),
                HurtSound = registry.GetAudioClip(HurtSoundId),
                DeathSound = registry.GetAudioClip(DeathSoundId),
                SkillSound = registry.GetAudioClip(SkillSoundId)
            };
        }
    }
    
    /// <summary>
    /// Asset references for a unit (resolved from AssetRegistry).
    /// </summary>
    public struct UnitAssets
    {
        public Sprite Icon;
        public Sprite Sprite;
        public RuntimeAnimatorController Animator;
        public GameObject Prefab;
        public AudioClip AttackSound;
        public AudioClip HurtSound;
        public AudioClip DeathSound;
        public AudioClip SkillSound;
    }
    
    /// <summary>
    /// Runtime unit stats (can be modified by buffs/debuffs).
    /// </summary>
    [System.Serializable]
    public struct UnitStats
    {
        public int MaxHp;
        public int Attack;
        public int Defense;
        public float AttackSpeed;
        public float MoveSpeed;
        public float AttackRange;
        public float CriticalChance;
        public float CriticalDamage;
        
        public static UnitStats operator +(UnitStats a, UnitStats b)
        {
            return new UnitStats
            {
                MaxHp = a.MaxHp + b.MaxHp,
                Attack = a.Attack + b.Attack,
                Defense = a.Defense + b.Defense,
                AttackSpeed = a.AttackSpeed + b.AttackSpeed,
                MoveSpeed = a.MoveSpeed + b.MoveSpeed,
                AttackRange = a.AttackRange + b.AttackRange,
                CriticalChance = a.CriticalChance + b.CriticalChance,
                CriticalDamage = a.CriticalDamage + b.CriticalDamage
            };
        }
    }
    
    public enum UnitType
    {
        Warrior,    // Melee tank
        Archer,     // Ranged DPS
        Assassin,   // Melee burst
        Healer,     // Support
        Mage,       // Ranged AoE
        Tank        // Pure tank
    }
    
    public enum UnitRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }
}
