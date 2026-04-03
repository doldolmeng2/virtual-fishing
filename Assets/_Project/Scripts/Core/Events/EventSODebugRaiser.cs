using UnityEngine;
using VirtualFishing.Core.Events;

namespace VirtualFishing.Core
{
    public class EventSODebugRaiser : MonoBehaviour
    {
        [Header("수동 발행할 이벤트 채널")]
        [SerializeField] private VoidEventSO voidEvent;
        [SerializeField] private FloatEventSO floatEvent;
        [SerializeField] private IntEventSO intEvent;

        [Header("발행 시 사용할 값")]
        [SerializeField] private float floatValue = 50f;
        [SerializeField] private int intValue = 1;

        [ContextMenu("Raise Void Event")]
        public void RaiseVoid()
        {
            if (voidEvent != null)
            {
                voidEvent.Raise();
                Debug.Log($"[DebugRaiser] Void Event raised: {voidEvent.name}");
            }
        }

        [ContextMenu("Raise Float Event")]
        public void RaiseFloat()
        {
            if (floatEvent != null)
            {
                floatEvent.Raise(floatValue);
                Debug.Log($"[DebugRaiser] Float Event raised: {floatEvent.name} = {floatValue}");
            }
        }

        [ContextMenu("Raise Int Event")]
        public void RaiseInt()
        {
            if (intEvent != null)
            {
                intEvent.Raise(intValue);
                Debug.Log($"[DebugRaiser] Int Event raised: {intEvent.name} = {intValue}");
            }
        }
    }
}
