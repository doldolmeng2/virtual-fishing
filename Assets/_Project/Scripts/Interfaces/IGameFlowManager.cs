using System;

namespace VirtualFishing.Interfaces
{
    public interface IGameFlowManager
    {
        GameState CurrentState { get; }
        void TransitionTo(GameState newState);
        event Action<GameState, GameState> OnStateChanged;
    }

    public interface ISceneService
    {
        void LoadScene(string sceneName);
        event Action OnSceneLoaded;
    }
}
