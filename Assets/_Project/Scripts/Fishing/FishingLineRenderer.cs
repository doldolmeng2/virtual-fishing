using UnityEngine;

namespace VirtualFishing.Fishing
{
    [RequireComponent(typeof(LineRenderer))]
    public class FishingLineRenderer : MonoBehaviour
    {
        [Header("참조")]
        [SerializeField] private Transform rodTip;
        [SerializeField] private FloatController floatController;

        [Header("설정")]
        [SerializeField] private float lineWidth = 0.005f;
        [SerializeField] private int segmentCount = 20;
        [SerializeField] private float sagAmount = 0.3f;

        private LineRenderer _lineRenderer;

        private void Awake()
        {
            _lineRenderer = GetComponent<LineRenderer>();
            _lineRenderer.startWidth = lineWidth;
            _lineRenderer.endWidth = lineWidth;
            _lineRenderer.positionCount = segmentCount;
            _lineRenderer.enabled = false;
        }

        private void LateUpdate()
        {
            bool shouldRender = rodTip != null
                && floatController != null
                && floatController.gameObject.activeSelf;

            _lineRenderer.enabled = shouldRender;

            if (!shouldRender) return;

            Vector3 start = rodTip.position;
            Vector3 end = floatController.Position;

            for (int i = 0; i < segmentCount; i++)
            {
                float t = (float)i / (segmentCount - 1);
                Vector3 point = Vector3.Lerp(start, end, t);

                // 포물선 처짐: 중앙이 가장 많이 처짐
                float sag = Mathf.Sin(t * Mathf.PI) * sagAmount;
                point.y -= sag;

                _lineRenderer.SetPosition(i, point);
            }
        }
    }
}
