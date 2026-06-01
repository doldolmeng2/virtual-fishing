using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VirtualFishing.Core.Fish;
using VirtualFishing.Data;

namespace VirtualFishing.MiniGame
{
    /// <summary>
    /// 물고기 위에 떠다니는 World Space UI.
    /// - 좌/우 화살표: 물고기 진행 방향의 반대쪽에 표시. 안쪽 fill 이 phase hold 진행도(0~1)에 따라 차오름.
    /// - 물고기 그림: 정지 이미지, 방향 변경 시 동전 뒤집기(Y축 회전) 모션으로 좌우 전환.
    /// - 텐션 게이지: bar 배경 위를 막대 인디케이터가 좌우로 슬라이딩.
    /// MiniGameUITester 로 실행 중 더미 값을 주입해 테스트할 수 있다.
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    public class FishIndicatorUI : MonoBehaviour
    {
        // ── 방향 화살표 ─────────────────────────────
        [Header("좌측 화살표 (물고기가 오른쪽으로 갈 때 표시)")]
        [Tooltip("좌측 화살표 전체를 감싸는 GameObject. 표시/숨김 토글에 사용됩니다.")]
        [SerializeField] private GameObject arrowLeftGroup;
        [Tooltip("좌측 화살표 안쪽 fill 이미지. Image Type = Filled, Fill Method = Horizontal, Fill Origin = Right 권장.")]
        [SerializeField] private Image arrowLeftFill;

        [Header("우측 화살표 (물고기가 왼쪽으로 갈 때 표시)")]
        [Tooltip("우측 화살표 전체를 감싸는 GameObject. 같은 스프라이트를 X축 flip(localScale.x = -1) 해서 사용 가능.")]
        [SerializeField] private GameObject arrowRightGroup;
        [Tooltip("우측 화살표 안쪽 fill 이미지. Image Type = Filled, Fill Method = Horizontal, Fill Origin = Left 권장.")]
        [SerializeField] private Image arrowRightFill;

        // ── 물고기 그림 ─────────────────────────────
        [Header("물고기 그림 (동전 뒤집기 회전)")]
        [Tooltip("Y축 회전이 적용될 RectTransform. 물고기 이미지의 부모로 두세요.")]
        [SerializeField] private RectTransform fishImageRoot;
        [Tooltip("좌→우 또는 우→좌 회전에 걸리는 시간(초). 0 이면 즉시 전환.")]
        [SerializeField] private float flipDuration = 0.25f;
        [Tooltip("물고기 그림이 '좌측을 향하는' 상태의 localEulerAngles.y. 기본 이미지가 우측을 향하면 180.")]
        [SerializeField] private float facingLeftYRotation = 180f;
        [Tooltip("물고기 그림이 '우측을 향하는' 상태의 localEulerAngles.y. 기본 이미지가 우측을 향하면 0.")]
        [SerializeField] private float facingRightYRotation = 0f;

        // ── 텐션 게이지 ─────────────────────────────
        [Header("텐션 게이지")]
        [Tooltip("막대 인디케이터가 움직이는 트랙 RectTransform. width 기준으로 좌(0)~우(maxTension) 매핑.")]
        [SerializeField] private RectTransform tensionTrack;
        [Tooltip("실제로 좌우로 움직이는 막대 인디케이터. tensionTrack 의 자식으로, anchor/pivot 은 (0.5, 0.5) 권장.")]
        [SerializeField] private RectTransform tensionIndicator;
        [Tooltip("선택 — 수치 숫자 표시용 TextMeshPro")]
        [SerializeField] private TextMeshProUGUI tensionLabel;

        // ── SO 참조 ──────────────────────────────────
        [Header("데이터 참조 (인스펙터에서 연결)")]
        [SerializeField] private TensionDataSO tensionData;

        // ── 위치·스케일 ──────────────────────────────
        [Header("UI 위치")]
        [SerializeField] private Vector3 offset = new Vector3(0f, 1.5f, 0f);
        [SerializeField] private float canvasScale = 0.003f;

        // ── 런타임 참조 ──────────────────────────────
        private FishController _fish;
        private MiniGameManager _manager;
        private TensionCalculator _tension;
        private Camera _camera;

        // Flip 상태
        private Coroutine _flipCoroutine;
        private float _currentTargetY = float.NaN; // 현재 또는 진행 중인 회전 목표

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
            _manager = manager;
            _tension = tension;
            _camera  = Camera.main;

            var canvas = GetComponent<Canvas>();
            if (canvas != null) canvas.worldCamera = _camera;

            _tension.OnTensionChanged += RefreshTension;

            // 초기 상태 적용 (flip 은 즉시)
            RefreshTension(_tension.CurrentTension);
            RefreshDirection(_fish.CurrentMoveMode, instantFlip: true);
            RefreshPhaseHold(0f);
            gameObject.SetActive(true);
        }

        public void Shutdown()
        {
            if (_tension != null) _tension.OnTensionChanged -= RefreshTension;
            _manager = null;
            if (_flipCoroutine != null)
            {
                StopCoroutine(_flipCoroutine);
                _flipCoroutine = null;
            }
            gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            if (_fish == null) return;

            // 물고기 위에 붙어 카메라를 바라봄
            transform.position = _fish.VisualTransform.position + offset;
            if (_camera != null)
                transform.forward = _camera.transform.forward;

            RefreshDirection(_fish.CurrentMoveMode, instantFlip: false);

            if (_manager != null)
                RefreshPhaseHold(_manager.PhaseHoldProgress);
        }

        // ── 방향 (화살표 + 물고기 회전) ─────────────
        // 화살표: 물고기 진행 방향의 "반대쪽"에 표시. Normal(Stop) 일 때는 양쪽 모두 숨김.
        // 물고기 그림: MoveLeft/MoveRight 신호가 오면 해당 방향으로 회전. Stop 일 때는 마지막 방향 유지.
        private void RefreshDirection(FishMoveMode mode, bool instantFlip)
        {
            bool fishMovingLeft  = mode == FishMoveMode.MoveLeft;
            bool fishMovingRight = mode == FishMoveMode.MoveRight;

            // 물고기가 왼쪽으로 → 우측 화살표 표시 (플레이어가 오른쪽으로 당겨야)
            // 물고기가 오른쪽으로 → 좌측 화살표 표시 (플레이어가 왼쪽으로 당겨야)
            if (arrowLeftGroup  != null) arrowLeftGroup .SetActive(fishMovingRight);
            if (arrowRightGroup != null) arrowRightGroup.SetActive(fishMovingLeft);

            // 물고기 그림 방향
            if (fishMovingLeft)
                Flip(facingLeftYRotation, instantFlip);
            else if (fishMovingRight)
                Flip(facingRightYRotation, instantFlip);
            // Stop: 현재 방향 유지 (아무 것도 하지 않음)
        }

        private void Flip(float targetY, bool instant)
        {
            if (fishImageRoot == null) return;
            if (!float.IsNaN(_currentTargetY) && Mathf.Approximately(_currentTargetY, targetY)) return;

            _currentTargetY = targetY;

            if (_flipCoroutine != null)
            {
                StopCoroutine(_flipCoroutine);
                _flipCoroutine = null;
            }

            if (instant || flipDuration <= 0f)
            {
                fishImageRoot.localEulerAngles = new Vector3(0f, targetY, 0f);
            }
            else
            {
                _flipCoroutine = StartCoroutine(FlipRoutine(targetY));
            }
        }

        private IEnumerator FlipRoutine(float targetY)
        {
            float startY = fishImageRoot.localEulerAngles.y;
            float deltaY = Mathf.DeltaAngle(startY, targetY);
            float t = 0f;
            while (t < flipDuration)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / flipDuration);
                float eased = Mathf.SmoothStep(0f, 1f, k); // 부드럽게 가속/감속
                float y = startY + deltaY * eased;
                fishImageRoot.localEulerAngles = new Vector3(0f, y, 0f);
                yield return null;
            }
            fishImageRoot.localEulerAngles = new Vector3(0f, targetY, 0f);
            _flipCoroutine = null;
        }

        // ── 텐션 (막대 인디케이터 슬라이딩) ─────────
        private void RefreshTension(float tension)
        {
            float max = tensionData != null ? tensionData.maxTension : 100f;
            float ratio = max > 0f ? Mathf.Clamp01(tension / max) : 0f;

            if (tensionTrack != null && tensionIndicator != null)
            {
                float trackWidth = tensionTrack.rect.width;
                // 트랙의 왼쪽 끝(-w/2)에서 오른쪽 끝(+w/2)으로 인디케이터 X 좌표 매핑
                float x = Mathf.Lerp(-trackWidth * 0.5f, trackWidth * 0.5f, ratio);
                Vector2 pos = tensionIndicator.anchoredPosition;
                pos.x = x;
                tensionIndicator.anchoredPosition = pos;
            }

            if (tensionLabel != null)
                tensionLabel.text = $"{tension:F0}";
        }

        // ── 페이즈 유지 (화살표 안쪽 fill) ───────────
        // 비활성 화살표의 fill 도 같이 갱신해두면, 다음에 활성화될 때 잔여값 없이 0 부터 시작.
        private void RefreshPhaseHold(float progress)
        {
            if (arrowLeftFill  != null) arrowLeftFill .fillAmount = progress;
            if (arrowRightFill != null) arrowRightFill.fillAmount = progress;
        }

        // ── 테스트용 (MiniGameUITester 에서 호출) ────
        public void SimulateTension(float tension)  => RefreshTension(tension);
        public void SimulateDirection(FishMoveMode mode) => RefreshDirection(mode, instantFlip: false);
        public void SimulatePhaseHold(float progress)    => RefreshPhaseHold(Mathf.Clamp01(progress));
        public void ShowForTest()
        {
            _camera = Camera.main;
            var canvas = GetComponent<Canvas>();
            if (canvas != null) canvas.worldCamera = _camera;
            gameObject.SetActive(true);
        }
    }
}
