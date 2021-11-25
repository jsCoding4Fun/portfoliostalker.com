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

namespace PfsDevelUI.Components
{
    public partial class DlgStockSelect
    {
        [Inject] PfsClientAccess PfsClientAccess { get; set; }

        [CascadingParameter] MudDialogInstance MudDialog { get; set; }

        [Parameter] public bool AllowAddNewCompanies { get; set; } = true;

        /* Dialog: StockSelect, designed to be used for StockGroup to add stocks on list
         * 
         * - Has two behaviour modes:
         *   1) By default add's from SupportedStock's list, so search limited to stocks user is already tracking
         *   
         *   2) Changing 'all companies' allows to add totally new company to list but also adds it to TrackedStocks same time
         */

        protected bool _allCompanies { get; set; } = false;

        protected bool _fullscreen { get; set; } = false;

        protected string _addButton { get; set; } = "Add";

        

        List<MarketMeta> _markets = null;
        List<StockMeta> _viewedStocks = null;
        string _search = "";

        MarketMeta _selMarket = null;
        object m_selStockTicker;

        protected override void OnInitialized()
        {
            // All markets available on selection list
            _markets = PfsClientAccess.Fetch().GetMarketMeta(true/*configuredOnly*/);
        }

        public async Task MarketSelectionChangedAsync(MarketMeta market)
        {
            _selMarket = market;

            // Re-using function thats also event triggered by search box
            await UpdateStocks();
        }

        protected async Task OnSearchChangedAsync(string search)
        {
            await UpdateStocks();
        }

        protected async Task OnSearchRangeChangedAsync(bool allCompanies)
        {
            _allCompanies = allCompanies;

            if ( allCompanies == true )
            {
                _addButton = "Add & Track";
            }
            else
            {
                _addButton = "Add";
            }

            await UpdateStocks();
        }

        protected async Task UpdateStocks()
        {
            _viewedStocks = null;

            if ( string.IsNullOrWhiteSpace(_search) == true )
                return;

            if ( _allCompanies == true && _selMarket != null) // Going online search always requires market to be defined
            {
                List<CompanyMeta> searchCompanies = await PfsClientAccess.Fetch().SearchCompaniesAsync(_selMarket.ID, _search);

                _viewedStocks = searchCompanies.ConvertAll(s => new StockMeta()
                {
                    MarketID = _selMarket.ID,
                    Ticker = s.Ticker,
                    Name = s.CompanyName,
                    STID = Guid.Empty,
                });
            }
            else if (_allCompanies == false) // Only from existing SupportedStocks
            {
                if (_selMarket != null) // If market is defined then returns wider array of stocks
                {
                    List<StockMeta> allSearchTracked = PfsClientAccess.StalkerMgmt().GetTrackedStocks(_search).ToList();
                    _viewedStocks = allSearchTracked.Where(s => s.MarketID == _selMarket.ID).ToList();
                }
                else // If market is NOT defined, then returns something only if ticker is perfect match (=> fast 'my stocks' search w just ticker)
                {
                    List<StockMeta> allSearchTracked = PfsClientAccess.StalkerMgmt().GetTrackedStocks(_search).ToList();

                    if (allSearchTracked.Count() >= 1 && allSearchTracked[0].Ticker == _search.ToUpper())
                    {
                        _viewedStocks = new();
                        _viewedStocks.Add(allSearchTracked[0]);
                    }
                }
            }
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

        private async Task DlgAddStock()
        {
            if (m_selStockTicker == null || string.IsNullOrWhiteSpace(m_selStockTicker.ToString()) == true)
                return;

            Guid STID = Guid.Empty;

            if (_allCompanies == true )
            {
                if (_selMarket == null)
                    return;

                StockMeta stockMeta = _viewedStocks.Single(s => s.Ticker == m_selStockTicker.ToString());

                STID = await PfsClientAccess.StalkerMgmt().AddStockTrackingAsync(_selMarket.ID, m_selStockTicker.ToString(), stockMeta.Name);
            }
            else if (_allCompanies == false )
            {
                STID = _viewedStocks.FirstOrDefault(x => x.Ticker == m_selStockTicker.ToString()).STID;
            }

            MudDialog.Close(DialogResult.Ok(STID));
        }
    }
}
