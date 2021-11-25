using System;
using System.Collections.Generic;

using PFS.Shared.Types;

namespace PFS.Shared.UiTypes
{
    // Basic high level Client side report of Invested stocks purhace prices, latest valuations, and profit... on Market and HomeCurrency
    public class ReportInvestedData
    {
        // Stock
        public Guid STID { get; set; }
        public string Ticker { get; set; }
        public MarketID MarketID { get; set; }
        public string Name { get; set; }

        public decimal AvrgPrice { get; set; }
        public decimal TotalUnits { get; set; }

        public CurrencyCode Currency { get; set; }  // Stock's currency per its Market
        public decimal Invested { get; set; }       // We always know this one for each holding
        public decimal? Valuation { get; set; }     // Having valuation depends having LatestEOD, so almost always should have this
        public int? ProfitP { get; set; }           // And if has Valuation, like mostly have, then we can have % profit between Invested - Valuation for stock

        public CurrencyCode HomeCurrency { get; set; }  // User's default currency
        public decimal? HcInvested { get; set; }    // Shown for stocks those all holdings has initial currency conversion rate properly set for HomeCurrency
        public decimal? HcValuation { get; set; }  // As long LatestEOD is known, and current conversion rates this should be easy
        public int? HcProfitP { get; set; }       // Profit requires both '(HcValuation - HcInvested) / HcInvested'

        public decimal? HcInvestedOfTotalP { get; set; }
        public decimal? HcValuationOfTotalP { get; set; }

        public decimal? HcDividentAll { get; set; }

        public decimal? HcDividentAllP { get; set; }

        public decimal HcGain { get; set; }
        public int? HcGainP { get; set; }

        public List<ReportHoldingsData> Holdings { get; set; }
    }

    public class ReportInvestedHeader
    {
        public CurrencyCode Currency { get; set; }  // Set if there is investments, and they all for same currency stocks
        public decimal? TotalInvested { get; set; }  // Set if all stocks have same currency
        public decimal? TotalValuation { get; set; } // If all stocks have same currency, and each stock has valuation set, then total valuation can be calculated
        public int? TotalProfitAmount { get; set; } // Difference between 'TotalValuation - TotalInvested' shown header if all same currency stocks
        public int? TotalProfitP { get; set; }      // Prosentual presentation of gain 'TotalProfitAmount'

        public CurrencyCode HomeCurrency { get; set; }  // Set per current userSettings
        public decimal? HcTotalInvested { get; set; }  // Total available if all stocks has 'HcInvested' prorly set
        public decimal? HcTotalValuation { get; set; } // If normal 'TotalValuation' can be calculated then just requires latest currency conversion rates
        public int? HcTotalProfitAmount { get; set; }
        public int? HcTotalProfitP { get; set; }
        public decimal HcTotalDivident { get; set; }
        public decimal? HcTotalDividentP { get; set; }

        public decimal? HcTotalGain { get; set; }
        public int? HcTotalGainP { get; set; }
    }
}
