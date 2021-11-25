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
    // Private Server mgmt/access those client uses for Adminstrative and other operations
    public interface IPrivSrvMgmt
    {
        // Empty return string is success, anything else is errorMsg
        Task<string> UserLoginAsync(string newaddr = null);

        // Generic Pfs Private Server's property SET/GET functionality, if value defined its SET, and always does GET
        // [ENABLED]  = string (TRUE, FALSE)
        // [CONNECTED]  = string (TRUE, FALSE)
        // [REMOVE] = SET only, performs full factory reset for private server disabled & removed
        // [ADDRPORT] = string (empty/192.168.0.1:1973)
        string Property(string property, string value = null);

        Task<List<PrivSrvReportTrackedStocks>> ReportTrackedStocksAsync();

        Task<List<StockClosingData>> GetHistoryEODsAsync(Guid STID, DateTime from);

        Task<PrivSrvStockPerformanceData> GetPerformanceDataAsync(Guid STID);

        Task RemoveTrackedStockAsync(Guid STID);

        Task<bool> StockExtFetchData(Guid STID, DateTime start, DateTime end);

        Task<string> TestStockFetchAsync(Guid STID);

        Task<Dictionary<PrivSrvProperty, string>> SrvConfigPropertyGetAllAsync();
        Task<string> SrvConfigPropertySetAsync(PrivSrvProperty propertyID, string value);

        Task<SettMarketProviders> ProviderConfigsGetAsync();
        Task<bool> ProviderConfigsSetAsync(SettMarketProviders config);

        Task<List<PrivSrvUserInfo>> UserListGetAsync();

        Task UserUpdateAsync(PrivSrvUserInfo updates);

        Task<List<string>> BackupListAsync();

        Task<byte[]> BackupFetchAsync(string backupname);
    }
}
