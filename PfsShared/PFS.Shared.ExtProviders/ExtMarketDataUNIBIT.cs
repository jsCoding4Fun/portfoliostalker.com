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
    // Unibit provides market data for all main markets
    public class ExtMarketDataUNIBIT : IExtDataProvider
    {
        protected string _error = string.Empty;

        protected string _unibitApiKey = "";

        // https://unibit.ai/api/docs/V2.0/historical_stock_price
        // => This API is updated every day no later than 2 hours after the market closes in local time.

        public string GetLastError() { return _error; }

        public void SetPrivateKey(string key)
        {
            _unibitApiKey = key;
        }

        public bool Support(MarketID marketID)
        {
            switch (marketID)
            {
                case MarketID.NASDAQ:
                case MarketID.NYSE:
                case MarketID.AMEX:
                case MarketID.TSX:
                case MarketID.OMXH:
                    return true;
            }
            return false;
        }

        const int _maxTickers = 50; // 50 is API defined maximum, so running full speed...

        public int Limit(IExtDataProvider.LimitID limitID)
        {
            switch (limitID)
            {
                case IExtDataProvider.LimitID.LatestEodTickers: return _maxTickers;

                case IExtDataProvider.LimitID.HistoryEodTickers: return 1;
            }
            Log.Warning("ExtMarketDataUNIBIT():Limit({0}) missing implementation", limitID.ToString());
            return 1;
        }

        /* Provided functionalities: 
         * 
         *  - With Free Account 50,000 credits per month for about/over 100 stocks
         *  - First payment account is 200$/m but allows usage on application, as long as not selling data sets
         *  - Nice option for WASM Clients (personal usage), as fast fetch, good coverage of markets
         *  - All Markets, Latest + Date periodic History, Full Stock Meta for for all markets..
         *  
         *  => Pretty trustable feeling, definedly good choice.. specially valuable for other market's meta+data
         *  => Negative: New stocks arrive w pretty hefty delay, way later than other APIs get them
         * 
         * - NASDAQ/NYSE nicely 3 hours after closing
         * - Also TSX & HEL etc works with this one
         * 
         * Divident:
         * - Maybe try later better, did try pull LUMN for 2021, did get last historical divident but not next ones information,
         *   could be because next one not yet decided. Anyway cost is 100 points of credit for single line reply.. so cant even test properly :/
         * 
         * Per https://unibit.ai/pricing:
         * - "The general rule of thumb is that you may not resell or redistribute our datasets. Any research, analytics or 
         *    application built upon our datasets are generally permitted."
         */

        /* InvalidKey   => 404, NotFound (nothing else available, did use DEMOKEY text as ApiKey)
         * WrongTicker  => OK, no errors, just returns csv with headers but no data
         * 
         * 
         * 
         * 
         */

        public Task<Dictionary<string, StockIntradayData>> GetIntradayAsync(MarketMeta marketMeta, List<string> tickers)
        {
            _error = "GetIntradayAsync() - Not supported";
            return null;
        }

        public async Task<Dictionary<string, StockClosingData>> GetEodLatestAsync(MarketMeta marketMeta, List<string> tickers)
        {
            _error = string.Empty;

            if (string.IsNullOrEmpty(_unibitApiKey) == true)
            {
                _error = "UNIBIT::GetEodLatestAsync() Missing private access key!";
                return null;
            }

            int amountOfReqTickers = tickers.Count();

            string unibitJoinedTickers = ExtMarketSuppUNIBIT.JoinPfsTickers(marketMeta.ID, tickers, _maxTickers);

            if (string.IsNullOrEmpty(unibitJoinedTickers) == true)
            {
                _error = "Failed, over ticker limit";
                Log.Warning("UNIBIT::GetEodLatestAsync() " + _error);
                return null;
            }

            try
            {
                HttpClient tempHttpClient = new HttpClient();
                HttpResponseMessage resp = await tempHttpClient.GetAsync($"https://api.unibit.ai/v2/stock/historical/?tickers={unibitJoinedTickers}&dataType=csv&accessKey={_unibitApiKey}");

                if (resp.IsSuccessStatusCode == false)
                {
                    _error = string.Format("Unibit failed: {0} for [[{1}]]", resp.StatusCode.ToString(), unibitJoinedTickers);
                    Log.Warning("UNIBIT::GetEodLatestAsync() " + _error);
                    return null;
                }

                string content = await resp.Content.ReadAsStringAsync();
                var dailyItems = content.FromCsv<List<UnibitDailyFormat>>();

                if (dailyItems == null || dailyItems.Count() == 0)
                {
                    _error = string.Format("Failed, empty data: {0} for [[{1}]]", resp.StatusCode.ToString(), unibitJoinedTickers);
                    Log.Warning("UNIBIT::GetEodLatestAsync() " + _error);
                    return null;
                }
                else if (dailyItems.Count() < amountOfReqTickers)
                {
                    _error = string.Format("Warning, requested {0} got just {1} for [[{2}]]", amountOfReqTickers, dailyItems.Count(), unibitJoinedTickers);
                    Log.Warning("UNIBIT::GetEodLatestAsync() " + _error);

                    // This is just warning, still got data so going to go processing it...
                }

                // All seams to be enough well so lets convert data to PFS format

                Dictionary<string, StockClosingData> ret = new Dictionary<string, StockClosingData>();

                foreach (var item in dailyItems)
                {
                    string pfsTicker = ExtMarketSuppUNIBIT.TrimToPfsTicker(marketMeta.ID, item.ticker);

                    ret[pfsTicker] = new StockClosingData()
                    {
                        Date = new DateTime(item.date.Year, item.date.Month, item.date.Day),
                        Close = item.close,
                        High = item.high,
                        Low = item.low,
                        Open = item.open,
                        PrevClose = -1,
                        Volume = item.volume,
                    };
                }

                return ret;
            }
            catch ( Exception e )
            {
                _error = string.Format("Failed! Connection exception {0} for [[{1}]]", e.Message, unibitJoinedTickers);
                Log.Warning("UNIBIT::GetEodLatestAsync() " + _error);
            }
            return null;
        }

        public async Task<Dictionary<string, List<StockClosingData>>> GetEodHistoryAsync(MarketMeta marketMeta, List<string> tickers, DateTime startDay, DateTime endDay)
        {
            _error = string.Empty;

            if (string.IsNullOrEmpty(_unibitApiKey) == true)
            {
                _error = "UNIBIT::GetEodHistoryAsync() Missing private access key!";
                return null;
            }

            var unibitJoinedTickers = ExtMarketSuppUNIBIT.JoinPfsTickers(marketMeta.ID, tickers, _maxTickers);
            
            if (string.IsNullOrEmpty(unibitJoinedTickers) == true)
            {
                _error = "Failed, over ticker limit!";
                Log.Warning("UNIBIT::GetEodHistoryAsync() " + _error);
                return null;
            }

            string start = startDay.ToString("yyyy-MM-dd");
            string end = endDay.ToString("yyyy-MM-dd");

            try
            {
                HttpClient tempHttpClient = new HttpClient();
                HttpResponseMessage resp = await tempHttpClient.GetAsync($"https://api.unibit.ai/v2/stock/historical/?tickers={unibitJoinedTickers}&dataType=csv&startDate={start}&endDate={end}&accessKey={_unibitApiKey}");

                if (resp.IsSuccessStatusCode == false)
                {
                    _error = string.Format("Failed: {0} for [[{1}]]", resp.StatusCode.ToString(), unibitJoinedTickers);
                    Log.Warning("UNIBIT:GetEodHistoryAsync() " + _error);
                    return null;
                }

                string content = await resp.Content.ReadAsStringAsync();
                var stockRecords = content.FromCsv<List<UnibitDailyFormat>>();

                if (stockRecords == null || stockRecords.Count() == 0)
                {
                    _error = string.Format("Failed, empty data: {0} for [[{1}]]", resp.StatusCode.ToString(), unibitJoinedTickers);
                    Log.Warning("UNIBIT:GetEodHistoryAsync() " + _error);
                    return null;
                }

                Dictionary<string, List<StockClosingData>> ret = new Dictionary<string, List<StockClosingData>>();

                int receivedDataAmount = 0;
                foreach (string pfsTicker in tickers)
                {
                    string unibitTicker = ExtMarketSuppUNIBIT.ExpandToUnibitTicker(marketMeta.ID, pfsTicker);

                    List<UnibitDailyFormat> partial = stockRecords.Where(s => s.ticker == unibitTicker).ToList();

                    if (partial.Count() == 0)
                    {
                        // this one didnt receive any data
                        Log.Warning("UNIBIT:GetEodHistoryAsync() no data for {0}", pfsTicker);
                        continue;
                    }

                    List<StockClosingData> ext = partial.ConvertAll(s => new StockClosingData()
                    {
                        Date = s.date,
                        Close = s.close,
                        High = s.high,
                        Low = s.low,
                        Open = s.open,
                        PrevClose = -1,
                        Volume = s.volume,
                    });

                    ext.Reverse();
                    ret.Add(pfsTicker, ext);

                    receivedDataAmount++;
                }

                if (receivedDataAmount < tickers.Count())
                {
                    _error = string.Format("Warning, requested {0} got {1} for [[{2}]]", tickers.Count(), receivedDataAmount, unibitJoinedTickers);
                    Log.Warning("UNIBIT:GetEodHistoryAsync() " + _error);
                }

                return ret;
            }
            catch (Exception e)
            {
                _error = string.Format("Failed, connection exception {0} for [[{1}]]", e.Message, unibitJoinedTickers);
                Log.Warning("UNIBIT::GetEodHistoryAsync() " + _error);
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
            ticker,date,open,high,low,close,adj close,volume
            LUMN,2021-03-15,14.13,14.33,14.015,14.19,14.19,8842219
            MSFT,2021-03-15,234.96,235.185,231.82,234.81,234.81,25970922
         */

        [DataContract]
        private class UnibitDailyFormat
        {
            [DataMember]
            public string ticker { get; set; }

            [DataMember]
            public DateTime date { get; set; }

            [DataMember]
            public decimal open { get; set; }

            [DataMember]
            public decimal high { get; set; }

            [DataMember]
            public decimal low { get; set; }

            [DataMember]
            public decimal close { get; set; }

            [DataMember(Name = "adj close")]
            public decimal adjclose { get; set; }

            [DataMember]
            public int volume { get; set; }
        }
    }
}
