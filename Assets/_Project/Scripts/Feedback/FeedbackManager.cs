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

        #region SO Event 수신부 (UnityEvent로 연결)

        // 시나리오 05: 포획 결과 연출
        public void OnCatchResultEvent()
        {
            PlaySound("Fanfare");
            ShowVisualEffect("Fireworks", Vector3.zero);
            ShowUI("FishInfoPanel");
            PlayTTS("물고기를 잡으셨습니다.");
        }

        // 시나리오 06: 안전 구역 이탈 경고 (0:None, 1:Near, 2:Outside, 3:Emergency)
        public void OnSafetyWarningEvent(int level)
        {
            SafetyWarningLevel warningLevel = (SafetyWarningLevel)level;

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

        // 시나리오 08: 게임 종료 시퀀스
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