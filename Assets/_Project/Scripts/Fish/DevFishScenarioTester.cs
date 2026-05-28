using UnityEngine;
using UnityEngine.SceneManagement;
using VirtualFishing.Fishing;
using VirtualFishing.MiniGame;

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

        private const string DevFishSceneName = "Dev_Fish";

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
        }

        public void StartBiteTimer()
        {
            RefreshReferences();
            fishSpawner?.StartBiteTimer();
            Debug.Log("[DevFishScenarioTester] Bite timer started.");
        }

        public void ForceBiteNow()
        {
            RefreshReferences();
            fishSpawner?.DebugForceBiteImmediately();
            Debug.Log("[DevFishScenarioTester] Forced immediate bite.");
        }

        public void CancelBiteAndClear()
        {
            RefreshReferences();
            fishSpawner?.CancelBite();
            fishController?.ResetFish();
            Debug.Log("[DevFishScenarioTester] Bite cancelled and fish cleared.");
        }

        public void ApplyPhase(FishPhase phase)
        {
            RefreshReferences();
            fishController?.SetPhase(phase);
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

        public void PreviewHookSuccess()
        {
            RefreshReferences();
            fishController?.PreviewHookSuccess();
        }

        public void TryStartMiniGame()
        {
            RefreshReferences();
            fishController?.TryStartMiniGame();
        }

        public void SimulateCatchSuccess()
        {
            RefreshReferences();
            miniGameManager?.EndWith(MiniGameResult.Caught);
            if (fishController != null && !fishController.IsHookSuccessPreviewActive)
            {
                fishController.PreviewHookSuccess();
            }
            Debug.Log("[DevFishScenarioTester] Simulated catch success.");
        }

        public void SimulateLineBreak()
        {
            RefreshReferences();
            miniGameManager?.EndWith(MiniGameResult.LineBreak);
            fishController?.SimulateLineBreak();
            rodController?.ReelIn();
            Debug.Log("[DevFishScenarioTester] Simulated line break.");
        }

        public void SimulateFishEscape()
        {
            RefreshReferences();
            miniGameManager?.EndWith(MiniGameResult.FishEscaped);
            fishController?.SimulateFishEscape();
            rodController?.ReelIn();
            Debug.Log("[DevFishScenarioTester] Simulated fish escape.");
        }

        public void SimulateRodRelease()
        {
            RefreshReferences();
            rodController?.OnRelease();
            fishController?.ResetFish();
            Debug.Log("[DevFishScenarioTester] Simulated rod release.");
        }

        public void SimulateReelIn()
        {
            RefreshReferences();
            rodController?.ReelIn();
            fishController?.ResetFish();
            Debug.Log("[DevFishScenarioTester] Simulated reel-in reset.");
        }

        private void OnGUI()
        {
            if (!showOverlay || (onlyInDevFishScene && SceneManager.GetActiveScene().name != DevFishSceneName))
            {
                return;
            }

            const int width = 360;
            GUILayout.BeginArea(new Rect(12f, 12f, width, Screen.height - 24f), GUI.skin.box);
            GUILayout.Label("Dev_Fish Scenario Tester");
            GUILayout.Label($"Fish: {fishController?.SpeciesName ?? "-"}");
            GUILayout.Label($"Phase: {fishController?.CurrentPhase.ToString() ?? "-"} / Move: {fishController?.CurrentMoveMode.ToString() ?? "-"}");
            GUILayout.Label($"Rod: {rodController?.CurrentState.ToString() ?? "not found"}");
            GUILayout.Label($"MiniGame Gauge: {(miniGameManager != null ? miniGameManager.SuccessGauge.ToString("F1") : "-")}");

            GUILayout.Space(6f);
            if (GUILayout.Button("Start Bite Timer")) StartBiteTimer();
            if (GUILayout.Button("Force Bite Now")) ForceBiteNow();
            if (GUILayout.Button("Cancel Bite + Clear Fish")) CancelBiteAndClear();

            GUILayout.Space(6f);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("P1")) ApplyPhase(FishPhase.Phase1);
            if (GUILayout.Button("P2")) ApplyPhase(FishPhase.Phase2);
            if (GUILayout.Button("P3")) ApplyPhase(FishPhase.Phase3);
            if (GUILayout.Button("P4")) ApplyPhase(FishPhase.Phase4);
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Random Move")) TriggerRandomMove();

            GUILayout.Space(6f);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Pull 25%")) PreviewReelPull(25f);
            if (GUILayout.Button("Pull 60%")) PreviewReelPull(60f);
            if (GUILayout.Button("Pull 100%")) PreviewReelPull(100f);
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Try Start MiniGame")) TryStartMiniGame();
            if (GUILayout.Button("Hook Success Preview")) PreviewHookSuccess();
            if (GUILayout.Button("Catch Success + NamuFX")) SimulateCatchSuccess();
            if (GUILayout.Button("Line Break")) SimulateLineBreak();
            if (GUILayout.Button("Fish Escape")) SimulateFishEscape();
            if (GUILayout.Button("Rod Release")) SimulateRodRelease();
            if (GUILayout.Button("Reel In / Reset")) SimulateReelIn();

            GUILayout.Space(6f);
            GUILayout.Label("Hotkeys: Space bite, Enter force bite, 1-4 phase, Q/W/E pull, C catch, L line break, X escape, V release, R reel-in, S reset");
            GUILayout.EndArea();
        }
    }
}
