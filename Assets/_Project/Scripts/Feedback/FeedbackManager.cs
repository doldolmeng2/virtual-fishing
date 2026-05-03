using UnityEngine;
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

        [Header("Temporary UI References")]
        [Tooltip("Head-Locked 경고 캔버스 게임오브젝트를 여기에 넣으세요")]
        public GameObject safetyWarningPanel;
        [Tooltip("챔질 가이드 캔버스 게임오브젝트를 여기에 넣으세요")]
        public GameObject hookingGuidePanel;
        [Tooltip("게임 종료 캔버스 게임오브젝트를 여기에 넣으세요")]
        public GameObject exitSequencePanel;
        [Tooltip("포획 결과 캔버스 게임오브젝트를 여기에 넣으세요")]
        public GameObject catchResultPanel;

        #region 낚시, 미니게임 이벤트 수신부

        // 1. 낚싯대 상태 변경 이벤트 수신 (IntEventSO 등을 통해 Enum 인덱스 수신)
        public void OnRodStateChangedEvent(int stateIndex)
        {
            RodState state = (RodState)stateIndex; // GameEnums.cs 참조

            switch (state)
            {
                case RodState.Attached:
                    PlaySound("RodAttach");
                    PlayHaptic(HapticPattern.LightPulse, ControllerHand.Right); // 낚싯대를 쥔 주 사용 손
                    break;
                case RodState.Casting:
                    PlaySound("LineCast");
                    PlayHaptic(HapticPattern.StrongPulse, ControllerHand.Right);
                    break;
                case RodState.Hit: // 챔질 성공
                    HideUI("HookingGuide"); // 성공했으니 가이드 UI는 끕니다.
                    PlaySound("HookSuccess");
                    PlayHaptic(HapticPattern.StrongPulse, ControllerHand.Right);
                    ShowVisualEffect("HookSuccess", Vector3.zero); // 실제로는 낚싯대 끝 좌표 전달 필요
                    break;

                case RodState.Idle: // 챔질 실패로 상태 롤백 시
                    HideUI("HookingGuide");
                    // TTS 및 물고기 도망 이펙트는 실패 이벤트(별도)에서 처리
                    PlayTTS("아쉽습니다. 다시 도전해보세요"); // 실패 음성 안내
                    //visualManager.ShowEffect("FishEscapeVFX", floatPosition);
                    break;
            }
            Debug.Log($"<color=green>[피드백]</color> 낚싯대 상태 변경 이벤트 수신: {state}");
        }

        // 2. 찌 착수 이벤트 수신 (VoidEventSO)
        public void OnWaterLandedEvent()
        {
            PlaySound("WaterSplash");
            ShowVisualEffect("Splash", Vector3.zero);
        }

        // 예고 입질
        public void OnPreBiteEvent(Vector3 floatPosition)
        {
            visualManager.ShowEffect("Ripple", floatPosition); // 물결 이펙트
            PlayHaptic(HapticPattern.LightPulse, ControllerHand.Right); // 약한 진동
        }

        // 3. 입질 발생 이벤트 수신 (VoidEventSO)
        public void OnBiteOccurredEvent()
        {
            PlaySound("FloatSink");
            PlayHaptic(HapticPattern.StrongPulse, ControllerHand.Right);
            Debug.Log("<color=green>[피드백]</color> 입질 이벤트 수신: 찌가 가라앉습니다.");
            // 챔질 존 가이드 UI를 켭니다 (플레이어에게 낚싯대를 들라고 지시)
            ShowUI("HookingGuide"); 
        }

        // 4. 미니게임 시작 이벤트 수신 (VoidEventSO)
        public void OnMiniGameStartedEvent()
        {
            ShowUI("MiniGamePanel");
            // soundManager.PlayBGM(...); //미니게임 BGM 클립 전달 필요
            PlayTTS("릴을 감아주세요!");
        }

        // 5. 미니게임 중 텐션(장력) 변화 이벤트 수신 (FloatEventSO)
        public void OnTensionChangedEvent(float tension)
        {
            Debug.Log($"<color=green>[피드백]</color> 장력 변화 이벤트 수신: {tension}");
            // 기획된 장력 한계치(예: Danger 영역 진입 기준 80f)를 넘어가면 경고 피드백
            if (tension >= 80f) 
            {
                ShowUI("TensionWarning");
                PlaySound("WarningBeep");
                PlayHaptic(HapticPattern.StrongPulse, ControllerHand.Both); // 양손에 강한 저항감
                // PlayTTS("낚싯대를 반대로 당기세요!"); // 필요시 주석 해제
            }
            else
            {
                HideUI("TensionWarning");
            }
        }

        // 6. 미니게임 결과 이벤트 수신 (성공=true, 실패=false 전달받음)
        public void OnMiniGameResultEvent(bool isSuccess)
        {
            HideUI("MiniGamePanel");
            
            if (!isSuccess) // 실패 (줄 끊어짐 또는 시간 초과)
            {
                PlaySound("LineSnap");
                hapticManager.StopAll(); // 진동 강제 종료
                PlayTTS("아쉽습니다. 물고기를 놓쳤습니다.");
                ShowVisualEffect("FishEscape", Vector3.zero);
            }
            // 성공 시에는 OnCatchResultEvent()가 이어서 호출될 것이므로 생략합니다.
        }

        #endregion

        #region 결과, 안전, 종료 이벤트 수신부

        public void OnCatchResultEvent()
        {
            PlaySound("Fanfare");
            ShowVisualEffect("Fireworks", Vector3.zero); // 실제론 물고기 위치 전달
            //fishdata 전달받아서 이름, 크기, 무게, 별점 등 결과 UI에 표시할 데이터도 같이 전달 필요
            ShowUI("CatchResult"); 
            
            PlayTTS("물고기를 잡으셨습니다! 크기와 무게를 확인해보세요.");
            Debug.Log("<color=green>[피드백]</color> 포획 결과 이벤트 수신: 성공 및 보상 UI 출력");
        }

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
        }

        public void OnExitSequenceEvent()
        {
            ShowUI("ExitSequence");
            PlayTTS("기록을 안전하게 저장하고 있습니다.");
            visualManager.FadeScreen(0.7f, 3.0f);
        }

        #endregion

        #region IFeedbackService 구현 (위임)
        public void PlaySound(string soundId) => soundManager.PlayWithId(soundId);
        public void PlayHaptic(HapticPattern pattern, ControllerHand hand) => hapticManager.Play(pattern, hand);
        public void ShowVisualEffect(string effectId, Vector3 position) => visualManager.ShowEffect(effectId, position);
        public void PlayTTS(string message) => ttsManager.Speak(message);
        public void ShowUI(string uiId, object data = null)
        {
            if (uiId == "SafetyWarning")
            {
                if (safetyWarningPanel != null) 
                {
                    safetyWarningPanel.SetActive(true);
                }
                else 
                {
                    Debug.LogError("🚨 [오류] FeedbackManager의 'Safety Warning Panel' 슬롯이 비어있습니다! 하이어라키의 캔버스를 드래그해서 넣으세요.");
                }
            }
            else if (uiId == "HookingGuide" && hookingGuidePanel != null) 
            {
                hookingGuidePanel.SetActive(true);
            }
            else if (uiId == "CatchResult" && catchResultPanel != null)
            {
                catchResultPanel.SetActive(true);
                
                CatchResultController controller = catchResultPanel.GetComponent<CatchResultController>();
                if (controller != null)
                {
                    // 추후 data 인자값을 받아서 넘기도록 수정해야 합니다. 
                    // 지금은 테스트용으로 참돔 데이터를 넣습니다.
                    controller.DisplayResult("참돔", 45.2f, 3.1f, 4);
                }
            }
            else if (uiId == "ExitSequence" && exitSequencePanel != null)
            {
                exitSequencePanel.SetActive(true);
            }
        }
        public void HideUI(string uiId)
        {
            if (uiId == "SafetyWarning" && safetyWarningPanel != null)
            {
                safetyWarningPanel.SetActive(false);
            }
            else if (uiId == "HookingGuide" && hookingGuidePanel != null) 
            {
                hookingGuidePanel.SetActive(false);
            }
            else if (uiId == "CatchResult" && catchResultPanel != null)
            {
                catchResultPanel.SetActive(false);
            }
            else if (uiId == "ExitSequence" && exitSequencePanel != null)
            {
                exitSequencePanel.SetActive(false);
            }
        }
        #endregion
    }
}