
using System;

namespace PFS.Shared.Types
{
    public class CompanyMeta // !!!STORAGE_FORMAT!!! This is FullMarketMeta CSV file storage format, any change breaks lot of CSV files!
    {
        public string Ticker { get; set; }
        public string CompanyName { get; set; }

        //public string ISIN { get; set; }
    }
}
