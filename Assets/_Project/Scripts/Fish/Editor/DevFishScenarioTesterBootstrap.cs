#if UNITY_EDITOR && ENABLE_INPUT_SYSTEM
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using VirtualFishing.Core.Fish;

namespace VirtualFishing.EditorTools
{
    [InitializeOnLoad]
    public static class DevFishScenarioTesterBootstrap
    {
        private const string DevFishSceneName = "Dev_Fish";
        private const string TesterObjectName = "DevFishScenarioTester_Runtime";

        private static DevFishScenarioTester scenarioTester;

        static DevFishScenarioTesterBootstrap()
        {
            EditorApplication.update += OnEditorUpdate;
        }

        private static void OnEditorUpdate()
        {
            if (!EditorApplication.isPlaying || EditorApplication.isPaused || EditorApplication.isCompiling)
            {
                scenarioTester = null;
                return;
            }

            if (SceneManager.GetActiveScene().name != DevFishSceneName)
            {
                scenarioTester = null;
                return;
            }

            EnsureScenarioTester();
            HandleHotkeys();
        }

        private static void EnsureScenarioTester()
        {
            if (scenarioTester != null)
            {
                return;
            }

            scenarioTester = Object.FindFirstObjectByType<DevFishScenarioTester>();
            if (scenarioTester != null)
            {
                scenarioTester.RefreshReferences();
                return;
            }

            GameObject testerObject = new(TesterObjectName)
            {
                hideFlags = HideFlags.DontSave
            };
            scenarioTester = testerObject.AddComponent<DevFishScenarioTester>();
            scenarioTester.RefreshReferences();
            Debug.Log("[DevFishScenarioTesterBootstrap] Dev_Fish scenario tester overlay created.");
        }

        private static void HandleHotkeys()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || scenarioTester == null)
            {
                return;
            }

            if (keyboard.fKey.wasPressedThisFrame)
            {
                scenarioTester.ForceFakeBite();
            }

            if (keyboard.pKey.wasPressedThisFrame)
            {
                scenarioTester.TriggerRandomMove();
            }

            if (keyboard.qKey.wasPressedThisFrame)
            {
                scenarioTester.PreviewReelPull(0f);
            }

            if (keyboard.wKey.wasPressedThisFrame)
            {
                scenarioTester.PreviewReelPull(50f);
            }

            if (keyboard.eKey.wasPressedThisFrame)
            {
                scenarioTester.PreviewReelPull(100f);
            }

            if (keyboard.cKey.wasPressedThisFrame)
            {
                scenarioTester.SimulateFullSuccessFlow();
            }

            if (keyboard.rKey.wasPressedThisFrame)
            {
                scenarioTester.CancelBiteAndClear();
            }
        }
    }
}
#endif
