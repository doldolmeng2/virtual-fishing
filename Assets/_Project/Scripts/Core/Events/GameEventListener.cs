using UnityEngine;
using UnityEngine.Events;

namespace VirtualFishing.Core.Events
{
    /// <summary>
    /// VoidEventSO를 구독하여 UnityEvent로 전달하는 리스너.
    /// 프리팹에 부착하고 인스펙터에서 이벤트 SO를 연결하면
    /// 해당 이벤트 발생 시 response가 호출됨.
    /// </summary>
    public class VoidEventListener : MonoBehaviour, IVoidEventListener
    {
        [SerializeField] private VoidEventSO gameEvent;
        [SerializeField] private UnityEvent response;

        private void OnEnable() => gameEvent?.Register(this);
        private void OnDisable() => gameEvent?.Unregister(this);

        public void OnEventRaised() => response?.Invoke();
    }

    public class FloatEventListener : MonoBehaviour, IGameEventListener<float>
    {
        [SerializeField] private FloatEventSO gameEvent;
        [SerializeField] private UnityEvent<float> response;

        private void OnEnable() => gameEvent?.Register(this);
        private void OnDisable() => gameEvent?.Unregister(this);

        public void OnEventRaised(float value) => response?.Invoke(value);
    }

    public class IntEventListener : MonoBehaviour, IGameEventListener<int>
    {
        [SerializeField] private IntEventSO gameEvent;
        [SerializeField] private UnityEvent<int> response;

        private void OnEnable() => gameEvent?.Register(this);
        private void OnDisable() => gameEvent?.Unregister(this);

        public void OnEventRaised(int value) => response?.Invoke(value);
    }

    public class StringEventListener : MonoBehaviour, IGameEventListener<string>
    {
        [SerializeField] private StringEventSO gameEvent;
        [SerializeField] private UnityEvent<string> response;

        private void OnEnable() => gameEvent?.Register(this);
        private void OnDisable() => gameEvent?.Unregister(this);

        public void OnEventRaised(string value) => response?.Invoke(value);
    }
}
