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

        [Tooltip("안전 구역 모니터링 시스템을 연결하세요")]
        public VirtualFishing.Safety.PlayerSafetyMonitor safetyMonitor;

        [Tooltip("캘리브레이션된 중심점을 읽어올 PlayerDataSO를 연결하세요")]
        public PlayerDataSO playerData;

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

        private void Start() {
            PlayBGM("lake-waves");
        }

        #region 1. 시스템 및 초기화 이벤트

        public void OnAccountLoadedEvent()
        {
            PlaySound("LoginSuccess");
            HideUI("Loading"); // 로딩바 UI 끄기
            Debug.Log("<color=green>[피드백]</color> 계정 데이터 로드 완료");
        }

        public void OnCalibrationCompleteEvent()
        {
            PlayHaptic(HapticPattern.StrongPulse, ControllerHand.Both);
            Debug.Log("<color=green>[피드백]</color> 캘리브레이션 완료");
            HideUI("RodGrabGuide");
            if (safetyMonitor != null)
            {
                safetyMonitor.StartMonitoring();
                Debug.Log("<color=green>[피드백]</color> 플레이어 안전 모니터링 가동 시작");
            }
            else
            {
                Debug.LogWarning("<color=orange>[피드백]</color> PlayerSafetyMonitor가 연결되지 않아 안전 모니터링을 시작할 수 없습니다.");
            }
        }

        public void OnSceneLoadedEvent()
        {
            Debug.Log("<color=green>[피드백]</color> 낚시터 현장 도착");
            // PlayTTS("환경 설정이 완료되었습니다. 컨트롤러를 움직여 낚싯대에 두고 버튼을 눌러 낚싯대를 잡아주세요.");
            // ShowUI("RodGrabGuide");
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
        public void OnRodGrabbedEvent()
        {
            PlaySound("RodAttach");
            PlayHaptic(HapticPattern.LightPulse, ControllerHand.Right);
            PlayTTS("낚싯대를 잡았습니다. 컨트롤러를 꽉 쥐고 앞으로 힘껏 휘둘러 찌를 던져보세요.");
            Debug.Log("<color=green>[피드백]</color> 낚싯대 장착 완료 - 캐스팅 안내");
        }

        public void OnRodStateChangedEvent(RodStateTransition transition)
        {
            // 낚싯대를 놓은 상태(Idle)에서 잡은 상태(Attached)로 변했을 때만 장착 피드백 실행
            if (transition.Previous == RodState.Idle && transition.Current == RodState.Attached)
            {
                OnRodGrabbedEvent();
            }
            else if (transition.Current == RodState.Idle)
            {
                // 낚싯대를 놓았을 때 중복 방지 변수 초기화
                _hasPlayedGrabFeedback = false;
            }
        }

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
                hapticManager.Play(HapticPattern.RhythmicWarning, ControllerHand.Both, HapticSource.TensionWarning);
            }
            else
            {
                HideUI("TensionWarning");
                hapticManager.Stop(ControllerHand.Both, HapticSource.TensionWarning);
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
            hapticManager.Stop(ControllerHand.Both, HapticSource.TensionWarning);

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
            hapticManager.Stop(ControllerHand.Both, HapticSource.TensionWarning);

            HideUI("MiniGamePanel");
            HideUI("TensionWarning"); // 텐션 100이었으므로 무조건 켜져있을 경고 끄기
            
            PlaySound("LineBreak"); // 줄 끊어지는 팽팽한 타격음
            PlayHaptic(HapticPattern.StrongPulse, ControllerHand.Both); // 팅! 하는 강한 진동
            PlayTTS("힘을 버티지 못하고 낚싯줄이 끊어졌습니다.");
            
            Debug.Log("<color=red>[피드백]</color> 미니게임 실패: OnLineBreakEvent 수신 (줄 끊김)");
        }

        public void OnFishEscapedEvent()
        {
            hapticManager.Stop(ControllerHand.Both, HapticSource.TensionWarning);

            HideUI("MiniGamePanel");
            HideUI("TensionWarning");
            
            PlaySound("HookFail"); // 기존 실패 사운드 재사용
            ShowVisualEffect("FishEscape", Vector3.zero);
            PlayTTS("줄이 너무 느슨해져서 물고기가 도망갔습니다.");
            
            Debug.Log("<color=orange>[피드백]</color> 미니게임 실패: OnFishEscapedEvent 수신 (도망감)");
        }

        #endregion

        #region 4. 안전 및 종료 이벤트
        private Coroutine _emergencyCoroutine;

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
                    // 안전 경고 진동만 해제 (미니게임 장력 경고가 진행 중이면 유지)
                    hapticManager.Stop(ControllerHand.Both, HapticSource.SafetyWarning);
                    visualManager.FadeScreen(0.0f, 0.5f);
                    break;

                case SafetyWarningLevel.NearBoundary:
                    // 바닥에 파란색 격자 표시
                    Vector3 gridPos = Vector3.zero;
                    if (playerData != null) 
                    {
                        gridPos = playerData.currentPosition;
                    }
                    visualManager.ShowEffect("BlueGrid", gridPos);
                    // 경계 근접 단계에서는 안전 경고 진동을 켜지 않음 — 혹시 걸려있던 안전 진동만 해제
                    hapticManager.Stop(ControllerHand.Both, HapticSource.SafetyWarning);
                    visualManager.FadeScreen(0.0f, 0.5f);
                    break;

                case SafetyWarningLevel.Outside:
                    // 시야 중앙에 붉은색 큰 팝업 및 중앙 유도 화살표 켜기
                    ShowUI("SafetyWarning");
                    PlaySound("WarningAlarm");
                    PlayTTS("이동하시면 위험합니다. 가운데로 돌아가주세요.");
                    // 최우선 소스로 발행 → 장력 경고가 진행 중이어도 안전 경고가 우선 재생됨
                    hapticManager.Play(HapticPattern.RhythmicWarning, ControllerHand.Both, HapticSource.SafetyWarning);
                    visualManager.FadeScreen(0.0f, 0.5f);
                    break;

                case SafetyWarningLevel.Emergency:
                    // 게임 화면 어둡게 페이드 아웃 후 패스스루 전환
                    HideUI("SafetyWarning");
                    PlayTTS("안전을 위해 게임을 멈춥니다. 장비를 벗고 주변을 확인해주세요.");
                    _emergencyCoroutine = StartCoroutine(EmergencySequenceRoutine());
                    break;
            }
            Debug.Log($"<color=green>[피드백]</color> 안전 경고 단계 변경: {warningLevel}");
        }

        private System.Collections.IEnumerator EmergencySequenceRoutine()
        {
            // 1. 게임 화면을 완전히 검게 페이드 아웃 (1초 동안 진행)
            visualManager.FadeScreen(1.0f, 1.0f); 
            
            // 2. 화면이 완전히 까매질 때까지 1초간 대기 (매우 중요)
            yield return new WaitForSeconds(1.0f);
            
            // 3. 화면이 가려진 안전한 상태에서 패스스루(현실 카메라) 렌더링 활성화
            visualManager.ShowPassthrough(true);
            
            // 4. 다시 화면의 검은 장막을 거둬내어 현실 세계(패스스루)를 유저에게 보여줌 (0.5초)
            visualManager.FadeScreen(0.0f, 0.5f); 
        }

        public void OnAccountSavedEvent()
        {
            // 1. 저장이 완료되면 종료 UI를 띄움
            PlaySound("SaveComplete");
            PlayTTS("데이터 저장이 완료되었습니다.");
        }

        private System.Collections.IEnumerator ExitRoutine()
        {
            // 3초 대기 (TTS 음성이 끝나는 시간 정도)
            yield return new WaitForSeconds(3.0f);

            // [선택 1] UI만 끄고 메인 화면으로 돌아갈 경우
            HideUI("ExitSequence"); 
            
            // // [선택 2] 게임(앱)을 완전히 종료할 경우 (VR 환경에서는 보통 이걸 씁니다)
            // #if UNITY_EDITOR
            //     UnityEditor.EditorApplication.isPlaying = false; // 에디터에서는 플레이 모드 정지
            // #else
            //     Application.Quit(); // 실제 빌드된 앱에서는 앱 종료
            // #endif
        }

        // public void OnRodStateChangedEvent()
        // {
        //     // 레거시 void 디버그 채널용 no-op (FishingEventDebugTester의 void onRodStateChanged).
        //     // 실제 낚싯대 상태 전이 피드백은 RodStateTransition을 받는 오버로드(region 2)에서 처리.
        // }
        #endregion

        #region IFeedbackService 구현 (위임)
        public void PlaySound(string soundId) => soundManager.PlayWithId(soundId);
        public void PlayBGM(string soundId) => soundManager.PlayBGMWithId(soundId);
        public void StopBGM() => soundManager.StopBGM();
        public void PlayHaptic(HapticPattern pattern, ControllerHand hand) => hapticManager.Play(pattern, hand);
        public void PlayHaptic(HapticPattern pattern, ControllerHand hand, HapticSource source) => hapticManager.Play(pattern, hand, source);
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