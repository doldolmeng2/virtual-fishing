using UnityEngine;

namespace VirtualFishing.MiniGame
{
    /// <summary>
    /// Dev_MiniGame 씬에서 FishIndicatorUI 를 더미값으로 테스트한다.
    /// 재생 버튼만 누르면 텐션과 방향이 자동으로 바뀐다.
    /// 실제 게임 빌드에는 영향 없음 — 씬에서만 사용.
    /// </summary>
    [AddComponentMenu("VirtualFishing/Tests/MiniGame UI Tester")]
    public class MiniGameUITester : MonoBehaviour
    {
        [Header("연결 (인스펙터에서 드래그)")]
        [SerializeField] private FishIndicatorUI ui;

        [Header("자동 시뮬레이션")]
        [SerializeField] private bool autoSimulate = true;
        [Tooltip("텐션이 오르내리는 속도")]
        [SerializeField] private float tensionSpeed = 15f;
        [Tooltip("방향이 바뀌는 간격(초)")]
        [SerializeField] private float directionChangeInterval = 2f;

        [Header("현재 더미값 (실행 중 인스펙터에서 직접 수정 가능)")]
        [Range(0f, 100f)]
        [SerializeField] private float tension = 20f;
        [SerializeField] private FishMoveMode moveMode = FishMoveMode.Stop;

        private bool  _tensionGoingUp = true;
        private float _directionTimer;
        private int   _directionIndex;

        private readonly FishMoveMode[] _directionCycle =
        {
            FishMoveMode.Stop,
            FishMoveMode.MoveLeft,
            FishMoveMode.Stop,
            FishMoveMode.MoveRight
        };

        private void Start()
        {
            // 실제 빌드에서 실수로 포함됐을 경우 자동으로 자신을 제거
            if (!Application.isEditor)
            {
                Destroy(gameObject);
                return;
            }

            if (ui == null)
            {
                Debug.LogError("[MiniGameUITester] FishIndicatorUI 가 연결되지 않았습니다.");
                return;
            }

            ui.ShowForTest();
            PushToUI();
        }

        private void Update()
        {
            if (ui == null) return;

            if (autoSimulate)
                Simulate();

            PushToUI();
        }

        private void Simulate()
        {
            // 텐션 자동 오르내림
            tension += (_tensionGoingUp ? 1f : -1f) * tensionSpeed * Time.deltaTime;
            if (tension >= 100f) { tension = 100f; _tensionGoingUp = false; }
            if (tension <= 0f)   { tension = 0f;   _tensionGoingUp = true; }

            // 방향 자동 순환
            _directionTimer += Time.deltaTime;
            if (_directionTimer >= directionChangeInterval)
            {
                _directionTimer  = 0f;
                _directionIndex  = (_directionIndex + 1) % _directionCycle.Length;
                moveMode         = _directionCycle[_directionIndex];
            }
        }

        private void PushToUI()
        {
            ui.SimulateTension(tension);
            ui.SimulateDirection(moveMode);
        }
    }
}
