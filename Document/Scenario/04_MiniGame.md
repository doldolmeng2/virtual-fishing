# 시나리오 04 - 미니게임 실행

## 도메인

| 도메인 | 클래스 / 인터페이스 | 역할 |
|---|---|---|
| 미니게임 | `MiniGameManager` / `IMiniGame` | 난이도·제한시간 설정, 성공 게이지 관리, 결과 판정 |
| 물고기 | `FishController` / `IFish` | 물고기 AI 움직임 패턴 실행 |
| 낚싯대 | `FishingRodController` / `IFishingRod` | 릴링 속도·낚싯대 방향 입력 제공 |
| 장력 | `TensionCalculator` / `ITensionCalculator` | 장력 실시간 계산 |
| 장력 데이터 | `TensionDataSO` | 장력 값 공유 (SO 패턴) |
| 피드백 | `FeedbackManager` / `IFeedbackService` | 사운드, 진동, 시각효과, TTS 통합 Facade |
| 게임 흐름 | `GameFlowManager` / `IGameFlowManager` | 게임 상태 전이 |

## 사전 조건

1. 시나리오 03(입질 및 물고기 낚기)에서 챔질에 성공하여 바늘에 물고기가 걸린 상태
2. `FishingRodController.CurrentState == RodState.MiniGame`
3. `FishController`에 어종 데이터(저항력, 무게, 움직임 패턴)가 초기화 완료
4. `GameFlowManager.CurrentState == GameState.MiniGame`

## 로직

### 1단계: 미니게임 초기화

1. `MiniGameManager.StartMiniGame(fishCatchData)` 호출
2. 물고기의 `Resistance`와 `Weight`를 바탕으로 난이도(`Difficulty`)를 계산
   ```
   Difficulty = (fish.Resistance * 0.7) + (fish.Weight * 0.3)
   ```
3. 제한 시간(`RemainingTime`) 설정:
   ```
   RemainingTime = MiniGameSettingsSO.baseTimeLimit / Difficulty
   ```
4. `SuccessGauge = 0`, `TensionDataSO.ResetTension()` 호출

### 2단계: UI 표시 및 안내

플레이어의 정면(시야 중앙 하단)에 크고 직관적인 UI를 표시합니다.

| UI 요소 | 설명 |
|---|---|
| 텐션 게이지 | 큰 원형/반원형 게이지. 안전(초록)→경고(노랑)→위험(빨강) 색상 변화 |
| 성공 게이지 | 물고기 체력/진행률 바. 0에서 100까지 채워지는 형태 |
| 남은 시간 | 큰 숫자로 표시 |
| 물고기 방향 | 물고기가 이동하는 방향 화살표 표시 |

| 피드백 유형 | 내용 |
|---|---|
| 사운드 | 긴장감 있는 미니게임 BGM 재생 |
| TTS | "릴을 감아주세요!" 또는 상황별 안내 음성 |

### 3단계: 플레이어 입력 (릴링 + 낚싯대 당기기)

> **추가 옵션 사항**: 보조 컨트롤러의 **그립 버튼 홀드** 을 감지하여 릴 감기로 매핑합니다. 단, **트리거 연타** 또는 **손목 회전** 등 대안 입력 방식을 `GameSettingsSO`에서 전환 가능하도록 했습니다. 일단 그립 버튼 홀드를 기준으로 작성하셔도 될 것 같아요. 만약 손목 회전이 기본 옵션이면 변경해주시면 될 것 같습니다.

1. **릴 감기 (보조 컨트롤러)**:
   - 보조 컨트롤러의 회전 궤적을 매 프레임 계산
   - 회전 속도 → `FishingRodController.ReelingSpeed` 업데이트
   - 릴링 중에는 지속적 약한 진동 + 릴 사운드(끼릭끼릭)

2. **낚싯대 당기기 (주 컨트롤러)**:
   - 물고기가 이동하는 **반대 방향**으로 낚싯대를 당기면 효과 증가
   - 같은 방향으로 당기면 장력 급증

| 피드백 유형 | 내용 |
|---|---|
| 사운드 | 릴 감는 사운드 (끼릭끼릭) |
| 진동 | 릴링 중 약한 지속 진동 (`HapticPattern.Continuous`, 보조 컨트롤러) |

### 4단계: 물고기 AI 저항

1. `FishController.ExecuteMovement()` 매 프레임 호출
2. 물고기의 `MovementPattern`에 따라 행동:
   - **Calm**: 좌우로 완만하게 이동, 간헐적 약한 저항
   - **Aggressive**: 강하게 좌우로 요동, 갑작스러운 방향 전환
   - **Erratic**: 불규칙적으로 멀리 도망가려는 패턴

| 피드백 유형 | 내용 |
|---|---|
| 진동 | 물고기 강한 저항 시 진동 강화 (`HapticPattern.StrongPulse`, 양쪽) |
| 시각 | 물보라 이펙트 |
| TTS | "낚싯대를 반대로 당기세요!" (물고기 강한 저항 시) |

### 5단계: 장력 계산 및 게이지 업데이트

1. `TensionCalculator.Calculate(fish.Resistance, reelingSpeed, rodDirection)` 매 프레임 호출
2. 장력 계산 공식:
   ```
   tension += (fish.Resistance * resistanceFactor) - (oppositeDirectionBonus)
   tension += reelingSpeed * MiniGameSettingsSO.tensionIncreaseRate
   tension -= MiniGameSettingsSO.tensionDecreaseRate (릴링 안 할 때)
   tension = Clamp(tension, 0, TensionDataSO.maxTension)
   ```
3. `TensionDataSO.currentTension` 업데이트
4. `<<FloatEventSO>> TensionChanged` 이벤트 발행 → UI 및 피드백 갱신

5. **성공 게이지 업데이트**:
   - `TensionDataSO.GetCurrentZone() == TensionZone.Safe`일 때 릴링하면 상승
   ```
   SuccessGauge += MiniGameSettingsSO.gaugeIncreaseRate * reelingSpeed * deltaTime
   ```
   - `<<FloatEventSO>> SuccessGaugeChanged` 발행

6. **남은 시간 감소**: `RemainingTime -= deltaTime`

### 결과 판정

#### 성공: SuccessGauge >= 100

1. `MiniGameManager.EndMiniGame(true)` 호출
2. `<<VoidEventSO>> MiniGameResult` 발행 (성공)
3. 미니게임 UI 사라짐
4. `GameFlowManager.TransitionTo(GameState.Result)`

#### 실패 A: 장력 초과 (줄 끊어짐)

1. `TensionDataSO.currentTension`이 `maxTension`에 도달

| 피드백 유형 | 내용 |
|---|---|
| 시각 | 게이지 붉은색 점멸 → 줄 끊어지는 연출 |
| 사운드 | 삐-삐- 경고음 → 줄 끊어지는 사운드 |
| 진동 | 진동이 갑자기 끊김 (긴장감 전달) |
| TTS | "줄이 끊어졌습니다" (부드러운 톤) |

2. `MiniGameManager.EndMiniGame(false)` 호출
3. `GameFlowManager.TransitionTo(GameState.FishingReady)` → 시나리오 02로 복귀

#### 실패 B: 시간 초과

1. `RemainingTime <= 0`

| 피드백 유형 | 내용 |
|---|---|
| TTS | "시간이 초과되었습니다" |
| 시각 | 물고기 도망 이펙트 |

2. `MiniGameManager.EndMiniGame(false)` 호출
3. `GameFlowManager.TransitionTo(GameState.FishingReady)` → 시나리오 02로 복귀

## 속성 변화

| 속성 | 변화 내용 |
|---|---|
| `MiniGameManager.Difficulty` | 물고기 저항력과 크기에 비례하여 설정 |
| `MiniGameManager.RemainingTime` | 미니게임 시작과 동시에 실시간 감소 |
| `MiniGameManager.SuccessGauge` | 안전 장력 + 릴링 시 0→100 상승 |
| `TensionDataSO.currentTension` | 물고기 저항과 릴링 속도/방향에 따라 0~100 증감 |
| `FishingRodController.ReelingSpeed` | 보조 컨트롤러 회전 궤적에 따라 실시간 업데이트 |

## 사후 조건

### 성공 시
1. `SuccessGauge == 100` (물고기 제압 완료)
2. 미니게임 UI 제거
3. `GameFlowManager.CurrentState == GameState.Result`
4. 시나리오 05(포획 결과 및 수족관 도감)로 제어권 전환

### 실패 시
1. 찌와 낚싯줄이 회수됨
2. `FishingRodController.CurrentState == RodState.Idle`
3. `TensionDataSO.ResetTension()` 호출
4. `GameFlowManager.CurrentState == GameState.FishingReady`
5. 시나리오 02(낚시 준비 및 캐스팅)로 복귀

## 관련 SO Event 채널

| Event SO | 발행자 | 구독자 |
|---|---|---|
| `TensionChanged` (FloatEventSO) | TensionCalculator | MiniGameUI, FeedbackManager |
| `SuccessGaugeChanged` (FloatEventSO) | MiniGameManager | MiniGameUI |
| `MiniGameResult` (VoidEventSO) | MiniGameManager | GameFlowManager |

## 관련 SO Data

| SO | 사용 시스템 |
|---|---|
| `TensionDataSO` | TensionCalculator (쓰기), MiniGameManager (읽기), UI (읽기) |
| `MiniGameSettingsSO` | MiniGameManager (난이도, 시간, 게이지 증감률) |

## 담당자

**담당자 D - 미니게임/장력 시스템**
