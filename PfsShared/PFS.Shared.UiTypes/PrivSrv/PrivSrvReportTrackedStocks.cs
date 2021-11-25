using System;
using System.Collections.Generic;

using PFS.Shared.Types;

namespace PFS.Shared.UiTypes
{
    public class PrivSrvReportTrackedStocks
    {
        public StockMeta StockMeta { get; set; }

        public List<string> UsersTracking { get; set; }

        public DateTime? LatestEOD { get; set;  }

        public decimal? LatestEODClosing { get; set; }

        public bool IsUpToDate { get; set; }

        public DateTime? OldestEOD { get; set; }
    }
}
