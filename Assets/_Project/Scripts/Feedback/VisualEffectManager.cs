using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VirtualFishing.Interfaces;

namespace VirtualFishing.Feedback
{
    public class VisualEffectManager : MonoBehaviour, IVisualFeedback
    {
        [System.Serializable]
        public struct EffectEntry
        {
            public string id;
            public GameObject prefab;
        }

        [Header("VFX Library")]
        [SerializeField] private List<EffectEntry> effectLibrary;

        [Header("Screen Fade UI")]
        [SerializeField] private Image fadeOverlay; // VR 카메라 캔버스에 부착된 검은색 전체 화면 이미지

        private Dictionary<string, GameObject> effectDict;
        private Coroutine fadeCoroutine;

        private void Awake()
        {
            effectDict = new Dictionary<string, GameObject>();
            foreach (var entry in effectLibrary)
            {
                effectDict[entry.id] = entry.prefab;
            }
        }

        public void ShowEffect(string effectId, Vector3 position)
        {
            if (effectDict.TryGetValue(effectId, out GameObject prefab))
            {
                ShowEffect(prefab, position);
            }
            else
            {
                Debug.LogWarning($"[VisualManager] VFX ID 누락: {effectId}");
            }
        }

        public void ShowEffect(GameObject prefab, Vector3 position)
        {
            if (prefab != null)
            {
                Instantiate(prefab, position, Quaternion.identity);
            }
        }

        public void FadeScreen(float targetAlpha, float duration)
        {
            if (fadeOverlay == null) return;

            if (fadeCoroutine != null)
                StopCoroutine(fadeCoroutine);

            fadeCoroutine = StartCoroutine(FadeRoutine(targetAlpha, duration));
        }

        private IEnumerator FadeRoutine(float targetAlpha, float duration)
        {
            Color color = fadeOverlay.color;
            float startAlpha = color.a;
            float time = 0f;

            while (time < duration)
            {
                time += Time.deltaTime;
                color.a = Mathf.Lerp(startAlpha, targetAlpha, time / duration);
                fadeOverlay.color = color;
                yield return null;
            }

            color.a = targetAlpha;
            fadeOverlay.color = color;
        }

        public void ShowPassthrough(bool enable)
        {
            // XR 기기(Meta Quest 등)의 패스스루 API 활성화/비활성화 로직을 여기에 구현합니다.
            Debug.Log($"[VisualManager] 패스스루 모드 전환: {enable}");
        }
    }
}