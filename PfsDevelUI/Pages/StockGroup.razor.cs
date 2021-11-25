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

using PfsDevelUI.Components;

using PFS.Shared.Types;
using PFS.Shared.UiTypes;

namespace PfsDevelUI.Pages
{
    public partial class StockGroup
    {
        [Parameter] public string SgName { get; set; }

        [Inject] PfsUiState PfsUiState { get; set; }
        [Inject] PfsClientAccess PfsClientAccess { get; set; }
        [Inject] IDialogService Dialog { get; set; }
        [Inject] NavigationManager NavigationManager { get; set; }

        protected ReportStockTable _reportStockTable;

        protected bool _deleted = false;

        protected string _pfName = "";

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
                ID = SgMenuID.EDIT.ToString(),
                Text = "Sg Rename",
            });
            HeaderMenuItems.Add(new PageHeader.MenuItem()
            {
                ID = SgMenuID.DELETE.ToString(),
                Text = "Sg Delete",
            });
            HeaderMenuItems.Add(new PageHeader.MenuItem()
            {
                ID = SgMenuID.TOP.ToString(),
                Text = "Sg Top",
            });
        }

        protected override void OnParametersSet()
        {
            _pfName = PfsClientAccess.StalkerMgmt().PortfolioOfStockGroup(SgName);
        }

        protected async Task OnMenuSelEventAsync(string ID)
        {
            SgMenuID alarmType = (SgMenuID)Enum.Parse(typeof(SgMenuID), ID);

            switch (alarmType)
            {
                case SgMenuID.DELETE:
                    {
                        bool? result = await Dialog.ShowMessageBox("Are you sure?", "Remove stock group from portfolio?", yesText: "Ok", cancelText: "Cancel");

                        if (result.HasValue == false || result.Value == false)
                            return;

                        // Had to add this as keep crashing after delete
                        _deleted = true;

                        string action = string.Format("Delete-Group SgName=[{0}]", SgName);

                        if (PfsClientAccess.StalkerMgmt().DoAction(action) != StalkerError.OK)
                        {
                            await Dialog.ShowMessageBox("Delete failed!", "Cant delete Stock Group some reason, strange..", yesText: "Ok");
                        }
                        else
                        {
                            PfsUiState.UpdateNavMenu();

                            // Easiest just to move back to account page, as dont wanna end up trying to reload this nor quess where to go
                            NavigationManager.NavigateTo("/Account");
                        }
                    }
                    break;

                case SgMenuID.EDIT:
                    {
                        var parameters = new DialogParameters();
                        parameters.Add("PfName", _pfName);
                        parameters.Add("EditCurrSgName", SgName);

                        // !!!NOTE!!! MainLayout has default's for options
                        var dialog = Dialog.Show<DlgStockGroupEdit>("", parameters);
                        var result = await dialog.Result;

                        if (!result.Cancelled)
                        {
                            // From report point of view 'deleted' as name change to unknown...!!!LATER!!! Return name and update, atm who cares..
                            _deleted = true;

                            PfsUiState.UpdateNavMenu();

                            // Easiest just to move back to account page, as dont wanna end up trying to reload this nor quess where to go
                            NavigationManager.NavigateTo("/Account");
                        }
                    }
                    break;

                case SgMenuID.TOP:
                    {
                        string action = string.Format("Top-Group SgName=[{0}]", SgName);

                        if (PfsClientAccess.StalkerMgmt().DoAction(action) == StalkerError.OK)
                        {
                            PfsUiState.UpdateNavMenu();
                            StateHasChanged();
                        }
                    }
                    break;
            }
        }

        private async Task OnPageHeaderAddStockAsync()
        {
            if (_isDemoAccount == true)
            {
                await Dialog.ShowMessageBox("Not supported!", "Sorry, demo account doesnt support adding new stocks.", yesText: "Ok");
                return;
            }

            // !!!NOTE!!! MainLayout has default's for DialogOptions
            var dialog = Dialog.Show<DlgStockSelect>("", new DialogOptions() {  });
            var result = await dialog.Result;

            if (!result.Cancelled)
            {
                Guid STID;
                Guid.TryParse(result.Data.ToString(), out STID);

                if ( STID != Guid.Empty )
                {
                    // Follow-Group SgName Stock
                    string cmd = string.Format("Follow-Group SgName=[{0}] Stock=[{1}]", SgName, STID);
                    PfsClientAccess.StalkerMgmt().DoAction(cmd);

                    _reportStockTable.ReloadReport();

                    StateHasChanged();
                }
            }
        }

        protected enum SgMenuID
        {
            UNKNOWN = 0,
            EDIT,
            DELETE,
            TOP,
        }
    }
}
