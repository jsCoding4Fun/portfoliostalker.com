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
using PFS.Shared.UiTypes;


namespace PfsDevelUI.Components
{
    public partial class ReportTrades
    {
        /*!!!DOCUMENT!!! Sale/Trade feature
        - Under StockReport has SALE HOLDING -button, with FIFO base allowing sale max all remaining holdings
	        => Easy FIFO sale this amount logic, that automatically picks oldest Holdings to be sold 
        - Under InvestingReport has button for individual Holdings, to sell full/part of specific holding
	        => So can manually decide what part of specific holding is sold, w what price
        - Has own SalesHistoryReport, that allows to see already closed Trade's
        - Allowing cancel/delete Trade, and its rolling back holdings available.. but this is can of worms with
          FIFO logic on TradeAdd so need to be super carefull
        - Editing Trade's is NOT allowed, nor it should be allowed either, as its way too complex to avoid errors
         */
        [Parameter] public string PfName { get; set; } = string.Empty;
        
        [Inject] IDialogService Dialog { get; set; }
        [Inject] PfsClientAccess PfsClientAccess { get; set; }
        [Inject] NavigationManager NavigationManager { get; set; }

        private List<ViewReportTradeData> _viewReport = null;

        protected override void OnParametersSet()
        {
            _viewReport = null;

            ReloadReport();
        }

        protected void ReloadReport()
        {
            List<ReportTradeData> reportData = PfsClientAccess.Report().GetTradeData(PfName, 
            
                
                        new DateTime(2021,1,1), new DateTime(2022,1,1), Guid.Empty);


            _viewReport = new();

            foreach (ReportTradeData inData in reportData)
            {
                ViewReportTradeData outData = new()
                {
                    d = inData,
                    Currency = UiF.Curr(inData.Currency),
                    DualCurrency = UiF.Curr(inData.HomeCurrency),
                    AllowCancel = true,                             // !!!LATER!!! Is there way we could limit cancel timeframe to cause less problems...
                };                                                  // => really would need some hidden timestamp, to only allow cancel few days from add...

                // DropDown -start
                outData.ViewHoldings = new();

                foreach (ReportTradeHoldings inHoldings in inData.Holdings)
                {
                    ViewReportTradeHoldings outHoldings = new()
                    {
                        d = inHoldings,
                        // SortOnInvested = inData.DualInvested.HasValue ? inData.DualInvested.Value : inData.Invested,
                    };

                    outData.ViewHoldings.Add(outHoldings);
                }
                // DropDown -end

                _viewReport.Add(outData);
            }
        }

        private void OnRowClicked(TableRowClickEventArgs<ViewReportTradeData> data)
        {
            data.Item.ShowDetails = !data.Item.ShowDetails;
        }

        protected async Task OnBtnRemoveTradeAsync(ViewReportTradeData data)
        {
            bool? result = await Dialog.ShowMessageBox("Please confirm!", 
                    "Removing trade changes everything as it was before, with holdings owned again " + Environment.NewLine + 
                    "be very carefull with this as FIFO logic of sales gets easily confused if rolling back" + Environment.NewLine +
                    "old sales. Only use this if just made sale and need to fix some type on it by redoing it",
                    yesText: "Remove Sale", cancelText: "Cancel");

            if (result.HasValue == false || result.Value == false)
                return;

            // Delete-Trade TradeID
            string cmd = string.Format("Delete-Trade TradeID=[{0}]", data.d.TradeID);

            StalkerError err = PfsClientAccess.StalkerMgmt().DoAction(cmd);

            if (err == StalkerError.OK)
                ReloadReport();
            else
            {
                await Dialog.ShowMessageBox("Failed!", string.Format("Error: {0}", err.ToString()), yesText: "Ok");
            }
        }

        private void ViewStockRequested(Guid STID)
        {
            NavigationManager.NavigateTo("/stock/" + STID);
        }

        protected class ViewReportTradeData
        {
            public ReportTradeData d;

            public string Currency;
            public string DualCurrency;

            public bool ShowDetails;

            public bool AllowCancel;

            public List<ViewReportTradeHoldings> ViewHoldings;
        }

        protected class ViewReportTradeHoldings
        {
            public ReportTradeHoldings d;
        }
    }
}
