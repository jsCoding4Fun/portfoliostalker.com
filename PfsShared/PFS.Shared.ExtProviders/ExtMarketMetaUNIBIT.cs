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

using Serilog;
using ServiceStack;         // !!!NUGET!!!: GetStringFromUrl()

using PFS.Shared.Types;

namespace PFS.Shared.ExtProviders
{
    // Unibit seams to have nice coverage of market's, and pretty trustable EOD information.. atm good main daily EOD work horse, specially for side markets
    public class ExtMarketMetaUNIBIT : IExtMetaProvider
    {
        protected string _error = string.Empty;

        protected string _unibitApiKey = "";

        public string GetLastError() { return _error; }

        public void SetPrivateKey(string key)
        {
            _unibitApiKey = key;
        }

        public List<CompanyMeta> GetAllStocksOnMarket(MarketMeta marketMeta) // !!!NOTE!!! Only use this for non-US stocks per high cost issues!
        {
            if (marketMeta.ID == MarketID.NASDAQ || marketMeta.ID == MarketID.NYSE|| marketMeta.ID == MarketID.AMEX)
            {
                // Dont use this for US stock's as can get same information free-of-charge with manual CSV download from:
                // https://www.nasdaq.com/market-activity/stocks/screener?exchange=NASDAQ&render=download
                // and doing it with Unibit is very costly for free account limitations

                _error = string.Format("UNIBIT not supporting {0} atm per cost reasons", marketMeta.ID);
                Log.Warning("UNIBIT::GetAllStocksOnMarket() " + _error);
                return null;
            }

            try
            {
                // https://unibit.ai/api/docs/V2.0/stock_coverage

                var allStocksStr = $"https://api.unibit.ai/v2/ref/companyList?exchange={marketMeta.ID}&dataType=csv&accessKey={_unibitApiKey}"
                        .GetStringFromUrl();

                return GetAllStocksOnCSV(marketMeta, allStocksStr);
            }
            catch ( Exception e )
            {
                _error = string.Format("Failed! Connection exception {0} for {1}", e.Message, marketMeta.ID);
                Log.Warning("UNIBIT::GetAllStocksOnMarket() " + _error);
            }
            return null;
        }

        public List<CompanyMeta> GetAllStocksOnCSV(MarketMeta marketMeta, string csvContent)
        {
            // First convert resulted CSV string to Unibit formatted list
            var allStocksList = csvContent.FromCsv<List<UnibitStockMeta>>();

            // Note! Unibit names tickers on non-US markets w extra .EXT, like TSX all tickers has .TO.
            // Really dont like this Unibit specific naming to spread around on code, as we anyway always
            // use MarketID with Ticker.. so well strip those off 

            // And then do second round of convestions to make it PFS required EXT format
            return allStocksList.ConvertAll(x => new CompanyMeta 
                    { 
                        Ticker = ExtMarketSuppUNIBIT.TrimToPfsTicker(marketMeta.ID, x.ticker), 
                        CompanyName = x.companyName 
                    });
        }

        /*
        ticker,companyName,exchange,exchangeShort,currency,timezone
        AAL,American Airlines Group Inc.,NASDAQ Stock Exchange,NASDAQ,USD,America/New_York  
         */

        [DataContract]
        private class UnibitStockMeta
        {
            [DataMember]
            public string ticker { get; set; }

            [DataMember]
            public string companyName { get; set; }

            [DataMember]
            public string exchange { get; set; }

            [DataMember]
            public string exchangeShort { get; set; }

            [DataMember]
            public string currency { get; set; }

            [DataMember]
            public string timezone { get; set; }
        }
    }
}
