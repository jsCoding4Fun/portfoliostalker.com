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
    // Handles market specific configuration what provider is used, and if has backup provider for retry
    public partial class CompSettMarkets
    {
        [Inject] PfsClientAccess PfsClientAccess { get; set; }
        [Inject] PfsClientPlatform PfsClientPlatform { get; set; }
        [Parameter] public UseCaseID UseCase { get; set; } = UseCaseID.UNKNOWN;


        public enum UseCaseID
        {
            UNKNOWN = 0,
            LOCAL_SETT,
            PRIV_SERV_SETT,
        }

        protected List<ViewMarketProviders> _marketProviders = null;

        protected override async Task OnInitializedAsync()
        {
            SettMarketProviders configs = null;

            List<ExtDataProviders> availableProviders = new(); // Filled with all providers available for requested mode

            // Get current settings

            if (UseCase == UseCaseID.PRIV_SERV_SETT)
            {
                configs = await PfsClientAccess.PrivSrvMgmt().ProviderConfigsGetAsync();

                if (configs == null)
                {
                    // Cant connect to Priv Server.. show error & jump main screen !!!TODO!!! If cant get settings, we dont wanna let edit either go away
                    configs = new(); // temporary hack.. 
                }

                foreach (ExtDataProviders provider in Enum.GetValues(typeof(ExtDataProviders)))
                {
                    if (provider == ExtDataProviders.Unknown)
                        continue;

                    // PrivServ has each and every provider available, as otherwise it would not be listed
                    availableProviders.Add(provider);
                }
            }
            else if ( UseCase == UseCaseID.LOCAL_SETT )
            {
                configs = PfsClientAccess.Account().GetLocalMarketProviders();

                // Client side has limitation to providers
                availableProviders = PfsClientPlatform.GetClientProviderIDs(ExtDataProviderJobType.EndOfDay);
            }

            // And setup temporary list we using to control grid per configs

            _marketProviders = new();

            List<MarketMeta> marketMeta = PfsClientAccess.Fetch().GetMarketMeta();

            foreach (MarketMeta market in marketMeta)
            {
                ViewMarketProviders mp = new()
                {
                    MarketID = market.ID,
                    Provider = configs.MarketsProvider.ContainsKey(market.ID) == true ? configs.MarketsProvider[market.ID] : ExtDataProviders.Unknown,
                    Backup = configs.MarketsBackupProvider.ContainsKey(market.ID) == true ? configs.MarketsBackupProvider[market.ID] : ExtDataProviders.Unknown,
                    CustomTimeEnabled = configs.MarketsManualFetchTime.ContainsKey(market.ID),
                    CustomTime = configs.MarketsManualFetchTime.ContainsKey(market.ID) == true ? configs.MarketsManualFetchTime[market.ID] : 0,
                    AvailableProviders = new(),
                };

                // spin each 'availableProvider' and see per its object if it actually can serve this market
                foreach (ExtDataProviders ap in availableProviders)
                {
                    IExtDataProvider providerObj = PfsClientPlatform.GetProviderObj(ap);

                    if (providerObj != null && providerObj.Support(market.ID) == true)
                        // Yes this provider is available, and able to support this market
                        mp.AvailableProviders.Add(ap);
                }

                // Both Local & PrivSrv case, we also support Unknown, to allow unset market use.. important for Local, and important for 
                // PrivSrv backup provider.. plus acceptable for PrivSrv main provider also... 

                mp.AvailableProviders.Add(ExtDataProviders.Unknown);

                _marketProviders.Add(mp);
            }
        }

        protected async Task OnBtnSaveAsync()
        {
            SettMarketProviders configs = null;

            if (UseCase == UseCaseID.PRIV_SERV_SETT)
                configs = await PfsClientAccess.PrivSrvMgmt().ProviderConfigsGetAsync();
            else if (UseCase == UseCaseID.LOCAL_SETT)
                configs = PfsClientAccess.Account().GetLocalMarketProviders();

            configs.MarketsManualFetchTime = new();

            // And overwrite this sett components effected fields w user selections
            foreach (ViewMarketProviders entry in _marketProviders)
            {
                // For main provider
                if (configs.MarketsProvider.ContainsKey(entry.MarketID) == true)
                    configs.MarketsProvider[entry.MarketID] = entry.Provider;
                else
                    configs.MarketsProvider.Add(entry.MarketID, entry.Provider);

                if (UseCase == UseCaseID.PRIV_SERV_SETT)
                {
                    // And potential retry backup
                    if (configs.MarketsBackupProvider.ContainsKey(entry.MarketID) == true)
                        configs.MarketsBackupProvider[entry.MarketID] = entry.Backup;
                    else
                        configs.MarketsBackupProvider.Add(entry.MarketID, entry.Backup);

                    // Plus see if has custom fetch time defined
                    if ( entry.CustomTimeEnabled == true && entry.CustomTime >= 0)
                        configs.MarketsManualFetchTime.Add(entry.MarketID, entry.CustomTime);
                }
            }

            if (UseCase == UseCaseID.PRIV_SERV_SETT)
                await PfsClientAccess.PrivSrvMgmt().ProviderConfigsSetAsync(configs);
            else if (UseCase == UseCaseID.LOCAL_SETT)
                PfsClientAccess.Account().SetLocalMarketProviders(configs);
        }

        public class ViewMarketProviders
        {
            public MarketID MarketID { get; set; }

            public List<ExtDataProviders> AvailableProviders { get; set; }

            public ExtDataProviders Provider { get; set; }

            public ExtDataProviders Backup { get; set; }

            public bool CustomTimeEnabled { get; set; }

            public int CustomTime { get; set; }
        }
    }
}
