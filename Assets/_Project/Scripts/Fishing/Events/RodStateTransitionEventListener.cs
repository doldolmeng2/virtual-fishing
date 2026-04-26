using UnityEngine;
using UnityEngine.Events;
using VirtualFishing.Core.Events;

namespace VirtualFishing.Fishing.Events
{
    /// <summary>
    /// RodStateTransitionEventSO를 듣고 UnityEvent로 forward하는 bridge 컴포넌트.
    /// 씬에 배치 후 인스펙터에서 gameEvent + response 와이어링.
    /// </summary>
    public class RodStateTransitionEventListener : MonoBehaviour, IGameEventListener<RodStateTransition>
    {
        [SerializeField] private RodStateTransitionEventSO gameEvent;
        [SerializeField] private UnityEvent<RodStateTransition> response;

        private void OnEnable() => gameEvent?.Register(this);
        private void OnDisable() => gameEvent?.Unregister(this);

        public void OnEventRaised(RodStateTransition value) => response?.Invoke(value);
    }
}
