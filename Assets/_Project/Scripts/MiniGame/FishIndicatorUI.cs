using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VirtualFishing.Core.Fish;
using VirtualFishing.Data;

namespace VirtualFishing.MiniGame
{
    /// <summary>
    /// 물고기 위에 떠다니는 World Space UI.
    /// 방향 화살표(Left / Center / Right)와 텐션 바를 표시한다.
    /// MiniGameUITester 로 실행 중 더미 값을 주입해 테스트할 수 있다.
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    public class FishIndicatorUI : MonoBehaviour
    {
        // ── 방향 화살표 ─────────────────────────────
        [Header("방향 화살표 이미지")]
        [SerializeField] private Image arrowLeft;
        [SerializeField] private Image arrowCenter;
        [SerializeField] private Image arrowRight;

        [Header("방향 활성/비활성 색상")]
        [SerializeField] private Color colorArrowActive   = Color.white;
        [SerializeField] private Color colorArrowInactive = new Color(1f, 1f, 1f, 0.2f);

        // ── 텐션 게이지 ─────────────────────────────
        [Header("텐션 게이지 바")]
        [Tooltip("Image Type = Filled, Fill Method = Horizontal 으로 설정하세요")]
        [SerializeField] private Image tensionFill;
        [Tooltip("선택 — 수치 숫자 표시용 TextMeshPro")]
        [SerializeField] private TextMeshProUGUI tensionLabel;

        [Header("텐션 색상")]
        [SerializeField] private Color colorSafe     = new Color(0.2f, 0.85f, 0.3f);
        [SerializeField] private Color colorWarning  = new Color(1f,   0.75f, 0f);
        [SerializeField] private Color colorCritical = new Color(0.9f, 0.1f,  0.1f);

        // ── SO 참조 ──────────────────────────────────
        [Header("데이터 참조 (인스펙터에서 연결)")]
        [SerializeField] private TensionDataSO tensionData;

        // ── 위치·스케일 ──────────────────────────────
        [Header("UI 위치")]
        [SerializeField] private Vector3 offset     = new Vector3(0f, 1.5f, 0f);
        [SerializeField] private float   canvasScale = 0.003f;

        // ── 런타임 참조 ──────────────────────────────
        private FishController    _fish;
        private TensionCalculator _tension;
        private Camera            _camera;

        // ────────────────────────────────────────────
        private void Awake()
        {
            var canvas = GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            transform.localScale = Vector3.one * canvasScale;
            gameObject.SetActive(false);
        }

        /// <summary>미니게임 시작 시 MiniGameManager 가 호출한다.</summary>
        public void Initialize(FishController fish, MiniGameManager manager, TensionCalculator tension)
        {
            _fish    = fish;
            _tension = tension;
            _camera  = Camera.main;

            var canvas = GetComponent<Canvas>();
            if (canvas != null) canvas.worldCamera = _camera;

            _tension.OnTensionChanged += RefreshTension;

            RefreshTension(_tension.CurrentTension);
            gameObject.SetActive(true);
        }

        public void Shutdown()
        {
            if (_tension != null) _tension.OnTensionChanged -= RefreshTension;
            gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            if (_fish == null) return;

            transform.position = _fish.VisualTransform.position + offset;
            if (_camera != null)
                transform.forward = _camera.transform.forward;

            RefreshDirection(_fish.CurrentMoveMode);
        }

        // ── 방향 ─────────────────────────────────────
        private void RefreshDirection(FishMoveMode mode)
        {
            SetArrow(arrowLeft,   mode == FishMoveMode.MoveLeft);
            SetArrow(arrowCenter, mode == FishMoveMode.Stop);
            SetArrow(arrowRight,  mode == FishMoveMode.MoveRight);
        }

        private void SetArrow(Image img, bool active)
        {
            if (img == null) return;
            img.color = active ? colorArrowActive : colorArrowInactive;
        }

        // ── 텐션 ─────────────────────────────────────
        private void RefreshTension(float tension)
        {
            float max   = tensionData != null ? tensionData.maxTension : 100f;
            float ratio = Mathf.Clamp01(tension / max);

            if (tensionFill != null)
            {
                tensionFill.fillAmount = ratio;
                tensionFill.color      = GetTensionColor(tension, max);
            }

            if (tensionLabel != null)
                tensionLabel.text = $"{tension:F0}";
        }

        private Color GetTensionColor(float tension, float max)
        {
            float dangerRatio = tensionData != null ? tensionData.dangerThreshold / max : 0.9f;
            float ratio       = tension / max;

            if (ratio < dangerRatio)
                return Color.Lerp(colorSafe, colorWarning,
                    Mathf.InverseLerp(0f, dangerRatio, ratio));
            else
                return Color.Lerp(colorWarning, colorCritical,
                    Mathf.InverseLerp(dangerRatio, 1f, ratio));
        }

        // ── 테스트용 (MiniGameUITester 에서 호출) ────
        public void SimulateTension(float tension)  => RefreshTension(tension);
        public void SimulateDirection(FishMoveMode mode) => RefreshDirection(mode);
        public void ShowForTest()
        {
            _camera = Camera.main;
            var canvas = GetComponent<Canvas>();
            if (canvas != null) canvas.worldCamera = _camera;
            gameObject.SetActive(true);
        }
    }
}
