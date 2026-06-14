using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VirtualFishing.Account;
using VirtualFishing.Data;

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
        [SerializeField] private DifficultySettingsSO difficultySettings;

        [Header("UI 루트")]
        [SerializeField] private Transform accountButtonContainer;
        [SerializeField] private GameObject accountButtonPrefab;
        [SerializeField] private Button createNewAccountButton;
        [Tooltip("Inspector에서 OnClick → ToggleEasyMode 연결. 씬에 직접 배치한 버튼을 할당한다.")]
        [SerializeField] private Button easyModeButton;
        [SerializeField] private TextMeshProUGUI easyModeButtonLabel;
        [Tooltip("비워두면 Easy Mode Button의 Image를 사용한다.")]
        [SerializeField] private Image easyModeButtonBackground;

        [Header("난이도 버튼 색상")]
        [SerializeField] private Color easyModeOffColor = new(0.682f, 0.682f, 0.682f, 1f);       // #AEAEAE
        [SerializeField] private Color easyModeOnColor = new(0.486f, 1f, 0.773f, 1f);            // #7CFFC5
        [SerializeField] private Color easyModeOffTextColor = Color.black;
        [SerializeField] private Color easyModeOnTextColor = new(0f, 0.831f, 0.435f, 1f);         // #00D46F

        [SerializeField] private TextMeshProUGUI statusText;

        [Header("계정 정보 프리뷰 (Hover/Focus)")]
        [SerializeField] private GameObject accountPreviewPanel;
        [SerializeField] private TextMeshProUGUI previewNameText;
        [SerializeField] private TextMeshProUGUI previewLastPlayedText;
        [SerializeField] private TextMeshProUGUI previewScoreText;
        [SerializeField] private TextMeshProUGUI previewDexCountText;

        [Header("설정")]
        [SerializeField] private int maxAccountCount = 4;
        [SerializeField] private string accountPrefix = "플레이어";

        private string _saveFolderPath;
        private List<string> _existingAccounts = new();
        private readonly Dictionary<string, AccountPreviewData> _previewByAccountId = new();

        private struct AccountPreviewData
        {
            public string accountId;
            public string lastPlayedAt;
            public int totalScore;
            public int encyclopediaCount;
        }

        private void Awake()
        {
            _saveFolderPath = Path.Combine(Application.persistentDataPath, "Accounts");
        }

        private void OnEnable()
        {
            SetPreviewVisible(false);
            RefreshEasyModeButtonVisual();
            RefreshAccountList();
        }

        /// <summary>저장 폴더를 스캔해 계정 버튼 목록을 갱신한다.</summary>
        public void RefreshAccountList()
        {
            ClearAccountButtons();
            _existingAccounts = LoadAccountIds();
            BuildPreviewCache();

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

        private void BuildPreviewCache()
        {
            _previewByAccountId.Clear();
            foreach (string accountId in _existingAccounts)
            {
                string filePath = Path.Combine(_saveFolderPath, $"{accountId}.json");
                _previewByAccountId[accountId] = LoadPreviewData(accountId, filePath);
            }
        }

        private static AccountPreviewData LoadPreviewData(string accountId, string filePath)
        {
            var fallback = new AccountPreviewData
            {
                accountId = accountId,
                lastPlayedAt = "기록 없음",
                totalScore = 0,
                encyclopediaCount = 0
            };

            if (!File.Exists(filePath))
            {
                return fallback;
            }

            try
            {
                string json = File.ReadAllText(filePath);
                AccountSaveData saveData = JsonUtility.FromJson<AccountSaveData>(json);
                if (saveData == null)
                {
                    return fallback;
                }

                return new AccountPreviewData
                {
                    accountId = accountId,
                    lastPlayedAt = string.IsNullOrEmpty(saveData.lastPlayedAt) ? "기록 없음" : saveData.lastPlayedAt,
                    totalScore = saveData.totalScore,
                    encyclopediaCount = saveData.encyclopedia != null ? saveData.encyclopedia.Count : 0
                };
            }
            catch
            {
                return fallback;
            }
        }

        private void SpawnAccountButton(string accountId)
        {
            var go = Instantiate(accountButtonPrefab, accountButtonContainer);

            var label = go.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
                label.text = accountId;

            var btn = go.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.AddListener(() => OnAccountSelected(accountId));
                BindPreviewEvents(btn.gameObject, accountId);
            }
        }

        private void BindPreviewEvents(GameObject target, string accountId)
        {
            var trigger = target.GetComponent<EventTrigger>();
            if (trigger == null)
            {
                trigger = target.AddComponent<EventTrigger>();
            }

            AddEvent(trigger, EventTriggerType.PointerEnter, _ => ShowAccountPreview(accountId));
            AddEvent(trigger, EventTriggerType.Select, _ => ShowAccountPreview(accountId));
            AddEvent(trigger, EventTriggerType.PointerExit, _ => SetPreviewVisible(false));
            AddEvent(trigger, EventTriggerType.Deselect, _ => SetPreviewVisible(false));
        }

        private static void AddEvent(
            EventTrigger trigger,
            EventTriggerType eventType,
            UnityEngine.Events.UnityAction<BaseEventData> action)
        {
            var entry = new EventTrigger.Entry
            {
                eventID = eventType
            };
            entry.callback.AddListener(action);
            trigger.triggers.Add(entry);
        }

        private void ShowAccountPreview(string accountId)
        {
            if (!_previewByAccountId.TryGetValue(accountId, out AccountPreviewData data))
            {
                data = new AccountPreviewData
                {
                    accountId = accountId,
                    lastPlayedAt = "기록 없음",
                    totalScore = 0,
                    encyclopediaCount = 0
                };
            }

            if (previewNameText != null) previewNameText.text = data.accountId;
            if (previewLastPlayedText != null) previewLastPlayedText.text = $"최근 접속: {data.lastPlayedAt}";
            if (previewScoreText != null) previewScoreText.text = $"총 점수: {data.totalScore}";
            if (previewDexCountText != null) previewDexCountText.text = $"도감 수: {data.encyclopediaCount}";

            SetPreviewVisible(true);
        }

        private void SetPreviewVisible(bool visible)
        {
            if (accountPreviewPanel != null)
            {
                accountPreviewPanel.SetActive(visible);
            }
        }

        /// <summary>기존 계정 버튼 클릭 시 호출된다.</summary>
        public void OnAccountSelected(string accountId)
        {
            SetStatus($"{accountId} 로딩 중...");
            accountManager.LoadAccount(accountId);
        }

        /// <summary>
        /// 쉬운 난이도 토글 버튼 OnClick에 연결.
        /// 난이도만 변경하며, 씬 전환은 계정 선택/생성 시에만 발생한다.
        /// </summary>
        public void ToggleEasyMode()
        {
            if (difficultySettings == null)
            {
                SetStatus("난이도 설정을 찾을 수 없습니다.");
                return;
            }

            difficultySettings.SetEasyMode(!difficultySettings.IsEasyMode);
            RefreshEasyModeButtonVisual();
            SetStatus(difficultySettings.IsEasyMode ? "쉬운 난이도가 활성화되었습니다." : "일반 난이도로 변경되었습니다.");
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

        private void RefreshEasyModeButtonVisual()
        {
            if (easyModeButtonLabel == null && easyModeButton != null)
                easyModeButtonLabel = easyModeButton.GetComponentInChildren<TextMeshProUGUI>();

            if (difficultySettings == null)
                return;

            bool isOn = difficultySettings.IsEasyMode;

            if (easyModeButtonLabel != null)
            {
                easyModeButtonLabel.text = isOn ? "ON" : "OFF";
                easyModeButtonLabel.color = isOn ? easyModeOnTextColor : easyModeOffTextColor;
            }

            Image background = easyModeButtonBackground;
            if (background == null && easyModeButton != null)
                background = easyModeButton.GetComponent<Image>();

            if (background != null)
                background.color = isOn ? easyModeOnColor : easyModeOffColor;
        }

        [ContextMenu("Debug: Refresh List")]
        private void DebugRefresh() => RefreshAccountList();
    }
}
