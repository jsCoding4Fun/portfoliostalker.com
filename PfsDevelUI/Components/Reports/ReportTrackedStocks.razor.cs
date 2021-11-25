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
    public partial class ReportTrackedStocks
    {
        [Inject] PfsClientAccess PfsClientAccess { get; set; }

        private List<ViewReportTrackedStocksData> _viewReport;

        protected bool _showDayColumn;    // If all normal, == all has latest EOD and no Intraday data included.. then Day is not even shown

        protected bool _anythingToDelete; // 'Delete' column w button to delete is only shown if has stocks those wo dependencies so that they can be deleted

        protected string _headerTextName = string.Empty;

        protected override void OnParametersSet()
        {
            RefreshReport();
        }

        protected void RefreshReport()
        {
            _showDayColumn = false;
            _anythingToDelete = false;
            _viewReport = new();
            List<ReportTrackedStocksData> reportData = PfsClientAccess.Report().GetTrackedStocksData();

            foreach (ReportTrackedStocksData inData in reportData)
            {
                ViewReportTrackedStocksData outData = new()
                {
                    d = inData,
                    allowDelete = true,
                };

                if (inData.AnyPfHoldings.Count >= 1 || inData.AnySgTracking.Count >= 1)
                    outData.allowDelete = false;

                if (outData.allowDelete == true)
                    _anythingToDelete = true;

                if (outData.d.IsUpToDate == false || outData.d.IsIntraday == true)
                    _showDayColumn = true;

                _viewReport.Add(outData);
            }

            _headerTextName = string.Format("Name (total {0} stocks)", _viewReport.Count());
        }

        private void DoDeleteStock(Guid STID)
        {
            PfsClientAccess.StalkerMgmt().RemoveStockTrackingAsync(STID);

            RefreshReport();
            StateHasChanged();
        }

        protected class ViewReportTrackedStocksData
        {
            public ReportTrackedStocksData d;

            public bool allowDelete;
        }
    }
}
