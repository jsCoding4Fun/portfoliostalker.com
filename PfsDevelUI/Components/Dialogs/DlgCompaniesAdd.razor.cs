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
using PfsDevelUI.Shared;
using PfsDevelUI.PFSLib;

using Serilog;

using MudBlazor;

using PFS.Shared.Types;

namespace PfsDevelUI.Components
{
    public partial class DlgCompaniesAdd
    {
        [Inject] PfsClientAccess PfsClientAccess { get; set; }
        [Inject] IDialogService Dialog { get; set; }

        [CascadingParameter] MudDialogInstance MudDialog { get; set; }

        [Parameter] public EtCompany[] Companies { get; set; } = null;
        [Parameter] public string PfName { get; set; } = string.Empty;

        protected bool _fullscreen { get; set; } = false;

        protected List<string> _stockGroups = new();
        protected List<ViewCompanies> _viewCompanies = new();
        protected List<MarketID> _configuredMarketIDs = null;

        protected string _addToSgName = string.Empty;

        protected bool _isBusy = false;

        protected override void OnInitialized()
        {
            foreach (EtCompany company in Companies)
            {
                if (company.STID != Guid.Empty)
                    continue;

                ViewCompanies entry =new()
                {
                    Company = company,
                    BrokerInfo = company.Ticker,
                    ISIN = company.ISIN,
                    StockMeta = new(),
                    State = State.Unknown,
                };

                if (string.IsNullOrWhiteSpace(entry.BrokerInfo) == true)
                    entry.BrokerInfo = company.Name;

                _viewCompanies.Add(entry);
            }

            _viewCompanies = _viewCompanies.OrderBy(c => c.BrokerInfo).ToList();

            _stockGroups = PfsClientAccess.StalkerMgmt().StockGroupNameList(PfName);
            _configuredMarketIDs = PfsClientAccess.Fetch().GetMarketMeta(true /*configuredOnly*/).ConvertAll(m => m.ID).ToList();

            // Spinning fetch on background
            _ = FindCompanies();
        }

        protected async Task FindCompanies()
        {
            _isBusy = true;

            foreach ( ViewCompanies company in _viewCompanies )
            {
                if (string.IsNullOrWhiteSpace(company.Company.Ticker) == true)
                    // Cant do automatic search for this one
                    continue;

                (MarketID marketID, CompanyMeta meta) = await PfsClientAccess.Fetch().FindTickerAsync(string.Empty, company.Company.Ticker);

                if (marketID == MarketID.Unknown)
                    // No luck, up to user to figure this one out
                    continue;

                company.State = State.Automatic;
                company.StockMeta.MarketID = marketID;
                company.StockMeta.Ticker = meta.Ticker;
                company.StockMeta.Name = meta.CompanyName;

                StateHasChanged();
            }
            _isBusy = false;
            StateHasChanged();
        }

        protected void OnBtnAutomOffAsync(ViewCompanies company)
        {
            company.State = State.Unknown;
            company.StockMeta = new();
        }
        
        protected void OnBtnManualCleanAsync(ViewCompanies company)
        {
            company.State = State.Unknown;
            company.StockMeta = new();
        }

        protected async Task OnBtnSearchAsync(ViewCompanies company)
        {
            if (company.StockMeta.MarketID == MarketID.Unknown || string.IsNullOrWhiteSpace(company.StockMeta.Ticker) == true)
                return;

            (MarketID marketID, CompanyMeta found) = await PfsClientAccess.Fetch().FindTickerAsync(company.StockMeta.MarketID.ToString(), company.StockMeta.Ticker.ToUpper());

            if (marketID == MarketID.Unknown)
            {
                await Dialog.ShowMessageBox("Search failed!", "Could not find that exact ticker under selected market", yesText: "Ok");
                return;
            }

            company.State = State.UserManual;
            company.StockMeta.MarketID = marketID;
            company.StockMeta.Ticker = found.Ticker;
            company.StockMeta.Name = found.CompanyName;

            StateHasChanged();
        }

        protected void OnFullScreenChanged(bool fullscreen)     // !!!TODO!!! Those dang header icons overlap atm, one from mud one of my.. push my left..
        {
            _fullscreen = fullscreen;

            MudDialog.Options.FullWidth = _fullscreen;
            MudDialog.SetOptions(MudDialog.Options);
        }

        private void DlgCancel()
        {
            MudDialog.Cancel();
        }

        private async Task DlgAddTrackingAsync()
        {
            _isBusy = true;
            StateHasChanged();

            // First part is to add automatic/manually selected companies those not on user stalker yet to there w 'AddStockTrackingAsync'

            foreach (ViewCompanies company in _viewCompanies)
            {
                if (company.State != State.Automatic && company.State != State.UserManual)
                    continue;

                StockMeta stockMeta = PfsClientAccess.StalkerMgmt().GetStockMeta(company.StockMeta.MarketID, company.StockMeta.Ticker);

                if (stockMeta != null)
                {
                    // This is normal case, example if automatics fail and user manually assigns specific record-company to be something already
                    // existing on user's stalker.. or maybe has two records same company w bit different formats and one was just added pre-this
                    // Anyway link STID to company, and be done...
                    company.Company.STID = stockMeta.STID;
                    company.StockMeta.STID = stockMeta.STID;
                    continue;
                }

                // Getting here means that we have automatically or manually w search, defined Market/Ticker information from PFS but 
                // user doesnt track this stock atm.. so lets start tracking it...

                Guid STID = await PfsClientAccess.StalkerMgmt().AddStockTrackingAsync(company.StockMeta.MarketID, company.StockMeta.Ticker, company.StockMeta.Name);

                if ( STID == Guid.Empty )
                {
                    Log.Warning("DlgAddTrackingAsync: Failed to add {0}${1} [{2}]", company.StockMeta.MarketID.ToString(), company.StockMeta.Ticker, company.StockMeta.Name);
                    continue;
                }    

                company.Company.STID = STID;
                company.StockMeta.STID = STID;

                if ( string.IsNullOrWhiteSpace(_addToSgName) == false )
                { 
                    string action = string.Format("Follow-Group SgName=[{0}] Stock=[{1}]", _addToSgName, STID);

                    PfsClientAccess.StalkerMgmt().DoAction(action);
                }
            }

            _isBusy = false;

            // Second part is to return information for caller from progress that was done w companies ""List<EtCompany>""
            MudDialog.Close(DialogResult.Ok(_viewCompanies.Where(c => c.Company.STID != Guid.Empty).Select(c => c.Company).ToList()));
        }

        protected class ViewCompanies
        {
            public EtCompany Company { get; set; }
            public string BrokerInfo { get; set; }            // Visible for user to identify what Broker/Bank was giving for stock as identification
            public string ISIN { get; set; }
            public State State { get; set; }
            public StockMeta StockMeta { get; set; }
        }

        public enum State : int
        {
            Unknown = 0,
            Automatic,
            UserManual,
        }
    }
}
