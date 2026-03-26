# virtual-fishing
가상 및 증강현실 프로그래밍-오늘도 월척

---

## 빌드/실행 환경

| 항목 | 값 |
|------|-----|
| Unity 버전 | 6000.3.10f1 |
| Render Pipeline | |
| XR Plugin | |
| 최소 SDK / API Level | |

> 이 부분 일단 유니티 버전만 적어놓을테니까 아무나 적절한 버전으로 설정해주세요. 다 설정됐다면 이 주석부분 제거 부탁드려요

---

## 폴더 구조

```
Assets/
├── _Project/
│   ├── Scripts/
│   │   ├── Core/              # GameFlowManager, Enum, Event SO 인프라 (A)
│   │   ├── Interfaces/        # 모든 인터페이스 정의 (공용)
│   │   ├── Data/              # ScriptableObject 클래스 정의, 구조체 (공용)
│   │   ├── Account/           # AccountManager (A)
│   │   ├── Calibration/       # CalibrationController (B)
│   │   ├── Fishing/           # FishingRod, Float, FishingLine, CastingZone (B)
│   │   ├── Fish/              # FishController, FishSpawner (C)
│   │   ├── MiniGame/          # MiniGameManager, TensionCalculator (D)
│   │   ├── Feedback/          # FeedbackManager, Sound/Haptic/Visual/TTS (E)
│   │   ├── Safety/            # PlayerSafetyMonitor (E)
│   │   └── UI/                # 공통 UI 스크립트 (E)
│   ├── Prefabs/
│   │   ├── Core/              # GameFlowManager, SceneTransition 프리팹 (A)
│   │   ├── Fishing/           # 낚싯대, 찌, 줄, 캐스팅존 프리팹 (B)
│   │   ├── Fish/              # 어종별 물고기 프리팹 (C)
│   │   ├── Environment/       # 수면, 낚시터 환경 프리팹 (C)
│   │   ├── MiniGame/          # 미니게임 UI 프리팹 (D)
│   │   ├── Feedback/          # VFX, 파티클 프리팹 (E)
│   │   └── UI/                # 공통 UI 프리팹 (E)
│   ├── Scenes/
│   │   ├── Dev_Core.unity          # A 개발용
│   │   ├── Dev_FishingRod.unity    # B 개발용
│   │   ├── Dev_Fish.unity          # C 개발용
│   │   ├── Dev_MiniGame.unity      # D 개발용
│   │   ├── Dev_Feedback.unity      # E 개발용
│   │   └── Main_FishingSite.unity  # 통합 씬
│   ├── SO/
│   │   ├── Events/            # VoidEventSO, FloatEventSO 등 이벤트 에셋
│   │   ├── Data/              # PlayerDataSO, AccountDataSO 등 데이터 에셋
│   │   ├── FishDB/            # FishSpeciesDataSO, FishDatabaseSO 에셋
│   │   └── Settings/          # GameSettingsSO, MiniGameSettingsSO 에셋
│   ├── Materials/             # 머티리얼
│   └── Animations/            # 애니메이션 클립, 컨트롤러
├── Art/
│   ├── Audio/                 # 사운드, BGM
│   ├── Models/                # 3D 모델 (FBX 등)
│   ├── Textures/              # 텍스처, 스프라이트
│   └── VFX/                   # 파티클 소스, 셰이더 그래프
├── Plugins/                   # 서드파티 플러그인
└── Settings/                  # URP, Input System 등 프로젝트 설정
```

> 인터페이스나 이벤트같은 설계 규칙은 PM이나 팀원과의 상의 후, 카톡이나 깃헙에 알려주신 뒤 변경해주시면 좋겠습니다.

---

## 팀 역할 분배

| 담당 | 역할 | 작업 씬 | 담당 시스템 / 프리팹 | 주요 인터페이스 | 담당 SO |
|------|------|---------|---------------------|----------------|---------|
| **A — 코어 + 계정** | 게임 흐름 허브, 계정, 통합 조율 | `Dev_Core` | `GameFlowManager`, `SceneTransitionService`, `AccountManager`, 계정 선택 UI, Event SO 인프라 | `IGameFlowManager`, `ISceneService`, `IAccountService` | `GameSettingsSO`, `AccountDataSO`, `PlayerDataSO`, Event SO 전체 |
| **B — 낚시 + 캘리** | 컨트롤러 입력, 낚시 메커니즘, 캘리브레이션 | `Dev_FishingRod` | `FishingRod`, `FishingLine`, `Float`, `CastingZone`, `CalibrationController` | `IFishingRod`, `IGrabbable`, `ICastable`, `IFishingFloat`, `ICalibrationService` | `GameSettingsSO` (캐스팅/챔질 설정 읽기) |
| **C — 물고기 + 환경** | 어종, 스폰, 수면, 낚시터 환경, 서브 문서 작성 | `Dev_Fish` | `Fish` (각 어종), `FishSpawner`, `WaterSurface`, `FishingSiteEnv` | `IFish`, `IFishSpawner` | `FishSpeciesDataSO`, `FishDatabaseSO`, `FishingSiteDataSO` |
| **D — 미니게임 + 발표/문서** | 미니게임 로직, 발표 준비, 문서 정리 | `Dev_MiniGame` | `MiniGameManager`, `TensionCalculator`, 미니게임 UI (텐션 게이지, 성공 게이지, 타이머) | `IMiniGame`, `ITensionCalculator` | `TensionDataSO`, `MiniGameSettingsSO` |
| **E — 피드백 + 안전 + UI** | 사운드/진동/VFX/TTS, 안전 모니터, 공통 UI | `Dev_Feedback` | `FeedbackManager`, `SafetyMonitor`, 각종 UI/VFX 프리팹 | `IFeedbackService`, `ISoundFeedback`, `IHapticFeedback`, `IVisualFeedback`, `ITTSFeedback`, `ISafetyMonitor` | SoundDatabase, HapticPatterns |

### 작업 규칙

- 각자 **별도의 개발 씬**에서 프리팹 단위로 작업 후 통합 씬에서 병합
- 프리팹 간 직접 참조 금지 → **SO Event 채널**로만 통신
- 설정값 하드코딩 금지 → **ScriptableObject**에 저장
- 각 프리팹은 `[SerializeField]`로 SO를 참조 → 인스펙터에서 연결
- 기본적으로 깃헙으로 버전관리를 하되, 대용량 파일은 LFS 사용 (Git LFS 설치 후 git lfs pull해서 파일 받기)


## 기타

- 이 부분에는 자기가 생각할 때 이런 규칙이 있으면 효율적이겠다 싶은 것들 적어주시면 됩니다. (예를 들어 저희 깃헙 레포나 브랜치 규칙)
- 기본적으로 UML 시각화나 .md 파일은 제가 작성한 걸 토대로 AI한테 작성시킨거라 이상한 부분이나 수정할 부분 있으면 그 부분도 적어주세요.