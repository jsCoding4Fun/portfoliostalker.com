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
    public class TraceFetchData : IFetchData
    {
        public event EventHandler<string> ParsingEvent;

        protected IFetchData _forward;

        public TraceFetchData(ref IFetchData forward)
        {
            _forward = forward;
        }

        public List<FetchTreeData> GetTreeData()
        {
            List<FetchTreeData> data = _forward.GetTreeData();

            string line = string.Format("!F \x1F GetTreeData");

            foreach (FetchTreeData entry in data)
            {
                line += Environment.NewLine + "^ " + entry.Name + " " + entry.Type.ToString() + " " + entry.Path;
            }

            ParsingEvent?.Invoke(this, line);

            return data;
        }


        public List<MarketMeta> GetMarketMeta(bool configuredOnly = false)
        {
            List<MarketMeta> markets = _forward.GetMarketMeta(configuredOnly);

            string line = string.Format("!F \x1F GetMarketMeta \x1F configuredOnly={0}", configuredOnly.ToString());

            foreach (MarketMeta market in markets)
            {
                line += Environment.NewLine + "^ " + market.ID + " " + market.MIC + " " + market.Name;
            }

            ParsingEvent?.Invoke(this, line);

            return markets;
        }

        public async Task<string> TestStockFetchAsync(Guid STID)
        {
            string ret = await _forward.TestStockFetchAsync(STID);

            string line = string.Format("!F \x1F TestStockFetchAsync \x1F STID={0}", STID.ToString());

            ParsingEvent?.Invoke(this, line);

            return ret;
        }

        public async Task<string> TestStockFetchAsync(MarketID marketID, string ticker)
        {
            string ret = await _forward.TestStockFetchAsync(marketID, ticker);

            string line = string.Format("!F \x1F TestStockFetchAsync \x1F marketID={0} \x1F ticker={1}", marketID.ToString(), ticker);

            ParsingEvent?.Invoke(this, line);

            return ret;
        }

        public async Task<List<CompanyMeta>> SearchCompaniesAsync(MarketID marketID, string search)
        {
            List<CompanyMeta> companies = await _forward.SearchCompaniesAsync(marketID, search);

            string line = string.Format("!F \x1F SearchCompaniesAsync \x1F marketID={0} \x1F search={1}", marketID.ToString(), search);

            foreach (CompanyMeta comp in companies)
            {
                line += Environment.NewLine + "^ " + comp.Ticker + " " + comp.CompanyName;
            }

            ParsingEvent?.Invoke(this, line);

            return companies;
        }

        public async Task<(MarketID marketID, CompanyMeta companyMeta)> FindTickerAsync(string marketsToSearch, string ticker)
        {
            (MarketID marketID, CompanyMeta companyMeta) = await _forward.FindTickerAsync(marketsToSearch, ticker);

            string line = string.Format("!F \x1F FindTickerAsync \x1F marketsToSearch={0} \x1F ticker={1}", marketsToSearch, ticker);

            if ( marketID == MarketID.Unknown )
                line += Environment.NewLine + "^ not found";
            else
                line += Environment.NewLine + "^ " + marketID.ToString() + "$" + companyMeta.Ticker + " [" + companyMeta.CompanyName + "]";

            ParsingEvent?.Invoke(this, line);

            return (marketID: marketID, companyMeta: companyMeta);
        }


        public async Task<decimal?> GetCurrencyConversionAsync(CurrencyCode from, CurrencyCode to, DateTime date)
        {
            decimal? ret = await _forward.GetCurrencyConversionAsync(from, to, date);

            string line = string.Format("!F \x1F GetCurrencyConversionAsync \x1F from={0} \x1F to={1} \x1F date={2}", from.ToString(), to.ToString(), date.ToString("yyyy-MM-dd"));

            if (ret != null)
            {
                line += Environment.NewLine + "^ " + ret.Value;
            }

            ParsingEvent?.Invoke(this, line);

            return ret;
        }

        public List<News> NewsGetList()
        {
            List<News> news = _forward.NewsGetList();

            string line = string.Format("!F \x1F NewsGetList");

            foreach (News n in news)
            {
                line += Environment.NewLine + "^ " + n.ID + " " + n.Status.ToString() + " " + n.Category.ToString() + " " + n.Date.ToString("yyyy-MM-dd")
                      + " [" + n.Header + "] [" + n.Text + "] [" + n.Params + "]";
            }

            ParsingEvent?.Invoke(this, line);

            return news;
        }

        public void NewsChangeStatus(int newsID, NewsStatus status)
        {
            _forward.NewsChangeStatus(newsID, status);

            string line = string.Format("!F \x1F NewsChangeStatus \x1F newsID={0} \x1F status={1}", newsID, status.ToString());

            ParsingEvent?.Invoke(this, line);
        }

        public LocalEodDataStatus GetLocalEodDataStatus()
        {
            LocalEodDataStatus ret = _forward.GetLocalEodDataStatus();

            string line = string.Format("!F \x1F GetStockEodStatus");

            line += Environment.NewLine + "^ UpdateOnGoing=" + ret.UpdateOnGoing + " TotalTrackedStocks=" + ret.TotalTrackedStocks
                  + " NoDataStocks=" + ret.NoDataStocks + " ExpiredStocks=" + ret.ExpiredStocks;

            ParsingEvent?.Invoke(this, line);

            return ret;
        }

        public async Task DoUpdateExpiredEodAsync()
        {
            await _forward.DoUpdateExpiredEodAsync();

            string line = string.Format("!F \x1F DoUpdateExpiredEodAsync");

            ParsingEvent?.Invoke(this, line);
        }

        public async Task DoFetchNoDataEodAsync()
        {
            await _forward.DoFetchNoDataEodAsync();

            string line = string.Format("!F \x1F DoFetchNoDataEodAsync");

            ParsingEvent?.Invoke(this, line);
        }

        public async Task DoFetchLatestIntradayAsync(string[] markets, string[] portfolios, ExtDataProviders provider)
        {
            await _forward.DoFetchLatestIntradayAsync(markets, portfolios, provider);

            string line = string.Format("!F \x1F DoFetchLatestIntradayAsync \x1F markets={0} \x1F portfolios={1} \x1F provider={2}", string.Join(',', markets), string.Join(',', portfolios), provider.ToString());

            ParsingEvent?.Invoke(this, line);
        }
    }
}
