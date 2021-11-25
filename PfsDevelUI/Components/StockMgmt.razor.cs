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
using PfsDevelUI.Shared;
using PfsDevelUI.PFSLib;
using MudBlazor;

using PFS.Shared.UiTypes;

namespace PfsDevelUI.Components
{
    public partial class StockMgmt
    {
        // UI THINK! Maybe put History & Alarms should be on same tab...
        [Inject] PfsClientAccess PfsClientAccess { get; set; }

        [Parameter] public Guid STID { get; set; }

        // https://www.oqtane.org/Resources/Blog/PostId/529/calling-a-child-component-method-from-a-parent-component-in-blazor
        StockAlarmEdit _childStockAlarmEdit;

        protected bool _allowPerformanceTab = false;

        protected override void OnParametersSet()
        {
            AccountTypeID SessionAccountType = (AccountTypeID)Enum.Parse(typeof(AccountTypeID), PfsClientAccess.Account().Property("ACCOUNTTYPE"));

            _allowPerformanceTab = false;

            switch (SessionAccountType)
            {
                case AccountTypeID.Platinum:
                case AccountTypeID.Admin:
                case AccountTypeID.Demo:
                    _allowPerformanceTab = true;
                    break;

                case AccountTypeID.Gold:
                    // Not gold as would show BTM % type information there thats only for Platinum
                    break;
            }

            if (PfsClientAccess.PrivSrvMgmt().Property("CONNECTED") != "TRUE")
            {
                // Hups, never mind.. not allowing any indicator fields if not active connection to priv srv
                _allowPerformanceTab = false;
            }
        }

        protected async Task OnBtnAddAlarmAsync()
        {
            await _childStockAlarmEdit.AddNewAlarmAsync();
        }
    }
}
