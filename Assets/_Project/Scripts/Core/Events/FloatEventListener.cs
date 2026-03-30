using UnityEngine;
using UnityEngine.Events;

namespace VirtualFishing.Core.Events
{
    public class FloatEventListener : MonoBehaviour, IGameEventListener<float>
    {
        [SerializeField] private FloatEventSO gameEvent;
        [SerializeField] private UnityEvent<float> response;

        private void OnEnable() => gameEvent?.Register(this);
        private void OnDisable() => gameEvent?.Unregister(this);

        public void OnEventRaised(float value) => response?.Invoke(value);
    }
}
