using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.InputSystem;
using VirtualFishing.Interfaces;
using VirtualFishing.Core;

namespace VirtualFishing.Feedback
{
    public class HapticManager : MonoBehaviour, IHapticFeedback
    {
        // 손(노드)별 상태를 캡슐화하여 left/right 중복 코드를 제거한다.
        // - active: 현재 지속 진동(Continuous/RhythmicWarning)을 요청 중인 소스 집합
        // - routine/runningSource: 실제로 재생 중인 코루틴과 그 소유 소스
        private class HandState
        {
            public XRNode node;
            public readonly Dictionary<HapticSource, HapticPattern> active = new Dictionary<HapticSource, HapticPattern>();
            public Coroutine routine;
            public HapticSource runningSource;
            public bool running;
        }

        private readonly HandState left = new HandState { node = XRNode.LeftHand };
        private readonly HandState right = new HandState { node = XRNode.RightHand };

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

        #region 공개 API

        // IHapticFeedback: 소스를 지정하지 않으면 Default(단발성 피드백) 취급
        public void Play(HapticPattern pattern, ControllerHand hand) => Play(pattern, hand, HapticSource.Default);

        public void Play(HapticPattern pattern, ControllerHand hand, HapticSource source)
        {
            Debug.Log($"<color=cyan>[Haptic 요청]</color> <b>{pattern}</b> ({source}) → {hand}");

            if (hand != ControllerHand.Right) PlayOnHand(left, pattern, source);
            if (hand != ControllerHand.Left) PlayOnHand(right, pattern, source);
        }

        // IHapticFeedback: 해당 손의 모든 소스 진동을 정지 (트래킹 소실 등 전역 정지용)
        public void Stop(ControllerHand hand)
        {
            Debug.Log($"<color=orange>[Haptic 정지]</color> {hand} 전체 정지");

            if (hand != ControllerHand.Right) { left.active.Clear(); Reevaluate(left); }
            if (hand != ControllerHand.Left) { right.active.Clear(); Reevaluate(right); }
        }

        // 특정 소스가 건 경고만 해제. 더 낮은 우선순위의 다른 경고는 그대로 이어서 재생된다.
        public void Stop(ControllerHand hand, HapticSource source)
        {
            Debug.Log($"<color=orange>[Haptic 정지]</color> {hand} / {source} 해제");

            if (hand != ControllerHand.Right) ReleaseSource(left, source);
            if (hand != ControllerHand.Left) ReleaseSource(right, source);
        }

        public void StopAll() => Stop(ControllerHand.Both);

        #endregion

        #region 중재 로직

        private void PlayOnHand(HandState s, HapticPattern pattern, HapticSource source)
        {
            switch (pattern)
            {
                // 단발성 패턴: 중재에 참여하지 않고 즉시 1회 재생.
                // (진행 중인 경고 코루틴을 죽이지 않으므로 안전 경고가 유지됨)
                case HapticPattern.LightPulse:
                    TriggerHaptic(s.node, 0.3f, 0.1f, "LightPulse");
                    break;
                case HapticPattern.StrongPulse:
                    TriggerHaptic(s.node, 1.0f, 0.3f, "StrongPulse");
                    break;

                // 지속 패턴: 소스를 등록하고 우선순위로 중재한다.
                case HapticPattern.Continuous:
                case HapticPattern.RhythmicWarning:
                    s.active[source] = pattern;
                    Reevaluate(s);
                    break;
            }
        }

        private void ReleaseSource(HandState s, HapticSource source)
        {
            if (s.active.Remove(source))
                Reevaluate(s);
        }

        // active 집합에서 가장 높은 우선순위 소스를 골라 그것만 재생한다.
        // 이미 올바른 소스가 재생 중이면 아무것도 하지 않아(매 프레임 재호출에도) 코루틴 재시작을 막는다.
        private void Reevaluate(HandState s)
        {
            if (s.active.Count == 0)
            {
                if (s.running) StopRoutine(s);
                return;
            }

            HapticSource winner = default;
            HapticPattern winnerPattern = default;
            bool found = false;
            foreach (var kv in s.active)
            {
                if (!found || (int)kv.Key > (int)winner)
                {
                    winner = kv.Key;
                    winnerPattern = kv.Value;
                    found = true;
                }
            }

            // 이미 같은 소스가 돌고 있으면 유지 (장력 경고의 매 프레임 재요청을 무시)
            if (s.running && s.runningSource == winner) return;

            // 다른(또는 새) 소스로 전환: 기존 코루틴/잔여 진동 정리 후 새 패턴 시작
            if (s.routine != null) StopCoroutine(s.routine);
            HardStop(s.node);

            s.runningSource = winner;
            s.running = true;
            s.routine = StartCoroutine(PatternRoutine(s, winnerPattern));
        }

        private void StopRoutine(HandState s)
        {
            if (s.routine != null) StopCoroutine(s.routine);
            s.routine = null;
            s.running = false;
            HardStop(s.node);
        }

        #endregion

        #region 코루틴 구현부

        private IEnumerator PatternRoutine(HandState s, HapticPattern pattern)
        {
            if (pattern == HapticPattern.Continuous)
            {
                const float amplitude = 0.5f;
                const float duration = 0.5f;
                while (true)
                {
                    TriggerHaptic(s.node, amplitude, duration, "Continuous");
                    yield return new WaitForSeconds(duration);
                }
            }
            else // RhythmicWarning
            {
                const float amplitude = 0.8f;
                const float pulseDuration = 0.2f;
                const float pauseDuration = 0.2f;
                while (true)
                {
                    TriggerHaptic(s.node, amplitude, pulseDuration, "RhythmicWarning");
                    yield return new WaitForSeconds(pulseDuration + pauseDuration);
                }
            }
        }

        #endregion

        #region 하드웨어 제어부

        private void TriggerHaptic(XRNode node, float amplitude, float duration, string patternName)
        {
            UnityEngine.XR.InputDevice device = InputDevices.GetDeviceAtXRNode(node);
            if (!device.isValid) return;

            if (device.TryGetHapticCapabilities(out HapticCapabilities capabilities) && capabilities.supportsImpulse)
            {
                device.SendHapticImpulse(0, amplitude, duration);
            }
        }

        // 진행 중인 진동을 실제로 중단한다.
        // ※ SendHapticImpulse(0,0,0)은 "0짜리 새 임펄스 예약"일 뿐 재생 중 진동을 취소하지 못한다.
        //    잔여 진동을 끊는 정식 API는 InputDevice.StopHaptics() 이다.
        private void HardStop(XRNode node)
        {
            UnityEngine.XR.InputDevice device = InputDevices.GetDeviceAtXRNode(node);
            if (device.isValid)
            {
                device.StopHaptics();
                Debug.Log($"[완전 종료] 손: {node} StopHaptics() 호출 — 잔여 진동 차단");
            }
        }

        #endregion
    }
}
