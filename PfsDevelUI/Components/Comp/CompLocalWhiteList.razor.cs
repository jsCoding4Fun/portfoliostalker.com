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

namespace PfsDevelUI.Components
{
    // Allows locally add exception for Market fetch logic so that specific individual stock is fetch w different provider
    public partial class CompLocalWhiteList
    {
        [Inject] PfsClientAccess PfsClientAccess { get; set; }
        [Inject] PfsClientPlatform PfsClientPlatform { get; set; }
        [Inject] IDialogService Dialog { get; set; }

        protected List<ExtDataProviders> _providers = null;

        protected List<ViewWhiteList> _whiteListStocks = null;

        protected override void OnInitialized()
        {
            SettMarketProviders configs = PfsClientAccess.Account().GetLocalMarketProviders();

            _providers = PfsClientPlatform.GetClientProviderIDs(ExtDataProviderJobType.EndOfDay);

            _whiteListStocks = new();

            foreach (KeyValuePair<Guid, ExtDataProviders> stock in configs.WhiteListedStocks)
            {
                AddStock(stock.Key, stock.Value);
            }
        }

        protected bool AddStock(Guid STID, ExtDataProviders provider)
        {
            StockMeta stockMeta = PfsClientAccess.StalkerMgmt().GetStockMeta(STID);

            if (stockMeta == null)
                return false;

            _whiteListStocks.Add(new ViewWhiteList()
            {
                STID = stockMeta.STID,
                Ticker = stockMeta.Ticker,
                MarketID = stockMeta.MarketID,
                Name = stockMeta.Name,
                Provider = provider,
            });
            return true;
        }

        private void OnBtnDelete(Guid STID)
        {
            _whiteListStocks.RemoveAll(s => s.STID == STID);
            StateHasChanged();
        }

        private async Task OnBtnTestAsync(Guid STID)
        {
            var parameters = new DialogParameters();
            parameters.Add("STID", STID);
            parameters.Add("UseCase", DlgTestStockFetch.UseCaseID.LOCAL);

            // !!!NOTE!!! MainLayout has default's for options
            var dialog = Dialog.Show<DlgTestStockFetch>("", parameters);
            await dialog.Result;
        }

        protected async Task OnBtnAddAsync()
        {
            var parameters = new DialogParameters();
            parameters.Add("AllowAddNewCompanies", false);

            var dialog = Dialog.Show<DlgStockSelect>("", parameters);
            var result = await dialog.Result;

            if (!result.Cancelled)
            {
                Guid STID;
                Guid.TryParse(result.Data.ToString(), out STID);

                if (STID != Guid.Empty)
                {
                    if (_whiteListStocks.Any(s => s.STID == STID) == true )
                        // Duplicate...
                        return;

                    AddStock(STID, ExtDataProviders.Unknown);

                    StateHasChanged();
                }
            }
        }

        protected void OnBtnSave()
        {
            // Get current settings
            SettMarketProviders configs = PfsClientAccess.Account().GetLocalMarketProviders();

            // And recreate always full list
            configs.WhiteListedStocks = new();

            // And overwrite this sett components effected fields w user selections
            foreach (ViewWhiteList stock in _whiteListStocks)
            {
                configs.WhiteListedStocks.Add(stock.STID, stock.Provider);
            }

            // And save back...
            PfsClientAccess.Account().SetLocalMarketProviders(configs);
        }

        public class ViewWhiteList
        {
            public MarketID MarketID { get; set; }
            public string Ticker { get; set; }
            public string Name { get; set; }
            public Guid STID { get; set; }

            public ExtDataProviders Provider { get; set; }
        }
    }
}
