namespace VirtualFishing
{
    public enum GameState
    {
        Login,
        Calibration,
        FishingReady,
        Fishing,
        MiniGame,
        Result,
        Warning,
        Paused,
        ExitSequence
    }

    public enum RodState
    {
        Idle,
        Attached,
        Casting,
        WaitingForBite,
        Hit,
        MiniGame
    }

    public enum TensionZone
    {
        Safe,
        Warning,
        Danger,
        Critical
    }

    public enum SafetyWarningLevel
    {
        None,
        NearBoundary,
        Outside,
        Emergency
    }

    public enum BackgroundType // 우선 이 중 하나만 먼저 구현하는 걸 목표로 합시다
    {
        River,
        Lake,
        Sea,
        Pond
    }

    public enum MovementPattern
    {
        Calm,
        Aggressive,
        Erratic
    }

    public enum FishMoveMode
    {
        Stop,
        MoveLeft,
        MoveRight
    }

    public enum ControllerHand
    {
        Left,
        Right,
        Both
    }

    public enum HapticPattern
    {
        LightPulse,
        StrongPulse,
        Continuous,
        RhythmicWarning
    }
}
