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

using PfsDevelUI.Shared;
using PfsDevelUI.Components;
using PfsDevelUI.PFSLib;

using MudBlazor;

using PFS.Shared.Types;
using PFS.Shared.UiTypes;

namespace PfsDevelUI.Pages
{
    public partial class Portfolio
    {
        [Inject] IDialogService Dialog { get; set; }
        [Inject] PfsUiState PfsUiState { get; set; }
        [Inject] PfsClientAccess PfsClientAccess { get; set; }
        [Inject] NavigationManager NavigationManager { get; set; }
        [Parameter] public string PfName { get; set; }

        protected List<PageHeader.MenuItem> HeaderMenuItems = new();

        protected bool _isDemoAccount = false;

        protected override void OnInitialized()
        {
            AccountTypeID SessionAccountType = (AccountTypeID)Enum.Parse(typeof(AccountTypeID), PfsClientAccess.Account().Property("ACCOUNTTYPE"));

            switch (SessionAccountType)
            {
                case AccountTypeID.Demo:
                    _isDemoAccount = true;
                    break;
            }

            HeaderMenuItems.Add(new PageHeader.MenuItem()
            {
                ID = PfMenuID.EDIT.ToString(),
                Text = "Pf Rename",
            });
            HeaderMenuItems.Add(new PageHeader.MenuItem()
            {
                ID = PfMenuID.DELETE.ToString(),
                Text = "Pf Delete",
            });
            HeaderMenuItems.Add(new PageHeader.MenuItem()
            {
                ID = PfMenuID.TOP.ToString(),
                Text = "Pf Top",
            });
        }

        protected async Task OnMenuSelEventAsync(string ID)
        {
            PfMenuID alarmType = (PfMenuID)Enum.Parse(typeof(PfMenuID), ID);

            switch ( alarmType )
            {
                case PfMenuID.DELETE:
                    {
                        bool? result = await Dialog.ShowMessageBox("Are you sure?", "Delete portfolio from account?", yesText: "Ok", cancelText: "Cancel");

                        if (result.HasValue == false || result.Value == false)
                            return;

                        string action = string.Format("Delete-Portfolio PfName=[{0}]", PfName);

                        if (PfsClientAccess.StalkerMgmt().DoAction(action) != StalkerError.OK)
                        {
                            await Dialog.ShowMessageBox("Delete failed!", "Cant delete portfolios those has ANY content / references!", yesText: "Ok");
                        }
                        else
                        {
                            PfsUiState.UpdateNavMenu();

                            // Easiest just to move back to account page, as dont wanna end up trying to reload this nor quess where to go
                            NavigationManager.NavigateTo("/Account");
                        }
                    }
                    break;

                case PfMenuID.EDIT:
                    {
                        var parameters = new DialogParameters();
                        parameters.Add("EditCurrPfName", PfName);
                        var dialog = Dialog.Show<DlgPortfolioEdit>("", parameters);
                        await dialog.Result;

                        PfsUiState.UpdateNavMenu();
                        StateHasChanged();
                    }
                    break;

                case PfMenuID.TOP:
                    {
                        string action = string.Format("Top-Portfolio PfName=[{0}]", PfName);

                        if (PfsClientAccess.StalkerMgmt().DoAction(action) == StalkerError.OK)
                        {
                            PfsUiState.UpdateNavMenu();
                            StateHasChanged();
                        }
                    }
                    break;
            }
        }

        protected async Task OnPageHeaderAddStockGroupAsync()
        {
            var parameters = new DialogParameters();
            parameters.Add("PfName", PfName);

            // !!!NOTE!!! MainLayout has default's for options
            var dialog = Dialog.Show<DlgStockGroupEdit>("", parameters);
            var result = await dialog.Result;

            if (!result.Cancelled)
            {
                PfsUiState.UpdateNavMenu();

                StateHasChanged();
            }
        }

        protected enum PfMenuID
        {
            UNKNOWN = 0,
            EDIT,
            DELETE,
            TOP,
        }
    }
}
