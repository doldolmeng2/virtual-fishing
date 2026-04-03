using UnityEngine;
using UnityEngine.Events;

namespace VirtualFishing.Core.Events
{
    public class StringEventListener : MonoBehaviour, IGameEventListener<string>
    {
        [SerializeField] private StringEventSO gameEvent;
        [SerializeField] private UnityEvent<string> response;

        private void OnEnable() => gameEvent?.Register(this);
        private void OnDisable() => gameEvent?.Unregister(this);

        public void OnEventRaised(string value) => response?.Invoke(value);
    }
}
