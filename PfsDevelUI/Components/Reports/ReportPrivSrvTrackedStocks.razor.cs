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

using PFS.Shared.UiTypes;

namespace PfsDevelUI.Components
{
    public partial class ReportPrivSrvTrackedStocks
    {
        [Inject] PfsClientAccess PfsClientAccess { get; set; }
        [Inject] IDialogService Dialog { get; set; }

        protected bool _allUpToDate { get; set; }

        protected bool _anythingToDelete;

        protected string _headerTextName = string.Empty;

        protected List<ViewPrivSrvReportTrackedStocks> _viewReport = null;

        /* !!!TODO!!! Soon get that 'holders' information there also, and if no one holding it show 'delete icon button' 
         *            instead on that column. Pretty much allowing to delete stock w confirmation that no one tracks.
         */

        protected override async Task OnParametersSetAsync()
        {
            await RefreshReportAsync();
        }

        protected async Task RefreshReportAsync()
        {
            _anythingToDelete = false;
            _allUpToDate = true; // LatestEOD column is only shown if one-or-more of stocks is NOT having latest information available

            string yourUsername = PfsClientAccess.Account().Property("USERNAME");

            List<PrivSrvReportTrackedStocks> report = await PfsClientAccess.PrivSrvMgmt().ReportTrackedStocksAsync();

            _viewReport = new();

            foreach (PrivSrvReportTrackedStocks stock in report)
            {
                ViewPrivSrvReportTrackedStocks entry = new()
                {
                    d = stock,
                    HoldersLabel = string.Empty,
                };

                if (entry.d.UsersTracking.Count() >= 2)
                {
                    if (entry.d.UsersTracking.Contains(yourUsername) == true)
                        entry.HoldersLabel = string.Format("You + {0} people", entry.d.UsersTracking.Count() - 1);
                    else
                        entry.HoldersLabel = string.Format("{0} people", entry.d.UsersTracking.Count());
                }

                if (entry.d.IsUpToDate == false)
                    // Even one being late, causes extra column to be shown
                    _allUpToDate = false;

                if (entry.d.UsersTracking.Count() == 0)
                    // Nobody tracking this stock, so show delete button
                    _anythingToDelete = true;

                _viewReport.Add(entry);
            }

            _headerTextName = string.Format("Name (total {0} stocks)", _viewReport.Count());
        }

        private async Task DoDeleteStockAsync(Guid STID)
        {
            await PfsClientAccess.PrivSrvMgmt().RemoveTrackedStockAsync(STID);

            await RefreshReportAsync();
            StateHasChanged();
        }

        protected async Task OnBtnLaunchDlgFetchData(Guid STID)
        {
            /* !!!TODO!!! Find record w STID, add few parameters to be passed in STID, Start/End days... Market Ticker Stock Name
             * 
             * Have fields those show existing data period
             * 
             * Date Selection for start / end days
             * 
             * Dual switch later for EOD / EOW w own default start date
             * 
             * 
             * 
             */

            PrivSrvReportTrackedStocks stock = _viewReport.First(s => s.d.StockMeta.STID == STID).d;

            var parameters = new DialogParameters();
            parameters.Add("MarketID", stock.StockMeta.MarketID);
            parameters.Add("Ticker", stock.StockMeta.Ticker);
            parameters.Add("STID", STID);
            
            if (stock.OldestEOD.HasValue )
                parameters.Add("StartEOD", stock.OldestEOD.Value);

            // !!!NOTE!!! MainLayout has default's for options
            var dialog = Dialog.Show<DlgPrivSrvFetchData>("", parameters);
            var result = await dialog.Result;

            if (!result.Cancelled)
            {

            }
        }
    }

    public class ViewPrivSrvReportTrackedStocks
    {
        public PrivSrvReportTrackedStocks d;

        public string HoldersLabel;
    }
}
