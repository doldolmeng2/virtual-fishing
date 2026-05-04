using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VirtualFishing.Account;

namespace VirtualFishing.UI
{
    /// <summary>
    /// 로그인 화면 UI 제어.
    /// 저장된 계정 목록을 버튼으로 표시하고, 계정 선택/생성 시 AccountManager에 위임한다.
    /// 최대 계정 수에 도달하면 신규 생성 버튼을 숨긴다.
    /// </summary>
    public class LoginUIController : MonoBehaviour
    {
        [Header("외부 서비스")]
        [SerializeField] private AccountManager accountManager;

        [Header("UI 루트")]
        [SerializeField] private Transform accountButtonContainer;
        [SerializeField] private GameObject accountButtonPrefab;
        [SerializeField] private Button createNewAccountButton;
        [SerializeField] private TextMeshProUGUI statusText;

        [Header("설정")]
        [SerializeField] private int maxAccountCount = 4;
        [SerializeField] private string accountPrefix = "플레이어";

        private string _saveFolderPath;
        private List<string> _existingAccounts = new();

        private void Awake()
        {
            _saveFolderPath = Path.Combine(Application.persistentDataPath, "Accounts");
        }

        private void OnEnable()
        {
            RefreshAccountList();
        }

        /// <summary>저장 폴더를 스캔해 계정 버튼 목록을 갱신한다.</summary>
        public void RefreshAccountList()
        {
            ClearAccountButtons();
            _existingAccounts = LoadAccountIds();

            foreach (string id in _existingAccounts)
                SpawnAccountButton(id);

            bool canCreate = _existingAccounts.Count < maxAccountCount;
            createNewAccountButton.gameObject.SetActive(canCreate);

            SetStatus("");
        }

        private void ClearAccountButtons()
        {
            foreach (Transform child in accountButtonContainer)
                Destroy(child.gameObject);
        }

        private List<string> LoadAccountIds()
        {
            var ids = new List<string>();

            if (!Directory.Exists(_saveFolderPath))
                return ids;

            foreach (string path in Directory.GetFiles(_saveFolderPath, "*.json"))
            {
                string fileName = Path.GetFileNameWithoutExtension(path);
                if (!fileName.EndsWith(".backup"))
                    ids.Add(fileName);
            }

            return ids;
        }

        private void SpawnAccountButton(string accountId)
        {
            var go = Instantiate(accountButtonPrefab, accountButtonContainer);

            var label = go.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
                label.text = accountId;

            var btn = go.GetComponent<Button>();
            if (btn != null)
                btn.onClick.AddListener(() => OnAccountSelected(accountId));
        }

        /// <summary>기존 계정 버튼 클릭 시 호출된다.</summary>
        public void OnAccountSelected(string accountId)
        {
            SetStatus($"{accountId} 로딩 중...");
            accountManager.LoadAccount(accountId);
        }

        /// <summary>새 계정 만들기 버튼 클릭 시 호출된다.</summary>
        public void OnCreateNewAccount()
        {
            if (_existingAccounts.Count >= maxAccountCount)
            {
                SetStatus($"계정은 최대 {maxAccountCount}개까지 만들 수 있습니다.");
                return;
            }

            string newId = GenerateAccountId();
            SetStatus($"{newId} 생성 중...");
            accountManager.LoadAccount(newId);
        }

        /// <summary>accountPrefix + 번호(1부터) 방식으로 중복 없는 ID를 반환한다.</summary>
        private string GenerateAccountId()
        {
            int index = 1;
            string candidate;

            do
            {
                candidate = $"{accountPrefix}{index}";
                index++;
            }
            while (_existingAccounts.Contains(candidate));

            return candidate;
        }

        private void SetStatus(string message)
        {
            if (statusText != null)
                statusText.text = message;
        }

        [ContextMenu("Debug: Refresh List")]
        private void DebugRefresh() => RefreshAccountList();
    }
}
