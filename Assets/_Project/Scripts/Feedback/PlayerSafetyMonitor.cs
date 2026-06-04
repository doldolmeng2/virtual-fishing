using System.Collections;
using UnityEngine;
using VirtualFishing.Core.Events;
using VirtualFishing.Data; 

namespace VirtualFishing.Safety
{
    public class PlayerSafetyMonitor : MonoBehaviour
    {
        [Header("Shared Data SO")]
        [SerializeField] private PlayerDataSO playerData;

        [Header("Event Broadcasters")]
        [SerializeField] private IntEventSO safetyWarningEvent;

        [Header("Safety Settings (Local)")]
        [SerializeField] private float checkInterval = 0.2f;
        [SerializeField] private float emergencyTimeout = 10f;
        [SerializeField] private float nearDistance = 0.3f;

        [Header("Debug")]
        [Tooltip("체크하면 콘솔창에 현재 거리와 반경을 실시간으로 표시합니다.")]
        [SerializeField] private bool showDebugLog = true;

        private bool isMonitoring = false;
        private SafetyWarningLevel currentLevel = SafetyWarningLevel.None; 
        private float outsideTimer = 0f;
        
        // 중심점을 고정할 변수
        private Vector2 centerPos;

        public void StartMonitoring()
        {
            if (isMonitoring) return;
            isMonitoring = true;
            outsideTimer = 0f; 
            
            // ★ 중요: 모니터링이 시작되는 순간의 위치를 '안전 중심점'으로 고정합니다.
            if (playerData != null)
            {
                centerPos = new Vector2(playerData.currentPosition.x, playerData.currentPosition.z);
            }

            StartCoroutine(MonitorRoutine());
        }

        public void StopMonitoring()
        {
            isMonitoring = false;
            StopAllCoroutines();
        }

        private IEnumerator MonitorRoutine()
        {
            // 예외 처리 1: SO 연결 안 됨
            if (playerData == null)
            {
                Debug.LogError("<color=red>[SafetyMonitor] PlayerDataSO가 연결되지 않았습니다!</color>");
                isMonitoring = false;
                yield break;
            }

            // 예외 처리 2: MainCamera 태그 없음
            if (Camera.main == null)
            {
                Debug.LogError("<color=red>[SafetyMonitor] 씬에 'MainCamera' 태그를 가진 카메라(VR HMD)가 없습니다!</color>");
                isMonitoring = false;
                yield break;
            }

            while (isMonitoring)
            {
                // 현재 HMD(카메라) 위치 가져오기
                Vector2 currentPos = new Vector2(Camera.main.transform.position.x, Camera.main.transform.position.z);
                
                float safetyRadius = playerData.safetyRadius; 
                float distance = Vector2.Distance(centerPos, currentPos);

                // ★ 디버그 로그: 거리가 제대로 계산되고 있는지 확인
                if (showDebugLog)
                {
                    Debug.Log($"[SafetyMonitor] 중심점: {centerPos} | 현재: {currentPos} | 거리: {distance:F2}m / 반경: {safetyRadius}m");
                }

                // 경고 레벨 판정
                SafetyWarningLevel newLevel = SafetyWarningLevel.None;

                if (distance >= safetyRadius)
                {
                    outsideTimer += checkInterval;
                    if (outsideTimer >= emergencyTimeout)
                        newLevel = SafetyWarningLevel.Emergency;
                    else
                        newLevel = SafetyWarningLevel.Outside;
                }
                else if (distance >= safetyRadius - nearDistance)
                {
                    outsideTimer = 0f; 
                    newLevel = SafetyWarningLevel.NearBoundary;
                }
                else
                {
                    outsideTimer = 0f; 
                    newLevel = SafetyWarningLevel.None;
                }

                // 상태 변화 발행
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
            if (safetyWarningEvent != null)
            {
                safetyWarningEvent.Raise((int)level); 
            }
            else
            {
                Debug.LogWarning("[SafetyMonitor] safetyWarningEvent가 인스펙터에 연결되지 않아 UI로 신호를 보낼 수 없습니다!");
            }
            
            if (level == SafetyWarningLevel.Emergency)
                Debug.Log($"<color=red>[SafetyMonitor] 이탈 시간이 초과되어 Emergency 상태로 전환됩니다.</color>");
            else
                Debug.Log($"<color=yellow>[SafetyMonitor] 경고 레벨 변경: {level}</color>");
        }
    }
}