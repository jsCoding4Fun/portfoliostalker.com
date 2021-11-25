using System;

namespace PFS.Shared.Types
{
    // This is format that all End-Of-Day ala Stock's EOD history is collected
    public class StockClosingData // !!!STORAGE_FORMAT!!! Dont change tuff here as used on SrvMarketData ALL history storing format!
    {
        public DateTime Date { get; set; }      // !!!NOTE!!! This is Market's local date wo timestamp, and not utc (actually utc would be same)
        public decimal Close { get; set; }
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal PrevClose { get; set; }
        public int Volume { get; set; }

        public StockClosingData DeepCopy()
        {
            StockClosingData ret = (StockClosingData)this.MemberwiseClone(); // Works as deep as long no complex tuff
            return ret;
        }
    }

    public class StockIntradayData
    {
        public DateTime DayTime { get; set; }
        public decimal Latest { get; set; }
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal PrevClose { get; set; }
        public int Volume { get; set; }
    }
}
