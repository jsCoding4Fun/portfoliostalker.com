using System.Collections.Generic;

namespace PFS.Shared.Types
{
    // Presenting one of Accounts Portfolios
    public class Portfolio
    {
        public string Name { get; set; }

        public List<StockOrder> StockOrders { get; set; }

        public List<StockHolding> StockHoldings { get; set; }       // Still open holdings, and also already closed/traded holdings

        public List<StockTrade> StockTrades { get; set; }           // Tracks sales for stocks under this portfolio

        public List<StockDivident> StockDividents { get; set; }     // Track all dividents received for this portfolio

        public Portfolio()
        {
            StockHoldings = new();
            StockOrders = new();
            StockTrades = new();
            StockDividents = new();
        }

        public Portfolio DeepCopy()
        {
            Portfolio ret = (Portfolio)this.MemberwiseClone();

            ret.StockOrders = new();
            foreach (StockOrder order in this.StockOrders)
                ret.StockOrders.Add(order.DeepCopy());

            ret.StockHoldings = new();
            foreach (StockHolding holding in this.StockHoldings)
                ret.StockHoldings.Add(holding.DeepCopy());

            ret.StockTrades = new();
            foreach (StockTrade trade in this.StockTrades)
                ret.StockTrades.Add(trade.DeepCopy());

            ret.StockDividents = new();
            foreach (StockDivident divident in this.StockDividents)
                ret.StockDividents.Add(divident.DeepCopy());

            return ret;
        }
    }
}
