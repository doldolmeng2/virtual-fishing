# 시나리오 02 - 낚시 준비 및 캐스팅

## 도메인

| 도메인 | 클래스 / 인터페이스 | 역할 |
|---|---|---|
| 낚싯대 | `FishingRodController` / `IFishingRod`, `IGrabbable`, `ICastable` | 낚싯대 부착, 캐스팅 입력 처리, 상태 관리 |
| 찌 | `FloatController` / `IFishingFloat` | 찌 발사, 궤도 시뮬레이션, 착수 처리 |
| 낚싯줄 | `FishingLineRenderer` | 낚싯대 끝과 찌 사이의 줄 렌더링 |
| 피드백 | `FeedbackManager` / `IFeedbackService` | 사운드, 진동, 시각효과 통합 Facade |
| 게임 흐름 | `GameFlowManager` / `IGameFlowManager` | 게임 상태 전이 |

## 사전 조건

1. 시나리오 01(초기 접속 및 캘리브레이션)이 성공적으로 완료됨
2. `PlayerDataSO`에 앉은키, 팔 길이, 안전구역 데이터가 설정됨
3. 낚시터 씬 로드가 완료되어 플레이어가 해당 맵에 위치함
4. 낚싯대 프리팹이 플레이어의 상호작용 가능 범위 내에 생성되어 있음
5. `GameFlowManager.CurrentState == GameState.FishingReady`

## 로직

### 1단계: 낚싯대 부착

1. 플레이어의 손(컨트롤러)이 낚싯대에 인접하면 하이라이트 시각 효과로 상호작용 가능함을 표시
2. 트리거를 누르면 `FishingRodController.OnGrab(hand)` 호출
3. 낚싯대가 컨트롤러에 부착되고 `RodState`가 `Idle → Attached`로 변경
4. `<<VoidEventSO>> RodStateChanged` 이벤트 발행

| 피드백 유형 | 내용 |
|---|---|
| 사운드 | 낚싯대 부착 효과음 (딸깍) |
| 진동 | 약한 진동 (`HapticPattern.LightPulse`, 주 컨트롤러) |
| 시각 | 낚싯대 하이라이트 → 부착 시 해제 |

### 2단계: 캐스팅 준비 (캐스팅 존 판정)

1. 시스템은 매 프레임 컨트롤러의 위치가 **캐스팅 존**(정해진 범위) 내에 있는지 판정
   - 캐스팅 존: 플레이어 머리 위쪽~뒤쪽의 반구 영역
   - 반경은 `GameSettingsSO.castingZoneRadius`로 설정 가능
2. 낚싯대가 캐스팅 존에 진입하면 진입 알림 사운드 재생
3. 플레이어가 캐스팅 존에서 팔을 앞으로 휘두르면(존을 빠져나오면) 캐스팅으로 판정

| 피드백 유형 | 내용 |
|---|---|
| 사운드 | 캐스팅 존 진입 시 가벼운 알림음 |

### 3단계: 캐스팅 실행

1. 컨트롤러가 캐스팅 존을 빠져나오는 순간을 투척으로 판정
2. 시스템은 다음 값들을 계산:
   - **체류 시간**: 캐스팅 존에 머문 시간 (파워 보정)
   - **가속도**: 컨트롤러의 순간 가속도 (`FishingRodController.Acceleration`)
   - **방향**: 컨트롤러의 이동 방향 벡터 (`FishingRodController.Direction`)
3. 찌 발사 속도 계산:
   ```
   power = (acceleration * GameSettingsSO.castingPowerMultiplier) + timeBonus
   ```
4. `RodState`가 `Attached → Casting`으로 변경
5. `FloatController.Launch(power, direction)` 호출

| 피드백 유형 | 내용 |
|---|---|
| 진동 | 강한 진동 (`HapticPattern.StrongPulse`, 주 컨트롤러) |
| 사운드 | 줄 풀리는 사운드 |
| 시각 | 찌가 날아가는 궤도를 강조하는 트레일 이펙트 |

### 4단계: 착수

1. `FloatController`가 포물선 궤도를 시뮬레이션하며 찌를 이동
2. 찌가 수면(Water Surface)에 충돌하면:
   - `FloatController.velocity = 0`
   - 침강 깊이(`sinkingDepth`)를 계산하여 찌가 수면 아래로 일부 잠김
   - `<<VoidEventSO>> OnWaterLanded` 이벤트 발행
3. `RodState`가 `Casting → WaitingForBite`로 변경
4. 착수 후 찌는 수면 위에서 물의 흐름에 따라 자연스럽게 미세 드리프트

| 피드백 유형 | 내용 |
|---|---|
| 시각 | 착수 지점 물 튀김 파티클 이펙트 |
| 사운드 | 물에 빠지는 첨벙 사운드 |

## 속성 변화

| 속성 | 변화 내용 |
|---|---|
| `FishingRodController.CurrentState` | Idle → Attached → Casting → WaitingForBite 순차 변경 |
| `FishingRodController.Acceleration` | 컨트롤러 가속도에 따라 업데이트 |
| `FishingRodController.Direction` | 컨트롤러 방향에 따라 업데이트 |
| `FloatController.Velocity` | 계산된 찌 속도로 설정 후 착수 시 0 |
| `FloatController.SinkingDepth` | 착수 후부터 임계값까지 천천히 증가 |

## 사후 조건

1. `FishingRodController.CurrentState == RodState.WaitingForBite`
2. 찌가 수면 위에 안착하여 입질 대기 상태
3. `FishingLineRenderer`가 낚싯대 끝과 찌 사이를 연결하여 렌더링 중
4. 시나리오 03(입질 및 물고기 낚기)을 시작할 준비가 완료됨

## 관련 SO Event 채널

| Event SO | 발행자 | 구독자 |
|---|---|---|
| `RodStateChanged` (VoidEventSO) | FishingRodController | FeedbackManager, GameFlowManager |
| `OnWaterLanded` (VoidEventSO) | FloatController | FeedbackManager, FishSpawner |

## 관련 SO Data

| SO | 사용 시스템 |
|---|---|
| `GameSettingsSO` | FishingRodController (castingZoneRadius, castingPowerMultiplier) |

## 담당자

**담당자 B - 낚싯대/캐스팅 시스템**
