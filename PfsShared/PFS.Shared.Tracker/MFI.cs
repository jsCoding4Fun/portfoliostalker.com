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
    public class MfiCalc
    {
        /* MFI is not effected by huge amount of history, but only by limited days like MFI14D is 15 days required for 
         * recounting it. It does calculate total over that period so seeding would get pretty complicated compared to
         * benefits => No seed to be used! Calculate requires enough data, and not just latest days data.
         */

        // Testing against: https://www.profitspi.com/stock/view.aspx?v=stock-chart&uv=100588&p=MSFT

        // Returns all possible MFI values able to calculate from given data
        public List<IndicatorValue> RecalculateAll(List<StockClosingData> data, int period)
        {
            List<IndicatorValue> ret = new();

            if (data.Count < period + 1) // Needs [0] + 14 moneyFlows to create first real value
                return null;

            decimal posMoneyFlow = 0;
            decimal negMoneyFlow = 0;

            decimal prevTypicalPrice = LocalTypicalPrice(data[0]);

            // First records are used just to get up and speed w periods total money flows
            for ( int pos = 1; pos < period; pos++ )
            {
                decimal currTypicalPrice = LocalTypicalPrice(data[pos]);
                decimal currMoneyFlow = currTypicalPrice * data[pos].Volume;

                if (currTypicalPrice > prevTypicalPrice)
                    posMoneyFlow += currMoneyFlow;
                else if (currTypicalPrice < prevTypicalPrice)
                    negMoneyFlow += currMoneyFlow;

                prevTypicalPrice = currTypicalPrice;
            }

            for ( int pos = period; pos < data.Count; pos++ )
            {
                decimal currTypicalPrice = LocalTypicalPrice(data[pos]);
                decimal currMoneyFlow = currTypicalPrice * data[pos].Volume;

                // Increase money flows
                if (currTypicalPrice > prevTypicalPrice)
                    posMoneyFlow += currMoneyFlow;
                else if (currTypicalPrice < prevTypicalPrice)
                    negMoneyFlow += currMoneyFlow;

                // Calculate MFI
                ret.Add(new IndicatorValue()
                {
                    Date = data[pos].Date,
                    Value = LocalCalculateMfi(posMoneyFlow, negMoneyFlow),
                });

                // Remove older moneyflow from total to allow endless looping
                decimal oldCurrTypicalPrice = LocalTypicalPrice(data[pos - period + 1]);
                decimal oldCurrMoneyFlow = oldCurrTypicalPrice * data[pos - period + 1].Volume;
                decimal oldPrevTypicalPrice = LocalTypicalPrice(data[pos - period]);

                if (oldCurrTypicalPrice > oldPrevTypicalPrice)
                    posMoneyFlow -= oldCurrMoneyFlow;
                else if (oldCurrTypicalPrice < oldPrevTypicalPrice)
                    negMoneyFlow -= oldCurrMoneyFlow;

                prevTypicalPrice = currTypicalPrice;
            }
            return ret; 

            decimal LocalTypicalPrice(StockClosingData d)
            {
                return (d.High + d.Low + d.Close) / 3;
            }

            decimal LocalCalculateMfi(decimal pos, decimal neg)
            {
                if (pos <= 1)
                    return 0;
                else if (neg <= 1) // Note! Did actually happen, came here w 0.00000...002 w full recount and crash below to 'pos / neg'
                    return 100;

                return 100.0m - 100.0m / (1.0m + pos / neg);
            }
        }

        public decimal? CalculateLatest(List<StockClosingData> data, int period)
        {
            if (data.Count < period + 1) // Needs 1 + 14 moneyFlows for MFI14 
                return null;

            decimal prevTypicalPrice = -1;
            // decimal prevMoneyFlow = -1;

            decimal posMoneyFlow = 0;
            decimal negMoneyFlow = 0;

            // This loop we just calculate 'posMoneyFlow' & 'negMoneyFlow' for period
            for ( int pos = data.Count - period - 1; pos < data.Count; pos++ )
            {
                decimal currTypicalPrice = (data[pos].High + data[pos].Low + data[pos].Close) / 3;
                decimal currMoneyFlow = currTypicalPrice * data[pos].Volume;

                if (pos == data.Count - period - 1)
                {
                    // For first record to be processed we just set prev -values to initial values
                    prevTypicalPrice = currTypicalPrice;
//                    prevMoneyFlow = currMoneyFlow;
                    continue;
                }

                if (currTypicalPrice > prevTypicalPrice)
                {
                    // if current typical price > previous typical price then add the curr/todays money flow to the positive money flow
                    posMoneyFlow += currMoneyFlow;          // !!!OPEN ISSUE!!! Some material this is latest moneyFlow, others its prevMoneyFlow.. 
                }
                else if (currTypicalPrice < prevTypicalPrice)
                {
                    // if current typical price <previous typical price then add the curr/todays money flow to the negative money flow
                    negMoneyFlow += currMoneyFlow;
                }

                prevTypicalPrice = currTypicalPrice;
//                prevMoneyFlow = currMoneyFlow;
            }

            if (posMoneyFlow <= 1)
                return 0;
            else if (negMoneyFlow <= 1)
                return 100;

            return 100.0m - 100.0m / (1.0m + posMoneyFlow / negMoneyFlow);
        }
    }
}
