using System;

namespace VirtualFishing.Fishing.Events
{
    /// <summary>
    /// 낚싯대 상태 전이 정보. OnRodStateChanged 이벤트의 페이로드로 사용.
    /// 구독자가 이전/현재 상태 모두를 알 수 있게 해 특정 전이(예: 챔질 실패 = WaitingForBite→Attached)를 식별 가능.
    /// </summary>
    [Serializable]
    public struct RodStateTransition
    {
        public RodState Previous;
        public RodState Current;

        public RodStateTransition(RodState previous, RodState current)
        {
            Previous = previous;
            Current = current;
        }

        public override string ToString() => $"{Previous} → {Current}";
    }
}
