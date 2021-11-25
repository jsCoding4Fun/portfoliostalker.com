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

namespace PfsDevelUI.Components
{
    public partial class StockMgmtCharts // As of 2021-Sep DISABLED, as just too simpleton chart to be usefull...
    {
        [Inject] PfsClientAccess PfsClientAccess { get; set; }

        [Parameter] public Guid STID { get; set; }

        private readonly ChartOptions _chartOptions = new ChartOptions()
        {
            //YAxisTicks = 10,            // Step on Y axis... this needs to be like rounded 10% from max-min
            MaxNumYAxisTicks = 6,      // related to YAxisTicks to give absolute max of visible steps on Y (or may show less)

            YAxisLines = true,            // if showing support lines for x/y 
            XAxisLines = true,

            DisableLegend = true,       // Remove description of lines from under chart

            //InterpolationOption = InterpolationOption.


        };

#if false
        //
        // Summary:
        //     Spacing of Y-axis ticks.
        public int YAxisTicks { get; set; }
        //
        // Summary:
        //     Maximum number of Y-axis ticks. The ticks will be thinned out if the value range
        //     is leading to too many ticks.
        public string YAxisFormat { get; set; }


        => As of 2021-Sep.. not worth of using.. doesnt allow define range for Y so any bigger number w little movement shows still
           zero line on bottom so its just causes things to flat line

           also doesnt seam to understand that XAxisLabels would be more rarely than actual data

           way way way too simple atm for any stock value viewing... could use for RSI etc maybe... but why waste time.. 

#endif

        protected async override Task OnInitializedAsync()
        {
            List<StockClosingData> EODs = await PfsClientAccess.PrivSrvMgmt().GetHistoryEODsAsync(STID, new DateTime(2021, 1, 1));
        }

        private int Index = -1; //default value cannot be 0 -> first selectedindex is 0.

            public List<ChartSeries> Series = new List<ChartSeries>()
        {
            new ChartSeries() { Name = "Series 1", Data = new double[] { 190, 179, 172, 169, 162, 162, 155, 165, 170 } },
            new ChartSeries() { Name = "Series 2", Data = new double[] { 170, 141, 135, 151, 149, 162, 169, 191, 148 } },
        };
        //public string[] XAxisLabels = { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep" };
        public string[] XAxisLabels = { "Jan", "Mar", "May", "Jul", "Sep" };

    }
}
