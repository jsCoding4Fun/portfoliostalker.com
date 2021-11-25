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
    public partial class DlgIntradayFetch
    {
        [Inject] PfsClientAccess PfsClientAccess { get; set; }
        [Inject] PfsClientPlatform PfsClientPlatform { get; set; }

        [CascadingParameter] MudDialogInstance MudDialog { get; set; }
        [Inject] private IDialogService Dialog { get; set; }

        protected bool _fullscreen { get; set; } = false;

        protected bool _isFetching = false;

        protected bool _waitFetching { get; set; } = false;

        protected List<MarketMeta> _markets = null;

        protected List<string> _portfolios = null;

        protected List<ExtDataProviders> _providers = null;

        protected string _selectedMarkets;

        protected string _selectedPortfolios;

        protected ExtDataProviders _selectedProvider = ExtDataProviders.Unknown;

        protected override void OnInitialized()
        {
            // All markets available on selection list
            _markets = PfsClientAccess.Fetch().GetMarketMeta(true/*configuredOnly*/);

            _portfolios = PfsClientAccess.StalkerMgmt().PortfolioNameList();

            _providers = PfsClientPlatform.GetClientProviderIDs(ExtDataProviderJobType.Intraday);
        }

        protected void OnFullScreenChanged(bool fullscreen)     // !!!TODO!!! Those dang header icons overlap atm, one from mud one of my.. push my left..
        {
            _fullscreen = fullscreen;

            MudDialog.Options.FullWidth = _fullscreen;
            MudDialog.SetOptions(MudDialog.Options);
        }

        protected void OnWaitChanged(bool wait)
        {
            _waitFetching = wait;
            StateHasChanged();
        }

        private void DlgCancel()
        {
            MudDialog.Cancel();
        }

        private async Task DlgFetch()
        {
            if ( string.IsNullOrWhiteSpace(_selectedMarkets) || string.IsNullOrWhiteSpace(_selectedPortfolios) || _selectedProvider == ExtDataProviders.Unknown)
            {
                bool? result = await Dialog.ShowMessageBox("Cant do!", "Select at least one market, and minimum one portfolio please?", yesText: "Ok");
                return;
            }

            if (_waitFetching)
            {
                _isFetching = true;
                StateHasChanged();

                await PfsClientAccess.Fetch().DoFetchLatestIntradayAsync(
                    _selectedMarkets.Split(','),
                    _selectedPortfolios.Split(','),
                    _selectedProvider);
            }
            else
            {
                _= PfsClientAccess.Fetch().DoFetchLatestIntradayAsync(
                    _selectedMarkets.Split(','),
                    _selectedPortfolios.Split(','),
                    _selectedProvider);
            }
            MudDialog.Close();
        }
    }
}
