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

using PfsDevelUI;
using PfsDevelUI.Shared;
using PfsDevelUI.PFSLib;

using BlazorDownloadFile;

using Serilog;

using PFS.Shared.UiTypes;

namespace PfsDevelUI.Components
{
    public partial class PageHeader
    {
        /* !!!DOCUMENT!!! ONLINE/OFFLINE overview of UI indicators & available features
         * 
         * 1) Full Features with both main serv and private server online -> username visible + green OK cloud
         * 
         *      -> More instant backups, reports, and history viewing available
         *      -> daily data and history is fetched from private server
         * 
         * 2) Normal mode, where has logged on, but no private server setup / available -> username visible
         * 
         *      - If private server is configured, then shows red error cloud as cant connect (maybe out of home internet)
         *         (anyway can try to click cloud icon to reconnect, as its not done automatically atm)
         *      - If private server is not even used, ala not configured, then no cloud icon is shown next to username
         *      
         *      -> Can add new stocks tracking, see some reports, no history viewing, daily data is fetched by web appl
         *      
         * 3) Show 'not logged in' -> clicking it would open login/register pages.. 
         * 
         *      -> Still actually usable per what ever data/stocks is on local storage as its not removed/protected
         *      -> This is all valid use situation as anyway application only uses daily closing data so most time
         *         if doesnt have private server data updates rarely...
         *      -> Can still see alarms & profits & do notes etc...
         */

        [Inject] PfsUiState PfsUiState { get; set; }
        [Inject] IDialogService Dialog { get; set; }
        [Inject] PfsClientAccess PfsClientAccess { get; set; }
        [Inject] IBlazorDownloadFileService BlazorDownloadFileService { get; set; }
        [Inject] NavigationManager NavigationManager { get; set; }

        // ""AddUnder"" is ugly name for '+' button thats used to add groups under portfolio, etc what ever is under current page
        [Parameter] public string AddUnderBtnText { get; set; }
        [Parameter] public EventCallback OnAddUnder { get; set; }
        [Parameter] public EventCallback OnNewNews { get; set; }
        [Parameter] public EventCallback EvLoginSuccessfull { get; set; }   // !!!TODO!!! All these 'EventCallback' should be Ev... not On...

        [Parameter] public string Username { get; set; }
        [Parameter] public List<MenuItem> MenuItems { get; set; } = null;
        [Parameter] public EventCallback<string> OnMenuSelEventAsync { get; set; }

        [Parameter] public int Garbage { get; set; }

        protected bool _allowPrivServer = false;
        protected bool _privSrvEnabled = false;
        protected bool _privSrvConnected = false;
        protected bool _privSrvAdmin = false;
        protected int _unreadNewsAmount = -1;
        protected bool _recording = false;
        protected bool _isDemoMode = false;
        protected bool _showSaveAndExitBtn = false;

        protected override void OnParametersSet()
        {
            // Note! This is received on "sub page changes" when jumping example from group to other w NavMenu
            UpdateStateVariables();

            // !!!LATER!!! 'CompStockStatus' OnParameterSet() did not wanna get called if changing w NavMenu from group to other,
            //             even this components did.. so cascading garbage random value there to make sure it gets updated also!
            Random rnd = new Random();
            Garbage = rnd.Next(1, 10000);
        }

        protected void UpdateStateVariables()
        {
            AccountTypeID SessionAccountType = (AccountTypeID)Enum.Parse(typeof(AccountTypeID), PfsClientAccess.Account().Property("ACCOUNTTYPE"));

            switch (SessionAccountType)
            {
                case AccountTypeID.Gold:
                case AccountTypeID.Platinum:
                case AccountTypeID.Admin:
                    _allowPrivServer = true;
                    break;

                case AccountTypeID.Demo:
                    _allowPrivServer = true;
                    _isDemoMode = true;
                    break;
            }

            Username = PfsClientAccess.Account().Property("USERNAME");

            if (_isDemoMode == false && Username.Contains("DEMO") == true)
                _showSaveAndExitBtn = true;

            if (PfsClientAccess.Account().AccountProperty(UserSettProperty.NoLocalStorage.ToString()) == "TRUE" )
                _showSaveAndExitBtn = true;

            int unreadnews = int.Parse(PfsClientAccess.Account().Property("UNREADNEWS"));

            if (unreadnews != _unreadNewsAmount)
            {
                if ( _unreadNewsAmount == -1 )
                {
                    // Bit of nastyness, as dont wanna do invoke on first round of setups
                    _unreadNewsAmount = unreadnews;
                }
                else
                {
                    _unreadNewsAmount = unreadnews;
                    OnNewNews.InvokeAsync();
                }
            }

            _recording = false;

            if (PfsClientAccess.Account().Property("RECORDING") == "TRUE")
                _recording = true;

            _privSrvEnabled = false;
            _privSrvConnected = false;
            _privSrvAdmin = false;

            if (_allowPrivServer)
            {
                if (PfsClientAccess.PrivSrvMgmt().Property("ENABLED") == "TRUE")
                {
                    _privSrvEnabled = true;

                    if (PfsClientAccess.PrivSrvMgmt().Property("CONNECTED") == "TRUE")
                    {
                        _privSrvConnected = true;

                        if (PfsClientAccess.PrivSrvMgmt().Property("ADMIN") == "TRUE")
                            _privSrvAdmin = true;
                    }
                }
            }
        }

        // Used by Demo account logins w special buttons and no password
        public async Task<bool> OnLoginAccountByCall(string username)
        {
            // Thats minimal checking but ok, lets go then
            string errorMsg = await PfsClientAccess.Account().UserLoginAsync(username, string.Empty, false);

            if (string.IsNullOrEmpty(errorMsg) == false)
            {
                await Dialog.ShowMessageBox("Login Failed!", errorMsg, yesText: "Ok");
                return false;
            }

            // Need to get state updated as 'DoPrivateSrvLogin' needs it, plus we update UI as can redo update after login if successfull
            UpdateStateVariables();
            StateHasChanged();

            await PerformPostLoginSetupAsync();
            return true;
        }

        protected async Task OnUsernameClickedAsync()
        {
            var dialog = Dialog.Show<DlgLogin>();
            var result = await dialog.Result;

            if (!result.Cancelled)
            {
                // 
                DlgLoginRespTypes resp = (DlgLoginRespTypes)Enum.Parse(typeof(DlgLoginRespTypes), result.Data.ToString());

                if ( resp == DlgLoginRespTypes.REGISTER )
                {
                    var parameters = new DialogParameters();
                    parameters.Add("UseCase", DlgCredentials.UseCaseID.REGISTER);

                    // User selected -register- shortcut, so lets show that dialog instead, dont care what happens on it..
                    dialog = Dialog.Show<DlgCredentials>("", parameters);
                    await dialog.Result;
                }
                else if (resp == DlgLoginRespTypes.OK)
                {
                    // Need to get state updated as 'DoPrivateSrvLogin' needs it, plus we update UI as can redo update after login if successfull
                    UpdateStateVariables();
                    NavigationManager.NavigateTo("/");
                    PfsUiState.UpdateNavMenu();
                    StateHasChanged();

                    _ = PerformPostLoginSetupAsync();

                    _ = EvLoginSuccessfull.InvokeAsync();
                }
            }
            return;
        }

        protected async Task PerformPostLoginSetupAsync()
        {
            if (_allowPrivServer)
                // LOGIN SUCCESSFULL, so connected to Pfs Srv.. get private server update going on background
                await DoPrivateSrvLogin();

            else if (PfsClientAccess.PrivSrvMgmt().Property("ENABLED") == "TRUE")
            {
                // Special case! In this case PrivSrv is NOT allowed, but user has it still configured.. and that means
                // user cant see privsrv settings anymore to remove it. This happens example when loosing platinum and 
                // dropping back to free mode. => This case we wanna remove automatically priv srv address

                // !!!LATER!!! This really should be done on PFS.Client, NOT on UI.. actually PrivSrv login should be there also!
                PfsClientAccess.PrivSrvMgmt().Property("REMOVE");
            }

            if (PfsClientAccess.StalkerMgmt().PortfolioNameList().Count() == 0 || Username.Contains("DEMO") == true)
            {
                // Login was successfull, and looks like we are pretty darn empty looking Stalker here... lets see if 
                // Pfs has backup done that we could offer for user
                await Local_CheckIfPfsHasBackup();
            }
            return;


            async Task Local_CheckIfPfsHasBackup()
            {
                (byte[] content, DateTime backupDateUTC) = await PfsClientAccess.Account().FetchAccountBackupZipFromPfsAsync();

                if (content == null || content.Length == 0)
                    return;

                if (Username.Contains("DEMO") == true)
                {
                    // DemoMode and DemoEdit always starts w backup data... 
                    if (PfsClientAccess.Account().ImportAccountFromZip(content) == true)
                    {
                        // This is DEMO account either on DemoMode or DemoEdit, to speed things up lets do PrivSrv connection
                        if (string.IsNullOrEmpty(await PfsClientAccess.PrivSrvMgmt().UserLoginAsync()) == true)
                        {
                            // errorMsg was empty so we successfully logged in to PrivSrv now... kick update of data going on
                            _ = PfsClientAccess.Fetch().DoUpdateExpiredEodAsync();
                        }

                        UpdateStateVariables();
                        StateHasChanged();
                        PfsUiState.UpdateNavMenu();
                    }
                }
                else // As everything seams empty, and there is backup on PFS, lets see if user wants to use it..
                {
                    string msg = string.Format("Found backup from {0}UTC, size {1} bytes. Would you like to use backup?",
                            backupDateUTC.ToString("yyyy-MMM-dd HH:mm"), content.Length);

                    bool? result = await Dialog.ShowMessageBox("Use PFS backup?", msg, yesText: "Use", cancelText: "Cancel");

                    if (result.HasValue == true && result.Value == true)
                    {
                        if (PfsClientAccess.Account().ImportAccountFromZip(content) == true)
                        {
                            StateHasChanged();
                            PfsUiState.UpdateNavMenu();
                        }
                        else
                        {
                            await Dialog.ShowMessageBox("Failed!", "Something go wrong, could not restore backup", cancelText: "Ok");
                        }
                        return;
                    }
                }
            }
        }

        protected async Task OnBtnSaveAndExitAsync()
        {
            bool success = await PfsClientAccess.Account().BackupAccountZipToPfsAsync();

            if (success == false)
            {
                await Dialog.ShowMessageBox("Backup failed!", "Failed to create PFS backup!", yesText: "Ok");
                return;
            }

            NavigationManager.NavigateTo("/", true);
        }

        protected void OnBtnExitDemoMode()
        {
            NavigationManager.NavigateTo("/", true);
        }

        protected async Task OnBtnRecordingOpenAsync()
        {
            // Note! This also trickers some pending logs if havent logged anything after startup...
            Log.Information("Launching OnBtnRecordingOpenAsync() to show Recordings");

            // Launch to show Recordings, and allow User to report bug's on Application (requires Recording feature to see this button)
            var dialog = Dialog.Show<DlgApplBugCreate>("", new DialogOptions() { CloseButton = true });
            await dialog.Result;
        }

        protected void OnBtnUnreadNews()
        {
            NavigationManager.NavigateTo("/");
        }

        protected async Task OnCustomMenuSelAsync(string ID)
        {
            await OnMenuSelEventAsync.InvokeAsync(ID);
        }

        protected async Task OnBtnPrivSrvTryReconnectAsync()
        {
            if (_allowPrivServer == false)
                return;

            await DoPrivateSrvLogin();
        }

        protected async Task DoPrivateSrvLogin()
        {
            if (_allowPrivServer == false || _privSrvEnabled == false || _privSrvConnected == true)
                return;

            string errorMsg = await PfsClientAccess.PrivSrvMgmt().UserLoginAsync();

            if ( string.IsNullOrEmpty(errorMsg) == true )
            {
                // And update everything per latest status
                UpdateStateVariables();
                StateHasChanged();
            }
            else
                await Dialog.ShowMessageBox("Login PrivSrv Failed!", errorMsg, yesText: "Ok");        
        }

        private Task AddUnder()
        {
            return OnAddUnder.InvokeAsync();
        }

        protected async void OnExportAccount()
        {
            // Alternative, not attempted yet: https://www.meziantou.net/generating-and-downloading-a-file-in-a-blazor-webassembly-application.htm

            byte[] zip = PfsClientAccess.Account().ExportAccountAsZip();
            string fileName = "PfsExport_" + DateTime.Today.ToString("yyyyMMdd") + ".zip";
            await BlazorDownloadFileService.DownloadFile(fileName, zip, "application/zip");
        }

        public class MenuItem
        {
            public string ID { get; set; }      // Page specific item ID, example string version of its MenuID ENUM's

            public string Text { get; set; }    // Menu Text visible for user
        }
    }
}
