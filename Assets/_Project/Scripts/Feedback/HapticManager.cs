using System.Collections;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.InputSystem;
using VirtualFishing.Interfaces;
using VirtualFishing.Core;

namespace VirtualFishing.Feedback
{
    public class HapticManager : MonoBehaviour, IHapticFeedback
    {
        // 양손에 각각 독립적으로 실행되는 코루틴을 추적하여 중복 실행을 방지
        private Coroutine leftCoroutine;
        private Coroutine rightCoroutine;

        #region PC 환경 키보드 테스트용 (빌드 전 제거 또는 주석 처리 권장)
        private void Update()
        {
            // 키보드가 연결되어 있지 않으면 무시
            if (Keyboard.current == null) return;

            // 새로운 Input System 방식의 키 입력 감지
            if (Keyboard.current.digit1Key.wasPressedThisFrame) 
                Play(HapticPattern.LightPulse, ControllerHand.Right);
            
            if (Keyboard.current.digit2Key.wasPressedThisFrame) 
                Play(HapticPattern.StrongPulse, ControllerHand.Right);
            
            if (Keyboard.current.digit3Key.wasPressedThisFrame) 
                Play(HapticPattern.Continuous, ControllerHand.Both);
            
            if (Keyboard.current.digit4Key.wasPressedThisFrame) 
                Play(HapticPattern.RhythmicWarning, ControllerHand.Both);
            
            if (Keyboard.current.spaceKey.wasPressedThisFrame) 
                StopAll();
        }
        #endregion

        public void Play(HapticPattern pattern, ControllerHand hand)
        {
            Debug.Log($"<color=cyan>[Haptic 요청]</color> <b>{pattern}</b> 패턴을 {hand} 컨트롤러에 실행합니다.");
            
            // Both(양손)일 경우 분리해서 개별적으로 처리
            if (hand == ControllerHand.Both)
            {
                PlayPatternOnNode(pattern, XRNode.LeftHand, ref leftCoroutine);
                PlayPatternOnNode(pattern, XRNode.RightHand, ref rightCoroutine);
            }
            else if (hand == ControllerHand.Left)
            {
                PlayPatternOnNode(pattern, XRNode.LeftHand, ref leftCoroutine);
            }
            else if (hand == ControllerHand.Right)
            {
                PlayPatternOnNode(pattern, XRNode.RightHand, ref rightCoroutine);
            }
        }

        private void PlayPatternOnNode(HapticPattern pattern, XRNode node, ref Coroutine activeCoroutine)
        {
            // 새로운 진동을 시작하기 전에 기존에 돌고 있던 코루틴이 있다면 정지
            if (activeCoroutine != null)
            {
                StopCoroutine(activeCoroutine);
                activeCoroutine = null;
            }

            switch (pattern)
            {
                case HapticPattern.LightPulse:
                    TriggerHaptic(node, 0.3f, 0.1f, pattern.ToString());
                    break;
                case HapticPattern.StrongPulse:
                    TriggerHaptic(node, 1.0f, 0.3f, pattern.ToString());
                    break;
                case HapticPattern.Continuous:
                    activeCoroutine = StartCoroutine(ContinuousRoutine(node));
                    break;
                case HapticPattern.RhythmicWarning:
                    activeCoroutine = StartCoroutine(RhythmicRoutine(node));
                    break;
            }
        }

        public void Stop(ControllerHand hand)
        {
            Debug.Log($"<color=orange>[Haptic 정지]</color> {hand} 컨트롤러 진동을 정지합니다.");

            // 지정된 손의 코루틴을 멈추고 기기에도 진동 정지(0f) 신호 전달
            if (hand == ControllerHand.Both || hand == ControllerHand.Left)
            {
                if (leftCoroutine != null) StopCoroutine(leftCoroutine);
                TriggerHaptic(XRNode.LeftHand, 0f, 0f, "Stop");
            }
            
            if (hand == ControllerHand.Both || hand == ControllerHand.Right)
            {
                if (rightCoroutine != null) StopCoroutine(rightCoroutine);
                TriggerHaptic(XRNode.RightHand, 0f, 0f, "Stop");
            }
        }

        public void StopAll() => Stop(ControllerHand.Both);

        #region 코루틴 구현부

        // [지속 진동] - Stop()이 호출될 때까지 끊기지 않고 진동
        private IEnumerator ContinuousRoutine(XRNode node)
        {
            float amplitude = 0.5f; // 진동 세기
            float duration = 0.5f;  // 한 번의 펄스 길이

            while (true)
            {
                TriggerHaptic(node, amplitude, duration, "Continuous");
                
                // duration(0.5초)만큼 기다린 후 다시 0.5초짜리 진동
                yield return new WaitForSeconds(duration); 
            }
        }

        // [점멸 진동] - 징~ (쉬고) 징~ (쉬고) 반복 (경고성 진동)
        private IEnumerator RhythmicRoutine(XRNode node)
        {
            float amplitude = 0.8f;     // 강한 진동 세기
            float pulseDuration = 0.2f; // 진동이 켜져 있는 시간
            float pauseDuration = 0.2f; // 진동이 꺼져 있는 시간

            while (true)
            {
                TriggerHaptic(node, amplitude, pulseDuration, "RhythmicWarning");
                
                // 진동 시간 + 쉬는 시간만큼 대기하여 점멸 효과를 만듭니다.
                yield return new WaitForSeconds(pulseDuration + pauseDuration);
            }
        }

        #endregion

        #region 하드웨어 제어부
        private void TriggerHaptic(XRNode node, float amplitude, float duration, string patternName)
        {
            if (amplitude > 0f)
            {
                Debug.Log($"[실행 중] 손: {node} | 패턴: {patternName} | 세기: {amplitude} | 길이: {duration}초");
            }
            else
            {
                Debug.Log($"[완전 종료] 손: {node} 에 잔여 진동 차단 신호(0f) 전송됨.");
            }

            // 1. 모호성 해결: UnityEngine.XR 소속임을 명확히 적어줍니다.
            UnityEngine.XR.InputDevice device = InputDevices.GetDeviceAtXRNode(node);

            // 2. 초기화 에러 해결: out 변수를 미리 선언하고 기본값을 넣어줍니다.
            UnityEngine.XR.HapticCapabilities capabilities = default;

            if (device.isValid && device.TryGetHapticCapabilities(out capabilities))
            {
                if (capabilities.supportsImpulse)
                {
                    device.SendHapticImpulse(0, amplitude, duration);
                }
            }
        }
        #endregion
    }
}