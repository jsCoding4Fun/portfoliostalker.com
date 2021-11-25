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
    // Lists allowed admins + users for PrivSrv and allows to add/change user's status 
    public partial class CompPrivSrvUserMgmt
    {
        [Inject] PfsClientAccess PfsClientAccess { get; set; }
        [Inject] private IDialogService Dialog { get; set; }

        protected List<ViewUser> _view = null;

        protected bool _editOnly = false;
        protected PrivSrvUserInfo _editUser = null;
        protected DateTime? _editExpirationDate = null;
        protected string _editProperties = string.Empty;

        protected override async Task OnParametersSetAsync()
        {
            SetEditUserEmpty();
            await ReloadAsync();
        }

        protected void SetEditUserEmpty()
        {
            _editUser = new()
            {
                Admin = false,
                Expiration = DateTime.MaxValue,
                Username = string.Empty,
                Note = string.Empty,
                TrackedStocks = 0,
            };
            _editExpirationDate = null;
            _editOnly = false;
            _editProperties = string.Empty;
        }

        protected async Task ReloadAsync()
        {
            List<PrivSrvUserInfo> allUserInfo = await PfsClientAccess.PrivSrvMgmt().UserListGetAsync();

            _view = new();

            foreach (PrivSrvUserInfo user in allUserInfo)
            {
                ViewUser entry = new()
                {
                    d = user,
                };

                if (user.Admin == true)
                    entry.Expiration = "N/A";
                else if (user.Expiration == DateTime.MaxValue)
                    entry.Expiration = "unlimited";
                else if (user.Expiration == DateTime.MinValue)
                    entry.Expiration = "expired";
                else
                    entry.Expiration = user.Expiration.ToString("yyyy-MMM-dd");

                if (user.Admin == true)
                    entry.ShowName = "[ADMIN]:" + user.Username;
                else
                    entry.ShowName = user.Username;

                _view.Add(entry);
            }
            StateHasChanged();
        }

        protected async Task OnBtnAddUserAsync()
        {
            if (string.IsNullOrWhiteSpace(_editUser.Username) == true || 
                _editUser.Username.All(c => Char.IsLetterOrDigit(c)) == false ||
                _editUser.Username.Length > 64 )
                return;

            PrivSrvUserInfo newUser = new()
            {
                Admin = false,
                Username = _editUser.Username.ToUpper(),
                Note = _editUser.Note,
                Expiration = DateTime.MaxValue,
                TrackedStocks = 0,
            };

            if (_editExpirationDate != null)
                newUser.Expiration = _editExpirationDate.Value;

            await PfsClientAccess.PrivSrvMgmt().UserUpdateAsync(newUser);

            SetEditUserEmpty();

            await ReloadAsync();
        }

        protected async Task OnBtnSaveUserAsync()
        {
            if (_editOnly == false)
                return;

            PrivSrvUserInfo updateUser = new()
            {
                Admin = false,
                Username = _editUser.Username,
                Note = _editUser.Note,
                Expiration = DateTime.MaxValue,
                TrackedStocks = 0,
            };

            if (_editExpirationDate != null)
                updateUser.Expiration = _editExpirationDate.Value;


            if (string.IsNullOrWhiteSpace(_editProperties) == false)
            {
                if (updateUser.SetProperties(_editProperties) == false)
                    return;
            }

            await PfsClientAccess.PrivSrvMgmt().UserUpdateAsync(updateUser);

            SetEditUserEmpty();

            await ReloadAsync();
        }

        protected async Task OnBtnDeleteUserAsync()
        {
            if (_editOnly == false)
                return;

            if (string.IsNullOrWhiteSpace(_editUser.Note) == true)
            {
                await Dialog.ShowMessageBox("Note field is required!", "Please enter description of reason to NOTE field.", yesText: "Ok");
                return;
            }

            bool? result = await Dialog.ShowMessageBox("Be carefull!", "Are you sure this user is remove totally from server?", yesText: "Ok", cancelText: "Cancel");

            if (result.HasValue == false || result.Value == false)
                return;

            PrivSrvUserInfo deleteUser = new()
            {
                Admin = false,
                Username = _editUser.Username,
                Note = _editUser.Note,
                Expiration = DateTime.MinValue,     // !!!LATER!!! lazy hands, this marks end of user atm!
                TrackedStocks = 0,
            };

            await PfsClientAccess.PrivSrvMgmt().UserUpdateAsync(deleteUser);

            SetEditUserEmpty();

            await ReloadAsync();
        }

        private void OnRowClicked(TableRowClickEventArgs<ViewUser> data)
        {
            if (data.Item.d.Admin == true)
                // We do not edit admins on UI, only manually on configs file
                return;

            _editOnly = true;
            _editUser = data.Item.d;
            _editExpirationDate = null;
            _editProperties = data.Item.d.UserProperties;

            if (data.Item.d.Expiration != DateTime.MaxValue)
                _editExpirationDate = data.Item.d.Expiration;
        }

        public class ViewUser
        {
            public PrivSrvUserInfo d { get; set; }

            public string ShowName { get; set; }

            public string Expiration { get; set; }
        }

    }
}
