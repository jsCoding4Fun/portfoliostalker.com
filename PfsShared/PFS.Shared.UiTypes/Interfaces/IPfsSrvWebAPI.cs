/*
 * Copyright (c) 2021 Jami Suni
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using PFS.Shared.Types;

namespace PFS.Shared.UiTypes
{
    // Interface to be implemented outside of PFS Library, to perform PFS Account/Meta Srv API call's for Appl/PrivSrv
    public interface IPfsSrvWebAPI
    {
        string GetAccountAddrPort();
        void SetAccountAddrPort(string accountAddrPort);

        string GetMetaAddrPort();
        void SetMetaAddrPort(string metaAddrPort);

        string GetBackupsAddrPort();
        void SetBackupsAddrPort(string backupsAddrPort);

        // Returns full list of markets those PFS system supports
        Task<List<MarketMeta>> GetMarketMetaAsync();

        // Allows to search company per ticker/name from specific market
        Task<List<CompanyMeta>> SearchCompaniesAsync(Guid sessionID, MarketID marketID, string search, int max = 100);

        Task<(MarketID marketID, CompanyMeta companyMeta)> FindTickerAsync(Guid sessionID, string marketsToSearch, string ticker);

        // Fetch PFS Stock Meta information (including STID) for specific exact market+ticker
        Task<StockMeta> GetStockMetaAsync(Guid sessionID, MarketID marketID, string ticker);

        // Note! This is without SessionID checking, as its one of rare functions allowed for 3th party un-authenticated PrivSrv to use
        Task<StockMeta> GetStockMetaAsync(Guid STID);

        Task<CompanyMeta> GetCompanyMetaAsync(Guid sessionID, MarketID marketID, string ticker);

        // 
        Task<(AccountTypeID typeID, Guid sessionID, Guid identityID, List<News> news, Dictionary<string, string> properties, string errorMsg)>
            UserLoginAsync(string usernameHash, string passwordHash, int? newsID, string clientVersionNumber);

        // Creates new account assuming username is unique
        Task<bool> UserRegisterAsync(string usernameHash, string passwordHash, string emailHash);

        Task<bool> UserChangePasswordAsync(Guid sessionID, string usernameHash, string oldPasswordHash, string newPasswordHash);

        Task<bool> UserChangeEmailAsync(Guid sessionID, string usernameHash, string passwordHash, string newEmailHash);

        Task<bool> UserSetPropertyAsync(Guid sessionID, string property, string value);

        // Backup/Fetch user's full account content to PFS server's
        Task<bool> BackupStoreFullAsync(Guid sessionID, byte[] backupFileContent);
        Task<(byte[] content, DateTime backupDateUTC)> BackupFetchFullAsync(Guid sessionID);
    }
}
