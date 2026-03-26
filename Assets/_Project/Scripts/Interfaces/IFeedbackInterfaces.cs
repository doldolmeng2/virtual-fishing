using System;
using UnityEngine;

namespace VirtualFishing.Interfaces
{
    /// <summary>
    /// 통합 피드백 서비스 (Facade).
    /// 내부적으로 Sound, Haptic, Visual, TTS 하위 시스템에 위임.
    /// </summary>
    public interface IFeedbackService
    {
        void PlaySound(string soundId);
        void PlayHaptic(HapticPattern pattern, ControllerHand hand);
        void ShowVisualEffect(string effectId, Vector3 position);
        void PlayTTS(string message);
        void ShowUI(string uiId, object data = null);
        void HideUI(string uiId);
    }

    public interface ISoundFeedback
    {
        void Play(AudioClip clip);
        void PlayWithId(string soundId);
        void PlayBGM(AudioClip clip);
        void StopBGM();
        void SetVolume(float volume);
    }

    public interface IHapticFeedback
    {
        void Play(HapticPattern pattern, ControllerHand hand);
        void Stop(ControllerHand hand);
        void StopAll();
    }

    public interface IVisualFeedback
    {
        void ShowEffect(string effectId, Vector3 position);
        void ShowEffect(GameObject prefab, Vector3 position);
        void FadeScreen(float targetAlpha, float duration);
        void ShowPassthrough(bool enable);
    }

    public interface ITTSFeedback
    {
        void Speak(string message);
        void Stop();
        bool IsSpeaking { get; }
        event Action OnSpeechComplete;
    }
}
