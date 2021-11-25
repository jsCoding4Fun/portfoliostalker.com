using System;

namespace PFS.Shared.UiTypes
{
    public class StockTableDividentsView
    {
        public DateTime Date { get; set; }

        public decimal Units { get; set; }

        public decimal PaymentPerUnit { get; set; }
    }
}
