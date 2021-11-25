/*
 * Copyright (c) 2021 Jami Suni
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections.ObjectModel;

using PFS.Shared.Types;
using PFS.Shared.UiTypes;
using PFS.Shared.Stalker;

namespace PFS.Shared.TraceAPIs
{
    // Work as wrapper front of Stalker handling to convert all events to string format for logging example
    public class TraceStalkerMgmt : IStalkerMgmt
    {
        public event EventHandler<string> ParsingEvent;

        protected IStalkerMgmt _forward;

        public TraceStalkerMgmt(ref IStalkerMgmt forward)
        {
            _forward = forward;
        }

        public IReadOnlyCollection<StockMeta> GetTrackedStocks(string search)
        {
            IReadOnlyCollection<StockMeta> ret = _forward.GetTrackedStocks(search);

            string line = string.Format("!S \x1F GetTrackedStocks \x1F search={0}", search);

            line += Environment.NewLine + "^ ret:";

            foreach ( StockMeta stockMeta in ret )
                line += Environment.NewLine + "^ " + stockMeta.ToDebugString();

            ParsingEvent?.Invoke(this, line);

            return ret;
        }

        public StockMeta GetStockMeta(Guid STID)
        {
            StockMeta ret = _forward.GetStockMeta(STID);

            string line = string.Format("!S \x1F GetStockMeta \x1F STID={0}", STID);

            if (ret == null)
                line += Environment.NewLine + "^ ret: (null)";
            else
                line += Environment.NewLine + "^ ret:" + ret.ToDebugString();

            ParsingEvent?.Invoke(this, line);

            return ret;
        }

        public StockMeta GetStockMeta(MarketID marketID, string ticker)
        {
            StockMeta ret = _forward.GetStockMeta(marketID, ticker);

            string line = string.Format("!S \x1F GetStockMeta \x1F marketID={0} \x1F ticker={1}", marketID, ticker);

            if (ret == null )
                line += Environment.NewLine + "^ ret: (null)";
            else
                line += Environment.NewLine + "^ ret:" + ret.ToDebugString();

            ParsingEvent?.Invoke(this, line);

            return ret;
        }

        public List<string> PortfolioNameList()
        {
            List<string> ret = _forward.PortfolioNameList();

            string line = string.Format("!S \x1F PortfolioNameList");

            line += Environment.NewLine + "^ ret:";

            foreach (string pfName in ret)
                line += Environment.NewLine + "^ " + pfName;

            ParsingEvent?.Invoke(this, line);

            return ret;
        }

        public List<string> StockGroupNameList(string pfName)
        {
            List<string> ret = _forward.StockGroupNameList(pfName);

            string line = string.Format("!S \x1F StockGroupNameList \x1F pfName={0}", pfName);

            line += Environment.NewLine + "^ ret:";

            foreach (string sgName in ret)
                line += Environment.NewLine + "^ " + sgName;

            ParsingEvent?.Invoke(this, line);

            return ret;
        }

        public string PortfolioOfStockGroup(string sgName)
        {
            string ret = _forward.PortfolioOfStockGroup(sgName);

            string line = string.Format("!S \x1F PortfolioOfStockGroup \x1F sgName={0}", sgName);

            line += Environment.NewLine + "^ ret:" + ret;

            ParsingEvent?.Invoke(this, line);

            return ret;
        }

        public ReadOnlyCollection<StockAlarm> StockAlarmList(Guid STID)
        {
            ReadOnlyCollection<StockAlarm> ret = _forward.StockAlarmList(STID);

            string line = string.Format("!S \x1F StockAlarmList \x1F STID={0}", STID);

            line += Environment.NewLine + "^ ret:";

            foreach (StockAlarm stockAlarm in ret)
                line += Environment.NewLine + "^ " + stockAlarm.ToDebugString();

            ParsingEvent?.Invoke(this, line);

            return ret;
        }

        public ReadOnlyCollection<StockOrder> StockOrderList(string pfName, Guid STID)
        {
            ReadOnlyCollection<StockOrder> ret = _forward.StockOrderList(pfName, STID);

            string line = string.Format("!S \x1F StockOrderList \x1F pfName={0} \x1F STID={1}", pfName, STID);

            line += Environment.NewLine + "^ ret:";

            foreach (StockOrder stockOrder in ret)
                line += Environment.NewLine + "^ " + stockOrder.ToDebugString();

            ParsingEvent?.Invoke(this, line);

            return ret;
        }

        public ReadOnlyCollection<StockDivident> StockDividentList(string pfName, Guid STID)
        {
            ReadOnlyCollection<StockDivident> ret = _forward.StockDividentList(pfName, STID);

            string line = string.Format("!S \x1F StockDividentList \x1F pfName={0} \x1F STID={1}", pfName, STID);

            line += Environment.NewLine + "^ ret:";

            foreach (StockDivident stockDivident in ret)
                line += Environment.NewLine + "^ " + stockDivident.ToDebugString();

            ParsingEvent?.Invoke(this, line);

            return ret;
        }

        public async Task<Guid> AddStockTrackingAsync(MarketID marketID, string ticker, string companyName)
        {
            Guid ret = await _forward.AddStockTrackingAsync(marketID, ticker, companyName);

            string line = string.Format("!S \x1F AddStockTrackingAsync \x1F marketID={0} \x1F ticker={1} \x1F sgName={2}", marketID.ToString(), ticker, companyName);

            line += Environment.NewLine + "^ ret:" + ret;

            ParsingEvent?.Invoke(this, line);

            return ret;
        }

        public async Task RemoveStockTrackingAsync(Guid STID)
        {
            await _forward.RemoveStockTrackingAsync(STID);

            string line = string.Format("!S \x1F RemoveStockTrackingAsync \x1F STID={0}", STID);

            ParsingEvent?.Invoke(this, line);
        }

        public StalkerContent GetCopyOfStalker()
        {
            StalkerContent ret =  _forward.GetCopyOfStalker();

            string line = string.Format("!S \x1F GetCopyOfStalker");

            line += Environment.NewLine + "^ ret: (StalkerContent Not Included)";

            ParsingEvent?.Invoke(this, line);

            return ret;
        }

        public StalkerError DoActionSet(List<string> actionSet)
        {
            StalkerError ret = _forward.DoActionSet(actionSet);

            string line = string.Format("!S \x1F DoActionSet \x1F actionSet=[List Not Included]");          // !!!TODO!!! !!!THINK!!! JSON??

            line += Environment.NewLine + "^ ret: " + ret.ToString();

            ParsingEvent?.Invoke(this, line);

            return ret;
        }

        public StalkerError DoAction(string action)
        {
            StalkerError ret = _forward.DoAction(action);

            string line = string.Format("!S \x1F DoAction \x1F action={0}", action);

            line += Environment.NewLine + "^ ret: " + ret.ToString();

            ParsingEvent?.Invoke(this, line);

            return ret;
        }
    }
}
