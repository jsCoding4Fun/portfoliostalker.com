
namespace PFS.Shared.Types
{
    public enum ExtDataProviders : int // !!!STORAGE_FORMAT!!! Used on Priv Servers config XML as field value, dont change names
    {
        Unknown = 0,
        Tiingo,
        Unibit,
        Marketstack,
        AlphaVantage,
        Polygon,
        Iexcloud,
    }

    public enum ExtDataProviderJobType : int
    {
        Unknown = 0,
        EndOfDay,       // Provides basic End-Of-Day valuation for history & latest (IExtMarketDataProvider)
        Intraday,       // Optional functionality for 'IExtMarketDataProvider' to provide also Intraday information
        Currency,       // History & Latest currency valuations (IExtCurrencyDataProvider)
    }
}
