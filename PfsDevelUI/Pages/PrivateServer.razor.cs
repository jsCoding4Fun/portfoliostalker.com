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

using PFS.Shared.UiTypes;

namespace PfsDevelUI.Pages
{
    public partial class PrivateServer
    {
        [Inject] PfsClientAccess PfsClientAccess { get; set; }

        protected bool _allowPrivServer = false;
        protected bool _allowUserMgmt = false;

        protected async override Task OnInitializedAsync()
        {
            if (PfsClientAccess.PrivSrvMgmt().Property("ENABLED") != "TRUE"
             || PfsClientAccess.PrivSrvMgmt().Property("CONNECTED") != "TRUE"
             || PfsClientAccess.PrivSrvMgmt().Property("ADMIN") != "TRUE")
                // Need to be connected & admin for connected PrivSrv to do anything here..
                return;

            AccountTypeID SessionAccountType = (AccountTypeID)Enum.Parse(typeof(AccountTypeID), PfsClientAccess.Account().Property("ACCOUNTTYPE"));

            switch (SessionAccountType)
            {
                case AccountTypeID.Gold:
                case AccountTypeID.Platinum:
                case AccountTypeID.Admin:
                    _allowPrivServer = true;
                    break;
            }

            Dictionary<PrivSrvProperty, string>  srvProperties = await PfsClientAccess.PrivSrvMgmt().SrvConfigPropertyGetAllAsync();

            // Note! Yes this is OK, person is Admin for this PrivSrv and this is authenticated ala PFS owned PrivSrv so we show UserMgmt
            if (srvProperties.ContainsKey(PrivSrvProperty.AuthenticatedRO) && srvProperties[PrivSrvProperty.AuthenticatedRO] == "TRUE")
                _allowUserMgmt = true;
        }
    }
}
