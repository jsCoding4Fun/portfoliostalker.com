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
using PfsDevelUI.PFSLib;
using MudBlazor;

using PFS.Shared.Types;
using PFS.Shared.UiTypes;

namespace PfsDevelUI.Components
{
    // Settings for Priv Srv, allowing to specify functionality and launch logs viewer etc
    public partial class CompPrivSrvSettings
    {
        [Inject] PfsClientAccess PfsClientAccess { get; set; }
        [Inject] private IDialogService Dialog { get; set; }

        protected Dictionary<PrivSrvProperty, string> _latest = new();
        protected List<ViewProperties> _view = null;

        protected PrivSrvProperty _editID = PrivSrvProperty.Unknown;
        protected string _editProperty = string.Empty;
        protected string _editValue = string.Empty;
        protected string _editLabel = string.Empty;

        protected override async Task OnParametersSetAsync()
        {
            await Reload();
        }

        protected async Task Reload()
        {
            _editID = PrivSrvProperty.Unknown;

            _latest = await PfsClientAccess.PrivSrvMgmt().SrvConfigPropertyGetAllAsync();

            if (_latest == null)
            {
                _view = null;
                return;
            }

            _view = new();

            // Manually add all command properties, those do not get stored nor returned w 'SrvConfigPropertyGetAllAsync'

            foreach (PrivSrvProperty prop in Enum.GetValues(typeof(PrivSrvProperty)))
            {
                if (prop.ToString().StartsWith("Cmd") == false)
                    continue;

                ViewProperties entry = new()
                {
                    PropertyID = prop,
                    Value = string.Empty,
                    Cmd = true,
                };

                switch (prop)
                {
                    case PrivSrvProperty.CmdGetStatus:
                    case PrivSrvProperty.CmdGetProviderLog: entry.Value = "Click to View"; break;

                    case PrivSrvProperty.CmdForceUpdateSoloJobs:
                    case PrivSrvProperty.CmdForceUpdate: entry.Value = "Click to Update"; break;
                }

                _view.Add(entry);
            }

            // Actual properties from Private Server w current values
            foreach (KeyValuePair<PrivSrvProperty, string> prop in _latest)
            {
                if (prop.Key == PrivSrvProperty.AuthenticatedRO && prop.Value != "TRUE")
                    // Only visible IF authenticated
                    continue;

                ViewProperties entry = new()
                {
                    PropertyID = prop.Key,
                    Value = prop.Value,
                    Cmd = false,
                };

                _view.Add(entry);
            }

            StateHasChanged();
        }

        private async Task OnRowClickedAsync(TableRowClickEventArgs<ViewProperties> data)
        {
            string showContentString = string.Empty;

            switch (data.Item.PropertyID)
            {
                case PrivSrvProperty.AuthenticatedRO:
                    // Read-Only
                    break;

                // These are simple, click to do, and show returned information on big dialog
                case PrivSrvProperty.CmdGetStatus:
                case PrivSrvProperty.CmdForceUpdateSoloJobs:
                case PrivSrvProperty.CmdForceUpdate:
                case PrivSrvProperty.CmdGetProviderLog:
                    {
                        showContentString = await PfsClientAccess.PrivSrvMgmt().SrvConfigPropertySetAsync(data.Item.PropertyID, string.Empty);
                        _editID = PrivSrvProperty.Unknown;
                    }
                    break;

                // These are actual properties, but w TRUE/FALSE value.. so still click to change
                case PrivSrvProperty.StartupCreateJobs:
                case PrivSrvProperty.AllowUrlCommands:
                    {
                        if (_latest.ContainsKey(data.Item.PropertyID) == false || _latest[data.Item.PropertyID] != "TRUE")
                            await PfsClientAccess.PrivSrvMgmt().SrvConfigPropertySetAsync(data.Item.PropertyID, "TRUE");
                        else
                            await PfsClientAccess.PrivSrvMgmt().SrvConfigPropertySetAsync(data.Item.PropertyID, "FALSE");

                        _editID = PrivSrvProperty.Unknown;
                    }
                    break;

                case PrivSrvProperty.NewStockFromYYMMDD:
                    {
                        _editID = PrivSrvProperty.NewStockFromYYMMDD;
                        _editProperty = _editID.ToString();
                        _editValue = data.Item.Value;
                        _editLabel = "YYMMDD";
                        StateHasChanged();
                    }
                    return;

                case PrivSrvProperty.NewStockProvider:
                    {
                        _editID = PrivSrvProperty.NewStockProvider;
                        _editProperty = _editID.ToString();
                        _editValue = data.Item.Value;
                        _editLabel = "Exact provider name!";
                        StateHasChanged();
                    }
                    return;

                case PrivSrvProperty.LimitGoldStockMax:
                case PrivSrvProperty.LimitPlatinumStockMax:
                    {
                        _editID = data.Item.PropertyID;
                        _editProperty = _editID.ToString();
                        _editValue = data.Item.Value;
                        _editLabel = "Limit from 50-1000!";
                        StateHasChanged();
                    }
                    return;
            }

            if (string.IsNullOrWhiteSpace(showContentString) == false)
            {
                DialogOptions maxWidth = new DialogOptions() { MaxWidth = MaxWidth.Large, FullWidth = true };

                var parameters = new DialogParameters();
                parameters.Add("Content", showContentString);
                Dialog.Show<DlgViewContentString>("", parameters, maxWidth);
            }

            await Reload();
            StateHasChanged();
        }

        protected async Task DlgSaveAsync()
        {
            switch (_editID)
            {
                case PrivSrvProperty.NewStockFromYYMMDD:
                    {
                        DateTime date;

                        if (DateTime.TryParseExact(_editValue, "yyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date) == true &&
                            date <= DateTime.Now.Date)
                        {
                            await PfsClientAccess.PrivSrvMgmt().SrvConfigPropertySetAsync(_editID, _editValue);

                            await Reload();
                            StateHasChanged();
                            return;
                        }
                            
                        await Dialog.ShowMessageBox("Failed!", "Invalid format? Use YYMMDD as a first day to fetch", yesText: "Ok");
                    }
                    break;

                case PrivSrvProperty.NewStockProvider:
                    {
                        ExtDataProviders providerID;
                        
                        if ( Enum.TryParse(_editValue, out providerID) == true && providerID != ExtDataProviders.Unknown )
                        {
                            await PfsClientAccess.PrivSrvMgmt().SrvConfigPropertySetAsync(_editID, _editValue);

                            await Reload();
                            StateHasChanged();
                            return;
                        }
                        else
                            await Dialog.ShowMessageBox("Failed!", "Invalid format? Need to match exactly for providers name on settings", yesText: "Ok");
                    }
                    break;

                case PrivSrvProperty.LimitGoldStockMax:
                case PrivSrvProperty.LimitPlatinumStockMax:
                    {
                        int value;

                        if ( int.TryParse(_editValue, out value) == true && value >= 50 && value <= 1000 )
                        {
                            await PfsClientAccess.PrivSrvMgmt().SrvConfigPropertySetAsync(_editID, _editValue);

                            await Reload();
                            StateHasChanged();
                            return;
                        }
                        else
                            await Dialog.ShowMessageBox("Failed!", "Invalid format? Limited to 50-1000", yesText: "Ok");
                    }
                    break;
            }
        }

        public class ViewProperties
        {
            public PrivSrvProperty PropertyID { get; set; }
            public string Value { get; set; }
            public bool Cmd { get; set; }
        }
    }
}
