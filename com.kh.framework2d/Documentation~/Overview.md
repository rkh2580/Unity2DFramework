# Overview

## Data(정적) vs Runtime(동적) vs Persist

- 정적 데이터: ScriptableObject 기반(예: UnitData, SkillData, EventChannel 등)
- 런타임 데이터: MonoBehaviour/Model 내부 상태(ObservableProperty 포함)
- 영속 저장: PlayerPrefs / JSON File (SaveManager)

## 권장 폴더/역할

- Runtime/KH.Framework2D
  - Services: Audio/Scene/Save/Input/Time/Settings/Localization
  - Pool: ObjectPool, PoolManager, PooledHandle
  - Data: ScriptableObject definitions
  - Events: EventChannel (SO)
  - Components2D: Character2D, HealthBar, DamagePopup, Camera2D

