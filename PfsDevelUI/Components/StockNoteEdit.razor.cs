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
using MudBlazor;

using PFS.Shared.Types;
using PFS.Shared.UiTypes;

namespace PfsDevelUI.Components
{
    public partial class StockNoteEdit
    {
        [Inject] PfsClientAccess PfsClientAccess { get; set; }

        [Parameter] public Guid STID { get; set; }

        protected WidgTagGroupEdit _widgTagGroupEdit;

        protected string _headerInfo = "empty";

        protected string _editingOverview = new(string.Empty);
        protected string _editingBodyText = new(string.Empty);
        protected string _buttonTextEditSave = "Edit";

        protected bool _viewMode = true;    // true = view, false = editing

        protected bool _allowLocalStorage = true;

        protected override void OnParametersSet()
        {
            if (PfsClientAccess.Account().AccountProperty(UserSettProperty.NoLocalStorage.ToString()) == "TRUE")
                _allowLocalStorage = false;

            StockNote current = PfsClientAccess.NoteMgmt().NoteGet(STID);

            StockMeta meta = PfsClientAccess.StalkerMgmt().GetStockMeta(STID);

            if (meta != null)
                _headerInfo = string.Format("$({0}) on {1}: {2}", meta.Ticker, meta.MarketID, meta.Name);

            if (current != null)
            {
                _editingOverview = new(current.Overview);
                _editingBodyText = new(current.BodyText);
            }

            if ( _allowLocalStorage == false )
                _editingBodyText = "--not supported--";
        }

        private void OnButtonEditSave()
        {
            if (_viewMode == true )
            {
                // From Viewing -> Editing 

                _viewMode = false;
                _buttonTextEditSave = "Save";

                _widgTagGroupEdit.OnSetEditMode();
            }
            else
            {
                // Do Save -> Viewing

                _viewMode = true;
                _buttonTextEditSave = "Edit";

                // First lets do save groups information
                _widgTagGroupEdit.OnSave();

                // Then need to reread from storage, as changed by group save
                StockNote current = PfsClientAccess.NoteMgmt().NoteGet(STID);

                if (current == null)
                    current = new();

                current.Overview = _editingOverview;

                if (_allowLocalStorage)
                    current.BodyText = _editingBodyText;

                if (current.IsEmpty() == false )
                {
                    PfsClientAccess.NoteMgmt().NoteSave(STID, current);

                    // Saving note always updates LastStockEdit date also for today...
                    string cmd = string.Format("Set-Stock Stock=[{0}] +LastEdit=[{1}]", STID, DateTime.Now.ToString("yyyy-MM-dd"));
                    PfsClientAccess.StalkerMgmt().DoAction(cmd);
                }
                else
                    PfsClientAccess.NoteMgmt().NoteSave(STID, null);
            }
        }
    }
}
