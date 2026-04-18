using System.Collections;
using UnityEngine;

namespace VirtualFishing.Safety
{
    public class PlayerSafetyMonitor : MonoBehaviour
    {
        [Header("Shared Data SO (Mock)")]
        // 실제 프로젝트 병합 시 코어팀의 PlayerDataSO, GameSettingsSO 타입으로 변경
        [SerializeField] private ScriptableObject playerData;
        [SerializeField] private ScriptableObject gameSettings;

        [Header("Event Broadcasters")]
        // IntEventSO: 경고 레벨을 타 시스템에 전파
        [SerializeField] private ScriptableObject safetyWarningEvent;

        private bool isMonitoring = false;
        private SafetyWarningLevel currentLevel = SafetyWarningLevel.None; // GameEnums.cs 활용

        // 구역 이탈 시간을 추적하는 타이머 변수
        private float outsideTimer = 0f;

        public void StartMonitoring()
        {
            if (isMonitoring) return;
            isMonitoring = true;
            outsideTimer = 0f; // 모니터링 시작 시 타이머 초기화
            StartCoroutine(MonitorRoutine());
        }

        public void StopMonitoring()
        {
            isMonitoring = false;
            StopAllCoroutines();
        }

        private IEnumerator MonitorRoutine()
        {
            // 실제 SO가 연결되면 아래 변수들을 SO에서 동적으로 읽어오도록 주석을 해제합니다.
            float checkInterval = 0.2f;    // 예: ((GameSettingsSO)gameSettings).safetyCheckInterval;
            float emergencyTimeout = 10f;  // 예: ((GameSettingsSO)gameSettings).emergencyTimeout;
            float safetyRadius = 1.5f;     // 예: ((PlayerDataSO)playerData).safetyRadius;
            float nearDistance = 0.3f;

            // 하드코딩된 Vector2.zero 대신 캘리브레이션된 중심점을 사용하도록 수정 필요
            // 예: Vector3 pos = ((PlayerDataSO)playerData).currentPosition;
            // Vector2 centerPos = new Vector2(pos.x, pos.z);
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
                    // 구역을 완전히 벗어난 경우 타이머 누적
                    outsideTimer += checkInterval;

                    if (outsideTimer >= emergencyTimeout)
                    {
                        newLevel = SafetyWarningLevel.Emergency;
                    }
                    else
                    {
                        newLevel = SafetyWarningLevel.Outside;
                    }
                }
                else if (distance >= safetyRadius - nearDistance)
                {
                    outsideTimer = 0f; // 구역 내로 들어왔으므로 타이머 초기화
                    newLevel = SafetyWarningLevel.NearBoundary;
                }
                else
                {
                    outsideTimer = 0f; // 안전 구역 내로 들어오면 타이머 리셋
                    newLevel = SafetyWarningLevel.None;
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
            // TODO: 실제 병합 시 주석 해제하여 이벤트 발행
            // ((IntEventSO)safetyWarningEvent).Raise((int)level); 
            
            if (level == SafetyWarningLevel.Emergency)
                Debug.Log($"<color=red>[SafetyMonitor] 이탈 시간이 초과되어 Emergency 상태로 전환됩니다.</color>");
            else
                Debug.Log($"[SafetyMonitor] 경고 레벨 변경: {level}");
        }
    }
}