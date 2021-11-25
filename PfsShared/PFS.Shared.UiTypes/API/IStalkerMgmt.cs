/*
 * Copyright (c) 2021 Jami Suni
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using PFS.Shared.Types;
using PFS.Shared.Stalker;
using System.Threading.Tasks;

namespace PFS.Shared.UiTypes
{
    // Provide's interface (for UI) to manage StalkerContent, thats pretty much core data for PFS.Client
    public interface IStalkerMgmt
    {
        IReadOnlyCollection<StockMeta> GetTrackedStocks(string search);

        // Note! Both versions of 'GetStockMeta' are local only, as they Stalker versions they dont go online but only look tracked stocks!
        StockMeta GetStockMeta(Guid STID);
        StockMeta GetStockMeta(MarketID marketID, string ticker);

        List<string> PortfolioNameList();
        List<string> StockGroupNameList(string pfName);

        string PortfolioOfStockGroup(string sgName);

        ReadOnlyCollection<StockAlarm> StockAlarmList(Guid STID);

        ReadOnlyCollection<StockOrder> StockOrderList(string pfName, Guid STID);

        ReadOnlyCollection<StockDivident> StockDividentList(string pfName, Guid STID);

        // This actually allows to add something client doesnt have atm, so pulls information from servers!
        Task<Guid> AddStockTrackingAsync(MarketID marketID, string ticker, string companyName);

        // Removes stock tracking from user, after this stock is not visible anywhere nor data is fetched for it
        Task RemoveStockTrackingAsync(Guid STID);

        // Allows outside of PFS.Client entities to use Stalker for preparation operations, a
        StalkerContent GetCopyOfStalker();

        // Perform single or set of actions against main Stalker object
        StalkerError DoActionSet(List<string> actionSet);
        StalkerError DoAction(string action);
    }
}
