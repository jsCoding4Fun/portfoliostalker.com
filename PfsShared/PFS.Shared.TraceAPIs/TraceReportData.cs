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
using PFS.Shared.UiTypes;

namespace PFS.Shared.TraceAPIs
{
    public class TraceReportData : IReportData
    {
        public event EventHandler<string> ParsingEvent;

        protected IReportData m_forward;

        protected TraceSymbols m_symbols;

        public TraceReportData(ref IReportData forward, ref TraceSymbols symbols)
        {
            m_symbols = symbols;
            m_forward = forward;
        }

        public (List<ReportExportHoldingsData> stocks, ReportExportHoldingsData total, List<ReportExportHoldingsData> groupTotals)
            GetExportHoldingsData(string pfName)
        {
            var ret = m_forward.GetExportHoldingsData(pfName);

            string line = string.Format("!R GetExportHoldingsData !!!TODO!!!");

            // !!!LATER!!! Missing data output

            ParsingEvent?.Invoke(this, line);

            return ret;
        }

        public List<ReportTrackedStocksData> GetTrackedStocksData()
        {
            List<ReportTrackedStocksData> ret = m_forward.GetTrackedStocksData();

            string line = string.Format("!R GetSupportedStocksData");

            // !!!LATER!!! Missing data output

            ParsingEvent?.Invoke(this, line);

            return ret;
        }

        public List<ReportUserEventsData> GetUserEventsData(string pfName)
        {
            List<ReportUserEventsData> ret = m_forward.GetUserEventsData(pfName);

            string line = string.Format("!R GetSupportedStocksData pfName=[" + pfName + "]");

            // !!!LATER!!! Missing data output

            ParsingEvent?.Invoke(this, line);

            return ret;
        }


        public void UpdateUserEventMode(int eventID, UserEventMode mode)
        {
            m_forward.UpdateUserEventMode(eventID, mode);

            string line = string.Format("!R UpdateUserEventMode EVID todoMode");

            ParsingEvent?.Invoke(this, line);
        }

        public void DeleteUserEvent(int eventID)
        {
            m_forward.DeleteUserEvent(eventID);

            string line = string.Format("!R DeleteUserEvent EVID");

            ParsingEvent?.Invoke(this, line);
        }

        public List<ReportStockTableData> GetPortfolioStockLatest(string pfName)
        {
            List<ReportStockTableData> stockLatestData = m_forward.GetPortfolioStockLatest(pfName);

            string line = string.Format("!R GetPortfolioStockLatest pfName=[" + pfName + "]");

            AddReportGroupData(stockLatestData, ref line);

            ParsingEvent?.Invoke(this, line);

            return stockLatestData;
        }

        public List<ReportStockTableData> GetStockGroupLatest(string sgName)
        {
            List<ReportStockTableData> stockLatestData = m_forward.GetStockGroupLatest(sgName);

            string line = string.Format("!R GetStockGroupLatest sgName=[" + sgName + "]");

            AddReportGroupData(stockLatestData, ref line);

            ParsingEvent?.Invoke(this, line);

            return stockLatestData;
        }

        public List<ReportStockTableData> GetPortfolioWatchStockLatest(string pfName)
        {
            List<ReportStockTableData> stockLatestData = m_forward.GetPortfolioWatchStockLatest(pfName);

            string line = string.Format("!R GetPortfolioWatchStockLatest pfName=[" + pfName + "]");

            AddReportGroupData(stockLatestData, ref line);

            ParsingEvent?.Invoke(this, line);

            return stockLatestData;
        }

        public List<ReportHoldingsData> GetHoldingsData(string pfName)
        {
            List<ReportHoldingsData> holdingsData = m_forward.GetHoldingsData(pfName);

            string line = string.Format("!R GetHoldingsData pfName=[" + pfName + "]");

            //AddReportGroupData(stockLatestData, ref line);

            ParsingEvent?.Invoke(this, line);

            return holdingsData;
        }

        public (ReportInvestedHeader header, List<ReportInvestedData> stocks) GetInvestedData(string pfName)
        {
            (ReportInvestedHeader header, List<ReportInvestedData> investedStocks) = m_forward.GetInvestedData(pfName);

            string line = string.Format("!R GetInvestedData pfName=[" + pfName + "]");

            //AddReportGroupData(stockLatestData, ref line);

            ParsingEvent?.Invoke(this, line);

            return (header: header, stocks: investedStocks);
        }

        public List<ReportTradeData> GetTradeData(string pfName, DateTime fromDate, DateTime toDate, Guid STID)
        {
            List<ReportTradeData> ret  = m_forward.GetTradeData(pfName, fromDate, toDate, STID);

            string line = string.Format("!R GetTradeData pfName=[" + pfName + "]");

            //AddReportGroupData(stockLatestData, ref line);

            ParsingEvent?.Invoke(this, line);

            return ret;
        }

        protected void AddReportGroupData(List<ReportStockTableData> reportGroupData, ref string line)
        {
            foreach (ReportStockTableData data in reportGroupData)
            {
#if false
                line += Environment.NewLine + "^ " + data.Market_Date.ToString("yyyy-MM-dd") + " " + data.Market + " " + data.Ticker + " CL:"
                     + data.Market_Close.ToString("0.00");

                if (data.AlarmUnderP.HasValue)
                    line += " AlarmUnderP:" + data.AlarmUnderP.Value;

                if (data.AlarmOverP.HasValue)
                    line += " AlarmOverP:" + data.AlarmOverP.Value;
#endif
                line += " Company:" + data.Name;

                // !!!TODO!!! Missing a lot of fields!
            }
        }
    }
}
