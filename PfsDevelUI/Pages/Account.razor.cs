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
using PfsDevelUI.Components;

using MudBlazor;

using PFS.Shared.Types;
using PFS.Shared.UiTypes;

namespace PfsDevelUI.Pages
{
    public partial class Account
    {
        [Inject] PfsUiState PfsUiState { get; set; }
        [Inject] PfsClientAccess PfsClientAccess { get; set; }
        [Inject] PfsClientPlatform PfsClientPlatform { get; set; }
        [Inject] IDialogService Dialog { get; set; }

        private List<ViewMarketMeta> _markets = null;

        protected bool _allowIntraDay = false;
        protected bool _allowMarkets = false;

        protected override void OnInitialized()
        {
            AccountTypeID SessionAccountType = (AccountTypeID)Enum.Parse(typeof(AccountTypeID), PfsClientAccess.Account().Property("ACCOUNTTYPE"));

            switch (SessionAccountType)
            {
                case AccountTypeID.Platinum:
                    _allowIntraDay = true;
                    break;

                case AccountTypeID.Admin:
                    _allowIntraDay = true;
                    _allowMarkets = true;
                    break;
            }
        }

        protected override void OnParametersSet()
        {
            _markets = PfsClientAccess.Fetch().GetMarketMeta(true/*configuredOnly*/).ConvertAll(m => new ViewMarketMeta()
            {
                Meta = m,
                Closes = MarketCloses.Calculate(DateTime.UtcNow, m),
            });
        }

        public async Task OnLaunchIntradayDlgAsync()
        {
            if (_allowIntraDay == false)
                return;

            // !!!NOTE!!! MainLayout has default's for DialogOptions
            var dialog = Dialog.Show<DlgIntradayFetch>("", new DialogOptions() { });
            var result = await dialog.Result;

            if (!result.Cancelled)
            {
            }
        }

        private async Task OnPageHeaderOnAddPortfolioAsync()
        {
            // !!!NOTE!!! MainLayout has default's for options
            var dialog = Dialog.Show<DlgPortfolioEdit>();
            var result = await dialog.Result;

            if (!result.Cancelled)
            {
                PfsUiState.UpdateNavMenu();

                StateHasChanged();
            }
        }

        protected class ViewMarketMeta
        {
            public MarketMeta Meta { get; set; }
            public MarketCloses Closes { get; set; }
        }
    }
}
