using UnityEngine;

namespace KH.Framework2D.Data
{
    /// <summary>
    /// ScriptableObject definition for skills and abilities.
    /// </summary>
    [CreateAssetMenu(fileName = "SkillData", menuName = "Game Data/Skill Data")]
    public class SkillDataSO : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField, TextArea] private string _description;
        [SerializeField] private Sprite _icon;
        
        [Header("Skill Type")]
        [SerializeField] private SkillType _skillType = SkillType.Active;
        [SerializeField] private TargetType _targetType = TargetType.Enemy;
        [SerializeField] private DamageType _damageType = DamageType.Physical;
        
        [Header("Damage / Effect")]
        [SerializeField] private float _baseDamage = 10f;
        [SerializeField] private float _damageScaling = 1f;      // Multiplier with Attack stat
        [SerializeField] private float _healAmount = 0f;
        [SerializeField] private float _healScaling = 0f;
        
        [Header("Cooldown & Cost")]
        [SerializeField] private float _cooldown = 5f;
        [SerializeField] private int _manaCost = 0;
        [SerializeField] private float _castTime = 0f;
        
        [Header("Range & Area")]
        [SerializeField] private float _range = 5f;
        [SerializeField] private bool _isAoE = false;
        [SerializeField] private float _aoeRadius = 2f;
        [SerializeField] private int _maxTargets = 1;
        
        [Header("Projectile (if ranged)")]
        [SerializeField] private bool _hasProjectile = false;
        [SerializeField] private GameObject _projectilePrefab;
        [SerializeField] private float _projectileSpeed = 10f;
        
        [Header("Status Effects")]
        [SerializeField] private StatusEffectData[] _appliedEffects;
        
        [Header("Visual Effects")]
        [SerializeField] private GameObject _castEffect;
        [SerializeField] private GameObject _hitEffect;
        [SerializeField] private string _animationTrigger;
        
        [Header("Audio")]
        [SerializeField] private AudioClip _castSound;
        [SerializeField] private AudioClip _hitSound;
        
        // Properties
        public string Id => _id;
        public string DisplayName => _displayName;
        public string Description => _description;
        public Sprite Icon => _icon;
        
        public SkillType SkillType => _skillType;
        public TargetType TargetType => _targetType;
        public DamageType DamageType => _damageType;
        
        public float BaseDamage => _baseDamage;
        public float DamageScaling => _damageScaling;
        public float HealAmount => _healAmount;
        public float HealScaling => _healScaling;
        
        public float Cooldown => _cooldown;
        public int ManaCost => _manaCost;
        public float CastTime => _castTime;
        
        public float Range => _range;
        public bool IsAoE => _isAoE;
        public float AoERadius => _aoeRadius;
        public int MaxTargets => _maxTargets;
        
        public bool HasProjectile => _hasProjectile;
        public GameObject ProjectilePrefab => _projectilePrefab;
        public float ProjectileSpeed => _projectileSpeed;
        
        public StatusEffectData[] AppliedEffects => _appliedEffects;
        
        public GameObject CastEffect => _castEffect;
        public GameObject HitEffect => _hitEffect;
        public string AnimationTrigger => _animationTrigger;
        
        public AudioClip CastSound => _castSound;
        public AudioClip HitSound => _hitSound;
        
        /// <summary>
        /// Calculate final damage based on caster's attack stat.
        /// </summary>
        public int CalculateDamage(int attackStat, bool isCritical = false, float criticalMultiplier = 1.5f)
        {
            float damage = _baseDamage + (attackStat * _damageScaling);
            
            if (isCritical)
            {
                damage *= criticalMultiplier;
            }
            
            return Mathf.RoundToInt(damage);
        }
        
        /// <summary>
        /// Calculate heal amount.
        /// </summary>
        public int CalculateHeal(int attackStat)
        {
            return Mathf.RoundToInt(_healAmount + (attackStat * _healScaling));
        }
        
        /// <summary>
        /// Get formatted description with actual values.
        /// </summary>
        public string GetFormattedDescription(int attackStat)
        {
            string desc = _description;
            desc = desc.Replace("{damage}", CalculateDamage(attackStat).ToString());
            desc = desc.Replace("{heal}", CalculateHeal(attackStat).ToString());
            desc = desc.Replace("{cooldown}", _cooldown.ToString("F1"));
            desc = desc.Replace("{range}", _range.ToString("F1"));
            return desc;
        }
        
        #region Editor
#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(_id))
            {
                _id = name.ToLower().Replace(" ", "_");
            }
        }
#endif
        #endregion
    }
    
    /// <summary>
    /// Status effect data applied by skills.
    /// </summary>
    [System.Serializable]
    public class StatusEffectData
    {
        public StatusEffectType effectType;
        public float value;           // Damage per tick, slow amount, etc.
        public float duration;
        public float tickInterval;    // For DoT effects
        
        [Tooltip("Chance to apply (0-1)")]
        public float applyChance = 1f;
    }
    
    public enum SkillType
    {
        Active,     // Manually activated
        Passive,    // Always active
        Ultimate,   // Charged/powerful skill
        Basic       // Auto-attack
    }
    
    public enum TargetType
    {
        Self,
        Enemy,
        Ally,
        AllEnemies,
        AllAllies,
        Area
    }
    
    public enum DamageType
    {
        Physical,
        Magic,
        True,       // Ignores defense
        Heal
    }
    
    public enum StatusEffectType
    {
        None,
        // Debuffs
        Stun,
        Slow,
        Silence,
        Burn,       // DoT
        Poison,     // DoT
        Bleed,      // DoT
        Freeze,
        Weakness,   // Reduce attack
        Vulnerable, // Increase damage taken
        
        // Buffs
        Haste,      // Increase speed
        Shield,
        Regen,      // HoT
        Strength,   // Increase attack
        Invincible
    }
}
