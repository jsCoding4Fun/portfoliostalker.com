using System;
using System.Collections.Generic;

namespace PFS.Shared.Types
{
    // // Presenting one of StockGroup under one of accounts Portfolio
    public class StockGroup
    {
        public string Name { get; set; }                // User given name

        public string OwnerPfName { get; set; }        //

        public List<Guid> StocksSTIDs { get; set; }

        public StockGroup()
        {
            StocksSTIDs = new List<Guid>();
        }

        public StockGroup DeepCopy()
        {
            StockGroup ret = (StockGroup)this.MemberwiseClone();

            ret.StocksSTIDs = new();
            foreach (Guid STID in this.StocksSTIDs)
                ret.StocksSTIDs.Add(STID);

            return ret;
        }
    }
}
