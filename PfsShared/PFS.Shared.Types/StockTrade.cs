using System;
using System.Collections.Generic;

namespace PFS.Shared.Types
{
    public class StockTrade
    {
        // Decision! Its all with one price, if needs separate prices then add as separate Trade's

        public Guid STID { get; set; }

        public string TradeID { get; set; }        // User given or system generated unique identifier for each holding

        public decimal SoldUnits { get; set; }

        public decimal PricePerUnit { get; set; }

        public decimal Fee { get; set; }

        public DateTime SaleDate { get; set; }

        public List<SoldHoldingType> SoldHoldings { get; set; }     // Tuple is hard to read, (field,field) new format doesnt JSON correct?


        public decimal ConversionRate { get; set; }     // This is from stock's market defined currency to account currency by multiplying
                                                        // so example PricePerUnit for $T is 27.3 then this is 0.85 example for euro account
        public CurrencyCode ConversionTo { get; set; } = CurrencyCode.Unknown; // What is account HomeCurrency on time of adding 'ConversionRate'

        public StockTrade DeepCopy()
        {
            StockTrade ret = (StockTrade)this.MemberwiseClone(); // Works as deep as long no complex tuff

            ret.SoldHoldings = new();

            foreach (SoldHoldingType item in SoldHoldings)
            {
                ret.SoldHoldings.Add(new()
                {
                    HoldingID = item.HoldingID,
                    SoldUnits = item.SoldUnits,
                });
            }

            return ret;
        }

        public class SoldHoldingType
        {
            public string HoldingID { get; set; }
            public decimal SoldUnits { get; set; }

            public SoldHoldingType DeepCopy()
            {
                return (SoldHoldingType)this.MemberwiseClone(); // Works as deep as long no complex tuff
            }
        }
    }
}
