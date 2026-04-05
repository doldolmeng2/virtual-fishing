using UnityEngine;
using VirtualFishing.Interfaces;

namespace VirtualFishing.Feedback
{
    public class HapticManager : MonoBehaviour, IHapticFeedback
    {
        public void Play(HapticPattern pattern, ControllerHand hand)
        {
            float amplitude = 0f;
            float duration = 0f;

            switch (pattern)
            {
                case HapticPattern.LightPulse:
                    amplitude = 0.3f; duration = 0.1f; break;
                case HapticPattern.StrongPulse:
                    amplitude = 1.0f; duration = 0.3f; break;
                case HapticPattern.Continuous:
                    amplitude = 0.5f; duration = 1.0f; break; // 코루틴 루프 필요
                case HapticPattern.RhythmicWarning:
                    amplitude = 0.8f; duration = 0.2f; break; // 코루틴 점멸 필요
            }

            SendHapticToHardware(hand, amplitude, duration);
        }

        public void Stop(ControllerHand hand) { /* 지속 진동 정지 로직 */ }
        public void StopAll() { /* 양손 진동 정지 로직 */ }

        private void SendHapticToHardware(ControllerHand hand, float amp, float dur)
        {
            // 실제 플러그인(XR Toolkit, Oculus SDK 등) API 연동 구간
            Debug.Log($"[Haptic] {hand} 컨트롤러 | 강도: {amp} | 시간: {dur}");
        }
    }
}