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

    public enum FishMoveState
    {
        Normal,   // 일반 상황 - 릴링 시 기본 텐션 증가
        Left,     // 물고기가 왼쪽으로 이동 → 릴링 시 텐션 배율 증가
        Right,    // 물고기가 오른쪽으로 이동 → 릴링 시 텐션 배율 증가
        Opposite  // 물고기가 멀어짐 → 릴링 시 텐션 배율 2x 이상 증가
    }
}
