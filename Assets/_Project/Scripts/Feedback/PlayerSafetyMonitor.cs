using System.Collections;
using UnityEngine;

namespace VirtualFishing.Safety
{
    public class PlayerSafetyMonitor : MonoBehaviour
    {
        [Header("Shared Data SO (Mock)")]
        // 실제 프로젝트 병합 시 코어팀의 PlayerDataSO, GameSettingsSO 타입으로 변경
        [SerializeField] private ScriptableObject playerData;

        [Header("Event Broadcasters")]
        // IntEventSO: 경고 레벨을 타 시스템에 전파
        [SerializeField] private ScriptableObject safetyWarningEvent;

        private bool isMonitoring = false;
        private SafetyWarningLevel currentLevel = SafetyWarningLevel.None; // GameEnums.cs 활용

        public void StartMonitoring()
        {
            if (isMonitoring) return;
            isMonitoring = true;
            StartCoroutine(MonitorRoutine());
        }

        public void StopMonitoring()
        {
            isMonitoring = false;
            StopAllCoroutines();
        }

        private IEnumerator MonitorRoutine()
        {
            float checkInterval = 0.2f; // Settings SO에서 가져올 값
            float safetyRadius = 1.5f;  // PlayerData SO에서 가져올 값
            float nearDistance = 0.3f;

            // 중앙 좌표 초기화 (실제로는 PlayerData SO에서 로드)
            Vector2 centerPos = Vector2.zero;

            while (isMonitoring)
            {
                // 1. HMD 위치 업데이트 (수평면 기준)
                Vector2 currentPos = new Vector2(Camera.main.transform.position.x, Camera.main.transform.position.z);
                float distance = Vector2.Distance(centerPos, currentPos);

                // 2. 경고 레벨 판정
                SafetyWarningLevel newLevel = SafetyWarningLevel.None;

                if (distance >= safetyRadius)
                {
                    newLevel = SafetyWarningLevel.Outside;
                    // Emergency 카운트다운 로직 추가 필요
                }
                else if (distance >= safetyRadius - nearDistance)
                {
                    newLevel = SafetyWarningLevel.NearBoundary;
                }

                // 3. 상태 변화가 있을 때만 이벤트 발행
                if (newLevel != currentLevel)
                {
                    currentLevel = newLevel;
                    BroadcastWarning(currentLevel);
                }

                yield return new WaitForSeconds(checkInterval);
            }
        }

        private void BroadcastWarning(SafetyWarningLevel level)
        {
            // TODO: safetyWarningEvent.Raise((int)level); 
            Debug.Log($"[SafetyMonitor] 경고 레벨 변경: {level}");
        }
    }
}