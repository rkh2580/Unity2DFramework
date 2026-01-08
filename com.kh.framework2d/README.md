# KH Framework 2D - Improved v2

> **버전**: 2.1.1  
> **업데이트**: 2025-01-09  
> **호환성**: Unity 6.x, VContainer, UniTask

---

## 📦 패키지 구조

```
KH_Framework2D_Improved_v2/
├── Editor/
│   └── DataPipeline/
│       └── ExcelToXmlConverter.cs    # [NEW] Excel → XML 변환기
│
└── Runtime/
    ├── Core/
    │   └── Define.cs                 # [NEW] 중앙 열거형 정의
    │
    ├── Data/
    │   ├── SampleDataClasses.cs      # [NEW] 샘플 데이터 클래스
    │   └── Pipeline/
    │       ├── DataService.cs        # 타입화된 데이터 로더
    │       └── XmlDataParser.cs      # [NEW] XML 파서
    │
    ├── Pool/
    │   └── PoolManager.cs            # [FIXED] warmup 상태 관리
    │
    └── Services/
        ├── ServiceLocator.cs         # Strict Mode 지원
        ├── Input/
        │   └── InputManager.cs       # [FIXED] InputMode 전환
        └── Save/
            └── SaveManager.cs        # AES-256 암호화
```

---

## 🔧 v2.1.1 변경사항

### 버그 수정

#### InputManager - SetInputMode 복구 버그 수정
```csharp
// ❌ 이전: Disabled → Gameplay 전환 시 입력 안 됨
inputManager.SetInputMode(Define.InputMode.Disabled);
inputManager.SetInputMode(Define.InputMode.Gameplay); // 입력 여전히 비활성화!

// ✅ 수정: 자동으로 EnableInput() 호출
inputManager.SetInputMode(Define.InputMode.Gameplay); // 정상 작동
```

#### PoolManager - IsWarmedUp 무한 대기 수정
```csharp
// ❌ 이전: warmup 비활성화 시 영원히 false
await UniTask.WaitUntil(() => poolManager.IsWarmedUp); // 무한 대기!

// ✅ 수정: warmup 없어도 즉시 true
// _warmUpOnStart = false 여도 IsWarmedUp = true
```

### 신규 기능

#### 1. Define.cs - 중앙 열거형 관리
```csharp
// 모든 열거형을 한 곳에서 관리
Define.InputMode.Gameplay
Define.Scene.Battle
Define.Layer.Enemy
Define.Tag.Player
```

#### 2. 데이터 파이프라인
```csharp
// Excel/CSV → XML → 게임 데이터
// 1. Excel 파일을 Assets/Data/Excel/ 에 배치
// 2. Unity 메뉴: Tools > Data Pipeline > Convert All Excel to XML
// 3. DataService에서 로드

var dataService = new DataService()
    .WithData<CardData>("Data/Cards")
    .WithData<UnitData>("Data/Units");

await dataService.InitializeAsync();
var card = dataService.Get<CardData>("card_001");
```

---

## 🚀 적용 방법

### 방법 1: 전체 교체

```bash
# 기존 프레임워크 백업
cp -r com.kh.framework2d/Runtime com.kh.framework2d/Runtime_backup

# 새 파일 복사
cp -r KH_Framework2D_Improved_v2/Runtime/* com.kh.framework2d/Runtime/
cp -r KH_Framework2D_Improved_v2/Editor/* com.kh.framework2d/Editor/
```

### 방법 2: 개별 파일 교체

| 파일 | 적용 경로 |
|------|-----------|
| `InputManager.cs` | `Runtime/Services/Input/` |
| `PoolManager.cs` | `Runtime/Pool/` |
| `Define.cs` | `Runtime/Core/` (신규) |
| `XmlDataParser.cs` | `Runtime/Data/Pipeline/` (신규) |
| `SampleDataClasses.cs` | `Runtime/Data/` (신규) |
| `ExcelToXmlConverter.cs` | `Editor/DataPipeline/` (신규) |

---

## ⚠️ Breaking Changes

### InputManager

UI 시스템에서 InputMode를 관리해야 합니다:

```csharp
// UIManager.cs에 추가
public T ShowPopupUI<T>() where T : UI_Popup
{
    // ... 기존 코드 ...
    
    // 입력 모드 전환
    if (ServiceLocator.TryGet<IInputService>(out var input))
    {
        (input as InputManager)?.SetInputMode(Define.InputMode.UI);
    }
    
    return popup;
}

public void CloseAllPopupUI()
{
    // ... 기존 코드 ...
    
    if (_popupStack.Count == 0)
    {
        if (ServiceLocator.TryGet<IInputService>(out var input))
        {
            (input as InputManager)?.SetInputMode(Define.InputMode.Gameplay);
        }
    }
}
```

---

## 🧪 테스트 체크리스트

- [ ] 게임 시작 → 데이터 로드 정상
- [ ] Escape 키: 게임 중 = Pause, UI 열림 = Cancel
- [ ] 팝업 열기/닫기 후 입력 정상 작동
- [ ] 오브젝트 풀링 Spawn/Despawn 정상
- [ ] 저장/불러오기 정상
- [ ] Excel → XML 변환 정상

---

## 📚 사용 예시

### 데이터 파이프라인

```csharp
// 1. 데이터 클래스 정의
public class CardData : IGameData
{
    public string Id { get; set; }
    public string Name { get; set; }
    public int Cost { get; set; }
    public CardType Type { get; set; }
}

// 2. DataService 설정 (Installer에서)
var dataService = new DataService()
    .WithData<CardData>("Data/Cards")
    .WithData<UnitData>("Data/Units");

builder.RegisterInstance<IDataService>(dataService);

// 3. 사용
[Inject] private IDataService _data;

void Start()
{
    var card = _data.Get<CardData>("card_001");
    var allCards = _data.GetAll<CardData>();
    var attackCards = _data.GetWhere<CardData>(c => c.Type == CardType.Attack);
}
```

### InputMode 관리

```csharp
// 게임플레이 중
inputManager.SetInputMode(Define.InputMode.Gameplay);
// Escape = OnPause 이벤트

// UI 열림
inputManager.SetInputMode(Define.InputMode.UI);
// Escape = OnCancel 이벤트

// 컷씬 중
inputManager.SetInputMode(Define.InputMode.Cinematic);
// 대부분의 입력 무시

// 로딩 중
inputManager.SetInputMode(Define.InputMode.Disabled);
// 모든 입력 비활성화
```

---

## 📝 라이선스

MIT License - 자유롭게 사용 가능

---

## 문의

문제 발생 시 이슈를 등록하거나 `Runtime_backup/`으로 복원하세요.
