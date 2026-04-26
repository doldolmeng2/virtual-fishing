using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace VirtualFishing.Fishing
{
    /// <summary>
    /// XR Grab Interactable의 호버/셀렉트 상태를 머티리얼 색상으로 표시.
    /// MaterialPropertyBlock 사용 → 머티리얼 인스턴스 누수 없음.
    /// </summary>
    [RequireComponent(typeof(XRBaseInteractable))]
    public class XRGrabHighlight : MonoBehaviour
    {
        [Header("대상 렌더러 (비우면 자식 전체 자동 검색)")]
        [SerializeField] private List<Renderer> targetRenderers = new();

        [Header("색상")]
        [SerializeField] private Color hoverColor = new Color(1f, 0.9f, 0.3f, 1f);    // 노랑
        [SerializeField] private Color selectColor = new Color(0.4f, 1f, 0.5f, 1f);   // 초록
        [SerializeField] private Color rejectColor = new Color(1f, 0.3f, 0.3f, 1f);   // 빨강 (필터 거부)

        [Header("렌더링")]
        [Tooltip("강조 강도 (0=원본 색상 유지, 1=완전히 강조 색상으로 덮기)")]
        [SerializeField, Range(0f, 1f)] private float tintStrength = 0.7f;
        [Tooltip("Emission도 같이 켤지 (URP Lit에서 효과적)")]
        [SerializeField] private bool useEmission = true;
        [SerializeField, Range(0f, 4f)] private float emissionIntensity = 1.5f;

        private XRBaseInteractable _interactable;
        private MaterialPropertyBlock _mpb;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        private bool _isSelected;
        private bool _isHovered;

        private void Awake()
        {
            _interactable = GetComponent<XRBaseInteractable>();
            _mpb = new MaterialPropertyBlock();
            if (targetRenderers.Count == 0)
                targetRenderers.AddRange(GetComponentsInChildren<Renderer>(includeInactive: true));
        }

        private void OnEnable()
        {
            _interactable.hoverEntered.AddListener(OnHoverEntered);
            _interactable.hoverExited.AddListener(OnHoverExited);
            _interactable.selectEntered.AddListener(OnSelectEntered);
            _interactable.selectExited.AddListener(OnSelectExited);
            ApplyHighlight(Color.white, 0f); // 초기화: 원본 색상
        }

        private void OnDisable()
        {
            _interactable.hoverEntered.RemoveListener(OnHoverEntered);
            _interactable.hoverExited.RemoveListener(OnHoverExited);
            _interactable.selectEntered.RemoveListener(OnSelectEntered);
            _interactable.selectExited.RemoveListener(OnSelectExited);
            ApplyHighlight(Color.white, 0f);
        }

        private void OnHoverEntered(HoverEnterEventArgs args)
        {
            _isHovered = true;
            UpdateColor();
        }

        private void OnHoverExited(HoverExitEventArgs args)
        {
            _isHovered = false;
            UpdateColor();
        }

        private void OnSelectEntered(SelectEnterEventArgs args)
        {
            _isSelected = true;
            UpdateColor();
        }

        private void OnSelectExited(SelectExitEventArgs args)
        {
            _isSelected = false;
            UpdateColor();
        }

        private void UpdateColor()
        {
            if (_isSelected) ApplyHighlight(selectColor, tintStrength);
            else if (_isHovered) ApplyHighlight(hoverColor, tintStrength);
            else ApplyHighlight(Color.white, 0f);
        }

        /// <summary>외부에서 일시적인 거부 표시 (예: 필터 fail)</summary>
        public void FlashReject()
        {
            ApplyHighlight(rejectColor, tintStrength);
            CancelInvoke(nameof(ResetTint));
            Invoke(nameof(ResetTint), 0.25f);
        }

        private void ResetTint() => UpdateColor();

        private void ApplyHighlight(Color tint, float strength)
        {
            if (targetRenderers == null || targetRenderers.Count == 0) return;

            for (int i = 0; i < targetRenderers.Count; i++)
            {
                var r = targetRenderers[i];
                if (r == null) continue;

                r.GetPropertyBlock(_mpb);

                // 원본 색상과 강조색 lerp (URP는 _BaseColor, Built-in은 _Color)
                Color baseColor = Color.Lerp(Color.white, tint, strength);
                _mpb.SetColor(BaseColorId, baseColor);
                _mpb.SetColor(ColorId, baseColor);

                if (useEmission)
                {
                    Color emission = strength > 0.01f
                        ? tint * emissionIntensity * strength
                        : Color.black;
                    _mpb.SetColor(EmissionColorId, emission);
                }

                r.SetPropertyBlock(_mpb);
            }
        }
    }
}
