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
    // Per givent parameter targets specific stock of specific portfolio, to add it new divident by manually entering details
    public partial class DlgDividentAdd
    {
        [Inject] PfsClientAccess PfsClientAccess { get; set; }
        [Inject] private IDialogService Dialog { get; set; }

        [CascadingParameter] MudDialogInstance MudDialog { get; set; }

        [Parameter] public string Ticker { get; set; }
        [Parameter] public Guid STID { get; set; }
        [Parameter] public string PfName { get; set; }
        [Parameter] public CurrencyCode Currency { get; set; } = CurrencyCode.Unknown;

        // Decision! There is no EDIT! But its possible to delete latest 'Trade' of specific stock under specific Portfolio (so can retry)

        protected bool _fullscreen { get; set; } = false;

        protected StockDivident _values = null;
        protected DateTime? _dividentDate = DateTime.UtcNow.Date.AddDays(-1); // Going -1 as PFS is using 'Latest' of day, and its not available for today...
        protected string _dividentID = string.Empty;                          // Either customID, or if empty then set to system default, but must be unique
        protected string _conversionLabel = string.Empty;
        protected bool _viewConversionRate = true;
        protected string _viewCurrency = "";
        protected string _header = "";

        protected override void OnInitialized()
        {
            _values = new()
            {
            };

            CurrencyCode defCurrency = (CurrencyCode)Enum.Parse(typeof(CurrencyCode), PfsClientAccess.Account().Property("HOMECURRENCY"));

            _conversionLabel = string.Format("Conversion rate from {0} to {1}", Currency.ToString(), defCurrency.ToString());

            if (Currency == defCurrency)
            {
                // Actually no conversion rate viewing as its home currency, just hardcode it 1x now
                _viewConversionRate = false;
                _values.ConversionRate = 1;
            }

            _viewCurrency = UiF.Curr(Currency);
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

            decimal? ret = await PfsClientAccess.Fetch().GetCurrencyConversionAsync(Currency, defCurrency, _dividentDate.Value);

            if (ret.HasValue)
                _values.ConversionRate = ret.Value;
        }

        private void DlgCancel()
        {
            MudDialog.Cancel();
        }

        private async Task OnBtnAddAsync()
        {
            _values.Date = _dividentDate.Value;

            CurrencyCode defCurrency = (CurrencyCode)Enum.Parse(typeof(CurrencyCode), PfsClientAccess.Account().Property("HOMECURRENCY"));

            // Add-Divident PfName Stock Date Units PaymentPerUnit DividentID Conversion ConversionTo
            string cmd = string.Format("Add-Divident PfName=[{0}] Stock=[{1}] Date=[{2}] Units=[{3}] PaymentPerUnit=[{4}] DividentID=[{5}] " +
                                       "Conversion=[{6}] ConversionTo=[{7}]",
                                       PfName, STID, _values.Date.ToString("yyyy-MM-dd"), _values.Units, _values.PaymentPerUnit, _values.DividentID,
                                       _values.ConversionRate, defCurrency.ToString());

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
