# 🎓 KH Framework 2D 완전 정복 가이드 (Part 4)

# 9. 오브젝트 풀링

## 9.1 왜 오브젝트 풀링이 필요한가?

```
❌ Instantiate/Destroy의 문제:
- 메모리 할당/해제 → GC 발생 → 프레임 드랍
- 총알, 이펙트 등 빈번한 생성/소멸 시 심각

✅ 오브젝트 풀의 해결책:
- 미리 생성해두고 재사용
- 메모리 할당 최소화
- 일관된 성능

성능 비교 (1000개 오브젝트):
┌─────────────────┬────────────┬────────────┐
│                 │  일반 생성  │  풀링 사용  │
├─────────────────┼────────────┼────────────┤
│ 생성 시간       │  ~50ms     │  ~2ms      │
│ GC Alloc        │  ~2MB      │  ~0MB      │
│ 프레임 드랍     │  심각       │  없음      │
└─────────────────┴────────────┴────────────┘
```

## 9.2 ObjectPool 사용법

### 기본 사용

```csharp
public class BulletSpawner : MonoBehaviour
{
    [SerializeField] private Bullet _bulletPrefab;
    
    private ObjectPool<Bullet> _bulletPool;
    
    private void Awake()
    {
        // 풀 생성
        _bulletPool = new ObjectPool<Bullet>(
            prefab: _bulletPrefab,
            parent: transform,        // 비활성 오브젝트 보관 위치
            defaultCapacity: 20,      // 초기 생성 개수
            maxSize: 100              // 최대 개수
        );
        
        // 미리 생성 (선택사항)
        _bulletPool.WarmUp();
    }
    
    public void Fire(Vector3 position, Vector3 direction)
    {
        // 풀에서 가져오기
        Bullet bullet = _bulletPool.Spawn(position, Quaternion.identity);
        bullet.Initialize(direction);
    }
    
    public void ReturnBullet(Bullet bullet)
    {
        // 풀에 반환
        _bulletPool.Despawn(bullet);
    }
    
    // 지연 반환
    public void ReturnBulletDelayed(Bullet bullet, float delay)
    {
        _bulletPool.DespawnDelayed(bullet, delay).Forget();
    }
}
```

### IPoolable 인터페이스

```csharp
/// <summary>
/// 풀링 가능 오브젝트가 구현할 인터페이스
/// </summary>
public interface IPoolable
{
    void OnSpawn();   // 풀에서 꺼낼 때
    void OnDespawn(); // 풀에 반환할 때
}

// 구현 예제
public class Bullet : MonoBehaviour, IPoolable
{
    private Vector3 _direction;
    private float _speed = 10f;
    private TrailRenderer _trail;
    
    private void Awake()
    {
        _trail = GetComponent<TrailRenderer>();
    }
    
    public void Initialize(Vector3 direction)
    {
        _direction = direction.normalized;
    }
    
    // ═══════════════════════════════════════════════
    // IPoolable 구현
    // ═══════════════════════════════════════════════
    public void OnSpawn()
    {
        // 풀에서 꺼낼 때 초기화
        _trail?.Clear();  // 트레일 초기화
    }
    
    public void OnDespawn()
    {
        // 풀에 반환할 때 정리
        _direction = Vector3.zero;
    }
    
    private void Update()
    {
        transform.position += _direction * _speed * Time.deltaTime;
    }
}
```

### PooledHandle로 자기 자신 반환

```csharp
/// <summary>
/// 풀 오브젝트가 스스로 반환할 수 있게 해주는 컴포넌트
/// ObjectPool이 자동으로 추가함
/// </summary>
public class Bullet : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        // 충돌 처리
        if (other.CompareTag("Enemy"))
        {
            other.GetComponent<Health>()?.TakeDamage(10);
            
            // 스스로 풀에 반환!
            // 풀 참조 없이도 가능
            if (TryGetComponent<PooledHandle>(out var handle))
            {
                handle.TryReturnToPool();
            }
        }
    }
}
```

## 9.3 PoolManager로 중앙 관리

### 설정

```csharp
// 1. PoolSettings 에셋 생성
// Create > Pool > Pool Settings

// 2. Inspector에서 설정
// pools:
//   - key: "Bullet"
//     prefab: BulletPrefab
//     initialSize: 20
//     maxSize: 100
//   - key: "Effect_Hit"
//     prefab: HitEffectPrefab
//     initialSize: 10
//     maxSize: 50
```

### 사용

```csharp
public class GameManager : MonoBehaviour
{
    [SerializeField] private PoolManager _poolManager;
    
    private void Start()
    {
        // 런타임에 풀 추가도 가능
        _poolManager.CreatePool("Enemy", enemyPrefab, initialSize: 10, maxSize: 30);
    }
    
    public void SpawnBullet(Vector3 pos, Quaternion rot)
    {
        // 키로 스폰
        GameObject bullet = _poolManager.Spawn("Bullet", pos, rot);
    }
    
    public void SpawnEnemy(Vector3 pos)
    {
        // 컴포넌트로 직접 받기
        Enemy enemy = _poolManager.Spawn<Enemy>("Enemy", pos, Quaternion.identity);
        enemy.Initialize();
    }
    
    public void RemoveEnemy(GameObject enemyObj)
    {
        // 반환 (키 필요)
        _poolManager.Despawn("Enemy", enemyObj);
        
        // 또는 PooledHandle 사용 (키 불필요)
        _poolManager.Despawn(enemyObj);
    }
    
    public void PlayHitEffect(Vector3 pos)
    {
        // 이펙트 스폰 후 자동 반환
        GameObject effect = _poolManager.Spawn("Effect_Hit", pos, Quaternion.identity);
        _poolManager.DespawnDelayed("Effect_Hit", effect, 1f);
    }
}
```

---

# 10. 유틸리티

## 10.1 ObservableProperty (반응형 프로퍼티)

### 개념

```
일반 프로퍼티:
int _health = 100;
_health = 80;  // UI는 모름, 수동 업데이트 필요

ObservableProperty:
ObservableProperty<int> Health = new(100);
Health.Value = 80;  // 자동으로 구독자에게 알림!
```

### 사용법

```csharp
public class PlayerModel
{
    // 반응형 프로퍼티 선언
    public ObservableProperty<int> Health { get; } = new(100);
    public ObservableProperty<int> Gold { get; } = new(0);
    public ObservableProperty<bool> IsDead { get; } = new(false);
    
    public void TakeDamage(int amount)
    {
        Health.Value -= amount;  // 자동 UI 업데이트!
        
        if (Health.Value <= 0)
        {
            IsDead.Value = true;
        }
    }
}

public class PlayerPresenter
{
    private readonly PlayerModel _model;
    private readonly PlayerView _view;
    
    public PlayerPresenter(PlayerModel model, PlayerView view)
    {
        _model = model;
        _view = view;
        
        // 구독: 값 변경 시 자동 호출
        _model.Health.Subscribe(
            value => _view.SetHealth(value),
            invokeImmediately: true  // 구독 즉시 현재 값으로 호출
        );
        
        _model.Gold.Subscribe(value => _view.SetGold(value));
        
        _model.IsDead.Subscribe(isDead =>
        {
            if (isDead) _view.ShowGameOver();
        });
    }
    
    public void Dispose()
    {
        // 메모리 누수 방지
        _model.Health.ClearSubscriptions();
        _model.Gold.ClearSubscriptions();
        _model.IsDead.ClearSubscriptions();
    }
}
```

### 고급 기능

```csharp
var score = new ObservableProperty<int>(0);

// 알림 없이 값 설정 (초기화용)
score.SetSilently(100);

// 강제 알림 (같은 값이어도 알림)
score.NotifySubscribers();

// 암시적 변환 (편의 기능)
int currentScore = score;  // score.Value와 동일

// 문자열 변환
Debug.Log(score.ToString());  // "100"
```

## 10.2 Timer & Cooldown

### Timer (일반 타이머)

```csharp
public class BombController : MonoBehaviour
{
    private Timer _explosionTimer;
    
    private void Start()
    {
        // 3초 타이머 생성
        _explosionTimer = new Timer(3f);
        
        // 이벤트 연결
        _explosionTimer.OnTick += remaining => 
            Debug.Log($"폭발까지: {remaining:F1}초");
        
        _explosionTimer.OnComplete += Explode;
        
        // 시작
        _explosionTimer.Start();
    }
    
    public void DefuseBomb()
    {
        _explosionTimer.Stop();
    }
    
    public void AddTime()
    {
        _explosionTimer.AddTime(5f);  // 5초 추가
    }
    
    private void Explode()
    {
        Debug.Log("BOOM!");
    }
}

// 정적 메서드로 간단히 사용
private async void DelayedAction()
{
    await Timer.DelayAsync(2f);  // 2초 대기
    Debug.Log("2초 후 실행!");
}
```

### Cooldown (스킬 쿨다운)

```csharp
public class SkillSystem : MonoBehaviour
{
    private Cooldown _fireballCooldown;
    private Cooldown _shieldCooldown;
    
    private void Awake()
    {
        _fireballCooldown = new Cooldown(5f);  // 5초 쿨타임
        _shieldCooldown = new Cooldown(10f);   // 10초 쿨타임
        
        // 쿨타임 완료 알림
        _fireballCooldown.OnReady += () => Debug.Log("파이어볼 준비!");
    }
    
    public void CastFireball()
    {
        // TryUse: 쿨타임이면 false, 사용 가능하면 true + 쿨타임 시작
        if (_fireballCooldown.TryUse())
        {
            SpawnFireball();
        }
        else
        {
            Debug.Log($"쿨타임 중! {_fireballCooldown.RemainingTime:F1}초 남음");
        }
    }
    
    // UI 업데이트용
    private void Update()
    {
        // 스킬 아이콘 쿨다운 표시
        _skillIcon.fillAmount = _fireballCooldown.Progress;  // 0~1
    }
    
    // 쿨타임 감소 버프
    public void ApplyCooldownReduction(float amount)
    {
        _fireballCooldown.ReduceCooldown(amount);
    }
}
```

### RepeatingTimer (반복 타이머)

```csharp
public class PoisonEffect : MonoBehaviour
{
    private RepeatingTimer _tickTimer;
    private int _damagePerTick = 5;
    private int _ticksRemaining = 5;
    
    private void Start()
    {
        // 0.5초마다 틱
        _tickTimer = new RepeatingTimer(0.5f);
        _tickTimer.OnTick += DealPoisonDamage;
        _tickTimer.Start();
    }
    
    private void DealPoisonDamage()
    {
        GetComponent<Health>().TakeDamage(_damagePerTick);
        _ticksRemaining--;
        
        if (_ticksRemaining <= 0)
        {
            _tickTimer.Stop();
            Destroy(this);
        }
    }
}
```

## 10.3 Extensions (확장 메서드)

### Transform 확장

```csharp
// 개별 축 설정
transform.SetX(10f);
transform.SetY(5f);
transform.SetLocalX(0f);

// 자식 전부 삭제
transform.DestroyChildren();

// 리셋
transform.Reset();  // position, rotation, scale 초기화

// 2D 회전 (Z축)
transform.LookAt2D(targetPosition);
transform.LookAt2D(targetPosition, offsetAngle: -90f);

// 방향과 거리
Vector2 direction = transform.DirectionTo2D(target);
float distance = transform.DistanceTo2D(target);
```

### Vector 확장

```csharp
// 개별 값 변경 (새 벡터 반환)
Vector3 newPos = position.WithX(10f);
Vector3 groundPos = position.WithY(0f);

// 변환
Vector2 v2 = v3.ToVector2();      // XY만
Vector3 v3 = v2.ToVector3(z: 5f); // Z 추가

// 회전
Vector2 rotated = direction.Rotate(45f);  // 45도 회전
Vector2 perpendicular = direction.Perpendicular();  // 수직 벡터

// 각도 변환
float angle = direction.ToAngle();  // Vector2 → 각도

// 랜덤 위치
Vector2 randomInCircle = center.RandomInRadius(radius: 5f);
Vector2 randomOnCircle = center.RandomOnRadius(radius: 5f);
```

### Color 확장

```csharp
// 알파 변경
Color transparent = color.WithAlpha(0.5f);

// Hex 변환
string hex = color.ToHex();  // "FF0000FF"

// 색상 반전
Color inverted = color.Invert();
```

### Collection 확장

```csharp
// 랜덤 요소
Enemy randomEnemy = enemies.GetRandom();

// Fisher-Yates 셔플
deck.Shuffle();

// 최소/최대 요소
Enemy weakest = enemies.MinBy(e => e.Health);
Enemy strongest = enemies.MaxBy(e => e.Health);

// 가중치 랜덤
var lootTable = new List<(Item item, float weight)>
{
    (commonItem, 70f),
    (rareItem, 25f),
    (legendaryItem, 5f)
};
Item drop = lootTable.GetWeightedRandom(x => x.weight).item;
```

### Component 확장

```csharp
// GetOrAdd
var rb = gameObject.GetOrAddComponent<Rigidbody2D>();

// HasComponent
if (gameObject.HasComponent<Enemy>())
{
    // ...
}
```

### 숫자 포맷팅

```csharp
// 큰 숫자 축약
1234567.ToShortString()  // "1.2M"
12345.ToShortString()    // "12.3K"

// 시간 포맷
125.5f.ToTimeString()      // "02:05"
3725.5f.ToLongTimeString() // "01:02:05"
```

## 10.4 Character2D (2D 캐릭터 컴포넌트)

```csharp
public class Enemy : MonoBehaviour
{
    [SerializeField] private Character2D _character;
    
    private void Start()
    {
        // 스프라이트 변경
        _character.SetSprite(angrySprite);
        _character.SetColor(Color.red);
        _character.SetAlpha(0.5f);
        
        // 방향 전환
        _character.SetFacing(faceRight: false);
        _character.FaceTarget(player.transform);
        _character.Flip();
        
        // 애니메이션
        _character.PlayAnimation("Walk");
        _character.SetTrigger("Attack");
        _character.SetBool("IsMoving", true);
        _character.SetFloat("Speed", 1.5f);
        
        // 비동기 애니메이션
        await _character.PlayAnimationAsync("Attack");
        Debug.Log("공격 완료!");
    }
    
    public async void TakeDamage()
    {
        // 피격 효과
        await _character.FlashAsync();  // 기본 빨간색 깜빡임
        await _character.FlashAsync(Color.yellow, duration: 0.1f, count: 3);
        
        // 흔들림
        _character.Shake(duration: 0.2f, strength: 0.1f);
        
        // 펀치 스케일
        _character.PunchScale(strength: 0.2f);
    }
    
    public async void Die()
    {
        // 사망 연출 (플래시 + 페이드아웃 + 비활성화/풀 반환)
        await _character.DeathEffectAsync();
    }
}
```

---

# 11. 실전 예제: 덱빌딩 카드 게임

## 11.1 프로젝트 구조

```
Assets/
├── _Project/
│   ├── Scripts/
│   │   ├── Application/         # VContainer 설정
│   │   │   └── GameLifetimeScope.cs
│   │   ├── Domain/              # 게임 로직
│   │   │   ├── Models/
│   │   │   │   ├── CardModel.cs
│   │   │   │   ├── DeckModel.cs
│   │   │   │   └── BattleModel.cs
│   │   │   └── Services/
│   │   │       ├── CardService.cs
│   │   │       └── BattleService.cs
│   │   ├── Presentation/        # UI
│   │   │   ├── Views/
│   │   │   │   ├── HandView.cs
│   │   │   │   └── BattleView.cs
│   │   │   └── Presenters/
│   │   │       ├── HandPresenter.cs
│   │   │       └── BattlePresenter.cs
│   │   └── Infrastructure/      # 데이터
│   │       └── Data/
│   │           └── CardData.cs
│   ├── DataTables/              # CSV 원본
│   │   └── Cards.csv
│   └── Resources/
│       └── Data/                # 생성된 XML
│           └── Cards.xml
```

## 11.2 핵심 코드

### CardData (데이터 클래스)

```csharp
[Serializable]
public class CardData : IGameData
{
    public string Id { get; set; }
    public string NameKey { get; set; }
    public CardType CardType { get; set; }
    public int Cost { get; set; }
    public int Damage { get; set; }
    public int Block { get; set; }
    public int Draw { get; set; }
    public string EffectIds { get; set; }
}
```

### DeckModel (덱 모델)

```csharp
public class DeckModel
{
    private readonly List<string> _drawPile = new();
    private readonly List<string> _hand = new();
    private readonly List<string> _discardPile = new();
    
    public ObservableProperty<int> DrawPileCount { get; } = new(0);
    public ObservableProperty<int> DiscardPileCount { get; } = new(0);
    
    public event Action<string> OnCardDrawn;
    public event Action<string> OnCardDiscarded;
    
    public void ShuffleDrawPile()
    {
        _drawPile.Shuffle();
    }
    
    public bool TryDrawCard(out string cardId)
    {
        if (_drawPile.Count == 0)
        {
            ReshuffleDiscard();
        }
        
        if (_drawPile.Count > 0)
        {
            cardId = _drawPile[0];
            _drawPile.RemoveAt(0);
            _hand.Add(cardId);
            
            DrawPileCount.Value = _drawPile.Count;
            OnCardDrawn?.Invoke(cardId);
            return true;
        }
        
        cardId = null;
        return false;
    }
    
    public void DiscardCard(string cardId)
    {
        if (_hand.Remove(cardId))
        {
            _discardPile.Add(cardId);
            DiscardPileCount.Value = _discardPile.Count;
            OnCardDiscarded?.Invoke(cardId);
        }
    }
    
    private void ReshuffleDiscard()
    {
        _drawPile.AddRange(_discardPile);
        _discardPile.Clear();
        _drawPile.Shuffle();
        
        DrawPileCount.Value = _drawPile.Count;
        DiscardPileCount.Value = 0;
    }
}
```

### HandPresenter (손패 프레젠터)

```csharp
public class HandPresenter : BasePresenter<HandView, DeckModel>
{
    private readonly IDataService _dataService;
    private readonly BattleService _battleService;
    
    public HandPresenter(
        HandView view, 
        DeckModel model,
        IDataService dataService,
        BattleService battleService) 
        : base(view, model)
    {
        _dataService = dataService;
        _battleService = battleService;
    }
    
    protected override void OnBind()
    {
        // 카드 드로우 시 UI에 추가
        Model.OnCardDrawn += HandleCardDrawn;
        Model.OnCardDiscarded += HandleCardDiscarded;
        
        // 카드 클릭 이벤트
        View.OnCardClicked += HandleCardClicked;
        
        // 덱 카운트 업데이트
        Model.DrawPileCount.Subscribe(View.SetDrawPileCount);
        Model.DiscardPileCount.Subscribe(View.SetDiscardCount);
    }
    
    protected override void OnUnbind()
    {
        Model.OnCardDrawn -= HandleCardDrawn;
        Model.OnCardDiscarded -= HandleCardDiscarded;
        View.OnCardClicked -= HandleCardClicked;
        
        Model.DrawPileCount.ClearSubscriptions();
        Model.DiscardPileCount.ClearSubscriptions();
    }
    
    private void HandleCardDrawn(string cardId)
    {
        var cardData = _dataService.Get<CardData>(cardId);
        var sprite = AssetRegistry.Instance.GetSprite(cardId);
        
        View.AddCardToHand(cardId, cardData.NameKey, cardData.Cost, sprite);
    }
    
    private void HandleCardDiscarded(string cardId)
    {
        View.RemoveCardFromHand(cardId);
    }
    
    private void HandleCardClicked(string cardId)
    {
        var cardData = _dataService.Get<CardData>(cardId);
        
        // 카드 사용 가능 여부 체크
        if (_battleService.CanPlayCard(cardData))
        {
            _battleService.PlayCard(cardData);
            Model.DiscardCard(cardId);
        }
        else
        {
            View.ShowNotEnoughEnergy();
        }
    }
}
```

### GameLifetimeScope (VContainer 설정)

```csharp
public class GameLifetimeScope : LifetimeScope
{
    [Header("Views")]
    [SerializeField] private HandView _handView;
    [SerializeField] private BattleView _battleView;
    
    protected override void Configure(IContainerBuilder builder)
    {
        // 데이터 서비스
        var dataService = new DataService();
        dataService.RegisterDataType<CardData>("Data/Cards");
        builder.RegisterInstance<IDataService>(dataService);
        
        // 모델 (Singleton)
        builder.Register<DeckModel>(Lifetime.Singleton);
        builder.Register<BattleModel>(Lifetime.Singleton);
        
        // 서비스
        builder.Register<BattleService>(Lifetime.Singleton);
        
        // 뷰 (씬 컴포넌트)
        builder.RegisterComponent(_handView);
        builder.RegisterComponent(_battleView);
        
        // 프레젠터 (EntryPoint)
        builder.RegisterEntryPoint<HandPresenter>();
        builder.RegisterEntryPoint<BattlePresenter>();
        
        // 초기화
        builder.RegisterBuildCallback(async container =>
        {
            await container.Resolve<IDataService>().InitializeAsync();
        });
    }
}
```

---

# 🎯 마무리

## 학습 로드맵

```
1주차: 기본 개념
├── VContainer 이해 (의존성 주입)
├── MVP 패턴 실습
└── 간단한 UI 만들기

2주차: 서비스 활용
├── AudioManager로 사운드 관리
├── SceneLoader로 씬 전환
├── SaveManager로 저장/로드
└── 설정 화면 만들기

3주차: 데이터 파이프라인
├── CSV 데이터 작성
├── DataService 설정
├── AssetRegistry 연동
└── 카드 데이터 로드

4주차: 게임 시스템
├── StateMachine으로 캐릭터 AI
├── ObjectPool로 총알/이펙트
├── EventChannel로 시스템 연결
└── 미니 게임 완성
```

## 핵심 원칙 요약

| 원칙 | 설명 |
|------|------|
| **인터페이스 사용** | 구현체가 아닌 인터페이스에 의존 |
| **DI 활용** | new 대신 생성자 주입 |
| **이벤트 디커플링** | 직접 참조 대신 이벤트 채널 |
| **데이터 분리** | 로직과 데이터는 별도로 |
| **풀링 적용** | 자주 생성/삭제되는 건 풀링 |

---

*이 프레임워크와 함께 멋진 게임을 만들어 보세요! 🎮*
