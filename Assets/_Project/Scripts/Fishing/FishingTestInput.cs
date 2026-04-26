using UnityEngine;
using UnityEngine.InputSystem;
using VirtualFishing.Data;

namespace VirtualFishing.Fishing
{
    public class FishingTestInput : MonoBehaviour
    {
        [Header("참조")]
        [SerializeField] private FishingRodController rodController;
        [SerializeField] private Transform simulatedHand;
        [SerializeField] private PlayerDataSO playerData;
        [SerializeField] private GameSettingsSO gameSettings;

        [Header("시뮬레이션 설정")]
        [SerializeField] private float handMoveSpeed = 3f;
        [SerializeField] private float castSwingSpeed = 8f;

        private bool _isSwinging;
        private float _swingTimer;
        private Keyboard _kb;
        private bool _ready;

        private void Start()
        {
            if (rodController == null) { Debug.LogError("[TestInput] rodController 미할당!"); return; }
            if (simulatedHand == null) { Debug.LogError("[TestInput] simulatedHand 미할당!"); return; }
            if (playerData == null) { Debug.LogError("[TestInput] playerData 미할당!"); return; }
            if (gameSettings == null) { Debug.LogError("[TestInput] gameSettings 미할당!"); return; }

            _kb = Keyboard.current;
            if (_kb == null) { Debug.LogError("[TestInput] Keyboard.current == null!"); return; }

            playerData.sittingHeight = 1.2f;
            playerData.armLength = 0.6f;
            playerData.currentPosition = transform.position;
            playerData.safetyRadius = 1.5f;

            if (simulatedHand != null)
            {
                simulatedHand.position = new Vector3(
                    transform.position.x,
                    playerData.sittingHeight,
                    transform.position.z
                );
            }

            _ready = true;
            Debug.Log("[TestInput] 초기화 완료");
        }

        private void Update()
        {
            if (!_ready) return;
            _kb = Keyboard.current;
            if (_kb == null) return;

            HandleHandMovement();
            HandleGrabInput();
            HandleCastInput();
            HandleReelingInput();
            HandleDebugInput();

            rodController.UpdateCastingInput(GetSimulatedVelocity(), simulatedHand.forward);
        }

        private void HandleHandMovement()
        {
            float h = 0f, v = 0f, y = 0f;
            if (_kb.aKey.isPressed) h = -1f;
            if (_kb.dKey.isPressed) h = 1f;
            if (_kb.wKey.isPressed) v = 1f;
            if (_kb.sKey.isPressed) v = -1f;
            if (_kb.qKey.isPressed) y = -1f;
            if (_kb.eKey.isPressed) y = 1f;

            Vector3 move = new Vector3(h, y, v) * handMoveSpeed * Time.deltaTime;
            simulatedHand.position += move;
        }

        private void HandleGrabInput()
        {
            if (_kb.gKey.wasPressedThisFrame)
            {
                if (rodController.IsGrabbed)
                {
                    Debug.Log("[TestInput] Release");
                    rodController.OnRelease();
                }
                else
                {
                    Debug.Log("[TestInput] Grab");
                    rodController.OnGrab(simulatedHand);
                }
            }
        }

        // 0: 대기, 1: 존 안에서 체류 중, 2: 존 밖으로 이동 중
        private int _castPhase;

        private void HandleCastInput()
        {
            if (_kb.spaceKey.wasPressedThisFrame && rodController.CurrentState == RodState.Attached)
            {
                Debug.Log("[TestInput] 캐스팅 시작 → 존 진입");
                _castPhase = 1;
                _swingTimer = 0f;

                // 캐스팅 존 중심으로 손 이동
                Vector3 zoneCenter = playerData.currentPosition;
                zoneCenter.y = playerData.sittingHeight + gameSettings.castingZoneOffset.y + 0.1f;
                simulatedHand.position = zoneCenter;
            }

            if (_castPhase == 1)
            {
                // 존 안에서 체류 (minCastingHoldTime 이상)
                _swingTimer += Time.deltaTime;
                if (_swingTimer > gameSettings.minCastingHoldTime + 0.1f)
                {
                    Debug.Log("[TestInput] 체류 완료 → 존 이탈 시작");
                    _castPhase = 2;
                    _swingTimer = 0f;
                }
            }
            else if (_castPhase == 2)
            {
                // 존 밖으로 빠르게 이동 (여러 프레임에 걸쳐)
                simulatedHand.position += Vector3.forward * castSwingSpeed * Time.deltaTime;
                _swingTimer += Time.deltaTime;

                // Cast 성공(찌 발사됨) 또는 시간 초과 시 즉시 종료
                if (rodController.IsCasting || _swingTimer > 2.5f)
                {
                    Debug.Log("[TestInput] 캐스팅 시뮬레이션 종료");
                    _castPhase = 0;
                    _swingTimer = 0f;
                }
            }
        }

        private void HandleReelingInput()
        {
            // F키 홀드: 릴 감기 (수면 위 찌를 천천히 당겨옴)
            float reelSpeed = _kb.fKey.isPressed ? 1f : 0f;
            rodController.UpdateReelingInput(reelSpeed);
        }

        private void HandleDebugInput()
        {
            // R: 줄 회수 (캐스팅 이후 상태에서만)
            if (_kb.rKey.wasPressedThisFrame)
            {
                var state = rodController.CurrentState;
                if (state == RodState.Casting || state == RodState.WaitingForBite
                    || state == RodState.Hit || state == RodState.MiniGame)
                {
                    Debug.Log("[TestInput] 줄 회수 (R)");
                    rodController.ReelIn();
                }
                else
                {
                    Debug.Log($"[TestInput] 회수 불가 - 현재 상태: {state}");
                }
            }

            // B: 입질 시뮬레이션
            if (_kb.bKey.wasPressedThisFrame)
            {
                if (rodController.CurrentState == RodState.WaitingForBite)
                {
                    Debug.Log("[TestInput] 입질!");
                    rodController.HandleBiteOccurred();
                }
                else
                {
                    Debug.Log($"[TestInput] 입질 불가 - 현재 상태: {rodController.CurrentState} (WaitingForBite 필요)");
                }
            }

            // H: 챔질 시뮬레이션
            if (_kb.hKey.wasPressedThisFrame)
            {
                if (rodController.CurrentState == RodState.WaitingForBite)
                {
                    Debug.Log("[TestInput] 챔질!");
                    Vector3 hookZone = playerData.currentPosition;
                    hookZone.y = playerData.sittingHeight + gameSettings.hookingZoneOffset.y;
                    simulatedHand.position = hookZone;
                }
                else
                {
                    Debug.Log($"[TestInput] 챔질 불가 - 현재 상태: {rodController.CurrentState}");
                }
            }

            // 숫자키: 강제 상태 전환
            if (_kb.digit1Key.wasPressedThisFrame) { rodController.ForceState(RodState.Idle); }
            if (_kb.digit2Key.wasPressedThisFrame) { rodController.ForceState(RodState.Attached); }
            if (_kb.digit3Key.wasPressedThisFrame) { rodController.ForceState(RodState.Casting); }
            if (_kb.digit4Key.wasPressedThisFrame) { rodController.ForceState(RodState.WaitingForBite); }
            if (_kb.digit5Key.wasPressedThisFrame) { rodController.ForceState(RodState.Hit); }
            if (_kb.digit6Key.wasPressedThisFrame) { rodController.ForceState(RodState.MiniGame); }
        }

        private Vector3 GetSimulatedVelocity()
        {
            // 캐스팅 존 이탈 중 → 앞으로 빠른 속도
            if (_castPhase == 2)
                return Vector3.forward * castSwingSpeed;

            // 챔질 중 → 위로 빠른 속도
            if (_kb.hKey.isPressed)
                return Vector3.up * (gameSettings.hookingMinAcceleration + 1f);

            return Vector3.zero;
        }

        private void OnGUI()
        {
            if (rodController == null) return;

            GUIStyle style = new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold };
            style.normal.textColor = Color.white;
            GUIStyle headerStyle = new GUIStyle(style) { fontSize = 16 };
            headerStyle.normal.textColor = Color.yellow;

            float x = 10f, y = 10f, w = 380f, lineH = 20f;
            GUI.Box(new Rect(x - 5, y - 5, w + 10, lineH * 20 + 10), "");

            GUI.Label(new Rect(x, y, w, lineH), "=== Fishing Debug ===", headerStyle); y += lineH + 4;

            GUI.Label(new Rect(x, y, w, lineH), $"State: {rodController.CurrentState}", style); y += lineH;
            GUI.Label(new Rect(x, y, w, lineH), $"Grabbed: {rodController.IsGrabbed}", style); y += lineH;
            GUI.Label(new Rect(x, y, w, lineH), $"CastingZone: {rodController.IsInCastingZone}", style); y += lineH;
            GUI.Label(new Rect(x, y, w, lineH), $"HookingZone: {rodController.IsInHookingZone}", style); y += lineH;
            GUI.Label(new Rect(x, y, w, lineH), $"Accel: {rodController.Acceleration:F2}", style); y += lineH;
            GUI.Label(new Rect(x, y, w, lineH), $"ReelSpeed: {rodController.ReelingSpeed:F2}", style); y += lineH;
            GUI.Label(new Rect(x, y, w, lineH), $"Hand: {simulatedHand.position.ToString("F2")}", style); y += lineH;

            y += 8;
            GUI.Label(new Rect(x, y, w, lineH), "--- 조작 ---", headerStyle); y += lineH;
            GUI.Label(new Rect(x, y, w, lineH), "[G] Grab/Release", style); y += lineH;
            GUI.Label(new Rect(x, y, w, lineH), "[Space] Cast  [F hold] Reel", style); y += lineH;
            GUI.Label(new Rect(x, y, w, lineH), "[R] Quick Reel In", style); y += lineH;
            GUI.Label(new Rect(x, y, w, lineH), "[B] Bite  [H] Hook", style); y += lineH;
            GUI.Label(new Rect(x, y, w, lineH), "[WASD] Move  [QE] Up/Down", style); y += lineH;

            y += 8;
            GUI.Label(new Rect(x, y, w, lineH), "--- 강제 전환 ---", headerStyle); y += lineH;
            GUI.Label(new Rect(x, y, w, lineH), "[1]Idle [2]Attach [3]Cast [4]Wait", style); y += lineH;
            GUI.Label(new Rect(x, y, w, lineH), "[5]Hit [6]MiniGame", style);
        }
    }
}
