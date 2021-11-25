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
using System.Globalization;
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

using BlazorDownloadFile;

using PFS.Shared.Types;
using PFS.Shared.UiTypes;

namespace PfsDevelUI.Components
{
    public partial class TabSettings
    {
        [Inject] PfsClientAccess PfsClientAccess { get; set; }
        [Inject] PfsUiState PfsUiState { get; set; }
        [Inject] private IDialogService Dialog { get; set; }
        [Inject] IBlazorDownloadFileService BlazorDownloadFileService { get; set; }
        [Inject] NavigationManager NavigationManager { get; set; }

        protected string _privSrvAddr = "";
        protected bool _privSrvConnected = false;
        protected string _accountType = "";

        protected bool _allowPrivServer = false;
        protected bool _isDemoMode = false;         // DemoMode
        protected bool _showBusySignal = false;
        protected bool _allowDebugRecording = false;
        protected bool _allowCleanAllLocal = true;

        protected override void OnInitialized()
        {
            AccountTypeID SessionAccountType = (AccountTypeID)Enum.Parse(typeof(AccountTypeID), PfsClientAccess.Account().Property("ACCOUNTTYPE"));

            switch (SessionAccountType)
            {
                case AccountTypeID.Gold:
                case AccountTypeID.Platinum:
                    _allowPrivServer = true;
                    break;

                case AccountTypeID.Admin:
                    _allowPrivServer = true;
                    _allowDebugRecording = true;
                    break;

                case AccountTypeID.Demo:
                    _allowPrivServer = true;
                    _isDemoMode = true;
                    _allowCleanAllLocal = false; // This is off just for real demo mode, demo editing should have it to allow resetting demo
                    break;
            }

            UpdateAllSettings();
        }

        // To be used on Init and after ClearAll/RestoreBackup
        protected void UpdateAllSettings()
        {
            _privSrvAddr = "";
            _privSrvConnected = false;
            _accountType = "";
            _backups = null;
            _editingAddr = "";

            UpdateCurrencyFields();
            UpdatePfsBackup();
            UpdateUserProperties();

            if (_allowPrivServer)
            {
                _privSrvAddr = PfsClientAccess.PrivSrvMgmt().Property("ADDRPORT");
                _privSrvConnected = PfsClientAccess.PrivSrvMgmt().Property("CONNECTED") == "TRUE" ? true : false;
            }

            _recordDebugData = PfsClientAccess.Account().Property("RECORDING") == "TRUE" ? true : false;

            _accountType = PfsClientAccess.Account().Property("ACCOUNTTYPE");
        }

        #region CURRENCY

        protected string _homeCurrency = "";
        protected string _selCurrencyProvider = "";
        protected string _latestCurrencyDate = "-missing-";
        protected List<ExtDataProviders> _currencyProviders = null;

        protected void UpdateCurrencyFields()
        {
            _homeCurrency = PfsClientAccess.Account().Property("HOMECURRENCY");
            _selCurrencyProvider = PfsClientAccess.Account().Property("CURRENCYPROVIDER");
            _latestCurrencyDate = PfsClientAccess.Account().Property("CURRENCYCONVERSIONDATE");

            DateTime conversionDate = DateTime.ParseExact(_latestCurrencyDate, "yyyy-MMM-dd", CultureInfo.InvariantCulture);

            if (conversionDate.Year < 2021)
                _latestCurrencyDate = "-missing-";

            _currencyProviders = new();
            _currencyProviders.Add(ExtDataProviders.Polygon); // !!!LATER!!! Go fancy, check keys, has options, maybe read somewhere.. not now...
        }

        protected void OnBtnUpdateCurrencyConversionRates()
        {
            PfsClientAccess.Account().Property("CURRENCYCONVERSIONDATE", "UPDATE");
        }

        protected void OnSetHomeCurrency(CurrencyCode currency)
        {
            _homeCurrency = PfsClientAccess.Account().Property("HOMECURRENCY", currency.ToString());
            StateHasChanged();
        }

        protected void OnSetCurrencyProvider(ExtDataProviders provider)
        {
            _selCurrencyProvider = PfsClientAccess.Account().Property("CURRENCYPROVIDER", provider.ToString());
        }

        #endregion

        #region PRIVATE SERVER ADDRESS

        protected string _editingAddr = "";

        protected async Task OnBtnNewPrivSrvConnectAsync()
        {
            if (string.IsNullOrWhiteSpace(_editingAddr) == true)
                return;

            string errorMsg = await PfsClientAccess.PrivSrvMgmt().UserLoginAsync(_editingAddr);

            if (string.IsNullOrEmpty(errorMsg) == true )
            {
                _privSrvAddr = _editingAddr;

                StateHasChanged();

                await Dialog.ShowMessageBox("Connection OK!", "Please re-login to activate", yesText: "Ok");

                NavigationManager.NavigateTo("/", true); // <= enforcing reload application and jump to idle page w new login required!

            }
            else
                await Dialog.ShowMessageBox("Connection failed!", string.Format("Could not connect, or was rejected by server! {0}", errorMsg), yesText: "Ok");
        }

        protected async Task OnBtnRemovePrivSrvAsync()
        {
            _privSrvConnected = false;
            _privSrvAddr = "";

            PfsClientAccess.PrivSrvMgmt().Property("REMOVE");

            await Dialog.ShowMessageBox("Usage removed!", "Please re-login", yesText: "Ok");

            NavigationManager.NavigateTo("/", true); // <= enforcing reload application and jump to idle page w new login required!
        }

        #endregion

        #region PRIVATE SERVER BACKUPS

        protected List<string> _backups = null;

        protected async Task OnBtnBackupGetListAsync()
        {
            _backups = await PfsClientAccess.PrivSrvMgmt().BackupListAsync();
        }

        protected async Task OnFetchBackupFileAsync(string backupname)
        {
            byte[] zip = await PfsClientAccess.PrivSrvMgmt().BackupFetchAsync(backupname);
            string fileName = "PfsPrivSrvBackup_" + backupname + ".zip";
            await BlazorDownloadFileService.DownloadFile(fileName, zip, "application/zip");
        }

        #endregion

        #region CHANGE PASSWORD / EMAIL

        protected async Task OnBtnChangePasswordAsync()
        {
            var parameters = new DialogParameters();
            parameters.Add("UseCase", DlgCredentials.UseCaseID.CHANGE_PASSWORD);

            // User selected -register- shortcut, so lets show that dialog instead, dont care what happens on it..
            var dialog = Dialog.Show<DlgCredentials>("", parameters);
            await dialog.Result;
        }

        protected async Task OnBtnChangeEmailAsync()
        {
            var parameters = new DialogParameters();
            parameters.Add("UseCase", DlgCredentials.UseCaseID.CHANGE_EMAIL);

            // User selected -register- shortcut, so lets show that dialog instead, dont care what happens on it..
            var dialog = Dialog.Show<DlgCredentials>("", parameters);
            await dialog.Result;
        }

        #endregion

        #region PFS BACKUP

        protected string _pfsBackupDate = string.Empty;

        protected void UpdatePfsBackup()
        {
            _pfsBackupDate = PfsClientAccess.Account().Property("PFSBACKUPDATE");
            if (string.IsNullOrWhiteSpace(_pfsBackupDate) == true)
                _pfsBackupDate = "Create Backup";
        }

        protected async Task OnBtnStorePfsBackupAsync()
        {
            _showBusySignal = true;
            bool success = await PfsClientAccess.Account().BackupAccountZipToPfsAsync();
            _showBusySignal = false;

            if ( success == false )
            {
                await Dialog.ShowMessageBox("Backup failed!", "Nothing to backup, or could not connect server!", yesText: "Ok");
                return;
            }
            else
                await Dialog.ShowMessageBox("Backup Done!", "PFS only holds single backup file, so this it is!", yesText: "Ok");

            UpdatePfsBackup();
        }

        protected async Task OnBtnFetchPfsBackupAsync()
        {
            (byte[] zip, DateTime backupDateUTC) = await PfsClientAccess.Account().FetchAccountBackupZipFromPfsAsync();

            if ( zip == null )
            {
                await Dialog.ShowMessageBox("Fetch failed!", "Could not connect, or backup missing!", yesText: "Ok");
                return;
            }

            string msg = string.Format("Got backup from {0}UTC, size {1} bytes. Download to harddrive? Or take use, and delete current local data? ",
                                        backupDateUTC.ToString("yyyy-MMM-dd HH:mm"), zip.Length);

            bool? result = await Dialog.ShowMessageBox("Select what to do", msg, yesText: "Use it (carefull)", noText: "Download", cancelText: "Cancel");

            if (result.HasValue == false )
                return;

            if (result.Value == false)
            { 
                string fileName = string.Format("PfsLatestBackup_{0}.zip", backupDateUTC.ToString("yyyy-MMM-dd_HHmmUTC"));
                await BlazorDownloadFileService.DownloadFile(fileName, zip, "application/zip");
            }
            else
            {
                if (  PfsClientAccess.Account().ImportAccountFromZip(zip) == false )
                {
                    await Dialog.ShowMessageBox("Fetch failed!", "Failed to process backup!", yesText: "Ok");
                    return;
                }

                UpdateAllSettings();
                PfsUiState.UpdateNavMenu();
                StateHasChanged();
            }
        }

        #endregion

        protected async Task OnBtnImportDlgAsync()
        {
            // !!!NOTE!!! MainLayout has default's for options
            var dialog = Dialog.Show<DlgImport>("", new DialogOptions() { CloseButton = true });
            var result = await dialog.Result;

            if (!result.Cancelled)
            {
                // All done on dialog side, except need to update..
                UpdateAllSettings();
                PfsUiState.UpdateNavMenu();
                StateHasChanged();
            }
        }

        protected async Task OnBtnClearAllAsync()
        {
            bool? result = await Dialog.ShowMessageBox("Are you sure sure?", "Clears all locally, wiping this applications Local Storage in browser. Make sure you have backup! Clear all data from application?", yesText: "Ok", cancelText: "Cancel");

            if (result.HasValue == false || result.Value == false)
                return;

            PfsClientAccess.Account().ClearLocally(true);
            PfsUiState.UpdateNavMenu();
            NavigationManager.NavigateTo("/", true);        // <= enforcing reload application and jump to idle page w new login required!
            StateHasChanged();
        }

        protected bool _recordDebugData = false;

        protected void OnRecordChanged(bool state)
        {
            _recordDebugData = state;

            // PFS.Client is only used to hold state information w other settings so that gets activated on restart
            PfsClientAccess.Account().Property("RECORDING", state ? "TRUE" : "FALSE");

            // Actual recording is UI side feature, and handled by PfsClientAccess wo calling PFS.Client
            if (state)
                PfsClientAccess.RecordingOn();
            else
                PfsClientAccess.RecordingOff();
        }

        #region USER PROPERTIES

        protected Dictionary<string, string> _userProperties = null; // key is property value

        protected void UpdateUserProperties()
        {
            bool demoAcc = false;
            _userProperties = new();

            if (PfsClientAccess.Account().Property("DEMOACC") == "TRUE")
                demoAcc = true;

            // This is more flexible grid presentation showing values per UserProperties those are server stored special features

            if (demoAcc == false && PfsClientAccess.Account().AccountProperty(UserSettPropertyAdmin.ActUserPropNoLocalStorage.ToString()) == "TRUE" )
            {
                _userProperties.Add(UserSettProperty.NoLocalStorage.ToString(),
                    PfsClientAccess.Account().AccountProperty(UserSettProperty.NoLocalStorage.ToString()));
            }

            if (demoAcc == false )
            {
                _userProperties.Add(UserSettProperty.BackupPrivateKeys.ToString(),
                    PfsClientAccess.Account().AccountProperty(UserSettProperty.BackupPrivateKeys.ToString()));
            }
        }

        protected async Task OnBtnUpdateUserPropertyAsync(KeyValuePair<string,string> prop)
        {
            string value;

            switch ( prop.Key ) // Note! Ones those not used by DemoMode & DemoEdit are not even shown for user
            {
                case "NoLocalStorage":  // UserSettProperty.NoLocalStorage
                case "BackupPrivateKeys":  // UserSettProperty.BackupPrivateKeys

                    if (prop.Value == "FALSE")
                        value = "TRUE";
                    else
                        value = "FALSE";

                    // Update server side
                    if (await PfsClientAccess.Account().AccountPropertySetAsync(prop.Key, value) == false)
                    {
                        await Dialog.ShowMessageBox("Failed!", "Could not contact server atm", yesText: "Ok");
                        return;
                    }
                    else
                    {
                        // And memory information
                        _userProperties[prop.Key] = value;
                    }
                    break;
            }

            if ( prop.Key == "NoLocalStorage")
                await Dialog.ShowMessageBox("Only effects next login!", "Make sure you have backup done PFS and hit cntrl-F5 to relogin", yesText: "Ok");

            return;
        }

        #endregion
    }
}
