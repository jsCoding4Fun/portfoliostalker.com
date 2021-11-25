/*
 * Copyright (c) 2021 Jami Suni
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Collections.Generic;
using System.Runtime.Serialization;

using ServiceStack;     // !!!NUGET!!! ServiceStack.Text for 'FromCsv'

using PFS.Shared.Types;

namespace PFS.Shared.ExtProviders
{
    public class ExtMarketMetaNasdaqDotCom : IExtMetaProvider
    {
        public string GetLastError()
        {
            return string.Empty;
        }

        public void SetPrivateKey(string key)
        {

        }

        // This function is to ONLY convert downloaded external nasdaq formatted CSV to list of Pfs used ExtStockMeta
        public List<CompanyMeta> GetAllStocksOnMarket(MarketMeta marketMeta) // !!!NOTE!!! Only use this for US stocks
        {
            // OBSOLETE ATM, SUPPORT MOVED TO UI: Still manual download from: https://www.nasdaq.com/market-activity/stocks/screener?exchange=NASDAQ&render=download
            return null;
        }

        public List<CompanyMeta> GetAllStocksOnCSV(MarketMeta marketMeta, string csvContent)
        {
            var allStocksList = csvContent.FromCsv<List<NasdaqDotComMeta>>();

            return allStocksList.ConvertAll(x => new CompanyMeta { Ticker = x.Symbol, CompanyName = x.Name });
        }

        /*
Symbol,Name,Last Sale,Net Change,% Change,Market Cap,Country,IPO Year,Volume,Sector,Industry
AACG,ATA Creativity Global American Depositary Shares,$4.74,0.14,3.043%,148601375.00,China,,681699,Consumer Services,Other Consumer Services
        */

        [DataContract]
        private class NasdaqDotComMeta
        {
            [DataMember]
            public string Symbol { get; set; }

            [DataMember]
            public string Name { get; set; }

            [DataMember(Name = "Last Sale")]
            public string LastSale { get; set; }

            [DataMember(Name = "Net Change")]
            public string NetChange { get; set; }

            [DataMember(Name = "% Change")]
            public string ProsChange { get; set; }

            [DataMember(Name = "Market Cap")]
            public string MarketCap { get; set; }

            [DataMember(Name = "Country")]
            public string Country { get; set; }

            [DataMember(Name = "IPO Year")]
            public string IpoYear { get; set; }

            [DataMember]
            public string Volume { get; set; }

            [DataMember]
            public string Sector { get; set; }

            [DataMember]
            public string Industry { get; set; }
        }
    }
}
