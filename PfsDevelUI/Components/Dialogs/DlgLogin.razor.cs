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
using MudBlazor;
using System.ComponentModel.DataAnnotations;

using PfsDevelUI.Shared;
using PfsDevelUI.PFSLib;

namespace PfsDevelUI.Components
{
    public partial class DlgLogin
    {
        [CascadingParameter] MudDialogInstance MudDialog { get; set; }

        [Inject] private IDialogService Dialog { get; set; }
        [Inject] PfsClientPlatform PfsClientPlatform { get; set; }
        [Inject] PfsUiState PfsUiState { get; set; }
        [Inject] PfsClientAccess PfsClientAccess { get; set; }

        protected bool _fullscreen = false;
        protected bool _remember = false;
        protected DlgLoginFormData _userinfo = null;
        protected string _defUsername = "";
        protected bool _showBusySignal = false;

        protected override void OnInitialized()
        {
            _defUsername = PfsClientAccess.Account().Property("DEFUSERNAME");

            if (string.IsNullOrEmpty(_defUsername) == false)
                // Keep remember me setting on
                _remember = true;

            _userinfo = new()
            {
                Username = _defUsername,
            };
        }

        protected void OnSwapRegister()
        {
            MudDialog.Close(DialogResult.Ok(DlgLoginRespTypes.REGISTER));
        }

        protected void OnUsernameChanged(string username)
        {
            if (string.IsNullOrEmpty(_defUsername) == false)
            {
                // RememberMe is finished if username is edited
                _defUsername = string.Empty;
                _remember = false;
                StateHasChanged();
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

        private async Task DlgOkAsync()
        {
            // Little bit of verifications

            if (string.IsNullOrWhiteSpace(_userinfo.Username) == true)
            {
                // Must have username
                await Dialog.ShowMessageBox("Failed!", "Give username", yesText: "Ok");
                return;
            }

            if (string.IsNullOrEmpty(_defUsername) == true && string.IsNullOrWhiteSpace(_userinfo.Password) == true)
            {
                if (_userinfo.Username.ToUpper().Contains("DEMO") == false)
                {
                    // Must have password if not previous 'Remember Me' active
                    await Dialog.ShowMessageBox("Failed!", "Give password", yesText: "Ok");
                    return;
                }
            }

            _showBusySignal = true;

            // Thats minimal checking but ok, lets go then
            string errorMsg = await PfsClientAccess.Account().UserLoginAsync(_userinfo.Username, _userinfo.Password, _remember);

            _showBusySignal = false;

            if (string.IsNullOrEmpty(errorMsg) == true)
            {
                // close dialog and let caller know 'OK'
                MudDialog.Close(DialogResult.Ok(DlgLoginRespTypes.OK));
            }
            else
            {
                await Dialog.ShowMessageBox("Login Failed!", errorMsg, yesText: "Ok");
                StateHasChanged();
            }
        }
    }

    internal enum DlgLoginRespTypes
    {
        UNKNOWN = 0,
        OK,
        REGISTER,
    }

    public class DlgLoginFormData
    {
        [Required]
        [StringLength(32, MinimumLength = 5)]
        public string Username { get; set; }

        [Required]
        [StringLength(32, MinimumLength = 8)]
        public string Password { get; set; }
    }
}
