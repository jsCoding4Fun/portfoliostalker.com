/*
 * Copyright (c) 2021 Jami Suni
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Net.Http;

using Serilog;              // Nuget: Serilog
using ServiceStack;         // Nuget: FromCsv()

using PFS.Shared.Types;

namespace PFS.Shared.ExtProviders
{
    public class ExtMarketDataTiingo: IExtDataProvider
    {
        protected string _error = string.Empty;

        protected string _tiingoApiKey = "";

        public void SetPrivateKey(string key)
        {
            _tiingoApiKey = key;
        }

        public bool Support(MarketID marketID)
        {
            switch ( marketID )
            {
                case MarketID.NASDAQ:
                case MarketID.NYSE:
                case MarketID.AMEX:
                    return true;
            }
            return false;
        }

        public int Limit(IExtDataProvider.LimitID limitID)
        {
            switch (limitID)
            {
                case IExtDataProvider.LimitID.LatestEodTickers: return 1;

                case IExtDataProvider.LimitID.HistoryEodTickers: return 1;
            }
            Log.Warning("ExtMarketDataTiingo():Limit({0}) missing implementation", limitID.ToString());
            return 1;
        }

        /* Tiingo: (Still 2021-Nov-3st, doesnt work w WASM, even w https activated on developer studio testing)
         * 
         * - As of 2021-Jun.. forget TSX / Canadian stock market.. just focus US ones w Tiingo.. did try 'search' cant see their canadian tickers
         * 
         * - Has Intraday functionality, that seams to be working ok... except doesnt wanna work on WASM!
         * 
         * - Hour limit of max 500 queries, Month limit is max 500 unique tickers !very nice! and month unlimited is 10$ only!
         * 
         * => TODO! Would be pretty perfect Intraday tool, if just would work on WASM :/ Try again later! 10$ per month would not be bad 
         *      => But without Toronto, without WASM .. blaah.. anyway all nice as Free account even for US stocks so retry WASM later!!!TODO!!!
         * 
         * => No dividents 
         * 
         * => As of 2021-Aug setting this up as default US market PrivSrv provider, as able to fetch hour from closing NASDAQ/NYSE/AMEX
         * 
         * (can download list of supported_tickers:https://apimedia.tiingo.com/docs/tiingo/daily/supported_tickers.zip)
         */

#if false // Search 

                // THIS FORMAT WORKS!

                HttpResponseMessage respSearch = await tempHttpClient.GetAsync($"https://api.tiingo.com/tiingo/utilities/search/apple?token={_tiingoApiKey}&format=csv");

                if (respSearch.IsSuccessStatusCode == true)
                {
                    var searchRet = await respSearch.Content.ReadAsStringAsync();
                }

                // THIS DOESNT AS OF 19th Jun 2021

                HttpResponseMessage respSearch = await tempHttpClient.GetAsync($"https://api.tiingo.com/tiingo/utilities/search?query=apple?token={_tiingoApiKey}&format=csv");

                if (respSearch.IsSuccessStatusCode == true)
                {
                    var searchRet = await respSearch.Content.ReadAsStringAsync();
                }
#endif

#if false // INTRADAY !


        // ? https://api.tiingo.com/documentation/iex => https://api.tiingo.com/iex/<ticker>

        
                HttpResponseMessage respIntra = await tempHttpClient.GetAsync($"https://api.tiingo.com/iex/{tickers[0]}?token={_tiingoApiKey}&format=csv");

                if (respIntra.IsSuccessStatusCode == true)
                {
                    var searchRet = await respIntra.Content.ReadAsStringAsync();
                }



ticker,askPrice,askSize,bidPrice,bidSize,high,last,lastSize,lastSaleTimestamp,low,mid,open,prevClose,quoteTimestamp,timestamp,tngoLast,volume
MSFT,261.080000,300,261.060000,126,262.290000,261.080000,100,2021-06-18T15:45:51.552978927-04:00,258.850000,261.070000,260.580000,260.900000,2021-06-18T15:45:52.823840008-04:00,2021-06-18T15:45:52.823840008-04:00,261.070000,604042

#endif

        public string GetLastError() { return _error; }

        public Task<Dictionary<string, StockIntradayData>> GetIntradayAsync(MarketMeta marketMeta, List<string> tickers)
        {
            _error = "GetIntradayAsync() - Not supported";
            return null;
        }

        public async Task<Dictionary<string, StockClosingData>> GetEodLatestAsync(MarketMeta marketMeta, List<string> tickers)
        {
            _error = string.Empty;

            if (string.IsNullOrEmpty(_tiingoApiKey) == true)
            {
                _error = "Tiingo:GetEodLatestAsync() Missing private access key!";
                return null;
            };

            if (tickers.Count != 1)
            {
                _error = "Failed, has 1 ticker on time limit!";
                Log.Warning("Tiingo:GetEodLatestAsync() " + _error);
                return null;
            }

            try
            {
                HttpClient tempHttpClient = new HttpClient();

                // Doesnt work with WASM, just doesnt wanna do it...
                HttpResponseMessage resp = await tempHttpClient.GetAsync($"https://api.tiingo.com/tiingo/daily/{tickers[0]}/prices?token={_tiingoApiKey}&format=csv");

                if (resp.IsSuccessStatusCode == false)
                {
                    _error = string.Format("Failed: {0} for [[{1}]]", resp.StatusCode.ToString(), tickers[0]);
                    Log.Warning("Tiingo:GetEodLatestAsync() " + _error);
                    return null;
                }

                string content = await resp.Content.ReadAsStringAsync();
                content = "garbage" + content;                          // As 2021-Nov-3th, this is valid as comes with ",field,field.." on header 
                var dailyItems = content.FromCsv<List<TiingoDailyLatestFormat>>();

                if (dailyItems == null || dailyItems.Count != 1)
                {
                    _error = string.Format("Failed, empty data: {0} for [[{1}]]", resp.StatusCode.ToString(), tickers[0]);
                    Log.Warning("Tiingo:GetEodLatestAsync() " + _error);
                    return null;
                }

                Dictionary<string, StockClosingData> ret = new Dictionary<string, StockClosingData>();

                ret[tickers[0]] = new StockClosingData()
                {
                    Date = new DateTime(dailyItems[0].date.Year, dailyItems[0].date.Month, dailyItems[0].date.Day),
                    Close = dailyItems[0].close,
                    High = dailyItems[0].high,
                    Low = dailyItems[0].low,
                    Open = dailyItems[0].open,
                    PrevClose = -1,
                    Volume = dailyItems[0].volume,
                };

                return ret;
            }
            catch ( Exception e )
            {
                _error = string.Format("Failed, connection exception {0} for [[{1}]]", e.Message, tickers[0]);
                Log.Warning("Tiingo:GetEodLatestAsync() " + _error);
            }
            return null;
        }

        public async Task<Dictionary<string, List<StockClosingData>>> GetEodHistoryAsync(MarketMeta marketMeta, List<string> tickers, DateTime startDay, DateTime endDay)
        {
            _error = string.Empty;

            if (string.IsNullOrEmpty(_tiingoApiKey) == true)
            {
                _error = "Tiingo:GetEodHistoryAsync() Missing private access key!";
                return null;
            }
            if (tickers.Count != 1)
            {
                _error = "Filed, has 1 ticker on time limit!";
                Log.Warning("Tiingo:GetEodLatestAsync() " + _error);
                return null;
            }

            string start = startDay.ToString("yyyy-MM-dd");
            string end = endDay.ToString("yyyy-MM-dd");

            try
            {
                HttpClient tempHttpClient = new HttpClient();
                HttpResponseMessage resp = await tempHttpClient.GetAsync($"https://api.tiingo.com/tiingo/daily/{tickers[0]}/prices?startDate={start}&endDate={end}&format=csv&resampleFreq=daily&token={_tiingoApiKey}");

                if (resp.IsSuccessStatusCode == false)
                {
                    _error = string.Format("Failed: {0} for [[{1}]]", resp.StatusCode.ToString(), tickers[0]);
                    Log.Warning("Tiingo:GetEodHistoryAsync() " + _error);
                    return null;
                }

                string content = await resp.Content.ReadAsStringAsync();
                var stockRecords = content.FromCsv<List<TiingoHistoryFormat>>();

                if (stockRecords == null || stockRecords.Count == 0)
                {
                    _error = string.Format("Failed, empty data: {0} for [[{1}]]", resp.StatusCode.ToString(), tickers[0]);
                    Log.Warning("Tiingo:GetEodHistoryAsync() " + _error);
                    return null;
                }

                Dictionary<string, List<StockClosingData>> ret = new Dictionary<string, List<StockClosingData>>();

                List<StockClosingData> ext = stockRecords.ConvertAll(s => new StockClosingData()
                {
                    Date = s.date,
                    Close = s.close,
                    High = s.high,
                    Low = s.low,
                    Open = s.open,
                    PrevClose = -1,
                    Volume = s.volume,
                });

                ret.Add(tickers[0], ext);

                return ret;
            }
            catch (Exception e)
            {
                _error = string.Format("Failed, connection exception {0} for [[{1}]]", e.Message, tickers[0]);
                Log.Warning("Tiingo:GetEodLatestAsync() " + _error);
            }
            return null;
        }


        public Task<decimal?> GetCurrencyLatestAsync(CurrencyCode fromCurrency, CurrencyCode toCurrency)
        {
            // Not supported
            return null;
        }

        public Task<List<Tuple<DateTime, decimal>>> GetCurrencyHistoryAsync(CurrencyCode fromCurrency, CurrencyCode toCurrency, DateTime startDay, DateTime endDay)
        {
            // Not supported
            return null;
        }

        /*
        ,adjClose,adjHigh,adjLow,adjOpen,adjVolume,close,date,divCash,high,low,open,splitFactor,volume
        0,17.0,17.16,16.91,16.99,17613186,17.0,2021-04-27T00:00:00+00:00,0.0,17.16,16.91,16.99,1.0,17613186
         */

        [DataContract]
        private class TiingoDailyLatestFormat
        {
            [DataMember] public decimal garbage { get; set; }
            [DataMember] public decimal adjClose { get; set; }
            [DataMember] public decimal adjHigh { get; set; }
            [DataMember] public decimal adjLow { get; set; }
            [DataMember] public decimal adjOpen { get; set; }
            [DataMember] public int adjVolume { get; set; }
            [DataMember] public decimal close { get; set; }
            [DataMember] public DateTime date { get; set; }
            [DataMember] public decimal divCash { get; set; }
            [DataMember] public decimal high { get; set; }
            [DataMember] public decimal low { get; set; }
            [DataMember] public decimal open { get; set; }
            [DataMember] public decimal splitFactor { get; set; }
            [DataMember] public int volume { get; set; }
        }

        /*
        date,close,high,low,open,volume,adjClose,adjHigh,adjLow,adjOpen,adjVolume,divCash,splitFactor
        2021-04-01,16.84,16.84,16.49,16.66,13316794,16.84,16.84,16.49,16.66,13316794,0.0,1.0
        */

        [DataContract]
        private class TiingoHistoryFormat
        {
            [DataMember] public DateTime date { get; set; }
            [DataMember] public decimal close { get; set; }
            [DataMember] public decimal high { get; set; }
            [DataMember] public decimal low { get; set; }
            [DataMember] public decimal open { get; set; }
            [DataMember] public int volume { get; set; }
            [DataMember] public decimal adjClose { get; set; }
            [DataMember] public decimal adjHigh { get; set; }
            [DataMember] public decimal adjLow { get; set; }
            [DataMember] public decimal adjOpen { get; set; }
            [DataMember] public int adjVolume { get; set; }
            [DataMember] public decimal divCash { get; set; }
            [DataMember]public decimal splitFactor { get; set; }
        }
    }
}
