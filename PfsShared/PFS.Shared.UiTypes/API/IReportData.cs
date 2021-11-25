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

namespace PFS.Shared.UiTypes
{
    // Provides data for locally generated reports
    public interface IReportData
    {
        List<ReportStockTableData> GetStockGroupLatest(string sgName);

        List<ReportStockTableData> GetPortfolioStockLatest(string pfName);   // merge of all Stock Groups under PF + holdings

        List<ReportTrackedStocksData> GetTrackedStocksData();

        // More export oriented report from currently owned stocks
        (List<ReportExportHoldingsData> stocks, ReportExportHoldingsData total, List<ReportExportHoldingsData> groupTotals)
            GetExportHoldingsData(string pfName);  // !!!TODO!!! Create separate 'PfsReportFilters' & 'PfsReportSorting' types!

        // Holdings & Invested presents current ownership of stocks, and gains of them per latest valuation (pfName = empty for all PF's)

        List<ReportHoldingsData> GetHoldingsData(string pfName);

        (ReportInvestedHeader header, List<ReportInvestedData> stocks) GetInvestedData(string pfName);

        // Trade presents previous closed trades. Buy-Holding-Sell is Trade (pfName must be defined, STID = Empty is for all Stocks)

        List<ReportTradeData> GetTradeData(string pfName, DateTime fromDate, DateTime toDate, Guid STID);

        //

        List<ReportStockTableData> GetPortfolioWatchStockLatest(string pfName); // All under Portfolio those actively on Alarm's Watch zone

        // Later! See if Event's could go their own Interface, maube with some meta etc fetching functionalities

        List<ReportUserEventsData> GetUserEventsData(string pfName);

        void UpdateUserEventMode(int eventID, UserEventMode mode);

        void DeleteUserEvent(int eventID);
    }
}
