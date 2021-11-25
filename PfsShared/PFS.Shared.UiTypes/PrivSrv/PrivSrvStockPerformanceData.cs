using PFS.Shared.Types;

namespace PFS.Shared.UiTypes
{
    // This is format that all End-Of-Day ala Stock's EOD history is collected
    public class PrivSrvStockPerformanceData
    {
        public StockClosingData[] Eod { get; set; }
        public StockClosingData[] Eow { get; set; }

        public decimal[] RSI14D { get; set; }
        public int[] RSI14Dlvl { get; set; }

        public decimal[] RSI14W { get; set; }
        public int[] RSI14Wlvl { get; set; }

        public decimal[] MFI14D { get; set; }
        public int[] MFI14Dlvl { get; set; }

        public decimal[] MFI14W { get; set; }
        public int[] MFI14Wlvl { get; set; }

        public PrivSrvStockPerformanceData DeepCopy()
        {
            PrivSrvStockPerformanceData ret = (PrivSrvStockPerformanceData)this.MemberwiseClone(); // Works as deep as long no complex tuff
            return ret;
        }
    }
}
