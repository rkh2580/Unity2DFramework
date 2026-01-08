# 🎓 KH Framework 2D 완전 정복 가이드 (Part 2)

# 5. 서비스 시스템

## 5.1 서비스 아키텍처 개요

```
┌─────────────────────────────────────────────────────────────────┐
│                      SERVICE LAYER                               │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐              │
│  │  IAudio     │  │  IScene     │  │  ISave      │              │
│  │  Service    │  │  Service    │  │  Service    │              │
│  └──────┬──────┘  └──────┬──────┘  └──────┬──────┘              │
│         │                │                │                      │
│         ▼                ▼                ▼                      │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐              │
│  │  Audio      │  │  Scene      │  │  Save       │              │
│  │  Manager    │  │  Loader     │  │  Manager    │              │
│  └─────────────┘  └─────────────┘  └─────────────┘              │
│                                                                 │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐              │
│  │  IInput     │  │  ITime      │  │  IData      │              │
│  │  Service    │  │  Service    │  │  Service    │              │
│  └──────┬──────┘  └──────┬──────┘  └──────┬──────┘              │
│         │                │                │                      │
│         ▼                ▼                ▼                      │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐              │
│  │  Input      │  │  Time       │  │  Data       │              │
│  │  Manager    │  │  Manager    │  │  Service    │              │
│  └─────────────┘  └─────────────┘  └─────────────┘              │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

## 5.2 AudioManager (오디오 서비스)

### 인터페이스

```csharp
public interface IAudioService
{
    // ═══════════════════════════════════════════════
    // SFX (효과음)
    // ═══════════════════════════════════════════════
    void PlaySFX(string key);                    // 키로 재생
    void PlaySFX(AudioClip clip);                // 클립으로 재생
    void PlaySFX(string key, Vector3 position);  // 3D 위치에서 재생
    void PlaySFXWithPitch(string key, float minPitch = 0.9f, float maxPitch = 1.1f);
    
    // ═══════════════════════════════════════════════
    // BGM (배경음)
    // ═══════════════════════════════════════════════
    void PlayBGM(string key, bool loop = true);
    void StopBGM();
    void PauseBGM();
    void ResumeBGM();
    UniTask CrossfadeBGM(string key, float duration = -1);  // 크로스페이드
    
    // ═══════════════════════════════════════════════
    // 볼륨 (0-1)
    // ═══════════════════════════════════════════════
    void SetMasterVolume(float volume);
    void SetBGMVolume(float volume);
    void SetSFXVolume(float volume);
    
    float MasterVolume { get; }
    float BGMVolume { get; }
    float SFXVolume { get; }
    bool IsBGMPlaying { get; }
    
    // 런타임 클립 등록
    void RegisterClip(string key, AudioClip clip);
}
```

### 사용 예제

```csharp
public class CombatSystem
{
    private readonly IAudioService _audio;
    
    public CombatSystem(IAudioService audio)
    {
        _audio = audio;
    }
    
    public void Attack()
    {
        // 기본 효과음
        _audio.PlaySFX("sword_swing");
        
        // 피치 변화로 다양성 추가 (같은 소리 반복 시 지루함 방지)
        _audio.PlaySFXWithPitch("hit", 0.9f, 1.1f);
    }
    
    public void EnterBossBattle()
    {
        // 기존 BGM에서 보스 BGM으로 크로스페이드
        _audio.CrossfadeBGM("boss_theme", 2f).Forget();
    }
}

// 설정 화면에서 볼륨 조절
public class SettingsPresenter
{
    private readonly IAudioService _audio;
    
    public void OnMasterVolumeChanged(float value)
    {
        _audio.SetMasterVolume(value);  // 자동으로 PlayerPrefs에 저장됨
    }
}
```

### AudioManager 핵심 구현 해설

```csharp
public class AudioManager : MonoBehaviour, IAudioService
{
    // ═══════════════════════════════════════════════
    // SFX 풀링: 여러 효과음 동시 재생을 위한 오디오소스 풀
    // ═══════════════════════════════════════════════
    private readonly List<AudioSource> _sfxPool = new();
    private int _sfxPoolIndex;
    
    private void InitializeSFXPool()
    {
        // 10개의 AudioSource를 미리 생성
        for (int i = 0; i < _sfxPoolSize; i++)
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            _sfxPool.Add(source);
        }
    }
    
    // 라운드 로빈 방식으로 순환
    private AudioSource GetNextSFXSource()
    {
        var source = _sfxPool[_sfxPoolIndex];
        _sfxPoolIndex = (_sfxPoolIndex + 1) % _sfxPool.Count;
        source.pitch = 1f;  // 피치 리셋
        return source;
    }
    
    // ═══════════════════════════════════════════════
    // 볼륨: 선형값(0-1)을 데시벨로 변환
    // ═══════════════════════════════════════════════
    private float LinearToDecibel(float linear)
    {
        // 0 = 무음(-80dB), 1 = 최대(0dB)
        return linear > 0.0001f ? Mathf.Log10(linear) * 20f : -80f;
    }
    
    // ═══════════════════════════════════════════════
    // 클립 캐싱: 키로 클립 조회
    // ═══════════════════════════════════════════════
    private AudioClip GetClip(string key)
    {
        // 1. 캐시에서 먼저 찾기
        if (_clipCache.TryGetValue(key, out var clip))
            return clip;
        
        // 2. Resources에서 로드
        clip = Resources.Load<AudioClip>($"Audio/{key}");
        if (clip != null)
        {
            _clipCache[key] = clip;  // 캐시에 저장
            return clip;
        }
        
        Debug.LogWarning($"[AudioManager] Clip not found: {key}");
        return null;
    }
}
```

---

## 5.3 SceneLoader (씬 서비스)

### 인터페이스

```csharp
public interface ISceneService
{
    UniTask LoadSceneAsync(string sceneName, bool showLoadingScreen = true);
    UniTask LoadSceneAdditiveAsync(string sceneName);  // 추가 로드
    UniTask UnloadSceneAsync(string sceneName);
    UniTask ReloadCurrentSceneAsync();
    
    string CurrentSceneName { get; }
    bool IsLoading { get; }
    bool IsSceneLoaded(string sceneName);
    void SetActiveScene(string sceneName);
    
    // 이벤트
    event Action<float> OnLoadProgress;    // 0-1 진행률
    event Action<string> OnSceneLoaded;
    event Action<string> OnSceneUnloaded;
}
```

### 사용 예제

```csharp
public class MainMenuPresenter
{
    private readonly ISceneService _scene;
    
    public MainMenuPresenter(ISceneService scene)
    {
        _scene = scene;
        
        // 씬 로드 완료 이벤트 구독
        _scene.OnSceneLoaded += OnSceneLoaded;
    }
    
    public async void StartGame()
    {
        // 로딩 화면 표시하며 씬 로드
        await _scene.LoadSceneAsync("GameScene", showLoadingScreen: true);
    }
    
    public async void QuickRestart()
    {
        // 현재 씬 재시작
        await _scene.ReloadCurrentSceneAsync();
    }
    
    private void OnSceneLoaded(string sceneName)
    {
        Debug.Log($"씬 로드 완료: {sceneName}");
    }
}

// 멀티 씬 관리 (UI 씬 + 게임 씬)
public class GameBootstrap
{
    private readonly ISceneService _scene;
    
    public async UniTask LoadGame()
    {
        // 메인 게임 씬 로드
        await _scene.LoadSceneAsync("GameLevel1");
        
        // UI 씬 추가 로드
        await _scene.LoadSceneAdditiveAsync("GameUI");
        
        // 게임 씬을 활성 씬으로 설정 (라이팅 기준)
        _scene.SetActiveScene("GameLevel1");
    }
}
```

### SceneLoader 핵심 구현 해설

```csharp
public async UniTask LoadSceneAsync(string sceneName, bool showLoadingScreen = true)
{
    if (IsLoading)
    {
        Debug.LogWarning("[SceneLoader] Already loading a scene!");
        return;
    }
    
    IsLoading = true;
    
    if (showLoadingScreen)
        ShowLoadingScreen();
    
    float startTime = Time.realtimeSinceStartup;
    
    // ═══════════════════════════════════════════════
    // 핵심: allowSceneActivation = false
    // 씬 로드가 90%에서 멈추고 활성화 대기
    // ═══════════════════════════════════════════════
    var operation = SceneManager.LoadSceneAsync(sceneName);
    operation.allowSceneActivation = false;
    
    // 90%까지 진행률 업데이트
    while (operation.progress < 0.9f)
    {
        float progress = Mathf.Clamp01(operation.progress / 0.9f);
        UpdateProgress(progress * 0.9f);
        await UniTask.Yield();
    }
    
    // ═══════════════════════════════════════════════
    // 최소 로딩 시간 보장 (UX 개선)
    // 너무 빠른 로딩은 오히려 불안정해 보임
    // ═══════════════════════════════════════════════
    float elapsed = Time.realtimeSinceStartup - startTime;
    if (elapsed < _minimumLoadTime)
    {
        // 페이크 진행률로 자연스럽게
        float fakeProgress = 0.9f;
        while (fakeProgress < 1f)
        {
            fakeProgress += Time.unscaledDeltaTime * _fakeProgressSpeed;
            UpdateProgress(Mathf.Min(fakeProgress, 0.99f));
            await UniTask.Yield();
        }
    }
    
    // 씬 활성화!
    UpdateProgress(1f);
    operation.allowSceneActivation = true;
    
    await UniTask.WaitUntil(() => operation.isDone);
    
    // 약간의 딜레이 후 로딩 화면 숨김
    if (showLoadingScreen)
    {
        await UniTask.Delay(TimeSpan.FromSeconds(0.2f));
        HideLoadingScreen();
    }
    
    IsLoading = false;
    OnSceneLoaded?.Invoke(sceneName);
}
```

---

## 5.4 SaveManager (저장 서비스)

### 인터페이스

```csharp
public interface ISaveService
{
    // ═══════════════════════════════════════════════
    // PlayerPrefs 기반 (간단한 데이터)
    // ═══════════════════════════════════════════════
    void Save<T>(string key, T data);
    T Load<T>(string key, T defaultValue = default);
    bool HasKey(string key);
    void Delete(string key);
    void DeleteAll();
    
    // ═══════════════════════════════════════════════
    // 파일 기반 (복잡한 데이터)
    // ═══════════════════════════════════════════════
    void SaveToFile<T>(string fileName, T data);
    T LoadFromFile<T>(string fileName, T defaultValue = default);
    bool FileExists(string fileName);
    void DeleteFile(string fileName);
    string[] GetAllSaveFiles();
}
```

### 사용 예제

```csharp
// ═══════════════════════════════════════════════
// 예제 1: 간단한 설정 저장
// ═══════════════════════════════════════════════
public class SettingsManager
{
    private readonly ISaveService _save;
    
    public float MusicVolume
    {
        get => _save.Load("settings_music", 1f);
        set => _save.Save("settings_music", value);
    }
    
    public bool TutorialComplete
    {
        get => _save.Load("tutorial_complete", false);
        set => _save.Save("tutorial_complete", value);
    }
}

// ═══════════════════════════════════════════════
// 예제 2: 게임 진행 데이터 저장
// ═══════════════════════════════════════════════

// 저장용 데이터 클래스 (Serializable 필수!)
[Serializable]
public class GameSaveData : SaveData  // SaveData 상속 권장
{
    public int currentLevel;
    public int gold;
    public int[] unlockedCharacters;
    public List<InventoryItem> inventory;
}

[Serializable]
public class InventoryItem
{
    public string itemId;
    public int count;
}

public class GameSaveManager
{
    private readonly ISaveService _save;
    private const string SAVE_FILE = "game_save";
    
    public void SaveGame(GameSaveData data)
    {
        _save.SaveToFile(SAVE_FILE, data);
        Debug.Log("게임 저장 완료!");
    }
    
    public GameSaveData LoadGame()
    {
        if (!_save.FileExists(SAVE_FILE))
        {
            Debug.Log("저장 파일 없음, 새 게임 시작");
            return new GameSaveData();
        }
        
        return _save.LoadFromFile<GameSaveData>(SAVE_FILE);
    }
    
    public void DeleteSave()
    {
        _save.DeleteFile(SAVE_FILE);
    }
    
    // 모든 저장 파일 목록 (슬롯 시스템용)
    public string[] GetSaveSlots()
    {
        return _save.GetAllSaveFiles();
    }
}
```

### SaveManager 핵심 구현 해설

```csharp
public class SaveManager : ISaveService
{
    // ═══════════════════════════════════════════════
    // 타입 프리픽스: primitive 타입 안전하게 저장
    // JsonUtility는 primitive를 직접 직렬화 못함!
    // ═══════════════════════════════════════════════
    private const string PrefixInt = "__i:";
    private const string PrefixFloat = "__f:";
    private const string PrefixBool = "__b:";
    private const string PrefixString = "__s:";
    private const string PrefixJson = "__j:";
    
    public void Save<T>(string key, T data)
    {
        // 타입에 따라 프리픽스 추가
        string payload = data switch
        {
            int i => PrefixInt + i.ToString(CultureInfo.InvariantCulture),
            float f => PrefixFloat + f.ToString("R", CultureInfo.InvariantCulture),
            bool b => PrefixBool + (b ? "1" : "0"),
            string s => PrefixString + s,
            null => null,
            _ => PrefixJson + JsonUtility.ToJson(data)  // 객체는 JSON
        };
        
        // 암호화 옵션
        if (_useEncryption)
            payload = Encrypt(payload);
        
        PlayerPrefs.SetString(key, payload);
        PlayerPrefs.Save();  // 즉시 저장!
    }
    
    public T Load<T>(string key, T defaultValue = default)
    {
        if (!PlayerPrefs.HasKey(key))
            return defaultValue;
        
        string payload = PlayerPrefs.GetString(key);
        
        if (_useEncryption)
            payload = Decrypt(payload);
        
        // 프리픽스로 타입 판단 후 파싱
        if (payload.StartsWith(PrefixInt))
            return (T)(object)int.Parse(payload.Substring(PrefixInt.Length));
        
        if (payload.StartsWith(PrefixFloat))
            return (T)(object)float.Parse(payload.Substring(PrefixFloat.Length));
        
        // ... 다른 타입들
        
        if (payload.StartsWith(PrefixJson))
            return JsonUtility.FromJson<T>(payload.Substring(PrefixJson.Length));
        
        // 이전 버전 호환: 프리픽스 없으면 JSON으로 시도
        return JsonUtility.FromJson<T>(payload);
    }
    
    // ═══════════════════════════════════════════════
    // 파일 저장: persistentDataPath에 JSON으로
    // ═══════════════════════════════════════════════
    public void SaveToFile<T>(string fileName, T data)
    {
        string path = GetFilePath(fileName);  // Saves/filename.json
        string json = JsonUtility.ToJson(data, prettyPrint: true);
        
        if (_useEncryption)
            json = Encrypt(json);
        
        File.WriteAllText(path, json);
    }
}
```

---

## 5.5 TimeManager (시간 서비스)

### 인터페이스

```csharp
public interface ITimeService
{
    // ═══════════════════════════════════════════════
    // 시간 값 (매 프레임 캐싱됨)
    // ═══════════════════════════════════════════════
    float DeltaTime { get; }           // Time.deltaTime
    float UnscaledDeltaTime { get; }   // Time.unscaledDeltaTime
    float FixedDeltaTime { get; }
    float TotalTime { get; }           // 게임 시작 후 경과 시간
    float UnscaledTotalTime { get; }
    float TimeScale { get; set; }
    bool IsPaused { get; }
    
    // ═══════════════════════════════════════════════
    // 일시정지/재개
    // ═══════════════════════════════════════════════
    void Pause();
    void Resume();
    void TogglePause();
    
    // ═══════════════════════════════════════════════
    // 특수 효과
    // ═══════════════════════════════════════════════
    void SetTimeScale(float scale);
    void SlowMotion(float scale = 0.3f);  // 슬로우모션
    void ResetTimeScale();
    
    // 이벤트
    event Action OnPaused;
    event Action OnResumed;
    event Action<float> OnTimeScaleChanged;
}
```

### 사용 예제

```csharp
public class CombatEffects
{
    private readonly ITimeService _time;
    
    public async UniTask PlayCriticalHitEffect()
    {
        // 크리티컬 히트 시 슬로우모션
        _time.SlowMotion(0.2f);
        
        await UniTask.Delay(TimeSpan.FromSeconds(0.3f), 
                            ignoreTimeScale: true);  // 실제 시간 기준!
        
        _time.ResetTimeScale();
    }
}

public class PauseMenuPresenter
{
    private readonly ITimeService _time;
    private readonly IInputService _input;
    
    public PauseMenuPresenter(ITimeService time, IInputService input)
    {
        _time = time;
        _input = input;
        
        // ESC 키로 일시정지 토글
        _input.OnPause += HandlePauseInput;
    }
    
    private void HandlePauseInput()
    {
        _time.TogglePause();
        
        if (_time.IsPaused)
        {
            ShowPauseMenu();
        }
        else
        {
            HidePauseMenu();
        }
    }
}
```

---

## 5.6 GameManager (게임 상태 관리)

### 기본 구조

```csharp
// 게임 상태 열거형
public enum GameState
{
    None,
    Loading,    // 로딩 중
    MainMenu,   // 메인 메뉴
    Playing,    // 게임 진행
    Paused,     // 일시정지
    Win,        // 승리
    Lose,       // 패배
    GameOver    // 게임오버
}

// 추상 GameManager (상속해서 사용)
public abstract class GameManager<TGameManager> : MonoBehaviour 
    where TGameManager : GameManager<TGameManager>
{
    public static TGameManager Instance { get; private set; }
    
    protected StateMachine<TGameManager> _stateMachine;
    public GameState CurrentState { get; protected set; }
    
    // 이벤트
    public event Action<GameState, GameState> OnStateChanged;
    public event Action OnGameStarted;
    public event Action OnGamePaused;
    public event Action OnGameResumed;
    public event Action<bool> OnGameEnded;  // isWin
    
    // 자식 클래스에서 구현
    protected abstract void InitializeStateMachine();
    public abstract void RestartGame();
    public abstract void ReturnToMenu();
}
```

### 구현 예제

```csharp
public class MyGameManager : GameManager<MyGameManager>
{
    [SerializeField] private SceneLoader _sceneLoader;
    
    protected override void InitializeStateMachine()
    {
        // 상태 머신 초기화 (선택적)
        _stateMachine = new StateMachine<MyGameManager>(this);
        _stateMachine.AddState<LoadingState>();
        _stateMachine.AddState<PlayingState>();
        _stateMachine.AddState<PausedState>();
    }
    
    protected override void Awake()
    {
        base.Awake();  // 싱글톤 처리
        
        // 게임 시작 시 로딩 상태
        ChangeState(GameState.Loading);
    }
    
    public override void RestartGame()
    {
        Time.timeScale = 1f;
        _sceneLoader.ReloadCurrentSceneAsync().Forget();
    }
    
    public override void ReturnToMenu()
    {
        Time.timeScale = 1f;
        _sceneLoader.LoadSceneAsync("MainMenu").Forget();
    }
    
    // 게임 승리 처리
    public void OnPlayerWin()
    {
        EndGame(isWin: true);
    }
    
    // 게임 패배 처리
    public void OnPlayerDeath()
    {
        EndGame(isWin: false);
    }
}

// 간단한 프로토타입용 (상속 없이 바로 사용)
public class QuickGame : MonoBehaviour
{
    private void Start()
    {
        // SimpleGameManager 사용
        SimpleGameManager.Instance.StartGame();
        
        SimpleGameManager.Instance.OnGameEnded += isWin =>
        {
            if (isWin)
                Debug.Log("You Win!");
            else
                Debug.Log("Game Over!");
        };
    }
    
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SimpleGameManager.Instance.TogglePause();
        }
    }
}
```

---

*다음 파트에서 계속: 데이터 파이프라인, 이벤트 시스템, 상태 머신...*
