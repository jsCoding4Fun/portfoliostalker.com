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
using System.Xml.Linq;
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

using PFS.Shared.Types;
using PFS.Shared.UiTypes;

namespace PfsDevelUI.Components
{
    public partial class ReportStockTable
    {
        /* PLAN: This report is for KEEPING EYE OF COMPANIES WANTING TO PURHACE, specially so on mainline of report:
         *  => Mainline doesnt show holdings except profit/gain
         *  => Mainline doesnt show dividents
         *  => !!!DECISION!!! FOCUS of this report is on Alarm's and keeping eye company as investment target
         *  
         *  If someday future wants to show divident % on this report, it needs to be for each and every stock.. not just ones w holdings! But really dont!
         * 
         * 
         * Remember! Investment report is there for overview of gains, and StockMgmt shows way more details per currencies
         */


        [Inject] IDialogService Dialog { get; set; }
        [CascadingParameter] MudDialogInstance MudDialog { get; set; }

        [Parameter] public string PfName { get; set; } // Should be non-empty if launched from Portfolio or StockGroup
        [Parameter] public string SgName { get; set; } // Should be non-empty if launched from StockGroup
        [Parameter] public ReportTypeID ReportType { get; set; } = ReportTypeID.UNKNOWN; // <== mandatory to set!

        protected bool _showIntradayColumn;  // Special column that shows time, and only visible if has Intraday fetched
        protected bool _showProfitColumn;
        protected bool _showAlarmOverColumn;

        protected bool _allowIndicators = false;        // Limited to payed accounts & PfsAccSrv/PfsPrivSrv must be connected
        protected bool _allowIndicatorsLvls = false;    // Limited to payed accounts & PfsAccSrv/PfsPrivSrv must be connected

        public enum ReportTypeID
        {
            UNKNOWN,            
            STOCK_GROUP,
            PORTFOLIO,
            PORTFOLIO_WATCH,
            ACCOUNT_WATCH,
        };

        // Set true if report should show specific operation buttons on dropdown
        [Parameter] public bool ViewBtnAddHolding { get; set; } = false;    // Requires that Portfolio is defined
        [Parameter] public bool ViewBtnAddOrder { get; set; } = false;      // Requires that Portfolio is defined
        [Parameter] public bool ViewBtnSaleHolding { get; set; } = false;   // Requires that Portfolio is defined
        [Parameter] public bool ViewBtnAddDivident { get; set; } = false;   // Requires that Portfolio is defined
        [Parameter] public bool ViewBtnRemoveStock { get; set; } = false;   // Requires both Portfolio & Stock Group defined

        [Inject] PfsClientAccess PfsClientAccess { get; set; }
        [Inject] NavigationManager NavigationManager { get; set; }

        protected List<ViewReportStockTableData> _report = null;

        protected override void OnParametersSet()
        {
            AccountTypeID SessionAccountType = (AccountTypeID)Enum.Parse(typeof(AccountTypeID), PfsClientAccess.Account().Property("ACCOUNTTYPE"));

            _allowIndicators = false;
            _allowIndicatorsLvls = false;

            switch (SessionAccountType)
            {
                case AccountTypeID.Platinum:
                case AccountTypeID.Admin:
                case AccountTypeID.Demo:
                    // Indicators are cached, so here we limit them from viewing until user logs on
                    _allowIndicators = true;
                    _allowIndicatorsLvls = true;
                    break;

                case AccountTypeID.Gold:
                    _allowIndicators = true;
                    break;
            }

            if (PfsClientAccess.PrivSrvMgmt().Property("CONNECTED") != "TRUE" )
            {
                // Hups, never mind.. not allowing any indicator fields if not active connection to priv srv
                _allowIndicators = false;
                _allowIndicatorsLvls = false;
            }

            ReloadReportData();
        }

        public void ReloadReport()
        {
            ReloadReportData();
            StateHasChanged();
        }

        protected void ReloadReportData()
        {
            _showIntradayColumn = false;
            _showProfitColumn = false;
            _showAlarmOverColumn = false;
            List<ReportStockTableData> reportData = null;
            _report = new();

            switch (ReportType)
            {
                case ReportTypeID.STOCK_GROUP:

                    reportData = PfsClientAccess.Report().GetStockGroupLatest(SgName);
                    break;

                case ReportTypeID.PORTFOLIO:

                    reportData = PfsClientAccess.Report().GetPortfolioStockLatest(PfName);
                    break;

                case ReportTypeID.PORTFOLIO_WATCH:

                    reportData = PfsClientAccess.Report().GetPortfolioWatchStockLatest(PfName);
                    break;

                case ReportTypeID.ACCOUNT_WATCH:

                    reportData = PfsClientAccess.Report().GetPortfolioWatchStockLatest(string.Empty);
                    break;
            }

            if (reportData != null)
            {
                foreach (ReportStockTableData inData in reportData)
                {
                    ViewReportStockTableData outData = new()
                    {
                        d = inData,
                        ShowDetails = false,
                        OrderDetails = null,
                        Currency = UiF.Curr(inData.Currency),
                        DualCurrency = UiF.Curr(inData.HomeCurrency),
                        GoogleFinances = GetUrlGoogleFinances(inData),
                        TradingView = GetUrlTradingView(inData),
                        YahooFinances = GetUrlYahooFinances(inData),
                        StockTwitch = GetUrlStockTwits(inData),
                        Graph90DaysEOD = "data:image/gif;base64,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",
                    };

                    if (outData.d.IsIntraday == true)
                        _showIntradayColumn = true;

                    if (outData.d.AlarmOverP.HasValue)
                        _showAlarmOverColumn = true;

                    if ( inData.Orders != null && inData.Orders.Count() > 0)
                    {
                        if (inData.Orders.Count() > 1) 
                            outData.OrderDetails = "(multiple buy/sell orders)";
                        else if ( inData.Orders[0].Type == StockOrderType.Buy ) 
                        {
                            if (inData.Orders[0].FillDate.HasValue == false)
                                outData.OrderDetails = string.Format("(Buy Order for {0}pcs at {1}{2})", inData.Orders[0].Units, inData.Orders[0].PricePerUnit.ToString("0.00"), outData.Currency);
                            else
                                outData.OrderDetails = string.Format("(Buy {0}pcs at {1}{2} - PURHACED? {3})", inData.Orders[0].Units, inData.Orders[0].PricePerUnit.ToString("0.00"),
                                                                     outData.Currency, inData.Orders[0].FillDate.Value.ToString("yyyy-MM-dd"));
                        }
                        else if (inData.Orders[0].Type == StockOrderType.Sell)
                        {
                            if (inData.Orders[0].FillDate.HasValue == false)
                                outData.OrderDetails = string.Format("(Sell Order for {0}pcs at {1}{2})", inData.Orders[0].Units, inData.Orders[0].PricePerUnit.ToString("0.00"), outData.Currency);
                            else
                                outData.OrderDetails = string.Format("(Sell {0}pcs at {1}{2} - SOLD NOW? {3})", inData.Orders[0].Units, inData.Orders[0].PricePerUnit.ToString("0.00"),
                                                                     outData.Currency, inData.Orders[0].FillDate.Value.ToString("yyyy-MM-dd"));
                        }
                    }

                    if (inData.ProfitAmount.HasValue)
                        // Profit column is shown only if group has something w it..
                        _showProfitColumn = true;

                    if (_allowIndicatorsLvls == true && inData.RSI14Dlvl.HasValue )
                    {
                        if (inData.RSI14Dlvl.Value <= 9)
                            outData.ViewRSI14Dlvl = string.Format("BTM {0}%", inData.RSI14Dlvl.Value);
                        else if (inData.RSI14Dlvl.Value >= 91)
                            outData.ViewRSI14Dlvl = string.Format("TOP {0}%", 100-inData.RSI14Dlvl.Value);
                    }

                    if (_allowIndicatorsLvls == true && inData.RSI14Wlvl.HasValue)
                    {
                        if (inData.RSI14Wlvl.Value <= 9)
                            outData.ViewRSI14Wlvl = string.Format("BTM {0}%", inData.RSI14Wlvl.Value);
                        else if (inData.RSI14Wlvl.Value >= 91)
                            outData.ViewRSI14Wlvl = string.Format("TOP {0}%", 100 - inData.RSI14Wlvl.Value);
                    }

                    if (_allowIndicatorsLvls == true && inData.MFI14Dlvl.HasValue)
                    {
                        if (inData.MFI14Dlvl.Value <= 9)
                            outData.ViewMFI14Dlvl = string.Format("BTM {0}%", inData.MFI14Dlvl.Value);
                        else if (inData.MFI14Dlvl.Value >= 91)
                            outData.ViewMFI14Dlvl = string.Format("TOP {0}%", 100 - inData.MFI14Dlvl.Value);
                    }

                    if (_allowIndicatorsLvls == true && inData.MFI14Wlvl.HasValue)
                    {
                        if (inData.MFI14Wlvl.Value <= 9)
                            outData.ViewMFI14Wlvl = string.Format("BTM {0}%", inData.MFI14Wlvl.Value);
                        else if (inData.MFI14Wlvl.Value >= 91)
                            outData.ViewMFI14Wlvl = string.Format("TOP {0}%", 100 - inData.MFI14Wlvl.Value);
                    }

                    _report.Add(outData);
                }
            }
            return;

            string GetUrlGoogleFinances(ReportStockTableData data) // Fixed 2021-Oct-11th, works for main US/CAD markets
            {
                string url = @"https://www.google.com/finance/quote/";

                switch (data.MarketID)
                {
                    case MarketID.TSX:
                        return url + data.Ticker + ":TSE";

                    case MarketID.NYSE:
                    case MarketID.NASDAQ:
                        return url + data.Ticker + ":" + data.MarketID.ToString();

                    case MarketID.AMEX:
                        return url + data.Ticker + ":NYSEAMERICAN";

                    case MarketID.OMXH:
                        return url + data.Ticker + ":HEL";
                }
                return string.Empty;
            }

            string GetUrlTradingView(ReportStockTableData data) // Fixed 2021-Oct-11th, works for main US/CAD markets
            {
                //TSX:  https://www.tradingview.com/chart/?symbol=TSX%3ATXG

                string url = @"https://www.tradingview.com/chart/?symbol=";

                switch (data.MarketID)
                {
                    case MarketID.TSX:
                    case MarketID.NYSE:
                    case MarketID.NASDAQ:
                    case MarketID.AMEX:
                        return url + data.MarketID.ToString() + "%3A" + data.Ticker;

                    case MarketID.OMXH:
                        return url + "OMXHEX%3A" + data.Ticker;
                }
                return string.Empty;
            }

            string GetUrlYahooFinances(ReportStockTableData data) // Fixed 2021-Oct-11th, works for main US/CAD markets
            {
                // https://finance.yahoo.com/quote/ABX.TO
                string url = @"https://finance.yahoo.com/quote/";

                switch (data.MarketID)
                {
                    case MarketID.NYSE:
                    case MarketID.NASDAQ:
                    case MarketID.AMEX:
                        return url + data.Ticker;

                    case MarketID.TSX:
                        return url + data.Ticker + ".TO";

                    case MarketID.OMXH:
                        return url + data.Ticker + ".HE";
                }
                return string.Empty;
            }

            string GetUrlStockTwits(ReportStockTableData data) // Fixed 2021-Oct-11th, works for main US/CAD markets
            {
                // https://stocktwits.com/symbol/TRIT
                string url = @"https://stocktwits.com/symbol/";

                switch (data.MarketID)
                {
                    case MarketID.NYSE:
                    case MarketID.NASDAQ:
                    case MarketID.AMEX:
                        return url + data.Ticker;

                    case MarketID.TSX:
                        return url + data.Ticker + ".CA";
                }
                return string.Empty;
            }
        }

        private void OnRowClicked(TableRowClickEventArgs<ViewReportStockTableData> data)
        {
            data.Item.ShowDetails = !data.Item.ShowDetails;
        }

        private void OnBtnLastStockEdit(ViewReportStockTableData data)
        {
            data.d.LastStockEdit = DateTime.Now;

            // !!!LATER!!! If clicked and has already todays day, could popup dlg w MudDatePicker and allow custom day selection from 2021-1-1 -> today

            string action = string.Format("Set-Stock Stock=[{0}] +LastEdit=[{1}]", data.d.STID, data.d.LastStockEdit.ToString("yyyy-MM-dd"));
            PfsClientAccess.StalkerMgmt().DoAction(action);

            StateHasChanged();
        }

        private void ViewStockRequested(Guid STID)
        {
            NavigationManager.NavigateTo("/stock/" + STID);
        }

        private async Task ViewAddOrderDialogAsync(ViewReportStockTableData data)
        {
            var parameters = new DialogParameters();
            parameters.Add("MarketID", data.d.MarketID);
            parameters.Add("Ticker", data.d.Ticker);
            parameters.Add("STID", data.d.STID);
            parameters.Add("PfName", PfName);
            parameters.Add("Defaults", null);
            parameters.Add("Edit", false);

            // !!!NOTE!!! MainLayout has default's for options
            var dialog = Dialog.Show<DlgOrderEdit>("", parameters);
            var result = await dialog.Result;

            if (!result.Cancelled)
                ReloadReport();
        }

        private async Task ViewAddHoldingsDialogAsync(ViewReportStockTableData entry)
        {
            var parameters = new DialogParameters();
            parameters.Add("Ticker", entry.d.Ticker);
            parameters.Add("STID", entry.d.STID);
            parameters.Add("PfName", PfName);
            parameters.Add("Currency", entry.d.Currency);

            var dialog = Dialog.Show<DlgHoldingsEdit>("", parameters);
            var result = await dialog.Result;

            if (!result.Cancelled)
                ReloadReport();
        }

        private async Task ViewSaleHoldingDialogAsync(ViewReportStockTableData entry)
        {
            var parameters = new DialogParameters();
            parameters.Add("Ticker", entry.d.Ticker);
            parameters.Add("STID", entry.d.STID);
            parameters.Add("PfName", PfName);
            parameters.Add("TargetHolding", null); // Just targeting PF/STID, w MaxUnits using FIFO 
            parameters.Add("MaxUnits", (int)(entry.d.Holdings.ToList().ConvertAll(h => h.RemainingUnits).Sum()));
            parameters.Add("Currency", entry.d.Currency);

            // Ala Sale Holding operation == finishing trade of buy holding, and now sell holding(s)
            var dialog = Dialog.Show<DlgTradeFinish>("", parameters);
            var result = await dialog.Result;

            if (!result.Cancelled)
                ReloadReport();
        }

        private async Task ViewAddDividentDialogAsync(ViewReportStockTableData entry)
        {
            var parameters = new DialogParameters();
            parameters.Add("Ticker", entry.d.Ticker);
            parameters.Add("STID", entry.d.STID);
            parameters.Add("PfName", PfName);
            parameters.Add("Currency", entry.d.Currency);

            var dialog = Dialog.Show<DlgDividentAdd>("", parameters);
            var result = await dialog.Result;

            if (!result.Cancelled)
                ReloadReport();
        }

        private async Task RemoveStockFromStockGroupAsync(Guid STID)
        {
            bool? result = await Dialog.ShowMessageBox("Are you sure...", "Remove stock from this group?", yesText: "Ok", cancelText: "Cancel");

            if (result.HasValue == false || result.Value == false)
                return;

            // Unfollow-Group SgName Stock
            string cmd = string.Format("Unfollow-Group SgName=[{0}] Stock=[{1}]", SgName, STID);
            PfsClientAccess.StalkerMgmt().DoAction(cmd);

            ReloadReport();
        }
    }

    public class ViewReportStockTableData
    {
        public ReportStockTableData d { get; set; }

        public bool ShowDetails { get; set; }

        public string OrderDetails { get; set; }

        public string Currency { get; set; }
        public string DualCurrency { get; set; }

        public string ViewRSI14Dlvl { get; set; }       // lvl is 1Y period's TOP N% or BTM N%
        public string ViewMFI14Dlvl { get; set; }

        public string ViewRSI14Wlvl { get; set; }       // lvl is 5Y period's TOP N% or BTM N%
        public string ViewMFI14Wlvl { get; set; }

        public string GoogleFinances { get; set; }

        public string TradingView { get; set; }

        public string YahooFinances { get; set; }

        public string StockTwitch { get; set; }

        public string Graph90DaysEOD { get; set; }
    }
}
