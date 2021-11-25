using System;

using PFS.Shared.Types;

namespace PFS.Shared.UiTypes
{
    public class ReportUserEventsData
    {
        public UserEventType Type { get; set; }
        public UserEventMode Mode { get; set; }

        public DateTime Date { get; set; }
        public int EventID { get; set; }
        public Guid STID { get; set; }
        public MarketID MarketID { get; set; }
        public string Ticker { get; set; }
        public string CompanyName { get; set; }
        public CurrencyCode Currency { get; set; }
        public string PfName { get; set; }
        public object Content { get; set; }

        public string Debug { get; set; }
    }
}
