using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using PFS.Shared.Types;

namespace PFS.Shared.UiTypes
{
    public class ReportStockTableData
    {
        // Stock
        public Guid STID { get; set; }
        public string Ticker { get; set; }
        public MarketID MarketID { get; set; }
        public string Name { get; set; }
        public DateTime LastStockEdit { get; set; }     // Information of last editing date user has market for stock (dropdown)

        public DateTime? LatestDate { get; set; }       // Fields used for daily closing related viewing
        public decimal? Latest { get; set; }
        public decimal? LatestChangeP { get; set; }
        public bool IsUpToDate { get; set; }
        public bool IsIntraday { get; set; }

        // Alarm Data
        public int? AlarmUnderP { get; set; }           // Base number w highest under alarm against current, shows as % (sortable)
        public int? AlarmOverP { get; set; }            // Base number w highest under alarm against current, shows as % (sortable)

        public bool AlarmUnderWatch { get; set; }       // Set if alarm has Watch, and its active so has gone under watch pre-warning level
        public bool AlarmOverWatch { get; set; }

        // Alarm Tooltips
        public string AlarmUnderTT { get; set; }
        public string AlarmOverTT { get; set; }

        // RSI14D & RSI14W
        public decimal? RSI14D { get; set; }
        public decimal? RSI14Dlvl { get; set; }
        public decimal? RSI14W { get; set; }
        public decimal? RSI14Wlvl { get; set; }

        // MFI14D & MFI14W
        public decimal? MFI14D { get; set; }
        public decimal? MFI14Dlvl { get; set; }
        public decimal? MFI14W { get; set; }
        public decimal? MFI14Wlvl { get; set; }

        public ReadOnlyCollection<StockAlarm> Alarms { get; set; }
        
        public ReadOnlyCollection<StockHolding> Holdings { get; set; } // raw data of holdings (null if no holdings)

        public ReadOnlyCollection<StockOrder> Orders { get; set; } // raw data of orders (null if has none)

        public List<ViewStockHoldings> ViewHoldings { get; set; } // view on table ready data of holdings (never null, always created)

        public List<StockTableDividentsView> ViewDividents { get; set; }

        public CurrencyCode Currency { get; set; }
        public int? ProfitP { get; set; }               // Per holdings, shows % gains wo including dividents (market currency)
        public int? ProfitAmount { get; set; }

        public CurrencyCode HomeCurrency { get; set; }
        public int? HcProfitP { get; set; }            // Per holdings, shows % gains wo including dividents (market currency)
        public int? HcProfitAmount { get; set; }


    }
}
