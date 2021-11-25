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
using System.Collections.ObjectModel;
using Microsoft.AspNetCore.Components;
using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.AspNetCore.Components.WebAssembly.Http;
using Microsoft.JSInterop;
using PfsDevelUI.PFSLib;
using MudBlazor;

using PFS.Shared.Types;
using PFS.Shared.UiTypes;

namespace PfsDevelUI.Components
{
    public partial class CompSettProviders
    {
        [Inject] PfsClientAccess PfsClientAccess { get; set; }
        [Inject] PfsClientPlatform PfsClientPlatform { get; set; }
        [Inject] IDialogService Dialog { get; set; }

        [Parameter] public UseCaseID UseCase { get; set; } = UseCaseID.UNKNOWN;

        protected List<ExtDataProviders> _availableProviders = null;
        protected ExtDataProviders _selectedProvider = ExtDataProviders.Unknown;
        protected SettMarketProviders _configs = null;

        public enum UseCaseID
        {
            UNKNOWN = 0,
            LOCAL_SETT,
            PRIV_SERV_SETT,
        }

        protected static readonly ReadOnlyDictionary<ExtDataProviders, DlgProviderCfg> _description = new ReadOnlyDictionary<ExtDataProviders, DlgProviderCfg>(new Dictionary<ExtDataProviders, DlgProviderCfg>
        {
            [ExtDataProviders.Tiingo] = new DlgProviderCfg()    // 
            {
                Name = "Tiingo",
                Addr = "https://api.tiingo.com/",
                Desc = "Only supports USA markets. Bit slow fetching. 500 unique stock monthly limit. Does NOT work 2021-Nov-->, so skip this now!" + Environment.NewLine
                     + "Also has 500 max request per hour limitation on free account, but havent hit that one myself." + Environment.NewLine
                     + "Web Appl: => Does NOT work! Last time tested 2021-Nov-3th, doesnt work on Local / WASM client, ok on PrivSrv!" + Environment.NewLine
//                     + "Priv Srv: => For US markets all nice option. Been using this myself, and seams trustable!" + Environment.NewLine
                     + "Premium: => 10$ per month, for personal use not a bad price but US only, allows to extend from 500 -> unlimited stock amount" + Environment.NewLine
                     + "Extra: Would have Intraday feature, that would be nice.. but as mentioned doesnt work under WASM, so no intraday." + Environment.NewLine
                     + "Supported tickers: https://apimedia.tiingo.com/docs/tiingo/daily/supported_tickers.zip"
            },
            [ExtDataProviders.Unibit] = new DlgProviderCfg()    // 
            {
                Name = "UniBit",
                Addr = "https://unibit.ai/solution",
                Desc = "Recommended as supports all markets. Data available in 3+ hours after market closing. New stocks arrival is slow on lists." + Environment.NewLine
                     + "50,000 monthly credits of free account should be able to handle 150 stocks. Last test 2021-Oct-30th" + Environment.NewLine
                     + "Web Appl: => Nice as very fast fetching of daily closing data. " + Environment.NewLine
//                     + "Priv Srv: => Good option for Non-US markets, but keep eye stock amounts under it." + Environment.NewLine
                     + "Premium: => 200$/month as cheapest is hefty price for personal / home use. Atm cant see value worth of testing it out." + Environment.NewLine
                     + "Note! Did see often days when End-Of-Day values for bunch of stocks on main markets where missing half day after market closing."
                    // => One day missed half stocks from 3 main markets for all day, next day missed 15 stocks from TSX still 8 hours after closing
                    //    so all ok dang fast for use but has to live w issues is seams to have often to keep up w data
            },
            [ExtDataProviders.Marketstack] = new DlgProviderCfg()           // Updated 2021-Nov-6th
            {
                Name = "Marketstack",
                Addr = "https://marketstack.com/?utm_source=FirstPromoter&utm_medium=Affiliate&fpr=pfs",
                Desc = "Supports all markets. Requires 9$/month account minimum! They do NOT provide functional free account." + Environment.NewLine
                     + "9$/month gives 10,000 Req/mo. Thats enough for personal use for few hundred stocks with normal using." + Environment.NewLine
                     + "Web Appl: => Works on WASM, Fast, All markets! This is one I have atm personally (premium) (testing ongoing atm) " + Environment.NewLine
//                     + "Priv Srv: => As of 2021-Nov on progress to study if their premium plans works properly for backend usage " + Environment.NewLine
                     + "If registering premium please use following link, price for you is same, but using it provides some development funds for this project." + Environment.NewLine
                     + "https://marketstack.com/?utm_source=FirstPromoter&utm_medium=Affiliate&fpr=pfs",
            },
            [ExtDataProviders.AlphaVantage] = new DlgProviderCfg() // 
            {
                Name = "AlphaVantage",
                Addr = "https://www.alphavantage.co/",
                Desc = "Support US, and other markets! " + Environment.NewLine
                     + "Web Appl: => Slow, as speed capped to 5 stocks per minute fetching. 500 request per day limit." + Environment.NewLine
//                     + "Priv Srv: => 500 request per day, OK, but limits max amount of stocks clearly under that number." + Environment.NewLine
                     + "Premium: => 50$/month for 75 API request per minute would solve speed issues. Plus add unlimited daily requests. Not bad!",
            },

            [ExtDataProviders.Polygon] = new DlgProviderCfg()   // 
            {
                Name = "Polygon",
                Addr = "https://polygon.io/",
                Desc = "All good option for US markets, sadly no other markets. Able to provide currency rates! Data availability is ?? hours." + Environment.NewLine
                     + "Web Appl: => Slow, as speed capped to 5 stocks per minute fetching, so usable but takes some waiting." + Environment.NewLine
//                     + "Priv Srv: => Speed limit doesnt matter so much here. No limits except speed-cap. Havent tested yet properly! " + Environment.NewLine
                     + "Premium: => 99$ per month is hefty price for personal use. Cant see plan for server / commercial usage.",
//                     + "Extra! !!!TODO!!! Have lot of nice features there, needs way more testing asap! Promising looking!",
            },

            [ExtDataProviders.Iexcloud] = new DlgProviderCfg()              // Updated 2021-Nov-16th (finished week plus wasm only testing w res: NICE!)
            {
                Name = "iexcloud",
                Addr = "https://iexcloud.io/",
                Desc = "Supports all markets. Fast. Seams to have good coverage of markets & stocks. Recommended!" + Environment.NewLine
                     + "Web Appl: => Almost perfection with Free account for WASm use! Plus has nice Intraday feature! " + Environment.NewLine
//                     + "Priv Srv: => their history is overly expensive, so Free account is not usable for PrivSrv + Free only gives 5 years history" + Environment.NewLine
                     + "Premium: => 100$/year for personal.. is definedly good option for more serious personal use. " + Environment.NewLine
                     + "Looks pretty trustable and good option per initial testing, but data is available late, about 6+ hours after market closing. " + Environment.NewLine
                     + "Did run about week testing w 200 stocks on 2021-Nov, all nice as long as just waits enough long after market close!" + Environment.NewLine
                     + "199$/m for business version so havent got change to test this on PrivSrv side, as history functionalities are just super expensive on free account"
            },
        });

        protected string _providerDesc = string.Empty; // Has to copy here as cant do bind to dictionary field wo errors

        bool _isDemoAccount = false;

        protected override async Task OnInitializedAsync()
        {
            AccountTypeID SessionAccountType = (AccountTypeID)Enum.Parse(typeof(AccountTypeID), PfsClientAccess.Account().Property("ACCOUNTTYPE"));

            switch (SessionAccountType)
            {
                case AccountTypeID.Demo:
                    _isDemoAccount = true;
                    break;
            }

            if (UseCase == UseCaseID.PRIV_SERV_SETT)
            {
                _configs = await PfsClientAccess.PrivSrvMgmt().ProviderConfigsGetAsync();

                if (_configs == null)
                {
                    // Cant connect to Priv Server.. show error & kick root         !!!TODO!!! Missing kick kick kick... and log...
                    _configs = new(); // temporary hack
                }

                _availableProviders = new();

                foreach (ExtDataProviders provider in Enum.GetValues(typeof(ExtDataProviders)))
                {
                    if (provider == ExtDataProviders.Unknown)
                        continue;

                    // PrivServ has each and every provider available, as otherwise it would not be listed
                    _availableProviders.Add(provider);
                }
            }
            else if ( UseCase == UseCaseID.LOCAL_SETT )
            {
                _configs = PfsClientAccess.Account().GetLocalMarketProviders();

                // Client side has limitation to providers
                _availableProviders = PfsClientPlatform.GetClientProviderIDs(ExtDataProviderJobType.EndOfDay);
            }
        }

        protected void OnProviderChanged(object provider)
        {
            _selectedProvider = (ExtDataProviders)Enum.Parse(typeof(ExtDataProviders), provider.ToString());
            _providerDesc = _description[_selectedProvider].Desc;
        }

        protected async Task OnKeySaveAsync()
        {
            _selectedProvider = ExtDataProviders.Unknown;

            if (UseCase == UseCaseID.PRIV_SERV_SETT)
            {
                await PfsClientAccess.PrivSrvMgmt().ProviderConfigsSetAsync(_configs);
            }
            else if ( UseCase == UseCaseID.LOCAL_SETT )
            {
                PfsClientAccess.Account().SetLocalMarketProviders(_configs);
            }
        }
        private async Task OnBtnManualTestAsync()
        {
            var parameters = new DialogParameters();
            parameters.Add("STID", Guid.Empty); // <= Dlg goes to manual use mode to query ticker
            parameters.Add("UseCase", DlgTestStockFetch.UseCaseID.LOCAL);

            // !!!NOTE!!! MainLayout has default's for options
            var dialog = Dialog.Show<DlgTestStockFetch>("", parameters);
            await dialog.Result;
        }

        protected struct DlgProviderCfg
        {
            public string Name;
            public string Addr;
            public string Desc;
        };
    }
}
