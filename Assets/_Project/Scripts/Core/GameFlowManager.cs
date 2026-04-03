using System;
using UnityEngine;
using VirtualFishing.Core.Events;

namespace VirtualFishing.Core
{
    public class GameFlowManager : MonoBehaviour, Interfaces.IGameFlowManager
    {
        [Header("현재 상태 (디버그용 — 인스펙터에서 확인)")]
        [SerializeField] private GameState currentState = GameState.Login;

        [Header("이벤트 채널")]
        [SerializeField] private VoidEventSO onCalibrationComplete;
        [SerializeField] private VoidEventSO onSceneLoaded;
        [SerializeField] private VoidEventSO onMiniGameResult;
        [SerializeField] private IntEventSO onSafetyWarning;

        public GameState CurrentState => currentState;
        public event Action<GameState, GameState> OnStateChanged;

        private GameState _stateBeforeWarning;

        public void TransitionTo(GameState newState)
        {
            if (currentState == newState) return;

            var prev = currentState;
            currentState = newState;
            OnStateChanged?.Invoke(prev, newState);
            Debug.Log($"[GameFlow] {prev} → {newState}");
        }

        public void RestorePreviousState()
        {
            TransitionTo(_stateBeforeWarning);
        }

        #region SO Event 핸들러 (인스펙터에서 EventListener로 연결)

        public void HandleCalibrationComplete()
        {
            if (currentState == GameState.Calibration)
                TransitionTo(GameState.FishingReady);
        }

        public void HandleSceneLoaded()
        {
            if (currentState == GameState.Calibration)
                TransitionTo(GameState.FishingReady);
        }

        public void HandleMiniGameResult()
        {
            TransitionTo(GameState.Result);
        }

        public void HandleSafetyWarning(int level)
        {
            var warningLevel = (SafetyWarningLevel)level;

            switch (warningLevel)
            {
                case SafetyWarningLevel.None:
                    if (currentState is GameState.Warning or GameState.Paused)
                        RestorePreviousState();
                    break;

                case SafetyWarningLevel.Outside:
                    _stateBeforeWarning = currentState;
                    TransitionTo(GameState.Warning);
                    break;

                case SafetyWarningLevel.Emergency:
                    TransitionTo(GameState.Paused);
                    break;
            }
        }

        #endregion

        #region 디버그용 — 에디터에서 수동 테스트

        [ContextMenu("Debug: → Calibration")]
        private void DebugToCalibration() => TransitionTo(GameState.Calibration);

        [ContextMenu("Debug: → FishingReady")]
        private void DebugToFishingReady() => TransitionTo(GameState.FishingReady);

        [ContextMenu("Debug: → MiniGame")]
        private void DebugToMiniGame() => TransitionTo(GameState.MiniGame);

        [ContextMenu("Debug: → Result")]
        private void DebugToResult() => TransitionTo(GameState.Result);

        [ContextMenu("Debug: → ExitSequence")]
        private void DebugToExit() => TransitionTo(GameState.ExitSequence);

        #endregion
    }
}
