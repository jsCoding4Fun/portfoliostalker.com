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
    // Trade allows to sell previous holding(s) to cash profit/looses. Dlg allows to target selling specific holding, or do automatic fifo sale
    public partial class DlgTradeFinish
    {
        [Inject] PfsClientAccess PfsClientAccess { get; set; }
        [Inject] private IDialogService Dialog { get; set; }

        [CascadingParameter] MudDialogInstance MudDialog { get; set; }

        [Parameter] public string Ticker { get; set; }
        [Parameter] public Guid STID { get; set; }
        [Parameter] public string PfName { get; set; }
        [Parameter] public StockHolding TargetHolding { get; set; } = null; // If != null then targets to sale specific holding fully/partially
        [Parameter] public int MaxUnits { get; set; } = 0;   // If set then limits max amount of sale per caller given (remaining holdings total) 
                                                             // Note! Does NOT like to take double in atm so int it is...
        [Parameter] public CurrencyCode Currency { get; set; } = CurrencyCode.Unknown;

        [Parameter] public StockTrade Defaults { get; set; } = null; // If set allows bring some values in

        // Decision! There is no EDIT! But its possible to delete latest 'Trade' of specific stock under specific Portfolio (so can retry)

        protected bool _fullscreen { get; set; } = false;

        protected StockTrade _values = null;
        protected DateTime? _saleDate = DateTime.UtcNow.Date.AddDays(-1); // Going -1 as PFS is using 'Latest' of day, and its not available for today...
        protected string _tradeID = string.Empty;                         // Either customID, or if empty then set to system default, but must be unique
        protected string _conversionLabel = string.Empty;
        protected bool _viewConversionRate = true;
        protected string _labelSoldUnits = "";
        protected string _viewCurrency = "";
        protected string _header = "";

        protected override void OnInitialized()
        {
            _values = new()
            {
                Fee = 0,
                SaleDate = _saleDate.Value,
            };

            CurrencyCode defCurrency = (CurrencyCode)Enum.Parse(typeof(CurrencyCode), PfsClientAccess.Account().Property("HOMECURRENCY"));

            _conversionLabel = string.Format("Conversion rate from {0} to {1}", Currency.ToString(), defCurrency.ToString());

            if (Currency == defCurrency)
            {
                // Actually no conversion rate viewing as its home currency, just hardcode it 1x now
                _viewConversionRate = false;
                _values.ConversionRate = 1;
            }

            _labelSoldUnits = string.Format("Sold Units (max={0})", MaxUnits);
            _viewCurrency = UiF.Curr(Currency);

            if (TargetHolding != null)
                _header = string.Format("Sale holding of {0} under {1}", Ticker, PfName);
            else
                _header = string.Format("Sale of {0} as FIFO under {1}", Ticker, PfName);

            if (Defaults != null) 
            {
                if ( Defaults.SoldUnits > 0)
                    _values.SoldUnits = Defaults.SoldUnits;

                if ( Defaults.PricePerUnit > 0)
                    _values.PricePerUnit = Defaults.PricePerUnit;

                if (Defaults.SaleDate != DateTime.MinValue)
                    _values.SaleDate = Defaults.SaleDate;
            }
        }

        protected void OnFullScreenChanged(bool fullscreen)     // !!!TODO!!! Those dang header icons overlap atm, one from mud one of my.. push my left..
        {
            _fullscreen = fullscreen;

            MudDialog.Options.FullWidth = _fullscreen;
            MudDialog.SetOptions(MudDialog.Options);
        }

        protected async Task OnBtnGetCurrencyAsync()
        {
            CurrencyCode defCurrency = (CurrencyCode)Enum.Parse(typeof(CurrencyCode), PfsClientAccess.Account().Property("HOMECURRENCY"));

            decimal? ret = await PfsClientAccess.Fetch().GetCurrencyConversionAsync(Currency, defCurrency, _saleDate.Value);

            if (ret.HasValue)
                _values.ConversionRate = ret.Value;
        }

        private void DlgCancel()
        {
            MudDialog.Cancel();
        }

        private async Task OnBtnSaleAsync()
        {
            _values.SaleDate = _saleDate.Value;

            string holdingStrID = string.Empty;

            if (TargetHolding != null)
                holdingStrID = TargetHolding.HoldingID;

            CurrencyCode defCurrency = (CurrencyCode)Enum.Parse(typeof(CurrencyCode), PfsClientAccess.Account().Property("HOMECURRENCY"));

            // Add-Trade PfName Stock Date Units Price Fee TradeID HoldingStrID Conversion ConversionTo
            string cmd = string.Format("Add-Trade PfName=[{0}] Stock=[{1}] Date=[{2}] Units=[{3}] Price=[{4}] Fee=[{5}] TradeID=[{6}] HoldingStrID=[{7}] " +
                                       "Conversion=[{8}] ConversionTo=[{9}]",
                                       PfName, STID, _values.SaleDate.ToString("yyyy-MM-dd"), _values.SoldUnits, _values.PricePerUnit,
                                       _values.Fee, _values.TradeID, holdingStrID, _values.ConversionRate, defCurrency.ToString());

            StalkerError err = PfsClientAccess.StalkerMgmt().DoAction(cmd);

            if ( err == StalkerError.OK )
                MudDialog.Close();
            else
            {
                bool? result = await Dialog.ShowMessageBox("Failed!", string.Format("Error: {0}", err.ToString()), yesText: "Ok");
            }
        }
    }
}
