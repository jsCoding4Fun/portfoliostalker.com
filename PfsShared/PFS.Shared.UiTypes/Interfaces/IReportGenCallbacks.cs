using PFS.Shared.Types;

namespace PFS.Shared.UiTypes
{
    public interface IReportGenCallbacks
    {
        CurrencyCode GetHomeCurrency();

        StockClosingData GetLatestEOD(Guid STID);               // return *null* if not available

        MarketMeta GetMarketMeta(MarketID marketID);

        decimal? GetLatestConversionRate(CurrencyCode currencyCode);
    }
}
