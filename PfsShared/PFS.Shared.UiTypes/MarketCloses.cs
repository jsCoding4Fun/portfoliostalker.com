using System;
using PFS.Shared.Common;
using PFS.Shared.Types;

namespace PFS.Shared.UiTypes
{
    // !!!LATER!!! Could derive this class from MarketMeta, and set some of those calculation fields as protected.. and do derive as protected
    //             with constructor that eats MarketMeta & UTC and does calculation.. should be able to hide fields and force calculation always??



    public class MarketCloses
    {
        public DateTime LastClosingDate { get; set; }       // Date only (wo time) identifying last mon-fri whats market data should be available as latest

        public DateTime LastCloseUTC { get; set; }          // Exact UTC day+time when market per Mon-Friday is was closing last time (per Mon-Fri)

        public DateTime NextCloseUTC { get; set; }          // Exact UTC day+time when market per Mon-Friday is expected closing next time

        private static bool exceptionOnLinuxZoneInfo = false;// Any exception of handling conversion on official way permanently falls handling to 'manual'
        private static bool exceptionOnWasmZoneInfo = false;

        static public MarketCloses Calculate(DateTime currentUTC, MarketMeta marketMeta)
        {
            // !!!NOTE!!!   As originally WASM did have different names for tags, this code still has support separately
            // !!!LATER!!!  => maybe just merge to one TAG, but no rush...

            MarketCloses closes = new();

            DateTime marketLocalTime = currentUTC; // This is fake assignment as compiler getting confused, and erroring otherwise

            //
            // LINUX) On Linux containers, for servers, this following code should work properly and find defined markets timezones
            //

            if (exceptionOnLinuxZoneInfo == false)
            {
                try
                {
                    // Note! Uncomment this one, to see list of all zones.. then find proper ID for market and use that as LinuxTag
                    //IReadOnlyCollection<TimeZoneInfo> zones = TimeZoneInfo.GetSystemTimeZones();

                    // With timezone can get nicely local time on what ever city market is
                    TimeZoneInfo marketTimezone = TimeZoneInfo.FindSystemTimeZoneById(marketMeta.LinuxTag);
                    marketLocalTime = TimeZoneInfo.ConvertTimeFromUtc(currentUTC, marketTimezone);
                }
                catch (Exception)
                {
                    // Failed, maybe WASM or missing/invalid defination.. fall back to next attempt.. and never try again.. 
                    exceptionOnLinuxZoneInfo = true;
                }
            }

            //
            // WASM) With NET 6.0 this should now work, and they actually seams to be using same tags.. but atm still separate on code
            //

            if (exceptionOnLinuxZoneInfo == true && exceptionOnWasmZoneInfo == false)
            {
                try
                {
                    TimeZoneInfo marketTimezone = TimeZoneInfo.FindSystemTimeZoneById(marketMeta.WasmTag);
                    marketLocalTime = TimeZoneInfo.ConvertTimeFromUtc(currentUTC, marketTimezone);
                }
                catch (Exception)
                {
                    exceptionOnWasmZoneInfo = true;
                }
            }

            //
            // BACKUP) When either of Tag's works.. we fall back to manually set 'MarketLocalToUtc' that just simple contains +- value for conversion
            //         problem with this is that needs to be manually updated each time winter/summer time messes up things...
            // 

            if (exceptionOnLinuxZoneInfo == true && exceptionOnWasmZoneInfo == true)
            {
                // And if doesnt work then fall back to 'manual XML base difference between market time and UTC as hours'
                marketLocalTime = currentUTC.AddHours(marketMeta.MarketLocalToUtc);
            }

            //
            // As prep work is done and we should know market's local time.. we can start figuring out rest of calculations
            // 

            // Next figure out what should be closing day+time of market, per XML hardcoded market HHMM closing time
            DateTime marketLocalClosing = new DateTime(marketLocalTime.Year, marketLocalTime.Month, marketLocalTime.Day,
                                                       marketMeta.MarketLocalClosingHour, marketMeta.MarketLocalClosingMin, 0);

            // Make sure falls back to Friday if atm living weekend
            if (marketLocalClosing.DayOfWeek == DayOfWeek.Saturday)
                marketLocalClosing = marketLocalClosing.AddDays(-1);
            else if (marketLocalClosing.DayOfWeek == DayOfWeek.Sunday)
                marketLocalClosing = marketLocalClosing.AddDays(-2);

            if (marketLocalClosing < marketLocalTime)
            {
                // This case market has 'just' closed... 
            }
            else
            {
                // This case we are atm open or waiting to open.. so fall back previous closing
                marketLocalClosing = PfsSupp.AddWorkingDays(marketLocalClosing, -1);

            }

            closes.LastClosingDate = marketLocalClosing.Date;

            if (exceptionOnLinuxZoneInfo == false)
            {
                TimeZoneInfo marketTimezone = TimeZoneInfo.FindSystemTimeZoneById(marketMeta.LinuxTag);
                closes.LastCloseUTC = TimeZoneInfo.ConvertTimeToUtc(marketLocalClosing, marketTimezone);
            }
            else
            {
                closes.LastCloseUTC = marketLocalClosing.AddHours(-1 * marketMeta.MarketLocalToUtc);
            }

            closes.NextCloseUTC = PfsSupp.AddWorkingDays(closes.LastCloseUTC, +1);

            return closes;
        }
    }
}
