using UnityEngine;
using UnityEngine.SceneManagement;
using VirtualFishing.Data;
using VirtualFishing.Fishing;
using VirtualFishing.MiniGame;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace VirtualFishing.Core.Fish
{
    public class DevFishScenarioTester : MonoBehaviour
    {
        [SerializeField] private bool onlyInDevFishScene = true;
        [SerializeField] private bool showOverlay = true;
        [SerializeField] private FishController fishController;
        [SerializeField] private FishSpawner fishSpawner;
        [SerializeField] private FishingRodController rodController;
        [SerializeField] private MiniGameManager miniGameManager;
        [SerializeField] private FishSpeciesDataSO[] testSpecies = new FishSpeciesDataSO[0];

        private const string DevFishSceneName = "Dev_Fish";
        private Vector2 scrollPosition;

        private void Awake()
        {
            RefreshReferences();
        }

        private void Update()
        {
            if (onlyInDevFishScene && SceneManager.GetActiveScene().name != DevFishSceneName)
            {
                return;
            }

            if (fishController == null || fishSpawner == null)
            {
                RefreshReferences();
            }
        }

        public void RefreshReferences()
        {
            if (fishController == null) fishController = FindObjectOfType<FishController>();
            if (fishSpawner == null) fishSpawner = FindObjectOfType<FishSpawner>();
            if (rodController == null) rodController = FindObjectOfType<FishingRodController>();
            if (miniGameManager == null) miniGameManager = FindObjectOfType<MiniGameManager>();
            LoadDefaultTestSpecies();
        }

        public void ForceSpeciesBite(FishSpeciesDataSO speciesData)
        {
            RefreshReferences();
            fishSpawner?.DebugForceBite(speciesData);
            Debug.Log($"[DevFishScenarioTester] Forced species bite: {(speciesData != null ? speciesData.DisplayName : "null")}");
        }

        public void ForceFakeBite()
        {
            RefreshReferences();
            fishSpawner?.DebugForceFakeBite();
            Debug.Log("[DevFishScenarioTester] Forced fake bite.");
        }

        public void CancelBiteAndClear()
        {
            RefreshReferences();
            fishSpawner?.CancelBite();
            fishController?.ResetFish();
            Debug.Log("[DevFishScenarioTester] Bite cancelled and fish cleared.");
        }

        public void TriggerRandomMove()
        {
            RefreshReferences();
            fishController?.TriggerRandomMoveMode();
        }

        public void PreviewReelPull(float percent)
        {
            RefreshReferences();
            fishController?.PreviewReelingPull(percent);
        }

        public void ShowHookSuccessPresentation()
        {
            RefreshReferences();
            EnsureFishForPresentation();

            if (fishController == null || string.IsNullOrEmpty(fishController.SpeciesName))
            {
                Debug.LogWarning("[DevFishScenarioTester] Hook success presentation skipped: no fish is active.");
                return;
            }

            fishController.PreviewHookSuccess();
            Debug.Log("[DevFishScenarioTester] Hook success presentation shown.");
        }

        public void SimulateCatchSuccess()
        {
            RefreshReferences();
            if (miniGameManager == null)
            {
                ShowHookSuccessPresentation();
                return;
            }

            miniGameManager?.EndWith(MiniGameResult.Caught);
            if (fishController != null && !fishController.IsHookSuccessPreviewActive)
            {
                Debug.LogWarning("[DevFishScenarioTester] Catch success event did not attach fish. Check MiniGameManager/FishController event wiring.");
            }
            Debug.Log("[DevFishScenarioTester] Simulated catch success.");
        }

        public void SimulateFullSuccessFlow()
        {
            RefreshReferences();
            if (fishController == null)
            {
                Debug.LogWarning("[DevFishScenarioTester] Full success skipped: FishController not found.");
                return;
            }

            if (miniGameManager == null)
            {
                ShowHookSuccessPresentation();
                return;
            }

            EnsureFishForPresentation();

            fishController.TryStartMiniGame();
            miniGameManager?.EndWith(MiniGameResult.Caught);

            if (!fishController.IsHookSuccessPreviewActive)
            {
                Debug.LogWarning("[DevFishScenarioTester] Full success flow did not attach fish through actual mini-game events.");
            }

            Debug.Log("[DevFishScenarioTester] Simulated full success flow.");
        }

        private void EnsureFishForPresentation()
        {
            if (fishController != null && !string.IsNullOrEmpty(fishController.SpeciesName))
            {
                return;
            }

            if (testSpecies == null || testSpecies.Length == 0)
            {
                LoadDefaultTestSpecies();
            }

            if (testSpecies != null && testSpecies.Length > 0 && testSpecies[0] != null)
            {
                ForceSpeciesBite(testSpecies[0]);
                return;
            }

            fishSpawner?.DebugForceBiteImmediately();
        }

        private void LoadDefaultTestSpecies()
        {
            if (testSpecies != null && testSpecies.Length > 0)
            {
                return;
            }

#if UNITY_EDITOR
            testSpecies = new[]
            {
                AssetDatabase.LoadAssetAtPath<FishSpeciesDataSO>("Assets/_Project/SO/FishDB/Test/Fish_Crucian.asset"),
                AssetDatabase.LoadAssetAtPath<FishSpeciesDataSO>("Assets/_Project/SO/FishDB/Test/Fish_Carp.asset"),
                AssetDatabase.LoadAssetAtPath<FishSpeciesDataSO>("Assets/_Project/SO/FishDB/Test/Fish_Bass.asset"),
                AssetDatabase.LoadAssetAtPath<FishSpeciesDataSO>("Assets/_Project/SO/FishDB/Test/Fish_Catfish.asset"),
                AssetDatabase.LoadAssetAtPath<FishSpeciesDataSO>("Assets/_Project/SO/FishDB/Test/Fish_Snakehead.asset")
            };
#endif
        }

        private void OnGUI()
        {
            if (!showOverlay || (onlyInDevFishScene && SceneManager.GetActiveScene().name != DevFishSceneName))
            {
                return;
            }

            const int width = 380;
            GUILayout.BeginArea(new Rect(12f, 12f, width, Screen.height - 24f), GUI.skin.box);
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
            GUILayout.Label("Dev_Fish Scenario Tester");
            GUILayout.Label($"Fish: {fishController?.SpeciesName ?? "-"}");
            GUILayout.Label($"Move: {fishController?.CurrentMoveMode.ToString() ?? "-"}");
            GUILayout.Label($"Rod: {rodController?.CurrentState.ToString() ?? "not found"}");
            GUILayout.Label($"MiniGame Gauge: {(miniGameManager != null ? miniGameManager.SuccessGauge.ToString("F1") : "-")}");

            GUILayout.Space(6f);
            GUILayout.Label("Bite Events");
            if (GUILayout.Button("Fake Bite")) ForceFakeBite();

            GUILayout.Space(6f);
            GUILayout.Label("Fish Spawn");
            if (testSpecies == null || testSpecies.Length == 0)
            {
                LoadDefaultTestSpecies();
            }

            if (testSpecies != null)
            {
                for (int i = 0; i < testSpecies.Length; i++)
                {
                    FishSpeciesDataSO speciesData = testSpecies[i];
                    using (new GUILayout.HorizontalScope())
                    {
                        string label = speciesData != null ? $"{speciesData.DisplayName} Bite" : $"Missing Species {i + 1}";
                        if (GUILayout.Button(label))
                        {
                            ForceSpeciesBite(speciesData);
                        }
                    }
                }
            }

            GUILayout.Space(6f);
            GUILayout.Label("Move Trigger");
            if (GUILayout.Button("Random Left / Right / Stop")) TriggerRandomMove();

            GUILayout.Space(6f);
            GUILayout.Label("Reeling Pull");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Pull 0%")) PreviewReelPull(0f);
            if (GUILayout.Button("Pull 50%")) PreviewReelPull(50f);
            if (GUILayout.Button("Pull 100%")) PreviewReelPull(100f);
            GUILayout.EndHorizontal();

            GUILayout.Space(6f);
            GUILayout.Label("Catch Flow");
            if (GUILayout.Button("Test Catch Success")) ShowHookSuccessPresentation();
            if (miniGameManager != null)
            {
                if (GUILayout.Button("Actual Full Success Flow")) SimulateFullSuccessFlow();
                if (GUILayout.Button("Actual Catch Success Event")) SimulateCatchSuccess();
            }

            GUILayout.Space(6f);
            if (GUILayout.Button("Clear Fish")) CancelBiteAndClear();
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }
    }
}
