using System;
using System.Collections.Generic;

namespace PFS.Shared.Types
{
    // Mainly client side, presents one tracked Stock on internal data format where Alarm's & StockMeta etc are available for specific Stock STID,
    public class Stock
    {
        public List<StockAlarm> Alarms { get; set; }    // Stock can have multiple different alarms per its value & indicators

        public StockMeta Meta { get; set; }

        public DateTime LastStockEdit { get; set; } = new DateTime(2021, 1, 1);

        public Stock DeepCopy()
        {
            Stock ret = (Stock)this.MemberwiseClone();
            ret.Meta = Meta.DeepCopy();
            ret.Alarms = new();

            foreach (StockAlarm alarm in this.Alarms)
                ret.Alarms.Add(alarm.DeepCopy());

            return ret;
        }

        /* !!!DOCUMENT!!! LastStockEdit
         * 
         * - StalkerAPI allows Set-Stock to update date
         * - StalkerXML backups/restores it
         * - Import StockNotes takes it out of TAG's if has #210607 type tag
         * - Show on Tracking Report
         * - Update date per WaveAlarms import
         * - Stock report drop down to show latest edit day
         * - Stock report drop down to allow click-set edit for today
         * - Adding new stock from online to stock group (ala new tracked stock) gets today as default
         * - When ever user edits stock's notes manually from UI, this date gets set to current day
         * - Add/Edit alarm is updating dayfield
         */
    }
}
