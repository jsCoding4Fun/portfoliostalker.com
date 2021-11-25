/*
 * Copyright (c) 2021 Jami Suni
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Collections.Generic;

using PFS.Shared.Types;

namespace PFS.Shared.ExtProviders
{
    // Some helper functions used for Unibit related conversions
    public class ExtMarketSuppUNIBIT
    {
        static public string TrimToPfsTicker(MarketID marketID, string unibitTicker)
        {
            string unibitTickerEnding = UnibitTickerEnding(marketID);

            if (string.IsNullOrWhiteSpace(unibitTickerEnding) == false && unibitTicker.EndsWith(unibitTickerEnding) == true)
                return unibitTicker.Substring(0, unibitTicker.Length - unibitTickerEnding.Length);

            return unibitTicker;
        }

        static public string ExpandToUnibitTicker(MarketID marketID, string pfsTicker)
        {
            string unibitTickerEnding = UnibitTickerEnding(marketID);
             
            if (string.IsNullOrWhiteSpace(unibitTickerEnding) == false && pfsTicker.EndsWith(unibitTickerEnding) == false)
                return pfsTicker + unibitTickerEnding;

            return pfsTicker;
        }

        static public string JoinPfsTickers(MarketID marketID, List<string> pfsTickers, int maxTickers)
        {
            // Up to 50 stock quotes can be requested at a time. (https://unibit.ai/api/docs/V2.0/historical_stock_price)

            if (pfsTickers.Count > maxTickers)
                // Coding error, should have divided this task to multiple parts
                return string.Empty;

            return string.Join(',', pfsTickers.ConvertAll<string>(s => ExpandToUnibitTicker(marketID, s)));
        }

        static protected string UnibitTickerEnding(MarketID marketID)
        {
            switch (marketID)
            {
                case MarketID.OMX:     return ".ST";
                case MarketID.OMXH:    return ".HE";
                case MarketID.TSX:     return ".TO";
                case MarketID.GER:     return ".DE";
            }
            return "";
        }
    }
}
