/*
 * Copyright (c) 2021 Jami Suni
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;

using Serilog;              // Nuget: Serilog
using ServiceStack;         // Nuget: FromCsv()

using PFS.Shared.Types;

namespace PFS.Shared.ExtProviders
{
    public class ExtMarketDataPolygon : IExtDataProvider
    {
        /* - Only USA markets!, has currency support ex EUR -> US for Latest & History
         * - 5 calls per minute limit, one ticker at time... but no other limits
         * - Works also WASM, but sure slow w 5 tickers per minute updating
         * 
         *  => Yes, very potential looking!
         * 
         * TRY OUT:
         * 
         * - Good looking API to get divident history, w same 5 querys a minute limit! Nice!
         *      => Show history, shows next divident.. all nice.. but slow for Local mode w 1 ticker per req and 5 per minute...
         * 
         * - Get upcoming market holidays and their open/close times
         * 
         * - Get the current trading status of the exchanges and overall financial markets. (tells if open atm etc)
         * 
         * THINK:
         * 
         * - NICE! !!!LATER!!! /v2/aggs/grouped/locale/us/market/stocks/{date} 
         *   => returns each and every dang stock for that day... => this could solve lot of limitations for freebie users
         */

        protected string _error = string.Empty;

        protected string _polygonApiKey = "";

        public string GetLastError() { return _error; }

        public void SetPrivateKey(string key)
        {
            _polygonApiKey = key;
        }

        public bool Support(MarketID marketID)
        {
            switch (marketID)
            {
                case MarketID.NASDAQ:
                case MarketID.NYSE:
                case MarketID.AMEX:
                    return true;        // Actually would support total 16 or so US only markets
            }
            return false;               // Nothing outside of USA is supported
        }

        public int Limit(IExtDataProvider.LimitID limitID)
        {
            switch (limitID)
            {
                case IExtDataProvider.LimitID.LatestEodTickers: return 1;

                case IExtDataProvider.LimitID.HistoryEodTickers: return 1;
            }
            Log.Warning("ExtMarketDataPolygon():Limit({0}) missing implementation", limitID.ToString());
            return 1;
        }

        protected DateTime _lastUseTime = DateTime.UtcNow.AddMinutes(-1);

        protected async Task InternalDelayAwait()
        {
            int seconds = 0;

            if (DateTime.UtcNow < _lastUseTime.AddSeconds(15))
            {
                seconds = (int)(_lastUseTime.AddSeconds(15) - DateTime.UtcNow).TotalSeconds;

                if (seconds == 0)
                    return;

                if (seconds > 15)
                    seconds = 15;

                await Task.Delay(seconds * 1000); // As a delay this works, but !!!TODO!!! How about part of actual code, should not freeze or lock?
            }
            _lastUseTime = DateTime.UtcNow;
        }

        public Task<Dictionary<string, StockIntradayData>> GetIntradayAsync(MarketMeta marketMeta, List<string> tickers)
        {
            _error = "GetIntradayAsync() - Not supported!";
            return null;
        }

        public async Task<Dictionary<string, StockClosingData>> GetEodLatestAsync(MarketMeta marketMeta, List<string> tickers)
        {
            _error = string.Empty;

            if (string.IsNullOrEmpty(_polygonApiKey) == true)
            {
                _error = "Polygon::GetEodLatestAsync() Missing private access key!";
                return null;
            }

            if (tickers.Count != 1)
            {
                _error = "Failed, has 1 ticker on time limit!";
                Log.Warning("Polygon::GetEodLatestAsync() " + _error);
                return null;
            }

            await InternalDelayAwait();

            try
            {
                HttpClient tempHttpClient = new HttpClient();
                tempHttpClient.Timeout = TimeSpan.FromSeconds(20);

#if false
                // open-close requires date, and we do not atm bring expected date here... pretty easily could, but atm dont...
                //HttpResponseMessage resp = await tempHttpClient.GetAsync($"https://api.polygon.io/v1/open-close/{tickers[0]}/2021-07-12?adjusted=true&apiKey={_polygonApiKey}");
                
        [DataContract]
        private class PolygonLatestEodFormat
        {
            [DataMember] public string status { get; set; }
            [DataMember] public string from { get; set; }
            [DataMember] public string symbol { get; set; }
            [DataMember] public decimal open { get; set; }
            [DataMember] public decimal high { get; set; }
            [DataMember] public decimal low { get; set; }
            [DataMember] public decimal close { get; set; }
            [DataMember] public int volume { get; set; }
            [DataMember] public decimal afterHours { get; set; }
            [DataMember] public decimal preMarket { get; set; }
        }

#endif
                // Yes, as of 2021-Jul this works OK!
                HttpResponseMessage resp = await tempHttpClient.GetAsync($"https://api.polygon.io/v2/aggs/ticker/{tickers[0]}/prev?adjusted=true&apiKey={_polygonApiKey}");

                if (resp.IsSuccessStatusCode == false)
                {
                    _error = string.Format("Polygon failed: {0} for [[{1}]]", resp.StatusCode.ToString(), tickers[0]);
                    Log.Warning("Polygon::GetEodLatestAsync() " + _error);
                    return null;
                }

                string content = await resp.Content.ReadAsStringAsync();
                var dailyItem = content.FromJson<PolygonLatestEodRoot>();

                if (dailyItem == null || dailyItem.results == null || dailyItem.results.Count() != 1)
                {
                    _error = string.Format("Failed, empty data: {0} for [[{1}]]", resp.StatusCode.ToString(), tickers[0]);
                    Log.Warning("Polygon::GetEodLatestAsync() " + _error);
                    return null;
                }

                // All seams to be enough well so lets convert data to PFS format

                Dictionary<string, StockClosingData> ret = new Dictionary<string, StockClosingData>();

                ret[tickers[0]] = new StockClosingData()
                {
                    Date = DateTime.UnixEpoch.AddMilliseconds(dailyItem.results[0].t),
                    Close = dailyItem.results[0].c,
                    High = dailyItem.results[0].h,
                    Low = dailyItem.results[0].l,
                    Open = dailyItem.results[0].o,
                    PrevClose = -1,
                    Volume = (int)(dailyItem.results[0].v),
                };

                return ret;
            }
            catch ( Exception e )
            {
                _error = string.Format("Polygon connection exception {0} for [[{1}]]", e.Message, tickers[0]);
                Log.Warning("Polygon::GetEodLatestAsync() " + _error);
            }
            return null;
        }

        public async Task<Dictionary<string, List<StockClosingData>>> GetEodHistoryAsync(MarketMeta marketMeta, List<string> tickers, DateTime startDay, DateTime endDay)
        {
            _error = string.Empty;

            if (string.IsNullOrEmpty(_polygonApiKey) == true)
            {
                _error = "Polygon::GetEodHistoryAsync() Missing private access key!";
                return null;
            }

            if (tickers.Count != 1)
            {
                _error = "Failed, has 1 ticker on time limit!";
                Log.Warning("Polygon::GetEodLatestAsync() " + _error);
                return null;
            }

            string start = startDay.ToString("yyyy-MM-dd");
            string end = endDay.ToString("yyyy-MM-dd");

            await InternalDelayAwait();

            try
            {
                HttpClient tempHttpClient = new HttpClient();
                HttpResponseMessage resp = await tempHttpClient.GetAsync($"https://api.polygon.io/v2/aggs/ticker/{tickers[0]}/range/1/day/{start}/{end}?adjusted=true&sort=asc&limit=1000&apiKey=4IQkzSIMp7wRvbqPbvnoK91Tu0QZiE3n");

                if (resp.IsSuccessStatusCode == false)
                {
                    _error = string.Format("Failed: {0} for [[{1}]]", resp.StatusCode.ToString(), tickers[0]);
                    Log.Warning("Polygon::GetEodHistoryAsync() " + _error);
                    return null;
                }

                string content = await resp.Content.ReadAsStringAsync();
                var polygonResp = content.FromJson<PolygonHistoryEodRoot>();

                if (polygonResp == null || polygonResp.results == null || polygonResp.results.Count() == 0)
                {
                    _error = string.Format("Failed, empty data: {0} for [[{1}]]", resp.StatusCode.ToString(), tickers[0]);
                    Log.Warning("Polygon::GetEodHistoryAsync() " + _error);
                    return null;
                }

                Dictionary<string, List<StockClosingData>> ret = new Dictionary<string, List<StockClosingData>>();

                List<StockClosingData> ext = polygonResp.results.ConvertAll(s => new StockClosingData()
                {
                    Date = DateTime.UnixEpoch.AddMilliseconds(s.t),
                    Close = s.c,
                    High = s.h,
                    Low = s.l,
                    Open = s.o,
                    PrevClose = -1,
                    Volume = (int)(s.v),
                });

                //ext.Reverse();
                ret.Add(tickers[0], ext);

                return ret;
            }
            catch (Exception e)
            {
                _error = string.Format("Failed, connection exception {0} for [[{1}]]", e.Message, tickers[0]);
                Log.Warning("Polygon::GetEodHistoryAsync() " + _error);
            }
            return null;
        }

        public async Task<decimal?> GetCurrencyLatestAsync(CurrencyCode fromCurrency, CurrencyCode toCurrency)
        {
            _error = string.Empty;

            if (string.IsNullOrEmpty(_polygonApiKey) == true)
            {
                _error = "Polygon::GetCurrencyLatestAsync() Missing private access key!";
                return null;
            }

            await InternalDelayAwait();

            string combo = string.Format("{0}{1}", fromCurrency.ToString(), toCurrency.ToString());

            try
            {
                HttpClient tempHttpClient = new HttpClient();
                tempHttpClient.Timeout = TimeSpan.FromSeconds(20);

                HttpResponseMessage resp = await tempHttpClient.GetAsync($"https://api.polygon.io/v2/aggs/ticker/C:{combo}/prev?adjusted=true&apiKey={_polygonApiKey}");

                if (resp.IsSuccessStatusCode == false)
                {
                    _error = string.Format("Polygon failed: {0} for [[{1}]]", resp.StatusCode.ToString(), combo);
                    Log.Warning("Polygon::GetCurrencyLatestAsync() " + _error);
                    return null;
                }

                string content = await resp.Content.ReadAsStringAsync();
                var polygonCurrency = content.FromJson<PolygonLatestCurrencyRoot>();

                if (polygonCurrency == null || polygonCurrency.results == null || polygonCurrency.results.Count() != 1)
                {
                    _error = string.Format("Failed, empty data: {0} for [[{1}]]", resp.StatusCode.ToString(), combo);
                    Log.Warning("Polygon::GetCurrencyLatestAsync() " + _error);
                    return null;
                }

                // All seams to be enough well so lets convert data to PFS format

                return polygonCurrency.results[0].c;
            }
            catch (Exception e)
            {
                _error = string.Format("Polygon connection exception {0} for [[{1}]]", e.Message, combo);
                Log.Warning("Polygon::GetCurrencyLatestAsync() " + _error);
            }
            return null;
        }

        public async Task<List<Tuple<DateTime, decimal>>> GetCurrencyHistoryAsync(CurrencyCode fromCurrency, CurrencyCode toCurrency, DateTime startDay, DateTime endDay)
        {
            _error = string.Empty;

            if (string.IsNullOrEmpty(_polygonApiKey) == true)
            {
                _error = "Polygon::GetCurrencyHistoryAsync() Missing private access key!";
                return null;
            }

            await InternalDelayAwait();

            string combo = string.Format("{0}{1}", fromCurrency.ToString(), toCurrency.ToString());

            string start = startDay.ToString("yyyy-MM-dd");
            string end = endDay.ToString("yyyy-MM-dd");

            try
            {
                HttpClient tempHttpClient = new HttpClient();
                tempHttpClient.Timeout = TimeSpan.FromSeconds(20);

                HttpResponseMessage resp = await tempHttpClient.GetAsync($"https://api.polygon.io/v2/aggs/ticker/C:{combo}/range/1/day/{start}/{end}?adjusted=true&apiKey={_polygonApiKey}");

                if (resp.IsSuccessStatusCode == false)
                {
                    _error = string.Format("Polygon failed: {0} for [[{1}]]", resp.StatusCode.ToString(), combo);
                    Log.Warning("Polygon::GetCurrencyHistoryAsync() " + _error);
                    return null;
                }

                string content = await resp.Content.ReadAsStringAsync();
                var polygonCurrency = content.FromJson<PolygonLatestCurrencyRoot>();

                if (polygonCurrency == null || polygonCurrency.results == null || polygonCurrency.results.Count() == 0)
                {
                    _error = string.Format("Failed, empty data: {0} for [[{1}]]", resp.StatusCode.ToString(), combo);
                    Log.Warning("Polygon::GetCurrencyHistoryAsync() " + _error);
                    return null;
                }

                return polygonCurrency.results.ConvertAll(c => new Tuple<DateTime, decimal>(DateTime.UnixEpoch.AddMilliseconds(c.t), c.c));
            }
            catch (Exception e)
            {
                _error = string.Format("Polygon connection exception {0} for [[{1}]]", e.Message, combo);
                Log.Warning("Polygon::GetCurrencyHistoryAsync() " + _error);
            }
            return null;
        }

        public class PolygonLatestEodResult
        {
            public string T { get; set; }
            public decimal v { get; set; }
            public decimal vw { get; set; }
            public decimal o { get; set; }
            public decimal c { get; set; }
            public decimal h { get; set; }
            public decimal l { get; set; }
            public long t { get; set; }
            public int n { get; set; }
        }

        public class PolygonLatestEodRoot
        {
            public string ticker { get; set; }
            public int queryCount { get; set; }
            public int resultsCount { get; set; }
            public bool adjusted { get; set; }
            public List<PolygonLatestEodResult> results { get; set; }
            public string status { get; set; }
            public string request_id { get; set; }
            public int count { get; set; }
        }

        public class PolygonHistoryEodResult
        {
            public decimal v { get; set; }
            public decimal vw { get; set; }
            public decimal o { get; set; }
            public decimal c { get; set; }
            public decimal h { get; set; }
            public decimal l { get; set; }
            public long t { get; set; }
            public int n { get; set; }
        }

        public class PolygonHistoryEodRoot
        {
            public string ticker { get; set; }
            public int queryCount { get; set; }
            public int resultsCount { get; set; }
            public bool adjusted { get; set; }
            public List<PolygonHistoryEodResult> results { get; set; }
            public string status { get; set; }
            public string request_id { get; set; }
            public int count { get; set; }
        }

        public class PolygonLatestCurrencyResult
        {
            public string T { get; set; }
            public int v { get; set; }
            public decimal o { get; set; }
            public decimal c { get; set; }
            public decimal h { get; set; }
            public decimal l { get; set; }
            public long t { get; set; }
            public int n { get; set; }
        }

        public class PolygonLatestCurrencyRoot
        {
            public string ticker { get; set; }
            public int queryCount { get; set; }
            public int resultsCount { get; set; }
            public bool adjusted { get; set; }
            public List<PolygonLatestCurrencyResult> results { get; set; }
            public string status { get; set; }
            public string request_id { get; set; }
            public int count { get; set; }
        }

    }
}
