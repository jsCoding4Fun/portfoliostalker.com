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
    public partial class CompStockStatus
    {
        /* !!!DOCUMENT!!! Stock Status & Fetch button on UI
         * 
         * Before user has logged in to Pfs (Main) Server
         *  - This component should not even be shown! Hidden by (owner) PageHeader 
         *
         * Under Priv Server (doesnt matter if green/red)
         * 
         *  - Just 1 button always, showing "Up-To-Date" "Expired 15/75" "N/D 5/75" or "Expired 15 N/D 5" etc
         *  - Always attempts to get all missing data from Priv Server when pressed (both expired & N/D's together)
         *  - Wait's fetch operation to finish, and knows if its going on also on PFS.Client side
         *  - If user jumps to other page before finishing then still shows "updating" maybe just dont auto update it off anymore !!!TEST!!!
         *  - Never disabled, so even if shows "Up-To-Date" still can press and its actually making update attempt to priv serv
         *
         * Local Mode
         *
         * 	- Minimum 1 button that shows "Up-To-Date" or "Expired 15/75", and press does local update for expired tickers
         *	- If has N/D's then 2 buttons, second showing "N/D 5"
         *  - Pressing main button makes instant attemp to Locally fetch data for all 3 hour Expired tickers (await's full operation)
         *  - Pressing N/A button if its visible, is going to make SOLO fetch attempts to all No Data stocks (await's full operation)
         */

        [Inject] IDialogService Dialog { get; set; }
        [Inject] PfsClientAccess PfsClientAccess { get; set; }
        [CascadingParameter] int Garbage { get; set; } // Note! this is cascaded from PageHeader to tricker 'OnParametersSet'.. so its used atm!

        // 'Stock Status' shows if local client has latest assumed market data for stock this users account has
        protected string _stockStatusText = "uninitialized";
        protected bool _stockStatusDisable = true;

        protected string _naStatusText = "uninitialized";
        protected bool _naHidden = true;

        protected bool _privSrvEnabled = false;

        protected override void OnParametersSet() // !!!WARNING!!! This doesnt launch correctly atm wo owner PageHeader cascading some grabage here.. 
        {                                         // as wo 'Garbage' this would not be called when moving from group to other w navmenu
            UpdateState();
        }

        protected void UpdateState()
        {
            // Behaviour is different for Local Connection than Priv Server configured
            _privSrvEnabled = PfsClientAccess.PrivSrvMgmt().Property("ENABLED") == "TRUE" ? true : false;

            LocalEodDataStatus eodStatus = PfsClientAccess.Fetch().GetLocalEodDataStatus();

            if (eodStatus.UpdateOnGoing == true)
            {
                ChangeToUpdatingStatus();
            }
            else if (_privSrvEnabled == true)
            {
                // For Private Server, with just one button, either all is ok or missing data / expired data right after closing is shown as issue
                // Note! Doesnt mean private server has data available for long time yet.. but warning is given its old.. user can figure out rest!

                if (eodStatus.NoDataStocks == 0 && eodStatus.ExpiredStocks == 0)
                {
                    _stockStatusText = "Up-to-date";
                }
                else if (eodStatus.ExpiredStocks > 0 && eodStatus.NoDataStocks == 0)
                {
                    _stockStatusText = string.Format("OK {0} Expired {1}", eodStatus.TotalTrackedStocks - eodStatus.ExpiredStocks, eodStatus.ExpiredStocks);
                }
                else if (eodStatus.ExpiredStocks == 0 && eodStatus.NoDataStocks > 0)
                {
                    _stockStatusText = string.Format("OK {0} N/D {1}", eodStatus.TotalTrackedStocks - eodStatus.NoDataStocks, eodStatus.NoDataStocks);
                }
                else if (eodStatus.ExpiredStocks > 0 && eodStatus.NoDataStocks > 0)
                {
                    _stockStatusText = string.Format("Expired {0}, N/D {1}", eodStatus.ExpiredStocks, eodStatus.NoDataStocks);
                }
            }
            else
            {
                // Local Mode, main button focuses to expired status... with allowing actual local fetch to be done only 3 hours after 

                if (eodStatus.ExpiredStocks == 0)
                {
                    _stockStatusText = "Up-to-date";
                    _stockStatusDisable = true;
                }
                else
                {
                    _stockStatusText = string.Format("OK {0} Expired {1}", eodStatus.TotalTrackedStocks - eodStatus.ExpiredStocks - eodStatus.NoDataStocks, 
                                                                           eodStatus.ExpiredStocks);

                    if (eodStatus.ExpiryMins.Max() < 180)
                        // Now we have expired stocks, but we DO NOT allow fetch except if something is actually expired more than 3 hours ago...
                        _stockStatusDisable = true;
                    else
                        _stockStatusDisable = false;
                }

                if (eodStatus.NoDataStocks == 0)
                {
                    _naHidden = true;
                }
                else
                {
                    _naHidden = false;
                    _naStatusText = string.Format("N/D {0}", eodStatus.NoDataStocks);
                }
            }
        }

        protected async Task OnBtnUpdateStocksAsync()
        {
            _privSrvEnabled = PfsClientAccess.PrivSrvMgmt().Property("ENABLED") == "TRUE" ? true : false;

            if (_privSrvEnabled == true)
            {
                bool privSrvConnected = PfsClientAccess.PrivSrvMgmt().Property("CONNECTED") == "TRUE" ? true : false;

                if (privSrvConnected == true)
                {
                    // For PrivSrv connected mode... if clicked button we attempt to fetch from Priv Srv each and every stock that has market 
                    // closed even if its just second ago... and button shows same... this means it shows expired way before data maybe available

                    ChangeToUpdatingStatus();

                    await PfsClientAccess.Fetch().DoUpdateExpiredEodAsync();

                    UpdateState();
                    return;
                }

                // Has Priv Srv setup, but its RED.. show Message Box and do Local
                bool? result = await Dialog.ShowMessageBox("Cant connect Private Server atm", "Perform fetch locally!", yesText: "Ok", cancelText: "Cancel");

                if (result.HasValue == false || result.Value == false)
                    return;

                // Drop out to normal local handling...
            }

            ChangeToUpdatingStatus();

            await PfsClientAccess.Fetch().DoUpdateExpiredEodAsync();

            UpdateState();
            return;
        }

        protected void ChangeToUpdatingStatus()
        {
            _stockStatusText = "...updating...";
            _stockStatusDisable = true;
            _naHidden = true;
        }

        protected async Task OnBtnUpdateNoDatasAsync()
        {
            ChangeToUpdatingStatus();

            await PfsClientAccess.Fetch().DoFetchNoDataEodAsync();

            UpdateState();
            return;
        }
    }
}
