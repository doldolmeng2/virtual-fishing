using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.TestTools;
using VirtualFishing.Core.Events;
using VirtualFishing.Data;
using VirtualFishing.Fishing;

namespace VirtualFishing.Tests
{
    /// <summary>
    /// 다른 도메인 모듈(C: Fish, D: MiniGame, E: Feedback, A: Core)과
    /// SO Event 채널로 통합될 때를 대비한 테스트.
    ///
    /// 테스트 카테고리:
    ///   1) 발행 (Publish) — 본 모듈이 다른 모듈이 들을 수 있는 SO 이벤트를 정확한 시점에 발화하는가
    ///   2) 구독 (Subscribe) — 다른 모듈이 발행한 SO 이벤트가 VoidEventListener bridge를 통해
    ///      본 모듈의 핸들러에 정확히 도달하는가
    ///   3) E2E — 전체 시나리오(03_BiteAndHooking)가 SO 이벤트 통합 환경에서 정상 동작하는가
    ///
    /// 주의: Unity는 GameObject 활성 상태에서 AddComponent 시 즉시 OnEnable을 호출하므로,
    ///       필드 주입(SetField)이 OnEnable의 이벤트 구독보다 늦으면 구독이 실패합니다.
    ///       본 테스트는 SetActive(false) → AddComponent + SetField → SetActive(true) 패턴으로
    ///       OnEnable이 모든 필드가 채워진 뒤 실행되도록 보장합니다.
    /// </summary>
    public class FishingRodIntegrationTests
    {
        private GameObject _rodGO;
        private GameObject _floatGO;
        private GameObject _handGO;
        private GameObject _bridgeGO;

        private FishingRodController _rod;
        private FloatController _float;
        private GameSettingsSO _settings;
        private PlayerDataSO _playerData;

        // Cross-module SO Event 채널
        private VoidEventSO _onRodStateChangedSO;
        private VoidEventSO _onWaterLandedSO;
        private VoidEventSO _onBiteOccurredSO;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _settings = ScriptableObject.CreateInstance<GameSettingsSO>();
            _settings.castingZoneRadius = 0.5f;
            _settings.castingZoneOffset = new Vector3(0f, 0.3f, -0.1f);
            _settings.minCastingHoldTime = 0.15f;
            _settings.minCastingAcceleration = 1.0f;
            _settings.minCastingPower = 0.3f;
            _settings.maxCastingPower = 1.0f;
            _settings.castingPowerMultiplier = 1f;
            _settings.hookTimingWindow = 1.5f; // 테스트 단축
            _settings.hookingZoneRadius = 0.4f;
            _settings.hookingZoneOffset = new Vector3(0f, 0.5f, 0f);
            _settings.hookingMinAcceleration = 1.5f;
            _settings.minCastingDistance = 2.0f;
            _settings.castingBoundaryRadius = 15.0f;

            _playerData = ScriptableObject.CreateInstance<PlayerDataSO>();
            _playerData.sittingHeight = 1.2f;
            _playerData.currentPosition = Vector3.zero;

            // SO Event 채널 (실제 환경에선 .asset 파일)
            _onRodStateChangedSO = ScriptableObject.CreateInstance<VoidEventSO>();
            _onWaterLandedSO = ScriptableObject.CreateInstance<VoidEventSO>();
            _onBiteOccurredSO = ScriptableObject.CreateInstance<VoidEventSO>();

            // Float — SetActive(false) → 필드 주입 → SetActive(true)
            // 시작 위치는 y=5로 충분히 높게 두어, FixedUpdate의 자동 water contact
            // (transform.position.y <= surfaceY=0 fallback)이 캐스팅 직후 즉시 트리거되지 않게 함.
            _floatGO = new GameObject("Float");
            _floatGO.transform.position = new Vector3(0f, 5f, 0f);
            _floatGO.SetActive(false);
            var rb = _floatGO.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.isKinematic = true;
            _float = _floatGO.AddComponent<FloatController>();
            SetField(_float, "gameSettings", _settings);
            SetField(_float, "onWaterLanded", _onWaterLandedSO);
            _floatGO.SetActive(true);

            // Rod
            _rodGO = new GameObject("FishingRod");
            _rodGO.SetActive(false);
            _rod = _rodGO.AddComponent<FishingRodController>();
            SetField(_rod, "gameSettings", _settings);
            SetField(_rod, "playerData", _playerData);
            SetField(_rod, "floatCtrl", _float);
            SetField(_rod, "onRodStateChanged", _onRodStateChangedSO);
            _rodGO.SetActive(true);

            // Bridge: OnBiteOccurred SO → VoidEventListener → HandleBiteOccurred
            // 실제 씬의 BiteEventBridge GameObject 구성을 그대로 재현
            _bridgeGO = new GameObject("BiteBridge_Test");
            _bridgeGO.SetActive(false);
            var listener = _bridgeGO.AddComponent<VoidEventListener>();
            SetField(listener, "gameEvent", _onBiteOccurredSO);
            var response = new UnityEvent();
            response.AddListener(_rod.HandleBiteOccurred);
            SetField(listener, "response", response);
            _bridgeGO.SetActive(true);

            _handGO = new GameObject("Hand");

            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.Destroy(_bridgeGO);
            Object.Destroy(_rodGO);
            Object.Destroy(_floatGO);
            Object.Destroy(_handGO);
            Object.Destroy(_settings);
            Object.Destroy(_playerData);
            Object.Destroy(_onRodStateChangedSO);
            Object.Destroy(_onWaterLandedSO);
            Object.Destroy(_onBiteOccurredSO);
            yield return null;
        }

        // ────────────────────────────────────────────────────────────────────
        // 1. 발행 (Publish) — 본 모듈이 다른 모듈에 보내는 이벤트
        // ────────────────────────────────────────────────────────────────────

        #region Publish

        /// <summary>
        /// 시나리오: E역할(Feedback)이 OnRodStateChanged를 구독.
        /// 검증: 상태가 한 번 전이되면 SO 이벤트가 정확히 1회 발화.
        /// </summary>
        [UnityTest]
        public IEnumerator Publish_OnRodStateChanged_RaisedOnGrab()
        {
            var listener = new TestVoidListener();
            _onRodStateChangedSO.Register(listener);

            _rod.OnGrab(_handGO.transform); // Idle → Attached
            yield return null;

            Assert.AreEqual(1, listener.RaiseCount,
                "Idle→Attached 전이 시 OnRodStateChanged SO가 1회 발화되어야 함");

            _onRodStateChangedSO.Unregister(listener);
        }

        /// <summary>
        /// 시나리오: E역할(Feedback)이 매 상태 전이마다 사운드/햅틱 트리거.
        /// 검증: 여러 전이 발생 시 각각에 대해 발화.
        /// </summary>
        [UnityTest]
        public IEnumerator Publish_OnRodStateChanged_RaisedOnEachTransition()
        {
            var listener = new TestVoidListener();
            _onRodStateChangedSO.Register(listener);

            _rod.OnGrab(_handGO.transform);    // Idle → Attached  (1)
            yield return null;
            _rod.Cast(0.5f, Vector3.forward);  // Attached → Casting (2)
            yield return null;

            Assert.AreEqual(2, listener.RaiseCount,
                "전이가 N회면 SO 이벤트도 N회 발화되어야 함");

            _onRodStateChangedSO.Unregister(listener);
        }

        /// <summary>
        /// 시나리오: 다른 모듈이 잘못된 가정을 하지 않도록 — ForceState는 디버그 전용.
        /// 검증: ForceState는 SetState를 거치지 않으므로 SO 이벤트도 발화 안 됨.
        ///
        /// 다른 팀 통합 시 주의: 디버그 코드에서 ForceState로 상태를 바꾸면
        /// 구독자(Feedback 등)에게 알림이 가지 않으므로 이벤트 기반 로직이 누락됨.
        /// </summary>
        [UnityTest]
        public IEnumerator Publish_OnRodStateChanged_NotRaisedByForceState()
        {
            var listener = new TestVoidListener();
            _onRodStateChangedSO.Register(listener);

            _rod.ForceState(RodState.MiniGame);
            yield return null;

            Assert.AreEqual(0, listener.RaiseCount,
                "ForceState는 디버그 전용이며 SO 이벤트 채널을 우회함");

            _onRodStateChangedSO.Unregister(listener);
        }

        /// <summary>
        /// 시나리오: C역할(FishSpawner)이 OnWaterLanded를 구독해 입질 타이머 시작.
        /// 검증: 찌가 수면에 닿으면 SO가 정확히 발화.
        /// </summary>
        [UnityTest]
        public IEnumerator Publish_OnWaterLanded_RaisedWhenFloatLands()
        {
            var listener = new TestVoidListener();
            _onWaterLandedSO.Register(listener);

            _float.Launch(5f, Vector3.forward);
            yield return null;
            _float.OnWaterContact();
            yield return null;

            Assert.AreEqual(1, listener.RaiseCount,
                "Float.OnWaterContact() 호출 시 OnWaterLanded SO가 1회 발화되어야 함");

            _onWaterLandedSO.Unregister(listener);
        }

        #endregion

        // ────────────────────────────────────────────────────────────────────
        // 2. 구독 (Subscribe) — 다른 모듈이 발행하면 본 모듈이 반응
        // ────────────────────────────────────────────────────────────────────

        #region Subscribe

        /// <summary>
        /// 시나리오: C역할 FishSpawner가 OnBiteOccurred.Raise() 호출.
        /// 검증: BiteEventBridge → HandleBiteOccurred → 입질 타이머 활성화.
        ///
        /// 입질 타이머가 활성화되었음을 간접 검증:
        ///   - WaitingForBite 상태에서 입질 발생 후 챔질 동작 없이 hookTimingWindow 경과
        ///   - 타이머 만료 시 ReelIn 호출되어 (그랩 유지 중이므로) Attached 상태 복귀
        ///   - 만약 입질이 활성화되지 않았다면 WaitingForBite 그대로 유지됨
        /// </summary>
        [UnityTest]
        public IEnumerator Subscribe_BiteOccurred_BridgeRoutesToHandler()
        {
            // 사전 조건: 그랩 + WaitingForBite
            _rod.OnGrab(_handGO.transform);
            _rod.ForceState(RodState.WaitingForBite);
            yield return null;

            // C역할 모듈이 발행했다고 가정 — bridge가 처리해야 함
            _onBiteOccurredSO.Raise();
            yield return null;

            // hookTimingWindow + 마진 대기 → 챔질 없으므로 타이머 만료
            yield return new WaitForSeconds(_settings.hookTimingWindow + 0.3f);

            Assert.AreEqual(RodState.Attached, _rod.CurrentState,
                "OnBiteOccurred SO → Bridge → HandleBiteOccurred → 타이머 시작 → 만료 → ReelIn → Attached");
        }

        /// <summary>
        /// 시나리오: 잘못된 시점(예: WaitingForBite 아닐 때)에 입질 SO가 발화돼도
        ///           본 모듈이 안전하게 무시해야 함.
        /// 검증: Idle 상태에서 OnBiteOccurred → 상태 변화 없음.
        /// </summary>
        [UnityTest]
        public IEnumerator Subscribe_BiteOccurred_IgnoredWhenNotInWaitingForBite()
        {
            Assert.AreEqual(RodState.Idle, _rod.CurrentState);

            _onBiteOccurredSO.Raise();
            yield return new WaitForSeconds(0.5f);

            Assert.AreEqual(RodState.Idle, _rod.CurrentState,
                "WaitingForBite가 아닌 상태에서 입질 발화는 안전하게 무시되어야 함");
        }

        #endregion

        // ────────────────────────────────────────────────────────────────────
        // 3. E2E — 전체 시나리오를 SO 이벤트 통합 환경에서 검증
        // ────────────────────────────────────────────────────────────────────

        #region E2E

        /// <summary>
        /// 시나리오 03 전체 흐름:
        ///   Grab → Cast → 찌 착수(OnWaterLanded) → 입질(OnBiteOccurred) → 챔질 성공 → MiniGame
        ///
        /// 모든 SO 이벤트 채널이 활성화된 상태에서 끝까지 정상 동작해야 함.
        /// 통합 시 가장 먼저 깨질 가능성이 높은 회귀 테스트.
        /// </summary>
        [UnityTest]
        public IEnumerator E2E_FullFlow_GrabToMiniGame()
        {
            // 1. Grab
            _rod.OnGrab(_handGO.transform);
            yield return null;
            Assert.AreEqual(RodState.Attached, _rod.CurrentState);

            // 2. Cast
            _rod.Cast(0.5f, Vector3.forward);
            yield return null;
            Assert.AreEqual(RodState.Casting, _rod.CurrentState);

            // 3. 찌 착수 — FloatController가 OnWaterLanded SO 발화 +
            //    같은 프리팹 내 C# 이벤트로 FishingRodController.HandleWaterLanded 호출
            _float.OnWaterContact();
            yield return null;
            yield return null; // LateUpdate 사이클
            Assert.AreEqual(RodState.WaitingForBite, _rod.CurrentState,
                "착수 후 WaitingForBite로 전이되어야 함");

            // 4. C역할 FishSpawner가 OnBiteOccurred 발화 (bridge 경유)
            _onBiteOccurredSO.Raise();
            yield return null;

            // 5. 챔질 동작 시뮬레이션 — 손을 챔질 존(머리 위)으로 이동
            Vector3 hookZoneCenter = _playerData.currentPosition;
            hookZoneCenter.y = _playerData.sittingHeight + _settings.hookingZoneOffset.y;
            _handGO.transform.position = hookZoneCenter;

            // 위쪽 가속도 주입 — 컨트롤러 어댑터 역할
            _rod.UpdateCastingInput(
                Vector3.up * (_settings.hookingMinAcceleration + 1f),
                Vector3.up);

            yield return null;
            yield return null; // LateUpdate가 챔질 판정 처리

            Assert.AreEqual(RodState.MiniGame, _rod.CurrentState,
                "챔질 성공 시 MiniGame 상태로 자동 전이되어야 함 (Hit → MiniGame)");
        }

        /// <summary>
        /// 시나리오 03 실패 경로: 입질 후 챔질 동작 없이 hookTimingWindow 경과 → ReelIn → 상태 복귀.
        /// GameFlowManager가 OnRodStateChanged 발화로 FishingReady 복귀를 트리거할 수 있어야 함.
        /// </summary>
        [UnityTest]
        public IEnumerator Integration_HookFailure_RaisesStateBackToAttached()
        {
            var listener = new TestVoidListener();
            _onRodStateChangedSO.Register(listener);

            _rod.OnGrab(_handGO.transform);
            _rod.ForceState(RodState.WaitingForBite);
            int countBeforeBite = listener.RaiseCount;

            _onBiteOccurredSO.Raise(); // 입질 활성화
            yield return null;

            // 챔질 동작 없이 hookTimingWindow 경과
            yield return new WaitForSeconds(_settings.hookTimingWindow + 0.3f);

            Assert.AreEqual(RodState.Attached, _rod.CurrentState,
                "타이밍 초과 시 ReelIn → Attached (그랩 유지)");
            Assert.Greater(listener.RaiseCount, countBeforeBite,
                "WaitingForBite → Attached 전이로 OnRodStateChanged SO 발화되어야 함");

            _onRodStateChangedSO.Unregister(listener);
        }

        /// <summary>
        /// 챔질 성공 시 Hit → MiniGame이 같은 프레임에 연쇄 전이되며,
        /// OnRodStateChanged SO가 각각 발화되어야 함 (총 +2회).
        /// E역할 Feedback이 Hit 효과음/햅틱을 놓치지 않도록 보장.
        /// </summary>
        [UnityTest]
        public IEnumerator Integration_RodStateChanged_FiresOnHitAndMiniGameSeparately()
        {
            _rod.OnGrab(_handGO.transform);
            _rod.ForceState(RodState.WaitingForBite);
            _onBiteOccurredSO.Raise();
            yield return null;

            var listener = new TestVoidListener();
            _onRodStateChangedSO.Register(listener);

            // 챔질 동작
            Vector3 hookZone = _playerData.currentPosition;
            hookZone.y = _playerData.sittingHeight + _settings.hookingZoneOffset.y;
            _handGO.transform.position = hookZone;
            _rod.UpdateCastingInput(
                Vector3.up * (_settings.hookingMinAcceleration + 1f),
                Vector3.up);
            yield return null;
            yield return null;

            Assert.AreEqual(RodState.MiniGame, _rod.CurrentState);
            Assert.AreEqual(2, listener.RaiseCount,
                "Hit, MiniGame 각각의 전이마다 SO 발화 — Feedback이 Hit 이벤트를 놓치지 않음");

            _onRodStateChangedSO.Unregister(listener);
        }

        #endregion

        // ────────────────────────────────────────────────────────────────────
        // 헬퍼
        // ────────────────────────────────────────────────────────────────────

        /// <summary>
        /// 테스트용 IVoidEventListener — 발화 횟수만 기록.
        /// </summary>
        private class TestVoidListener : IVoidEventListener
        {
            public int RaiseCount { get; private set; }
            public void OnEventRaised() => RaiseCount++;
        }

        /// <summary>
        /// SerializeField에 값 주입 (private 포함, 상속 체인 탐색).
        /// </summary>
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
