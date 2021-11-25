using System;
using System.Collections.Generic;

using PFS.Shared.Types;

namespace PFS.Shared.UiTypes
{
    public class ReportExportHoldingsData
    {
        // Stock
        public Guid STID;
        public string Ticker;
        public MarketID MarketID;
        public string Name;
        public string GroupDef;
        public CurrencyCode MarketCurrency;
        public string ReportNote;

        // Purhace information (MC)
        public decimal AvrgPrice;
        public decimal AvrgTime;
        public decimal Invested;
        public decimal TotalUnits;

        // Current Valuation/Growth (MC)
        public decimal? LatestEOD;
        public decimal? Valuation;
        public decimal? Growth;
        public decimal? GrowthP;

        // Divident (MC)
        public decimal? DividentLast;
        public decimal? DividentLastP;
        public decimal? DividentTotal;
        public decimal? DividentTotalP;

        // Total Gain (MC)
        public decimal? TotalGain;
        public decimal? TotalGainP;

        // Purhace information (HC)
        public decimal? HcAvrgPrice;
        public decimal? HcInvested;

        // Current Valuation/Growth (HC)
        public decimal? HcLatestEOD;
        public decimal? HcValuation;
        public decimal? HcGrowth;
        public decimal? HcGrowthP;

        // Divident (HC)
        public decimal? HcDividentLast;
        public decimal? HcDividentLastP;
        public decimal? HcDividentTotal;
        public decimal? HcDividentTotalP;

        // Total Gain (HC)
        public decimal? HcTotalGain;
        public decimal? HcTotalGainP;

        // Weight  (HC-only!)
        public decimal? HcWeightP;

        public ReportExportHoldingsData()
        {
            STID = Guid.Empty;
            Ticker = String.Empty;
            MarketID = MarketID.Unknown;
            Name = String.Empty;
            GroupDef = String.Empty;
            MarketCurrency = CurrencyCode.Unknown;
            ReportNote = String.Empty;

            // Purhace information (MC)
            AvrgPrice = 0;
            AvrgTime = 0;
            Invested = 0;
            TotalUnits = 0;

            // Current Valuation/Growth (MC)
            LatestEOD = null;
            Valuation = 0;
            Growth = 0;
            GrowthP = 0;

            // Divident (MC)
            DividentLast = 0;
            DividentLastP = 0;
            DividentTotal = 0;
            DividentTotalP = 0;

            // Total Gain (MC)
            TotalGain = 0;
            TotalGainP = 0;

            // Purhace information (HC)
            HcAvrgPrice = 0;
            HcInvested = 0;

            // Current Valuation/Growth (HC)
            HcLatestEOD = 0;
            HcValuation = 0;
            HcGrowth = 0;
            HcGrowthP = 0;

            // Divident (HC)
            HcDividentLast = 0;
            HcDividentLastP = 0;
            HcDividentTotal = 0;
            HcDividentTotalP = 0;

            // Total Gain (HC)
            HcTotalGain = 0;
            HcTotalGainP = 0;

            // Weight  (HC-only!)
            HcWeightP = 0;
        }
    }
}
