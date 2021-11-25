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
    public partial class DlgPrivSrvFetchData
    {
        [Inject] PfsClientAccess PfsClientAccess { get; set; }
        [Inject] IDialogService Dialog { get; set; }
        [CascadingParameter] MudDialogInstance MudDialog { get; set; }

        [Parameter] public MarketID MarketID { get; set; }  // These fill automatically per caller pages 'DialogParameters' 
        [Parameter] public string Ticker { get; set; }
        [Parameter] public Guid STID { get; set; }
        [Parameter] public DateTime? StartEOD { get; set; }  // This is 'current start of existing data' so by default doesnt overlap old data

        protected string _title = "";

        protected bool _fullscreen { get; set; } = false;

        protected DateTime _eodMinDay = new DateTime(DateTime.Now.AddYears(-10).Year, 1, 1);

        protected DateTime? _eodFromDay = new DateTime(DateTime.Now.AddYears(-5).Year,1,1);
        protected DateTime? _eodToDay = DateTime.Now;

        protected override void OnInitialized()
        {
            _title = MarketID.ToString() + " $" + Ticker;

            if (StartEOD != null && StartEOD.Value.Date > _eodFromDay)
                // Proposing by default to only fetch data 
                _eodToDay = StartEOD.Value.Date;
        }

        protected void OnFullScreenChanged(bool fullscreen)     // !!!TODO!!! Those dang header icons overlap atm, one from mud one of my.. push my left..
        {
            _fullscreen = fullscreen;

            MudDialog.Options.FullWidth = _fullscreen;
            MudDialog.SetOptions(MudDialog.Options);
        }

        private async Task DlgTestAsync()
        {
            var parameters = new DialogParameters();
            parameters.Add("STID", STID);
            parameters.Add("UseCase", DlgTestStockFetch.UseCaseID.PRIV_SERV);

            // !!!NOTE!!! MainLayout has default's for options
            var dialog = Dialog.Show<DlgTestStockFetch>("", parameters);
            var result = await dialog.Result;

            if (!result.Cancelled)
            {

            }
        }

        private void DlgCancel()
        {
            MudDialog.Cancel();
        }

        private async Task DlgFetchAsync()
        {
            bool success = await PfsClientAccess.PrivSrvMgmt().StockExtFetchData(STID, _eodFromDay.Value, _eodToDay.Value);

            if ( success == false )
            {
                bool? result = await Dialog.ShowMessageBox("Failed!", "Initiation of fetch failed", yesText: "Ok");
                return;
            }

            MudDialog.Close();
        }
    }
}
