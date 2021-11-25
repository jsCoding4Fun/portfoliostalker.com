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
    public partial class DlgOrderEdit
    {
        [Inject] PfsClientAccess PfsClientAccess { get; set; }
        [Inject] IDialogService Dialog { get; set; }

        [CascadingParameter] MudDialogInstance MudDialog { get; set; }

        [Parameter] public MarketID MarketID { get; set; }  // These fill automatically per caller pages 'DialogParameters' 
        [Parameter] public string Ticker { get; set; }
        [Parameter] public Guid STID { get; set; }
        [Parameter] public string PfName { get; set; }
        [Parameter] public StockOrder Defaults { get; set; } = null; // If set then brings defaults in (used on edit and re-new's etc)
        [Parameter] public bool Edit { get; set; } = false;          // If set true then 'Defaults' must contain actually existing stored Order


        protected bool _fullscreen { get; set; } = false;

        protected MarketMeta _marketMeta = null;

        protected StockOrder _order = null;
        protected DateTime? _firstDate = DateTime.UtcNow.Date;
        protected DateTime? _lastDate = DateTime.UtcNow.Date;

        protected DateTime _minDate = DateTime.UtcNow.Date;

        // Decision, for now: Dont wanna show OrderFilled date here, nor allow resetting it.. just not worth of effort.. 
        //                    as really if it alarms, then order should be filled and converted anyway.. worry if not..

        protected override void OnInitialized()
        {
            _marketMeta = PfsClientAccess.Fetch().GetMarketMeta().SingleOrDefault(m => m.ID == MarketID);

            MarketCloses marketCloses = MarketCloses.Calculate(DateTime.UtcNow, _marketMeta);

            // Note! Decision! These Order's are not for past, not even for open market.. they are just tool to keep track 
            //                 of orders as additional way to see if specific alarm/watch is reacted or not. As of this 
            //                 they must be set for future, earliest for next market opening

            // !!!TODO!!! !!!LATER!!! Following is NOT accurate on all cases, and may not prevent atm adding order yet to open market!

            if ( Defaults != null )
                _order = Defaults.DeepCopy();

            if (Edit) // Expects 'Defaults' to be set if Edit=true
            {
                _minDate = marketCloses.LastClosingDate.AddDays(1);
                _firstDate = _order.FirstDate;
                _lastDate = _order.LastDate;
            }
            else // Add -operation (even if default is given, still uses new period)
            {
                _minDate = marketCloses.LastClosingDate.AddDays(1);
                _firstDate = _minDate;
                _lastDate = _minDate;
            }

            if (_order == null)
            {
                _order = new()
                {
                    FirstDate = _firstDate.Value,
                    LastDate = _lastDate.Value,
                };
            }
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

        protected void DlgDeleteOrder()
        {
            if (Edit)
            {
                // Delete-Order PfName Stock Price
                string cmd = string.Format("Delete-Order PfName=[{0}] Stock=[{1}] Price=[{2}]", PfName, STID, Defaults.PricePerUnit);

                if (PfsClientAccess.StalkerMgmt().DoAction(cmd) == StalkerError.OK)
                    MudDialog.Close();
                else
                {
                    // !!!LATER!!! Add error
                }
            }
        }
        private async Task DlgConvertOrderSync()
        {
            StockHolding holding = new()
            {
                PricePerUnit = Defaults.PricePerUnit,
                PurhacedUnits = Defaults.Units,
                PurhaceDate = Defaults.FirstDate,
            };

            var parameters = new DialogParameters();
            parameters.Add("Ticker", Ticker);
            parameters.Add("STID", STID);
            parameters.Add("PfName", PfName);
            parameters.Add("Defaults", holding);
            parameters.Add("Edit", false);
            parameters.Add("Currency", _marketMeta.Currency);

            // !!!NOTE!!! MainLayout has default's for options
            var dialog = Dialog.Show<DlgHoldingsEdit>("", parameters);
            var result = await dialog.Result;

            if (!result.Cancelled)
            {
                // Looks like this was Converted to new Holding, so delete Order itself
                DlgDeleteOrder();
            }
        }
        protected async Task<bool> Verify()
        {
            if (_order.Type == StockOrderType.Unknown || _order.PricePerUnit <= 0.01M || _order.Units <= 0.01M)
            {
                bool? result = await Dialog.ShowMessageBox("Cant do!", "Please select Type, and fill fields!", yesText: "Ok");
                return false;
            }
            return true;
        }

        private async Task DlgAddOrderAsync()
        {
            if (await Verify() == false)
                return;

            _order.FirstDate = _firstDate.Value;
            _order.LastDate = _lastDate.Value;

            // Add-Order PfName Type Stock Units Price FirstDate LastDate
            string cmd = string.Format("Add-Order PfName=[{0}] Type=[{1}] Stock=[{2}] Units=[{3}] Price=[{4}] FirstDate=[{5}] LastDate=[{6}]",
                                       PfName, _order.Type, STID, _order.Units, _order.PricePerUnit,
                                        _order.FirstDate.ToString("yyyy-MM-dd"), _order.LastDate.ToString("yyyy-MM-dd"));

            if (PfsClientAccess.StalkerMgmt().DoAction(cmd) == StalkerError.OK)
                MudDialog.Close();
            else
            {
                // !!!LATER!!! Show error...
            }
        }

        private async Task DlgEditOrderAsync()
        {
            if (await Verify() == false)
                return;

            _order.FirstDate = _firstDate.Value;
            _order.LastDate = _lastDate.Value;

            // Edit-Order PfName Type Stock EditedPrice Units Price FirstDate LastDate
            string cmd = string.Format("Edit-Order PfName=[{0}] Type=[{1}] Stock=[{2}] EditedPrice=[{3}] Units=[{4}] Price=[{5}] FirstDate=[{6}] LastDate=[{7}]",
                                       PfName, _order.Type, STID, Defaults.PricePerUnit, _order.Units, _order.PricePerUnit,
                                        _order.FirstDate.ToString("yyyy-MM-dd"), _order.LastDate.ToString("yyyy-MM-dd"));

            if (PfsClientAccess.StalkerMgmt().DoAction(cmd) == StalkerError.OK)
                MudDialog.Close();
            else
            {
                // !!!LATER!!! Show error...
            }
        }
    }
}
