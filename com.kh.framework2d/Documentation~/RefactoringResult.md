# KH Framework 2D - 리뷰 및 리팩토링 결과

## 📊 프레임워크 리뷰 요약

### ✅ 장점 (유지)

| 영역 | 평가 | 비고 |
|------|------|------|
| **ServiceLocator** | ⭐⭐⭐⭐⭐ | VContainer 보조용으로 적절, TryGet 패턴 |
| **인터페이스 분리** | ⭐⭐⭐⭐⭐ | IAudioService, ISceneService 등 잘 정의 |
| **MVP 패턴** | ⭐⭐⭐⭐⭐ | VContainer IStartable/IDisposable 통합 |
| **StateMachine** | ⭐⭐⭐⭐ | 제네릭 Owner 패턴, 상태 전환 깔끔 |
| **ObjectPool** | ⭐⭐⭐⭐⭐ | UniTask WarmUp, IPoolable 콜백 |
| **EventChannel** | ⭐⭐⭐⭐ | ScriptableObject 기반 디커플링 |
| **네이밍 컨벤션** | ⭐⭐⭐⭐⭐ | _언더스코어, I접두사 일관성 |

### ⚠️ 개선된 부분 (이번 리팩토링)

| 문제 | 해결 |
|------|------|
| 데이터 파이프라인 없음 | ✅ Excel→CSV→XML→Game 파이프라인 추가 |
| IDataService 없음 | ✅ 데이터 로딩/캐싱 통합 인터페이스 추가 |
| 데이터-에셋 혼합 | ✅ AssetRegistry로 분리 |
| SO만 지원 | ✅ IGameData 기반 데이터 클래스 추가 |

---

## 📁 추가/수정된 파일 목록

### 새로 추가된 파일 (Data Pipeline)

```
Runtime/KH.Framework2D/Data/Pipeline/
├── IDataService.cs          # 데이터 서비스 인터페이스
├── DataContainer.cs         # 제네릭 데이터 컨테이너
├── DataService.cs           # IDataService 구현체
├── XmlDataParser.cs         # XML 파싱 유틸리티
└── AssetRegistry.cs         # Unity 에셋 바인딩

Runtime/KH.Framework2D/Data/
├── CardData.cs              # 덱빌딩용 카드 데이터
├── SkillData.cs             # 스킬 데이터 (IGameData)
├── UnitData.cs              # 유닛 데이터 (IGameData) - 리팩토링
├── UnitDataSO.cs            # 유닛 데이터 (ScriptableObject)
├── SkillDataSO.cs           # 스킬 데이터 (ScriptableObject)
└── DataServiceInstaller.cs  # VContainer 설정용

Editor/KH.Framework2D.Editor/DataPipeline/
├── KH.Framework2D.Editor.asmdef
└── ExcelToXmlConverter.cs   # CSV→XML 변환 에디터 도구

Documentation~/
└── DataPipeline.md          # 데이터 파이프라인 가이드
```

---

## 🔄 데이터 파이프라인 워크플로우

```
┌─────────────────────────────────────────────────────────────────┐
│                    데이터 파이프라인 흐름                        │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   1. [Excel/Google Sheets]                                      │
│      │  기획자가 데이터 작성                                     │
│      ▼                                                          │
│   2. [CSV 내보내기] (UTF-8)                                      │
│      │  Assets/DataTables/Cards.csv                             │
│      ▼                                                          │
│   3. [XML 변환] - 메뉴: Tools > Data Pipeline > Convert          │
│      │  Assets/Resources/Data/Cards.xml                         │
│      ▼                                                          │
│   4. [런타임 로드] - DataService.InitializeAsync()              │
│      │  IDataService.Get<CardData>("card_001")                  │
│      ▼                                                          │
│   5. [에셋 바인딩] - AssetRegistry                               │
│         AssetRegistry.Instance.GetSprite("card_001")            │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## 💻 사용 예제

### 1. CSV 데이터 작성 (기획자)

```csv
Id,NameKey,Cost,BaseDamage,CardType,Effects,@Notes
card_001,card_fireball,3,5,Attack,"damage,burn",기본 공격 카드
card_002,card_shield,2,0,Skill,block,기본 방어 카드
```

### 2. VContainer 설정

```csharp
public class GameLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        // DataService 설정
        var dataService = new DataService();
        dataService.RegisterDataType<CardData>("Data/Cards");
        dataService.RegisterDataType<UnitData>("Data/Units");
        dataService.RegisterDataType<SkillData>("Data/Skills");
        
        builder.RegisterInstance<IDataService>(dataService);
        
        // 초기화
        builder.RegisterBuildCallback(async container =>
        {
            var service = container.Resolve<IDataService>();
            await service.InitializeAsync();
        });
    }
}
```

### 3. 데이터 사용

```csharp
public class CardSystem
{
    private readonly IDataService _dataService;
    
    [Inject]
    public CardSystem(IDataService dataService)
    {
        _dataService = dataService;
    }
    
    public void PlayCard(string cardId)
    {
        // 데이터 가져오기
        var card = _dataService.Get<CardData>(cardId);
        if (card == null) return;
        
        // 에셋 가져오기
        var sprite = AssetRegistry.Instance.GetSprite(cardId);
        var sound = AssetRegistry.Instance.GetAudioClip(card.SoundId);
        
        Debug.Log($"Playing {card.NameKey} - Cost: {card.Cost}, Damage: {card.BaseDamage}");
    }
    
    // 필터링 예제
    public void ShowAttackCards()
    {
        var attackCards = _dataService.GetWhere<CardData>(
            card => card.CardType == CardType.Attack
        );
        
        foreach (var card in attackCards)
        {
            Debug.Log(card.NameKey);
        }
    }
}
```

---

## 📋 ID 네이밍 컨벤션 (권장)

```
{category}_{subcategory}_{number}

예시:
├── card_attack_001       # 공격 카드
├── card_skill_heal_001   # 스킬 카드 (힐)
├── card_power_buff_001   # 파워 카드 (버프)
├── unit_player_warrior   # 플레이어 유닛
├── unit_enemy_goblin_01  # 적 유닛
├── skill_active_fire_01  # 액티브 스킬
├── skill_passive_regen   # 패시브 스킬
└── item_potion_health_01 # 아이템
```

---

## 🗂️ 폴더 구조 (권장)

```
Assets/
├── DataTables/                    # CSV 원본 (Git 관리)
│   ├── Cards.csv
│   ├── Units.csv
│   └── Skills.csv
│
├── Resources/
│   ├── Data/                      # 생성된 XML (자동 생성)
│   │   ├── Cards.xml
│   │   ├── Units.xml
│   │   └── Skills.xml
│   └── AssetRegistry.asset        # 에셋 바인딩
│
├── ScriptableObjects/             # SO 에셋들 (필요시)
│   ├── Units/
│   └── Skills/
│
└── Scripts/
    └── Data/
        ├── CardData.cs            # 게임별 데이터 클래스
        └── ...
```

---

## ✅ 다음 단계 권장사항

1. **데이터 클래스 정의**: 덱빌딩 게임에 필요한 데이터 구조 확정
   - CardData (완료)
   - UnitData (완료)
   - SkillData (완료)
   - RelicData, EventData 등 추가 필요

2. **ID 체계 설계**: 전체 게임 데이터의 ID 규칙 확정

3. **AssetRegistry 에디터**: 편리한 에셋 등록 UI 구현

4. **데이터 검증 도구**: ID 중복, 참조 무결성 체크

5. **테스트 작성**: DataService 단위 테스트

---

*생성일: 2026-01-08*
