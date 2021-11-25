namespace PFS.Shared.Types
{
    public enum MarketID : int  // Note! Order here also presents default order when trying to find ticker from markets
    {
        Unknown,
        NASDAQ,
        NYSE,
        AMEX,
        TSX,
        OMXH,
        OMX,
        GER
    }
}
