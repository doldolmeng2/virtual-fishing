using UnityEngine;
using UnityEngine.Events;

namespace VirtualFishing.Core.Events
{
    public class IntEventListener : MonoBehaviour, IGameEventListener<int>
    {
        [SerializeField] private IntEventSO gameEvent;
        [SerializeField] private UnityEvent<int> response;

        private void OnEnable() => gameEvent?.Register(this);
        private void OnDisable() => gameEvent?.Unregister(this);

        public void OnEventRaised(int value) => response?.Invoke(value);
    }
}
