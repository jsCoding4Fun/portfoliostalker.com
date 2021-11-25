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

using PFS.Shared.Types;

namespace PfsDevelUI.Components
{
    public partial class DlgAlarmEdit
    {
        [CascadingParameter] MudDialogInstance MudDialog { get; set; }
        [Inject] private IDialogService Dialog { get; set; }
        [Inject] PfsClientAccess PfsClientAccess { get; set; }

        [Parameter] public Guid STID { get; set; } // These fill automatically per caller pages 'DialogParameters' 
        [Parameter] public StockAlarm Alarm { get; set; }

        protected string _title;

        protected DlgAlarmEditFormData _edit = null;

        protected bool _registerMode = false;

        protected string _param1Label = "";

        public static readonly Tuple<StockAlarmType, string>[] _param1 =
        {
            Tuple.Create(StockAlarmType.Unknown,        ""),
            Tuple.Create(StockAlarmType.Under,          ""),
            Tuple.Create(StockAlarmType.Over,           ""),
            Tuple.Create(StockAlarmType.UnderWatch,     "Watch Higher Level"),
            Tuple.Create(StockAlarmType.UnderWatchP,    "Procent 1-50"),
            Tuple.Create(StockAlarmType.OverWatch,      "Watch Lower Level"),
            Tuple.Create(StockAlarmType.OverWatchP,     "Procent 1-50"),
        };

        private StockAlarmType _editType;
        public StockAlarmType EditType              
        { 
            get => _editType; 
            set
            {
                if(value == _editType)
                    return;
                _editType = value;

                TypeSelectionChanged();             // <<!!!IMPORTANT!!! Here is trick to have dual bind, and still get change events!!!
            }
        }

        protected override void OnInitialized()
        {
            if (Alarm != null)
            {
                _title = "Edit Alarm";
                _edit = new()
                {
                    Value = Alarm.Value,
                    Note = Alarm.Note,
                    Param1 = Alarm.Param1,
                };
                _editType = Alarm.Type;
            }
            else
            {
                _title = "Add Alarm";
                _edit = new();
                _editType = StockAlarmType.Unknown;
            }

            TypeSelectionChanged();
        }

        private void DlgCancel()
        {
            MudDialog.Cancel();
        }

        public void TypeSelectionChanged()
        {
            if (_editType == StockAlarmType.Unknown )
            {
                _param1Label = "";
                return;
            }

            // Set label for 'Param1' as its viewed on UI, and controls if field is hidden per type
            _param1Label = _param1.First(t => t.Item1 == _editType).Item2;
        }

        protected async Task DlgSaveAsync()
        {
            if (_editType == StockAlarmType.Unknown)        // !!!TODO!!! Needs lot of manual validation of required fields etc
            {
                bool? result = await Dialog.ShowMessageBox("Cant do!", "Invalid field values!", yesText: "Ok");
                return;
            }

            string cmd;

            if (_editType == StockAlarmType.Under || _editType == StockAlarmType.Over )
            {
                // Field is actually hidden from user, we wanna force it minimum allowed
                _edit.Param1 = 0.01M;
            }

            if (Alarm != null)
            {
                cmd = string.Format("Edit-Alarm Type=[{0}] Stock=[{1}] EditedValue=[{2}]  Value=[{3}] Param1=[{4}] Note=[{5}]",
                                           _editType, STID, Alarm.Value, _edit.Value, _edit.Param1, _edit.Note);
            }
            else
            {
                cmd = string.Format("Add-Alarm Type=[{0}] Stock=[{1}] Value=[{2}] Param1=[{3}] Note=[{4}]",
                                           _editType, STID, _edit.Value, _edit.Param1, _edit.Note);
            }

            if (PfsClientAccess.StalkerMgmt().DoAction(cmd) == StalkerError.OK)
            {
                // Add/Edit alarm always updates stock's LastStockEdit date for today..
                cmd = string.Format("Set-Stock Stock=[{0}] +LastEdit=[{1}]", STID, DateTime.Now.ToString("yyyy-MM-dd"));
                PfsClientAccess.StalkerMgmt().DoAction(cmd);

                MudDialog.Close();
            }
            else
            {
                // !!!LATER!!! Add error
            }
        }

        protected void DlgDelete()
        {
            if (Alarm != null)
            {
                string cmd = string.Format("Delete-Alarm Stock=[{0}] Value=[{1}]", STID, Alarm.Value);

                if (PfsClientAccess.StalkerMgmt().DoAction(cmd) == StalkerError.OK)
                    MudDialog.Close();
                else
                {
                    // !!!LATER!!! Add error
                }
            }
        }
    }

    // Verification of username+password is done on client only, really doesnt matter if rules followed or not as server never gets this info
    public class DlgAlarmEditFormData            // !!!TODO!!! Enforce rules on razor files w https://mudblazor.com/components/form
    {
        [Required]
        public decimal Value { get; set; }

        public decimal Param1 { get; set; }

        [StringLength(50)]
        public string Note { get; set; }
    }
}
