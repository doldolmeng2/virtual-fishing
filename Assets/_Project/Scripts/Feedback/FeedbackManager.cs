using UnityEngine;
using VirtualFishing.Fishing;
using VirtualFishing.Fishing.Events;
using VirtualFishing.Interfaces;
using VirtualFishing.MiniGame;
using VirtualFishing.Core.Fish;
using VirtualFishing.Data;

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

        [Tooltip("물고기 데이터를 읽어올 미니게임 매니저를 연결하세요")]
        public MiniGameManager miniGameManager;

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
        [Tooltip("낚싯대의 위치를 표시해주는 UI를 여기에 넣으세요")]
        public GameObject rodGrabGuidePanel;

        private bool _hasPlayedGrabFeedback = false;

        #region 1. 시스템 및 초기화 이벤트

        public void OnAccountLoadedEvent()
        {
            PlaySound("LoginSuccess");
            HideUI("Loading"); // 로딩바 UI 끄기
            Debug.Log("<color=green>[피드백]</color> 계정 데이터 로드 완료");
        }

        public void OnCalibrationCompleteEvent()
        {
            PlayTTS("환경 설정이 완료되었습니다. 컨트롤러를 움직여 낚싯대에 두고 버튼을 눌러 낚싯대를 잡아주세요.");
            PlayHaptic(HapticPattern.StrongPulse, ControllerHand.Both);
            ShowUI("RodGrabGuide");
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

        // [와이어링 — 낚싯대가 실제 발행하는 기존 이벤트만 사용]
        //  · onRodStateChanged (RodStateTransitionEventSO) → OnRodStateChangedEvent(transition)
        //      └ 전용 void 이벤트가 없는 '낚싯대 장착(Idle→Attached)' 피드백을 상태 전이로부터 감지
        //  · onCastingStarted  (VoidEventSO) → OnCastingStartedEvent
        //  · onBiteOccurredEvent (VoidEventSO) → OnBiteOccurredEvent  (낚싯대와 공유 구독)
        //  · onHookingSuccess  (VoidEventSO) → OnHookingSuccessEvent
        //  · onHookingFailed   (VoidEventSO) → OnHookingFailedEvent
        //  ※ '찌 착수(OnWaterLandedEvent)'는 FloatController.onWaterLanded(VoidEventSO)가 직접 발행하므로
        //    여기서 다루지 않는다. (상태 전이로도 처리하면 착수 피드백이 이중 발행됨)

        /// <summary>
        /// FishingRodController.onRodStateChanged(RodStateTransitionEventSO)를
        /// RodStateTransitionEventListener를 통해 받는 진입점.
        /// 낚싯대가 별도 void 이벤트로 발행하지 않는 '장착' 피드백을 상태 전이(Idle→Attached)에서 끌어낸다.
        /// (캐스팅·챔질 성공/실패·입질·착수는 각자 전용 이벤트로 직접 수신하므로 여기서 다루지 않음 — 중복 방지)
        /// </summary>
        public void OnRodStateChangedEvent(RodStateTransition transition)
        {
            if (transition.Previous == RodState.Idle && transition.Current == RodState.Attached)
            {
                OnRodGrabbedEvent();
            }
        }

        public void OnRodGrabbedEvent()
        {
            HideUI("RodGrabGuide");
            PlaySound("RodAttach");
            PlayHaptic(HapticPattern.LightPulse, ControllerHand.Right);
            PlayTTS("낚싯대를 잡았습니다. 컨트롤러를 꽉 쥐고 앞으로 힘껏 휘둘러 찌를 던져보세요.");
            Debug.Log("<color=green>[피드백]</color> 낚싯대 장착 완료 - 캐스팅 안내");
        }

        public void OnRodStateChangedEvent()
        {
            if (fishingRodController == null) return;

            // 낚싯대 컨트롤러에서 현재 상태를 직접 읽어옵니다.
            RodState currentState = fishingRodController.CurrentState;
            Debug.Log($"<color=cyan>[피드백]</color> 낚싯대 상태 변경 감지: {currentState}");

            switch (currentState)
            {
                case RodState.Idle:
                    // 낚싯대를 놓았을 때: 중복 방지 변수 초기화
                    _hasPlayedGrabFeedback = false;
                    break;

                case RodState.Attached:
                    // 낚싯대를 잡았을 때
                    TriggerGrabFeedback();
                    break;

                case RodState.Casting:
                    // 투척 중 (기존 OnCastStartedEvent와 동일하게 연동 가능)
                    // PlaySound("LineCast"); 
                    break;

                case RodState.WaitingForBite:
                    // 찌가 물에 닿고 입질을 기다리는 상태
                    break;

                case RodState.Hit:
                    // 챔질에 성공한 직후 (기존 OnHookSuccessEvent와 동일하게 연동 가능)
                    break;

                case RodState.MiniGame:
                    // 미니게임 진행 중
                    break;
            }
        }

        // ★ 신규 추가됨: 실제 낚싯대 잡기 피드백을 실행하는 핵심 함수
        private void TriggerGrabFeedback()
        {
            // 이미 피드백이 재생되었다면 무시하여 TTS나 사운드가 겹치는 것을 막습니다.
            if (_hasPlayedGrabFeedback) return; 
            _hasPlayedGrabFeedback = true;

            HideUI("RodGrabGuide");
            PlaySound("RodAttach");
            PlayHaptic(HapticPattern.LightPulse, ControllerHand.Right);
            PlayTTS("낚싯대를 잡았습니다. 컨트롤러를 꽉 쥐고 앞으로 힘껏 휘둘러 찌를 던져보세요.");
            
            Debug.Log("<color=green>[피드백]</color> 낚싯대 장착 완료 - 캐스팅 안내 (통합 로직 실행됨)");
        }

        public void OnCastingStartedEvent()
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

        public void OnHookingSuccessEvent()
        {
            HideUI("HookingGuide");
            PlaySound("HookSuccess");
            ShowVisualEffect("HookSuccess", Vector3.zero);
            PlayHaptic(HapticPattern.StrongPulse, ControllerHand.Right);
            Debug.Log("<color=green>[피드백]</color> 챔질 성공! 미니게임으로 진입합니다");
        }

        public void OnHookingFailedEvent()
        {
            HideUI("HookingGuide");
            PlaySound("HookFail");
            PlayTTS("물고기가 도망갔습니다.");
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

        public void OnFishCaughtEvent()
        {
            hapticManager.Stop(ControllerHand.Both);

            HideUI("MiniGamePanel");
            HideUI("TensionWarning"); 
            
            // 미니게임 매니저에서 방금 잡은 물고기 데이터를 가져옴
            object catchData = null;
            if (miniGameManager != null)
            {
                catchData = miniGameManager.CurrentFishData;
            }

            ShowUI("CatchResult", catchData); 
            PlaySound("Fanfare");
            ShowVisualEffect("Fireworks", Vector3.zero);
            PlayTTS("축하합니다! 물고기를 낚으셨습니다.");
            
            Debug.Log("<color=green>[피드백]</color> 미니게임 성공: OnFishCaughtEvent 수신 및 실제 데이터 출력");
        }

        public void OnLineBreakEvent()
        {
            hapticManager.Stop(ControllerHand.Both);

            HideUI("MiniGamePanel");
            HideUI("TensionWarning"); // 텐션 100이었으므로 무조건 켜져있을 경고 끄기
            
            PlaySound("LineBreak"); // 줄 끊어지는 팽팽한 타격음
            PlayHaptic(HapticPattern.StrongPulse, ControllerHand.Both); // 팅! 하는 강한 진동
            PlayTTS("힘을 버티지 못하고 낚싯줄이 끊어졌습니다.");
            
            Debug.Log("<color=red>[피드백]</color> 미니게임 실패: OnLineBreakEvent 수신 (줄 끊김)");
        }

        public void OnFishEscapedEvent()
        {
            hapticManager.Stop(ControllerHand.Both);

            HideUI("MiniGamePanel");
            HideUI("TensionWarning");
            
            PlaySound("HookFail"); // 기존 실패 사운드 재사용
            ShowVisualEffect("FishEscape", Vector3.zero);
            PlayTTS("줄이 너무 느슨해져서 물고기가 도망갔습니다.");
            
            Debug.Log("<color=orange>[피드백]</color> 미니게임 실패: OnFishEscapedEvent 수신 (도망감)");
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
                    PlayTTS("이동하시면 위험합니다. 가운데로 돌아가주세요.");
                    PlayHaptic(HapticPattern.RhythmicWarning, ControllerHand.Both);
                    visualManager.FadeScreen(0.0f, 0.5f);
                    break;

                case SafetyWarningLevel.Emergency:
                    // 게임 화면 어둡게 페이드 아웃 후 패스스루 전환
                    HideUI("SafetyWarning");
                    visualManager.FadeScreen(0.9f, 1.0f); // 1초에 걸쳐 90% 어둡게
                    visualManager.ShowPassthrough(true);
                    PlayTTS("안전을 위해 게임을 멈춥니다. 장비를 벗고 주변을 확인해주세요.");
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
            // 레거시 void 디버그 채널용 no-op (FishingEventDebugTester의 void onRodStateChanged).
            // 실제 낚싯대 상태 전이 피드백은 RodStateTransition을 받는 오버로드(region 2)에서 처리.
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
                    if (ctrl != null) 
                    {
                        // data가 FishCatchData 타입이고, species 정보가 비어있지 않은지 확인
                        if (data is FishCatchData fishData && fishData.species != null)
                        {
                            // 1. 데이터 구조에서 값 꺼내기
                            string fishName = fishData.species.DisplayName; // 어종 이름
                            float fishWeight = fishData.weight;             // 무게 (kg)
                            int fishStars = fishData.species.Rarity;        // 희귀도 (별 등급)
                            
                            // 2. 크기(Size)는 현재 잡힌 데이터(FishCatchData)에 없으므로,
                            // 원본 데이터(FishSpeciesDataSO)의 범위 내에서 임시로 랜덤 값을 가져옵니다.
                            float fishSize = fishData.species.GetRandomSizeCm();

                            // 3. UI 컨트롤러에 실제 데이터 전달
                            // (소수점 1자리까지만 깔끔하게 보이게 하려면 ToString("F1") 처리를 UI 쪽에서 하시면 됩니다)
                            ctrl.DisplayResult(fishName, fishSize, fishWeight, fishStars); 
                        }
                        else
                        {
                            // 데이터가 제대로 넘어오지 않았을 때의 예비용(Fallback)
                            ctrl.DisplayResult("알 수 없는 물고기", 0.0f, 0.0f, 1);
                        }
                    }
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
            "RodGrabGuide" => rodGrabGuidePanel,
            _ => null
        };

        #endregion
    }
}