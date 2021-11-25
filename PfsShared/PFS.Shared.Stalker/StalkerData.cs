/*
 * Copyright (c) 2021 Jami Suni
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using PFS.Shared.Types;

namespace PFS.Shared.Stalker
{
    // Raw presentation of specific accounts core data
    public class StalkerData
    {
        // One account can have multiple portfolios, those each have their own Holdings
        protected List<Portfolio> _portfolios = new();

        // Stock group belongs to one of portfolios, but its kept on separate list
        protected List<StockGroup> _stockGroups = new();

        // This list contains all alarm & meta etc information of stocks user is following/tracking
        protected List<Stock> _stocks = new();

        public static void DeepCopy(StalkerData from, StalkerData to) // Main issue w C# to C++ is that just cant get strong feeling what happens on "assembler" level, too automatic,
        {                                                             // so just in case well do it this way.. even this is easy code to catch errors on future extentions :/
            to._portfolios = new();
            to._stockGroups = new();
            to._stocks = new();

            foreach (Portfolio pf in from._portfolios)
                to._portfolios.Add(pf.DeepCopy());

            foreach (StockGroup sg in from._stockGroups)
                to._stockGroups.Add(sg.DeepCopy());

            foreach (Stock s in from._stocks)
                to._stocks.Add(s.DeepCopy());
        }

        public Portfolio PortfolioRef(string pfName)
        {
            return _portfolios.SingleOrDefault(p => p.Name == pfName);
        }

        public ref readonly List<Portfolio> Portfolios()
        {
            return ref _portfolios;
        }

        // Returns only Holdings, those still active w unsold unit's
        public ReadOnlyCollection<StockHolding> PortfolioHoldings(string pfName, bool includeTradedHistoricalHoldings = false)
        {
            Portfolio pf = _portfolios.SingleOrDefault(p => p.Name == pfName);

            if (pf == null)
                // Should always return at least empty collection, never null, even this major failure
                return new List<StockHolding>().AsReadOnly();

            if (includeTradedHistoricalHoldings == false)
                return pf.StockHoldings.Where(h => h.RemainingUnits > 0).OrderBy(h => h.PurhaceDate).ToList().AsReadOnly();
            else
                return pf.StockHoldings.OrderBy(h => h.PurhaceDate).ToList().AsReadOnly();
        }

        public ReadOnlyCollection<StockDivident> PortfolioDividents(string pfName)
        {
            Portfolio pf = _portfolios.SingleOrDefault(p => p.Name == pfName);

            if (pf == null)
                // Should always return at least empty collection, never null, even this major failure
                return new List<StockDivident>().AsReadOnly();

            return pf.StockDividents.AsReadOnly();
        }

        public ReadOnlyCollection<StockTrade> PortfolioTrades(string pfName)
        {
            Portfolio pf = _portfolios.SingleOrDefault(p => p.Name == pfName);

            if (pf == null)
                // Should always return at least empty collection, never null, even this major failure
                return new List<StockTrade>().AsReadOnly();

            return pf.StockTrades.AsReadOnly();
        }

        public ReadOnlyCollection<StockOrder> PortfolioOrders(string pfName)
        {
            Portfolio pf = _portfolios.SingleOrDefault(p => p.Name == pfName);

            if (pf == null)
                // Should always return at least empty collection, never null, even this major failure
                return new List<StockOrder>().AsReadOnly();

            return pf.StockOrders.AsReadOnly();
        }

        public StockHolding StockHoldingRef(string holdingID)
        {
            Portfolio pf = _portfolios.Where(p => p.StockHoldings.Any(h => h.HoldingID == holdingID)).SingleOrDefault();

            if (pf == null)
                return null;

            return pf.StockHoldings.Single(h => h.HoldingID == holdingID);
        }

        public StockTrade StockTradeRef(string tradeID)
        {
            Portfolio pf = _portfolios.Where(p => p.StockTrades.Any(t => t.TradeID == tradeID)).SingleOrDefault();

            if (pf == null)
                return null;

            return pf.StockTrades.Single(t => t.TradeID == tradeID);
        }

        public StockDivident StockDividentRef(string dividentID)
        {
            Portfolio pf = _portfolios.Where(p => p.StockDividents.Any(d => d.DividentID == dividentID)).SingleOrDefault();

            if (pf == null)
                return null;

            return pf.StockDividents.Single(d => d.DividentID == dividentID);
        }

        public List<Guid> PortfolioFollows(string pfName)
        {
            List<Guid> ret = new();

            List<StockGroup> pfGroups = StockGroups().Where(g => g.OwnerPfName == pfName).ToList();

            foreach ( StockGroup group in pfGroups)
            {
                ret.AddRange(group.StocksSTIDs);
            }
            return ret.Distinct().ToList();
        }

        public StockGroup StockGroupRef(string sgName)
        {
            return _stockGroups.SingleOrDefault(p => p.Name == sgName);
        }

        public ref readonly List<StockGroup> StockGroups()
        {
            return ref _stockGroups;
        }

        public ReadOnlyCollection<StockGroup> StockGroups(string pfName)
        {
            return StockGroups().Where(g => g.OwnerPfName == pfName).ToList().AsReadOnly();
        }

        public Stock StockRef(Guid STID)
        {
            return _stocks.SingleOrDefault(s => s.Meta.STID == STID);
        }

        public ReadOnlyCollection<StockAlarm> StockAlarms(Guid STID)
        {
            Stock stock = StockRef(STID);

            if (stock == null)
                return new List<StockAlarm>().AsReadOnly();

            return stock.Alarms.AsReadOnly();
        }

        public ref readonly List<Stock> Stocks()
        {
            return ref _stocks;
        }

        public StockMeta StockGetMeta(Guid STID)
        {
            Stock stock = StockRef(STID);

            if (stock == null)
                return null;

            return stock.Meta;
        }

        public StockMeta StockGetMeta(MarketID marketID, string ticker)
        {
            Stock stock = _stocks.SingleOrDefault(s => s.Meta.MarketID == marketID && s.Meta.Ticker == ticker);

            if (stock == null)
                return null;

            return stock.Meta;
        }
    }
}
