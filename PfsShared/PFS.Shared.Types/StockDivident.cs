using System;

namespace PFS.Shared.Types
{
    // Presents Divident gained on specific day to Stock of Portfolio (either against one or multiple StockHoldings of Portfolio)
    public class StockDivident
    {
        // Decision! Editing of 'Divident' is not allowed, if wants to edit then need to delete and recreate

        public Guid STID { get; set; }

        public string DividentID { get; set; }          // User given or system generated unique identifier for each divident (cant be edited)

        public decimal Units { get; set; }              // How many units where owned that divident was payed for

        public decimal PaymentPerUnit { get; set; }

        public DateTime Date { get; set; }

        public decimal ConversionRate { get; set; }     // This is from stock's market defined currency to account currency by multiplying
                                                        // so example DividentPerUnit for $T is 0.3 then this is 0.85 example for euro account
                                                        // Decision! yes use ConversionRate as thats easiest automatize & verify/edit by user
        public CurrencyCode ConversionTo { get; set; } = CurrencyCode.Unknown; // What is account HomeCurrency on time of adding 'ConversionRate'

        public StockDivident DeepCopy()
        {
            StockDivident ret = (StockDivident)this.MemberwiseClone(); // Works as deep as long no complex tuff
            return ret;
        }
    }
}
