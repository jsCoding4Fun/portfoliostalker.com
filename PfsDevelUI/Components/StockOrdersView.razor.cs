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
    // Super simple component to be used on stock report's dropdown to view orders wo editing etc (used as per specific portfolio under are atm)
    public partial class StockOrdersView
    {
        [Parameter] public IReadOnlyCollection<StockOrder> Orders { get; set; }

        [Parameter] public CurrencyCode Currency { get; set; } = CurrencyCode.Unknown;

        // !!!DECISION!!! This is for StockReport, and to keep UI work minimal StockReport doesnt allow editing, as updating report after each
        //                edit and then trying to show updates wo loosing position on report would most propably get complex.. and atm UI is 
        //                not supposed to cause complex coding... so no delete, no edit, no convert... just very simple view!
        //                => Place to edit/delete/convert Order's is under Stock itself.. 
    }
}
