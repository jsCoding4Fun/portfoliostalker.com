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

namespace PFS.Shared.Types
{
    // Prototype for any Ext Providers to access Market Data / History information. Do NOT let exceptions out!
    public interface IExtDataProvider
    {
        void SetPrivateKey(string key);

        string GetLastError();

        /* Decided to go separate functions to have query type API to know what specific Provider is capable for:
         * - What markets it can support
         * 
         */

        bool Support(MarketID marketID);

        // Allows to fetch some provider specific limits, like max ticker amount per job
        int Limit(LimitID limitID);

        public enum LimitID : int
        {
            Unknown = 0,
            LatestEodTickers,
            HistoryEodTickers,
            IntradayTickers,
        }

        /* FOLLOWING FUNCTIONS: 
         * - return Dictionary of data if partial/full success
         * - null if total failure or not supported
         * - Sets GetLastError() for full failure, and may set if for partial success as warning as log type string to inform what happend
         * - Caller can then compare tickers to returned tickers amounts or actual codes to see what success and what didnt
         * - Similarly caller can check ending dates of datas to see if expected data was received
         */

        //
        // 'EndOfDay', needs implementation of all of these
        //

        // Returns latest end-of-day closing data for specified stocks
        Task<Dictionary<string, StockClosingData>> GetEodLatestAsync(MarketMeta marketMeta, List<string> tickers);

        // Returns end-of-day data for specific period
        Task<Dictionary<string, List<StockClosingData>>> GetEodHistoryAsync(MarketMeta marketMeta, List<string> tickers, DateTime startDay, DateTime endDay);

        //
        // 'Intraday', implement if available, must also implement EOD parts
        //

        // Returns intraday, meaning currently active trading valuation / last trade information 
        Task<Dictionary<string, StockIntradayData>> GetIntradayAsync(MarketMeta marketMeta, List<string> tickers);

        //
        // 'Currency', implement if available, actually no dependency to other functions so can be currency only
        // 

        Task<decimal?> GetCurrencyLatestAsync(CurrencyCode fromCurrency, CurrencyCode toCurrency);

        Task<List<Tuple<DateTime, decimal>>> GetCurrencyHistoryAsync(CurrencyCode fromCurrency, CurrencyCode toCurrency, DateTime startDay, DateTime endDay);
    }
}
