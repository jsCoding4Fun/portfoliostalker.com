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
    public partial class DlgStockGroupEdit
    {
        [Inject] PfsClientAccess PfsClientAccess { get; set; }
        [Inject] private IDialogService Dialog { get; set; }

        [CascadingParameter] MudDialogInstance MudDialog { get; set; }
        [Parameter] public string PfName { get; set; }

        [Parameter] public string EditCurrSgName { get; set; } = string.Empty;

        protected bool _fullscreen { get; set; } = false;
        protected string _editSgName = string.Empty;


        protected override void OnInitialized()
        {
            _editSgName = EditCurrSgName;
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

        private async Task OnBtnEditAsync()
        {
            if (string.IsNullOrWhiteSpace(_editSgName) == true)
                return;

            // Edit-Group SgCurrName SgNewName
            string cmd = string.Format("Edit-Group SgCurrName=[{0}] SgNewName=[{1}]", EditCurrSgName, _editSgName);
            StalkerError err = PfsClientAccess.StalkerMgmt().DoAction(cmd);

            if (err == StalkerError.OK)
                MudDialog.Close();
            else
            {
                await Dialog.ShowMessageBox("Failed!", string.Format("Error: {0}", err.ToString()), yesText: "Ok");
            }
        }

        private async Task OnBtnAddAsync()
        {
            if (string.IsNullOrWhiteSpace(_editSgName) == true)
                return;

            string cmd = string.Format("Add-Group PfName=[{0}] SgName=[{1}]", PfName, _editSgName);

            // Add stock group under currently selected portfolio
            StalkerError err = PfsClientAccess.StalkerMgmt().DoAction(cmd);

            if (err == StalkerError.OK)
                MudDialog.Close();
            else
            {
                bool? result = await Dialog.ShowMessageBox("Failed!", string.Format("Error: {0}", err.ToString()), yesText: "Ok");
            }
        }
    }
}
