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
    // Interface to be implemented outside of core PFS Library, to perform PFS Priv Srv API call's from Application
    public interface IPfsPrivSrvWebAPI
    {
        void SetAddrPort(string addrport);

        Task<(bool isAdmin, bool doBackup, string errorMsg)> UserLoginAsync(string username, Guid identityID, int stockAmount);

        // 
        Task<bool> UserBackupToServerAsync(Guid identityID, byte[] backupFileContent);

        Task<List<string>> UserBackupListAsync(Guid identityID);

        Task<byte[]> UserBackupFetchAsync(Guid identityID, string backupname);

        Task<Dictionary<Guid, StockClosingData>> GetLatestEODsAsync(Guid identityID, List<Guid> STIDs);

        Task<List<StockClosingData>> GetHistoryEODsAsync(Guid identityID, Guid STID, DateTime from);

        Task<Dictionary<Guid, StockIndicatorData>> GetLatestIndsAsync(Guid identityID, List<Guid> STIDs);

        Task<Dictionary<Guid, PrivSrvStockPerformanceData>> GetLatestPerformanceAsync(Guid identityID, List<Guid> STIDs);

        Task<bool> UserAddStockTrackingAsync(Guid identityID, Guid STID);

        Task UserRemoveStockTrackingAsync(Guid identityID, Guid STID);

        Task<List<PrivSrvReportTrackedStocks>> ReportServerTrackedStocksAsync(Guid identityID);

        Task RemoveServerTrackedStockAsync(Guid identityID, Guid STID);

        Task<bool> StockExtFetchData(Guid identityID, Guid STID, DateTime start, DateTime end);

        Task<string> ServerTestStockFetchAsync(Guid identityID, Guid STID);

        Task<Dictionary<PrivSrvProperty, string>> PropertyGetAllAsync(Guid identityID);
        Task<string> PropertySetAsync(Guid identityID, PrivSrvProperty propertyID, string value);

        Task<SettMarketProviders> ProviderConfigsGetAsync(Guid identityID);
        Task<bool> ProviderConfigsSetAsync(Guid identityID, SettMarketProviders config);

        Task<List<PrivSrvUserInfo>> UserListGetAsync(Guid identityID);
        Task<bool> UserUpdateAsync(Guid identityID, PrivSrvUserInfo updates);
    }
}
