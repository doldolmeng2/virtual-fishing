using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using VirtualFishing.Core.Events;

namespace VirtualFishing.Core
{
    public class SceneTransitionService : MonoBehaviour, Interfaces.ISceneService
    {
        [Header("이벤트 채널")]
        [SerializeField] private VoidEventSO onSceneLoaded;

        public event Action OnSceneLoaded;

        public void LoadScene(string sceneName)
        {
            StartCoroutine(LoadSceneAsync(sceneName));
        }

        private IEnumerator LoadSceneAsync(string sceneName)
        {
            Debug.Log($"[SceneTransition] Loading: {sceneName}");

            var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            if (op == null)
            {
                Debug.LogError($"[SceneTransition] Scene not found: {sceneName}");
                yield break;
            }

            while (!op.isDone)
                yield return null;

            Debug.Log($"[SceneTransition] Loaded: {sceneName}");
            onSceneLoaded?.Raise();
            OnSceneLoaded?.Invoke();
        }
    }
}
