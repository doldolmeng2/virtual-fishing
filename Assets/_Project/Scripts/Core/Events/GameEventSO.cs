using System.Collections.Generic;
using UnityEngine;

namespace VirtualFishing.Core.Events
{
    public interface IGameEventListener<T>
    {
        void OnEventRaised(T value);
    }

    public interface IVoidEventListener
    {
        void OnEventRaised();
    }

    /// <summary>
    /// SO 기반 Observer 패턴의 제네릭 이벤트 채널.
    /// 프리팹 간 직접 참조 없이 인스펙터에서 드래그&드롭으로 연결 가능.
    /// </summary>
    public abstract class GameEventSO<T> : ScriptableObject
    {
        private readonly List<IGameEventListener<T>> _listeners = new();

        public void Raise(T value)
        {
            for (int i = _listeners.Count - 1; i >= 0; i--)
                _listeners[i].OnEventRaised(value);
        }

        public void Register(IGameEventListener<T> listener)
        {
            if (!_listeners.Contains(listener))
                _listeners.Add(listener);
        }

        public void Unregister(IGameEventListener<T> listener)
        {
            _listeners.Remove(listener);
        }
    }
}
