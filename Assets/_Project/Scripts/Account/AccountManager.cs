using System;
using System.Collections;
using System.IO;
using UnityEngine;
using VirtualFishing.Core.Events;
using VirtualFishing.Data;
using VirtualFishing.Interfaces;

namespace VirtualFishing.Account
{
    public class AccountManager : MonoBehaviour, IAccountService
    {
        [Header("런타임 데이터 (SO)")]
        [SerializeField] private AccountDataSO accountData;

        [Header("설정")]
        [SerializeField] private GameSettingsSO gameSettings;

        [Header("이벤트 채널 (출력)")]
        [SerializeField] private VoidEventSO onAccountLoaded;
        [SerializeField] private VoidEventSO onAccountSaved;

        public event Action OnAccountLoaded;
        public event Action OnAccountSaved;

        private string _saveFolderPath;
        private Coroutine _autoSaveCoroutine;

        private void Awake()
        {
            _saveFolderPath = Path.Combine(Application.persistentDataPath, "Accounts");
            if (!Directory.Exists(_saveFolderPath))
                Directory.CreateDirectory(_saveFolderPath);

            ClearRuntimeData();
        }

        private void ClearRuntimeData()
        {
            accountData.accountId = "";
            accountData.lastPlayedAt = "";
            accountData.encyclopedia.Clear();
            accountData.totalScore = 0;
        }

        private void OnEnable()
        {
            StartAutoSave();
        }

        private void OnDisable()
        {
            StopAutoSave();
        }

        public void LoadAccount(string accountId)
        {
            string filePath = GetSavePath(accountId);

            if (File.Exists(filePath))
            {
                try
                {
                    string json = File.ReadAllText(filePath);
                    var saveData = JsonUtility.FromJson<AccountSaveData>(json);

                    if (saveData == null || string.IsNullOrEmpty(saveData.accountId))
                        throw new Exception("역직렬화 결과가 유효하지 않음");

                    ApplyToSO(saveData);
                    Debug.Log($"[Account] 계정 로드 완료: {accountId}");
                }
                catch (Exception e)
                {
                    Debug.LogError($"[Account] 세이브 파일 손상 — 백업 후 새 계정 생성: {e.Message}");

                    string backupPath = filePath + $".backup_{DateTime.Now:yyyyMMdd_HHmmss}";
                    try { File.Copy(filePath, backupPath); }
                    catch { /* 백업 실패해도 진행 */ }

                    InitializeNewAccount(accountId);
                }
            }
            else
            {
                InitializeNewAccount(accountId);
            }

            UpdateLastPlayedAt();

            onAccountLoaded?.Raise();
            OnAccountLoaded?.Invoke();
        }

        private void InitializeNewAccount(string accountId)
        {
            accountData.accountId = accountId;
            accountData.lastPlayedAt = "";
            accountData.encyclopedia.Clear();
            accountData.totalScore = 0;
            Debug.Log($"[Account] 새 계정 생성: {accountId}");
        }

        public void SaveAccount()
        {
            if (string.IsNullOrEmpty(accountData.accountId))
            {
                Debug.LogWarning("[Account] 저장 실패: accountId가 비어있음");
                return;
            }

            var saveData = ExtractFromSO();
            string json = JsonUtility.ToJson(saveData, true);
            string filePath = GetSavePath(accountData.accountId);

            File.WriteAllText(filePath, json);
            Debug.Log($"[Account] 계정 저장 완료: {filePath}");

            onAccountSaved?.Raise();
            OnAccountSaved?.Invoke();
        }

        public void UpdateLastPlayedAt()
        {
            accountData.lastPlayedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }

        public void AddToEncyclopedia(FishCatchData fishData)
        {
            var record = new FishCatchRecord
            {
                speciesName = fishData.species != null ? fishData.species.speciesName : "Unknown",
                weight = fishData.weight,
                caughtAt = fishData.caughtAt,
                siteName = fishData.siteType.ToString(),
                score = CalculateScore(fishData)
            };

            accountData.encyclopedia.Add(record);
            accountData.totalScore += record.score;

            Debug.Log($"[Account] 도감 추가: {record.speciesName} ({record.weight}kg, +{record.score}점)");
        }

        private void StartAutoSave()
        {
            if (gameSettings == null) return;
            _autoSaveCoroutine = StartCoroutine(AutoSaveLoop());
        }

        private void StopAutoSave()
        {
            if (_autoSaveCoroutine != null)
            {
                StopCoroutine(_autoSaveCoroutine);
                _autoSaveCoroutine = null;
            }
        }

        private IEnumerator AutoSaveLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(gameSettings.autoSaveInterval);

                if (!string.IsNullOrEmpty(accountData.accountId))
                {
                    UpdateLastPlayedAt();
                    SaveAccount();
                    Debug.Log("[Account] 자동 저장 실행");
                }
            }
        }

        private void ApplyToSO(AccountSaveData saveData)
        {
            accountData.accountId = saveData.accountId;
            accountData.lastPlayedAt = saveData.lastPlayedAt;
            accountData.totalScore = saveData.totalScore;

            accountData.encyclopedia.Clear();
            if (saveData.encyclopedia != null)
                accountData.encyclopedia.AddRange(saveData.encyclopedia);
        }

        private AccountSaveData ExtractFromSO()
        {
            return new AccountSaveData
            {
                accountId = accountData.accountId,
                lastPlayedAt = accountData.lastPlayedAt,
                encyclopedia = new(accountData.encyclopedia),
                totalScore = accountData.totalScore
            };
        }

        private int CalculateScore(FishCatchData fishData)
        {
            int rarity = fishData.species != null ? fishData.species.rarity : 1;
            return Mathf.RoundToInt(fishData.weight * 10f * rarity);
        }

        private string GetSavePath(string accountId)
        {
            return Path.Combine(_saveFolderPath, $"{accountId}.json");
        }

        [ContextMenu("Debug: Load TestAccount")]
        private void DebugLoad() => LoadAccount("TestAccount");

        [ContextMenu("Debug: Save")]
        private void DebugSave() => SaveAccount();

        [ContextMenu("Debug: Add Dummy Fish")]
        private void DebugAddFish()
        {
            AddToEncyclopedia(new FishCatchData
            {
                species = null,
                weight = 2.5f,
                caughtAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                siteType = BackgroundType.Lake
            });
        }
    }
}
