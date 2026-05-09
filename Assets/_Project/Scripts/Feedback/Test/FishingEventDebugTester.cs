using UnityEngine;
using VirtualFishing.Core.Events;

namespace VirtualFishing.Feedback.Test
{
    public class FishingEventDebugTester : MonoBehaviour
    {
        [Header("Void Events (13종)")]
        public VoidEventSO onAccountLoaded;
        public VoidEventSO onCalibrationComplete;
        public VoidEventSO onRodGrabbed;
        public VoidEventSO onCastStarted;
        public VoidEventSO onWaterLanded;
        public VoidEventSO onBiteOccurred;
        public VoidEventSO onHookSuccess;
        public VoidEventSO onHookFailed;
        public VoidEventSO onMiniGameResult;
        public VoidEventSO onAccountSaved;
        public VoidEventSO onRodStateChanged;
        public VoidEventSO onSceneLoaded;
        public VoidEventSO onTrackingLost;

        [Header("Float Events (미니게임)")]
        public FloatEventSO onTensionChanged;
        [Range(0f, 100f)] public float tensionValue = 85f; // 기본 경고치 초과 설정

        public FloatEventSO onSuccessGaugeChanged;
        [Range(0f, 100f)] public float successGaugeValue = 50f;

        [Header("Int Events (안전 경고)")]
        public IntEventSO onSafetyWarning;
        [Tooltip("0: None, 1: NearBoundary, 2: Outside, 3: Emergency")]
        [Range(0, 3)] public int safetyLevelValue = 1;


        #region ContextMenu 발동 메서드 (우클릭 메뉴)

        // --- 시나리오 01. 시스템 & 캘리브레이션 ---
        [ContextMenu("01. On Account Loaded (로그인 완료)")]
        public void RaiseAccountLoaded() => RaiseVoid(onAccountLoaded, "OnAccountLoaded");

        [ContextMenu("02. On Calibration Complete (캘리 완료)")]
        public void RaiseCalibrationComplete() => RaiseVoid(onCalibrationComplete, "OnCalibrationComplete");

        [ContextMenu("03. On Scene Loaded (씬 로드 완료)")]
        public void RaiseSceneLoaded() => RaiseVoid(onSceneLoaded, "OnSceneLoaded");

        [ContextMenu("04. On Tracking Lost (트래킹 소실 경고)")]
        public void RaiseTrackingLost() => RaiseVoid(onTrackingLost, "OnTrackingLost");

        // --- 시나리오 02 & 03. 낚시 캐스팅 및 입질 ---
        [ContextMenu("05. On Rod Grabbed (낚싯대 잡음)")]
        public void RaiseRodGrabbed() => RaiseVoid(onRodGrabbed, "OnRodGrabbed");

        [ContextMenu("06. On Cast Started (캐스팅 투척)")]
        public void RaiseCastStarted() => RaiseVoid(onCastStarted, "OnCastStarted");

        [ContextMenu("07. On Water Landed (찌 착수)")]
        public void RaiseWaterLanded() => RaiseVoid(onWaterLanded, "OnWaterLanded");

        [ContextMenu("08. On Bite Occurred (입질 발생/챔질 UI)")]
        public void RaiseBiteOccurred() => RaiseVoid(onBiteOccurred, "OnBiteOccurred");

        [ContextMenu("09. On Hook Success (챔질 성공)")]
        public void RaiseHookSuccess() => RaiseVoid(onHookSuccess, "OnHookSuccess");

        [ContextMenu("10. On Hook Failed (챔질 실패)")]
        public void RaiseHookFailed() => RaiseVoid(onHookFailed, "OnHookFailed");

        // --- 시나리오 04 & 05. 미니게임 및 보상 ---
        [ContextMenu("11. On Tension Changed (장력 갱신)")]
        public void RaiseTensionChanged()
        {
            if (onTensionChanged != null) { onTensionChanged.Raise(tensionValue); Debug.Log($"<color=yellow>[테스트]</color> OnTensionChanged: {tensionValue}"); }
            else LogMissing("OnTensionChanged");
        }

        [ContextMenu("12. On Success Gauge Changed (성공 게이지)")]
        public void RaiseSuccessGaugeChanged()
        {
            if (onSuccessGaugeChanged != null) { onSuccessGaugeChanged.Raise(successGaugeValue); Debug.Log($"<color=yellow>[테스트]</color> OnSuccessGaugeChanged: {successGaugeValue}"); }
            else LogMissing("OnSuccessGaugeChanged");
        }

        [ContextMenu("13. On Mini Game Result (보상 UI 켜기)")]
        public void RaiseMiniGameResult() => RaiseVoid(onMiniGameResult, "OnMiniGameResult");

        // --- 시나리오 06 & 08. 안전 및 시스템 종료 ---
        [ContextMenu("14. On Safety Warning (안전 구역 경고)")]
        public void RaiseSafetyWarning()
        {
            if (onSafetyWarning != null) { onSafetyWarning.Raise(safetyLevelValue); Debug.Log($"<color=yellow>[테스트]</color> OnSafetyWarning Level: {safetyLevelValue}"); }
            else LogMissing("OnSafetyWarning");
        }

        [ContextMenu("15. On Account Saved (저장 완료/종료 UI)")]
        public void RaiseAccountSaved() => RaiseVoid(onAccountSaved, "OnAccountSaved");

        [ContextMenu("16. On Rod State Changed (상태 변경 로그)")]
        public void RaiseRodStateChanged() => RaiseVoid(onRodStateChanged, "OnRodStateChanged");

        #endregion

        // 내부 도우미 함수
        private void RaiseVoid(VoidEventSO ev, string eventName)
        {
            if (ev != null)
            {
                ev.Raise();
                Debug.Log($"<color=yellow>[테스트 발사]</color> {eventName} 이벤트 발행됨!");
            }
            else LogMissing(eventName);
        }

        private void LogMissing(string eventName) => Debug.LogWarning($"<color=orange>[테스트 에러]</color> 인스펙터에 {eventName} 에셋이 비어있습니다.");
    }
}