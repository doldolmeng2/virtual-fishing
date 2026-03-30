using UnityEngine;
using UnityEngine.Events;

namespace VirtualFishing.Core.Events
{
    public class VoidEventListener : MonoBehaviour, IVoidEventListener
    {
        [SerializeField] private VoidEventSO gameEvent;
        [SerializeField] private UnityEvent response;

        private void OnEnable() => gameEvent?.Register(this);
        private void OnDisable() => gameEvent?.Unregister(this);

        public void OnEventRaised() => response?.Invoke();
    }
}
