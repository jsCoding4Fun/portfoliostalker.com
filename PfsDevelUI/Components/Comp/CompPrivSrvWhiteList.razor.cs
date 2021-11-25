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
    // Allows add exception to PrivSrv for Market fetch logic so that specific individual stock is fetch w different provider
    public partial class CompPrivSrvWhiteList
    {
        // !!!NOTE!!! Important: This is Admin feature, and as whitelist may very well contain stocks those specific
        //            user doesnt anymore have, nor never did have. As of this cant trust Stock/StockMeta of these 
        //            whitelist stocks to be found from localStalker! Instead all data shown per company needs to 
        //            be received from server itself. This is done atm by calling track report that provides all 
        //            required information, even if for way more stocks than required.

        [Inject] PfsClientAccess PfsClientAccess { get; set; }
        [Inject] IDialogService Dialog { get; set; }


        protected List<ViewWhiteList> _whiteListStocks = null;

        protected List<PrivSrvReportTrackedStocks> _privSrvTrackedStocks = null;    // Note! This is only allowed place to use here for company information!

        protected override async Task OnInitializedAsync()
        {
            SettMarketProviders configs = await PfsClientAccess.PrivSrvMgmt().ProviderConfigsGetAsync();

            if (configs == null)
            {
                // Cant connect to Priv Server.. show error & jump main screen      !!!TODO!!! Add NavMenu a JumpDefaultPage()
                return;
            }

            _privSrvTrackedStocks = await PfsClientAccess.PrivSrvMgmt().ReportTrackedStocksAsync();

            // And setup temporary list we using to control grid per configs

            _whiteListStocks = new();

            foreach (KeyValuePair<Guid, ExtDataProviders> stock in configs.WhiteListedStocks)
            //foreach (Tuple<Guid, ExtDataProviders> stock in configs.WhiteListedStocks)
            {
                AddStock(stock.Key, stock.Value);
            }
        }

        protected bool AddStock(Guid STID, ExtDataProviders provider)
        {
            PrivSrvReportTrackedStocks stock = _privSrvTrackedStocks.SingleOrDefault(s => s.StockMeta.STID == STID);

            if (stock == null)
                return false;

            _whiteListStocks.Add(new ViewWhiteList()
            {
                StockMeta = stock.StockMeta,
                Provider = provider,
            });
            return true;
        }

        private async Task OnBtnTestAsync(Guid STID)
        {
            StockMeta stockMeta = PfsClientAccess.StalkerMgmt().GetStockMeta(STID);

            var parameters = new DialogParameters();
            parameters.Add("STID", STID);
            parameters.Add("UseCase", DlgTestStockFetch.UseCaseID.PRIV_SERV);

            // !!!NOTE!!! MainLayout has default's for options
            var dialog = Dialog.Show<DlgTestStockFetch>("", parameters);
            await dialog.Result;
        }

        private void OnBtnDelete(Guid STID)
        {
            _whiteListStocks.RemoveAll(s => s.StockMeta.STID == STID);
            StateHasChanged();
        }

        protected async Task OnBtnAddAsync()
        {
            var parameters = new DialogParameters();
            parameters.Add("TrackedStocksReport", _privSrvTrackedStocks);   // Note! Passing full 'report' there as a source for Priv Srvs StockMeta info

            var dialog = Dialog.Show<DlgPrivSrvWhiteList>("", parameters);
            var result = await dialog.Result;

            if (!result.Cancelled)
            {
                Guid STID;
                Guid.TryParse(result.Data.ToString(), out STID);

                if (STID != Guid.Empty)
                {
                    if (_whiteListStocks.Any(s => s.StockMeta.STID == STID) == true )
                        // Duplicate...
                        return;

                    AddStock(STID, ExtDataProviders.Unknown);

                    StateHasChanged();
                }
            }
        }

        protected async Task OnBtnSaveAsync()
        {
            // Get current settings
            SettMarketProviders configs = await PfsClientAccess.PrivSrvMgmt().ProviderConfigsGetAsync();
            // And recreate always full list
            configs.WhiteListedStocks = new();

            // And overwrite this sett components effected fields w user selections
            foreach (ViewWhiteList stock in _whiteListStocks)
            {
                configs.WhiteListedStocks.Add(stock.StockMeta.STID, stock.Provider);
            }

            // And save back...
            if ( await PfsClientAccess.PrivSrvMgmt().ProviderConfigsSetAsync(configs) == true )
            {
                // msgbox?
            }
        }

        public class ViewWhiteList
        {
            public StockMeta StockMeta { get; set; }

            public ExtDataProviders Provider { get; set; }
        }
    }
}
