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
    public partial class StockMgmtDividents
    {
        [Inject] PfsClientAccess PfsClientAccess { get; set; }
        [Inject] private IDialogService Dialog { get; set; }

        [Parameter] public Guid STID { get; set; }

        protected List<ViewStockMgmtDividents> _dividents = new();

        protected override void OnInitialized()
        {
            RefreshReport();
        }

        protected void RefreshReport()
        {
            _dividents = new();
            List<string> portfolios = PfsClientAccess.StalkerMgmt().PortfolioNameList();

            List<MarketMeta> allMarketMetas = PfsClientAccess.Fetch().GetMarketMeta();

            StockMeta stockMeta = PfsClientAccess.StalkerMgmt().GetStockMeta(STID);

            foreach (string pfName in portfolios)
            {
                ReadOnlyCollection<StockDivident> dividents = PfsClientAccess.StalkerMgmt().StockDividentList(pfName, STID);

                foreach (StockDivident divident in dividents)
                {
                    _dividents.Add(new ViewStockMgmtDividents()
                    {
                        PfName = pfName,
                        d = divident,
                        Currency = allMarketMetas.Single(m => m.ID == stockMeta.MarketID).Currency,
                    });
                }
            }
        }

        private async Task OnDeleteDividentAsync(ViewStockMgmtDividents viewDivident)
        {
            bool? result = await Dialog.ShowMessageBox("Are you sure sure?", "Specific divident will be removed from holding?", yesText: "Ok", cancelText: "Cancel");

            if (result.HasValue == false || result.Value == false)
                return;

            string cmd = string.Format("Delete-Divident DividentID=[{0}]", viewDivident.d.DividentID);

            StalkerError error = PfsClientAccess.StalkerMgmt().DoAction(cmd);

            if (error == StalkerError.OK)
                RefreshReport();
            else
                await Dialog.ShowMessageBox("Failed!", "Hmm.. something go boom, strange.. plz report.", yesText: "Ok");
        }

        public class ViewStockMgmtDividents
        {
            public string PfName { get; set; }

            public StockDivident d { get; set; }

            public CurrencyCode Currency { get; set; }
        }
    }
}
