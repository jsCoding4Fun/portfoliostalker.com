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

namespace PfsDevelUI.Components
{
    public partial class DlgViewContentString
    {
        [Inject] PfsClientAccess PfsClientAccess { get; set; }
        [CascadingParameter] MudDialogInstance MudDialog { get; set; }

        [Parameter] public string Content { get; set; }      // These fill automatically per caller pages 'DialogParameters' 

        private void DlgCancel()
        {
            MudDialog.Cancel();
        }
    }
}
