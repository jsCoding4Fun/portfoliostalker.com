using System;

namespace PFS.Shared.UiTypes
{
    public class ViewStockHoldings
    {
        /* MISSING ATM: 
         * 
         * - ProfitTotalP -> with divident total profit
         * - ProfitAnnualTotalP -> with divident avarage annualised profit
         * - Dividents collected
         * - Divident earn % to original price
         */

        public DateTime PurhaceDate { get; set; }   // Purhace Date

        public decimal Units { get; set; }          // Share's left from this Holding's set

        public decimal AvrgPrice { get; set; }      // Avarage purhace price per share including its share of fee's

        public int ProfitP { get; set; }            // This is profit without dividents included, so simple closing against avrgprice

        public decimal Invested { get; set; }       // How much total cost of remaining units is
    }
}
