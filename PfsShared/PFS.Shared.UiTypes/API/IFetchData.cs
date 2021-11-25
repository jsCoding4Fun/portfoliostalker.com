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
    // Bit of random collection of different Fetch & etc operations to access client's information
    public interface IFetchData
    {
        List<FetchTreeData> GetTreeData();

        List<MarketMeta> GetMarketMeta(bool configuredOnly = false);

        Task<string> TestStockFetchAsync(Guid STID);

        Task<string> TestStockFetchAsync(MarketID marketID, string ticker);

        Task<List<CompanyMeta>> SearchCompaniesAsync(MarketID marketID, string search);

        Task<(MarketID marketID, CompanyMeta companyMeta)> FindTickerAsync(string marketsToSearch, string ticker);

        Task<decimal?> GetCurrencyConversionAsync(CurrencyCode from, CurrencyCode to, DateTime date);

        // NEWS

        List<News> NewsGetList();

        void NewsChangeStatus(int newsID, NewsStatus status);

        // End-Of-Day data on Client

        LocalEodDataStatus GetLocalEodDataStatus();

        Task DoUpdateExpiredEodAsync();

        Task DoFetchNoDataEodAsync();

        Task DoFetchLatestIntradayAsync(string[] markets, string[] portfolios, ExtDataProviders provider);
    }

    // This is more than bit ugly structure, but its purpose is to solely pass information client of statistics of current stock eod expirations
    public class LocalEodDataStatus
    {
        public int TotalTrackedStocks { get; set; }         // Total stocks on account

        public int NoDataStocks { get; set; }               // How many dont have any data

        public int ExpiredStocks { get; set; }              // How many stocks have data thats older than latest closing of market

        public List<int> ExpiryMins { get; set; }           // For each 'ExpiredStocks' lists details of its expiry minute amount

        public bool UpdateOnGoing { get; set; }             // Local Fetch logic is busy atm, and cant accept requests currently
    }
}
