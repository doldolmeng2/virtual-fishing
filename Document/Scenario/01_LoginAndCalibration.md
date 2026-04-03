# 시나리오 01 - 초기 접속 및 캘리브레이션

## 도메인

| 도메인 | 클래스 / 인터페이스 | 역할 |
|---|---|---|
| 계정 | `AccountManager` / `IAccountService` | 계정 로드, 도감·플레이 시간 관리 |
| 플레이어 | `CalibrationController` / `ICalibrationService` | 앉은키·팔 길이 측정, 안전구역 계산 |
| 게임 흐름 | `GameFlowManager` / `IGameFlowManager` | 게임 상태 전이 (Login → Calibration → FishingReady) |
| 피드백 | `FeedbackManager` / `IFeedbackService` | 사운드, 진동, 시각효과, TTS 통합 Facade |
| 낚시터 | `FishingSiteDataSO` | 낚시터 설정 데이터 (배경, 출현 어종, 씬 이름) |
| 씬 전환 | `SceneTransitionService` / `ISceneService` | 낚시터 씬 비동기 로드 |

## 사전 조건

1. HMD와 컨트롤러가 시스템에 정상적으로 연결되어 트래킹 상태가 유효함
2. 저장소(로컬 또는 클라우드)에 계정 데이터 접근 권한이 확보됨
3. 플레이어가 물리적 안전구역(Guardian/Boundary) 내에 위치함
4. `GameFlowManager.CurrentState == GameState.Login`

## 로직

### 1단계: 계정 선택 및 로드

1. 시스템이 저장된 계정 목록을 큰 버튼 형태의 UI로 표시함
2. 플레이어가 계정(ID)을 선택하면 `AccountManager.LoadAccount(id)`가 호출됨
   - 해당 계정의 **도감(Encyclopedia)** 과 **마지막 플레이 시간(LastPlayedAt)** 을 로드
   - `AccountDataSO`에 런타임 데이터 바인딩
3. 로드 완료 시 `<<VoidEventSO>> OnAccountLoaded` 이벤트 발행

| 피드백 유형 | 내용 |
|---|---|
| 시각 | 로딩 화면(진행률 표시) 출력 |
| 사운드 | 로그인 완료 효과음 |

4. `GameFlowManager.TransitionTo(GameState.Calibration)`

### 2단계: 앉은키 캘리브레이션

1. 시스템이 정면 응시를 요청하는 안내 아이콘을 플레이어 시야 중앙에 표시
2. TTS로 "정면을 바라봐 주세요"를 또렷하고 천천히 출력
3. 플레이어가 정면을 응시한 상태에서 트리거 또는 일정 시간(3초) 대기 후 자동 확인
4. HMD의 y 좌표를 읽어 `PlayerDataSO.sittingHeight`에 저장
5. HMD의 x, z 좌표를 `PlayerDataSO.currentPosition`에 저장 (안전구역 중심점)

| 피드백 유형 | 내용 |
|---|---|
| 시각 | 정면 응시 안내 아이콘 (시선 유도 UI) |
| TTS | "정면을 바라봐 주세요" |

### 3단계: 팔 길이 캘리브레이션

1. TTS로 "양팔을 옆으로 뻗어주세요"를 출력
2. 플레이어가 양팔을 뻗으면, 시스템이 양쪽 컨트롤러 사이의 거리(D)를 계산
3. `PlayerDataSO.armLength = D / 2`로 저장
4. 팔 길이를 기반으로 안전구역 반경을 계산하여 `PlayerDataSO.safetyRadius`에 저장
   - 안전 반경 = `armLength * 계수` (GameSettingsSO에서 설정 가능)
5. `<<VoidEventSO>> CalibrationComplete` 이벤트 발행

| 피드백 유형 | 내용 |
|---|---|
| 진동 | 양쪽 컨트롤러에 완료 진동 (`HapticPattern.StrongPulse`) |
| TTS | "캘리브레이션이 완료되었습니다" |
| 시각 | 안전 구역 반경을 바닥에 원형으로 시각화 |

### 4단계: 낚시터 선택 및 씬 전환

1. 시스템이 낚시터 목록을 큰 카드 UI로 출력 (배경 이미지 + 이름)
2. 각 카드에 마우스오버(레이캐스트 호버) 시 해당 낚시터의 배경 사운드 미리듣기 재생
3. 플레이어가 낚시터를 선택하면 `FishingSiteDataSO.backgroundType`이 설정됨
4. `SceneTransitionService.LoadScene(siteData.sceneName)` 호출
5. 씬 로드 완료 시 `<<VoidEventSO>> OnSceneLoaded` 발행
6. `GameFlowManager.TransitionTo(GameState.FishingReady)`

| 피드백 유형 | 내용 |
|---|---|
| 시각 | 낚시터별 배경 이미지 카드 UI |
| 사운드 | 선택된 낚시터의 환경 사운드 (물소리, 새소리 등) |

### 예외 흐름: 컨트롤러 추적 실패

1. 캘리브레이션 중 컨트롤러 트래킹이 소실되면 `<<VoidEventSO>> TrackingLost` 발행
2. `FeedbackManager`가 구독하여 경고 UI + TTS("컨트롤러를 확인해주세요") 출력
3. 트래킹 복구 시 캘리브레이션을 해당 단계부터 재개

## 속성 변화

| 속성 | 변화 내용 |
|---|---|
| `AccountDataSO.lastPlayedAt` | 현재 시스템 시간으로 업데이트 |
| `PlayerDataSO.sittingHeight` | HMD의 y 좌표값 저장 |
| `PlayerDataSO.armLength` | 계산된 D/2 값 저장 (D = 컨트롤러 사이 거리) |
| `PlayerDataSO.currentPosition` | 캘리 시점의 HMD x, z 좌표 저장 |
| `PlayerDataSO.safetyRadius` | 팔 길이 기반으로 계산된 안전 반경 저장 |
| `FishingSiteDataSO.backgroundType` | 유저가 선택한 BackgroundType Enum 값 |
| `GameFlowManager.CurrentState` | Login → Calibration → FishingReady |

## 사후 조건

1. 플레이어의 캘리브레이션 데이터(`PlayerDataSO`)가 정상적으로 설정됨
2. 선택된 낚시터 씬이 로드 완료됨
3. `GameFlowManager.CurrentState == GameState.FishingReady`
4. 시나리오 02(낚시 준비 및 캐스팅)를 시작할 준비가 완료됨

## 관련 SO Event 채널

| Event SO | 발행자 | 구독자 |
|---|---|---|
| `OnAccountLoaded` (VoidEventSO) | AccountManager | GameFlowManager, FeedbackManager |
| `CalibrationComplete` (VoidEventSO) | CalibrationController | GameFlowManager |
| `TrackingLost` (VoidEventSO) | CalibrationController | FeedbackManager |
| `OnSceneLoaded` (VoidEventSO) | SceneTransitionService | GameFlowManager |

## 관련 SO Data

| SO | 사용 시스템 |
|---|---|
| `PlayerDataSO` | CalibrationController (쓰기), SafetyMonitor (읽기) |
| `AccountDataSO` | AccountManager (읽기/쓰기) |
| `FishingSiteDataSO` | FishingSiteManager (읽기), FishSpawner (읽기) |
| `GameSettingsSO` | CalibrationController (calibrationTimeout, 안전 반경 계수) |

## 담당자

**담당자 A - 코어/계정/캘리브레이션 시스템**
