using System;

namespace PFS.Shared.Types
{
    public enum Indicator : int
    {
        Unknown = 0,
        RSI14D,
        RSI14W,
        MFI14D,
        MFI14W,
    }

    public class IndicatorValue
    {
        public DateTime Date { get; set; }
        public decimal Value { get; set; }
    }
}
