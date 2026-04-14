using System.Collections.Generic;
using UnityEngine;
using VirtualFishing.Interfaces;

namespace VirtualFishing.Feedback
{
    public class SoundManager : MonoBehaviour, ISoundFeedback
    {
        [System.Serializable]
        public struct SoundEntry
        {
            public string id;
            public AudioClip clip;
        }

        [SerializeField] private List<SoundEntry> soundLibrary;
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource bgmSource;

        private Dictionary<string, AudioClip> soundDict = new Dictionary<string, AudioClip>();

        private void Awake()
        {
            foreach (var entry in soundLibrary)
            {
                soundDict[entry.id] = entry.clip;
            }
        }

        public void Play(AudioClip clip) => sfxSource.PlayOneShot(clip);

        public void PlayWithId(string soundId)
        {
            if (soundDict.TryGetValue(soundId, out AudioClip clip))
                Play(clip);
            else
                Debug.LogWarning($"[SoundManager] 사운드 ID 누락: {soundId}");
        }

        public void PlayBGM(AudioClip clip)
        {
            bgmSource.clip = clip;
            bgmSource.Play();
        }

        public void StopBGM() => bgmSource.Stop();
        public void SetVolume(float volume) => AudioListener.volume = volume;
    }
}