using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace VirtualFishing.Fishing
{
    /// <summary>
    /// XR Grab Interactable 이벤트를 FishingRodController에 연결하는 어댑터.
    /// 낚싯대 그랩/릴리즈/속도 추적 담당.
    /// 릴 입력은 별도 FishingReelController(왼손 그랩 회전)가 담당하므로 여기서는 처리하지 않음.
    ///
    /// [한번 잡으면 놓지 못하도록]
    /// 그랩 시 인터랙터의 Select Action Trigger를 Toggle로 바꿔 그립을 떼도 XR select가 유지되게 한다
    /// (State/StateChange 모드는 그립을 떼는 순간 isSelectActive=false → 매 프레임 SelectExit).
    /// 의도적인 토글 해제까지 막기 위해, select가 풀리면 Update에서 즉시 다시 선택한다.
    /// XR select 자체가 유지되므로 XRGrabInteractable의 추종(위치/회전)이 그대로 이어진다.
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

        // 낚싯대를 잡은 인터랙터 — XR select를 강제로 유지(재선택)하는 데 사용
        private IXRSelectInteractor _heldBy;

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
            if (_interactorTransform != null)
            {
                // 컨트롤러 속도/방향 계산 → FishingRodController에 전달 (캐스팅·챔질 가속도 판정용)
                Vector3 currentPos = _interactorTransform.position;
                _currentVelocity = (currentPos - _previousPosition) / Time.deltaTime;
                _previousPosition = currentPos;

                _rodController.UpdateCastingInput(_currentVelocity, _interactorTransform.forward);
            }

            // [한번 잡으면 놓지 못하도록] XR select가 어떤 이유로든 풀리면(의도적 토글 해제 등)
            // 즉시 다시 선택해 XRGrab 추종을 유지한다. Toggle 모드 덕분에 그립을 떼는 것만으로는
            // 풀리지 않으므로, 이 재선택은 사실상 의도적 해제 시도 시에만 1프레임 동작한다.
            if (_heldBy != null && _rodController.IsGrabbed && !_heldBy.IsSelecting(_grabInteractable))
            {
                var manager = _grabInteractable.interactionManager;
                if (manager != null)
                    manager.SelectEnter(_heldBy, (IXRSelectInteractable)_grabInteractable);
            }
        }

        private void OnGrab(SelectEnterEventArgs args)
        {
            _interactorTransform = args.interactorObject.transform;
            _heldBy = args.interactorObject as IXRSelectInteractor;
            _previousPosition = _interactorTransform.position;

            // 그립을 떼도 select가 유지되도록 Toggle 모드로 전환 (한번 잡으면 안 놓음).
            // 그랩 시작 시점에 m_ToggleActive=true가 되어, 이후 isSelectActive가 계속 true로 유지된다.
            if (args.interactorObject is XRBaseInputInteractor inputInteractor)
                inputInteractor.selectActionTrigger = XRBaseInputInteractor.InputTriggerType.Toggle;

            _rodController.OnGrab(_interactorTransform);
        }

        private void OnRelease(SelectExitEventArgs args)
        {
            // 한번 잡으면 놓지 못하도록 — 게임 로직 해제는 차단(no-op)되고,
            // XR select는 Update의 재선택 가드가 즉시 복구한다. (interactor 참조는 유지)
            _rodController.OnRelease();
        }
    }
}
