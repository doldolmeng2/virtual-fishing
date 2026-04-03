# 시나리오 05 - 포획 결과 및 수족관 도감

## 도메인

| 도메인 | 클래스 / 인터페이스 | 역할 |
|---|---|---|
| 계정 | `AccountManager` / `IAccountService` | 도감 데이터 저장, 점수 갱신 |
| 물고기 | `FishController` / `IFish` | 포획된 물고기 표시 및 상호작용 |
| 낚싯대 | `FishingRodController` / `IFishingRod` | 상태 초기화 (Idle) |
| 피드백 | `FeedbackManager` / `IFeedbackService` | 사운드, 진동, 시각효과, TTS 통합 Facade |
| 게임 흐름 | `GameFlowManager` / `IGameFlowManager` | 게임 상태 전이 |

## 사전 조건

1. 시나리오 04(미니게임 실행)에서 `SuccessGauge`가 100에 도달하여 물고기 제압 성공
2. `FishController`에 어종 데이터(종류, 무게, 등급 등)가 유효함
3. `GameFlowManager.CurrentState == GameState.Result`

## 로직

### 1단계: 포획 연출

1. 화면 전환 효과와 함께 잡힌 물고기가 플레이어 **정면 시야** 눈높이에 크고 선명하게 표시됨
2. 물고기 오브젝트가 자연스럽게 회전하며 디테일을 보여줌

| 피드백 유형 | 내용 |
|---|---|
| 사운드 | 팡파르 또는 경쾌한 성공 사운드 |
| 시각 | 반짝이는 폭죽 / 물보라 형태의 축하 이펙트 |

### 2단계: 물고기 상호작용

1. 플레이어가 **보조 컨트롤러**(낚싯대를 쥐지 않은 손)를 뻗어 물고기를 터치하거나 회전시킬 수 있음
2. 손이 물고기 콜라이더에 닿을 때마다 상호작용 피드백 발생

| 피드백 유형 | 내용 |
|---|---|
| 진동 | 펄떡이는 듯한 부드러운 진동 (`HapticPattern.LightPulse`, 보조 컨트롤러) |
| 사운드 | 물고기 펄떡거리는 사운드 |

### 3단계: 정보창 표시 및 TTS

1. 물고기 옆에 **크고 대비가 뚜렷한 글씨체**로 정보창 UI를 표시:
   - 어종 이름
   - 크기 (cm)
   - 무게 (kg)
   - 등급 (희귀도에 따른 별 등급)
2. TTS 엔진이 정보창 내용을 **천천히, 또렷한 음성**으로 읽어줌
   - 예: "붕어를 잡으셨습니다! 크기는 35cm, 무게는 2.3kg입니다. 별 3개 등급이에요!"

| 피드백 유형 | 내용 |
|---|---|
| 시각 | 큰 정보창 UI (고대비 색상, 큰 글씨) |
| TTS | 어종 정보를 천천히 또렷하게 읽어주는 음성 |

### 4단계: 확인 처리 (수동 + 자동)

1. 정보창 하단에 커다란 **'수족관에 넣기'** 버튼 표시
2. 동시에 자동 확인 타이머 시작 (`autoConfirmDelay`)
3. 확인 트리거:
   - **수동**: 플레이어가 버튼을 누르면 즉시 확인 → 타이머 취소
   - **자동**: 타이머 만료 시 자동 확인

| 피드백 유형 | 내용 |
|---|---|
| 사운드 | 버튼 클릭 시 명확한 조작음 |
| 진동 | 버튼 클릭 시 진동 (`HapticPattern.LightPulse`) |

### 5단계: 도감 저장 및 연출

1. 확인 처리와 동시에 `AccountManager.AddToEncyclopedia(fishCatchData)` 호출
   - `FishCatchData` 구성: species, weight, caughtAt(현재시간), siteType
2. `AccountManager.SaveAccount()` 호출 (자동 저장 트리거 중 하나)
3. 물고기가 빛으로 변하며 수면 아래로 헤엄쳐 들어가는 시각 연출

| 피드백 유형 | 내용 |
|---|---|
| 시각 | 물고기 → 빛 변환 → 수면 아래로 이동하는 이펙트 |

### 6단계: 상태 복귀

1. 포획 결과 UI와 물고기 오브젝트가 화면에서 사라짐
2. `FishingRodController.CurrentState = RodState.Idle`
3. `GameFlowManager.TransitionTo(GameState.FishingReady)` → 시나리오 02로 복귀

## 속성 변화

| 속성 | 변화 내용 |
|---|---|
| `AccountDataSO.encyclopedia` | 새로 포획한 물고기 `FishCatchRecord`가 리스트에 추가 |
| `AccountDataSO.totalScore` | 물고기 가치(크기, 희귀도)에 따른 점수 가산 |
| `FishingRodController.CurrentState` | MiniGame → Idle |

## 사후 조건

1. 물고기 정보가 `AccountDataSO.encyclopedia`에 성공적으로 기록됨
2. 포획 결과 UI와 물고기 오브젝트가 화면에서 사라짐
3. `FishingRodController.CurrentState == RodState.Idle`
4. `GameFlowManager.CurrentState == GameState.FishingReady`
5. 시나리오 02(낚시 준비 및 캐스팅)를 다시 수행할 수 있는 상태

## 관련 SO Event 채널

| Event SO | 발행자 | 구독자 |
|---|---|---|
| `OnAccountSaved` (VoidEventSO) | AccountManager | (확인용 로깅) |
| `RodStateChanged` (VoidEventSO) | FishingRodController | FeedbackManager |

## 관련 SO Data

| SO | 사용 시스템 |
|---|---|
| `AccountDataSO` | AccountManager (도감 추가, 점수 갱신) |
| `GameSettingsSO` | 포획 결과 화면 (autoConfirmDelay) |

## 담당자

**담당자 A - 코어/계정 시스템** (도감 저장)
**담당자 E - 피드백/UI** (포획 결과 UI, 시각 연출)
