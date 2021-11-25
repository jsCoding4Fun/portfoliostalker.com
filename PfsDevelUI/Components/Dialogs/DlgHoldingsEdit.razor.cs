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
    public partial class DlgHoldingsEdit
    {
        [Inject] PfsClientAccess PfsClientAccess { get; set; }
        [Inject] private IDialogService Dialog { get; set; }

        [CascadingParameter] MudDialogInstance MudDialog { get; set; }

        [Parameter] public string Ticker { get; set; }
        [Parameter] public Guid STID { get; set; }
        [Parameter] public string PfName { get; set; }
        [Parameter] public StockHolding Defaults { get; set; } = null;      // If defined allows to preset some proposed values
        [Parameter] public bool Edit { get; set; } = false;                 // If edit is true then 'Defaults' should contain full StockHolding structure
        [Parameter] public CurrencyCode Currency { get; set; } = CurrencyCode.Unknown;  // Currency must be defined per Market of STID

        protected bool _fullscreen { get; set; } = false;

        protected StockHolding _holding = null;
        protected DateTime? _purhaceDate = DateTime.UtcNow.Date.AddDays(-1);// Going -1 as PFS is using 'Latest' of day, and its not available for today...
        protected string _holdingID = string.Empty;                         // Either customID, or if empty then set to system default, but must be unique
        protected string _conversionLabel = string.Empty;
        protected bool _viewConversionRate = true;
        protected string _viewCurrency = "";                                // Per 'CurrencyCode' set to be UI Display formatted U$ etc
        protected int _minPurhacedUnits = 1;
        protected bool _allowDelete = false;

        /*
         * DlgHolding, if anything is sold, then:
	     * - Cant Delete anymore
	     * - Cant change Min Editable unit amount is what ever is sold so far
	     * - Price per unit, Fee, Conversion Rate, Note and PurhaceDay can be still Edited
         */

        protected override void OnInitialized()
        {
            _viewCurrency = UiF.Curr(Currency);

            _holding = new()
            {
                Fee = 0,
                PurhaceDate = _purhaceDate.Value,
            };

            if (Defaults != null)
            {
                _holding.PricePerUnit = Defaults.PricePerUnit;
                _holding.PurhacedUnits = Defaults.PurhacedUnits;
                _holding.Fee = Defaults.Fee;

                if (Edit == false || Defaults.HoldingID.StartsWith("PFSHLDID:") == false )
                    _holding.HoldingID = Defaults.HoldingID;

                _holdingID = Defaults.HoldingID;
                _holding.ConversionRate = Defaults.ConversionRate;
                _holding.PurhaceDate = Defaults.PurhaceDate;
                _purhaceDate = Defaults.PurhaceDate;
                _holding.HoldingNote = Defaults.HoldingNote;

                if (Defaults.RemainingUnits < Defaults.PurhacedUnits)
                {
                    // This is sign that part of Holding is already SOLD, so editing is limited
                    _minPurhacedUnits = (int)(Defaults.PurhacedUnits - Defaults.RemainingUnits);
                    _allowDelete = false;
                }
                else
                    _allowDelete = true;
            }

            CurrencyCode defCurrency = (CurrencyCode)Enum.Parse(typeof(CurrencyCode), PfsClientAccess.Account().Property("HOMECURRENCY"));

            _conversionLabel = string.Format("Conversion rate from {0} to {1}", Currency, defCurrency.ToString());

            if (Currency == defCurrency)
            {
                // Actually no conversion rate viewing as its home currency, just hardcode it 1x now
                _viewConversionRate = false;
                _holding.ConversionRate = 1;
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

            decimal? ret = await PfsClientAccess.Fetch().GetCurrencyConversionAsync(Currency, defCurrency, _purhaceDate.Value);

            if (ret.HasValue)
                _holding.ConversionRate = ret.Value;
        }

        private void DlgCancel()
        {
            MudDialog.Cancel();
        }
        private async Task OnBtnDeleteAsync()
        {
            if (_allowDelete)
            {
                // Delete-Holding HoldingID
                string cmd = string.Format("Delete-Holding HoldingID=[{0}]", Defaults.HoldingID);

                StalkerError err = PfsClientAccess.StalkerMgmt().DoAction(cmd);

                if (err == StalkerError.OK)
                    MudDialog.Close();
                else
                {
                    bool? result = await Dialog.ShowMessageBox("Failed!", string.Format("Error: {0}", err.ToString()), yesText: "Ok");
                }
            }
        }

        private async Task OnBtnEditAsync()
        {
            _holding.PurhaceDate = _purhaceDate.Value;

            CurrencyCode defCurrency = (CurrencyCode)Enum.Parse(typeof(CurrencyCode), PfsClientAccess.Account().Property("HOMECURRENCY"));

            // Edit-Holding HoldingID Date Units Price Fee Conversion ConversionTo Note
            string cmd = string.Format("Edit-Holding HoldingID=[{0}] Date=[{1}] Units=[{2}] Price=[{3}] Fee=[{4}] Conversion=[{5}] ConversionTo=[{6}] Note=[{7}]",
                                       Defaults.HoldingID, _holding.PurhaceDate.ToString("yyyy-MM-dd"), _holding.PurhacedUnits,
                                       _holding.PricePerUnit, _holding.Fee, _holding.ConversionRate, defCurrency.ToString(), _holding.HoldingNote);

            StalkerError err = PfsClientAccess.StalkerMgmt().DoAction(cmd);

            if (err == StalkerError.OK)
                MudDialog.Close();
            else
            {
                bool? result = await Dialog.ShowMessageBox("Failed!", string.Format("Error: {0}", err.ToString()), yesText: "Ok");
            }
        }

        private async Task OnBtnAddAsync()
        {
            _holding.PurhaceDate = _purhaceDate.Value;

            CurrencyCode defCurrency = (CurrencyCode)Enum.Parse(typeof(CurrencyCode), PfsClientAccess.Account().Property("HOMECURRENCY"));

            // // Add-Holding PfName Stock Date Units Price Fee HoldingID Conversion ConversionTo Note
            string cmd = string.Format("Add-Holding PfName=[{0}] Stock=[{1}] Date=[{2}] Units=[{3}] Price=[{4}] Fee=[{5}] HoldingID=[{6}] Conversion=[{7}] "+
                                                   "ConversionTo=[{8}] Note=[{9}]",
                                       PfName, STID, _holding.PurhaceDate.ToString("yyyy-MM-dd"), _holding.PurhacedUnits, _holding.PricePerUnit,
                                       _holding.Fee, _holdingID, _holding.ConversionRate, defCurrency.ToString(), _holding.HoldingNote);

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
