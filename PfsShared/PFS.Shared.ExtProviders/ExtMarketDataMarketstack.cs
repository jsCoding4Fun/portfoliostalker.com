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
    // Marketstack: https://marketstack.com/documentation
    public class ExtMarketDataMarketstack : IExtDataProvider
    {
        protected string _error = string.Empty;

        protected string _marketstackApiKey = "";

        public string GetLastError() { return _error; }

        public void SetPrivateKey(string key)
        {
            _marketstackApiKey = key;
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

        const int _maxTickers = 20; // As of 2021-Nov-9th, did try 100->50->20.. and as this fails full patch if one stock fails inside it just 
                                    // wastes lot of credits most time.. so 20 it is now, speed is not benefit if half goes to retry...

        public int Limit(IExtDataProvider.LimitID limitID)
        {
            switch ( limitID )
            {
                case IExtDataProvider.LimitID.LatestEodTickers:
                case IExtDataProvider.LimitID.HistoryEodTickers:    return _maxTickers;

                case IExtDataProvider.LimitID.IntradayTickers:      return 1;           // Supposed to have API for 100, but it didnt work.. so 1-by-1 it is :/
            }
            Log.Warning("ExtMarketDataMarketstack():Limit({0}) missing implementation", limitID.ToString());
            return 1;
        }

        /* Note! 
         * 
         * - Cant use Free account on WASM, as only http thats not allowed w https wasm.. so minimum 9$ account required
         *   (To test with Free account on Developer Studio a PrivSrv works ok, but WASM only if application NOT on HTTPS,
         *    so need to change launchSettings.json to not have sslPort defined)
         * 
         * - Even 9$ payment account is 10,000 request ONLY per month, w 10 year history so long run would need 50$ plan,
         *   but this actually is interesting option as has wide market variety and 50$ account could cover pretty nicely
         *   even some limited commercial cases. Plus has Divident etc some features for future use.
         * 
         * Results (November as main LIVE provider, with premium account, so daily server testing run with w dream of being able to subscript for long term):
         * - 2021-Nov: Lot of stuff can be found from 'https://marketstack.com/search' but doesnt seam to mean there is data for them
         * - 2021-Nov: QQQ ETF from NASDAQ, did fast /eod/latest test for it, and got OK resp with empty data                           => No ETFS data??
         * - 2021-Nov: Failing daily supricinly many stocks: DOCN, LUMN, ... plus handful of new ones
         * - 2021-Nov: '/eod/latest' seams to work for 100 tickers, but Intraday just for one ticker on time
         * - 2021-Nov: $HON for Honeywell International Inc they have it under NYSE / XNYS, isnt that supposed to be NASDAQ? They dont have error reporting so nm..
         *             $BNL for Broadstone Net Lease Inc, they have it on NASDAQ, but its supposed to be under NYSE?
         *             $NTST Netstreit Corp, they have it on NASDAQ, supposed to be NYSE 
         *             => Yep, dang, after I whitelisted NYSES: HON, BNL, NTST and DOCN off from Marketstack.. all others 10+ NYSE stocks those failed with 
         *                them on same query worked ok. So they have issues with some stocks, looks almost like lazy hands on entering.. and need to 
         *                hand pick those out to whitelist :/ can see them w 'https://marketstack.com/search' and google if markets are same or not.
         * - 2021-Nov: 5th. Did have very bad day, I assume doesnt work with 100 tickers after all... down to 50 it goes ets see...
         * - 2021-Nov: 9th. Nah still loosing too many fetches and wasting credits as one loose seams to kill full pull. 50 -> 20
         *  
         *  
         * 
         * TODO:
         * - Waiting time testing, so see how soon after market closing a data is available. OMXH maybe even has to go longer than 3 hours??
         * - XETRA,     and specially XETRA ETF's
         * - STO/OMX, 
         * - Test more to see what markets require ticker expansion, and what market do not like it..
         * 
         * Pending features:
         * - Has indexes there w full eod-of-day data
         * - Divident, has info there... 
         */

        public async Task<Dictionary<string, StockClosingData>> GetEodLatestAsync(MarketMeta marketMeta, List<string> tickers)
        {
            _error = string.Empty;

            if (string.IsNullOrEmpty(_marketstackApiKey) == true)
            {
                _error = "Marketstack:GetEodLatestAsync() Missing private access key!";
                return null;
            }

            int amountOfReqTickers = tickers.Count();

            if (tickers.Count() > _maxTickers)
            {
                _error = "Failed, requesting too many tickers!";
                Log.Warning("Marketstack:GetEodLatestAsync() " + _error);
                return null;
            }

            string joinedMsTickers = JoinPfsTickers(marketMeta, tickers);

            try
            {
                HttpClient tempHttpClient = new HttpClient();
                HttpResponseMessage resp = await tempHttpClient.GetAsync($"https://api.marketstack.com/v1/eod/latest?access_key={_marketstackApiKey}&exchange={marketMeta.MIC}&symbols={joinedMsTickers}");

                if (resp.IsSuccessStatusCode == false)
                {
                    _error = string.Format("Failed: {0} for [[{1}]]", resp.StatusCode.ToString(), joinedMsTickers);
                    Log.Warning("Marketstack:GetEodLatestAsync() " + _error);
                    return null;
                }

                string content = await resp.Content.ReadAsStringAsync();
                var dailyItems = content.FromJson<MarketstackPeriod>();

                if (dailyItems == null || dailyItems.data == null || dailyItems.data.Count() == 0)
                {
                    _error = string.Format("Failed, Empty data: {0} for [[{1}]]", resp.StatusCode.ToString(), joinedMsTickers);
                    Log.Warning("Marketstack:GetEodLatestAsync() " + _error);
                    return null;
                }
                else if (dailyItems.data.Count() < amountOfReqTickers)
                {
                    _error = string.Format("Warning, requested {0} got just {1} for [[{2}]]", amountOfReqTickers, dailyItems.data.Count(), joinedMsTickers);
                    Log.Warning("Marketstack:GetEodLatestAsync() " + _error);

                    // This is just warning, still got data so going to go processing it...
                }

                // All seams to be enough well so lets convert data to PFS format

                Dictionary<string, StockClosingData> ret = new Dictionary<string, StockClosingData>();

                foreach (var item in dailyItems.data)
                {
                    ret[TrimToPfsTicker(marketMeta, item.symbol)] = new StockClosingData()
                    {
                        Date = new DateTime(item.date.Year, item.date.Month, item.date.Day),
                        Close = item.close,
                        High = item.high,
                        Low = item.low,
                        Open = item.open,
                        PrevClose = -1,
                        Volume = (int)(item.volume),
                    };
                }

                return ret;
            }
            catch (Exception e)
            {
                _error = string.Format("Failed, connection exception {0} for [[{1}]]", e.Message, joinedMsTickers);
                Log.Warning("Marketstack:GetEodLatestAsync() " + _error);
            }
            return null;
        }

        public async Task<Dictionary<string, List<StockClosingData>>> GetEodHistoryAsync(MarketMeta marketMeta, List<string> tickers, DateTime startDay, DateTime endDay)
        {
            _error = string.Empty;

            if (string.IsNullOrEmpty(_marketstackApiKey) == true)
            {
                _error = "Marketstack:GetEodHistoryAsync() Missing private access key!";
                return null;
            }

            if (tickers.Count() > _maxTickers)
            {
                _error = "Failed, requesting too many tickers!";
                Log.Warning("Marketstack:GetEodHistoryAsync() " + _error);
                return null;
            }

            string joinedTickers = JoinPfsTickers(marketMeta, tickers);

            string start = startDay.ToString("yyyy-MM-dd");
            string end = endDay.ToString("yyyy-MM-dd");

            try
            {
                HttpClient tempHttpClient = new HttpClient();
#if false // WORKS OK, kind of return max (or 1000 last days at least)
                // By default using same default as they, 100 last.. doesnt seam to effect cost
                int limit = 100;

                if ((endDay - startDay).TotalDays > 50)
                    // But even smallest risk of having too little data, we go long... anyway free account dont allow more than one year now
                    limit = 1000;

                // This seams to work, sure doesnt seam to mind returning 100 last days.. better too much than little
//                HttpResponseMessage resp = await tempHttpClient.GetAsync($"https://api.marketstack.com/v1/eod?access_key={_marketstackApiKey}&exchange={marketMeta.MIC}&symbols={joinedTickers}&limit={limit}");
#endif

                // Using date_from -> date_to seams to work also, go with this one atm!
                HttpResponseMessage resp = await tempHttpClient.GetAsync($"https://api.marketstack.com/v1/eod?access_key={_marketstackApiKey}&date_from={start}&date_to={end}&exchange={marketMeta.MIC}&symbols={joinedTickers}");

                // Works also with /eod/2021-08-01?... 
                // HttpResponseMessage resp = await tempHttpClient.GetAsync($"https://api.marketstack.com/v1/eod/{start}?access_key={_marketstackApiKey}&exchange={marketMeta.MIC}&symbols={joinedTickers}");

                if (resp.IsSuccessStatusCode == false)
                {
                    _error = string.Format("Failed: {0} for [[{1}]]", resp.StatusCode.ToString(), joinedTickers);
                    Log.Warning("Marketstack:GetEodHistoryAsync() " + _error);
                    return null;
                }

                string content = await resp.Content.ReadAsStringAsync();
                var stockRecords = content.FromJson<MarketstackPeriod>();

                if (stockRecords == null || stockRecords.data == null || stockRecords.data.Count() == 0)
                {
                    _error = string.Format("Failed, empty data: {0} for [[{1}]]", resp.StatusCode.ToString(), joinedTickers);
                    Log.Warning("Marketstack:GetEodHistoryAsync() " + _error);
                    return null;
                }

                Dictionary<string, List<StockClosingData>> ret = new Dictionary<string, List<StockClosingData>>();

                int receivedDataAmount = 0;
                foreach (string ticker in tickers)
                {
                    string msTicker = ExpandToMsTicker(marketMeta, ticker);

                    List<MarketstackPeriodEOD> partial = stockRecords.data.Where(s => s.symbol == msTicker).ToList();

                    if (partial.Count() == 0)
                    {
                        // this one didnt receive any data
                        Log.Warning("Marketstack:GetEodHistoryAsync() no data for {0}", ticker);
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
                        Volume = (int)(s.volume),
                    });

                    ext.Reverse();
                    ret.Add(TrimToPfsTicker(marketMeta, ticker), ext);

                    receivedDataAmount++;
                }

                if (receivedDataAmount < tickers.Count())
                {
                    _error = string.Format("Warning, requested {0} got {1} for [[{2}]]", tickers.Count(), receivedDataAmount, joinedTickers);
                    Log.Warning("Marketstack:GetEodHistoryAsync() " + _error);
                }

                return ret;
            }
            catch (Exception e)
            {
                _error = string.Format("Failed, connection exception {0} for [[{1}]]", e.Message, joinedTickers);
                Log.Warning("Marketstack:GetEodHistoryAsync() " + _error);
            }
            return null;
        }

        public async Task<Dictionary<string, StockIntradayData>> GetIntradayAsync(MarketMeta marketMeta, List<string> tickers)
        {
            // Note! Actually works for intraday, but need to call each ticker separately, and eats 1 point per ticker.. so credits go fast!!
            //       Even w 9$ 10000 credits would need to be carefull not to consume too fastly a monthly plan! Bah Bah Bah...

            _error = string.Empty;

            if (string.IsNullOrEmpty(_marketstackApiKey) == true)
            {
                _error = "Marketstack:GetIntradayAsync() Missing private access key!";
                return null;
            }

            if (tickers.Count() != 1)
            {
                _error = "Failed, requesting too many tickers!";
                Log.Warning("Marketstack:GetIntradayAsync() " + _error);
                return null;
            }

            string ticker = ExpandToMsTicker(marketMeta, tickers[0]);

            try
            {
                HttpClient tempHttpClient = new HttpClient(); // As of 2021-Nov-5th, go with this as fetching multiple doesnt seam to work w intraday/latest?...
                HttpResponseMessage resp = await tempHttpClient.GetAsync($"https://api.marketstack.com/v1/tickers/{ticker}/intraday/latest?access_key={_marketstackApiKey}&exchange={marketMeta.MIC}");

                // (2021-Nov) This atm hardcoded to AUY,VST.. they work separately but not together.. so hmm.. try later again...LUMN didnt wanna work at all...
                //HttpResponseMessage resp = await tempHttpClient.GetAsync($"https://api.marketstack.com/v1/intraday/latest?access_key={_marketstackApiKey}&symbols=AUY,VST&exchange={marketMeta.MIC}");

                if (resp.IsSuccessStatusCode == false)
                {
                    _error = string.Format("Failed: {0} for [[{1}]]", resp.StatusCode.ToString(), ticker);
                    Log.Warning("Marketstack:GetIntradayAsync() " + _error);
                    return null;
                }

                string content = await resp.Content.ReadAsStringAsync();
                var intradayData = content.FromJson<MarketstackIntraday>();

                if (intradayData == null || intradayData.last == 0)
                {
                    _error = string.Format("Failed, Empty data: {0} for [[{1}]]", resp.StatusCode.ToString(), ticker);
                    Log.Warning("Marketstack:GetIntradayAsync() " + _error);
                    return null;
                }

                // All seams to be enough well so lets convert data to PFS format

                Dictionary<string, StockIntradayData> ret = new Dictionary<string, StockIntradayData>();

                ret[tickers[0]] = new StockIntradayData()
                {
                    DayTime = intradayData.date,
                    Latest = intradayData.last,
                    Open = intradayData.open,
                    High = intradayData.high,
                    Low = intradayData.low,
                    Volume = (int)intradayData.volume,
                    PrevClose = -1,
                };

                return ret;
            }
            catch (Exception e)
            {
                _error = string.Format("Failed, connection exception {0} for [[{1}]]", e.Message, ticker);
                Log.Warning("Marketstack:GetIntradayAsync() " + _error);
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

        //
        // Even that can pass in a Market's MIC value, doesnt actually work without ticker being set for TICKER.MIC for XTSE etc to some markets
        // 

        static public string TrimToPfsTicker(MarketMeta marketMeta, string marketstackTicker)
        {
            if (ExpandRequired(marketMeta) == true )
                return marketstackTicker.ReplaceFirst("." + marketMeta.MIC, ""); // Example XHEL needs ticker on UPM.XHEL format

            return marketstackTicker;
        }

        static public string ExpandToMsTicker(MarketMeta marketMeta, string pfsTicker)
        {
            if (ExpandRequired(marketMeta) == true)
                return string.Format("{0}.{1}", pfsTicker, marketMeta.MIC); // US markets would work ok without, but example XHEL NOT!

            return pfsTicker;
        }

        static public string JoinPfsTickers(MarketMeta marketMeta, List<string> pfsTickers)
        {
            return string.Join(',', pfsTickers.ConvertAll<string>(s => ExpandToMsTicker(marketMeta, s)));
        }

        static public bool ExpandRequired(MarketMeta marketMeta)
        {
            switch ( marketMeta.ID )
            {
                case MarketID.NASDAQ:
                case MarketID.NYSE:
                case MarketID.AMEX:             // Not sure AMEX yet
                    // These seam NOT liking to do expansion... rest of markets seams to REQUIRE it... 
                    return false;
            }
            // Assuming everyone else needs this expansion
            return true;
        }

        // !!!REMEMBER!!! JSON!!! https://json2csharp.com/json-to-csharp

        public class MarketstackIntraday
        {
            public decimal open { get; set; }
            public decimal high { get; set; }
            public decimal low { get; set; }
            public decimal last { get; set; }
            public decimal close { get; set; }
            public decimal volume { get; set; }
            public DateTime date { get; set; }
            public string symbol { get; set; }
            public string exchange { get; set; }
        }

        public class PeriodPagination
        {
            public int limit { get; set; }
            public int offset { get; set; }
            public int count { get; set; }
            public int total { get; set; }
        }

        public class MarketstackPeriodEOD
        {
            public decimal open { get; set; }
            public decimal high { get; set; }
            public decimal low { get; set; }
            public decimal close { get; set; }
            public decimal volume { get; set; }
            public decimal? adj_high { get; set; }
            public decimal? adj_low { get; set; }
            public decimal adj_close { get; set; }
            public decimal? adj_open { get; set; }
            public decimal? adj_volume { get; set; }
            public decimal split_factor { get; set; }
            public string symbol { get; set; }
            public string exchange { get; set; }
            public DateTime date { get; set; }
        }

        public class MarketstackPeriod
        {
            public PeriodPagination             pagination { get; set; }
            public List<MarketstackPeriodEOD>   data { get; set; }
        }
    }
}
