# 🎓 KH Framework 2D 완전 정복 가이드 (Part 3)

# 6. 데이터 파이프라인

## 6.1 왜 데이터 파이프라인이 필요한가?

### ❌ 문제: ScriptableObject만 사용할 때

```
문제점:
1. 기획자가 Unity 에디터 직접 사용해야 함
2. 대량 데이터 수정이 어려움 (100개 카드를 일일이 클릭)
3. 버전 관리 충돌 (바이너리 파일)
4. 데이터 검증이 어려움
```

### ✅ 해결: Excel → XML → Game 파이프라인

```
┌─────────────────────────────────────────────────────────────────┐
│                     DATA PIPELINE                                │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   [Excel/Google Sheets]    ← 기획자가 편집                       │
│          │                                                      │
│          ▼  (CSV 내보내기)                                       │
│   [Assets/DataTables/*.csv] ← Git에서 텍스트로 관리              │
│          │                                                      │
│          ▼  (에디터 도구: 자동 변환)                              │
│   [Assets/Resources/Data/*.xml]                                 │
│          │                                                      │
│          ▼  (런타임 로드)                                        │
│   [DataService]             ← IDataService.Get<CardData>("id") │
│          │                                                      │
│          ▼  (에셋 바인딩)                                        │
│   [AssetRegistry]           ← 스프라이트, 프리팹 연결            │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

## 6.2 데이터 클래스 작성법

### 기본 규칙

```csharp
using KH.Framework2D.Data.Pipeline;

/// <summary>
/// 모든 게임 데이터는 IGameData를 구현해야 함
/// </summary>
[Serializable]
public class CardData : IGameData
{
    // ═══════════════════════════════════════════════
    // 필수: IGameData 구현
    // ═══════════════════════════════════════════════
    public string Id { get; set; }  // 유일 식별자
    
    // ═══════════════════════════════════════════════
    // 데이터 필드들 (XML 컬럼명과 일치해야 함!)
    // ═══════════════════════════════════════════════
    public string NameKey { get; set; }      // 다국어 키
    public CardType CardType { get; set; }   // Enum 자동 파싱
    public int Cost { get; set; }
    public int BaseDamage { get; set; }
    public bool Exhausts { get; set; }       // "true"/"1"/"yes" → true
    
    // ═══════════════════════════════════════════════
    // 배열/리스트: 쉼표로 구분
    // CSV: "damage,burn,stun" → List: ["damage","burn","stun"]
    // ═══════════════════════════════════════════════
    public string Effects { get; set; }  // 원본 문자열
    
    // 파싱된 리스트 (캐싱)
    private List<string> _effectIds;
    public IReadOnlyList<string> GetEffectIds()
    {
        if (_effectIds == null && !string.IsNullOrEmpty(Effects))
        {
            _effectIds = Effects.Split(',')
                               .Select(s => s.Trim())
                               .Where(s => !string.IsNullOrEmpty(s))
                               .ToList();
        }
        return _effectIds ?? new List<string>();
    }
}

// Enum은 자동으로 파싱됨 (대소문자 무시)
public enum CardType
{
    Attack,
    Skill,
    Power,
    Status,
    Curse
}
```

### 다국어 지원 (ILocalizable)

```csharp
/// <summary>
/// 다국어 지원이 필요한 데이터
/// </summary>
[Serializable]
public class CardData : IGameData, ILocalizable
{
    public string Id { get; set; }
    public string NameKey { get; set; }        // "card_fireball_name"
    public string DescriptionKey { get; set; } // "card_fireball_desc"
    
    // ILocalizable 구현
    string ILocalizable.NameKey => NameKey;
    string ILocalizable.DescriptionKey => DescriptionKey;
}

// 사용법
public void ShowCardInfo(CardData card)
{
    // 로컬라이제이션 서비스로 실제 텍스트 조회
    string name = LocalizationManager.Get(card.NameKey);
    string desc = LocalizationManager.Get(card.DescriptionKey);
}
```

## 6.3 CSV 작성 규칙

### 기본 형식

```csv
Id,NameKey,CardType,Cost,BaseDamage,Effects,@Notes
card_001,card_fireball,Attack,3,5,"damage,burn",화염 공격
card_002,card_shield,Skill,2,0,block,방어 스킬
card_003,card_rage,Power,1,0,"buff_strength,draw",분노 버프
```

### 특수 기능

```csv
# 이 줄은 주석입니다 (# 으로 시작)
# 변환 시 무시됩니다

Id,Name,Damage,@Comment,IsBoss
# @로 시작하는 컬럼은 무시됩니다 (메모용)
enemy_001,Goblin,10,약한 적,false
enemy_002,Dragon,100,보스 몬스터,true

# 쉼표가 포함된 값은 따옴표로 감싸기
card_003,Rage,"힘이 증가하고, 카드를 뽑습니다",1,0
```

### ID 네이밍 컨벤션

```
{카테고리}_{세부분류}_{번호}

예시:
card_attack_001      # 공격 카드
card_skill_heal_001  # 스킬 카드 (힐)
unit_player_warrior  # 플레이어 유닛
unit_enemy_goblin_01 # 적 유닛
skill_active_fire_01 # 액티브 스킬
item_potion_health   # 아이템
```

## 6.4 DataService 사용법

### VContainer 설정

```csharp
public class GameLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        // DataService 생성 및 설정
        var dataService = new DataService();
        
        // 데이터 타입 등록 (Resources 폴더 경로)
        dataService.RegisterDataType<CardData>("Data/Cards");
        dataService.RegisterDataType<UnitData>("Data/Units");
        dataService.RegisterDataType<SkillData>("Data/Skills");
        
        // 싱글톤으로 등록
        builder.RegisterInstance<IDataService>(dataService);
        
        // 게임 시작 시 자동 로드
        builder.RegisterBuildCallback(async container =>
        {
            var service = container.Resolve<IDataService>();
            await service.InitializeAsync();
        });
    }
}
```

### 데이터 조회

```csharp
public class CardSystem
{
    private readonly IDataService _dataService;
    
    public CardSystem(IDataService dataService)
    {
        _dataService = dataService;
    }
    
    // ═══════════════════════════════════════════════
    // 단일 조회
    // ═══════════════════════════════════════════════
    public CardData GetCard(string cardId)
    {
        return _dataService.Get<CardData>(cardId);
    }
    
    // 안전한 조회 (null 체크)
    public bool TryGetCard(string cardId, out CardData card)
    {
        return _dataService.TryGet(cardId, out card);
    }
    
    // ═══════════════════════════════════════════════
    // 전체 조회
    // ═══════════════════════════════════════════════
    public IReadOnlyList<CardData> GetAllCards()
    {
        return _dataService.GetAll<CardData>();
    }
    
    // ═══════════════════════════════════════════════
    // 조건 필터링
    // ═══════════════════════════════════════════════
    public IReadOnlyList<CardData> GetAttackCards()
    {
        return _dataService.GetWhere<CardData>(
            card => card.CardType == CardType.Attack
        );
    }
    
    public IReadOnlyList<CardData> GetAffordableCards(int maxCost)
    {
        return _dataService.GetWhere<CardData>(
            card => card.Cost <= maxCost
        );
    }
    
    // ═══════════════════════════════════════════════
    // 존재 확인
    // ═══════════════════════════════════════════════
    public bool CardExists(string cardId)
    {
        return _dataService.Exists<CardData>(cardId);
    }
    
    // 개수 조회
    public int TotalCardCount => _dataService.Count<CardData>();
}
```

## 6.5 AssetRegistry (에셋 바인딩)

### 왜 데이터와 에셋을 분리하는가?

```
데이터 (XML)              에셋 (AssetRegistry)
──────────────────────    ──────────────────────
- 숫자 값                 - 스프라이트
- 문자열                  - 프리팹
- Enum 타입               - 오디오 클립
- ID 참조                 - 애니메이터

장점:
✅ 기획자: 숫자만 수정, 에셋 걱정 없음
✅ 아티스트: 에셋만 교체, 데이터 걱정 없음
✅ 프로그래머: 명확한 분리로 유지보수 용이
```

### AssetRegistry 사용법

```csharp
// 1. 에디터에서 AssetRegistry 생성
// Create > Data > Asset Registry

// 2. Inspector에서 에셋 등록
// [Sprites]
//   card_001 → FireballSprite
//   card_002 → ShieldSprite
// [Prefabs]
//   unit_warrior → WarriorPrefab
// [Audio]
//   sfx_attack → AttackSound

// 3. 코드에서 사용
public class CardRenderer
{
    public void RenderCard(CardData card)
    {
        // ID로 스프라이트 조회
        Sprite cardSprite = AssetRegistry.Instance.GetSprite(card.Id);
        
        // 없으면 기본값
        Sprite icon = AssetRegistry.Instance.GetSprite(card.Id, defaultSprite);
        
        // 프리팹 조회
        GameObject prefab = AssetRegistry.Instance.GetPrefab(card.Id);
        
        // 사운드 조회
        AudioClip sound = AssetRegistry.Instance.GetAudioClip(card.SoundId);
    }
}
```

---

# 7. 이벤트 시스템

## 7.1 ScriptableObject 기반 이벤트

### 왜 SO 이벤트인가?

```
일반 C# 이벤트의 문제:
❌ 발행자와 구독자가 서로 알아야 함 (강결합)
❌ 씬 전환 시 참조 끊김
❌ 에디터에서 디버깅 어려움

SO 이벤트의 장점:
✅ 완전한 디커플링 (서로 모름)
✅ 씬 독립적 (Asset으로 존재)
✅ 에디터에서 이벤트 발생 가능 (테스트)
✅ 구독자 수 확인 가능
```

### 이벤트 채널 종류

```csharp
// ═══════════════════════════════════════════════
// 1. VoidEventChannel: 매개변수 없음
// ═══════════════════════════════════════════════
[CreateAssetMenu(menuName = "Events/Void Event Channel")]
public class VoidEventChannel : BaseEventChannel
{
    public void Raise();
    public void Subscribe(Action listener);
    public void Unsubscribe(Action listener);
}

// 사용: 게임 시작, 일시정지, 재개 등

// ═══════════════════════════════════════════════
// 2. EventChannel<T>: 매개변수 1개
// ═══════════════════════════════════════════════
// 예: IntEventChannel, StringEventChannel, FloatEventChannel

[CreateAssetMenu(menuName = "Events/Int Event Channel")]
public class IntEventChannel : EventChannel<int> { }

// 사용: 점수 변경, 골드 획득, 데미지 등

// ═══════════════════════════════════════════════
// 3. EventChannel<T1, T2>: 매개변수 2개
// ═══════════════════════════════════════════════
// 예: DamageEventChannel (대상, 데미지량)
```

### 이벤트 생성 및 사용

```csharp
// Step 1: 이벤트 채널 에셋 생성
// Project 창 > Create > Events > Void Event Channel
// 이름: "OnGameStarted"

// Step 2: 발행자 (이벤트 발생시키는 쪽)
public class GameManager : MonoBehaviour
{
    [SerializeField] private VoidEventChannel _onGameStarted;
    [SerializeField] private IntEventChannel _onScoreChanged;
    
    public void StartGame()
    {
        // 이벤트 발생!
        _onGameStarted.Raise();
    }
    
    public void AddScore(int amount)
    {
        _score += amount;
        _onScoreChanged.Raise(_score);
    }
}

// Step 3: 구독자 (이벤트 받는 쪽)
public class UIManager : MonoBehaviour
{
    [SerializeField] private VoidEventChannel _onGameStarted;
    [SerializeField] private IntEventChannel _onScoreChanged;
    
    private void OnEnable()
    {
        // 이벤트 구독
        _onGameStarted.Subscribe(HandleGameStarted);
        _onScoreChanged.Subscribe(HandleScoreChanged);
    }
    
    private void OnDisable()
    {
        // 메모리 누수 방지: 반드시 해제!
        _onGameStarted.Unsubscribe(HandleGameStarted);
        _onScoreChanged.Unsubscribe(HandleScoreChanged);
    }
    
    private void HandleGameStarted()
    {
        ShowGameUI();
    }
    
    private void HandleScoreChanged(int newScore)
    {
        _scoreText.text = newScore.ToString();
    }
}
```

### EventListener 컴포넌트 (코드 없이 연결)

```csharp
/// <summary>
/// Inspector에서 이벤트 연결 (코드 불필요)
/// </summary>
public class VoidEventListener : MonoBehaviour
{
    [SerializeField] private VoidEventChannel _eventChannel;
    [SerializeField] private UnityEvent _response;  // Inspector에서 설정
    
    private void OnEnable() => _eventChannel.Subscribe(OnEventRaised);
    private void OnDisable() => _eventChannel.Unsubscribe(OnEventRaised);
    
    private void OnEventRaised() => _response?.Invoke();
}

// 사용법:
// 1. GameObject에 VoidEventListener 추가
// 2. Event Channel 필드에 SO 드래그
// 3. Response에 원하는 메서드 연결 (UnityEvent처럼)
```

---

# 8. 상태 머신 (State Machine)

## 8.1 상태 머신이란?

```
┌─────────────────────────────────────────────────────────────────┐
│                     STATE MACHINE                                │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   ┌─────────┐    공격    ┌─────────┐                           │
│   │  Idle   │ ─────────▶ │ Attack  │                           │
│   │ (대기)  │            │ (공격)   │                           │
│   └────┬────┘ ◀───────── └────┬────┘                           │
│        │       완료            │                                │
│        │                      │ 피격                            │
│   이동 │                      ▼                                │
│        ▼      ┌─────────┐    ┌─────────┐                       │
│   ┌─────────┐ │  Hurt   │    │  Death  │                       │
│   │  Move   │ │ (피격)   │    │ (사망)   │                       │
│   │ (이동)  │ └─────────┘    └─────────┘                       │
│   └─────────┘                                                   │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘

장점:
✅ 상태별 로직 분리 → 깔끔한 코드
✅ 상태 전환 조건 명확
✅ 디버깅 쉬움 (현재 상태 확인)
```

## 8.2 State 클래스 작성

```csharp
/// <summary>
/// 상태 기본 클래스
/// TOwner: 이 상태를 소유하는 객체 타입
/// </summary>
public abstract class State<TOwner> : IState
{
    // 소유자 참조 (캐릭터, 적 등)
    protected TOwner Owner { get; private set; }
    
    // 상태 머신 참조 (상태 전환용)
    protected StateMachine<TOwner> StateMachine { get; private set; }
    
    // 생명주기 메서드
    protected virtual void OnInitialize() { }  // 최초 1회
    public virtual void Enter() { }            // 상태 진입
    public virtual void Update() { }           // 매 프레임
    public virtual void FixedUpdate() { }      // 물리 업데이트
    public virtual void Exit() { }             // 상태 퇴장
}
```

## 8.3 실전 예제: 2D 캐릭터 상태

### 캐릭터 클래스

```csharp
public class Player2D : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Rigidbody2D _rb;
    [SerializeField] private Animator _animator;
    [SerializeField] private Character2D _character;
    
    [Header("Stats")]
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _attackDuration = 0.5f;
    
    // 상태 머신
    private StateMachine<Player2D> _stateMachine;
    
    // 외부 접근용 프로퍼티
    public Rigidbody2D Rb => _rb;
    public Animator Animator => _animator;
    public Character2D Character => _character;
    public float MoveSpeed => _moveSpeed;
    public float AttackDuration => _attackDuration;
    
    // 입력 캐싱
    public Vector2 MoveInput { get; private set; }
    public bool AttackInput { get; private set; }
    
    private void Awake()
    {
        // 상태 머신 초기화
        _stateMachine = new StateMachine<Player2D>(this);
        
        // 상태 등록
        _stateMachine.AddState<IdleState>();
        _stateMachine.AddState<MoveState>();
        _stateMachine.AddState<AttackState>();
        _stateMachine.AddState<HurtState>();
        _stateMachine.AddState<DeathState>();
        
        // 초기 상태
        _stateMachine.ChangeState<IdleState>();
        
        // 상태 변경 이벤트 (디버깅용)
        _stateMachine.OnStateChanged += (from, to) =>
        {
            Debug.Log($"상태 변경: {from?.GetType().Name} → {to?.GetType().Name}");
        };
    }
    
    private void Update()
    {
        // 입력 캐싱
        MoveInput = new Vector2(Input.GetAxisRaw("Horizontal"), 0);
        AttackInput = Input.GetButtonDown("Fire1");
        
        // 상태 업데이트
        _stateMachine.Update();
    }
    
    private void FixedUpdate()
    {
        _stateMachine.FixedUpdate();
    }
    
    // 외부에서 상태 전환 요청
    public void TakeDamage(int amount)
    {
        _stateMachine.ChangeState<HurtState>();
    }
    
    public void Die()
    {
        _stateMachine.ChangeState<DeathState>();
    }
}
```

### 각 상태 구현

```csharp
// ═══════════════════════════════════════════════
// Idle 상태: 대기
// ═══════════════════════════════════════════════
public class IdleState : State<Player2D>
{
    public override void Enter()
    {
        Owner.Animator.Play("Idle");
    }
    
    public override void Update()
    {
        // 이동 입력 → Move 상태로
        if (Owner.MoveInput.x != 0)
        {
            StateMachine.ChangeState<MoveState>();
            return;
        }
        
        // 공격 입력 → Attack 상태로
        if (Owner.AttackInput)
        {
            StateMachine.ChangeState<AttackState>();
            return;
        }
    }
}

// ═══════════════════════════════════════════════
// Move 상태: 이동
// ═══════════════════════════════════════════════
public class MoveState : State<Player2D>
{
    public override void Enter()
    {
        Owner.Animator.Play("Run");
    }
    
    public override void Update()
    {
        // 이동 입력 없음 → Idle로
        if (Owner.MoveInput.x == 0)
        {
            StateMachine.ChangeState<IdleState>();
            return;
        }
        
        // 공격 입력 → Attack으로
        if (Owner.AttackInput)
        {
            StateMachine.ChangeState<AttackState>();
            return;
        }
        
        // 방향 전환
        Owner.Character.SetFacing(Owner.MoveInput.x > 0);
    }
    
    public override void FixedUpdate()
    {
        // 물리 이동
        Vector2 velocity = Owner.Rb.linearVelocity;
        velocity.x = Owner.MoveInput.x * Owner.MoveSpeed;
        Owner.Rb.linearVelocity = velocity;
    }
    
    public override void Exit()
    {
        // 이동 멈춤
        Vector2 velocity = Owner.Rb.linearVelocity;
        velocity.x = 0;
        Owner.Rb.linearVelocity = velocity;
    }
}

// ═══════════════════════════════════════════════
// Attack 상태: 공격
// ═══════════════════════════════════════════════
public class AttackState : State<Player2D>
{
    private float _timer;
    
    public override void Enter()
    {
        Owner.Animator.Play("Attack");
        _timer = Owner.AttackDuration;
        
        // 공격 중 이동 멈춤
        Owner.Rb.linearVelocity = Vector2.zero;
    }
    
    public override void Update()
    {
        _timer -= Time.deltaTime;
        
        // 공격 완료 → 이전 상태로
        if (_timer <= 0)
        {
            StateMachine.RevertToPreviousState();
        }
    }
}

// ═══════════════════════════════════════════════
// Hurt 상태: 피격
// ═══════════════════════════════════════════════
public class HurtState : State<Player2D>
{
    private float _stunDuration = 0.3f;
    private float _timer;
    
    public override void Enter()
    {
        Owner.Animator.Play("Hurt");
        Owner.Rb.linearVelocity = Vector2.zero;
        Owner.Character.FlashAsync().Forget();
        Owner.Character.Shake();
        _timer = _stunDuration;
    }
    
    public override void Update()
    {
        _timer -= Time.deltaTime;
        
        if (_timer <= 0)
        {
            StateMachine.ChangeState<IdleState>();
        }
    }
}

// ═══════════════════════════════════════════════
// Death 상태: 사망
// ═══════════════════════════════════════════════
public class DeathState : State<Player2D>
{
    public override void Enter()
    {
        Owner.Animator.Play("Death");
        Owner.Rb.linearVelocity = Vector2.zero;
        Owner.Rb.simulated = false;  // 물리 비활성화
        
        // 사망 연출
        Owner.Character.DeathEffectAsync().Forget();
    }
    
    // 다른 상태로 전환 불가 (사망 상태 유지)
}
```

---

*다음 파트에서 계속: 오브젝트 풀링, 유틸리티, 실전 예제...*
