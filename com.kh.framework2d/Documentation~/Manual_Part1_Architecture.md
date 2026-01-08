# 🎓 KH Framework 2D 완전 정복 가이드

## Unity 2D 프로그래밍 마스터 클래스

> **작성자**: Unity 2D 게임 개발 교수  
> **대상**: 중급 이상 Unity 개발자  
> **목표**: 프레임워크의 모든 시스템을 이해하고 실무에 적용하기

---

# 📚 목차

1. [프레임워크 개요](#1-프레임워크-개요)
2. [아키텍처 이해하기](#2-아키텍처-이해하기)
3. [의존성 주입 (VContainer)](#3-의존성-주입-vcontainer)
4. [MVP 패턴](#4-mvp-패턴)
5. [서비스 시스템](#5-서비스-시스템)
6. [데이터 파이프라인](#6-데이터-파이프라인)
7. [이벤트 시스템](#7-이벤트-시스템)
8. [상태 머신](#8-상태-머신)
9. [오브젝트 풀링](#9-오브젝트-풀링)
10. [유틸리티](#10-유틸리티)
11. [실전 예제](#11-실전-예제)

---

# 1. 프레임워크 개요

## 1.1 이 프레임워크가 해결하는 문제

게임 개발에서 흔히 마주치는 문제들:

```
❌ 스파게티 코드: 모든 것이 서로 얽혀있음
❌ 테스트 불가: 컴포넌트들이 너무 밀접하게 연결됨
❌ 재사용 어려움: 하드코딩된 참조들
❌ 확장성 부족: 새 기능 추가가 기존 코드를 망가뜨림
```

이 프레임워크의 해결책:

```
✅ 관심사 분리 (Separation of Concerns)
✅ 의존성 주입 (Dependency Injection)
✅ 인터페이스 기반 설계
✅ 이벤트 기반 통신
```

## 1.2 기술 스택

| 기술 | 역할 | 왜 선택했나? |
|------|------|-------------|
| **VContainer** | 의존성 주입 | Zenject보다 5-10x 빠름, 가벼움 |
| **UniTask** | 비동기 처리 | 코루틴보다 성능 좋고, async/await 문법 |
| **DOTween** | 애니메이션 | 가장 널리 쓰이는 트윈 라이브러리 |
| **uGUI** | UI 시스템 | Unity 표준, TextMeshPro 지원 |

## 1.3 폴더 구조

```
KH.Framework2D/
│
├── Base/                    # MVP 기본 클래스
│   ├── BasePresenter.cs     # 프레젠터 기본 클래스
│   └── BaseView.cs          # 뷰 기본 클래스
│
├── Services/                # 글로벌 서비스들
│   ├── ServiceLocator.cs    # 서비스 위치자 패턴
│   ├── ServiceInterfaces.cs # 모든 서비스 인터페이스
│   ├── Audio/               # 오디오 관리
│   ├── Scene/               # 씬 로딩
│   ├── Save/                # 저장/로드
│   ├── Input/               # 입력 처리
│   ├── Time/                # 시간 관리
│   ├── Game/                # 게임 상태 관리
│   ├── Settings/            # 설정
│   └── Localization/        # 다국어
│
├── Data/                    # 데이터 시스템
│   ├── Pipeline/            # Excel→XML→Game 파이프라인
│   ├── CardData.cs          # 카드 데이터
│   ├── UnitData.cs          # 유닛 데이터
│   └── SkillData.cs         # 스킬 데이터
│
├── Events/                  # 이벤트 시스템
│   ├── EventChannel.cs      # ScriptableObject 이벤트
│   └── TypedEventChannels.cs
│
├── StateMachine/            # 상태 머신
│   └── StateMachine.cs
│
├── Pool/                    # 오브젝트 풀링
│   ├── ObjectPool.cs
│   ├── PoolManager.cs
│   └── PooledHandle.cs
│
├── Components2D/            # 2D 게임 컴포넌트
│   ├── Character2D.cs       # 2D 캐릭터 기본
│   ├── Camera2D.cs          # 2D 카메라
│   ├── HealthBar.cs         # 체력바
│   └── DamagePopup.cs       # 데미지 팝업
│
├── Combat/                  # 전투 시스템
│   ├── CombatFormulas.cs    # 데미지 계산
│   ├── Projectile.cs        # 투사체
│   └── StatusEffect.cs      # 상태이상
│
├── UI/                      # UI 시스템
│   ├── UIManager.cs         # UI 스택 관리
│   ├── LoadingScreenView.cs
│   └── LocalizedText.cs     # 다국어 텍스트
│
└── Utils/                   # 유틸리티
    ├── Extensions.cs        # 확장 메서드
    ├── Timer.cs             # 타이머/쿨다운
    ├── ObservableProperty.cs # 반응형 프로퍼티
    └── Spawner.cs           # 스폰 유틸리티
```

---

# 2. 아키텍처 이해하기

## 2.1 레이어 아키텍처

```
┌─────────────────────────────────────────────────────────────────┐
│                      PRESENTATION LAYER                         │
│                   (View, Presenter, UI)                         │
├─────────────────────────────────────────────────────────────────┤
│                      APPLICATION LAYER                          │
│               (Services, Managers, Controllers)                 │
├─────────────────────────────────────────────────────────────────┤
│                       DOMAIN LAYER                              │
│                (Models, Game Logic, Rules)                      │
├─────────────────────────────────────────────────────────────────┤
│                    INFRASTRUCTURE LAYER                         │
│             (Data Pipeline, Save System, Network)               │
└─────────────────────────────────────────────────────────────────┘
```

### 각 레이어의 역할

**Presentation Layer (표현 계층)**
- 사용자가 보고 상호작용하는 모든 것
- View: 화면에 데이터를 표시
- Presenter: View와 Model 사이의 중재자

**Application Layer (응용 계층)**
- 게임의 흐름을 제어
- 여러 도메인 객체들을 조율
- 서비스들이 여기에 위치

**Domain Layer (도메인 계층)**
- 게임의 핵심 규칙과 로직
- 외부 의존성 없이 순수한 C# 코드
- 가장 테스트하기 쉬움

**Infrastructure Layer (인프라 계층)**
- 외부 시스템과의 통신
- 데이터 저장/로드
- 파일 시스템 접근

## 2.2 의존성 방향

```
     Presentation
          ↓
     Application
          ↓
        Domain
          ↓
    Infrastructure
```

**핵심 규칙**: 위에서 아래로만 의존!
- View는 Presenter를 알지만, Presenter는 View 인터페이스만 앎
- Service는 Model을 알지만, Model은 Service를 모름

## 2.3 통신 방식

```
┌─────────────┐         Events          ┌─────────────┐
│  System A   │ ────────────────────▶  │  System B   │
└─────────────┘                         └─────────────┘
      │                                       │
      │ DI (의존성 주입)                       │
      ▼                                       │
┌─────────────┐                               │
│  Interface  │ ◀─────────────────────────────┘
└─────────────┘        구현체 사용
```

---

# 3. 의존성 주입 (VContainer)

## 3.1 왜 의존성 주입이 필요한가?

### ❌ 나쁜 예 (하드코딩된 의존성)

```csharp
public class PlayerController : MonoBehaviour
{
    private void Start()
    {
        // 문제: AudioManager에 강하게 결합됨
        // 테스트할 때 AudioManager 없이 테스트 불가!
        AudioManager.Instance.PlaySFX("jump");
    }
}
```

### ✅ 좋은 예 (의존성 주입)

```csharp
public class PlayerController
{
    private readonly IAudioService _audioService;
    
    // 생성자에서 인터페이스로 받음
    // 테스트할 때 Mock 객체 주입 가능!
    public PlayerController(IAudioService audioService)
    {
        _audioService = audioService;
    }
    
    public void Jump()
    {
        _audioService.PlaySFX("jump");
    }
}
```

## 3.2 VContainer 기본 사용법

### LifetimeScope 설정

```csharp
using VContainer;
using VContainer.Unity;

public class GameLifetimeScope : LifetimeScope
{
    [SerializeField] private UIManager _uiManager;
    [SerializeField] private AudioManager _audioManager;
    
    protected override void Configure(IContainerBuilder builder)
    {
        // ═══════════════════════════════════════════════
        // 1. 씬에 있는 컴포넌트 등록 (MonoBehaviour)
        // ═══════════════════════════════════════════════
        builder.RegisterComponent(_uiManager);
        builder.RegisterComponent(_audioManager);
        
        // ═══════════════════════════════════════════════
        // 2. 일반 클래스 등록 (Lifetime 지정)
        // ═══════════════════════════════════════════════
        
        // Singleton: 앱 전체에서 하나의 인스턴스
        builder.Register<GameModel>(Lifetime.Singleton);
        
        // Scoped: LifetimeScope당 하나의 인스턴스
        builder.Register<BattleService>(Lifetime.Scoped);
        
        // Transient: 요청할 때마다 새 인스턴스
        builder.Register<Enemy>(Lifetime.Transient);
        
        // ═══════════════════════════════════════════════
        // 3. 인터페이스 → 구현체 매핑
        // ═══════════════════════════════════════════════
        builder.Register<IAudioService, AudioManager>(Lifetime.Singleton);
        builder.Register<ISceneService, SceneLoader>(Lifetime.Singleton);
        builder.Register<ISaveService, SaveManager>(Lifetime.Singleton);
        
        // ═══════════════════════════════════════════════
        // 4. EntryPoint 등록 (자동 Start/Dispose 호출)
        // ═══════════════════════════════════════════════
        builder.RegisterEntryPoint<GamePresenter>();
        builder.RegisterEntryPoint<BattlePresenter>();
    }
}
```

### 의존성 받는 방법

```csharp
// ═══════════════════════════════════════════════
// 방법 1: 생성자 주입 (권장! ⭐)
// ═══════════════════════════════════════════════
public class GamePresenter
{
    private readonly GameModel _model;
    private readonly IAudioService _audio;
    
    // VContainer가 자동으로 의존성 주입
    public GamePresenter(GameModel model, IAudioService audio)
    {
        _model = model;
        _audio = audio;
    }
}

// ═══════════════════════════════════════════════
// 방법 2: [Inject] 어트리뷰트 (MonoBehaviour용)
// ═══════════════════════════════════════════════
public class PlayerView : MonoBehaviour
{
    [Inject] private IAudioService _audio;
    [Inject] private IInputService _input;
    
    private void Start()
    {
        _audio.PlaySFX("spawn");
    }
}

// ═══════════════════════════════════════════════
// 방법 3: 메서드 주입
// ═══════════════════════════════════════════════
public class EnemySpawner : MonoBehaviour
{
    private IPoolService _pool;
    
    [Inject]
    public void Construct(IPoolService pool)
    {
        _pool = pool;
    }
}
```

## 3.3 ServiceLocator (보조 수단)

VContainer로 DI가 안 되는 곳에서 사용:
- ScriptableObject 내부
- static 메서드
- 외부 라이브러리

```csharp
// 등록 (LifetimeScope에서)
builder.RegisterBuildCallback(container =>
{
    ServiceLocator.Register<IAudioService>(container.Resolve<IAudioService>());
    ServiceLocator.Register<IDataService>(container.Resolve<IDataService>());
});

// 사용 (어디서든)
public static class GameEvents
{
    public static void PlaySound(string key)
    {
        // ServiceLocator로 서비스 접근
        if (ServiceLocator.TryGet<IAudioService>(out var audio))
        {
            audio.PlaySFX(key);
        }
    }
}

// ScriptableObject에서 사용
[CreateAssetMenu]
public class CardEffect : ScriptableObject
{
    public void Execute()
    {
        // ScriptableObject는 DI 불가 → ServiceLocator 사용
        var audio = ServiceLocator.Get<IAudioService>();
        audio.PlaySFX("card_play");
    }
}
```

---

# 4. MVP 패턴

## 4.1 MVP란?

```
┌─────────────────────────────────────────────────────────────────┐
│                         MVP 패턴                                 │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│    ┌─────────┐         ┌─────────────┐         ┌─────────┐     │
│    │  Model  │ ◀────── │  Presenter  │ ──────▶ │  View   │     │
│    │ (Data)  │         │  (Logic)    │         │  (UI)   │     │
│    └─────────┘         └─────────────┘         └─────────┘     │
│         │                    ▲                      │           │
│         │                    │                      │           │
│         │    데이터 변경      │      버튼 클릭       │           │
│         └────────────────────┘◀─────────────────────┘           │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘

Model: 데이터와 비즈니스 로직
View: 화면 표시, 사용자 입력 감지
Presenter: Model과 View 사이 중재자
```

## 4.2 BaseView 상세 설명

```csharp
/// <summary>
/// 모든 UI View의 기본 클래스
/// 
/// 핵심 기능:
/// - Show/Hide (즉시)
/// - ShowAsync/HideAsync (애니메이션)
/// - CanvasGroup 기반 페이드
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public abstract class BaseView : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private float _fadeDuration = 0.2f;
    [SerializeField] private Ease _fadeEase = Ease.OutQuad;
    
    private CanvasGroup _canvasGroup;
    
    // ═══════════════════════════════════════════════
    // 상태 프로퍼티
    // ═══════════════════════════════════════════════
    public bool IsVisible => gameObject.activeSelf && _canvasGroup.alpha > 0;
    
    // ═══════════════════════════════════════════════
    // 즉시 표시/숨김
    // ═══════════════════════════════════════════════
    public virtual void Show()
    {
        gameObject.SetActive(true);
        _canvasGroup.alpha = 1f;
        SetInteractable(true);
        OnShow();  // 자식 클래스에서 오버라이드
    }
    
    public virtual void Hide()
    {
        OnHide();  // 자식 클래스에서 오버라이드
        SetInteractable(false);
        _canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
    }
    
    // ═══════════════════════════════════════════════
    // 애니메이션 표시/숨김
    // ═══════════════════════════════════════════════
    public virtual async UniTask ShowAsync()
    {
        gameObject.SetActive(true);
        _canvasGroup.alpha = 0f;
        SetInteractable(false);  // 애니메이션 중 입력 차단
        
        // DOTween으로 페이드 인
        await _canvasGroup
            .DOFade(1f, _fadeDuration)
            .SetEase(_fadeEase)
            .AsyncWaitForCompletion();
        
        SetInteractable(true);
        OnShow();
    }
    
    public virtual async UniTask HideAsync()
    {
        OnHide();
        SetInteractable(false);
        
        // DOTween으로 페이드 아웃
        await _canvasGroup
            .DOFade(0f, _fadeDuration)
            .SetEase(_fadeEase)
            .AsyncWaitForCompletion();
        
        gameObject.SetActive(false);
    }
    
    // ═══════════════════════════════════════════════
    // 자식 클래스에서 오버라이드할 훅(hook) 메서드
    // ═══════════════════════════════════════════════
    protected virtual void OnShow() { }   // 표시 후 호출
    protected virtual void OnHide() { }   // 숨김 전 호출
    
    // ═══════════════════════════════════════════════
    // 내부 헬퍼
    // ═══════════════════════════════════════════════
    protected void SetInteractable(bool value)
    {
        _canvasGroup.interactable = value;
        _canvasGroup.blocksRaycasts = value;
    }
}
```

## 4.3 BasePresenter 상세 설명

```csharp
/// <summary>
/// View 전용 Presenter (Model 없음)
/// 사용 예: 설정 화면, 단순 메뉴
/// </summary>
public abstract class BasePresenter<TView> : IStartable, IDisposable 
    where TView : BaseView
{
    protected readonly TView View;
    
    protected BasePresenter(TView view)
    {
        View = view;
    }
    
    // VContainer가 자동 호출
    public void Start() => OnBind();
    public void Dispose() => OnUnbind();
    
    // 자식 클래스에서 구현
    protected abstract void OnBind();    // 이벤트 연결
    protected abstract void OnUnbind();  // 이벤트 해제
}

/// <summary>
/// Model + View Presenter
/// 사용 예: 인벤토리, 상점, 게임 HUD
/// </summary>
public abstract class BasePresenter<TView, TModel> : BasePresenter<TView>
    where TView : BaseView
{
    protected readonly TModel Model;
    
    protected BasePresenter(TView view, TModel model) : base(view)
    {
        Model = model;
    }
}
```

## 4.4 MVP 실전 예제: 리소스 UI

### Step 1: Model (데이터)

```csharp
/// <summary>
/// 플레이어 리소스 데이터
/// ObservableProperty로 변경 감지
/// </summary>
public class PlayerResourceModel
{
    // 값이 변경되면 자동으로 구독자에게 알림
    public ObservableProperty<int> Gold { get; } = new(0);
    public ObservableProperty<int> Gem { get; } = new(0);
    public ObservableProperty<int> Energy { get; } = new(100);
    public ObservableProperty<int> MaxEnergy { get; } = new(100);
    
    public void AddGold(int amount)
    {
        Gold.Value += amount;  // 자동으로 UI 업데이트됨!
    }
    
    public bool TrySpendGold(int amount)
    {
        if (Gold.Value < amount) return false;
        Gold.Value -= amount;
        return true;
    }
    
    public void AddEnergy(int amount)
    {
        Energy.Value = Mathf.Min(Energy.Value + amount, MaxEnergy.Value);
    }
}
```

### Step 2: View (UI)

```csharp
/// <summary>
/// 리소스 표시 UI
/// 데이터를 표시하고 버튼 클릭을 알림
/// </summary>
public class ResourceView : BaseView
{
    [Header("UI References")]
    [SerializeField] private TMP_Text _goldText;
    [SerializeField] private TMP_Text _gemText;
    [SerializeField] private Slider _energySlider;
    [SerializeField] private TMP_Text _energyText;
    [SerializeField] private Button _addGoldButton;
    
    // 이벤트: Presenter에게 알림
    public event Action OnAddGoldClicked;
    
    private void Awake()
    {
        _addGoldButton.onClick.AddListener(() => OnAddGoldClicked?.Invoke());
    }
    
    // Presenter가 호출하는 메서드들
    public void SetGold(int value) => _goldText.text = value.ToShortString();
    public void SetGem(int value) => _gemText.text = value.ToShortString();
    
    public void SetEnergy(int current, int max)
    {
        _energySlider.value = (float)current / max;
        _energyText.text = $"{current}/{max}";
    }
}
```

### Step 3: Presenter (중재자)

```csharp
/// <summary>
/// Model과 View 연결
/// </summary>
public class ResourcePresenter : BasePresenter<ResourceView, PlayerResourceModel>
{
    public ResourcePresenter(ResourceView view, PlayerResourceModel model) 
        : base(view, model) { }
    
    protected override void OnBind()
    {
        // ═══════════════════════════════════════════════
        // Model → View 바인딩 (데이터 변경 시 UI 업데이트)
        // ═══════════════════════════════════════════════
        Model.Gold.Subscribe(value => View.SetGold(value));
        Model.Gem.Subscribe(value => View.SetGem(value));
        
        // 에너지는 두 값 모두 필요
        Model.Energy.Subscribe(_ => UpdateEnergyUI());
        Model.MaxEnergy.Subscribe(_ => UpdateEnergyUI());
        
        // ═══════════════════════════════════════════════
        // View → Model 바인딩 (버튼 클릭 시 데이터 변경)
        // ═══════════════════════════════════════════════
        View.OnAddGoldClicked += HandleAddGold;
    }
    
    protected override void OnUnbind()
    {
        // 메모리 누수 방지: 이벤트 해제
        Model.Gold.ClearSubscriptions();
        Model.Gem.ClearSubscriptions();
        Model.Energy.ClearSubscriptions();
        Model.MaxEnergy.ClearSubscriptions();
        
        View.OnAddGoldClicked -= HandleAddGold;
    }
    
    private void HandleAddGold()
    {
        Model.AddGold(100);
    }
    
    private void UpdateEnergyUI()
    {
        View.SetEnergy(Model.Energy.Value, Model.MaxEnergy.Value);
    }
}
```

### Step 4: VContainer 등록

```csharp
public class GameLifetimeScope : LifetimeScope
{
    [SerializeField] private ResourceView _resourceView;
    
    protected override void Configure(IContainerBuilder builder)
    {
        // Model: Singleton (게임 전체에서 하나)
        builder.Register<PlayerResourceModel>(Lifetime.Singleton);
        
        // View: 씬에 있는 컴포넌트
        builder.RegisterComponent(_resourceView);
        
        // Presenter: EntryPoint로 등록 (자동 Start/Dispose)
        builder.RegisterEntryPoint<ResourcePresenter>();
    }
}
```

---

*다음 파트에서 계속...*
