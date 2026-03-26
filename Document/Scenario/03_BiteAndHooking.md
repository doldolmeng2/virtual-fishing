# 시나리오 03 - 입질 및 물고기 낚기

## 도메인

| 도메인 | 클래스 / 인터페이스 | 역할 |
|---|---|---|
| 낚싯대 | `FishingRodController` / `IFishingRod` | 챔질 동작 감지, 상태 전이 |
| 찌 | `FloatController` / `IFishingFloat` | 입질 시 침강 연출, 시각적 반응 |
| 물고기 | `FishController` / `IFish` | 어종 데이터 인스턴스화, 움직임 패턴 |
| 물고기 스포너 | `FishSpawner` / `IFishSpawner` | 확률 기반 입질 타이밍·어종 결정 |
| 장력 | `TensionDataSO` | 입질과 동시에 장력 추적 활성화 |
| 피드백 | `FeedbackManager` / `IFeedbackService` | 사운드, 진동, 시각효과, TTS 통합 Facade |
| 게임 흐름 | `GameFlowManager` / `IGameFlowManager` | 게임 상태 전이 |

## 사전 조건

1. 시나리오 02(낚시 준비 및 캐스팅)가 성공적으로 완료됨
2. 찌가 수면에 안착하여 미세 드리프트 중
3. `FishingRodController.CurrentState == RodState.WaitingForBite`
4. `FishingSiteDataSO.spawnFishList`(출현 어종 목록) 데이터가 정상 로드됨

## 로직

### 1단계: 입질 대기 및 예고

1. `FishSpawner.StartBiteTimer()` 호출
2. 출현 어종 목록(`FishSpawnEntry`)에서 확률에 따라 어종을 선택
3. 해당 어종의 `minWaitTime ~ maxWaitTime` 범위에서 랜덤 대기 시간 결정
4. 대기 시간 경과 후 **예고 입질** 발생:
   - 찌 주변에 작은 물결 효과
   - 주 컨트롤러에 미세한 진동

| 피드백 유형 | 내용 |
|---|---|
| 시각 | 찌 주변 물결(Ripple) 이펙트 |
| 진동 | 입질 예고 미세 진동 (`HapticPattern.LightPulse`) |

### 2단계: 본 입질 (물고기가 미끼를 뭄)

1. 예고 입질 후 0.5~1.5초 후 **본 입질** 발생
2. `FishSpawner`가 선택된 어종으로 `FishController.Initialize(speciesData)` 호출
   - `FishController.Weight`, `Resistance`, `Pattern` 등 데이터 초기화
3. 찌가 수면 아래로 크게 출렁이며 `FloatController.SinkingDepth` 급격히 증가
4. `TensionDataSO`의 장력 추적이 활성화됨
5. `<<VoidEventSO>> BiteOccurred` 이벤트 발행

| 피드백 유형 | 내용 |
|---|---|
| 사운드 | 찌가 물속으로 빠지는 퐁당 사운드 |
| 진동 | 물고기가 찌를 물고 있는 느낌의 강한 진동 (`HapticPattern.StrongPulse`) |
| 시각 | 찌가 물속으로 잠기는 애니메이션 |

### 3단계: 챔질 판정

플레이어가 **타이밍 윈도우** 내에 낚싯대를 **챔질 존(Hooking Zone)** 까지 들어 올리는 동작을 수행해야 합니다.

> **챔질 존 개념**: 시나리오 02의 캐스팅 존과 동일한 방식입니다. 플레이어 머리 위쪽에 구체(Sphere) 형태의 판정 영역이 존재하며, 입질이 발생하면 챔질 존이 활성화됩니다. 낚싯대(컨트롤러)가 이 존에 도달하면서 충분한 가속도를 가지고 있어야 챔질 성공으로 판정합니다.

#### 챔질 존 설정 (`GameSettingsSO`)

| 설정값 | 기본값 | 설명 |
|---|---|---|
| `hookingZoneRadius` | 0.4m | 챔질 존 반경 |
| `hookingZoneOffset` | (0, 0.5, 0) | 플레이어 머리 기준 존 중심 오프셋 (위쪽) |
| `hookingMinAcceleration` | 1.5 | 챔질 성공에 필요한 최소 가속도 (어르신 대상이므로 낮게) |
| `hookTimingWindow` | 3초 | 입질 후 챔질 유효 시간 |

#### 성공 경로

1. 본 입질 발생 후 `hookTimingWindow`(기본 3초) 이내에 챔질 동작 감지
2. 시스템은 매 프레임 다음 두 조건을 모두 검사:
   - **존 조건**: 컨트롤러가 챔질 존(Hooking Zone) 내에 진입했는지 (`IFishingRod.IsInHookingZone`)
   - **가속도 조건**: 컨트롤러의 위쪽(y+) 방향 가속도가 `hookingMinAcceleration` 이상인지
3. **두 조건이 동시에 충족**되면 챔질 성공으로 판정
   - 존에 도달했지만 가속도가 부족하면 → 판정 보류 (계속 대기)
   - 가속도는 충분하지만 존에 미도달이면 → 판정 보류 (계속 대기)
4. `RodState`가 `WaitingForBite → Hit → MiniGame`으로 순차 변경
5. `<<VoidEventSO>> RodStateChanged` 이벤트 발행
6. `GameFlowManager.TransitionTo(GameState.MiniGame)`

| 피드백 유형 | 내용 |
|---|---|
| 시각 | 입질 발생 시 챔질 존 가이드를 은은하게 시각화 (위로 올리라는 화살표 등) |
| 진동 | 챔질 존 진입 시 약한 진동 → 성공 시 강한 진동 (`HapticPattern.StrongPulse`) |
| 시각 | 챔질 성공 알림 이펙트 (번쩍임) |
| 사운드 | 성공 효과음 |

#### 실패 경로: 타이밍 초과

1. `hookTimingWindow`가 경과해도 두 조건이 동시에 충족되지 않음
2. 물고기가 미끼를 뱉고 도망

| 피드백 유형 | 내용 |
|---|---|
| TTS | "아쉽습니다. 다시 도전해보세요" (부드러운 톤) |
| 시각 | 물고기 도망 이펙트 |

3. `FishingRodController.CurrentState`가 `Idle`로 롤백
4. `FloatController`가 줄을 회수하며 초기화
5. `GameFlowManager.TransitionTo(GameState.FishingReady)` → 시나리오 02로 복귀

## 속성 변화

| 속성 | 변화 내용 |
|---|---|
| `FishingRodController.CurrentState` | WaitingForBite → Hit → MiniGame (성공 시) 또는 Idle (실패 시) |
| `FishController.Weight` | 입질 시 `FishSpeciesDataSO.weightRange`에서 랜덤 결정 |
| `FishController.Resistance` | 입질 시 `FishSpeciesDataSO.baseResistance` 기반 초기화 |
| `FishController.Pattern` | 입질 시 `FishSpeciesDataSO.movementPattern`으로 설정 |
| `TensionDataSO.currentTension` | 입질과 동시에 활성화되어 업데이트 시작 |
| `FloatController.SinkingDepth` | 물고기가 미끼를 물면 급격히 증가 |

## 사후 조건

### 성공 시
1. 물고기의 저항 데이터와 움직임 패턴이 미니게임 시스템으로 전달됨
2. `GameFlowManager.CurrentState == GameState.MiniGame`
3. 시나리오 04(미니게임 실행)로 제어권 전환

### 실패 시
1. 찌와 낚싯줄이 회수됨
2. `GameFlowManager.CurrentState == GameState.FishingReady`
3. 시나리오 02(낚시 준비 및 캐스팅)를 다시 수행할 수 있는 상태

## 관련 SO Event 채널

| Event SO | 발행자 | 구독자 |
|---|---|---|
| `BiteOccurred` (VoidEventSO) | FishSpawner | FishingRodController, FeedbackManager |
| `RodStateChanged` (VoidEventSO) | FishingRodController | FeedbackManager, GameFlowManager |

## 관련 SO Data

| SO | 사용 시스템 |
|---|---|
| `FishDatabaseSO` | FishSpawner (어종 선택) |
| `FishingSiteDataSO` | FishSpawner (출현 어종 목록) |
| `FishSpeciesDataSO` | FishController (어종별 속성 초기화) |
| `TensionDataSO` | TensionCalculator (장력 추적 시작) |
| `GameSettingsSO` | FishingRodController (hookTimingWindow, hookingZoneRadius, hookingZoneOffset, hookingMinAcceleration) |

## 담당자

**담당자 B - 낚싯대/캐스팅 시스템** (챔질 판정)
**담당자 C - 물고기/환경** (FishSpawner, FishController)
