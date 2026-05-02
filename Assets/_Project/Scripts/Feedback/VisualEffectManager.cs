using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VirtualFishing.Interfaces;
using VirtualFishing.Data;

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
        
        [Header("Test Settings")]
        [Tooltip("테스트를 위한 임시 안전 구역 반지름입니다.")]
        public float testSafetyRadius = 1.5f; // 인스펙터에서 수정 가능하도록 추가

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

        private Dictionary<string, GameObject> activeEffects = new Dictionary<string, GameObject>();

        public void ShowEffect(string effectId, Vector3 position)
        {
            if (effectDict.TryGetValue(effectId, out GameObject prefab))
            {
                HideEffect(effectId);

                GameObject instance = Instantiate(prefab, position, Quaternion.identity);
                instance.SetActive(true);
                activeEffects[effectId] = instance;

                // [추가] BlueGrid인 경우 PlayerDataSO의 radius에 맞춰 크기 조절
                if (effectId == "BlueGrid")
                {
                    ApplySafetyRadius(instance);
                }
            }
        }

        private void ApplySafetyRadius(GameObject gridObject)
        {
            // 1. PlayerDataSO에서 safetyRadius 값 읽어오기
            //float radius = ((PlayerDataSO)playerData).safetyRadius;
            float radius = testSafetyRadius; // 테스트용 임시 변수 사용 (PlayerDataSO 대신)

            // 2. RectTransform 가져오기 (World Space Canvas의 Image인 경우)
            RectTransform rectTransform = gridObject.GetComponentInChildren<RectTransform>();

            if (rectTransform != null)
            {
                // 반지름의 2배를 너비와 높이로 설정 (1 unit = 1m 기준)
                float diameter = radius * 2f;
                rectTransform.sizeDelta = new Vector2(diameter, diameter);
                
                Debug.Log($"[VisualManager] 안전 구역 그리드 크기 설정 완료: {diameter}m (반지름: {radius}m)");
            }
        }
    
        public void HideEffect(string effectId)
        {
            if (activeEffects.TryGetValue(effectId, out GameObject instance))
            {
                if (instance != null) Destroy(instance);
                activeEffects.Remove(effectId);
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