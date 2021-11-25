/*
 * Copyright (c) 2021 Jami Suni
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using PFS.Shared.Types;

namespace PFS.Shared.TraceAPIs
{
    internal static class ExtStockMetaClass
    {
        public static string ToDebugString(this StockMeta stockMeta)
        {
            return string.Format("StockMeta: MarketID={0} Ticker={1} Name=[{2}] STID={3}", stockMeta.MarketID, stockMeta.Ticker, stockMeta.Name, stockMeta.STID);
        }
    }

    internal static class ExtStockAlarmClass
    {
        public static string ToDebugString(this StockAlarm stockAlarm)
        {
            return string.Format("StockAlarm: Type={0} Value={1} Param1={2} Note=[{3}]", stockAlarm.Type.ToString(), stockAlarm.Value, stockAlarm.Param1, stockAlarm.Note);
        }
    }

    internal static class ExtStockOrderClass
    {
        public static string ToDebugString(this StockOrder stockOrder)
        {
            return string.Format("StockOrder: Type={0} Units={1} PricePerUnit={2} FirstDate={3} LastDate={4} FillDate={5} STID={6}", 
                stockOrder.Type.ToString(), stockOrder.Units, stockOrder.PricePerUnit, 
                stockOrder.FirstDate.ToString("yyyy-MM-dd"), stockOrder.LastDate.ToString("yyyy-MM-dd"),
                stockOrder.FillDate.HasValue ? stockOrder.FillDate.Value.ToString("yyyy-MM-dd") : "-no filled-", stockOrder.STID.ToString());
        }
    }

    internal static class ExtStockDividentClass
    {
        public static string ToDebugString(this StockDivident stockDivident)
        {
            return string.Format("StockDivident: DividentID={0} Units={1} PaymentPerUnit={2} Date={3} ConversionRate={4} ConversionTo={5} STID={6}",
                stockDivident.DividentID, stockDivident.Units, stockDivident.PaymentPerUnit,
                stockDivident.Date.ToString("yyyy-MM-dd"), stockDivident.ConversionRate,
                stockDivident.ConversionTo.ToString(), stockDivident.STID.ToString());
        }
    }
}
