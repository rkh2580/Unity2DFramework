# KH Framework 2D 코드 리뷰 및 개선 보고서

> **작성일**: 2025-01-09  
> **검토 대상**: KH Framework 2D Refactored 버전

---

## 📋 개요

코드 리뷰에서 제기된 5가지 주요 이슈에 대해 분석하고 개선안을 적용했습니다.

| 이슈 | 심각도 | 상태 | 개선 방법 |
|------|:------:|:----:|-----------|
| ServiceLocator 전역 상태 리스크 | 중간 | ✅ 개선됨 | 사용 정책 명확화, 경고 시스템 추가 |
| DataService Reflection 의존 | 낮음 | ✅ 개선됨 | 타입화된 로더 패턴으로 변경 |
| Legacy Input Escape 중복 | 높음 | ✅ 수정됨 | 입력 모드 기반 분기 처리 |
| SaveManager 단순 암호화 | 중간 | ✅ 개선됨 | 플러거블 암호화 인터페이스 |
| PoolManager async void | 낮음 | ✅ 수정됨 | UniTaskVoid + 예외 처리 |

---

## 1. ServiceLocator 전역 상태 리스크

### 문제점
```csharp
// DataServiceInstaller.cs에서 DI와 ServiceLocator 동시 등록
builder.RegisterInstance<IDataService>(dataService);

// ServiceLocator에도 등록 - 이중 관리 발생
builder.RegisterBuildCallback(container =>
{
    ServiceLocator.Register(container.Resolve<IDataService>());
});
```

- DI와 ServiceLocator 병행 사용으로 의존성 관리 복잡화
- 어디서 서비스를 가져와야 하는지 불명확
- 테스트 시 Mock 설정이 어려워짐

### 개선안

**사용 정책 명확화:**

| 컨텍스트 | 권장 방식 | ServiceLocator 허용? |
|----------|-----------|:--------------------:|
| MonoBehaviour | `[Inject]` 어트리뷰트 | ❌ |
| 일반 클래스 | 생성자 주입 | ❌ |
| ScriptableObject | ServiceLocator | ✅ |
| Static 유틸리티 | ServiceLocator | ✅ |
| 에디터 스크립트 | ServiceLocator | ✅ |

**개선된 ServiceLocator 기능:**
- Strict Mode: MonoBehaviour에서 호출 시 경고 로그
- 등록 소스 추적: 어디서 등록되었는지 디버깅 가능
- `GetDebugInfo()`: 현재 상태 덤프

```csharp
// 개선된 사용 예시 - ScriptableObject에서만 사용
[CreateAssetMenu]
public class GameEventSO : ScriptableObject
{
    public void Raise()
    {
        // ScriptableObject는 DI 받을 수 없으므로 ServiceLocator 사용 OK
        if (ServiceLocator.TryGet<IAudioService>(out var audio))
        {
            audio.PlaySFX("event_sound");
        }
    }
}
```

---

## 2. DataService Reflection 의존

### 문제점
```csharp
// 기존: 런타임 리플렉션 사용
var parseMethod = typeof(XmlDataParser)
    .GetMethod(nameof(XmlDataParser.Parse))
    .MakeGenericMethod(dataType);  // 느림, 타입 안전성 없음

var parsedData = parseMethod.Invoke(null, new object[] { textAsset.text, config.RowElementName });
```

### 개선안

**타입화된 로더 패턴:**
```csharp
// 개선: 컴파일 타임 타입 안전성
private class DataLoader<T> : IDataLoader where T : class, IGameData, new()
{
    public async UniTask LoadAsync(DataService service)
    {
        // 리플렉션 없이 직접 호출
        var parsedData = XmlDataParser.Parse<T>(textAsset.text, _rowElementName);
        container.AddRange(parsedData);
    }
}
```

**Fluent API 추가:**
```csharp
// 더 읽기 쉬운 설정
var dataService = new DataService()
    .WithData<CardData>("Data/Cards")
    .WithData<UnitData>("Data/Units")
    .WithData<SkillData>("Data/Skills");
```

---

## 3. Legacy Input Escape 중복 이벤트 (🔴 Critical)

### 문제점
```csharp
// 기존 코드: 273행과 287행에서 Escape 처리 중복
if (UnityEngine.Input.GetKeyDown(KeyCode.Escape))  // 273행
{
    PausePressed = true;
    OnPause?.Invoke();  // Pause 이벤트 발생
}

// ...

if (UnityEngine.Input.GetKeyDown(KeyCode.Escape))  // 287행
{
    CancelPressed = true;
    OnCancel?.Invoke();  // Cancel 이벤트도 발생! 🐛
}
```

**결과:** 한 프레임에 Pause와 Cancel 모두 발생 → UI/게임 로직 혼란

### 개선안

**입력 모드 기반 분기:**
```csharp
// 개선: 현재 모드에 따라 하나만 발생
private void HandleEscapeKey()
{
    switch (CurrentMode)
    {
        case Define.InputMode.Gameplay:
            // 게임플레이 중: Escape = 일시정지 메뉴
            PausePressed = true;
            OnPause?.Invoke();
            break;
            
        case Define.InputMode.UI:
            // UI 활성화 상태: Escape = 취소/닫기
            CancelPressed = true;
            OnCancel?.Invoke();
            break;
    }
}
```

**새로운 API:**
```csharp
// UI 열 때
inputManager.SetInputMode(Define.InputMode.UI);

// UI 닫을 때
inputManager.SetInputMode(Define.InputMode.Gameplay);
```

---

## 4. SaveManager 암호화 방식

### 문제점
```csharp
// 기존: 하드코딩된 XOR 암호화 - 보안 취약
private string Encrypt(string text)
{
    char[] result = new char[text.Length];
    for (int i = 0; i < text.Length; i++)
    {
        result[i] = (char)(text[i] ^ _encryptionKey[i % _encryptionKey.Length]);
    }
    return Convert.ToBase64String(/*...*/);
}
```

- XOR은 쉽게 해독 가능
- 암호화 방식 변경이 어려움
- 테스트/개발 시 암호화 비활성화 불가

### 개선안

**플러거블 암호화 인터페이스:**
```csharp
public interface IEncryptionProvider
{
    string Encrypt(string plainText);
    string Decrypt(string cipherText);
}
```

**사전 제공 구현체:**

| 클래스 | 용도 | 보안 수준 |
|--------|------|-----------|
| `NoEncryption` | 개발/디버깅 | 없음 |
| `XorEncryption` | 캐주얼 난독화 | 낮음 |
| `AesEncryption` | 프로덕션 | 높음 (AES-256) |

**팩토리 메서드:**
```csharp
// 개발용
var save = SaveManager.CreateDevelopment();

// 캐주얼 게임용
var save = SaveManager.CreateObfuscated("my_key");

// 프로덕션용 (AES-256)
var save = SaveManager.CreateSecure();
```

---

## 5. PoolManager async void Start

### 문제점
```csharp
// 기존: async void는 예외 전파 안 됨
private async void Start()  // 🐛 async void
{
    if (_warmUpOnStart)
    {
        await WarmUpAllAsync();  // 예외 발생 시 추적 불가
    }
}
```

- `async void`는 fire-and-forget
- 예외 발생 시 스택 트레이스 손실
- 디버깅 어려움

### 개선안

**UniTaskVoid + 명시적 예외 처리:**
```csharp
private void Start()
{
    if (_warmUpOnStart)
    {
        WarmUpOnStartAsync().Forget();
    }
}

private async UniTaskVoid WarmUpOnStartAsync()
{
    try
    {
        await WarmUpAllAsync();
        IsWarmedUp = true;
        OnWarmUpComplete?.Invoke();
        Debug.Log("[PoolManager] Warmup complete.");
    }
    catch (Exception ex)
    {
        // 예외를 로깅하지만 게임은 계속 진행
        Debug.LogError($"[PoolManager] Warmup failed: {ex.Message}\n{ex.StackTrace}");
        IsWarmedUp = true;  // 무한 대기 방지
        OnWarmUpComplete?.Invoke();
    }
}
```

**새로운 API:**
```csharp
// Warmup 완료 대기 가능
await UniTask.WaitUntil(() => poolManager.IsWarmedUp);

// 또는 이벤트 구독
poolManager.OnWarmUpComplete += () => StartGame();
```

---

## 📁 개선된 파일 목록

| 파일명 | 변경 내용 |
|--------|-----------|
| `InputManager.cs` | Escape 중복 처리 수정, InputMode 추가, Clear() 메서드 |
| `PoolManager.cs` | async void → UniTaskVoid, 예외 처리, IsWarmedUp 프로퍼티 |
| `SaveManager.cs` | IEncryptionProvider 인터페이스, AES 암호화 추가 |
| `ServiceLocator.cs` | Strict Mode, 등록 소스 추적, GetDebugInfo() |
| `DataService.cs` | 타입화된 로더, Fluent API |

---

## 🔧 적용 가이드

### 1. 파일 교체

```bash
# 개선된 파일들을 해당 위치로 복사
Runtime/KH.Framework2D/Services/Input/InputManager.cs
Runtime/KH.Framework2D/Pool/PoolManager.cs
Runtime/KH.Framework2D/Services/Save/SaveManager.cs
Runtime/KH.Framework2D/Services/ServiceLocator.cs
Runtime/KH.Framework2D/Data/Pipeline/DataService.cs
```

### 2. 기존 코드 수정 필요 사항

**SaveManager 사용 코드:**
```csharp
// 기존
new SaveManager(useEncryption: true, encryptionKey: "key");

// 변경
SaveManager.CreateSecure();  // 또는 CreateObfuscated("key")
```

**InputManager 사용 코드 (UI 전환 시):**
```csharp
// UI 열 때 추가
inputManager.SetInputMode(Define.InputMode.UI);

// UI 닫을 때 추가  
inputManager.SetInputMode(Define.InputMode.Gameplay);
```

---

## 📊 영향도 분석

| 변경 | 하위 호환성 | 마이그레이션 필요 |
|------|:-----------:|:-----------------:|
| InputManager | ⚠️ 부분 | UI 전환 코드 수정 권장 |
| PoolManager | ✅ 완전 | 없음 |
| SaveManager | ❌ 파괴적 | 팩토리 메서드 사용 |
| ServiceLocator | ✅ 완전 | 없음 |
| DataService | ✅ 완전 | 없음 |

---

## 🎯 추가 권장 사항

### 1. 데이터 파이프라인 개선
현재 Excel → CSV → XML → Game 파이프라인은 잘 구성되어 있습니다. 추가로:

- **Addressables 지원**: 대규모 프로젝트 시 Resources 폴더 대신 Addressables 사용
- **Hot Reload**: 에디터에서 CSV 수정 시 자동 리로드
- **Validation**: 데이터 로드 후 무결성 검사 (필수 필드, ID 중복 등)

### 2. 테스트 커버리지
```csharp
// Mock 주입 예시
var mockAudio = Substitute.For<IAudioService>();
ServiceLocator.Register<IAudioService>(mockAudio, "UnitTest");
```

### 3. 성능 프로파일링
- DataService 초기화 시간 측정
- Pool 메모리 사용량 모니터링
- 씬 전환 시 서비스 정리 검증

---

*보고서 작성: Claude (Anthropic)*  
*검토 기준: 초기 설계 철학 (VContainer DI, UniTask, MVP 패턴)*
