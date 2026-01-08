# 📋 KH Framework 2D - Quick Reference

## 🚀 빠른 시작

### 1. 의존성 설치 (Package Manager)
```
VContainer: https://github.com/hadashiA/VContainer.git?path=VContainer/Assets/VContainer
UniTask: https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask
DOTween: Asset Store에서 설치
```

### 2. LifetimeScope 설정
```csharp
public class GameScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        // 서비스
        builder.Register<IDataService, DataService>(Lifetime.Singleton);
        
        // 모델
        builder.Register<GameModel>(Lifetime.Singleton);
        
        // 뷰 (씬)
        builder.RegisterComponent(_myView);
        
        // 프레젠터
        builder.RegisterEntryPoint<MyPresenter>();
    }
}
```

---

## 📦 서비스 인터페이스

### IAudioService
```csharp
_audio.PlaySFX("key");
_audio.PlaySFXWithPitch("key", 0.9f, 1.1f);
_audio.PlayBGM("key", loop: true);
await _audio.CrossfadeBGM("newBGM", 1f);
_audio.SetMasterVolume(0.8f);
```

### ISceneService
```csharp
await _scene.LoadSceneAsync("GameScene");
await _scene.LoadSceneAdditiveAsync("UI");
await _scene.ReloadCurrentSceneAsync();
_scene.OnLoadProgress += p => Debug.Log($"{p:P0}");
```

### ISaveService
```csharp
_save.Save("key", value);          // PlayerPrefs
var v = _save.Load("key", default);

_save.SaveToFile("save1", data);   // JSON 파일
var d = _save.LoadFromFile<T>("save1");
```

### ITimeService
```csharp
_time.Pause();
_time.Resume();
_time.SlowMotion(0.3f);
_time.ResetTimeScale();
```

### IDataService
```csharp
await _data.InitializeAsync();
var card = _data.Get<CardData>("card_001");
var all = _data.GetAll<CardData>();
var filtered = _data.GetWhere<CardData>(c => c.Cost <= 2);
```

---

## 🎨 MVP 패턴

### Model
```csharp
public class MyModel
{
    public ObservableProperty<int> Value { get; } = new(0);
}
```

### View
```csharp
public class MyView : BaseView
{
    public event Action OnButtonClick;
    public void SetValue(int v) => _text.text = v.ToString();
    
    protected override void OnShow() { }
    protected override void OnHide() { }
}
```

### Presenter
```csharp
public class MyPresenter : BasePresenter<MyView, MyModel>
{
    public MyPresenter(MyView view, MyModel model) : base(view, model) { }
    
    protected override void OnBind()
    {
        Model.Value.Subscribe(v => View.SetValue(v));
        View.OnButtonClick += HandleClick;
    }
    
    protected override void OnUnbind()
    {
        Model.Value.ClearSubscriptions();
        View.OnButtonClick -= HandleClick;
    }
}
```

---

## 🔄 상태 머신

```csharp
// 상태 정의
public class IdleState : State<Player>
{
    public override void Enter() { }
    public override void Update() { }
    public override void Exit() { }
}

// 사용
_sm = new StateMachine<Player>(this);
_sm.AddState<IdleState>();
_sm.AddState<MoveState>();
_sm.ChangeState<IdleState>();

// Update에서
_sm.Update();
```

---

## 🎯 오브젝트 풀

```csharp
// 생성
var pool = new ObjectPool<Bullet>(prefab, parent, 10, 100);
pool.WarmUp();

// 사용
Bullet b = pool.Spawn(pos, rot);
pool.Despawn(b);
pool.DespawnDelayed(b, 1f);

// PoolManager
_poolManager.Spawn("Bullet", pos, rot);
_poolManager.Despawn(obj);  // PooledHandle 자동 처리
```

---

## 📡 이벤트 채널

```csharp
// 발행
[SerializeField] VoidEventChannel _onGameStart;
_onGameStart.Raise();

// 구독
void OnEnable() => _onGameStart.Subscribe(HandleStart);
void OnDisable() => _onGameStart.Unsubscribe(HandleStart);
```

---

## 🛠 유틸리티

### ObservableProperty
```csharp
var hp = new ObservableProperty<int>(100);
hp.Subscribe(v => UpdateUI(v), invokeImmediately: true);
hp.Value = 80;  // 자동 알림
```

### Timer
```csharp
var timer = new Timer(5f);
timer.OnComplete += () => Debug.Log("Done!");
timer.Start();

// 간단히
await Timer.DelayAsync(2f);
```

### Cooldown
```csharp
var cd = new Cooldown(5f);
if (cd.TryUse()) { /* 스킬 사용 */ }
float progress = cd.Progress;  // 0-1
```

### Extensions
```csharp
// Transform
transform.SetX(10f);
transform.LookAt2D(target);
transform.DestroyChildren();

// Vector
var v = pos.WithY(0f);
var angle = dir.ToAngle();
var rotated = dir.Rotate(45f);

// Collection
var item = list.GetRandom();
list.Shuffle();
var best = list.MaxBy(x => x.Score);

// Component
var rb = obj.GetOrAddComponent<Rigidbody2D>();
```

---

## 📊 데이터 파이프라인

### CSV 형식
```csv
Id,Name,Cost,Type
card_001,Fireball,3,Attack
card_002,Shield,2,Skill
```

### 데이터 클래스
```csharp
[Serializable]
public class CardData : IGameData
{
    public string Id { get; set; }
    public string Name { get; set; }
    public int Cost { get; set; }
    public CardType Type { get; set; }
}
```

### 등록 및 사용
```csharp
dataService.RegisterDataType<CardData>("Data/Cards");
await dataService.InitializeAsync();
var card = dataService.Get<CardData>("card_001");
```

### AssetRegistry
```csharp
var sprite = AssetRegistry.Instance.GetSprite("card_001");
var prefab = AssetRegistry.Instance.GetPrefab("unit_001");
```

---

## 🎮 Character2D

```csharp
// 방향
_char.SetFacing(true);
_char.FaceTarget(enemy.transform);
_char.Flip();

// 애니메이션
_char.PlayAnimation("Attack");
await _char.PlayAnimationAsync("Cast");
_char.SetTrigger("Jump");

// 효과
await _char.FlashAsync();
_char.Shake(0.2f, 0.1f);
_char.PunchScale();
await _char.DeathEffectAsync();
```

---

## 📁 폴더 구조 (권장)

```
Assets/
├── _Project/
│   ├── Scripts/
│   │   ├── Application/      # LifetimeScope
│   │   ├── Domain/           # Models, Services
│   │   ├── Presentation/     # Views, Presenters
│   │   └── Infrastructure/   # Data classes
│   ├── DataTables/           # CSV 원본
│   └── Resources/Data/       # 생성된 XML
└── Packages/
    └── com.kh.framework2d/   # 프레임워크
```

---

*상세 내용은 Manual_Part1~4.md 참조*
