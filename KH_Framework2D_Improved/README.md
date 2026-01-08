# KH Framework 2D - 개선 패치 적용 가이드

## 📦 패치 내용

이 패키지는 코드 리뷰에서 발견된 5가지 주요 이슈에 대한 수정 사항을 포함합니다.

### 수정된 파일들

| 파일 | 적용 경로 |
|------|-----------|
| `InputManager.cs` | `Runtime/KH.Framework2D/Services/Input/` |
| `PoolManager.cs` | `Runtime/KH.Framework2D/Pool/` |
| `SaveManager.cs` | `Runtime/KH.Framework2D/Services/Save/` |
| `ServiceLocator.cs` | `Runtime/KH.Framework2D/Services/` |
| `DataService.cs` | `Runtime/KH.Framework2D/Data/Pipeline/` |

## 🚀 적용 방법

### 방법 1: 직접 교체 (권장)

```bash
# 1. 기존 파일 백업
cp -r com.kh.framework2d/Runtime com.kh.framework2d/Runtime_backup

# 2. 개선된 파일 복사
cp KH_Framework2D_Improved/InputManager.cs com.kh.framework2d/Runtime/KH.Framework2D/Services/Input/
cp KH_Framework2D_Improved/PoolManager.cs com.kh.framework2d/Runtime/KH.Framework2D/Pool/
cp KH_Framework2D_Improved/SaveManager.cs com.kh.framework2d/Runtime/KH.Framework2D/Services/Save/
cp KH_Framework2D_Improved/ServiceLocator.cs com.kh.framework2d/Runtime/KH.Framework2D/Services/
cp KH_Framework2D_Improved/DataService.cs com.kh.framework2d/Runtime/KH.Framework2D/Data/Pipeline/
```

### 방법 2: Git Patch (버전 관리 사용 시)

```bash
git diff > backup.patch
# 파일 교체 후
git add -A
git commit -m "Apply code review improvements"
```

## ⚠️ Breaking Changes

### SaveManager

**기존 코드:**
```csharp
var save = new SaveManager(useEncryption: true, encryptionKey: "mykey");
```

**변경 후:**
```csharp
// 개발용 (암호화 없음)
var save = SaveManager.CreateDevelopment();

// 캐주얼 난독화 (XOR)
var save = SaveManager.CreateObfuscated("mykey");

// 프로덕션 (AES-256 암호화)
var save = SaveManager.CreateSecure();
```

### InputManager

UI 전환 시 입력 모드 설정이 필요합니다:

```csharp
// UI 팝업 열 때
inputManager.SetInputMode(Define.InputMode.UI);

// UI 팝업 닫을 때
inputManager.SetInputMode(Define.InputMode.Gameplay);
```

이를 자동화하려면 UIManager에서 처리:

```csharp
// UIManager.ShowPopupUI에 추가
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

// UIManager.CloseAllPopupUI에 추가
public void CloseAllPopupUI()
{
    // ... 기존 코드 ...
    
    // 모든 팝업 닫히면 게임플레이 모드로 복귀
    if (_popupStack.Count == 0)
    {
        if (ServiceLocator.TryGet<IInputService>(out var input))
        {
            (input as InputManager)?.SetInputMode(Define.InputMode.Gameplay);
        }
    }
}
```

## 🧪 테스트 체크리스트

적용 후 다음 항목을 테스트하세요:

- [ ] 게임 시작 시 데이터 로드 정상 동작
- [ ] Escape 키가 게임 중에는 Pause, UI에서는 Cancel 발생
- [ ] 오브젝트 풀링 정상 동작 (Spawn/Despawn)
- [ ] 저장/불러오기 정상 동작
- [ ] 씬 전환 시 서비스 정리 정상 동작

## 📚 자세한 내용

`CODE_REVIEW_REPORT.md` 파일에서 각 이슈에 대한 상세 분석과 개선 내용을 확인할 수 있습니다.

---

## 문의

문제 발생 시 원본 파일(`Runtime_backup/`)로 복원하거나 이슈를 보고해 주세요.
