# KH Framework 2D (UPM)

Unity 2D 상용 프로젝트에서 바로 쓸 수 있도록 정리한 공용 프레임워크 패키지입니다.

- Namespace: `KH.Framework2D.*`
- UPM Package Name: `com.kh.framework2d`
- 버전: **2.1.0**

## 포함 기능 (요약)
- **Services**: Audio, SceneLoader, Save, Input(Old/New), Time, Settings, Localization
- **Architecture**: MVP Base, SO Event Channels, Generic FSM, ServiceLocator
- **Gameplay/2D**: Character2D, Camera2D, HealthBar, DamagePopup
- **Combat**: CombatFormulas, Projectile, StatusEffect
- **Data (SO)**: UnitData, SkillData
- **Utilities**: ObjectPool, PoolManager, Timer/Cooldown, Extensions

## 설치 (UPM)
1) 이 패키지를 `Packages/` 아래에 두거나, Git URL로 추가합니다.

2) 이 프레임워크는 다음 의존 패키지를 전제로 합니다...

### 필수/권장 의존성
- VContainer (DI): Git URL 설치
- UniTask: Git URL 설치
- DOTween: Asset Store(또는 프로젝트에 DOTween 설치)
- TextMeshPro: `com.unity.textmeshpro`
- Input System(선택): `com.unity.inputsystem`

> **Git URL 예시**
- VContainer: `https://github.com/hadashiA/VContainer.git?path=VContainer/Assets/VContainer`
- UniTask: `https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask`

## 샘플
Package Manager > `KH Framework 2D` > **Samples** > *Demo Game (VContainer setup)* 를 Import.

## 변경점(2.1.0)
- Pool 반환 경로 정리: `PooledHandle` 추가, 풀 오브젝트가 스스로 안전하게 return 가능
- `Projectile`, `DamagePopup`, `Character2D`의 `SetActive(false)` 기반 종료를 풀 반환 우선으로 변경
- `InputManager`: UI Cancel/Confirm 지원 + 파괴 시 InputAction 이벤트 unsubscribe
- `SettingsManager`: ObservableProperty의 즉시 콜백 호출로 인한 초기 이벤트 폭주 방지
- `SaveManager`: PlayerPrefs에 primitive 저장/로드 호환성 추가(typed payload)

