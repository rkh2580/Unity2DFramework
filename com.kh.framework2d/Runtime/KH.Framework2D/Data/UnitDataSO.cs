using UnityEngine;

namespace KH.Framework2D.Data
{
    /// <summary>
    /// ScriptableObject version of UnitData.
    /// Use this when you need direct asset references in the inspector.
    /// 
    /// For data pipeline (Excel/XML), use UnitData class instead.
    /// </summary>
    [CreateAssetMenu(fileName = "UnitDataSO", menuName = "Game Data/Unit Data (SO)")]
    public class UnitDataSO : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField, TextArea] private string _description;
        [SerializeField] private UnitType _unitType;
        [SerializeField] private UnitRarity _rarity;
        
        [Header("Visuals")]
        [SerializeField] private Sprite _icon;
        [SerializeField] private Sprite _sprite;
        [SerializeField] private RuntimeAnimatorController _animator;
        [SerializeField] private GameObject _prefab;
        
        [Header("Base Stats")]
        [SerializeField] private int _maxHp = 100;
        [SerializeField] private int _attack = 10;
        [SerializeField] private int _defense = 5;
        [SerializeField] private float _attackSpeed = 1f;
        [SerializeField] private float _moveSpeed = 3f;
        [SerializeField] private float _attackRange = 1.5f;
        [SerializeField] private float _criticalChance = 0.1f;
        [SerializeField] private float _criticalDamage = 1.5f;
        
        [Header("Skills")]
        [SerializeField] private SkillDataSO _basicSkill;
        [SerializeField] private SkillDataSO _ultimateSkill;
        [SerializeField] private SkillDataSO[] _passiveSkills;
        
        [Header("Level Scaling")]
        [SerializeField] private float _hpPerLevel = 10f;
        [SerializeField] private float _attackPerLevel = 2f;
        [SerializeField] private float _defensePerLevel = 1f;
        
        [Header("Audio")]
        [SerializeField] private AudioClip _attackSound;
        [SerializeField] private AudioClip _hurtSound;
        [SerializeField] private AudioClip _deathSound;
        [SerializeField] private AudioClip _skillSound;
        
        // Properties
        public string Id => _id;
        public string DisplayName => _displayName;
        public string Description => _description;
        public UnitType UnitType => _unitType;
        public UnitRarity Rarity => _rarity;
        
        public Sprite Icon => _icon;
        public Sprite Sprite => _sprite;
        public RuntimeAnimatorController Animator => _animator;
        public GameObject Prefab => _prefab;
        
        public int MaxHp => _maxHp;
        public int Attack => _attack;
        public int Defense => _defense;
        public float AttackSpeed => _attackSpeed;
        public float MoveSpeed => _moveSpeed;
        public float AttackRange => _attackRange;
        public float CriticalChance => _criticalChance;
        public float CriticalDamage => _criticalDamage;
        
        public SkillDataSO BasicSkill => _basicSkill;
        public SkillDataSO UltimateSkill => _ultimateSkill;
        public SkillDataSO[] PassiveSkills => _passiveSkills;
        
        public AudioClip AttackSound => _attackSound;
        public AudioClip HurtSound => _hurtSound;
        public AudioClip DeathSound => _deathSound;
        public AudioClip SkillSound => _skillSound;
        
        /// <summary>
        /// Get stats scaled for a specific level.
        /// </summary>
        public UnitStats GetStatsForLevel(int level)
        {
            int bonusLevels = level - 1;
            
            return new UnitStats
            {
                MaxHp = Mathf.RoundToInt(_maxHp + (_hpPerLevel * bonusLevels)),
                Attack = Mathf.RoundToInt(_attack + (_attackPerLevel * bonusLevels)),
                Defense = Mathf.RoundToInt(_defense + (_defensePerLevel * bonusLevels)),
                AttackSpeed = _attackSpeed,
                MoveSpeed = _moveSpeed,
                AttackRange = _attackRange,
                CriticalChance = _criticalChance,
                CriticalDamage = _criticalDamage
            };
        }
        
        /// <summary>
        /// Convert to data class (for systems that expect UnitData).
        /// </summary>
        public UnitData ToData()
        {
            return new UnitData
            {
                Id = _id,
                NameKey = _displayName,
                DescriptionKey = _description,
                UnitType = _unitType,
                Rarity = _rarity,
                MaxHp = _maxHp,
                Attack = _attack,
                Defense = _defense,
                AttackSpeed = _attackSpeed,
                MoveSpeed = _moveSpeed,
                AttackRange = _attackRange,
                CriticalChance = _criticalChance,
                CriticalDamage = _criticalDamage,
                HpPerLevel = _hpPerLevel,
                AttackPerLevel = _attackPerLevel,
                DefensePerLevel = _defensePerLevel,
                BasicSkillId = _basicSkill?.Id,
                UltimateSkillId = _ultimateSkill?.Id
            };
        }
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(_id))
            {
                _id = name.ToLower().Replace(" ", "_");
            }
        }
#endif
    }
}
