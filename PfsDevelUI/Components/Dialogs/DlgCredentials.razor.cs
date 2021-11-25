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
using System.ComponentModel.DataAnnotations;

namespace PfsDevelUI.Components
{
    public partial class DlgCredentials
    {
        [CascadingParameter] MudDialogInstance MudDialog { get; set; }
        [Inject] private IDialogService Dialog { get; set; }
        [Inject] PfsClientAccess PfsClientAccess { get; set; }

        protected bool _fullscreen = false;
        protected UserInfo _userinfo = null;
        protected bool _showBusySignal = false;

        [Parameter] public UseCaseID UseCase { get; set; } = UseCaseID.UNKNOWN;

        public enum UseCaseID
        {
            UNKNOWN = 0,
            REGISTER,
            CHANGE_PASSWORD,
            CHANGE_EMAIL,
            DISCLAIMER,
        }

        protected override void OnInitialized()
        {
            _userinfo = new()
            {
            };

            if (UseCase == UseCaseID.REGISTER)
                UseCase = UseCaseID.DISCLAIMER;
        }

        protected void DlgAgreed()
        {
            UseCase = UseCaseID.REGISTER;
        }

        protected void OnFullScreenChanged(bool fullscreen)
        {
            _fullscreen = fullscreen;

            MudDialog.Options.FullWidth = _fullscreen;
            MudDialog.SetOptions(MudDialog.Options);
        }

        private void DlgCancel()
        {
            MudDialog.Cancel();
        }

        private async Task DlgOkAsync()
        {
            if ( UseCase == UseCaseID.REGISTER && LocalVerifyUsername() == false )
            {
                await Dialog.ShowMessageBox("Failed!", "Invalid username", yesText: "Ok");
                return;
            }

            if ((UseCase == UseCaseID.REGISTER || UseCase == UseCaseID.CHANGE_PASSWORD) && LocalVerifyNewPassword() == false)
            {
                await Dialog.ShowMessageBox("Failed!", "New password invalid.", yesText: "Ok");
                return;
            }

            switch ( UseCase )
            {
                case UseCaseID.REGISTER:
                    {
                        _showBusySignal = true;
                        bool ret = await PfsClientAccess.Account().UserRegisterAsync(_userinfo.Username, _userinfo.NewPassword, _userinfo.Email);
                        _showBusySignal = false;

                        if (ret == true)
                        {
                            bool? result = await Dialog.ShowMessageBox("Success!", "Registeration done, please login with username/password", yesText: "Ok");
                            MudDialog.Close();
                        }
                        else
                        {
                            bool? result = await Dialog.ShowMessageBox("Register Failed!", "Duplicate username?", yesText: "Ok");
                        }
                    }
                    break;

                case UseCaseID.CHANGE_PASSWORD:
                    {
                        _showBusySignal = true;
                        bool ret = await PfsClientAccess.Account().UserChangePasswordAsync(_userinfo.CurrentPassword, _userinfo.NewPassword);
                        _showBusySignal = false;

                        if (ret == true)
                        {
                            bool? result = await Dialog.ShowMessageBox("Success!", "Password replaced successfully", yesText: "Ok");
                            MudDialog.Close();
                        }
                        else
                        {
                            bool? result = await Dialog.ShowMessageBox("Failed!", "Invalid current password given?", yesText: "Ok");
                        }
                    }
                    break;

                case UseCaseID.CHANGE_EMAIL:
                    {
                        _showBusySignal = true;
                        bool ret = await PfsClientAccess.Account().UserChangeEmailAsync(_userinfo.CurrentPassword, _userinfo.Email);
                        _showBusySignal = false;

                        if (ret == true)
                        {
                            bool? result = await Dialog.ShowMessageBox("Success!", "Email replaced successfully", yesText: "Ok");
                            MudDialog.Close();
                        }
                        else
                        {
                            bool? result = await Dialog.ShowMessageBox("Failed!", "Invalid current password given?", yesText: "Ok");
                        }
                    }
                    break;
            }


            bool LocalVerifyUsername()
            {
                if (string.IsNullOrWhiteSpace(_userinfo.Username) == true)
                    return false;

                return true;
            }

            bool LocalVerifyNewPassword()
            {
                if (_userinfo.NewPassword.Length < 3 || _userinfo.NewPassword != _userinfo.NewPassword2)
                    return false;

                return true;
            }
        }
    }

    public class UserInfo
    {
        public string Username { get; set; }
        public string CurrentPassword { get; set; }
        public string NewPassword { get; set; }
        public string NewPassword2 { get; set; }
        public string Email { get; set; }
    }
}