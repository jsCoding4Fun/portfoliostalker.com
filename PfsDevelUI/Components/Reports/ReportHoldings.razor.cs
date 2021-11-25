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

using PFS.Shared.UiTypes;

namespace PfsDevelUI.Components
{
    // Note! This report isnt really really need atm as merged content as copy to ReportInvest's DropDown! Still used from StockMgmt but
    //       could someday do that one a way simpler version? Anyway let this be now here.. just note that potentially could be removed
    public partial class ReportHoldings
    {
        [Parameter] public string PfName { get; set; } = string.Empty;

        [Parameter] public Guid STID { get; set; } = Guid.Empty;

        [Inject] IDialogService Dialog { get; set; }
        [Inject] PfsClientAccess PfsClientAccess { get; set; }

        private List<ViewReportHoldingsData> _viewReport;

        protected bool _viewDividentColumn;

        protected override void OnParametersSet()
        {
            RefreshReport();
        }

        protected void RefreshReport()
        {
            _viewReport = new();
            List<ReportHoldingsData> reportData = PfsClientAccess.Report().GetHoldingsData(PfName);
            _viewDividentColumn = false;

            foreach (ReportHoldingsData inData in reportData)
            {
                if (STID != Guid.Empty && inData.STID != STID)
                    // !!!LATER!!! Atm we load everything from PFS, and then discard majority of it.. so later pass this STID on request also..
                    continue;

                ViewReportHoldingsData outData = new()
                {
                    d = inData,
                    SortOnInvested = inData.HcInvested.HasValue ? inData.HcInvested.Value : inData.Invested,

                };

                outData.Units = string.Format("{0} / {1}", inData.Holding.RemainingUnits, inData.Holding.PurhacedUnits);
                outData.Currency = UiF.Curr(inData.Currency);
                outData.HomeCurrency = UiF.Curr(inData.HomeCurrency);

                if (inData.DividentLast.HasValue)
                    _viewDividentColumn = true;

                _viewReport.Add(outData);
            }
        }

        private void ViewStockRequested(Guid STID)              // !!!TODO!!! Dead code???
        {
        }

        protected async Task OnBtnEditEventAsync(ViewReportHoldingsData data)
        {
            var parameters = new DialogParameters();
            parameters.Add("Ticker", data.d.Ticker);
            parameters.Add("STID", data.d.Holding.STID);
            parameters.Add("PfName", data.d.PfName);
            parameters.Add("Defaults", data.d.Holding);
            parameters.Add("Edit", true);
            parameters.Add("Currency", data.d.Currency);

            // !!!NOTE!!! MainLayout has default's for options
            var dialog = Dialog.Show<DlgHoldingsEdit>("", parameters);
            var result = await dialog.Result;

            if (!result.Cancelled)
            {
                // Looks like this was Converted to new Holding, so delete Order itself
            }
            RefreshReport();
            StateHasChanged();
        }

        protected class ViewReportHoldingsData
        {
            public ReportHoldingsData d;

            public string Units;

            public string Currency;
            public string HomeCurrency;

            public decimal SortOnInvested;
        }
    }
}
