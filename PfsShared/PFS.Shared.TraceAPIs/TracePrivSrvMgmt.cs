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
using PFS.Shared.UiTypes;

namespace PFS.Shared.TraceAPIs
{
    public class TracePrivSrvMgmt : IPrivSrvMgmt
    {
        public event EventHandler<string> ParsingEvent;

        protected IPrivSrvMgmt m_forward;
        protected TraceSymbols m_symbols;

        public TracePrivSrvMgmt(ref IPrivSrvMgmt forward, ref TraceSymbols symbols)
        {
            m_forward = forward;
            m_symbols = symbols;
        }

        public string Property(string property, string value = null)
        {
            string ret = m_forward.Property(property, value);

            string line = string.Format("!P Property prop={0} value={1}", property, value == null ? "NULL" : value);

            line += Environment.NewLine + "^ ret:" + ret.ToString();

            ParsingEvent?.Invoke(this, line);

            return ret;
        }

        public async Task<string> UserLoginAsync(string newaddr = null)
        {
            string ret = await m_forward.UserLoginAsync(newaddr);

            string line = string.Format("!P UserLoginAsync addr={0}", newaddr == null ? "default" : newaddr);

            line += Environment.NewLine + "^ ret:" + ret;

            ParsingEvent?.Invoke(this, line);

            return ret;
        }

        public async Task<List<StockClosingData>> GetHistoryEODsAsync(Guid STID, DateTime from)
        {
            List<StockClosingData> ret = await m_forward.GetHistoryEODsAsync(STID, from);

            string line = string.Format("!P GetHistoryEODsAsync");

            // !!!LATER!!! Missing data output

            ParsingEvent?.Invoke(this, line);

            return ret;
        }

        public async Task<PrivSrvStockPerformanceData> GetPerformanceDataAsync(Guid STID)
        {
            PrivSrvStockPerformanceData ret = await m_forward.GetPerformanceDataAsync(STID);

            string line = string.Format("!P GetPerformanceDataAsync");

            // !!!LATER!!! Missing data output

            ParsingEvent?.Invoke(this, line);

            return ret;
        }

        public async Task<List<PrivSrvReportTrackedStocks>> ReportTrackedStocksAsync()
        {
            List<PrivSrvReportTrackedStocks> ret = await m_forward.ReportTrackedStocksAsync();

            string line = string.Format("!P ReportTrackedStocksAsync");

            // !!!LATER!!! Missing data output

            ParsingEvent?.Invoke(this, line);

            return ret;
        }

        public async Task RemoveTrackedStockAsync(Guid STID)
        {
            await m_forward.RemoveTrackedStockAsync(STID);

            string line = string.Format("!P RemoveTrackedStockAsync {0}", m_symbols.GetSymbol(STID.ToString()));

            ParsingEvent?.Invoke(this, line);
        }

        public async Task<bool> StockExtFetchData(Guid STID, DateTime start, DateTime end)
        {
            bool ret = await m_forward.StockExtFetchData(STID, start, end);

            string line = string.Format("!P StockExtFetchData {0} {1} {2}", m_symbols.GetSymbol(STID.ToString()), start.ToString("yyyy-MM-dd"), end.ToString("yyyy-MM-dd"));

            line += Environment.NewLine + "^ ret:" + ret.ToString();

            ParsingEvent?.Invoke(this, line);

            return ret;
        }

        public async Task<string> TestStockFetchAsync(Guid STID)
        {
            string ret = await m_forward.TestStockFetchAsync(STID);

            string line = string.Format("!P TestStockFetchAsync {0}", m_symbols.GetSymbol(STID.ToString()));

            line += Environment.NewLine + "^ ret:" + ret.ToString();

            ParsingEvent?.Invoke(this, line);

            return ret;
        }

        public async Task<Dictionary<PrivSrvProperty, string>> SrvConfigPropertyGetAllAsync()
        {
            Dictionary<PrivSrvProperty, string> ret = await m_forward.SrvConfigPropertyGetAllAsync();

            string line = string.Format("!P SrvConfigPropertyGetAllAsync");

            // !!!LATER!!! Missing data output

            ParsingEvent?.Invoke(this, line);

            return ret;
        }

        public async Task<string> SrvConfigPropertySetAsync(PrivSrvProperty propertyID, string value)
        {
            string ret = await m_forward.SrvConfigPropertySetAsync(propertyID, value);

            string line = string.Format("!P SrvConfigPropertySetAsync !!!LATER!!!");

            // line += Environment.NewLine + "^ ret:" + ret.ToString();

            ParsingEvent?.Invoke(this, line);

            return ret;
        }

        public async Task<SettMarketProviders> ProviderConfigsGetAsync()
        {
            SettMarketProviders ret = await m_forward.ProviderConfigsGetAsync();

            string line = string.Format("!P ProviderConfigsGetAsync");

            // !!!LATER!!! Missing data output

            ParsingEvent?.Invoke(this, line);

            return ret;
        }

        public async Task<bool> ProviderConfigsSetAsync(SettMarketProviders config)
        {
            bool ret = await m_forward.ProviderConfigsSetAsync(config);

            string line = string.Format("!P ProviderConfigsSetAsync !!!LATER!!!");

            line += Environment.NewLine + "^ ret:" + ret.ToString();

            ParsingEvent?.Invoke(this, line);

            return ret;
        }

        public async Task<List<PrivSrvUserInfo>> UserListGetAsync()
        {
            List<PrivSrvUserInfo> ret = await m_forward.UserListGetAsync();

            string line = string.Format("!P UserListGetAsync");

            // !!!LATER!!! Missing data output

            ParsingEvent?.Invoke(this, line);

            return ret;
        }
        
        public async Task UserUpdateAsync(PrivSrvUserInfo updates)
        {
            await m_forward.UserUpdateAsync(updates);

            string line = string.Format("!P UserUpdateAsync !!!LATER!!!");

            ParsingEvent?.Invoke(this, line);
        }

        public async Task<List<string>> BackupListAsync()
        {
            List<string> ret = await m_forward.BackupListAsync();

            string line = string.Format("!P BackupListAsync !!!LATER!!!");

            ParsingEvent?.Invoke(this, line);

            return ret;
        }

        public async Task<byte[]> BackupFetchAsync(string backupname)
        {
            byte[] ret = await m_forward.BackupFetchAsync(backupname);

            string line = string.Format("!P BackupFetchAsync !!!LATER!!!");

            ParsingEvent?.Invoke(this, line);

            return ret;
        }
    }
}
