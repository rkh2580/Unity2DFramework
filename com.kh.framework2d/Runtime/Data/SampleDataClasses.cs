using System;
using KH.Framework2D.Data.Pipeline;

namespace KH.Framework2D.Data
{
    /// <summary>
    /// Card type enumeration.
    /// </summary>
    public enum CardType
    {
        Attack,
        Skill,
        Power,
        Status,
        Curse
    }
    
    /// <summary>
    /// Skill target type.
    /// </summary>
    public enum TargetType
    {
        Single,
        All,
        Random,
        Self
    }
    
    /// <summary>
    /// Unit type.
    /// </summary>
    public enum UnitType
    {
        Player,
        Enemy,
        Boss,
        NPC
    }
    
    /// <summary>
    /// Card data - loaded from Cards.xml
    /// </summary>
    [Serializable]
    public class CardData : IGameData
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int Cost { get; set; }
        public CardType Type { get; set; }
        public string Effect { get; set; }
        public string SkillId { get; set; }
        public string SpritePath { get; set; }
        public int Rarity { get; set; }
        public bool IsUpgraded { get; set; }
        
        public override string ToString() => $"[Card] {Id}: {Name} ({Type}, Cost {Cost})";
    }
    
    /// <summary>
    /// Unit data - loaded from Units.xml
    /// </summary>
    [Serializable]
    public class UnitData : IGameData
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public int HP { get; set; }
        public int MaxHP { get; set; }
        public int ATK { get; set; }
        public int DEF { get; set; }
        public UnitType Type { get; set; }
        public string PrefabPath { get; set; }
        public string SpritePath { get; set; }
        
        public override string ToString() => $"[Unit] {Id}: {Name} (HP {HP}, ATK {ATK})";
    }
    
    /// <summary>
    /// Skill data - loaded from Skills.xml
    /// </summary>
    [Serializable]
    public class SkillData : IGameData
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int Damage { get; set; }
        public int Heal { get; set; }
        public int Block { get; set; }
        public TargetType TargetType { get; set; }
        public string EffectPrefab { get; set; }
        public float Duration { get; set; }
        
        public override string ToString() => $"[Skill] {Id}: {Name} (Damage {Damage}, Target {TargetType})";
    }
    
    /// <summary>
    /// Stage/Level data - loaded from Stages.xml
    /// </summary>
    [Serializable]
    public class StageData : IGameData
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public int Chapter { get; set; }
        public int StageNumber { get; set; }
        public string BgmId { get; set; }
        public string BackgroundPath { get; set; }
        public string EnemyIds { get; set; } // Comma-separated enemy IDs
        public int RewardGold { get; set; }
        
        public string[] GetEnemyIds()
        {
            if (string.IsNullOrEmpty(EnemyIds)) return Array.Empty<string>();
            return EnemyIds.Split(',', StringSplitOptions.RemoveEmptyEntries);
        }
        
        public override string ToString() => $"[Stage] {Id}: {Name} (Ch.{Chapter}-{StageNumber})";
    }
    
    /// <summary>
    /// Item data - loaded from Items.xml
    /// </summary>
    [Serializable]
    public class ItemData : IGameData
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int Price { get; set; }
        public int Rarity { get; set; }
        public string Effect { get; set; }
        public string SpritePath { get; set; }
        public bool IsConsumable { get; set; }
        
        public override string ToString() => $"[Item] {Id}: {Name} ({Price}G)";
    }
}
