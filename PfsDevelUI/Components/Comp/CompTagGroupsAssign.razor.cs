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

using PFS.Shared.UiTypes;

namespace PfsDevelUI.Components
{
    public partial class CompTagGroupsAssign
    {
        [Inject] PfsClientAccess PfsClientAccess { get; set; }

        private List<ViewStocks> _viewStocks;

        protected const string Unselected = "-unselected-";

        protected string _headerTextName = string.Empty;

        protected string[] _headerTextGroups = null;

        string[][] _tagGroups = null;

        protected override void OnParametersSet()
        {
            RefreshReport();
        }

        protected void RefreshReport()
        {
            _viewStocks = new();
            List<TagGroupsUsage> usageData = PfsClientAccess.NoteMgmt().GetTagGroupsUsage();

            _tagGroups = new string[TagGroupsUsage.MaxTagGroups][];
            for (int gr = 0; gr < TagGroupsUsage.MaxTagGroups; gr++)
                _tagGroups[gr] = PfsClientAccess.NoteMgmt().GetTagGroup(gr);

            _headerTextGroups = new string[TagGroupsUsage.MaxTagGroups];

            for (int gr = 0; gr < TagGroupsUsage.MaxTagGroups; gr++)
            {
                if (_tagGroups[gr] != null)
                {
                    _headerTextGroups[gr] = new string(_tagGroups[gr][0]);

                    // [0] came as header text, but we replace it to be something more usefull on selection list
                    _tagGroups[gr][0] = Unselected;
                }
                else
                    _headerTextGroups[gr] = null;
            }

            foreach (TagGroupsUsage inData in usageData)
            {
                ViewStocks outData = new()
                {
                    d = inData,
                    ShowDropDown = false,
                };

                for (int gr = 0; gr < TagGroupsUsage.MaxTagGroups; gr++)
                {
                    if ( string.IsNullOrWhiteSpace(outData.d.Groups[gr]) )
                        outData.d.Groups[gr] = Unselected;
                }

                _viewStocks.Add(outData);
            }

            _headerTextName = string.Format("Name (total {0} stocks)", _viewStocks.Count());

            // Finally lets remove all empty spots from given group values list, so selection is more clean...
            for (int gr = 0; gr < TagGroupsUsage.MaxTagGroups; gr++)
                if (_tagGroups[gr] != null)
                    _tagGroups[gr] = _tagGroups[gr].Where(v => string.IsNullOrWhiteSpace(v) == false).ToArray();
        }

        private void OnRowClicked(TableRowClickEventArgs<ViewStocks> data)
        {
            data.Item.ShowDropDown = !data.Item.ShowDropDown;
        }

        protected void OnBtnSaveAll()
        {
            List<TagGroupsUsage> originals = PfsClientAccess.NoteMgmt().GetTagGroupsUsage();

            List<TagGroupsUsage> updated = new();

            foreach (ViewStocks curr in _viewStocks)
            {
                TagGroupsUsage orig = originals.Single(s => s.StockMeta.STID == curr.d.StockMeta.STID);

                bool changed = false;

                for (int gr = 0; gr < TagGroupsUsage.MaxTagGroups; gr++)
                {
                    string compare = curr.d.Groups[gr];

                    if (compare == Unselected)
                        compare = string.Empty;

                    if (orig.Groups[gr] != compare)
                        changed = true;
                }

                if (changed) // Create copy of it, as wanting anyway get rid of '-unselected-' texts
                {
                    TagGroupsUsage upd = new()
                    {
                         StockMeta = curr.d.StockMeta,
                         Groups = new string[TagGroupsUsage.MaxTagGroups],
                    };

                    for (int gr = 0; gr < TagGroupsUsage.MaxTagGroups; gr++)
                    {
                        if (curr.d.Groups[gr] == Unselected)
                            upd.Groups[gr] = String.Empty;
                        else
                            upd.Groups[gr] = curr.d.Groups[gr];
                    }
                    updated.Add(upd);
                }
            }

            if (updated.Count > 0)
            {
                // Got some changes so lets save it..
                PfsClientAccess.NoteMgmt().SaveTagGroupsUsage(updated);

                RefreshReport();
                StateHasChanged();
            }
        }

        protected class ViewStocks
        {
            public TagGroupsUsage d;

            public bool ShowDropDown;
        }
    }
}
