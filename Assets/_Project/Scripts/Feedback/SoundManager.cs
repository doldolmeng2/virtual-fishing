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

        [SerializeField] private List<SoundEntry> soundLibrary = new List<SoundEntry>();
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource bgmSource;

        private Dictionary<string, AudioClip> soundDict = new Dictionary<string, AudioClip>();

        private AudioSource audioSource;

        private void Awake()
        {
            // 1. 게임이 시작되면 스피커(AudioSource)를 자기 자신에게 자동으로 붙입니다.
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false; // 시작하자마자 소리나는 것 방지

            // 2. 인스펙터에서 세팅한 리스트를 딕셔너리로 옮겨 담습니다.
            foreach (var entry in soundLibrary)
            {
                if (!string.IsNullOrEmpty(entry.id) && entry.clip != null)
                {
                    soundDict[entry.id] = entry.clip;
                }
            }
        }

        public void Play(AudioClip clip) => sfxSource.PlayOneShot(clip);

        public void PlayWithId(string soundId)
        {
            if (soundDict.TryGetValue(soundId, out AudioClip clip))
            {
                // PlayOneShot은 소리가 겹쳐도 끊기지 않고 자연스럽게 덧대어 재생해줍니다.
                audioSource.PlayOneShot(clip); 
                Debug.Log($"<color=cyan>[사운드]</color> '{soundId}' 효과음 재생됨!");
            }
            else
            {
                // 이름을 잘못 적었거나 빈칸일 때 에러 대신 경고를 띄워줍니다.
                Debug.LogWarning($"<color=orange>[사운드 오류]</color> '{soundId}' 아이디를 찾을 수 없습니다. 인스펙터에 등록했는지, 오타가 없는지 확인하세요!");
            }
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