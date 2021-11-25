using System;

namespace PFS.Shared.UiTypes
{
    // Standard indicator data calculated for all stocks 
    public class StockIndicatorData
    {
        public Guid STID { get; set; }
        public DateTime Date { get; set; }


        public decimal? RSI14D { get; set; }        // Daily
        public int? RSI14Dlvl { get; set; }

        public decimal? RSI14W { get; set; }        // Weekly
        public int? RSI14Wlvl { get; set; }

        public decimal? MFI14D { get; set; }        // Daily
        public int? MFI14Dlvl { get; set; }

        public decimal? MFI14W { get; set; }        // Weekly
        public int? MFI14Wlvl { get; set; }
    }
}
