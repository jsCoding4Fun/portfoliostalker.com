/*
 * Copyright (c) 2021 Jami Suni
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections.Generic;

using PFS.Shared.Types;

namespace PFS.Shared.Tracker
{
    public class RsiCalc
    {
        public int Period { get; set; } = 14;

        public List<IndicatorValue> Recalculate(List<StockClosingData> data, out RsiSeed seeds)
        {
            List<IndicatorValue> ret = new();

            // This is case where seed is not available, so we calculate RSI w given data only.. ala full recalc
            seeds = new()
            {
                AvgGain = 0,
                AvgLoss = 0,
                Date = DateTime.MinValue,
            };

            if (data.Count < Period + 1)
                return null;

            for (int pos = 0; pos < data.Count; pos++)
            {
                decimal lastChange = data[pos].Close - data[pos].PrevClose;

                if (pos <= Period)
                {
                    // First 14 days we just keep counting total, not avaraging yet...
                    if (lastChange > 0)
                        seeds.AvgGain += lastChange;
                    else if (lastChange < 0)
                        seeds.AvgLoss += -1 * lastChange;

                    if (pos == Period)
                    {
                        // Got required many historical values, so ready to count first actual 'previous avarage'
                        if (seeds.AvgLoss > 0)
                            seeds.AvgLoss = seeds.AvgLoss / Period;

                        if (seeds.AvgGain > 0)
                            seeds.AvgGain = seeds.AvgGain / Period;
                    }
                }
                else
                {
                    ret.Add(new IndicatorValue()
                    {
                        Date = data[pos].Date,
                        Value = Calculate(data[pos], ref seeds),
                    });
                }
            }

            return ret;
        }

        public decimal Calculate(StockClosingData data, ref RsiSeed seeds)
        {
            decimal lastChange = data.Close - data.PrevClose;

            if (lastChange < 0)
            {
                seeds.AvgLoss = seeds.AvgLoss * (Period - 1) + -1 * lastChange;
                seeds.AvgGain = seeds.AvgGain * (Period - 1);
            }
            else
            {
                seeds.AvgLoss = seeds.AvgLoss * (Period - 1);
                seeds.AvgGain = seeds.AvgGain * (Period - 1) + lastChange;
            }

            if (seeds.AvgLoss > 0)
                seeds.AvgLoss = seeds.AvgLoss / Period;

            if (seeds.AvgGain > 0)
            {
                seeds.AvgGain = seeds.AvgGain / Period;

                return 100 - (100 / (1 + (seeds.AvgGain / seeds.AvgLoss)));
            }

            return 50;
        }
    }

    public class RsiSeed
    {
        public DateTime Date { get; set; }
        public Decimal AvgGain { get; set; }
        public Decimal AvgLoss { get; set; }
    }
}
