using System;
using System.Collections.Generic;
using UnityEngine;
using VirtualFishing.Core.Events;

namespace VirtualFishing.Core
{
    public class GameFlowManager : MonoBehaviour, Interfaces.IGameFlowManager
    {
        #region 상태 전이 테이블

        private static readonly HashSet<(GameState from, GameState to)> AllowedTransitions = new()
        {
            (GameState.Login,        GameState.Calibration),
            (GameState.Calibration,  GameState.FishingReady),
            (GameState.FishingReady, GameState.Fishing),
            (GameState.FishingReady, GameState.ExitSequence),
            (GameState.Fishing,      GameState.MiniGame),
            (GameState.Fishing,      GameState.FishingReady),
            (GameState.MiniGame,     GameState.Result),
            (GameState.MiniGame,     GameState.FishingReady),
            (GameState.Result,       GameState.FishingReady),
            (GameState.Result,       GameState.ExitSequence),
        };

        #endregion

        [Header("현재 상태 (디버그용 — 인스펙터에서 확인)")]
        [SerializeField] private GameState currentState = GameState.Calibration;

        [Header("이벤트 채널")]
        [SerializeField] private VoidEventSO onCalibrationComplete;
        [SerializeField] private VoidEventSO onMiniGameResult;
        [SerializeField] private IntEventSO onSafetyWarning;

        public GameState CurrentState => currentState;
        public event Action<GameState, GameState> OnStateChanged;

        private GameState _stateBeforeWarning;

        public void TransitionTo(GameState newState)
        {
            if (currentState == newState) return;

            if (!IsTransitionAllowed(currentState, newState))
            {
                Debug.LogWarning($"[GameFlow] 허용되지 않은 전이: {currentState} → {newState}");
                return;
            }

            var prev = currentState;
            currentState = newState;
            OnStateChanged?.Invoke(prev, newState);
            Debug.Log($"[GameFlow] {prev} → {newState}");
        }

        private bool IsTransitionAllowed(GameState from, GameState to)
        {
            if (to is GameState.Warning or GameState.Paused)
                return true;

            if (from is GameState.Warning or GameState.Paused)
                return true;

            return AllowedTransitions.Contains((from, to));
        }

        public void RestorePreviousState()
        {
            TransitionTo(_stateBeforeWarning);
        }

        #region SO Event 핸들러 (인스펙터에서 EventListener로 연결)

        public void HandleCalibrationComplete()
        {
            if (currentState != GameState.Calibration) return;
            TransitionTo(GameState.FishingReady);
        }

        public void HandleCastingStarted()
        {
            if (currentState == GameState.FishingReady)
                TransitionTo(GameState.Fishing);
        }

        public void HandleReelIn()
        {
            if (currentState == GameState.Fishing)
                TransitionTo(GameState.FishingReady);
        }

        public void HandleHookingSuccess()
        {
            if (currentState == GameState.Fishing)
                TransitionTo(GameState.MiniGame);
        }

        public void HandleMiniGameResult()
        {
            if (currentState == GameState.MiniGame)
                TransitionTo(GameState.FishingReady);
        }

        public void HandleResultConfirmed()
        {
            if (currentState == GameState.Result)
                TransitionTo(GameState.FishingReady);
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
                    if (currentState is not (GameState.Warning or GameState.Paused))
                        _stateBeforeWarning = currentState;
                    TransitionTo(GameState.Warning);
                    break;

                case SafetyWarningLevel.Emergency:
                    if (currentState is not (GameState.Warning or GameState.Paused))
                        _stateBeforeWarning = currentState;
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

        [ContextMenu("Debug: → Fishing")]
        private void DebugToFishing() => TransitionTo(GameState.Fishing);

        [ContextMenu("Debug: → MiniGame")]
        private void DebugToMiniGame() => TransitionTo(GameState.MiniGame);

        [ContextMenu("Debug: → Result")]
        private void DebugToResult() => TransitionTo(GameState.Result);

        [ContextMenu("Debug: → ExitSequence")]
        private void DebugToExit() => TransitionTo(GameState.ExitSequence);

        #endregion
    }
}
