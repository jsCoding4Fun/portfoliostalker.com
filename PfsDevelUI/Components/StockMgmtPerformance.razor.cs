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
    public partial class StockMgmtPerformance
    {
        [Inject] PfsClientAccess PfsClientAccess { get; set; }

        [Parameter] public Guid STID { get; set; }

        protected List<ViewDailyPerformance> _viewDaily = new();
        protected List<ViewWeeklyPerformance> _viewWeekly = new();

        protected async override Task OnInitializedAsync()
        {
            PrivSrvStockPerformanceData perf = await PfsClientAccess.PrivSrvMgmt().GetPerformanceDataAsync(STID);

            if (perf == null)
                return;

            RefreshDailyReport(perf);

            RefreshWeeklyReport(perf);
        }

        protected void RefreshDailyReport(PrivSrvStockPerformanceData perf)
        {
            ViewDailyPerformance newerDailyEntry = null;

            // Tables go from latest 'Date' to oldest information available
            for (int pos = 0; pos < perf.Eod.Length; pos++ )
            {
                ViewDailyPerformance entry = new()
                {
                    DateStr = perf.Eod[pos].Date.DayOfWeek == DayOfWeek.Friday ? perf.Eod[pos].Date.ToString("MMM-dd") : perf.Eod[pos].Date.DayOfWeek.ToString(),
                    EodClose = perf.Eod[pos].Close,
                    EodUp = true,
                    RSI14D = null,
                    RSI14DUp = true,
                    RSI14Dlvl = null,
                    RSI14DlvlUp = true,
                    MFI14D = null,
                    MFI14DUp = true,
                    MFI14Dlvl = null,
                    MFI14DlvlUp = true,
                };

                if (newerDailyEntry != null && newerDailyEntry.EodClose < entry.EodClose)
                    newerDailyEntry.EodUp = false;

                if ( perf.RSI14D != null && perf.RSI14D.Length > pos )
                {
                    entry.RSI14D = perf.RSI14D[pos];

                    if (newerDailyEntry != null && newerDailyEntry.RSI14D.Value < entry.RSI14D.Value)
                        newerDailyEntry.RSI14DUp = false;
                }

                if (perf.RSI14Dlvl != null && perf.RSI14Dlvl.Length > pos)
                {
                    entry.RSI14Dlvl = perf.RSI14Dlvl[pos];

                    if (newerDailyEntry != null && newerDailyEntry.RSI14Dlvl.Value < entry.RSI14Dlvl.Value)
                        newerDailyEntry.RSI14DlvlUp = false;
                }

                if (perf.MFI14D != null && perf.MFI14D.Length > pos)
                {
                    entry.MFI14D = perf.MFI14D[pos];

                    if (newerDailyEntry != null && newerDailyEntry.MFI14D.Value < entry.MFI14D.Value)
                        newerDailyEntry.MFI14DUp = false;
                }

                if (perf.MFI14Dlvl != null && perf.MFI14Dlvl.Length > pos)
                {
                    entry.MFI14Dlvl = perf.MFI14Dlvl[pos];

                    if (newerDailyEntry != null && newerDailyEntry.MFI14Dlvl.Value < entry.MFI14Dlvl.Value)
                        newerDailyEntry.MFI14DlvlUp = false;
                }

                _viewDaily.Add(entry);
                newerDailyEntry = entry;
            }
        }

        public class ViewDailyPerformance
        {
            public string DateStr { get; set; }

            public decimal EodClose { get; set; }
            public bool EodUp { get; set; }

            public decimal? RSI14D { get; set; }
            public bool RSI14DUp { get; set; }

            public decimal? RSI14Dlvl { get; set; }
            public bool RSI14DlvlUp { get; set; }

            public decimal? MFI14D { get; set; }
            public bool MFI14DUp { get; set; }

            public decimal? MFI14Dlvl { get; set; }
            public bool MFI14DlvlUp { get; set; }
        }

        protected void RefreshWeeklyReport(PrivSrvStockPerformanceData perf)
        {
            ViewWeeklyPerformance newerWeeklyEntry = null;

            // Tables go from latest 'Date' to oldest information available
            for (int pos = 0; pos < perf.Eow.Length; pos++)
            {
                ViewWeeklyPerformance entry = new()
                {
                    Date = perf.Eow[pos].Date,
                    EowClose = perf.Eow[pos].Close,
                    EowUp = true,
                    RSI14W = null,
                    RSI14WUp = true,
                    RSI14Wlvl = null,
                    RSI14WlvlUp = true,
                    MFI14W = null,
                    MFI14WUp = true,
                    MFI14Wlvl = null,
                    MFI14WlvlUp = true,
                };

                if (newerWeeklyEntry != null && newerWeeklyEntry.EowClose < entry.EowClose)
                    newerWeeklyEntry.EowUp = false;

                if (perf.RSI14W != null && perf.RSI14W.Length > pos)
                {
                    entry.RSI14W = perf.RSI14W[pos];

                    if (newerWeeklyEntry != null && newerWeeklyEntry.RSI14W.Value < entry.RSI14W.Value)
                        newerWeeklyEntry.RSI14WUp = false;
                }

                if (perf.RSI14Wlvl != null && perf.RSI14Wlvl.Length > pos)
                {
                    entry.RSI14Wlvl = perf.RSI14Wlvl[pos];

                    if (newerWeeklyEntry != null && newerWeeklyEntry.RSI14Wlvl.Value < entry.RSI14Wlvl.Value)
                        newerWeeklyEntry.RSI14WlvlUp = false;
                }

                if (perf.MFI14W != null && perf.MFI14W.Length > pos)
                {
                    entry.MFI14W = perf.MFI14W[pos];

                    if (newerWeeklyEntry != null && newerWeeklyEntry.MFI14W.Value < entry.MFI14W.Value)
                        newerWeeklyEntry.MFI14WUp = false;
                }

                if (perf.MFI14Wlvl != null && perf.MFI14Wlvl.Length > pos)
                {
                    entry.MFI14Wlvl = perf.MFI14Wlvl[pos];

                    if (newerWeeklyEntry != null && newerWeeklyEntry.MFI14Wlvl.Value < entry.MFI14Wlvl.Value)
                        newerWeeklyEntry.MFI14WlvlUp = false;
                }

                _viewWeekly.Add(entry);
                newerWeeklyEntry = entry;
            }
        }

        public class ViewWeeklyPerformance
        {
            public DateTime Date { get; set; }

            public decimal EowClose { get; set; }
            public bool EowUp { get; set; }

            public decimal? RSI14W { get; set; }
            public bool RSI14WUp { get; set; }

            public decimal? RSI14Wlvl { get; set; }
            public bool RSI14WlvlUp { get; set; }

            public decimal? MFI14W { get; set; }
            public bool MFI14WUp { get; set; }

            public decimal? MFI14Wlvl { get; set; }
            public bool MFI14WlvlUp { get; set; }
        }
    }
}
