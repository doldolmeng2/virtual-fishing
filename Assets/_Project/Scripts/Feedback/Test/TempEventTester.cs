using UnityEngine;
using UnityEngine.InputSystem;
using VirtualFishing.Core.Events; // SO 클래스가 있는 네임스페이스

namespace VirtualFishing.Feedback.Test
{
    public class TempEventTester : MonoBehaviour
    {
        [Header("임시 이벤트 에셋 연결")]
        public VoidEventSO catchResultEvent;
        public IntEventSO rodStateEvent;
        public FloatEventSO tensionEvent;
        public IntEventSO safetyWarningEvent;

        private void Update()
        {
            if (Keyboard.current == null) return;

            // 1. 포획 결과 테스트 (Void)
            if (Keyboard.current.digit5Key.wasPressedThisFrame)
            {
                Debug.Log("<color=yellow>[테스트]</color> 포획 결과 이벤트 발행");
                catchResultEvent?.Raise();
            }

            // 2. 낚싯대 잡음 테스트 (Int) - GameEnums.RodState.Attached = 1
            if (Keyboard.current.digit6Key.wasPressedThisFrame)
            {
                Debug.Log("<color=yellow>[테스트]</color> 낚싯대 Attached 상태 이벤트 발행");
                rodStateEvent?.Raise(1); 
            }

            // 3. 미니게임 위험 장력 테스트 (Float) - 80 이상일 때 경고
            if (Keyboard.current.digit7Key.wasPressedThisFrame)
            {
                float mockTension = 85.5f;
                Debug.Log($"<color=yellow>[테스트]</color> 장력 {mockTension} 전달");
                tensionEvent?.Raise(mockTension);
            }

            // 4. 안전 구역 이탈 테스트 (Int) - GameEnums.SafetyWarningLevel.Outside = 2
            if (Keyboard.current.digit8Key.wasPressedThisFrame)
            {
                Debug.Log("<color=yellow>[테스트]</color> 안전 구역 이탈(Outside) 이벤트 발행");
                safetyWarningEvent?.Raise(2);
            }
            
            // 5. 안전 구역 복귀 테스트 (Int) - None = 0
            if (Keyboard.current.digit9Key.wasPressedThisFrame)
            {
                Debug.Log("<color=yellow>[테스트]</color> 안전 구역 복귀(None) 이벤트 발행");
                safetyWarningEvent?.Raise(0);
            }
        }
    }
}