using System;
using System.Collections.Generic;

using PFS.Shared.Types;

namespace PFS.Shared.UiTypes
{
    public class ReportTrackedStocksData
    {
        // Stock
        public Guid STID { get; set; }
        public MarketID MarketID { get; set; }
        public string Ticker { get; set; }
        public string Name { get; set; }

        public List<string> AnyPfHoldings { get; set; }     // Names of all portfolio's those have holdings of this stock
        public List<string> AnySgTracking { get; set; }     // Names of all stock group's those track this stock

        public DateTime? LatestDate { get; set;  }

        public decimal? Latest { get; set; }

        public DateTime LastStockEdit { get; set; }

        public bool IsUpToDate { get; set; }
        public bool IsIntraday { get; set; }
    }
}
