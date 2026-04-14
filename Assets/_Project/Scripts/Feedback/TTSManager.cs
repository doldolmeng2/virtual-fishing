using System;
using System.Collections;
using UnityEngine;
using VirtualFishing.Interfaces;

namespace VirtualFishing.Feedback
{
    public class TTSManager : MonoBehaviour, ITTSFeedback
    {
        public bool IsSpeaking { get; private set; }
        public event Action OnSpeechComplete;

        private Coroutine speechCoroutine;

        public void Speak(string message)
        {
            Stop(); // 기존 음성이 있다면 중단

            IsSpeaking = true;
            Debug.Log($"[TTSManager] 음성 출력 중: {message}");

            // 외부 플러그인(Meta Voice SDK, Google Cloud TTS 등) 연동 지점
            // 구현 전 프로토타입 단계이므로, 임시 딜레이 코루틴으로 완료 이벤트를 시뮬레이션합니다.
            speechCoroutine = StartCoroutine(SimulateSpeechDelay(message));
        }

        public void Stop()
        {
            if (speechCoroutine != null)
            {
                StopCoroutine(speechCoroutine);
                speechCoroutine = null;
            }

            if (IsSpeaking)
            {
                IsSpeaking = false;
                // 외부 TTS API 재생 중지 로직 호출
            }
        }

        private IEnumerator SimulateSpeechDelay(string message)
        {
            // 텍스트 길이에 비례한 임시 대기 시간 계산
            float simulatedDuration = Mathf.Max(1.0f, message.Length * 0.1f);
            yield return new WaitForSeconds(simulatedDuration);

            IsSpeaking = false;
            OnSpeechComplete?.Invoke();
        }
    }
}