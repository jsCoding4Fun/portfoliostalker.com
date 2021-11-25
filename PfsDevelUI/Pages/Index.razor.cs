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

using MudBlazor;

using PfsDevelUI.PFSLib;
using PfsDevelUI.Components;
using PfsDevelUI.Shared;

using PFS.Shared.UiTypes;

namespace PfsDevelUI.Pages
{
    public partial class Index
    {
        [Inject] PfsClientAccess PfsClientAccess { get; set; }
        [Inject] PfsUiState PfsUiState { get; set; }
        [Inject] IDialogService Dialog { get; set; }

        protected PageHeader _refChildPageHeader;

        protected TabNews _refChildTabNews;

        protected string _informationMsg = string.Empty;
        protected bool _showNews = true;
        protected bool _showDemoLinks = false;
        protected bool _disableDemoLinks = false;
        protected bool _showSetupWizard = false;

        protected override void OnParametersSet()
        {
            Update();
        }

        protected void Update()
        {
            AccountTypeID accountTypeID = (AccountTypeID)Enum.Parse(typeof(AccountTypeID), PfsClientAccess.Account().Property("ACCOUNTTYPE"));
            string username = PfsClientAccess.Account().Property("USERNAME");

            switch (accountTypeID)
            {
                case AccountTypeID.Unknown:
                    _informationMsg = string.Empty;
                    _showNews = false;
                    _showDemoLinks = true;
                    _disableDemoLinks = false;
                    break;

                case AccountTypeID.Demo:

                    _informationMsg = string.Empty;
                    _showNews = false;
                    _showDemoLinks = false;

                    switch (username)
                    {
                        case "DEMO":
                            _informationMsg = "Special notes for DEMO account... US$ base diidaadyy...";
                            break;

                        case "FINDEMO":
                            _informationMsg = "Saapuu, kunhan kerkee....joku vois tehda jotain..";
                            break;

                        case "MYDEMO":
                            _informationMsg = "Virtual retirement portfolio w 2040 target date. Details from purhaces on Discord, under MYDEMO channel!";
                            break;
                    }
                    break;

                default:

                    if (int.Parse(PfsClientAccess.Account().Property("UNREADNEWS")) == 0) // Making sure unread news is seen
                    {
                        _informationMsg =
                            "This site is running as a Browser's WebAssembly application, and that means data is hold, stored " +
                            "and most processing is done INSIDE of your BROWSER. When you cleanup your browsers Local Storage " +
                            "you are going to loose your data. As this is still BETA please use 'Export Backup' regularly " +
                            "to create your own local backup files. PFS server stores backups weekly or when your manual request it. " +
                            "We NEVER ask your password, but may need to ask your username in case you need help with your account.";
                    }
                    _showNews = true;
                    _showDemoLinks = false;

                    if (PfsClientAccess.Account().Property("ISCLEAN") == "TRUE")
                        // Looks like nothing is set, or CleanUp is done.. so offer wizard in case this is new user
                        _showSetupWizard = true;
                    break;
            }
        }

        protected void OnNewNewsByPageHeader()
        {
            // Get callback from PageHeader when new news is received (post login), and refresh news tab that case...
            if (_refChildTabNews != null )
                _refChildTabNews.Reload();
        }

        protected void OnEvLoginSuccessfull()
        {
            // Called from PageHeader when normal login is finished successfully..
            Update();
            StateHasChanged();
        }

        protected async Task OnBtnSetupWizardAsync()
        {
            if (PfsClientAccess.Account().Property("ISCLEAN") != "TRUE")
            {
                _showSetupWizard = false;
                return;
            }

            DialogOptions maxWidth = new DialogOptions() { MaxWidth = MaxWidth.Large, FullWidth = true };

            var parameters = new DialogParameters();

            var dialog = Dialog.Show<DlgSetupWizard>("", parameters, maxWidth);
            var result = await dialog.Result;

            if (!result.Cancelled)
            {
                _showSetupWizard = false;
                PfsUiState.UpdateNavMenu();
                StateHasChanged();
            }
        }

        protected async Task OnBtnLoginDemoAccountAsync(string accountName)
        {
            _disableDemoLinks = true;
            // Call PageHeader, as thats place where login codes are atm...
            bool success = await _refChildPageHeader.OnLoginAccountByCall(accountName);

            if ( success == false)
                _disableDemoLinks = false;
            
            Update();
            StateHasChanged();
        }
    }
}
