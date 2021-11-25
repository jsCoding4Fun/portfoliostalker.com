using System;

using PFS.Shared.Types;

namespace PFS.Shared.UiTypes
{
    public class ReportHoldingsData
    {
        public string PfName { get; set; }

        // Stock
        public Guid STID { get; set; }
        public string Ticker { get; set; }
        public MarketID MarketID { get; set; }
        public string Name { get; set; }

        public decimal? Latest { get; set; }

        public StockHolding Holding { get; set; }

        public decimal AvrgPrice { get; set; }      // Avarage purhace price per share including its share of fee's

        // Market priced valuations
        public CurrencyCode Currency { get; set; }
        public int ProfitP { get; set; }            // This is profit without dividents included, so simple closing against avrgprice
        public decimal Invested { get; set; }       // How much total cost of remaining units is
        public int? ProfitAmount { get; set; }

        public decimal? DividentTotal { get; set; }
        public decimal? DividentTotalP;
        public decimal? DividentLast { get; set; }
        public decimal? DividentLastP;

        // Valuations on Home currency
        public CurrencyCode HomeCurrency { get; set; } // Similar values but per account default currency if its on and rates are available
        public int? HcProfitP { get; set; }         
        public decimal? HcInvested { get; set; }     
        public int? HcProfitAmount { get; set; }

        public decimal? HcDividentTotal { get; set; }
        public decimal? HcDividentTotalP;
        public decimal? HcDividentLast { get; set; }
        public decimal? HcDividentLastP;
    }
}
