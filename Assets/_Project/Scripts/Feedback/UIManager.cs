using UnityEngine;
using TMPro; // 텍스트 가독성을 위해 필수
using VirtualFishing.Interfaces;

namespace VirtualFishing.UI
{
    public class UIManager : MonoBehaviour
    {
        [Header("UI Panels")]
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private GameObject warningPanel;

        [Header("References")]
        [SerializeField] private Feedback.FeedbackManager feedbackManager;

        // 1. 포획 결과 팝업 표시
        public void ShowCatchResult(string fishName)
        {
            HideAll();
            resultPanel.SetActive(true);

            // UI 표시와 동시에 음성 안내 및 진동 실행
            feedbackManager.PlayTTS($"{fishName}를 잡았습니다! 참 잘하셨습니다.");
            feedbackManager.PlayHaptic(HapticPattern.StrongPulse, ControllerHand.Both);
            feedbackManager.PlaySound("Fanfare");
        }

        // 2. 안전 경고 팝업 표시
        public void ShowSafetyWarning(string message)
        {
            // 경고는 다른 UI보다 최상단에 표시
            warningPanel.SetActive(true);
            
            feedbackManager.PlayTTS(message);
            feedbackManager.PlaySound("WarningBeep");
            feedbackManager.PlayHaptic(HapticPattern.RhythmicWarning, ControllerHand.Both);
        }

        public void HideAll()
        {
            mainPanel.SetActive(false);
            resultPanel.SetActive(false);
            warningPanel.SetActive(false);
        }

        // 버튼 클릭 시 호출될 공통 함수 (사운드 피드백 포함)
        public void OnButtonClick()
        {
            feedbackManager.PlaySound("ButtonClick");
            feedbackManager.PlayHaptic(HapticPattern.LightPulse, ControllerHand.Right);
        }
    }
}