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
                case RodState.Hit:
                    PlaySound("HookSuccess");
                    PlayHaptic(HapticPattern.StrongPulse, ControllerHand.Right);
                    ShowVisualEffect("HookSuccess", Vector3.zero); // 임시 위치(Vector3.zero)
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

        // 3. 입질 발생 이벤트 수신 (VoidEventSO)
        public void OnBiteOccurredEvent()
        {
            PlaySound("FloatSink");
            PlayHaptic(HapticPattern.StrongPulse, ControllerHand.Right);
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
            ShowVisualEffect("Fireworks", Vector3.zero);
            ShowUI("FishInfoPanel");
            PlayTTS("물고기를 잡으셨습니다.");
            Debug.Log("<color=green>[피드백]</color> 포획 결과 이벤트 수신: 성공");
        }

        public void OnSafetyWarningEvent(int level)
        {
            SafetyWarningLevel warningLevel = (SafetyWarningLevel)level;
            Debug.Log($"<color=green>[피드백]</color> 안전 경고 이벤트 수신: {warningLevel}");

            switch (warningLevel)
            {
                case SafetyWarningLevel.None:
                    HideUI("SafetyWarning");
                    hapticManager.StopAll();
                    visualManager.ShowPassthrough(false);
                    break;
                case SafetyWarningLevel.NearBoundary:
                    ShowVisualEffect("BlueGrid", Vector3.zero);
                    break;
                case SafetyWarningLevel.Outside:
                    ShowUI("SafetyWarning");
                    PlaySound("WarningAlarm");
                    PlayHaptic(HapticPattern.RhythmicWarning, ControllerHand.Both);
                    PlayTTS("안전 구역을 벗어났습니다. 중앙으로 돌아와 주세요.");
                    break;
                case SafetyWarningLevel.Emergency:
                    visualManager.FadeScreen(0.8f, 1.0f);
                    visualManager.ShowPassthrough(true);
                    PlayTTS("위험합니다. 제자리로 돌아와 주세요.");
                    break;
            }
        }

        public void OnExitSequenceEvent()
        {
            ShowUI("SavingProgress");
            PlayTTS("기록을 안전하게 저장하고 있습니다.");
            visualManager.FadeScreen(0.7f, 3.0f);
        }

        #endregion

        #region IFeedbackService 구현 (위임)
        public void PlaySound(string soundId) => soundManager.PlayWithId(soundId);
        public void PlayHaptic(HapticPattern pattern, ControllerHand hand) => hapticManager.Play(pattern, hand);
        public void ShowVisualEffect(string effectId, Vector3 position) => visualManager.ShowEffect(effectId, position);
        public void PlayTTS(string message) => ttsManager.Speak(message);
        public void ShowUI(string uiId, object data = null) { /* 통합 UI 로직 호출 */ }
        public void HideUI(string uiId) { /* 통합 UI 로직 호출 */ }
        #endregion
    }
}