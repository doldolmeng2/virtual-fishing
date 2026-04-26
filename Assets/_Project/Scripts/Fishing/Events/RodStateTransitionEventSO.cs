using UnityEngine;
using VirtualFishing.Core.Events;

namespace VirtualFishing.Fishing.Events
{
    /// <summary>
    /// 낚싯대 상태 전이를 (이전, 현재) 쌍으로 발행하는 SO 이벤트 채널.
    /// 기존 VoidEventSO 기반 OnRodStateChanged를 대체.
    /// </summary>
    [CreateAssetMenu(menuName = "VirtualFishing/Events/Rod State Transition Event")]
    public class RodStateTransitionEventSO : GameEventSO<RodStateTransition>
    {
    }
}
