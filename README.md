# virtual-fishing
가상 및 증강현실 프로그래밍-오늘도 월척

---

## 빌드/실행 환경

| 항목 | 값 |
|------|-----|
| Unity 버전 | 6000.3.10f1 |
| Render Pipeline | URP |
| XR Plugin | OpenXR 1.16.1, Meta OpenXR 2.4.0 |
| 최소 SDK / API Level | Android 10.0 |

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


## 에셋 크레딧 / 라이선스

프로젝트에 사용된 외부(서드파티) 에셋과 라이선스입니다. **CC BY** 에셋은 저작자 표시가 의무이므로, 최종 빌드의 크레딧 화면(또는 본 문서)에 출처가 반드시 유지되어야 합니다. CC0는 표시 의무가 없습니다.

| 에셋 | 종류 | 경로 | 저작자 | 라이선스 |
|------|------|------|--------|----------|
| Fishing Rod, Rigged and Animated (`rod06`) | 3D 모델 + PBR 텍스처 | `_Project/Models/FishingRod/` | Ergin ERYILDIR (ergin3d.com) | **CC BY 4.0** (저작자 표시 필수) |
| Kloppenheim 05 Pure Sky | HDRI(4K JPG 파노라마) | `Art/Environment/Backdrops/` | Greg Zaal, Jarod Guest (Poly Haven) | CC0 |
| Simple Water Shader | 워터 셰이더(ShaderGraph) | `Art/Environment/Water/Simple Water Shader/` | _(확인 필요)_ | _(확인 필요)_ |
| NamuFX — Stylized Water Effects | 워터 VFX | `Art/Environment/Water/NamuFX/` | _(확인 필요)_ | _(확인 필요)_ |
| PurePoly (`PP_*`) Low-Poly Nature | 환경 모델 | `Art/Environment/Pond/Models/PurePoly_Selected/` | _(확인 필요)_ | _(확인 필요)_ |
| Pack_FREE_Trees | 로우폴리 나무 | `Art/Environment/Pond/Models/Pack_FREE_Trees/` | _(확인 필요)_ | _(확인 필요)_ |

### 필수 저작자 표시 (CC BY 4.0)

> This work is based on "Fishing Rod, Rigged and Animated"
> (https://sketchfab.com/3d-models/fishing-rod-rigged-and-animated-78991bf44cf54acb8660bef317d72c7a)
> by Ergin ERYILDIR (https://sketchfab.com/ergin3d.com)
> licensed under CC-BY-4.0 (https://creativecommons.org/licenses/by/4.0/).

각 모델 폴더의 `*_ATTRIBUTION.txt`에 개별 출처를 함께 보관합니다 — 예: `_Project/Models/FishingRod/rod06_ATTRIBUTION.txt`, `Art/Environment/Backdrops/fish_eagle_hill_polyhaven_ATTRIBUTION.txt`.

> _(확인 필요)_ 로 표시된 에셋은 출처/라이선스 문서가 아직 폴더에 없습니다. Asset Store / 다운로드 출처를 확인해 채워주세요.


## 기타

- 이 부분에는 자기가 생각할 때 이런 규칙이 있으면 효율적이겠다 싶은 것들 적어주시면 됩니다. (예를 들어 저희 깃헙 레포나 브랜치 규칙)
- 기본적으로 UML 시각화나 .md 파일은 제가 작성한 걸 토대로 AI한테 작성시킨거라 이상한 부분이나 수정할 부분 있으면 그 부분도 적어주세요.
