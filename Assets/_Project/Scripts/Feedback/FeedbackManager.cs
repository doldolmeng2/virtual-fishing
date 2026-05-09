using UnityEngine;
using VirtualFishing.Fishing;
using VirtualFishing.Interfaces;

namespace VirtualFishing.Feedback
{
    public class FeedbackManager : MonoBehaviour, IFeedbackService
    {
        [Header("Sub Systems")]
        [SerializeField] private SoundManager soundManager;
        [SerializeField] private HapticManager hapticManager;
        [SerializeField] private VisualEffectManager visualManager;
        [SerializeField] private TTSManager ttsManager;
        // UI 매니저는 구조에 따라 분리하거나 이곳에 통합 가능

        [Header("Game References")]
        [Tooltip("현재 상태를 읽어올 낚싯대 컨트롤러를 연결하세요")]
        public FishingRodController fishingRodController;

        [Header("Temporary UI References")]
        [Tooltip("Head-Locked 경고 캔버스 게임오브젝트를 여기에 넣으세요")]
        public GameObject safetyWarningPanel;
        [Tooltip("챔질 가이드 캔버스 게임오브젝트를 여기에 넣으세요")]
        public GameObject hookingGuidePanel;
        [Tooltip("포획 결과 캔버스 게임오브젝트를 여기에 넣으세요")]
        public GameObject catchResultPanel;
        [Tooltip("게임 종료 캔버스 게임오브젝트를 여기에 넣으세요")]
        public GameObject exitSequencePanel;
        [Tooltip("미니게임 종합 캔버스 게임오브젝트를 여기에 넣으세요")]
        public GameObject miniGamePanel;
        [Tooltip("장력 경고(빨간색 점멸 등) 캔버스 게임오브젝트를 여기에 넣으세요")]
        public GameObject tensionWarningPanel;
        [Tooltip("로딩 중 표시할 캔버스 게임오브젝트를 여기에 넣으세요")]
        public GameObject loadingPanel;

        #region 1. 시스템 및 초기화 이벤트

        public void OnAccountLoadedEvent()
        {
            PlaySound("LoginSuccess");
            HideUI("Loading"); // 로딩바 UI 끄기
            Debug.Log("<color=green>[피드백]</color> 계정 데이터 로드 완료");
        }

        public void OnCalibrationCompleteEvent()
        {
            PlayTTS("환경 설정이 완료되었습니다. 이제 낚시를 시작할 수 있습니다.");
            PlayHaptic(HapticPattern.StrongPulse, ControllerHand.Both);
            Debug.Log("<color=green>[피드백]</color> 캘리브레이션 완료");
        }

        public void OnSceneLoadedEvent()
        {
            // 낚시터 배경 환경음 재생 등
            Debug.Log("<color=green>[피드백]</color> 낚시터 현장 도착");
        }

        public void OnTrackingLostEvent()
        {
            hapticManager.StopAll();
            PlayTTS("컨트롤러 연결을 확인해주세요.");
            // 필요 시 일시정지 UI 팝업
            Debug.LogWarning("<color=red>[피드백]</color> 컨트롤러 트래킹 소실");
        }

        #endregion

        #region 2. 낚시 상호작용 이벤트

        public void OnRodGrabbedEvent()
        {
            PlaySound("RodAttach");
            PlayHaptic(HapticPattern.LightPulse, ControllerHand.Right);
            Debug.Log("<color=green>[피드백]</color> 낚싯대 장착 완료");
        }

        public void OnCastStartedEvent()
        {
            PlaySound("LineCast");
            PlayHaptic(HapticPattern.StrongPulse, ControllerHand.Right);
            Debug.Log("<color=green>[피드백]</color> 캐스팅 투척 시작");
        }

        public void OnWaterLandedEvent()
        {
            PlaySound("WaterSplash");
            ShowVisualEffect("Splash", Vector3.zero); // 실제론 찌 위치
            Debug.Log("<color=green>[피드백]</color> 찌가 수면에 착수함");
        }

        public void OnBiteOccurredEvent()
        {
            PlaySound("FloatSink");
            PlayHaptic(HapticPattern.StrongPulse, ControllerHand.Right);
            ShowUI("HookingGuide"); // 챔질 가이드 화살표 켜기
            Debug.Log("<color=green>[피드백]</color> 물고기 입질 발생!");
        }

        public void OnHookSuccessEvent()
        {
            HideUI("HookingGuide");
            PlaySound("HookSuccess");
            ShowVisualEffect("HookSuccess", Vector3.zero);
            PlayHaptic(HapticPattern.StrongPulse, ControllerHand.Right);
            Debug.Log("<color=green>[피드백]</color> 챔질 성공! 미니게임으로 진입합니다");
        }

        public void OnHookFailedEvent()
        {
            HideUI("HookingGuide");
            PlaySound("HookFail");
            PlayTTS("물고기가 미끼만 먹고 도망갔습니다.");
            ShowVisualEffect("FishEscape", Vector3.zero);
            Debug.Log("<color=green>[피드백]</color> 챔질 실패");
        }

        #endregion

        #region 3. 미니게임 및 보상 이벤트

        public void OnTensionChangedEvent(float tension)
        {
            // 장력이 80 이상이면 경고 피드백
            if (tension >= 80f)
            {
                ShowUI("TensionWarning");
                PlaySound("WarningBeep");
                PlayHaptic(HapticPattern.RhythmicWarning, ControllerHand.Both);
            }
            else
            {
                HideUI("TensionWarning");
            }
            Debug.Log($"<color=green>[피드백]</color> 현재 줄 장력: {tension}");
        }

        public void OnSuccessGaugeChangedEvent(float value)
        {
            // 게이지가 오를 때마다 미세한 진동 피드백 등을 줄 수 있음
            Debug.Log($"<color=green>[피드백]</color> 성공 게이지 변화: {value}");
        }

        public void OnMiniGameResultEvent()
        {
            // 성공/실패 여부는 MiniGameManager의 상태를 참조하거나 직전 Hook 상태를 통해 판단
            HideUI("MiniGamePanel");
            ShowUI("CatchResult"); // 포획 결과창 출력
            PlaySound("Fanfare");
            ShowVisualEffect("Fireworks", Vector3.zero);
            PlayTTS("축하합니다! 물고기를 낚으셨습니다.");
            Debug.Log("<color=green>[피드백]</color> 미니게임 종료 및 결과창 출력");
        }

        #endregion

        #region 4. 안전 및 종료 이벤트

        public void OnSafetyWarningEvent(int level)
        {
            SafetyWarningLevel warningLevel = (SafetyWarningLevel)level;
            Debug.Log($"<color=green>[피드백]</color> 안전 경고 이벤트 수신: {warningLevel}");

            switch (warningLevel)
            {
                case SafetyWarningLevel.None:
                    // 모든 경고 UI 끄기, 패스스루 끄기
                    visualManager.HideEffect("BlueGrid");
                    HideUI("SafetyWarning");
                    visualManager.ShowPassthrough(false);
                    hapticManager.StopAll();
                    visualManager.FadeScreen(0.0f, 0.5f);
                    break;

                case SafetyWarningLevel.NearBoundary:
                    // 바닥에 파란색 격자 표시
                    visualManager.ShowEffect("BlueGrid", Vector3.zero);
                    hapticManager.StopAll(); 
                    visualManager.FadeScreen(0.0f, 0.5f);
                    break;

                case SafetyWarningLevel.Outside:
                    // 시야 중앙에 붉은색 큰 팝업 및 중앙 유도 화살표 켜기
                    ShowUI("SafetyWarning");
                    PlaySound("WarningAlarm");
                    PlayTTS("위험합니다. 발자국을 따라 가운데로 오세요.");
                    PlayHaptic(HapticPattern.RhythmicWarning, ControllerHand.Both);
                    visualManager.FadeScreen(0.0f, 0.5f);
                    break;

                case SafetyWarningLevel.Emergency:
                    // 게임 화면 어둡게 페이드 아웃 후 패스스루 전환
                    HideUI("SafetyWarning");
                    visualManager.FadeScreen(0.9f, 1.0f); // 1초에 걸쳐 90% 어둡게
                    visualManager.ShowPassthrough(true);
                    PlayTTS("안전을 위해 게임을 멈춥니다. 주변을 확인하세요.");
                    break;
            }
            Debug.Log($"<color=green>[피드백]</color> 안전 경고 단계 변경: {warningLevel}");
        }

        public void OnAccountSavedEvent()
        {
            // 저장이 완료되면 종료 UI를 띄움
            ShowUI("ExitSequence");
            PlaySound("SaveComplete");
            Debug.Log("<color=green>[피드백]</color> 데이터 저장 완료 및 종료 준비");
        }

        public void OnRodStateChangedEvent()
        {
            // 상태 로깅용 (필요 시 특정 상태에 대한 추가 피드백 구현)
        }

        #endregion

        #region IFeedbackService 구현 (위임)
        public void PlaySound(string soundId) => soundManager.PlayWithId(soundId);
        public void PlayHaptic(HapticPattern pattern, ControllerHand hand) => hapticManager.Play(pattern, hand);
        public void ShowVisualEffect(string effectId, Vector3 position) => visualManager.ShowEffect(effectId, position);
        public void PlayTTS(string message) => ttsManager.Speak(message);
        public void ShowUI(string uiId, object data = null)
        {
            GameObject target = GetUIPanel(uiId);
            if (target != null)
            {
                target.SetActive(true);
                if (uiId == "CatchResult")
                {
                    var ctrl = target.GetComponent<CatchResultController>();
                    if (ctrl != null) ctrl.DisplayResult("참돔", 45.2f, 3.1f, 4);
                }
            }
        }
        public void HideUI(string uiId)
        {
            GameObject target = GetUIPanel(uiId);
            if (target != null) target.SetActive(false);
        }

        private GameObject GetUIPanel(string id) => id switch
        {
            "SafetyWarning" => safetyWarningPanel,
            "HookingGuide" => hookingGuidePanel,
            "CatchResult" => catchResultPanel,
            "ExitSequence" => exitSequencePanel,
            "MiniGamePanel" => miniGamePanel,
            "TensionWarning" => tensionWarningPanel,
            "Loading" => loadingPanel,
            _ => null
        };

        #endregion
    }
}