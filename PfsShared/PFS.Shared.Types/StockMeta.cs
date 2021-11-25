using System;

namespace PFS.Shared.Types
{
    public class StockMeta // !!!STORAGE_FORMAT!!! Dont change order or fields here as used by SrvMarketMeta SupportedStocks storing format!
    {
        public MarketID MarketID { get; set; }                                  // Lets just go now with this one, MIC is not that well known so confusing
        public string Ticker { get; set; }                                      // 
        public string Name { get; set; }
        public Guid STID { get; set; }                                          // Using Guid as allows changing ticker, without loosing groups nor holdings
        //public string ISIN { get; set; }                                      // ISO 6166 (ISIN), optional as API's dont seam to provide this

        // NOTE: Doesnt need currency, as stock market & UI views dictate that

        public StockMeta DeepCopy()
        {
            StockMeta ret = (StockMeta)this.MemberwiseClone(); // Works as deep as long no complex tuff
            return ret;
        }
    }
}
