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
using PFS.Shared.UiTypes;

namespace PfsDevelUI.Components
{
    // Mgmt of available &GROUPS, and place to see/assign groups for stocks 
    public partial class CompTagGroupsEdit
    {
        [Inject] PfsClientAccess PfsClientAccess { get; set; }
        [Inject] private IDialogService Dialog { get; set; }

        protected List<ViewRow> _viewRow = new();

        protected List<TagGroupsUsage> _usageData = null;

        bool[] _groupRO = new bool[TagGroupsUsage.MaxTagGroups] { true, true, true };

        public class ViewRow
        {
            public string[] Field { get; set; }

            public int[] Usage { get; set; }
        }

        protected override void OnInitialized()
        {
            _usageData = PfsClientAccess.NoteMgmt().GetTagGroupsUsage();

            Reload();
        }

        protected void Reload()
        {
            string[][] grpFields = new string[TagGroupsUsage.MaxTagGroups][];

            for ( int gr = 0; gr < TagGroupsUsage.MaxTagGroups; gr++ )
                grpFields[gr] = PfsClientAccess.NoteMgmt().GetTagGroup(gr);

            _viewRow = new();
            for (int val = 0; val < TagGroupsUsage.MaxTagGroupValues; val++)
            {
                ViewRow entry = new ViewRow()
                {
                    Field = new string[TagGroupsUsage.MaxTagGroups],
                    Usage = new int[TagGroupsUsage.MaxTagGroups],
                };

                for (int gr = 0; gr < TagGroupsUsage.MaxTagGroups; gr++)
                {
                    entry.Field[gr] = grpFields[gr] != null ? grpFields[gr][val] : string.Empty;

                    if (string.IsNullOrWhiteSpace(entry.Field[gr]) == false)
                    {
                        if ( val == 0 )
                            // Group name, ala header, shows unassigned number
                            entry.Usage[gr] = _usageData.Where(s => string.IsNullOrWhiteSpace(s.Groups[gr]) == true).Count();
                        else
                            // Actual fields
                            entry.Usage[gr] = _usageData.Where(s => s.Groups[gr] == entry.Field[gr]).Count();
                    }
                    else
                        entry.Usage[gr] = 0;
                }

                _viewRow.Add(entry);
            }
        }

        protected async Task OnBtnGrp0Async() { await OnHandleGrpBtnAsync(0); }
        protected async Task OnBtnGrp1Async() { await OnHandleGrpBtnAsync(1); }
        protected async Task OnBtnGrp2Async() { await OnHandleGrpBtnAsync(2); }

        protected async Task OnHandleGrpBtnAsync(int gr)
        {
            if (_groupRO[gr] == false) // Do SAVE
            {
                if ( string.IsNullOrWhiteSpace(_viewRow[0].Field[gr]) == false )
                {
                    string[] content = _viewRow.Select(s => s.Field[gr]).ToArray();
                    
                    if ( PfsClientAccess.NoteMgmt().SaveTagGroup(gr, content) == false )
                    {
                        await Dialog.ShowMessageBox("Failed!", "Fields names are restricted to chars and numbers", yesText: "Ok");
                        return;
                    }
                }
                else
                    // Header to empty is disabling/removing group
                    PfsClientAccess.NoteMgmt().SaveTagGroup(gr, null);

                _groupRO[gr] = true;

                Reload();
                StateHasChanged();
            }
            else // Allow EDIT
                _groupRO[gr] = false;
        }
    }
}
