using System;
using VirtualFishing.Data;

namespace VirtualFishing.Interfaces
{
    public interface IAccountService
    {
        void LoadAccount(string accountId);
        void SaveAccount();
        void UpdateLastPlayedAt();
        void AddToEncyclopedia(FishCatchData fishData);
        event Action OnAccountLoaded;
        event Action OnAccountSaved;
    }
}
