using System.Collections.Generic;
using UnityEngine;

namespace VirtualFishing.Core.Events
{
    [CreateAssetMenu(menuName = "VirtualFishing/Events/Void Event")]
    public class VoidEventSO : ScriptableObject
    {
        private readonly List<IVoidEventListener> _listeners = new();

        public void Raise()
        {
            for (int i = _listeners.Count - 1; i >= 0; i--)
                _listeners[i].OnEventRaised();
        }

        public void Register(IVoidEventListener listener)
        {
            if (!_listeners.Contains(listener))
                _listeners.Add(listener);
        }

        public void Unregister(IVoidEventListener listener)
        {
            _listeners.Remove(listener);
        }
    }
}
