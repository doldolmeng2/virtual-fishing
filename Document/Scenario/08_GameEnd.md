# 시나리오 08 - 게임 종료

## 도메인

| 도메인 | 클래스 / 인터페이스 | 역할 |
|---|---|---|
| 계정 | `AccountManager` / `IAccountService` | 최종 데이터 저장 (도감, 점수, 플레이 시간) |
| 피드백 | `FeedbackManager` / `IFeedbackService` | 사운드, 진동, 시각효과, TTS 통합 Facade |
| 게임 흐름 | `GameFlowManager` / `IGameFlowManager` | 게임 상태 전이 (ExitSequence) |

## 사전 조건

1. 게임이 정상적으로 실행 중이며 플레이어가 메뉴 UI를 호출할 수 있는 상태
2. 디바이스의 로컬 또는 클라우드 저장소에 접근 권한이 확보됨
3. `GameFlowManager.CurrentState`가 Paused 또는 ExitSequence가 아닌 정상 진행 상태

## 로직

### 1단계: 종료 요청

1. 플레이어가 손목 UI 또는 가상 공간의 메인 메뉴에서 **가장 크고 눈에 띄는 색상**의 '게임 종료' 버튼을 선택

| 피드백 유형 | 내용 |
|---|---|
| 사운드 | 버튼 선택 시 명확한 조작음 (딸깍) |
| 진동 | 부드러운 진동 (`HapticPattern.LightPulse`) |

### 2단계: 데이터 저장

1. `GameFlowManager.TransitionTo(GameState.ExitSequence)` — 게임 진행 일시 정지
2. `AccountManager.SaveAccount()` 호출:
   - 이번 세션에서 획득한 도감 데이터 영구 기록(Commit)
   - 누적 점수 동기화
   - `AccountDataSO.lastPlayedAt`을 현재 시스템 시간으로 업데이트

| 피드백 유형 | 내용 |
|---|---|
| 시각 | 화면 중앙에 "기록을 안전하게 저장하고 있습니다..." 텍스트 + 회전 로딩 아이콘 |
| TTS | "기록을 안전하게 저장하고 있습니다" |

3. 저장 완료 시 `<<VoidEventSO>> OnAccountSaved` 발행

### 3단계: 화면 이탈 시퀀스 (VR 멀미 방지)

> **어르신들의 시각적 충격과 어지럼증(VR 멀미)을 예방하기 위한 단계적 전환**

1. 화면 전체를 **갑자기 끄지 않고**, 주변 풍경만 **서서히** 어둡게(Fade-out) 처리
   - `IVisualFeedback.FadeScreen(0.7, 3.0)` — 3초에 걸쳐 70% 어둡게
2. 플레이어가 **평형감각을 유지**할 수 있도록 다음 요소는 반드시 유지:
   - 발밑의 가상 지면 (그리드 또는 바닥 텍스처)
   - 희미한 배경광
3. 안전 구역 중앙에 안내 문구 표시

| 피드백 유형 | 내용 |
|---|---|
| 시각 | 풍경 서서히 Fade-out (바닥+배경광 유지) |
| 시각 | "게임이 모두 저장되었습니다" 안내 문구 |
| TTS | "VR 기기를 천천히 벗고, 주변에 부딪힐 물건이 없는지 확인한 뒤에 움직여주세요. 수고하셨습니다." — 차분하고 친절한 톤 |

### 4단계: 최종 종료

1. TTS 안내가 끝난 후 **'완전 종료'** 버튼 표시
2. 확인 트리거:
   - **수동**: 플레이어가 버튼을 누르면 즉시 종료
   - **자동**: `GameSettingsSO.autoConfirmDelay`(기본 10초) 경과 시 자동 종료
3. `Application.Quit()` 호출 → 앱 안전하게 종료

### 예외 흐름: 비정상 종료

배터리 방전, 기기 오류, 또는 플레이어가 강제로 HMD를 벗어 트래킹이 완전히 소실된 경우:

**자동 저장 트리거 시점:**

| 트리거 | 설명 |
|---|---|
| 물고기 포획 완료 시 | 시나리오 05의 도감 저장 단계에서 자동 실행 |
| 주기적 인터벌 | `GameSettingsSO.autoSaveInterval`(기본 120초)마다 실행 |
| 씬 전환 시 | 낚시터 변경 등 씬 전환 직전에 실행 |
| 게임 상태 변경 시 | `GameState`가 전환될 때 실행 |

비정상 종료 시 시스템은 가장 최근의 자동 저장 데이터를 호출하여 손실을 최소화합니다. 최악의 경우에도 마지막 자동 저장 시점까지의 데이터는 보존됩니다.

## 속성 변화

| 속성 | 변화 내용 |
|---|---|
| `AccountDataSO.lastPlayedAt` | 게임 최종 종료 시점의 시스템 시간으로 업데이트 |
| `AccountDataSO.encyclopedia` | 이번 세션 획득 도감 정보가 영구 기록(Commit) |
| `AccountDataSO.totalScore` | 최종 점수 동기화 |
| `GameFlowManager.CurrentState` | ExitSequence → 프로세스 종료 |

## 사후 조건

1. 플레이어의 모든 진행 데이터가 안전하게 저장소에 보존됨
2. VR 애플리케이션 프로세스가 완전히 종료됨
3. 기기의 기본 홈 화면으로 안전하게 복귀함

## 관련 SO Event 채널

| Event SO | 발행자 | 구독자 |
|---|---|---|
| `OnAccountSaved` (VoidEventSO) | AccountManager | GameFlowManager (저장 완료 확인 후 다음 단계 진행) |

## 관련 SO Data

| SO | 사용 시스템 |
|---|---|
| `AccountDataSO` | AccountManager (최종 저장) |
| `GameSettingsSO` | GameFlowManager (autoConfirmDelay, autoSaveInterval) |

## 담당자

**담당자 A - 코어/계정 시스템** (데이터 저장, 자동 저장 로직)
**담당자 E - 피드백/UI** (종료 시퀀스 연출, 패스스루)
