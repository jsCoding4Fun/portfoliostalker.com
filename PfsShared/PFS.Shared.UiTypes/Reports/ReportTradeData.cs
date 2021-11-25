using System;
using System.Collections.Generic;

using PFS.Shared.Types;

namespace PFS.Shared.UiTypes
{
    public class ReportTradeData
    {
        public string TradeID { get; set; }

        // Stock
        public Guid STID { get; set; }
        public string Ticker { get; set; }
        public MarketID MarketID { get; set; }
        public string Name { get; set; }

        public DateTime SaleDate { get; set; }
        public decimal SoldUnits { get; set; }

        public CurrencyCode Currency { get; set; }  // Stock's currency per its Market
        public decimal Invested { get; set; }
        public decimal Sold { get; set; }
        public int ProfitP { get; set; }

        public CurrencyCode HomeCurrency { get; set; }
        public decimal? HcInvested { get; set; } 
        public decimal? HcSold { get; set; }
        public int? HcProfitP { get; set; }

        public List<ReportTradeHoldings> Holdings { get; set; }
    }

    public class ReportTradeHoldings
    {
        public DateTime PurhaceDate { get; set; }
        public decimal PurhaceUnitPrice { get; set; }
        public decimal PurhacedUnits { get; set; }

        public decimal SoldUnitPrice { get; set; }
        public decimal SoldUnits { get; set; }

        public decimal Invested { get; set; }
        public decimal Sold { get; set; }
        public int ProfitP { get; set; }

        public decimal? HcInvested { get; set; }
        public decimal? HcSold { get; set; }
        public int? HcProfitP { get; set; }
    }
}
