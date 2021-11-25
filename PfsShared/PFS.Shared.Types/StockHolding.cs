using System;
using System.Collections.Generic;

namespace PFS.Shared.Types
{
    // Presents ownership of specific Stock (STID) under specific Portfolio (and structure exists also after its fully sold/traded off)
    public class StockHolding
    {
        // Decision! Editing of 'Holding' is only possible as long nothing is sold/traded off yet

        public Guid STID { get; set; }

        public string HoldingID { get; set; }           // User given or system generated unique identifier for each holding (cant be edited)

        public decimal PurhacedUnits { get; set; }       // Original owned unit amount

        public decimal PricePerUnit { get; set; }

        public decimal Fee { get; set; }

        public DateTime PurhaceDate { get; set; }

        public decimal RemainingUnits { get; set; }      // When smaller than 'PurhacedUnits' then partially sold some units, and if 0 then fully sold Holding

        public decimal ConversionRate { get; set; }     // This is from stock's market defined currency to account currency by multiplying
                                                        // so example PricePerUnit for $T is 27.3 then this is 0.85 example for euro account
        public CurrencyCode ConversionTo { get; set; } = CurrencyCode.Unknown; // What is account HomeCurrency on time of adding 'ConversionRate'

        public string HoldingNote { get; set; }         // Custom note related to 'purhace feelings'

        public List<DividentsToHolding> Dividents { get; set; }

        public StockHolding()
        {
            Dividents = new();
        }

        // Decision! Atm trying to make Holding-Trade as one directional, so Holding doesnt know its Trade, only Trade knows Holding it sold
        //           so only thing here from sale's is 'RemainingUnits' that is smaller than 'PurhacedUnits' if something is sold, and hits
        //           zero when Holding is not really anymore Holding anything.

        public StockHolding DeepCopy()
        {
            StockHolding ret = (StockHolding)this.MemberwiseClone(); // Works as deep as long no complex tuff

            ret.Dividents = new();
            foreach (DividentsToHolding divident in this.Dividents)
                ret.Dividents.Add(divident.DeepCopy());

            return ret;
        }

        public class DividentsToHolding
        {
            public string DividentID { get; set; }          // Reference to StockDivident that was used to add this record under holding

            public decimal Units { get; set; }              // How many RemainingUnits where owned time that divident was recorded

            public DividentsToHolding DeepCopy()
            {
                DividentsToHolding ret = (DividentsToHolding)this.MemberwiseClone(); // Works as deep as long no complex tuff

                return ret;
            }
        }
    }
}
