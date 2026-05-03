using UnityEngine;
using UnityEngine.InputSystem;
using VirtualFishing.Core.Events; // SO 클래스가 있는 네임스페이스

namespace VirtualFishing.Feedback.Test
{
    public class TempEventTester : MonoBehaviour
    {
        [Header("임시 이벤트 에셋 연결")]
        public VoidEventSO catchResultEvent;
        public VoidEventSO biteOccurredEvent;
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
                Debug.Log($"<color=yellow>[테스트]</color> 입질 이벤트 발행");
                biteOccurredEvent?.Raise();
            }

            // 4. 안전 구역 복귀 테스트 (None) - Level 0 : 모든 UI/효과 꺼짐
            if (Keyboard.current.digit8Key.wasPressedThisFrame)
            {
                Debug.Log("<color=yellow>[테스트]</color> 안전 구역 복귀(None: 0) 이벤트 발행");
                safetyWarningEvent?.Raise(0);
            }
            
            // 5. 경계 근접 테스트 (NearBoundary) - Level 1 : 바닥에 파란색 그리드 생성
            if (Keyboard.current.digit9Key.wasPressedThisFrame)
            {
                Debug.Log("<color=yellow>[테스트]</color> 경계 근접(NearBoundary: 1) 이벤트 발행");
                safetyWarningEvent?.Raise(1);
            }
            
            // 6. 구역 이탈 테스트 (Outside) - Level 2 : 붉은색 경고 패널 및 진동/소리
            if (Keyboard.current.digit0Key.wasPressedThisFrame)
            {
                Debug.Log("<color=yellow>[테스트]</color> 안전 구역 이탈(Outside: 2) 이벤트 발행");
                safetyWarningEvent?.Raise(2);
            }
            
            // 7. 긴급 상황 테스트 (Emergency) - Level 3 : 화면 페이드아웃 및 패스스루
            if (Keyboard.current.minusKey.wasPressedThisFrame) // 숫자 0 옆의 '-' 키
            {
                Debug.Log("<color=yellow>[테스트]</color> 긴급 상황(Emergency: 3) 이벤트 발행");
                safetyWarningEvent?.Raise(3);
            }
        }
    }
}