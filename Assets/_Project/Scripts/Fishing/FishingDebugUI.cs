using UnityEngine;

namespace VirtualFishing.Fishing
{
    /// <summary>
    /// 낚시 시스템 디버그 정보 표시. 키보드/VR 모두에서 동작.
    /// Game 뷰 좌측 상단에 상태 정보 표시.
    /// </summary>
    public class FishingDebugUI : MonoBehaviour
    {
        [SerializeField] private FishingRodController rodController;
        [SerializeField] private FloatController floatController;

        private void OnGUI()
        {
            if (rodController == null) return;

            GUIStyle style = new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold };
            style.normal.textColor = Color.white;
            GUIStyle headerStyle = new GUIStyle(style) { fontSize = 16 };
            headerStyle.normal.textColor = Color.yellow;

            float x = 10f, y = 10f, w = 380f, lineH = 20f;
            GUI.Box(new Rect(x - 5, y - 5, w + 10, lineH * 10 + 10), "");

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
    }
}
