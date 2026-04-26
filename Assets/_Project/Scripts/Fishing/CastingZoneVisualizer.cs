using TMPro;
using UnityEngine;
using VirtualFishing.Data;

namespace VirtualFishing.Fishing
{
    /// <summary>
    /// 캐스팅 존 시각화 — 위치/색상/투명도로 진입·홀드 상태 표시,
    /// 옵션으로 World Space TMP_Text를 통해 수치 표시.
    /// </summary>
    [RequireComponent(typeof(MeshRenderer))]
    public class CastingZoneVisualizer : MonoBehaviour
    {
        [SerializeField] private FishingRodController rodController;
        [SerializeField] private GameSettingsSO gameSettings;
        [Tooltip("선택: 릴 컨트롤러 — HUD에 릴 engagement 상태 표시용")]
        [SerializeField] private FishingReelController reelController;

        [Header("표시 조건")]
        [SerializeField] private bool showOnlyWhenGrabbed = true;
        [SerializeField] private bool showOnlyInAttachedState = true;

        [Header("색상")]
        [Tooltip("존 밖일 때 (대기)")]
        [SerializeField] private Color outsideColor = new Color(0.2f, 1f, 0.3f, 0.20f);
        [Tooltip("존 진입 + 홀드 진행 중")]
        [SerializeField] private Color holdingColor = new Color(1f, 0.85f, 0.2f, 0.35f);
        [Tooltip("홀드 충족 — 이탈 시 캐스트 발사 가능")]
        [SerializeField] private Color readyColor = new Color(0.3f, 0.9f, 1f, 0.45f);

        [Header("텍스트 (선택)")]
        [Tooltip("진입/홀드/파워 수치 표시. 비워두면 색상만 변경.")]
        [SerializeField] private TMP_Text infoText;
        [Tooltip("텍스트가 항상 카메라를 보도록 빌보드")]
        [SerializeField] private bool textBillboard = true;

        private MeshRenderer _renderer;
        private Material _material;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private void Awake()
        {
            _renderer = GetComponent<MeshRenderer>();
            _material = _renderer.material; // instance copy → 다른 머티리얼에 영향 안 줌
        }

        private void LateUpdate()
        {
            if (rodController == null || gameSettings == null)
            {
                SetVisible(false);
                return;
            }

            // 표시 여부
            bool show = true;
            if (showOnlyWhenGrabbed && !rodController.IsGrabbed) show = false;
            if (show && showOnlyInAttachedState && rodController.CurrentState != RodState.Attached) show = false;

            SetVisible(show);
            if (!show) return;

            // 위치·크기
            transform.position = rodController.CastingZoneCenter;
            transform.localScale = Vector3.one * gameSettings.castingZoneRadius * 2f;

            // 상태 추출
            bool inZone = rodController.IsInCastingZone;
            float hold = rodController.CastingHoldTime;
            float minHold = Mathf.Max(0.0001f, gameSettings.minCastingHoldTime);
            bool holdMet = hold >= gameSettings.minCastingHoldTime;
            float accel = rodController.Acceleration;
            float power = rodController.PredictedCastingPower;

            // 색상
            Color c;
            if (inZone && holdMet) c = readyColor;
            else if (inZone) c = Color.Lerp(holdingColor, readyColor, Mathf.Clamp01(hold / minHold));
            else c = outsideColor;
            _material.SetColor(BaseColorId, c);

            // 텍스트
            if (infoText != null)
            {
                string rodStatus = rodController.IsGrabbed
                    ? "<color=#7CFFB4>HELD</color>"
                    : "<color=#FF8888>FREE</color>";
                string reelStatus;
                if (reelController == null) reelStatus = "<color=#888888>n/a</color>";
                else if (reelController.IsBeingReeled) reelStatus = $"<color=#7CFFB4>REELING ({rodController.ReelingSpeed:F2})</color>";
                else if (reelController.IsEngaged) reelStatus = "<color=#FFD060>ENGAGED</color>";
                else reelStatus = "<color=#FF8888>idle</color>";

                infoText.text =
                    $"Rod: {rodStatus}   Reel: {reelStatus}\n" +
                    $"Zone: {(inZone ? "<color=#7CFFB4>IN</color>" : "<color=#FF8888>OUT</color>")}\n" +
                    $"Hold: {hold:F2} / {gameSettings.minCastingHoldTime:F2}s {(holdMet ? "<color=#7CFFB4>OK</color>" : "")}\n" +
                    $"Accel: {accel:F1} m/s {(accel >= gameSettings.minCastingAcceleration ? "<color=#7CFFB4>OK</color>" : "")}\n" +
                    $"Power: {power:F1}";

                if (textBillboard && Camera.main != null)
                {
                    var cam = Camera.main.transform;
                    infoText.transform.rotation = Quaternion.LookRotation(infoText.transform.position - cam.position, Vector3.up);
                }
            }
        }

        private void SetVisible(bool visible)
        {
            if (_renderer.enabled != visible) _renderer.enabled = visible;
            if (infoText != null && infoText.gameObject.activeSelf != visible)
                infoText.gameObject.SetActive(visible);
        }
    }
}
