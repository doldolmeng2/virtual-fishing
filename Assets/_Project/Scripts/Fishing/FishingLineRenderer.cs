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
        [SerializeField] private float lineWidth = 0.003f;
        [SerializeField] private int segmentCount = 30;
        [SerializeField] private float sagAmount = 0.15f;
        [SerializeField] private float waterSurfaceY = 0f;

        private LineRenderer _lineRenderer;

        private void Awake()
        {
            _lineRenderer = GetComponent<LineRenderer>();

            // URP 호환 머티리얼 자동 생성
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            var mat = new Material(shader);
            mat.color = new Color(0.9f, 0.9f, 0.9f, 1f);
            _lineRenderer.material = mat;
            _lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _lineRenderer.receiveShadows = false;

            _lineRenderer.startWidth = lineWidth;
            _lineRenderer.endWidth = lineWidth;
            _lineRenderer.positionCount = segmentCount;
            _lineRenderer.useWorldSpace = true;
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
            float lineLength = Vector3.Distance(start, end);

            // 거리에 따라 처짐량 조절 (멀수록 더 처짐)
            float dynamicSag = sagAmount * Mathf.Clamp01(lineLength / 5f);

            for (int i = 0; i < segmentCount; i++)
            {
                float t = (float)i / (segmentCount - 1);
                Vector3 point = Vector3.Lerp(start, end, t);

                // 자연스러운 현수선 처짐 (catenary 근사)
                // 양쪽 끝에서는 처짐 없고, 중앙으로 갈수록 처짐
                float sagT = 1f - Mathf.Pow(2f * t - 1f, 2f); // 포물선 커브
                point.y -= sagT * dynamicSag;

                // 수면 아래로 내려가지 않도록 제한
                if (point.y < waterSurfaceY && i > 0 && i < segmentCount - 1)
                {
                    point.y = waterSurfaceY;
                }

                _lineRenderer.SetPosition(i, point);
            }
        }
    }
}
