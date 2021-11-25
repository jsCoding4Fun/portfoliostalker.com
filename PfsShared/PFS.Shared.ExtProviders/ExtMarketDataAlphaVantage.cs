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
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Net.Http;

using Serilog;              // Nuget: Serilog
using ServiceStack;         // Nuget: FromCsv()

using PFS.Shared.Types;

namespace PFS.Shared.ExtProviders
{
    public class ExtMarketDataAlphaVantage : IExtDataProvider
    {
        /* Promising, needs way more testing!
         * 
         *  => 5 API request per minute is SLOW to WASM!   (50$ to 75 request is per minute is nice sounding)
         *  => Single ticker on time makes things bit slower
         *  => 500 request per day is ok for Private Server (50$ to unlimited)
         *  => Has latest EOD (Quote Endpoint / GLOBAL_QUOTE)
         *  => Has history (TIME_SERIES_DAILY)              (Decades on single request! Perfection++)
         *  => Supports all/most markets for history!       (Requires special ticker format (PfsToAlphaTicker))  !!!UNTESTED!!! !!!LATER!!!
         *  
         *  => NASDAQ/NYSE/AMEX available hour after close (or maybe earlier)
         *  
         * SKIP:
         * 
         * - Has intraday but 500 req/day not worth w free  (real usage amounts require payed account, so postponed now)
         * 
         * TODO: (=> One of main providers to test well, as very potential Pfs Main Server provider if all goes fine)
         * 
         *  - Has Forex history: Per pairs like Euro-Dollar can get example weekly history! => Again main server specific! MUST TEST!
         *  
         *  - Has Search end-point, so this could be potential WhitelistStock -provider, but not urgent 
         * 
         */

        protected string _error = string.Empty;

        protected string _alphaVantageApiKey = "";

        public string GetLastError() { return _error; }

        public void SetPrivateKey(string key)
        {
            _alphaVantageApiKey = key;
        }

        protected DateTime _lastUseTime = DateTime.UtcNow;

        public bool Support(MarketID marketID)
        {
            switch ( marketID )
            {
                case MarketID.NASDAQ:
                case MarketID.NYSE:
                case MarketID.AMEX:
                case MarketID.TSX:
                    return true;
            }
            return false;               // Rumor is that if using exactly same format as yahoo financing could access maybe more markets....!!!LATER!!!
        }

        public int Limit(IExtDataProvider.LimitID limitID)
        {
            switch (limitID)
            {
                case IExtDataProvider.LimitID.LatestEodTickers:
                case IExtDataProvider.LimitID.HistoryEodTickers: return 1;

                case IExtDataProvider.LimitID.IntradayTickers: return 1;
            }
            Log.Warning("ExtMarketDataAlphaVantage():Limit({0}) missing implementation", limitID.ToString());
            return 1;
        }

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
            _error = "GetIntradayAsync() - Supports but not worth of it using Free account w 5 queries per min, and 500 max per day";
            return null;
        }
#if false // Yep, RSI works returns a lot of history also..
        public async Task TestRSIAsync()
        {
            HttpClient tempHttpClient = new HttpClient();

            HttpResponseMessage resp = await tempHttpClient.GetAsync($"https://www.alphavantage.co/query?function=RSI&symbol=IBM&interval=weekly&time_period=10&series_type=close&apikey={_alphaVantageApiKey}&datatype=csv");
        
            if (resp.IsSuccessStatusCode == false)
            {
                return;
            }

            var readAsStringAsync = resp.Content.ReadAsStringAsync();
            
            var dailyItem = readAsStringAsync.Result.FromCsv<List<AlphaVantageRsiFormat>>();
        }

        //,

        [DataContract]
        private class AlphaVantageRsiFormat
        {
            [DataMember] public DateTime time { get; set; }
            [DataMember] public decimal RSI { get; set; }
        }
#endif
        public async Task<Dictionary<string, StockClosingData>> GetEodLatestAsync(MarketMeta marketMeta, List<string> tickers)
        {
            _error = string.Empty;

            if (string.IsNullOrEmpty(_alphaVantageApiKey) == true)
            {
                _error = "ALPHAV::GetEodLatestAsync() Missing private access key!";
                return null;
            }

            if (tickers.Count != 1)
            {
                _error = "Failed, has 1 ticker on time limit!";
                Log.Warning("ALPHAV:GetEodLatestAsync() " + _error);
                return null;
            }

            string alphaTicker = PfsToAlphaTicker(marketMeta.ID, tickers[0]);

            await InternalDelayAwait();

            try
            {
                HttpClient tempHttpClient = new HttpClient();

                HttpResponseMessage resp = await tempHttpClient.GetAsync($"https://www.alphavantage.co/query?function=GLOBAL_QUOTE&symbol={alphaTicker}&apikey={_alphaVantageApiKey}&datatype=csv");

                if (resp.IsSuccessStatusCode == false)
                {
                    _error = string.Format("AlphaV failed: {0} for [[{1}]]", resp.StatusCode.ToString(), tickers[0]);
                    Log.Warning("ALPHAV::GetEodLatestAsync() " + _error);
                    return null;
                }

                string content = await resp.Content.ReadAsStringAsync();
                var dailyItem = content.FromCsv<List<AlphaVantageQuoteFormat>>();

                if (dailyItem == null || dailyItem.Count() != 1)
                {
                    _error = string.Format("Failed, empty data: {0} for [[{1}]]", resp.StatusCode.ToString(), tickers[0]);
                    Log.Warning("ALPHAV::GetEodLatestAsync() " + _error);
                    return null;
                }

                // All seams to be enough well so lets convert data to PFS format

                Dictionary<string, StockClosingData> ret = new Dictionary<string, StockClosingData>();

                ret[tickers[0]] = new StockClosingData()
                {
                    Date = new DateTime(dailyItem[0].latestDay.Year, dailyItem[0].latestDay.Month, dailyItem[0].latestDay.Day),
                    Close = dailyItem[0].price,
                    High = dailyItem[0].high,
                    Low = dailyItem[0].low,
                    Open = dailyItem[0].open,
                    PrevClose = dailyItem[0].previousClose,
                    Volume = dailyItem[0].volume,
                };

                return ret;
            }
            catch ( Exception e )
            {
                _error = string.Format("AlphaV connection exception {0} for [[{1}]]", e.Message, tickers[0]);
                Log.Warning("ALPHAV::GetEodLatestAsync() " + _error);
            }
            return null;
        }

        public async Task<Dictionary<string, List<StockClosingData>>> GetEodHistoryAsync(MarketMeta marketMeta, List<string> tickers, DateTime startDay, DateTime endDay)
        {
            _error = string.Empty;

            if (string.IsNullOrEmpty(_alphaVantageApiKey) == true)
            {
                _error = "ALPHAV::GetEodHistoryAsync() Missing private access key!";
                return null;
            }

            if (tickers.Count != 1)
            {
                _error = "Failed, has 1 ticker on time limit!";
                Log.Warning("ALPHAV:GetEodLatestAsync() " + _error);
                return null;
            }

            string alphaTicker = PfsToAlphaTicker(marketMeta.ID, tickers[0]);

            await InternalDelayAwait();

            // By default using same default as they, 100 last.. doesnt seam to effect cost
            string outputsize = "compact";

            if ((endDay - startDay).TotalDays > 50)
                // But even smallest risk of having too little data, we go long...
                outputsize = "full";

            try
            {
                HttpClient tempHttpClient = new HttpClient();

                // Default is compact, and limits to 100 last records (but if needs jumps to full history)
                HttpResponseMessage resp = await tempHttpClient.GetAsync($"https://www.alphavantage.co/query?function=TIME_SERIES_DAILY&symbol={alphaTicker}&apikey={_alphaVantageApiKey}&datatype=csv&outputsize={outputsize}");

                if (resp.IsSuccessStatusCode == false)
                {
                    _error = string.Format("Failed: {0} for [[{1}]]", resp.StatusCode.ToString(), tickers[0]);
                    Log.Warning("ALPHAV::GetEodHistoryAsync() " + _error);
                    return null;
                }

                string content = await resp.Content.ReadAsStringAsync();
                var stockRecords = content.FromCsv<List<AlphaVantageDailyFormat>>();

                if (stockRecords == null || stockRecords.Count() == 0)
                {
                    _error = string.Format("Failed, empty data: {0} for [[{1}]]", resp.StatusCode.ToString(), tickers[0]);
                    Log.Warning("ALPHAV::GetEodHistoryAsync() " + _error);
                    return null;
                }

                Dictionary<string, List<StockClosingData>> ret = new Dictionary<string, List<StockClosingData>>();

                List<StockClosingData> ext = stockRecords.ConvertAll(s => new StockClosingData()
                {
                    Date = new DateTime(s.timestamp.Year, s.timestamp.Month, s.timestamp.Day),
                    Close = s.close,
                    High = s.high,
                    Low = s.low,
                    Open = s.open,
                    PrevClose = -1,     // dont start pulling it here from previous if dont have field, let central place to do pulling from prev day
                    Volume = s.volume,
                });

                ext.Reverse();
                ret.Add(tickers[0], ext);

                return ret;
            }
            catch (Exception e)
            {
                _error = string.Format("Failed, connection exception {0} for [[{1}]]", e.Message, tickers[0]);
                Log.Warning("ALPHAV::GetEodHistoryAsync() " + _error);
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

        protected string PfsToAlphaTicker(MarketID marketID, string ticker)
        {
            switch (marketID)
            {
                case MarketID.TSX: return ticker + ".TRT";
            }
            return ticker;
        }

        /*
                symbol,open,high,low,price,volume,latestDay,previousClose,change,changePercent
                LUMN,13.9300,14.0900,13.8820,14.0500,5368898,2021-06-25,13.9500,0.1000,0.7168%"
         */
        [DataContract]
        private class AlphaVantageQuoteFormat
        {
            [DataMember] public string symbol { get; set; }
            [DataMember] public decimal open { get; set; }
            [DataMember] public decimal high { get; set; }
            [DataMember] public decimal low { get; set; }
            [DataMember] public decimal price { get; set; }
            [DataMember] public int volume { get; set; }
            [DataMember] public DateTime latestDay { get; set; }
            [DataMember] public decimal previousClose { get; set; }
            [DataMember] public decimal change { get; set; }
            [DataMember] public string changePercent { get; set; }
        }


        /*
            timestamp,open,high,low,close,volume
            2021-06-25,13.9300,14.0900,13.8820,14.0500,5368898
        */
        [DataContract]
        private class AlphaVantageDailyFormat
        {
            [DataMember] public DateTime timestamp { get; set; }
            [DataMember] public decimal open { get; set; }
            [DataMember] public decimal high { get; set; }
            [DataMember] public decimal low { get; set; }
            [DataMember] public decimal close { get; set; }
            [DataMember] public int volume { get; set; }
        }
    }
}
