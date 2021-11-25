namespace PFS.Shared.Types
{
    // Provides information of specific market that PFS supports, mainly related to MarketID and information required for closing time calculations
    public class MarketMeta 
    {
        public MarketID ID { get; set; }                    // (TSX, NYSE, NASDAQ, etc)

        public string MIC { get; set; }                     // Market Identifier Code, ala MIC (XNYS for NYSE, XNAS for NASDAQ) https://en.wikipedia.org/wiki/Market_Identifier_Code

        public string Name { get; set; }                    // (New York Stock Exchange for NYSE)

        public CurrencyCode Currency { get; set; }          // 

        // Following ones are internal valuas from XML used on calculations to try figure out market's closing time

        public int MarketLocalClosingHour { get; set; }     // Example 0700 as 24h clock time when market closes, manually updated!
        public int MarketLocalClosingMin { get; set; }      // Defaults to zero, but can add like 30 if on half hour mark closing
        public string LinuxTag { get; set; }                // America/New_York etc  .... 'Eastern Standard Time' for New York example
        public string WasmTag { get; set; }                 // As of 2021-Aug missing most ... waiting .NET 6.0... 'Eastern Standard Time' for New York example
        public int MarketLocalToUtc { get; set; }           // Example -4h for new york (atm as may wary per winter/summer time)
    }
}
