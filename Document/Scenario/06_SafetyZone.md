# 시나리오 06 - 안전 구역 이탈 감지

## 도메인

| 도메인 | 클래스 / 인터페이스 | 역할 |
|---|---|---|
| 플레이어 | `PlayerSafetyMonitor` / `ISafetyMonitor` | 실시간 위치 모니터링, 경고 레벨 판정 |
| 플레이어 데이터 | `PlayerDataSO` | 중심 좌표, 안전 반경 데이터 |
| 피드백 | `FeedbackManager` / `IFeedbackService` | 사운드, 진동, 시각효과, TTS 통합 Facade |
| 게임 흐름 | `GameFlowManager` / `IGameFlowManager` | 게임 상태 전이 (Warning, Paused) |

> **이 시나리오는 다른 모든 시나리오와 병렬로 실행됩니다.** 게임이 진행 중인 한 `PlayerSafetyMonitor`는 항상 백그라운드에서 위치를 감시합니다.

## 사전 조건

1. 시나리오 01(캘리브레이션)을 통해 `PlayerDataSO.safetyRadius`가 설정됨
2. `PlayerDataSO.currentPosition`에 안전구역 중심점이 저장됨
3. 게임이 진행 중이며 HMD 및 컨트롤러가 정상 트래킹 중
4. `PlayerSafetyMonitor.StartMonitoring()`이 호출된 상태

## 로직

### 실시간 모니터링

> **개선사항 (성능 최적화)**: 원본 명세의 "매 프레임 실시간" 체크를 `GameSettingsSO.safetyCheckInterval`(기본 0.2초) 간격의 **코루틴 기반** 체크로 변경합니다.

```
IEnumerator MonitorLoop:
  while (isMonitoring):
    HMD의 x, z 좌표로 PlayerDataSO.currentPosition 업데이트
    distance = Distance(currentPosition, centerPosition)
    경고 레벨 판정
    yield return new WaitForSeconds(safetyCheckInterval)
```

### 경고 레벨 체계

| 레벨 | 조건 | 설명 |
|---|---|---|
| `None` | distance < safetyRadius - nearBoundaryDistance | 안전 영역 내 |
| `NearBoundary` | distance >= safetyRadius - nearBoundaryDistance | 경계선 근접 (기본: 반경 - 0.3m) |
| `Outside` | distance >= safetyRadius | 구역 이탈 |
| `Emergency` | Outside 상태가 emergencyTimeout 초과 | 장시간 이탈 (기본: 10초) |

경고 레벨이 변경될 때마다 `<<IntEventSO>> SafetyWarning` 이벤트 발행

### 1차 경고: NearBoundary (경계선 근접)

플레이어가 안전 구역 경계선에 근접(기본: 반경 30cm 이내)하면 은은한 시각 피드백을 제공합니다.

| 피드백 유형 | 내용 |
|---|---|
| 시각 | 바닥에 은은한 **푸른색 격자무늬** 가이드라인을 서서히 표시 |

> 어르신들이 놀라지 않도록 갑작스러운 알림 대신 시각적 힌트만 제공합니다.

### 2차 경고: Outside (구역 이탈)

플레이어가 안전 구역을 완전히 이탈하면 즉각적인 경고 피드백을 실행합니다.

1. `GameFlowManager.TransitionTo(GameState.Warning)` — 현재 게임 상태를 백업 후 경고 상태 전환
2. 이탈 타이머 시작 (`emergencyTimeout` 카운트다운)

| 피드백 유형 | 내용 |
|---|---|
| 시각 | 화면 중앙에 크고 명확한 **붉은색 경고 아이콘** (발자국 모양) + "안전 구역을 벗어났습니다. 중앙으로 돌아와 주세요" 텍스트 |
| TTS | "위험합니다. 안전을 위해 제자리로 돌아와 주세요" — 부드럽지만 단호한 톤, **반복 출력** |
| 진동 | 지속적이고 규칙적인 진동 (`HapticPattern.RhythmicWarning`, 양쪽 컨트롤러) |
| 사운드 | 뚜렷한 경고음 |

### 3차 대응: Emergency (장시간 이탈 — 안전사고 방지)

플레이어가 경고를 무시하고 `emergencyTimeout`(기본 10초) 이상 구역 밖에 머무르거나, HMD 트래킹이 심하게 벗어날 경우:

1. `GameFlowManager.TransitionTo(GameState.Paused)` — 게임 즉시 일시 정지
2. 가상현실 화면을 **서서히** 어둡게 처리
3. HMD 외부 카메라를 통해 **패스스루(Passthrough) 화면**으로 자동 전환
   - 어르신이 주변 장애물을 육안으로 확인할 수 있도록 현실 공간 표시

| 피드백 유형 | 내용 |
|---|---|
| 시각 | 화면 Fade-out (80% 어둡게) → 패스스루 전환 |
| TTS | "위험합니다. 제자리로 돌아와 주세요" |

### 안전 구역 복귀 시

#### Warning 상태에서 복귀
1. 경고 UI 숨김
2. 진동·경고음 중지
3. `GameFlowManager`가 백업해둔 이전 상태로 복원 (낚시 대기, 미니게임 등)

#### Paused (패스스루) 상태에서 복귀
1. 패스스루 화면 해제
2. '게임 계속하기' 버튼 표시
3. 플레이어가 버튼을 누르거나, 복지관 관리자가 확인할 때까지 대기
4. 확인 후 `GameFlowManager`가 이전 상태로 복원

## 속성 변화

| 속성 | 변화 내용 |
|---|---|
| `PlayerDataSO.currentPosition` | HMD의 실시간 x, z 좌표에 따라 주기적 업데이트 |
| `PlayerSafetyMonitor.CurrentWarningLevel` | None / NearBoundary / Outside / Emergency 전환 |
| `GameFlowManager.CurrentState` | 정상 진행 → Warning → Paused (레벨에 따라) |

## 사후 조건

### 안전 구역 복귀 시
1. 게임이 이전 상태(낚시 대기, 미니게임 등)로 정상 복귀
2. 경고 UI 및 효과 모두 해제

### 패스스루 모드 전환 시
1. 플레이어가 제자리로 돌아와 '게임 계속하기' 확인 시까지 대기
2. 또는 복지관 관리자의 확인이 있을 때까지 대기

## 관련 SO Event 채널

| Event SO | 발행자 | 구독자 |
|---|---|---|
| `SafetyWarning` (IntEventSO) | PlayerSafetyMonitor | GameFlowManager, FeedbackManager |

## 관련 SO Data

| SO | 사용 시스템 |
|---|---|
| `PlayerDataSO` | PlayerSafetyMonitor (currentPosition, safetyRadius 읽기) |
| `GameSettingsSO` | PlayerSafetyMonitor (safetyCheckInterval, nearBoundaryDistance, emergencyTimeout) |

## 담당자

**담당자 E - 피드백/안전/UI 시스템**
