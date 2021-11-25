/*
 * Copyright (c) 2021 Jami Suni
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.AspNetCore.Components.WebAssembly.Http;
using Microsoft.JSInterop;
using PfsDevelUI;
using PfsDevelUI.PFSLib;
using PfsDevelUI.Shared;
using MudBlazor;

using PFS.Shared.Types;
using PFS.Shared.UiTypes;
using System.Collections.Immutable;
using PFS.Shared.Common;

namespace PfsDevelUI.Components
{
    public partial class ReportUserEvents
    {
        [Parameter] public string PfName { get; set; } = string.Empty;
        
        [Inject] IDialogService Dialog { get; set; }
        [Inject] PfsClientAccess PfsClientAccess { get; set; }

        private List<ViewReportUserEventsData> _viewReport;

        protected override void OnParametersSet()
        {
            RefreshReport();
        }

        protected void RefreshReport()
        {
            _viewReport = new();
            List<ReportUserEventsData> reportData = PfsClientAccess.Report().GetUserEventsData(PfName);

            foreach (ReportUserEventsData inData in reportData)
            {
                ViewReportUserEventsData outData = new()
                {
                    d = inData,
                };

                switch ( outData.d.Type )
                {
                    case UserEventType.OrderExpired:
                        {
                            StockOrder stockOrder = (StockOrder)inData.Content;

                            if (stockOrder.Type == StockOrderType.Buy)
                                outData.Desc = string.Format("Buy {0}pcs with {1}{2}", stockOrder.Units, stockOrder.PricePerUnit, UiF.Curr(inData.Currency));
                            else if (stockOrder.Type == StockOrderType.Buy)
                                outData.Desc = string.Format("Sell {0}pcs with {1}{2}", stockOrder.Units, stockOrder.PricePerUnit, UiF.Curr(inData.Currency));

                            outData.Operation1 = "Re-open";
                        }
                        break;

                    case UserEventType.OrderBuy:
                        {
                            StockOrder stockOrder = (StockOrder)inData.Content;
                            outData.Desc = string.Format("Order purhace done? {0}pcs with {1}{2}", stockOrder.Units, stockOrder.PricePerUnit, UiF.Curr(inData.Currency));

                            outData.Operation1 = "To Holding";
                        }
                        break;

                    case UserEventType.OrderSell:
                        {
                            StockOrder stockOrder = (StockOrder)inData.Content;
                            outData.Desc = string.Format("Order sold now? {0}pcs with {1}{2}", stockOrder.Units, stockOrder.PricePerUnit, UiF.Curr(inData.Currency));

                            outData.Operation1 = "Process Sale";
                        }
                        break;

                    case UserEventType.AlarmOver:
                        {
                            ClientUserEvent.Alarm alarm = (ClientUserEvent.Alarm)inData.Content;

                            if (alarm.DayHigh.HasValue)
                                outData.Desc = string.Format("Stock visited at {0}{3} over alarm {1}{3}, but closed under it at {2}{3}", alarm.DayHigh.Value, alarm.AlarmValue, alarm.DayClosed, UiF.Curr(inData.Currency));
                            else
                                outData.Desc = string.Format("Stock closed to {0}{2} over alarm level {1}{2}", alarm.DayClosed, alarm.AlarmValue, UiF.Curr(inData.Currency));
                        }
                        break;

                    case UserEventType.AlarmUnder:
                        {
                            ClientUserEvent.Alarm alarm = (ClientUserEvent.Alarm)inData.Content;

                            if (alarm.DayLow.HasValue)
                                outData.Desc = string.Format("Stock visited at {0}{3} under alarm {1}{3}, but closed over it at {2}{3}", alarm.DayLow.Value, alarm.AlarmValue, alarm.DayClosed, UiF.Curr(inData.Currency));
                            else
                                outData.Desc = string.Format("Stock closed to {0}{2} under alarm level {1}{2}", alarm.DayClosed, alarm.AlarmValue, UiF.Curr(inData.Currency));
                        }
                        break;
                }

                // Get Icon per Event Mode
                outData.Icon = ModeIcons.Single(m => m.Item1 == outData.d.Mode).Item2;

                outData.TickerNavLink = "/Stock/" + outData.d.STID.ToString();

                _viewReport.Add(outData);
            }
        }

        protected async Task EvOperation1Async(ViewReportUserEventsData data)
        {
            switch (data.d.Type)
            {
                case UserEventType.OrderExpired:    // RE-OPEN (really use as default and create new)
                    {
                        // Note! Re-creates matching structure per Event's information, doesnt use original Order (nor require it existing anymore)
                        StockOrder stockOrder = (StockOrder)(data.d.Content);

                        var parameters = new DialogParameters();
                        parameters.Add("MarketID", data.d.MarketID);
                        parameters.Add("Ticker", data.d.Ticker);
                        parameters.Add("STID", data.d.STID);
                        parameters.Add("PfName", data.d.PfName);
                        parameters.Add("Defaults", stockOrder);
                        parameters.Add("Edit", false);              // Not edit actually! This is Add-New-As-Replacement

                        // !!!NOTE!!! MainLayout has default's for options
                        var dialog = Dialog.Show<DlgOrderEdit>("", parameters);
                        var result = await dialog.Result;

                        if (!result.Cancelled)
                        {
                            // Getting OK means user 'Re-open' or actually created new matching Order.. so delete Event.. looks like now w help of Reports() :/
                            PfsClientAccess.Report().DeleteUserEvent(data.d.EventID);

                            RefreshReport();
                            StateHasChanged();
                        }
                    }
                    break;

                case UserEventType.OrderBuy:      // TO HOLDING (uses Order info to Add Holding, and removed event/order)
                    {
                        StockOrder stockOrder = (StockOrder)(data.d.Content);

                        StockHolding defaults = new()
                        {
                            PricePerUnit = stockOrder.PricePerUnit,
                            PurhacedUnits = stockOrder.Units,
                            PurhaceDate = data.d.Date,
                        };

                        var parameters = new DialogParameters();
                        parameters.Add("Ticker", data.d.Ticker);
                        parameters.Add("STID", data.d.STID);
                        parameters.Add("PfName", PfName);
                        parameters.Add("Defaults", defaults);
                        parameters.Add("Edit", false);
                        parameters.Add("Currency", data.d.Currency);

                        // !!!NOTE!!! MainLayout has default's for options
                        var dialog = Dialog.Show<DlgHoldingsEdit>("", parameters);
                        var result = await dialog.Result;

                        if (!result.Cancelled)
                        {
                            // Filled Buy order converted to Holding, so event is handled and can be removed...
                            PfsClientAccess.Report().DeleteUserEvent(data.d.EventID);

                            // And Stock Order is also obsolete now that its filled, so also that can be removed
                            string cmd = string.Format("Delete-Order PfName=[{0}] Stock=[{1}] Price=[{2}]", PfName, data.d.STID, stockOrder.PricePerUnit);
                            PfsClientAccess.StalkerMgmt().DoAction(cmd);

                            RefreshReport();
                            StateHasChanged();
                        }
                    }
                    break;

                case UserEventType.OrderSell:
                    {
                        StockOrder stockOrder = (StockOrder)(data.d.Content);

                        StockTrade defaults = new()
                        {
                            PricePerUnit = stockOrder.PricePerUnit,
                            SoldUnits = stockOrder.Units,
                            SaleDate = data.d.Date,
                        };

                        var parameters = new DialogParameters();
                        parameters.Add("Ticker", data.d.Ticker);
                        parameters.Add("STID", data.d.STID);
                        parameters.Add("PfName", PfName);
                        parameters.Add("TargetHolding", null); // Just targeting PF/STID, w MaxUnits using FIFO 
                        parameters.Add("MaxUnits", (int)(stockOrder.Units));    // !!!LATER!!! This really isnt correct as should be per stalker remainings
                        parameters.Add("Currency", data.d.Currency);
                        parameters.Add("Defaults", defaults);

                        // Ala Sale Holding operation == finishing trade of buy holding, and now sell holding(s)
                        var dialog = Dialog.Show<DlgTradeFinish>("", parameters);
                        var result = await dialog.Result;

                        if (!result.Cancelled)
                        {
                            // Filled Buy order converted to Holding, so event is handled and can be removed...
                            PfsClientAccess.Report().DeleteUserEvent(data.d.EventID);

                            // And Stock Order is also obsolete now that its filled, so also that can be removed
                            string cmd = string.Format("Delete-Order PfName=[{0}] Stock=[{1}] Price=[{2}]", PfName, data.d.STID, stockOrder.PricePerUnit);
                            PfsClientAccess.StalkerMgmt().DoAction(cmd);

                            RefreshReport();
                            StateHasChanged();
                        }
                    }
                    break;
            }
        }

        protected void OnBtnSwapMode(ViewReportUserEventsData data)
        {
            UserEventMode mode = UserEventMode.Read;

            // Unread -> Read -> Starred -> UnreadImp -> Read -> Starred -> UnreadImp -> etc
            switch ( data.d.Mode )
            {
                case UserEventMode.Read: mode = UserEventMode.Starred; break;
                case UserEventMode.Starred: mode = UserEventMode.UnreadImp; break;
                case UserEventMode.Unread: mode = UserEventMode.Read; break;
                case UserEventMode.UnreadImp: mode = UserEventMode.Read; break;
            }

            PfsClientAccess.Report().UpdateUserEventMode(data.d.EventID, mode);
            RefreshReport();
            StateHasChanged();
        }

        protected void OnBtnDeleteEvent(ViewReportUserEventsData data)
        {
            PfsClientAccess.Report().DeleteUserEvent(data.d.EventID);
            RefreshReport();
            StateHasChanged();
        }

        protected void OnBtnDeleteAllEvents()
        {
            PfsClientAccess.Report().DeleteUserEvent(0);
            RefreshReport();
            StateHasChanged();
        }

        protected class ViewReportUserEventsData
        {
            public ReportUserEventsData d;

            public string Icon;

            public string Desc;

            public string Operation1;

            public string TickerNavLink;
        }

        protected readonly static ImmutableArray<Tuple<UserEventMode, string>> ModeIcons = ImmutableArray.Create(new Tuple<UserEventMode, string>[]
        {
            new Tuple<UserEventMode, string>( UserEventMode.Unread,     Icons.Material.Filled.Email),
            new Tuple<UserEventMode, string>( UserEventMode.UnreadImp,  Icons.Material.Filled.MarkEmailUnread),
            new Tuple<UserEventMode, string>( UserEventMode.Read,       Icons.Material.Filled.Check),
            new Tuple<UserEventMode, string>( UserEventMode.Starred,    Icons.Material.Filled.Star),
        });
    }
}
