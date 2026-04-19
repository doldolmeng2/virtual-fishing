using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VirtualFishing.Data;
using VirtualFishing.Fishing;

namespace VirtualFishing.Tests
{
    /// <summary>
    /// 낚시 시스템 보조 컴포넌트들의 단위 테스트.
    /// FishingLineRenderer / ReelController / FloatController 엣지 케이스.
    /// </summary>
    public class FishingComponentsTests
    {
        // ────────────────────────────────────────────────────────────────────
        // FishingLineRenderer
        // ────────────────────────────────────────────────────────────────────

        #region FishingLineRenderer

        /// <summary>
        /// rodTip 또는 floatController가 null일 때 LineRenderer가 비활성화되어야 함.
        /// (씬 통합 시 참조 누락이 화면에 깨진 라인으로 노출되지 않도록 방어)
        /// </summary>
        [UnityTest]
        public IEnumerator FishingLineRenderer_NullGuard_DisablesLineWhenRefMissing()
        {
            var go = new GameObject("Line");
            go.SetActive(false);
            var lr = go.AddComponent<LineRenderer>();
            var renderer = go.AddComponent<FishingLineRenderer>();
            // rodTip, floatController 일부러 null 유지
            go.SetActive(true);

            // LateUpdate 사이클을 거치며 disable 결정
            yield return null;
            yield return null;

            Assert.IsFalse(lr.enabled,
                "참조 누락 시 LineRenderer가 비활성화되어야 함 (잔류 그래픽 방지)");

            Object.Destroy(go);
        }

        /// <summary>
        /// rodTip + floatController 모두 셋업 + Float이 active이면 LineRenderer 활성화.
        /// </summary>
        [UnityTest]
        public IEnumerator FishingLineRenderer_AllRefsSet_EnablesLine()
        {
            var rodTip = new GameObject("RodTip");
            rodTip.transform.position = new Vector3(0f, 1f, 0f);

            var floatGO = new GameObject("Float");
            floatGO.transform.position = new Vector3(0f, 0f, 1f);
            floatGO.SetActive(false);
            var rb = floatGO.AddComponent<Rigidbody>();
            rb.useGravity = false; rb.isKinematic = true;
            var floatCtrl = floatGO.AddComponent<FloatController>();
            floatGO.SetActive(true);

            var lineGO = new GameObject("Line");
            lineGO.SetActive(false);
            var lr = lineGO.AddComponent<LineRenderer>();
            var renderer = lineGO.AddComponent<FishingLineRenderer>();
            SetField(renderer, "rodTip", rodTip.transform);
            SetField(renderer, "floatController", floatCtrl);
            lineGO.SetActive(true);

            yield return null;
            yield return null;

            Assert.IsTrue(lr.enabled,
                "참조 모두 갖춰지고 Float active이면 라인 활성화");

            Object.Destroy(lineGO);
            Object.Destroy(floatGO);
            Object.Destroy(rodTip);
        }

        #endregion

        // ────────────────────────────────────────────────────────────────────
        // ReelController
        // ────────────────────────────────────────────────────────────────────

        #region ReelController

        /// <summary>
        /// FishingRodController.ReelingSpeed에 비례해 reelPivot이 회전.
        /// 같은 시간 동안 speed가 2배면 회전각도 2배.
        /// </summary>
        [UnityTest]
        public IEnumerator ReelController_RotationScalesWithReelingSpeed()
        {
            var fakeRod = CreateFakeRodWithReelingSpeed(0f);
            var pivot = new GameObject("Pivot");

            var reelGO = new GameObject("Reel");
            reelGO.SetActive(false);
            var reel = reelGO.AddComponent<ReelController>();
            SetField(reel, "rodController", fakeRod);
            SetField(reel, "reelPivot", pivot.transform);
            SetField(reel, "degreesPerSecondAtFullSpeed", 360f);
            SetField(reel, "rotationAxis", Vector3.right);
            reelGO.SetActive(true);

            // Speed = 0 → 회전 없음
            yield return new WaitForSeconds(0.2f);
            float angleAfterIdle = QuaternionAngleX(pivot.transform.localRotation);
            Assert.That(Mathf.Abs(angleAfterIdle), Is.LessThan(2f),
                "ReelingSpeed=0 → 회전 거의 없음");

            // Speed = 1 → 단위 시간당 360도 회전
            SetReelingSpeed(fakeRod, 1f);
            yield return new WaitForSeconds(0.2f);
            float angleAfterFull = QuaternionAngleX(pivot.transform.localRotation);
            // 0.2초 * 360deg/s = 72도 누적되어야 함 (오차 허용)
            Assert.That(Mathf.Abs(angleAfterFull), Is.GreaterThan(40f),
                "ReelingSpeed=1로 0.2초 → 충분히 회전");

            Object.Destroy(reelGO);
            Object.Destroy(pivot);
            Object.Destroy(fakeRod.gameObject);
        }

        /// <summary>
        /// reelPivot 미할당 시 자동으로 자기 자신 transform으로 fallback.
        /// </summary>
        [UnityTest]
        public IEnumerator ReelController_Awake_FallsBackPivotToSelf()
        {
            var fakeRod = CreateFakeRodWithReelingSpeed(0f);

            var reelGO = new GameObject("Reel");
            reelGO.SetActive(false);
            var reel = reelGO.AddComponent<ReelController>();
            SetField(reel, "rodController", fakeRod);
            // reelPivot 일부러 null 유지
            reelGO.SetActive(true);

            yield return null;

            // Awake에서 reelPivot = transform 으로 채워졌는지 확인
            var assigned = (Transform)GetField(reel, "reelPivot");
            Assert.AreEqual(reel.transform, assigned,
                "reelPivot 미할당 시 자기 transform으로 자동 할당");

            Object.Destroy(reelGO);
            Object.Destroy(fakeRod.gameObject);
        }

        // ReelController는 FishingRodController에서 ReelingSpeed만 읽음.
        // 테스트 격리 위해 진짜 FishingRodController를 만들고 ForceState로 reeling 흉내.
        private static FishingRodController CreateFakeRodWithReelingSpeed(float speed)
        {
            var settings = ScriptableObject.CreateInstance<GameSettingsSO>();
            var playerData = ScriptableObject.CreateInstance<PlayerDataSO>();

            var go = new GameObject("FakeRod");
            go.SetActive(false);
            var rod = go.AddComponent<FishingRodController>();
            SetField(rod, "gameSettings", settings);
            SetField(rod, "playerData", playerData);
            go.SetActive(true);

            // ReelingSpeed는 UpdateReelingInput으로 설정
            rod.UpdateReelingInput(speed);
            return rod;
        }

        private static void SetReelingSpeed(FishingRodController rod, float speed)
        {
            rod.UpdateReelingInput(speed);
        }

        // X축 회전각만 추출 (rotationAxis = Vector3.right로 설정)
        private static float QuaternionAngleX(Quaternion q)
        {
            return q.eulerAngles.x > 180f ? q.eulerAngles.x - 360f : q.eulerAngles.x;
        }

        #endregion

        // ────────────────────────────────────────────────────────────────────
        // FloatController 엣지 케이스
        // ────────────────────────────────────────────────────────────────────

        #region FloatController

        private GameObject _floatGO;
        private FloatController _float;
        private GameSettingsSO _floatSettings;

        private IEnumerator SetUpFloat()
        {
            _floatSettings = ScriptableObject.CreateInstance<GameSettingsSO>();
            _floatSettings.castingBoundaryRadius = 15f;
            _floatSettings.minCastingDistance = 2f;

            _floatGO = new GameObject("Float");
            _floatGO.transform.position = new Vector3(0f, 5f, 0f);
            _floatGO.SetActive(false);
            var rb = _floatGO.AddComponent<Rigidbody>();
            rb.useGravity = false; rb.isKinematic = true;
            _float = _floatGO.AddComponent<FloatController>();
            SetField(_float, "gameSettings", _floatSettings);
            _floatGO.SetActive(true);
            yield return null;
        }

        private IEnumerator TearDownFloat()
        {
            Object.Destroy(_floatGO);
            Object.Destroy(_floatSettings);
            yield return null;
        }

        /// <summary>
        /// OnWaterContact는 idempotent — 두 번 호출돼도 OnWaterLanded는 1회만 발화.
        /// </summary>
        [UnityTest]
        public IEnumerator Float_OnWaterContact_Idempotent()
        {
            yield return SetUpFloat();

            int landedCount = 0;
            _float.OnWaterLanded += () => landedCount++;

            _float.Launch(5f, Vector3.forward);
            yield return null;
            _float.OnWaterContact();
            _float.OnWaterContact(); // 중복 호출
            yield return null;

            Assert.AreEqual(1, landedCount,
                "_hasLanded 가드로 OnWaterLanded는 첫 호출에만 발화");

            yield return TearDownFloat();
        }

        /// <summary>
        /// ResetFloat 호출 시 rodTip과 가까우면 즉시 AttachedToRod, 멀면 Reeling.
        /// rodTip null 케이스 fallback도 안전.
        /// </summary>
        [UnityTest]
        public IEnumerator Float_ResetFloat_NoRodTip_GoesAttached()
        {
            yield return SetUpFloat();

            _float.Launch(5f, Vector3.forward);
            yield return null;
            _float.OnWaterContact();
            yield return null;

            // rodTip 없는 상태 — fallback으로 AttachedToRod
            _float.ResetFloat();
            yield return null;

            Assert.AreEqual(0f, _float.SinkingDepth, 0.01f);
            // SinkingDepth 0이면 reset 처리됨 (state는 private이라 직접 접근 불가, 부수효과로 검증)

            yield return TearDownFloat();
        }

        #endregion

        // ────────────────────────────────────────────────────────────────────
        // FishingRodController 추가 엣지 케이스
        // ────────────────────────────────────────────────────────────────────

        #region FishingRodController Edge

        /// <summary>
        /// Casting 상태가 아닐 때 (예: Attached) Float이 OnWaterContact를 호출해도
        /// rod 상태는 변하지 않음 (HandleWaterLanded의 if (Casting) 가드).
        /// </summary>
        [UnityTest]
        public IEnumerator HandleWaterLanded_IgnoredWhenNotInCasting()
        {
            var settings = ScriptableObject.CreateInstance<GameSettingsSO>();
            var playerData = ScriptableObject.CreateInstance<PlayerDataSO>();

            var floatGO = new GameObject("Float");
            floatGO.transform.position = new Vector3(0f, 5f, 0f);
            floatGO.SetActive(false);
            var rb = floatGO.AddComponent<Rigidbody>();
            rb.useGravity = false; rb.isKinematic = true;
            var floatCtrl = floatGO.AddComponent<FloatController>();
            SetField(floatCtrl, "gameSettings", settings);
            floatGO.SetActive(true);

            var rodGO = new GameObject("Rod");
            rodGO.SetActive(false);
            var rod = rodGO.AddComponent<FishingRodController>();
            SetField(rod, "gameSettings", settings);
            SetField(rod, "playerData", playerData);
            SetField(rod, "floatCtrl", floatCtrl);
            rodGO.SetActive(true);

            var hand = new GameObject("Hand");
            rod.OnGrab(hand.transform); // → Attached
            yield return null;

            floatCtrl.OnWaterContact();
            yield return null;
            yield return null;

            Assert.AreEqual(RodState.Attached, rod.CurrentState,
                "Casting이 아닌 Attached 상태에서 OnWaterLanded는 무시 (시나리오 03 사전조건 보호)");

            Object.Destroy(rodGO); Object.Destroy(floatGO); Object.Destroy(hand);
            Object.Destroy(settings); Object.Destroy(playerData);
        }

        #endregion

        // ────────────────────────────────────────────────────────────────────
        // 헬퍼
        // ────────────────────────────────────────────────────────────────────

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

        private static object GetField(object target, string fieldName)
        {
            var type = target.GetType();
            while (type != null)
            {
                var field = type.GetField(fieldName,
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance);
                if (field != null)
                    return field.GetValue(target);
                type = type.BaseType;
            }
            return null;
        }
    }
}
