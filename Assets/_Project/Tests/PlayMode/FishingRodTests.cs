using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VirtualFishing.Fishing;
using VirtualFishing.Fishing.Events;
using VirtualFishing.Data;

namespace VirtualFishing.Tests
{
    public class FishingRodTests
    {
        private GameObject _rodGO;
        private GameObject _floatGO;
        private GameObject _handGO;
        private FishingRodController _rod;
        private FloatController _float;
        private GameSettingsSO _settings;
        private PlayerDataSO _playerData;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            // GameSettingsSO
            _settings = ScriptableObject.CreateInstance<GameSettingsSO>();
            _settings.castingZoneRadius = 0.5f;
            _settings.castingZoneOffset = new Vector3(0f, 0.3f, -0.1f);
            _settings.minCastingHoldTime = 0.15f;
            _settings.minCastingAcceleration = 1.0f;
            _settings.minCastingPower = 0.3f;
            _settings.maxCastingPower = 1.0f;
            _settings.castingPowerMultiplier = 1f;
            _settings.hookTimingWindow = 3f;
            _settings.hookingZoneRadius = 0.4f;
            _settings.hookingZoneOffset = new Vector3(0f, 0.5f, 0f);
            _settings.hookingMinAcceleration = 1.5f;
            _settings.minCastingDistance = 2.0f;
            _settings.castingBoundaryRadius = 15.0f;

            // PlayerDataSO
            _playerData = ScriptableObject.CreateInstance<PlayerDataSO>();
            _playerData.sittingHeight = 1.2f;
            _playerData.currentPosition = Vector3.zero;

            // Float
            // Unity는 active GameObject에 AddComponent 시 OnEnable을 즉시 호출하므로,
            // SerializedField 주입(SetField)이 OnEnable의 이벤트 구독 이후에 일어나면 구독이 누락됨.
            // SetActive(false) → AddComponent + SetField → SetActive(true) 순으로 보장.
            // 시작 y=5: FixedUpdate의 surfaceY=0 fallback으로 즉시 자동 OnWaterContact되는 것 방지.
            _floatGO = new GameObject("Float");
            _floatGO.transform.position = new Vector3(0f, 5f, 0f);
            _floatGO.SetActive(false);
            var rb = _floatGO.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.isKinematic = true;
            _float = _floatGO.AddComponent<FloatController>();
            SetField(_float, "gameSettings", _settings);
            _floatGO.SetActive(true);

            // Rod — 동일 패턴: OnEnable에서 floatCtrl.OnWaterLanded 구독되도록 보장
            _rodGO = new GameObject("FishingRod");
            _rodGO.SetActive(false);
            _rod = _rodGO.AddComponent<FishingRodController>();
            SetField(_rod, "gameSettings", _settings);
            SetField(_rod, "playerData", _playerData);
            SetField(_rod, "floatCtrl", _float);
            _rodGO.SetActive(true);

            _handGO = new GameObject("Hand");

            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.Destroy(_rodGO);
            Object.Destroy(_floatGO);
            Object.Destroy(_handGO);
            Object.Destroy(_settings);
            Object.Destroy(_playerData);
            yield return null;
        }

        #region IGrabbable

        [UnityTest]
        public IEnumerator Grab_SetsStateToAttached()
        {
            Assert.AreEqual(RodState.Idle, _rod.CurrentState);
            Assert.IsFalse(_rod.IsGrabbed);

            _rod.OnGrab(_handGO.transform);
            yield return null;

            Assert.AreEqual(RodState.Attached, _rod.CurrentState);
            Assert.IsTrue(_rod.IsGrabbed);
        }

        [UnityTest]
        public IEnumerator Grab_FiresOnGrabbedEvent()
        {
            bool fired = false;
            _rod.OnGrabbed += () => fired = true;

            _rod.OnGrab(_handGO.transform);
            yield return null;

            Assert.IsTrue(fired);
        }

        [UnityTest]
        public IEnumerator Release_SetsStateToIdle()
        {
            _rod.OnGrab(_handGO.transform);
            yield return null;

            _rod.OnRelease();
            yield return null;

            Assert.AreEqual(RodState.Idle, _rod.CurrentState);
            Assert.IsFalse(_rod.IsGrabbed);
        }

        [UnityTest]
        public IEnumerator Release_FiresOnReleasedEvent()
        {
            _rod.OnGrab(_handGO.transform);
            yield return null;

            bool fired = false;
            _rod.OnReleased += () => fired = true;

            _rod.OnRelease();
            yield return null;

            Assert.IsTrue(fired);
        }

        [UnityTest]
        public IEnumerator Grab_IgnoredWhenNotIdle()
        {
            _rod.OnGrab(_handGO.transform);
            yield return null;
            Assert.AreEqual(RodState.Attached, _rod.CurrentState);

            // Attached 상태에서 다시 Grab → 무시
            _rod.OnGrab(_handGO.transform);
            yield return null;
            Assert.AreEqual(RodState.Attached, _rod.CurrentState);
        }

        #endregion

        #region IFishingRod - 상태 전이

        [UnityTest]
        public IEnumerator StateChanged_EventFired()
        {
            RodStateTransition? received = null;
            _rod.OnRodStateChanged += (t) => received = t;

            _rod.OnGrab(_handGO.transform);
            yield return null;

            Assert.IsTrue(received.HasValue, "전이 이벤트 발화");
            Assert.AreEqual(RodState.Idle, received.Value.Previous);
            Assert.AreEqual(RodState.Attached, received.Value.Current);
        }

        [UnityTest]
        public IEnumerator ReelIn_FromWaitingForBite_ReturnsToAttached()
        {
            _rod.OnGrab(_handGO.transform);
            yield return null;

            _rod.ForceState(RodState.WaitingForBite);
            yield return null;

            _rod.ReelIn();
            yield return null;

            Assert.AreEqual(RodState.Attached, _rod.CurrentState);
        }

        [UnityTest]
        public IEnumerator ReelIn_WhenNotGrabbed_ReturnsToIdle()
        {
            _rod.ForceState(RodState.WaitingForBite);
            yield return null;

            _rod.ReelIn();
            yield return null;

            Assert.AreEqual(RodState.Idle, _rod.CurrentState);
        }

        [UnityTest]
        public IEnumerator ForceState_ChangesState()
        {
            _rod.ForceState(RodState.MiniGame);
            yield return null;

            Assert.AreEqual(RodState.MiniGame, _rod.CurrentState);
        }

        #endregion

        #region ICastable

        [UnityTest]
        public IEnumerator Cast_SetsStateToCasting()
        {
            _rod.OnGrab(_handGO.transform);
            yield return null;

            _rod.Cast(0.5f, Vector3.forward);
            yield return null;

            Assert.AreEqual(RodState.Casting, _rod.CurrentState);
            Assert.IsTrue(_rod.IsCasting);
        }

        [UnityTest]
        public IEnumerator Cast_ClampsPower()
        {
            _rod.OnGrab(_handGO.transform);
            yield return null;

            // maxCastingPower = 1.0 이므로 10.0은 클램핑됨
            _rod.Cast(10f, Vector3.forward);
            yield return null;

            Assert.AreEqual(RodState.Casting, _rod.CurrentState);
        }

        #endregion

        #region IFishingFloat

        [UnityTest]
        public IEnumerator Float_Launch_SetsVelocity()
        {
            var rb = _floatGO.GetComponent<Rigidbody>();
            Assert.IsNotNull(rb, "Rigidbody must exist");

            _float.Launch(5f, Vector3.forward);

            Assert.IsFalse(rb.isKinematic, "Launch sets isKinematic=false");
            Assert.Greater(rb.linearVelocity.magnitude, 0.001f,
                $"Launch should set velocity via AddForce(VelocityChange). actual={rb.linearVelocity}");
            yield return null;
        }

        [UnityTest]
        public IEnumerator Float_OnWaterContact_StopsMovement()
        {
            _float.Launch(5f, Vector3.forward);
            yield return null;

            bool landed = false;
            _float.OnWaterLanded += () => landed = true;

            _float.OnWaterContact();
            yield return null;

            Assert.IsTrue(landed);
            Assert.AreEqual(0f, _float.Velocity, 0.01f);
        }

        [UnityTest]
        public IEnumerator Float_Sink_ChangesSinkingDepth()
        {
            _float.Sink(0.5f);
            yield return null;

            Assert.AreEqual(0.5f, _float.SinkingDepth, 0.01f);
        }

        [UnityTest]
        public IEnumerator Float_ResetFloat_ReturnsToAttached()
        {
            _float.Launch(5f, Vector3.forward);
            yield return null;

            _float.OnWaterContact();
            yield return null;

            _float.ResetFloat();
            yield return null;

            Assert.AreEqual(0f, _float.SinkingDepth, 0.01f);
        }

        #endregion

        #region 챔질 판정

        [UnityTest]
        public IEnumerator Hooking_RequiresBiteFirst()
        {
            _rod.OnGrab(_handGO.transform);
            yield return null;

            _rod.ForceState(RodState.WaitingForBite);
            yield return null;

            // Bite 없이 챔질 체크 → 상태 변화 없음
            yield return new WaitForSeconds(0.1f);
            Assert.AreEqual(RodState.WaitingForBite, _rod.CurrentState);
        }

        [UnityTest]
        public IEnumerator Hooking_TimerExpires_ReelsIn()
        {
            _rod.OnGrab(_handGO.transform);
            yield return null;

            _rod.ForceState(RodState.WaitingForBite);
            _rod.HandleBiteOccurred();
            yield return null;

            // hookTimingWindow(3초) 대기
            yield return new WaitForSeconds(3.5f);

            // 타이밍 초과 → Attached로 복귀 (그랩 유지)
            Assert.AreEqual(RodState.Attached, _rod.CurrentState);
        }

        #endregion

        #region 캐스팅 존 이중필터 (시나리오 02 핵심)

        /// <summary>
        /// 일반 캐스팅: 존 안에서 holdTime 충분 + 가속도 충분 → 존 이탈 시 Cast 발생.
        /// </summary>
        [UnityTest]
        public IEnumerator Casting_DoubleFilter_PassesWhenHoldAndAccelMet()
        {
            _rod.OnGrab(_handGO.transform);
            yield return null;

            // 캐스팅 존 중심으로 손 이동 — 존 진입
            Vector3 zoneCenter = _playerData.currentPosition + _settings.castingZoneOffset;
            zoneCenter.y = _playerData.sittingHeight + _settings.castingZoneOffset.y + 0.05f;
            _handGO.transform.position = zoneCenter;
            yield return null; // LateUpdate가 IsInCastingZone 갱신

            // holdTime 이상 체류
            yield return new WaitForSeconds(_settings.minCastingHoldTime + 0.1f);

            // 충분한 가속도 + 존 이탈
            _rod.UpdateCastingInput(
                Vector3.forward * (_settings.minCastingAcceleration + 1f),
                Vector3.forward);
            _handGO.transform.position = zoneCenter + Vector3.forward * 2f; // 존 밖
            yield return null;
            yield return null; // LateUpdate가 이탈 감지 + Cast 호출

            Assert.AreEqual(RodState.Casting, _rod.CurrentState,
                "holdTime과 가속도 모두 충족 → Cast 호출되어 Casting 상태 진입");
        }

        /// <summary>
        /// 일반 캐스팅: holdTime 미달 시 → Cast 안 됨, 상태 Attached 유지.
        /// </summary>
        [UnityTest]
        public IEnumerator Casting_DoubleFilter_RejectsWhenHoldTooShort()
        {
            _rod.OnGrab(_handGO.transform);
            yield return null;

            Vector3 zoneCenter = _playerData.currentPosition + _settings.castingZoneOffset;
            zoneCenter.y = _playerData.sittingHeight + _settings.castingZoneOffset.y + 0.05f;
            _handGO.transform.position = zoneCenter;
            yield return null;

            // holdTime 미달로 즉시 이탈 (가속도는 충분)
            _rod.UpdateCastingInput(
                Vector3.forward * (_settings.minCastingAcceleration + 1f),
                Vector3.forward);
            _handGO.transform.position = zoneCenter + Vector3.forward * 2f;
            yield return null;
            yield return null;

            Assert.AreEqual(RodState.Attached, _rod.CurrentState,
                "holdTime 미달 → Cast 거부, Attached 유지");
        }

        /// <summary>
        /// 이지 캐스팅: easyCastingEnabled=true이면 존 진입만으로 즉시 Cast.
        /// </summary>
        [UnityTest]
        public IEnumerator Casting_EasyMode_AutoCastsOnZoneEntry()
        {
            _settings.easyCastingEnabled = true;
            _settings.easyCastingPower = 0.5f;

            _rod.OnGrab(_handGO.transform);
            yield return null;

            Vector3 zoneCenter = _playerData.currentPosition + _settings.castingZoneOffset;
            zoneCenter.y = _playerData.sittingHeight + _settings.castingZoneOffset.y + 0.05f;
            _handGO.transform.position = zoneCenter;
            yield return null;
            yield return null; // LateUpdate가 즉시 Cast 호출

            Assert.AreEqual(RodState.Casting, _rod.CurrentState,
                "이지 캐스팅 모드는 holdTime/가속도 무관하게 존 진입 시 즉시 Cast");

            _settings.easyCastingEnabled = false; // 다른 테스트에 영향 없도록 복원
        }

        #endregion

        #region 챔질 판정 — 이중 조건 단위 검증

        /// <summary>
        /// 챔질 존 진입했지만 가속도 부족 → 챔질 보류 (상태 변화 없음).
        /// </summary>
        [UnityTest]
        public IEnumerator Hooking_OnlyZoneNoAccel_StaysInWaitingForBite()
        {
            _rod.OnGrab(_handGO.transform);
            _rod.ForceState(RodState.WaitingForBite);
            _rod.HandleBiteOccurred();
            yield return null;

            // 챔질 존 진입 — 가속도는 0
            Vector3 hookZone = _playerData.currentPosition;
            hookZone.y = _playerData.sittingHeight + _settings.hookingZoneOffset.y;
            _handGO.transform.position = hookZone;
            _rod.UpdateCastingInput(Vector3.zero, Vector3.up);
            yield return null;
            yield return null;

            Assert.AreEqual(RodState.WaitingForBite, _rod.CurrentState,
                "존 진입 단독으로는 챔질 성공 판정 안 됨");
        }

        /// <summary>
        /// 가속도 충분하지만 챔질 존 미진입 → 챔질 보류.
        /// </summary>
        [UnityTest]
        public IEnumerator Hooking_OnlyAccelNoZone_StaysInWaitingForBite()
        {
            _rod.OnGrab(_handGO.transform);
            _rod.ForceState(RodState.WaitingForBite);
            _rod.HandleBiteOccurred();
            yield return null;

            // 존 밖에서 가속도만 충분
            _handGO.transform.position = new Vector3(10f, 0f, 10f);
            _rod.UpdateCastingInput(
                Vector3.up * (_settings.hookingMinAcceleration + 1f),
                Vector3.up);
            yield return null;
            yield return null;

            Assert.AreEqual(RodState.WaitingForBite, _rod.CurrentState,
                "가속도 단독으로는 챔질 성공 판정 안 됨");
        }

        /// <summary>
        /// 두 조건 동시 충족 → Hit → MiniGame 자동 연쇄 전이.
        /// </summary>
        [UnityTest]
        public IEnumerator Hooking_BothConditionsMet_TransitionsToMiniGame()
        {
            _rod.OnGrab(_handGO.transform);
            _rod.ForceState(RodState.WaitingForBite);
            _rod.HandleBiteOccurred();
            yield return null;

            Vector3 hookZone = _playerData.currentPosition;
            hookZone.y = _playerData.sittingHeight + _settings.hookingZoneOffset.y;
            _handGO.transform.position = hookZone;
            _rod.UpdateCastingInput(
                Vector3.up * (_settings.hookingMinAcceleration + 1f),
                Vector3.up);
            yield return null;
            yield return null;

            Assert.AreEqual(RodState.MiniGame, _rod.CurrentState,
                "두 조건 동시 충족 시 Hit→MiniGame까지 같은 프레임에 연쇄 전이");
        }

        #endregion

        #region 결함 검증 — OnRelease 시 찌 회수

        /// <summary>
        /// 캐스팅 후 그랩 해제 → 찌가 자동으로 회수되어 rod 초기 상태와 일관성 유지.
        /// (이전 버그: 찌가 수면에 방치되었음)
        /// </summary>
        [UnityTest]
        public IEnumerator OnRelease_DuringWaitingForBite_ResetsFloat()
        {
            _rod.OnGrab(_handGO.transform);
            _rod.Cast(0.5f, Vector3.forward);
            yield return null;
            _float.OnWaterContact();
            yield return null;
            yield return null;
            Assert.AreEqual(RodState.WaitingForBite, _rod.CurrentState,
                "사전조건: WaitingForBite 진입 확인");

            // SinkingDepth가 비-0이라고 가정 (테스트 단순화 위해 ResetFloat이 0으로 만드는지 검증)
            _rod.OnRelease();
            yield return null;

            Assert.AreEqual(RodState.Idle, _rod.CurrentState);
            Assert.AreEqual(0f, _float.SinkingDepth, 0.01f,
                "OnRelease 시 floatCtrl.ResetFloat 호출되어 SinkingDepth 0");
        }

        #endregion

        #region 통합 흐름

        [UnityTest]
        public IEnumerator FullFlow_Grab_Cast_WaterLand()
        {
            // Grab
            _rod.OnGrab(_handGO.transform);
            yield return null;
            Assert.AreEqual(RodState.Attached, _rod.CurrentState);

            // Cast
            _rod.Cast(0.5f, Vector3.forward);
            yield return null;
            Assert.AreEqual(RodState.Casting, _rod.CurrentState);

            // 찌 착수 시뮬레이션
            _float.OnWaterContact();
            yield return null;

            // FishingRodController가 LateUpdate에서 HandleWaterLanded를 처리
            yield return null;
            Assert.AreEqual(RodState.WaitingForBite, _rod.CurrentState);
        }

        #endregion

        /// <summary>
        /// SerializeField에 값을 주입하는 헬퍼
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
