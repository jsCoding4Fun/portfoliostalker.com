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
using System.Collections.ObjectModel;
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

namespace PfsDevelUI.Components
{
    // Super simple component to be used on stock report's dropdown to view orders wo editing etc (used as per specific portfolio under are atm)
    public partial class StockMgmtOrders
    {
        [Inject] PfsClientAccess PfsClientAccess { get; set; }
        [Inject] IDialogService Dialog { get; set; }

        [Parameter] public Guid STID { get; set; }

        protected List<ViewStockOrders> _orders = new();

        protected override void OnInitialized()
        {
            RefreshReport();
        }

        protected void RefreshReport()
        {
            _orders = new();
            List<string> portfolios = PfsClientAccess.StalkerMgmt().PortfolioNameList();

            List<MarketMeta> allMarketMetas = PfsClientAccess.Fetch().GetMarketMeta();

            StockMeta stockMeta = PfsClientAccess.StalkerMgmt().GetStockMeta(STID);

            foreach (string pfName in portfolios)
            {
                ReadOnlyCollection<StockOrder> orders = PfsClientAccess.StalkerMgmt().StockOrderList(pfName, STID);

                foreach (StockOrder order in orders)
                {
                    _orders.Add(new ViewStockOrders()
                    {
                        Order = order,
                        PfName = pfName,
                        Currency = UiF.Curr(allMarketMetas.Single(m => m.ID == stockMeta.MarketID).Currency),
                    });
                }
            }
        }

        private async Task OnEditOrderAsync(ViewStockOrders viewOrder)
        {
            StockMeta stockMeta = PfsClientAccess.StalkerMgmt().GetStockMeta(STID);

            var parameters = new DialogParameters();
            parameters.Add("MarketID", stockMeta.MarketID);
            parameters.Add("Ticker", stockMeta.Ticker);
            parameters.Add("STID", STID);
            parameters.Add("PfName", viewOrder.PfName);
            parameters.Add("Defaults", viewOrder.Order);
            parameters.Add("Edit", true);

            // !!!NOTE!!! MainLayout has default's for options
            var dialog = Dialog.Show<DlgOrderEdit>("", parameters);
            var result = await dialog.Result;

            if (!result.Cancelled)
            {
                RefreshReport();
                StateHasChanged();
            }
        }

        protected class ViewStockOrders
        {
            public string PfName { get; set; }

            public StockOrder Order { get; set; }

            public string Currency { get; set; }
        }
    }
}
