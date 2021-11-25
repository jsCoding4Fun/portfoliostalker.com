using System;

namespace PFS.Shared.Types
{
    public class StockOrder
    {
        public StockOrderType Type { get; set; }

        public Guid STID { get; set; }

        public decimal Units { get; set; }

        public decimal PricePerUnit { get; set; }

        public DateTime FirstDate { get; set; }
        public DateTime LastDate { get; set; }

        public DateTime? FillDate { get; set; }                 // Per days price order should be done on market, used as alarm

        public StockOrder DeepCopy()
        {
            StockOrder ret = (StockOrder)this.MemberwiseClone(); // Works as deep as long no complex tuff
            return ret;
        }
    }
}
