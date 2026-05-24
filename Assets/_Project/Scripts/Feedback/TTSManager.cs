using UnityEngine;
using System.Collections.Generic;

namespace VirtualFishing.Feedback
{
    public class TTSManager : MonoBehaviour
    {
        [Header("오디오 소스")]
        [Tooltip("TTS 음성을 재생할 AudioSource 컴포넌트")]
        [SerializeField] private AudioSource audioSource;

        [Header("TTS 음성 데이터 매핑")]
        [Tooltip("FeedbackManager에서 호출할 텍스트와 실제 음성 파일(.wav)을 연결하세요.")]
        [SerializeField] private List<TTSData> ttsList = new List<TTSData>();
        
        // 빠른 검색을 위한 내부 딕셔너리
        private Dictionary<string, AudioClip> ttsDictionary = new Dictionary<string, AudioClip>();

        [System.Serializable]
        public struct TTSData
        {
            [TextArea(2, 3)]
            public string textMessage; // 예: "캘리브레이션이 완료되었습니다."
            public AudioClip clip;     // 위 텍스트를 읽어주는 오디오 파일
        }

        private void Awake()
        {
            // AudioSource가 없으면 자동으로 추가
            if (audioSource == null) 
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }
            
            // 인스펙터에서 설정한 리스트를 딕셔너리로 변환하여 검색 속도 최적화
            foreach (var data in ttsList)
            {
                if (!ttsDictionary.ContainsKey(data.textMessage))
                {
                    ttsDictionary.Add(data.textMessage, data.clip);
                }
            }
        }

        public void Speak(string message)
        {
            // 딕셔너리에서 요청받은 텍스트와 똑같은 음성 파일이 있는지 검색
            if (ttsDictionary.TryGetValue(message, out AudioClip clip))
            {
                audioSource.clip = clip;
                audioSource.Play();
                Debug.Log($"<color=cyan>[TTS 재생]</color> {message}");
            }
            else
            {
                // 파일이 없으면 경고 로그 출력 (개발 중 누락 방지)
                Debug.LogWarning($"<color=orange>[TTS 경고]</color> 매핑된 오디오 파일이 없습니다: '{message}'\n인스펙터의 TTS List에 이 텍스트를 추가해주세요.");
            }
        }
    }
}