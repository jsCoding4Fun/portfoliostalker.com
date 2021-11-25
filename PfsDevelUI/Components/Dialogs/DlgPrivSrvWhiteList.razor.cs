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
using PfsDevelUI.Shared;
using PfsDevelUI.PFSLib;

using MudBlazor;

using PFS.Shared.Types;
using PFS.Shared.UiTypes;

namespace PfsDevelUI.Components
{
    public partial class DlgPrivSrvWhiteList
    {
        [Inject] PfsClientAccess PfsClientAccess { get; set; }

        [CascadingParameter] MudDialogInstance MudDialog { get; set; }

        // Note! Overly tiedup for generic use, but speeds up so much.. we pass this report as this is PrivSrv Admin operation, and cant 
        //       trust admin to have all stocks on her stalker.. but this report is having all stocks of PrivSrv and those are ones we 
        //       are searching from... so bingo!
        [Parameter] public List<PrivSrvReportTrackedStocks> TrackedStocksReport { get; set; } = null; // PfsClientAccess.PrivSrvMgmt().ReportTrackedStocksAsync();

        protected bool _fullscreen { get; set; } = false;

        List<MarketMeta> _markets = null;
        List<StockMeta> _viewedStocks = null;
        string _search = "";

        MarketMeta _selMarket = null;
        object _selStockTicker;

        protected override void OnInitialized()
        {
            // All markets available on selection list
            _markets = PfsClientAccess.Fetch().GetMarketMeta(false/*also unconfigured, as admin may not have all that PrivSrv has*/);
        }

        public void MarketSelectionChanged(MarketMeta market)
        {
            _selMarket = market;

            // Re-using function thats also event triggered by search box
            UpdateStocks();
        }

        protected void OnSearchChanged(string search)
        {
            UpdateStocks();
        }

        protected void UpdateStocks()
        {
            _viewedStocks = null;

            if (_selMarket == null || string.IsNullOrWhiteSpace(_search) == true )
                return;

            _viewedStocks = new();

            List<StockMeta> tickerMatch = TrackedStocksReport.Where(s => s.StockMeta.MarketID == _selMarket.ID && s.StockMeta.Ticker == _search.ToUpper()).Select(s => s.StockMeta).ToList();

            if (tickerMatch != null && tickerMatch.Count() == 1 )
                // Perfect match w ticker and market.. so this is always first one on list...
                _viewedStocks.AddRange(tickerMatch);

            List<StockMeta> nameMatch = TrackedStocksReport.Where(s => s.StockMeta.MarketID == _selMarket.ID && 
                                                                    s.StockMeta.Name.ToUpper().Contains(_search.ToUpper()))
                                                           .Select(s => s.StockMeta).ToList();

            if (nameMatch != null && nameMatch.Count() >= 1)
                // Plus all possible name matches from same market...
                _viewedStocks.AddRange(nameMatch);

            if (_viewedStocks.Count() == 0)
                _viewedStocks = null;
            else
                _viewedStocks = _viewedStocks.Distinct().ToList();
        }

        protected void OnFullScreenChanged(bool fullscreen)     // !!!TODO!!! Those dang header icons overlap atm, one from mud one of my.. push my left..
        {
            _fullscreen = fullscreen;

            MudDialog.Options.FullWidth = _fullscreen;
            MudDialog.SetOptions(MudDialog.Options);
        }

        private void DlgCancel()
        {
            MudDialog.Cancel();
        }

        private void DlgAddStock()
        {
            if (_selMarket == null || _selStockTicker == null || string.IsNullOrWhiteSpace(_selStockTicker.ToString()) == true )
                return;

            Guid STID = _viewedStocks.FirstOrDefault(x => x.MarketID == _selMarket.ID && x.Ticker == _selStockTicker.ToString()).STID;

            MudDialog.Close(DialogResult.Ok(STID));
        }
    }
}
