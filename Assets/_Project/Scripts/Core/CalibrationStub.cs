using UnityEngine;
using VirtualFishing.Core.Events;

namespace VirtualFishing.Core
{
    /// <summary>
    /// CalibrationController 구현 전 임시 스텁.
    /// Start()에서 즉시 캘리브레이션 완료 이벤트를 발생시켜 FishingReady로 전이시킨다.
    /// CalibrationController가 완성되면 이 컴포넌트를 교체한다.
    /// </summary>
    public class CalibrationStub : MonoBehaviour
    {
        [SerializeField] private VoidEventSO onCalibrationComplete;
        [SerializeField] private float delaySeconds = 0.1f;

        private void Start()
        {
            if (delaySeconds <= 0f)
            {
                Fire();
            }
            else
            {
                Invoke(nameof(Fire), delaySeconds);
            }
        }

        private void Fire()
        {
            Debug.Log("[CalibrationStub] 캘리브레이션 완료 (스텁)");
            onCalibrationComplete?.Raise();
        }
    }
}
