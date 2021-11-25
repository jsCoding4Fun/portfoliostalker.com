using System;
using System.Collections.Generic;

using PFS.Shared.Types;

namespace PFS.Shared.UiTypes
{
    // Used to pass Provider Keys & Markets using what External data Provider information
    public class SettMarketProviders
    {
        // Holds provider as key, and its 3th party access key string as value
        public Dictionary<ExtDataProviders, string> ProviderKeys { get; set; }

        // Holds MarketID's as keys to access specific markets current provider selection
        public Dictionary<MarketID, ExtDataProviders> MarketsProvider { get; set; }   // MarketID, ProviderID

        // Allows configure potential BackUp provider, thats used on some retry#'s instead of default provider in failure situations
        public Dictionary<MarketID, ExtDataProviders> MarketsBackupProvider { get; set; }

        // Holds STID for stock those WhiteListed, and provider.. note! provider can be 'Unknown' as thats case where whitelist is disabled atm for it
        public Dictionary<Guid, ExtDataProviders> WhiteListedStocks { get; set; }

        // Allows to define custom time when this markets information should be fetched [0...360 (minutes)]
        public Dictionary<MarketID, int> MarketsManualFetchTime { get; set; }

        public SettMarketProviders()
        {
            ProviderKeys = new();
            MarketsProvider = new();
            MarketsBackupProvider = new();
            WhiteListedStocks = new();
            MarketsManualFetchTime = new();

            // We add these foreach providers as allows to assign values directly to structure on editing
            foreach (ExtDataProviders provider in Enum.GetValues(typeof(ExtDataProviders)))
            {
                if (provider == ExtDataProviders.Unknown)
                    continue;

                ProviderKeys[provider] = string.Empty;
            }
        }
    }
}
