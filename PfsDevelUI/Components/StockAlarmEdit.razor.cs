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
using System.Collections.ObjectModel;
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

namespace PfsDevelUI.Components
{
    // Viewing/Editing component for Stock Alarm's to be used from multiple places
    public partial class StockAlarmEdit
    {
        [Inject] IDialogService Dialog { get; set; }
        [Inject] PfsClientAccess PfsClientAccess { get; set; }

        [Parameter] public Guid STID { get; set; }

        [Parameter] public bool AllowEditing { get; set; } = true;

        protected ReadOnlyCollection<StockAlarm> _viewAlarms = null;
        protected StockAlarm _selectedAlarm = new();
        protected StockMeta _stockMeta = null;
        protected MarketMeta _marketMeta = null;

        protected override void OnParametersSet()
        {
            if (STID == Guid.Empty)
                return;

            _stockMeta = PfsClientAccess.StalkerMgmt().GetStockMeta(STID);
            _marketMeta = PfsClientAccess.Fetch().GetMarketMeta().Single(m => m.ID == _stockMeta.MarketID);

            UpdateAlarms();
        }

        protected void UpdateAlarms()
        {
            // Copy of alarm list for local use
            _viewAlarms = PfsClientAccess.StalkerMgmt().StockAlarmList(STID);

            if (_viewAlarms != null && _viewAlarms.Count() > 0)
                // Proper sorting...
                _viewAlarms = _viewAlarms.OrderByDescending(a => a.Type).ThenByDescending(a => a.Value).ToList().AsReadOnly();
        }

        private async Task OnRowClickedAsync(TableRowClickEventArgs<StockAlarm> data)
        {
            if ( AllowEditing == false)
                return;

            await LaunchDlgAlarmEdit(STID, data.Item.Value);
        }

        public async Task AddNewAlarmAsync()
        {
            if (AllowEditing == false)
                return;

            await LaunchDlgAlarmEdit(STID, null);
        }

        private async Task OnEditAlarmAsync(decimal value)
        {
            if (AllowEditing == false)
                return;

            await LaunchDlgAlarmEdit(STID, value);

            StateHasChanged();
        }

        protected async Task LaunchDlgAlarmEdit(Guid STID, decimal? value)
        {
            var parameters = new DialogParameters();
            parameters.Add("STID", STID);

            if (value.HasValue)
                // If edit then has SAID, and we pass current content to Dlg
                parameters.Add("Alarm", _viewAlarms.First(x => x.Value == value.Value));

            // !!!NOTE!!! MainLayout has default's for options
            var dialog = Dialog.Show<DlgAlarmEdit>("", parameters);
            var result = await dialog.Result;

            if (!result.Cancelled)
            {
                UpdateAlarms();
                StateHasChanged();
            }
        }
    }
}
