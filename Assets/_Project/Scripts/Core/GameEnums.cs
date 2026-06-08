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

    public enum FishPhase
    {
        None,
        Phase1,
        Phase2,
        Phase3,
        Phase4
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

    /// <summary>
    /// 햅틱 진동의 발행 주체. 값이 클수록 우선순위가 높다.
    /// 여러 소스가 동시에 지속 진동(Continuous/RhythmicWarning)을 요청하면
    /// HapticManager가 가장 높은 우선순위의 소스만 실제로 재생하고,
    /// 정지(Stop)도 해당 소스만 해제하여 서로의 경고를 덮어쓰지 않는다.
    /// </summary>
    public enum HapticSource
    {
        Default = 0,        // 단발성 게임 피드백(장착/캐스팅/입질/챔질 등)
        TensionWarning = 10, // 미니게임 장력 임계치 경고
        SafetyWarning = 20   // 플레이 영역 이탈 경고 (최우선)
    }

    public enum FishMoveState
    {
        Normal,   // 일반 상황 - 릴링 시 기본 텐션 증가
        Left,     // 물고기가 왼쪽으로 이동 → 릴링 시 텐션 배율 증가
        Right,    // 물고기가 오른쪽으로 이동 → 릴링 시 텐션 배율 증가
        Opposite  // 물고기가 멀어짐 → 릴링 시 텐션 배율 2x 이상 증가
    }
}
