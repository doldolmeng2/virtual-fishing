using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.TestTools;
using VirtualFishing.Feedback;
using VirtualFishing.Fishing.Events;

namespace VirtualFishing.Tests
{
    /// <summary>
    /// E역할(Feedback)이 낚싯대가 실제 발행하는 onRodStateChanged(RodStateTransitionEventSO)를
    /// RodStateTransitionEventListener로 받아 '낚싯대 장착' 피드백을 끌어내는지 검증.
    ///
    /// 배경: 낚싯대는 '장착'을 별도 void 이벤트로 발행하지 않고 상태 전이(Idle→Attached)로만 표현한다.
    /// 따라서 FeedbackManager.OnRodStateChangedEvent(RodStateTransition) 디스패처가 장착 피드백을 담당한다.
    /// 반면 '찌 착수'는 FloatController.onWaterLanded(VoidEventSO)가 직접 발행하므로,
    /// 상태 전이(Casting→WaitingForBite)로 이중 발행되면 안 된다.
    ///
    /// 본 테스트는 실제 프리팹과 동일한 체인
    ///   RodStateTransitionEventSO.Raise → RodStateTransitionEventListener → FeedbackManager.OnRodStateChangedEvent
    /// 을 코드로 재구성해 검증한다.
    /// </summary>
    public class FeedbackRodStateWiringTests
    {
        private GameObject _fmGO;
        private GameObject _listenerGO;
        private FeedbackManager _fm;
        private RodStateTransitionEventSO _onRodStateChanged;

        private readonly List<string> _logs = new List<string>();

        private const string GrabLog = "낚싯대 장착 완료";
        private const string WaterLog = "찌가 수면에 착수함";

        // UnityEvent<T>는 추상이므로 직렬화/인스턴스화를 위해 구체 서브클래스 필요
        private class RodStateTransitionUnityEvent : UnityEvent<RodStateTransition> { }

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _onRodStateChanged = ScriptableObject.CreateInstance<RodStateTransitionEventSO>();

            // FeedbackManager + 하위 매니저 (OnEnable 전에 필드 주입).
            // 장착 피드백 경로(OnRodGrabbedEvent)는 PlaySound + PlayHaptic만 사용하므로
            // SoundManager / HapticManager만 주입한다. (VisualEffectManager는 인스펙터 데이터
            // 없이 AddComponent하면 effectLibrary가 null이라 Awake에서 NRE — 본 경로에 불필요.)
            _fmGO = new GameObject("FeedbackManager_Test");
            _fmGO.SetActive(false);
            var sound = _fmGO.AddComponent<SoundManager>();
            var haptic = _fmGO.AddComponent<HapticManager>();
            _fm = _fmGO.AddComponent<FeedbackManager>();
            SetField(_fm, "soundManager", sound);
            SetField(_fm, "hapticManager", haptic);
            _fmGO.SetActive(true);

            // 실제 프리팹의 RodStateTransitionEventListener 와이어링 재현
            _listenerGO = new GameObject("RodStateListener_Test");
            _listenerGO.SetActive(false);
            var listener = _listenerGO.AddComponent<RodStateTransitionEventListener>();
            SetField(listener, "gameEvent", _onRodStateChanged);
            var response = new RodStateTransitionUnityEvent();
            response.AddListener(_fm.OnRodStateChangedEvent); // (RodStateTransition) 오버로드로 컴파일타임 바인딩
            SetField(listener, "response", response);
            _listenerGO.SetActive(true); // OnEnable → gameEvent.Register(listener)

            _logs.Clear();
            Application.logMessageReceived += CaptureLog;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Application.logMessageReceived -= CaptureLog;
            Object.Destroy(_listenerGO);
            Object.Destroy(_fmGO);
            Object.Destroy(_onRodStateChanged);
            yield return null;
        }

        private void CaptureLog(string condition, string stackTrace, LogType type) => _logs.Add(condition);
        private bool Logged(string fragment) => _logs.Exists(l => l != null && l.Contains(fragment));

        /// <summary>핵심: 낚싯대 장착(Idle→Attached) 전이가 '장착' 피드백을 끌어내는가.</summary>
        [UnityTest]
        public IEnumerator Grab_IdleToAttached_TriggersRodGrabbedFeedback()
        {
            _onRodStateChanged.Raise(new RodStateTransition(RodState.Idle, RodState.Attached));
            yield return null;

            Assert.IsTrue(Logged(GrabLog),
                "Idle→Attached 전이 시 '낚싯대 장착' 피드백(OnRodGrabbedEvent)이 발생해야 함");
        }

        /// <summary>회귀 방지: 착수는 FloatController가 발행하므로 상태 전이로 이중 발행되면 안 됨.</summary>
        [UnityTest]
        public IEnumerator WaterLanded_CastingToWaiting_DoesNotDoubleFire()
        {
            _onRodStateChanged.Raise(new RodStateTransition(RodState.Casting, RodState.WaitingForBite));
            yield return null;

            Assert.IsFalse(Logged(WaterLog),
                "착수 피드백은 FloatController.onWaterLanded가 단독 발행 — 상태 전이로 이중 발행되면 안 됨");
        }

        /// <summary>장착 외 전이에서는 장착 피드백이 발생하지 않아야 함.</summary>
        [UnityTest]
        public IEnumerator OtherTransitions_DoNotTriggerGrab()
        {
            _onRodStateChanged.Raise(new RodStateTransition(RodState.Attached, RodState.Casting));
            _onRodStateChanged.Raise(new RodStateTransition(RodState.WaitingForBite, RodState.Hit));
            _onRodStateChanged.Raise(new RodStateTransition(RodState.Hit, RodState.MiniGame));
            yield return null;

            Assert.IsFalse(Logged(GrabLog),
                "장착(Idle→Attached) 외 전이에서는 '낚싯대 장착' 피드백이 발생하면 안 됨");
        }

        private static void SetField(object target, string fieldName, object value)
        {
            var type = target.GetType();
            while (type != null)
            {
                var field = type.GetField(fieldName,
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance);
                if (field != null)
                {
                    field.SetValue(target, value);
                    return;
                }
                type = type.BaseType;
            }
            Debug.LogError($"Field '{fieldName}' not found on {target.GetType().Name}");
        }
    }
}
