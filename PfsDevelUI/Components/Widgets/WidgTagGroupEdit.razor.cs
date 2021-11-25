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
    // Simple one-liner component by default showing current tag group selection for specified stock, and by call can be changed editing mode
    public partial class WidgTagGroupEdit
    {
        [Inject] PfsClientAccess PfsClientAccess { get; set; }

        [Parameter] public Guid STID { get; set; }  // Requires stock to be given

        protected readonly string UnSelected = "Unselected";
                                                                                    // Note! allFields[gr] is *null* if group not available
        protected string[][] _allFields = null;                                     // Field [0] as Unselected, and all possible field values 
        protected string[] _headers = new string[TagGroupsUsage.MaxTagGroups];      // Keep copy of field[0] thats header for UI viewing
        protected string[] _selectedField = new string[TagGroupsUsage.MaxTagGroups];// Actual current selected field value for group

        protected bool _viewMode = true;

        protected override void OnParametersSet()
        {
            StockNote stockNote = PfsClientAccess.NoteMgmt().NoteGet(STID);

            _allFields = new string[TagGroupsUsage.MaxTagGroups][];

            for (int gr = 0; gr < TagGroupsUsage.MaxTagGroups; gr++)
            {
                _allFields[gr] = PfsClientAccess.NoteMgmt().GetTagGroup(gr);

                if (_allFields[gr] != null)
                {
                    _headers[gr] = _allFields[gr][0];
                    _allFields[gr][0] = UnSelected;
                    _allFields[gr] = _allFields[gr].Where(s => String.IsNullOrEmpty(s) == false).ToArray();
                }

                // Set separate buffer for bind's usage, and make sure has strings there for each group
                if (stockNote != null && stockNote.Groups[gr] != null)
                    _selectedField[gr] = new string(stockNote.Groups[gr]);
                else
                    _selectedField[gr] = new string(string.Empty);
            }
        }

        // Note! Called by owner of component
        public void OnSetEditMode()
        {
            _viewMode = false;
            StateHasChanged();
        }

        // Note! Called by owner. Saves selections to note, and goes back to view mode
        public void OnSave()
        {
            StockNote stockNote = PfsClientAccess.NoteMgmt().NoteGet(STID);

            if (stockNote == null)
                stockNote = new();

            for (int gr = 0; gr < TagGroupsUsage.MaxTagGroups; gr++)
            {
                if (string.IsNullOrWhiteSpace(_selectedField[gr]) || _selectedField[gr] == UnSelected)
                    stockNote.Groups[gr] = string.Empty;
                else
                    stockNote.Groups[gr] = _selectedField[gr];
            }

            if (stockNote.IsEmpty() == false)
                PfsClientAccess.NoteMgmt().NoteSave(STID, stockNote);
            else
                PfsClientAccess.NoteMgmt().NoteSave(STID, null);

            _viewMode = true;
            StateHasChanged();
        }
    }
}
