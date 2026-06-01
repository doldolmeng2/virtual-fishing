using System;
using UnityEngine;
using VirtualFishing.Account;
using VirtualFishing.Core.Events;
using VirtualFishing.Data;
using VirtualFishing.MiniGame;

namespace VirtualFishing.Core
{
    /// <summary>
    /// 미니게임 포획 성공(OnFishCaughtEvent) 시 도감·점수를 계정에 반영한다.
    /// AccountManager는 이벤트를 구독하지 않고, 이 브리지만 오케스트레이션한다.
    /// </summary>
    public class FishCatchAccountBridge : MonoBehaviour, IVoidEventListener
    {
        [Header("이벤트")]
        [SerializeField] private VoidEventSO onFishCaughtEvent;

        [Header("참조 (비우면 Awake에서 탐색)")]
        [SerializeField] private AccountManager accountManager;
        [SerializeField] private MiniGameManager miniGameManager;

        [Header("에디터 테스트")]
        [Tooltip("accountId가 비어 있을 때 에디터에서만 자동 로드할 계정 ID. 빈 문자열이면 자동 로드 안 함.")]
        [SerializeField] private string editorFallbackAccountId = "TestAccount";

        private void Awake()
        {
            if (accountManager == null)
                accountManager = FindFirstObjectByType<AccountManager>();

            if (miniGameManager == null)
                miniGameManager = FindFirstObjectByType<MiniGameManager>();
        }

        private void OnEnable() => onFishCaughtEvent?.Register(this);
        private void OnDisable() => onFishCaughtEvent?.Unregister(this);

        void IVoidEventListener.OnEventRaised() => SaveCaughtFishToAccount();

        /// <summary>인스펙터 UnityEvent / 디버그용.</summary>
        public void SaveCaughtFishToAccount()
        {
            if (miniGameManager == null)
            {
                Debug.LogWarning("[FishCatchAccount] MiniGameManager를 찾을 수 없습니다.");
                return;
            }

            if (accountManager == null)
            {
                Debug.LogWarning("[FishCatchAccount] AccountManager를 찾을 수 없습니다.");
                return;
            }

            FishCatchData data = miniGameManager.CurrentFishData;
            if (data.species == null)
            {
                Debug.LogWarning("[FishCatchAccount] CurrentFishData.species가 null입니다. 저장을 건너뜁니다.");
                return;
            }

            if (string.IsNullOrEmpty(data.caughtAt))
                data.caughtAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm");

            if (!EnsureAccountReady())
                return;

            accountManager.AddToEncyclopedia(data);
            accountManager.SaveAccount();

            Debug.Log(
                $"[FishCatchAccount] 저장 완료 — {data.species.DisplayName} " +
                $"(+{data.weight}kg), 계정:{accountManager.HasActiveAccount}");
        }

        private bool EnsureAccountReady()
        {
#if UNITY_EDITOR
            if (!accountManager.HasActiveAccount && !string.IsNullOrEmpty(editorFallbackAccountId))
            {
                Debug.LogWarning(
                    $"[FishCatchAccount] accountId 없음 — 에디터에서 '{editorFallbackAccountId}' 로드합니다.");
                accountManager.LoadAccount(editorFallbackAccountId);
            }
#endif
            if (accountManager.HasActiveAccount)
                return true;

            Debug.LogError(
                "[FishCatchAccount] 활성 계정이 없습니다. Login 씬에서 계정을 선택한 뒤 Main으로 진입하세요.");
            return false;
        }
    }
}
