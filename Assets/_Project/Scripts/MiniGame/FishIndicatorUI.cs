using TMPro;
using UnityEngine;
using VirtualFishing.Core.Fish;
using VirtualFishing.Data;

namespace VirtualFishing.MiniGame
{
    // World Space Canvas에 붙이는 컴포넌트. 물고기 위를 따라다니며 미니게임 상태를 텍스트로 표시한다.
    [RequireComponent(typeof(Canvas))]
    public class FishIndicatorUI : MonoBehaviour
    {
        [Header("텍스트 (비워두면 Awake에서 자동 생성)")]
        [SerializeField] private TextMeshProUGUI directionText;
        [SerializeField] private TextMeshProUGUI tensionText;
        [SerializeField] private TextMeshProUGUI gaugeText;
        [SerializeField] private TextMeshProUGUI timeText;

        [Header("위치")]
        [SerializeField] private Vector3 offset = new Vector3(0f, 1.5f, 0f);
        [SerializeField] private float canvasScale = 0.003f;

        private FishController _fish;
        private MiniGameManager _manager;
        private TensionCalculator _tension;
        private Camera _camera;

        private void Awake()
        {
            var canvas = GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            var rt = GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(200f, 130f);
            transform.localScale = Vector3.one * canvasScale;

            if (directionText == null) directionText = CreateText("Direction", new Vector2(0f,  50f), 22);
            if (tensionText   == null) tensionText   = CreateText("Tension",   new Vector2(0f,  20f), 16);
            if (gaugeText     == null) gaugeText     = CreateText("Gauge",     new Vector2(0f, -10f), 16);
            if (timeText      == null) timeText      = CreateText("Time",      new Vector2(0f, -40f), 16);

            gameObject.SetActive(false);
        }

        public void Initialize(FishController fish, MiniGameManager manager, TensionCalculator tension)
        {
            _fish    = fish;
            _manager = manager;
            _tension = tension;
            _camera  = Camera.main;

            var canvas = GetComponent<Canvas>();
            if (canvas != null) canvas.worldCamera = _camera;

            _tension.OnTensionChanged      += RefreshTension;
            _manager.OnSuccessGaugeChanged += RefreshGauge;

            gameObject.SetActive(true);
        }

        public void Shutdown()
        {
            if (_tension != null) _tension.OnTensionChanged      -= RefreshTension;
            if (_manager != null) _manager.OnSuccessGaugeChanged -= RefreshGauge;

            gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            if (_fish == null) return;

            transform.position = _fish.VisualTransform.position + offset;

            // 항상 카메라 방향을 바라봄 (billboard)
            if (_camera != null)
                transform.forward = _camera.transform.forward;

            RefreshDirection();
            RefreshTime();
        }

        private void RefreshDirection()
        {
            if (directionText == null) return;
            directionText.text = _fish.CurrentMoveMode switch
            {
                FishMoveMode.MoveLeft  => "<< 왼쪽",
                FishMoveMode.MoveRight => "오른쪽 >>",
                _                      => "● 정지"
            };
        }

        private void RefreshTime()
        {
            if (timeText == null || _manager == null) return;
            timeText.text = $"시간: {_manager.RemainingTime:F1}s";
        }

        private void RefreshTension(float tension)
        {
            if (tensionText == null) return;
            string zone = _tension.CurrentZone == TensionZone.Critical
                ? "<color=red>위험</color>"
                : "<color=green>안전</color>";
            tensionText.text = $"장력: {tension:F0} / 100  {zone}";
        }

        private void RefreshGauge(float gauge)
        {
            if (gaugeText == null) return;
            gaugeText.text = $"게이지: {gauge:F0} %";
        }

        private TextMeshProUGUI CreateText(string goName, Vector2 anchoredPosition, float fontSize)
        {
            var go  = new GameObject(goName);
            go.transform.SetParent(transform, false);

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.fontSize  = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color     = Color.white;

            var rt = tmp.GetComponent<RectTransform>();
            rt.anchoredPosition = anchoredPosition;
            rt.sizeDelta        = new Vector2(200f, 30f);

            return tmp;
        }
    }
}
