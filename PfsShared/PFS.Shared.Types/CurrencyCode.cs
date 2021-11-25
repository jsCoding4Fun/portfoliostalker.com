using System;

namespace PFS.Shared.Types
{
    // https://fastspring.com/blog/how-to-format-30-currencies-from-countries-all-over-the-world/
    public enum CurrencyCode : int
    {
        Unknown = 0,
        CAD,            // Canada
        EUR,            // Euro
        SEK,            // Sweden
        USD,            // United States
    }
}
