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
    public partial class ReportInvested
    {
        [Parameter] public string PfName { get; set; } = string.Empty;
        
        [Inject] IDialogService Dialog { get; set; }
        [Inject] PfsClientAccess PfsClientAccess { get; set; }
        [Inject] NavigationManager NavigationManager { get; set; }

        private List<ViewReportInvestedData> _viewReport = null;

        private ViewReportInvestedHeader _viewHeader = null;

        protected string _headerTextCompany = string.Empty;
        protected string _headerTextInvested = string.Empty;
        protected string _headerTextValuation = string.Empty;
        protected string _headerTextDivident = string.Empty;
        protected string _headerTextGain = string.Empty;

        protected override void OnParametersSet()
        {
            _viewReport = null;
            _viewHeader = null;

            ReloadReport();
        }

        protected void ReloadReport()
        {
            (ReportInvestedHeader reportHeader, List <ReportInvestedData> reportData) = PfsClientAccess.Report().GetInvestedData(PfName);

            _viewReport = new();

            foreach (ReportInvestedData inData in reportData)
            {
                ViewReportInvestedData outData = new()
                {
                    d = inData,
                    Currency = UiF.Curr(inData.Currency),
                    HomeCurrency = UiF.Curr(inData.HomeCurrency),
                };

                // DropDown -start
                outData.ViewHoldings = new();

                foreach (ReportHoldingsData inHoldings in inData.Holdings)
                {
                    ViewReportHoldingsData outHoldings = new()
                    {
                        d = inHoldings,
                        SortOnInvested = inData.HcInvested.HasValue ? inData.HcInvested.Value : inData.Invested,
                        HcAnnualReturnP = null,
                    };

                    outHoldings.Units = string.Format("{0} / {1}", inHoldings.Holding.RemainingUnits, inHoldings.Holding.PurhacedUnits);
                    outHoldings.Currency = UiF.Curr(inHoldings.Currency);
                    outHoldings.HomeCurrency = UiF.Curr(inHoldings.HomeCurrency);

                    if (inHoldings.HcProfitAmount.HasValue && inHoldings.HcInvested.HasValue)       // !!!LATER!!! This is garbage calculation, see better ...
                    {
                        decimal gain = inHoldings.HcProfitAmount.Value;

                        if (inHoldings.HcDividentTotal.HasValue)
                            gain += inHoldings.HcDividentTotal.Value;

                        if (gain != 0 && DateTime.UtcNow.Date > inHoldings.Holding.PurhaceDate)
                        {
                            int days = (int)(DateTime.UtcNow - inHoldings.Holding.PurhaceDate).TotalDays;

                            if ( days >= 180 )
                                outHoldings.HcAnnualReturnP = (int)((gain / days * 365) / inHoldings.HcInvested * 100);
                        }
                    }

                    outData.ViewHoldings.Add(outHoldings);
                }
                // DropDown -end

                _viewReport.Add(outData);
            }

            _viewHeader = new()
            {
                h = reportHeader,
            };

            _headerTextCompany = string.Format("Company (total {0})", _viewReport.Count());

            if (_viewHeader.h.HcTotalInvested.HasValue)
            {
                _headerTextInvested = string.Format("Invested {0}{1}",
                                                _viewHeader.h.HcTotalInvested.Value.ToString("0"),
                                                @UiF.Curr(_viewHeader.h.HomeCurrency));
            }
            else
            {
                _headerTextInvested = string.Format("Invested ???{0}",
                                   @UiF.Curr(_viewHeader.h.HomeCurrency));
            }

            if ( _viewHeader.h.HcTotalValuation.HasValue )
            {
                // "Valuation +3% nnnE", on home currency, shows todays total valuation of all investments, with colored growth % if all set properly
                if ( _viewHeader.h.HcTotalProfitP.HasValue && _viewHeader.h.HcTotalProfitP.Value >= 0 )
                {
                    _headerTextValuation = string.Format("Val. +{0}% {1}{2}",
                                                _viewHeader.h.HcTotalProfitP.Value,
                                                _viewHeader.h.HcTotalValuation.Value.ToString("0"),
                                                UiF.Curr(_viewHeader.h.HomeCurrency));
                }
                else if ( _viewHeader.h.HcTotalProfitP.HasValue && _viewHeader.h.HcTotalProfitP.Value < 0 )
                {
                    _headerTextValuation = string.Format("Val. -{0}% {1}{2}",
                            _viewHeader.h.HcTotalProfitP.Value,
                            _viewHeader.h.HcTotalValuation.Value.ToString("0"),
                            UiF.Curr(_viewHeader.h.HomeCurrency));
                }
                else
                {
                    _headerTextValuation = string.Format("Val. ?% {0}{1}",
                            _viewHeader.h.HcTotalValuation.Value.ToString("0"),
                            UiF.Curr(_viewHeader.h.HomeCurrency));
                }
            }
            else
            {
                _headerTextValuation = string.Format("Val. ?% ???{0}",
                                        UiF.Curr(_viewHeader.h.HomeCurrency));
            }

            if( _viewHeader.h.HcTotalDivident > 0 )
            {
                if (_viewHeader.h.HcTotalDividentP > 0)
                {
                    _headerTextDivident = string.Format("Div. {0}% {1}{2}",
                            _viewHeader.h.HcTotalDividentP.Value.ToString("0.0"),
                            _viewHeader.h.HcTotalDivident.ToString("0"),
                            UiF.Curr(_viewHeader.h.HomeCurrency));
                }
                else
                {
                    _headerTextDivident = string.Format("Divident. ?% {0}{1}",
                            _viewHeader.h.HcTotalDivident.ToString("0"),
                            UiF.Curr(_viewHeader.h.HomeCurrency));
                }
            }
            else
            {
                _headerTextDivident = "Divident";
            }

            if (_viewHeader.h.HcTotalGain.HasValue)
            {
                if (_viewHeader.h.HcTotalGainP.HasValue)
                {
                    _headerTextGain = string.Format("Gain {0}% {1}{2}",
                            _viewHeader.h.HcTotalGainP.Value.ToString("0"),
                            _viewHeader.h.HcTotalGain.Value.ToString("0"),
                            UiF.Curr(_viewHeader.h.HomeCurrency));
                }
                else
                {
                    _headerTextGain = string.Format("Gain ?% {0}{1}",
                            _viewHeader.h.HcTotalGain.Value.ToString("0"),
                            UiF.Curr(_viewHeader.h.HomeCurrency));
                }
            }
            else
            {
                _headerTextGain = "Total Gain ?";
            }
        }

        private void OnRowClicked(TableRowClickEventArgs<ViewReportInvestedData> data)
        {
            data.Item.ShowDetails = !data.Item.ShowDetails;
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

                ReloadReport();
                StateHasChanged();
            }
        }
        protected async Task OnBtnSaleEventAsync(ViewReportHoldingsData data)
        {
            var parameters = new DialogParameters();
            parameters.Add("Ticker", data.d.Ticker);
            parameters.Add("STID", data.d.STID);
            parameters.Add("PfName", data.d.PfName);
            parameters.Add("TargetHolding", data.d.Holding);
            parameters.Add("MaxUnits", (int)(data.d.Holding.RemainingUnits));
            parameters.Add("Currency", data.d.Currency);

            // Ala Sale Holding operation == finishing trade of buy holding, and now sell holding(s)
            var dialog = Dialog.Show<DlgTradeFinish>("", parameters);
            var result = await dialog.Result;

            if (!result.Cancelled)
                ReloadReport();
        }

        private void ViewStockRequested(Guid STID)
        {
            NavigationManager.NavigateTo("/stock/" + STID);
        }

        protected class ViewReportInvestedData
        {
            public ReportInvestedData d;

            public string Currency;
            public string HomeCurrency;

            public bool ShowDetails;

            public List<ViewReportHoldingsData> ViewHoldings;
        }

        public class ViewReportInvestedHeader
        {
            public ReportInvestedHeader h;
        }

        protected class ViewReportHoldingsData
        {
            public ReportHoldingsData d;

            public string Units;

            public string Currency;
            public string HomeCurrency;

            public decimal SortOnInvested;

            public int? HcAnnualReturnP;            // !!!LATER!!! This needs to go PFS.Client also.. and fix this, calculation is garbage atm!
        }
    }
}
