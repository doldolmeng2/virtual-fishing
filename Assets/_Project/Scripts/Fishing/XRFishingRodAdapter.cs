using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace VirtualFishing.Fishing
{
    /// <summary>
    /// XR Grab Interactable 이벤트를 FishingRodController에 연결하는 어댑터.
    /// XR Interaction Toolkit의 그랩/릴리즈를 FishingRodController.OnGrab/OnRelease로 전달.
    /// </summary>
    [RequireComponent(typeof(XRGrabInteractable))]
    [RequireComponent(typeof(FishingRodController))]
    public class XRFishingRodAdapter : MonoBehaviour
    {
        private XRGrabInteractable _grabInteractable;
        private FishingRodController _rodController;

        private Transform _interactorTransform;
        private Vector3 _previousPosition;
        private Vector3 _currentVelocity;

        private void Awake()
        {
            _grabInteractable = GetComponent<XRGrabInteractable>();
            _rodController = GetComponent<FishingRodController>();
        }

        private void OnEnable()
        {
            _grabInteractable.selectEntered.AddListener(OnGrab);
            _grabInteractable.selectExited.AddListener(OnRelease);
        }

        private void OnDisable()
        {
            _grabInteractable.selectEntered.RemoveListener(OnGrab);
            _grabInteractable.selectExited.RemoveListener(OnRelease);
        }

        private void Update()
        {
            if (_interactorTransform == null) return;

            // 컨트롤러 속도/방향 계산 → FishingRodController에 전달
            Vector3 currentPos = _interactorTransform.position;
            _currentVelocity = (currentPos - _previousPosition) / Time.deltaTime;
            _previousPosition = currentPos;

            _rodController.UpdateCastingInput(_currentVelocity, _interactorTransform.forward);
        }

        private void OnGrab(SelectEnterEventArgs args)
        {
            _interactorTransform = args.interactorObject.transform;
            _previousPosition = _interactorTransform.position;
            _rodController.OnGrab(_interactorTransform);
        }

        private void OnRelease(SelectExitEventArgs args)
        {
            _rodController.OnRelease();
            _interactorTransform = null;
        }
    }
}
