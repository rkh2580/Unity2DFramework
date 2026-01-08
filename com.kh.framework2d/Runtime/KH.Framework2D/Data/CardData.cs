using System;
using System.Collections.Generic;
using KH.Framework2D.Data.Pipeline;

namespace KH.Framework2D.Data
{
    /// <summary>
    /// Card data for deck-building game.
    /// Loaded from XML, assets bound via AssetRegistry.
    /// 
    /// XML Fields:
    /// - Id: Unique identifier (e.g., "card_fireball_001")
    /// - NameKey: Localization key for card name
    /// - DescriptionKey: Localization key for description
    /// - CardType: Attack, Skill, Power, Status, Curse
    /// - Rarity: Common, Uncommon, Rare, Epic, Legendary
    /// - Cost: Mana cost to play
    /// - TargetType: Self, SingleEnemy, AllEnemies, SingleAlly, AllAllies, None
    /// - Effects: Comma-separated effect IDs
    /// </summary>
    [Serializable]
    public class CardData : IGameData, ILocalizable
    {
        // Identity
        public string Id { get; set; }
        public string NameKey { get; set; }
        public string DescriptionKey { get; set; }
        
        // Type & Rarity
        public CardType CardType { get; set; }
        public CardRarity Rarity { get; set; }
        
        // Cost
        public int Cost { get; set; }
        public bool Exhausts { get; set; }      // Removed from deck after use
        public bool Ethereal { get; set; }      // Exhausts if not played
        public bool Innate { get; set; }        // Starts in hand
        public bool Retain { get; set; }        // Keeps in hand between turns
        
        // Targeting
        public TargetType TargetType { get; set; }
        
        // Base values (can be modified by upgrades)
        public int BaseDamage { get; set; }
        public int BaseBlock { get; set; }
        public int BaseMagicNumber { get; set; }    // Generic value for effects
        public int BaseHeal { get; set; }
        
        // Upgraded values
        public int UpgradedCost { get; set; } = -1;     // -1 means no change
        public int UpgradedDamage { get; set; }
        public int UpgradedBlock { get; set; }
        public int UpgradedMagicNumber { get; set; }
        public int UpgradedHeal { get; set; }
        
        // Effects (comma-separated IDs, parsed at runtime)
        public string Effects { get; set; }
        public string UpgradedEffects { get; set; }
        
        // Visuals (references to AssetRegistry)
        public string SpriteId { get; set; }
        public string AnimationId { get; set; }
        public string SoundId { get; set; }
        
        // Parsed effect list (populated after loading)
        private List<string> _effectIds;
        private List<string> _upgradedEffectIds;
        
        /// <summary>
        /// Get list of effect IDs for this card.
        /// </summary>
        public IReadOnlyList<string> GetEffectIds(bool upgraded = false)
        {
            if (upgraded && !string.IsNullOrEmpty(UpgradedEffects))
            {
                _upgradedEffectIds ??= ParseCommaSeparated(UpgradedEffects);
                return _upgradedEffectIds;
            }
            
            _effectIds ??= ParseCommaSeparated(Effects);
            return _effectIds;
        }
        
        /// <summary>
        /// Get actual cost (considering upgrade).
        /// </summary>
        public int GetCost(bool upgraded)
        {
            if (upgraded && UpgradedCost >= 0)
                return UpgradedCost;
            return Cost;
        }
        
        /// <summary>
        /// Get actual damage (considering upgrade).
        /// </summary>
        public int GetDamage(bool upgraded)
        {
            return upgraded ? UpgradedDamage : BaseDamage;
        }
        
        /// <summary>
        /// Get actual block (considering upgrade).
        /// </summary>
        public int GetBlock(bool upgraded)
        {
            return upgraded ? UpgradedBlock : BaseBlock;
        }
        
        private static List<string> ParseCommaSeparated(string value)
        {
            var result = new List<string>();
            
            if (string.IsNullOrEmpty(value))
                return result;
            
            var parts = value.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                var trimmed = part.Trim();
                if (!string.IsNullOrEmpty(trimmed))
                    result.Add(trimmed);
            }
            
            return result;
        }
    }
    
    /// <summary>
    /// Card effect data.
    /// Defines what happens when a card is played.
    /// </summary>
    [Serializable]
    public class CardEffectData : IGameData
    {
        public string Id { get; set; }
        public CardEffectType EffectType { get; set; }
        public int Value { get; set; }
        public int Duration { get; set; }
        public string TargetOverride { get; set; }  // Override card's target type
        public string Condition { get; set; }       // Conditional execution
    }
    
    #region Enums
    
    public enum CardType
    {
        Attack,     // Damage dealing cards
        Skill,      // Utility/defense cards
        Power,      // Persistent effects
        Status,     // Negative cards added by enemies
        Curse       // Harmful cards that cannot be played
    }
    
    public enum CardRarity
    {
        Starter,    // Basic deck cards
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary,
        Special     // Event/boss rewards
    }
    
    public enum TargetType
    {
        None,           // No target (self-buff, etc.)
        Self,           // Targets the player
        SingleEnemy,    // Pick one enemy
        AllEnemies,     // All enemies
        SingleAlly,     // Pick one ally (if applicable)
        AllAllies,      // All allies
        Random          // Random valid target
    }
    
    public enum CardEffectType
    {
        // Damage
        Damage,
        DamageAll,
        DamageRandom,
        
        // Defense
        Block,
        BlockAll,
        
        // Healing
        Heal,
        HealAll,
        
        // Card manipulation
        Draw,
        Discard,
        DiscardRandom,
        Exhaust,
        AddToHand,
        AddToDeck,
        AddToDiscard,
        Upgrade,
        
        // Status effects
        ApplyBuff,
        ApplyDebuff,
        RemoveBuff,
        RemoveDebuff,
        
        // Resources
        GainEnergy,
        GainGold,
        
        // Special
        Scry,           // Look at top cards
        Retain,         // Keep card in hand
        Transform,      // Change to another card
        Duplicate       // Copy the card
    }
    
    #endregion
}
