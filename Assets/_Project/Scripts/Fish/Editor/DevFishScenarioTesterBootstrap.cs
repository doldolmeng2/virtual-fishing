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

            if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame)
            {
                scenarioTester.ForceBiteNow();
            }

            if (keyboard.pKey.wasPressedThisFrame)
            {
                scenarioTester.TriggerRandomMove();
            }

            if (keyboard.qKey.wasPressedThisFrame)
            {
                scenarioTester.PreviewReelPull(25f);
            }

            if (keyboard.wKey.wasPressedThisFrame)
            {
                scenarioTester.PreviewReelPull(60f);
            }

            if (keyboard.eKey.wasPressedThisFrame)
            {
                scenarioTester.PreviewReelPull(100f);
            }

            if (keyboard.mKey.wasPressedThisFrame)
            {
                scenarioTester.TryStartMiniGame();
            }

            if (keyboard.cKey.wasPressedThisFrame)
            {
                scenarioTester.SimulateCatchSuccess();
            }

            if (keyboard.lKey.wasPressedThisFrame)
            {
                scenarioTester.SimulateLineBreak();
            }

            if (keyboard.xKey.wasPressedThisFrame)
            {
                scenarioTester.SimulateFishEscape();
            }

            if (keyboard.vKey.wasPressedThisFrame)
            {
                scenarioTester.SimulateRodRelease();
            }

            if (keyboard.rKey.wasPressedThisFrame)
            {
                scenarioTester.SimulateReelIn();
            }
        }
    }
}
#endif
