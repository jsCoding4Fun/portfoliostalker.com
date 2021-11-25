
namespace PFS.Shared.Types
{
    // StockAlarm always belongs (under) specific Stock that valuation it tracks
    public class StockAlarm
    {
        public StockAlarmType Type { get; set; }

        public decimal Value { get; set; }

        public decimal Param1 { get; set; }             // Usage of this depends from Alarm type

        public string Note { get; set; }                // !!!LATER!!! limit length to 100 & strict format limits as XML stored

        public StockAlarm DeepCopy()
        {
            StockAlarm ret = (StockAlarm)this.MemberwiseClone(); // Works as deep as long no complex tuff
            return ret;
        }
    }
}
