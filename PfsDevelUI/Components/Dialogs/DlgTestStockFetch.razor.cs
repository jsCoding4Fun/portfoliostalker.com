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

namespace PfsDevelUI.Components
{
    public partial class DlgTestStockFetch
    {
        [Inject] PfsClientAccess PfsClientAccess { get; set; }
        [CascadingParameter] MudDialogInstance MudDialog { get; set; }

        [Parameter] public Guid STID { get; set; }      // These fill automatically per caller pages 'DialogParameters' 

        [Parameter] public UseCaseID UseCase { get; set; } = UseCaseID.UNKNOWN;

        protected List<ExtDataProviders> _availableProviders = new();

        protected bool _fetchMode = true;

        List<MarketMeta> _markets = null;
        MarketMeta _selMarket = null;

        protected string _manualTicker = string.Empty;

        public enum UseCaseID
        {
            UNKNOWN = 0,
            LOCAL,          // Give STID then does automatical fetch. If STID not defined, then allows select market and enter manually any ticker
            PRIV_SERV,      // STID must be defined, always
        }

        protected string _content { get; set; } = string.Empty;

        protected override async Task OnInitializedAsync()
        {
            if (UseCase == UseCaseID.PRIV_SERV && STID != Guid.Empty )
            {
                await PerformTestFetch();
            }
            else if (UseCase == UseCaseID.LOCAL && STID != Guid.Empty)
            {
                await PerformTestFetch();
            }
            else if (UseCase == UseCaseID.LOCAL && STID == Guid.Empty)
            {
                _fetchMode = false;

                _markets = PfsClientAccess.Fetch().GetMarketMeta(true/*configuredOnly*/);
            }
            else
            {
                // invalid call, tuff luck...
            }
        }

        protected async Task PerformTestFetch()
        {
            MudDialog.Options.MaxWidth = MaxWidth.Medium;
            MudDialog.Options.FullWidth = true;
            MudDialog.Options.NoHeader = true;
            MudDialog.SetOptions(MudDialog.Options);

            if (UseCase == UseCaseID.PRIV_SERV)
                _content = await PfsClientAccess.PrivSrvMgmt().TestStockFetchAsync(STID);
            else
                _content = await PfsClientAccess.Fetch().TestStockFetchAsync(STID);
        }

        public void MarketSelectionChanged(MarketMeta market)
        {
            _selMarket = market;
        }

        protected async Task DlgManualTestAsync()
        {
            if (_selMarket == null || string.IsNullOrWhiteSpace(_manualTicker) == true)
                return;

            _fetchMode = true;

            MudDialog.Options.MaxWidth = MaxWidth.Medium;
            MudDialog.Options.FullWidth = true;
            MudDialog.Options.NoHeader = true;
            MudDialog.SetOptions(MudDialog.Options);

            _content = await PfsClientAccess.Fetch().TestStockFetchAsync(_selMarket.ID, _manualTicker);
        }

        private void DlgCancel()
        {
            MudDialog.Cancel();
        }
    }
}
