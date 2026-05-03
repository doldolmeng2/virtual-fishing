using UnityEngine;
using UnityEngine.InputSystem;
using VirtualFishing.Calibration;
using VirtualFishing.Data;

namespace VirtualFishing.Fishing
{
    /// <summary>
    /// 낚시 시스템 디버그 정보 표시. 키보드/VR 모두에서 동작.
    /// Game 뷰 좌측 상단에 상태 정보 표시 + 캘리브레이션 컨트롤.
    /// </summary>
    public class FishingDebugUI : MonoBehaviour
    {
        [Header("낚시")]
        [SerializeField] private FishingRodController rodController;
        [SerializeField] private FloatController floatController;

        [Header("캘리브레이션 (선택)")]
        [SerializeField] private SeatedPoseCalibrator calibrator;
        [SerializeField] private PlayerDataSO playerData;
        [Tooltip("캘리브레이션 리셋 단축키 (테스트용)")]
        [SerializeField] private Key resetKey = Key.R;

        private void Update()
        {
            // 키보드 단축키로 캘리브레이션 리셋 (반복 테스트용)
            if (calibrator == null || Keyboard.current == null) return;
            if (Keyboard.current[resetKey].wasPressedThisFrame)
            {
                calibrator.ResetCalibration();
            }
        }

        private void OnGUI()
        {
            // 모든 패널이 비어 있으면 그냥 종료
            if (rodController == null && calibrator == null) return;

            GUIStyle style = new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold };
            style.normal.textColor = Color.white;
            GUIStyle headerStyle = new GUIStyle(style) { fontSize = 16 };
            headerStyle.normal.textColor = Color.yellow;
            GUIStyle calibStyle = new GUIStyle(style);
            calibStyle.normal.textColor = new Color(0.5f, 0.95f, 1f);

            float x = 10f, y = 10f, w = 380f, lineH = 20f;
            // 박스 높이를 동적으로 계산 (없는 패널은 제외)
            int rodLines = (rodController != null ? 7 + (floatController != null ? 1 : 0) + 1 : 0);
            int calibLines = (calibrator != null ? 5 : 0); // header + state + arm + safety + button
            int totalLines = rodLines + calibLines;
            GUI.Box(new Rect(x - 5, y - 5, w + 10, lineH * totalLines + 30), "");

            // === 낚시 상태 (rodController가 있을 때만) ===
            if (rodController != null)
            {
                GUI.Label(new Rect(x, y, w, lineH), "=== Fishing Debug ===", headerStyle); y += lineH + 4;
                GUI.Label(new Rect(x, y, w, lineH), $"State: {rodController.CurrentState}", style); y += lineH;
                GUI.Label(new Rect(x, y, w, lineH), $"Grabbed: {rodController.IsGrabbed}", style); y += lineH;
                GUI.Label(new Rect(x, y, w, lineH), $"CastingZone: {rodController.IsInCastingZone}", style); y += lineH;
                GUI.Label(new Rect(x, y, w, lineH), $"HookingZone: {rodController.IsInHookingZone}", style); y += lineH;
                GUI.Label(new Rect(x, y, w, lineH), $"Accel: {rodController.Acceleration:F2}", style); y += lineH;
                GUI.Label(new Rect(x, y, w, lineH), $"ReelSpeed: {rodController.ReelingSpeed:F2}", style); y += lineH;

                if (floatController != null)
                {
                    GUI.Label(new Rect(x, y, w, lineH), $"Float: {floatController.Position.ToString("F2")}", style); y += lineH;
                }
            }

            // === 캘리브레이션 상태 ===
            if (calibrator != null)
            {
                if (rodController != null) y += 4;
                GUI.Label(new Rect(x, y, w, lineH), "=== Calibration ===", headerStyle); y += lineH + 2;
                string status = calibrator.IsCalibrated ? "<DONE>" : "<WAITING>";
                GUI.Label(new Rect(x, y, w, lineH), $"State: {status}", calibStyle); y += lineH;
                if (playerData != null)
                {
                    GUI.Label(new Rect(x, y, w, lineH), $"ArmLength: {playerData.armLength:F2}m", calibStyle); y += lineH;
                    GUI.Label(new Rect(x, y, w, lineH), $"SafetyRadius: {playerData.safetyRadius:F2}m", calibStyle); y += lineH;
                }
                if (GUI.Button(new Rect(x, y, 180f, 24f), $"Reset Calibration ({resetKey})"))
                {
                    calibrator.ResetCalibration();
                }
            }
        }
    }
}
