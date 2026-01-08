# Data Pipeline Guide

## Overview

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         DATA PIPELINE FLOW                              │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│   [Excel/Google Sheets]                                                 │
│          │                                                              │
│          ▼  (Export as CSV - UTF-8)                                     │
│   [Assets/DataTables/*.csv]                                             │
│          │                                                              │
│          ▼  (Editor: Tools > Data Pipeline > Convert)                   │
│   [Assets/Resources/Data/*.xml]                                         │
│          │                                                              │
│          ▼  (Runtime: DataService.InitializeAsync())                    │
│   [DataContainer<T>]  ←──── IDataService.Get<CardData>("card_001")     │
│          │                                                              │
│          ▼  (Asset Binding)                                             │
│   [AssetRegistry]  ←──── AssetRegistry.Instance.GetSprite("card_001")  │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

## Quick Start

### 1. Create Data Table (Excel/Sheets)

| Id | NameKey | Cost | BaseDamage | CardType | Effects |
|----|---------|------|------------|----------|---------|
| card_001 | card_fireball | 3 | 5 | Attack | damage,burn |
| card_002 | card_shield | 2 | 0 | Skill | block |

### 2. Export as CSV (UTF-8)

Save to: `Assets/DataTables/Cards.csv`

```csv
Id,NameKey,Cost,BaseDamage,CardType,Effects
card_001,card_fireball,3,5,Attack,"damage,burn"
card_002,card_shield,2,0,Skill,block
```

### 3. Convert to XML

Menu: `Tools > Data Pipeline > Convert All CSV to XML`

Generated: `Assets/Resources/Data/Cards.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<DataTable source="Cards.csv" generated="2026-01-08 12:00:00">
  <Row>
    <Id>card_001</Id>
    <NameKey>card_fireball</NameKey>
    <Cost>3</Cost>
    <BaseDamage>5</BaseDamage>
    <CardType>Attack</CardType>
    <Effects>damage,burn</Effects>
  </Row>
  <!-- ... -->
</DataTable>
```

### 4. Define Data Class

```csharp
using KH.Framework2D.Data.Pipeline;

[Serializable]
public class CardData : IGameData
{
    public string Id { get; set; }
    public string NameKey { get; set; }
    public int Cost { get; set; }
    public int BaseDamage { get; set; }
    public CardType CardType { get; set; }
    public string Effects { get; set; }
}
```

### 5. Register and Load

```csharp
// In your LifetimeScope
protected override void Configure(IContainerBuilder builder)
{
    var dataService = new DataService();
    
    // Register data types
    dataService.RegisterDataType<CardData>("Data/Cards");
    dataService.RegisterDataType<UnitData>("Data/Units");
    
    builder.RegisterInstance<IDataService>(dataService);
}

// Or use DataServiceInstaller component
```

### 6. Access Data

```csharp
public class CardSystem
{
    private readonly IDataService _dataService;
    
    public CardSystem(IDataService dataService)
    {
        _dataService = dataService;
    }
    
    public void UseCard(string cardId)
    {
        var card = _dataService.Get<CardData>(cardId);
        if (card == null) return;
        
        Debug.Log($"Playing {card.NameKey} for {card.Cost} mana");
        
        // Get sprite from AssetRegistry
        var sprite = AssetRegistry.Instance.GetSprite(cardId);
    }
}
```

## CSV Format Rules

### Basic Rules
- First row: Column headers (become XML element names)
- Subsequent rows: Data values
- UTF-8 encoding required

### Special Features
- **Comments**: Rows starting with `#` are ignored
- **Skip columns**: Columns starting with `@` are ignored (useful for notes)
- **Quoted values**: Use quotes for values containing commas: `"damage,burn"`

### Example with All Features

```csv
Id,Name,Description,Cost,@Notes
# Hero cards
card_hero_001,Strike,"Deal 6 damage",1,Starter card
card_hero_002,Defend,"Gain 5 block",1,Starter card
# Boss drops
card_boss_001,Ragnarok,"Deal 50 damage to ALL enemies",4,Rare drop
```

## Asset Binding (AssetRegistry)

Separating data (numbers) from assets (sprites, sounds):

### Create AssetRegistry
1. Right-click in Project > Create > Data > Asset Registry
2. Add entries mapping IDs to assets

```csharp
// Access assets by data ID
var sprite = AssetRegistry.Instance.GetSprite("card_001");
var prefab = AssetRegistry.Instance.GetPrefab("unit_warrior");
var sound = AssetRegistry.Instance.GetAudioClip("sfx_attack");
```

## Best Practices

### ID Naming Convention
```
{category}_{subcategory}_{number}

Examples:
- card_attack_001
- card_skill_heal_001
- unit_enemy_goblin_01
- item_potion_health_01
```

### Data vs Assets
| Data (XML) | Assets (AssetRegistry) |
|------------|------------------------|
| Numeric values | Sprites |
| Enum types | Prefabs |
| String IDs | Audio clips |
| Localization keys | Animations |

### Hot Reload (Development)
```csharp
// Reload specific data type during development
await _dataService.ReloadAsync<CardData>();
```

### Validation
```csharp
// Check if data exists
if (_dataService.Exists<CardData>(cardId))
{
    // Safe to use
}

// Try pattern
if (_dataService.TryGet<CardData>(cardId, out var card))
{
    // Use card
}
```

## Folder Structure

```
Assets/
├── DataTables/              # Source CSV files (edit these)
│   ├── Cards.csv
│   ├── Units.csv
│   └── Items.csv
├── Resources/
│   └── Data/                # Generated XML (don't edit)
│       ├── Cards.xml
│       ├── Units.xml
│       └── Items.xml
└── ScriptableObjects/
    └── AssetRegistry.asset  # Unity asset references
```

## Troubleshooting

### Common Issues

**CSV not converting?**
- Check UTF-8 encoding
- Verify file is in DataTables folder
- Menu: Tools > Data Pipeline > Convert All

**Data not loading?**
- Check Resources/Data/ folder exists
- Verify RegisterDataType was called
- Check XML file format

**Property not mapping?**
- Property name must match XML element name
- Use public setters or {get; set;}
- Check for typos

### Debug Logging
```csharp
_dataService.OnLoadProgress += progress => 
    Debug.Log($"Loading: {progress:P0}");

_dataService.OnDataLoaded += () => 
    Debug.Log($"Loaded {_dataService.Count<CardData>()} cards");
```
