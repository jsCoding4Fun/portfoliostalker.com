
using PFS.Shared.Types;

namespace PfsDevelUI.Shared
{
    public class UiF
    {
        // !!!LATER!!! With far far on future, when doing actual clean UI with translations, change this system to use Microsoft proper output formats..
        static public string Curr(CurrencyCode currency)
        {
            switch (currency)
            {
                case CurrencyCode.CAD: return "C$";
                case CurrencyCode.EUR: return "E";
                case CurrencyCode.USD: return "U$";
                case CurrencyCode.SEK: return "SEK";
            }
            return "?";
        }
    }
}
