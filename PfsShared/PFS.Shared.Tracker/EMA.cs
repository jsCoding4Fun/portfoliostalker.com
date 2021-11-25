/*
 * Copyright (c) 2021 Jami Suni
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Collections.Generic;

using PFS.Shared.Types;

namespace PFS.Shared.Tracker
{
    public class EmaCalc
    {
        /* EMA calculation bases to previous value, so important to do initial calculation over hefty amount of data. 
         * As calculation itself only uses previous days EMA, there is no need for seed so most time can just use 
         * 'CalculateNew' to get newer values as long as has previous EMA calculated w initially done w max history.
         */

        // Returns all possible EMA values able to calculate from given data, use as long history of data as possible!
        public List<IndicatorValue> RecalculateAll(List<StockClosingData> data, int period)
        {
            List<IndicatorValue> ret = new();

            if (data.Count < period + 1) // Needs [0] + Period to create first real value
                return null;

            decimal previous = 0;
            decimal multiplyer = 2 / (period + 1m);

            // First records are just used to create avrg of closing valuations
            for ( int pos = 0; pos < period; pos++ )
                previous += data[pos].Close;

            previous = previous / period;

            for ( int pos = period; pos < data.Count; pos++ )
            {
                decimal ema = data[pos].Close * multiplyer + previous * (1 - multiplyer);

                ret.Add(new IndicatorValue()
                {
                    Date = data[pos].Date,
                    Value = ema,
                });

                previous = ema;
            }

            return ret; 
        }

        // Figures out new data it has on given set per lastEma, and calculates new Ema's from that forward
        public List<IndicatorValue> CalculateNew(List<StockClosingData> data, int period, IndicatorValue lastEma)
        {
            List<IndicatorValue> ret = new();

            decimal previous = 0;
            decimal multiplyer = 2 / (period + 1m);

            int pos = 0;

            // Find first data after last calculations
            for (; pos < data.Count && data[pos].Date <= lastEma.Date; pos++)
            { 
                // Just looping to find spot...
            }

            if (pos >= data.Count)
                // Calculations are up to date
                return null;

            previous = lastEma.Value;

            for ( ; pos < data.Count; pos++)
            {
                StockClosingData temp = data[pos];

                decimal ema = data[pos].Close * multiplyer + previous * (1 - multiplyer);

                ret.Add(new IndicatorValue()
                {
                    Date = data[pos].Date,
                    Value = ema,
                });

                previous = ema;
            }

            return ret;
        }
    }
}
