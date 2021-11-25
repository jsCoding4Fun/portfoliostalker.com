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

using PfsDevelUI;
using PfsDevelUI.Shared;
using PfsDevelUI.PFSLib;

using MudBlazor;

using PFS.Shared.Types;
using PFS.Shared.UiTypes;

namespace PfsDevelUI.Components
{
    // Step-by-Step wizard to setup minimal tuff for new client so that can see some action easily...
    public partial class DlgSetupWizard
    {
        [Inject] PfsClientAccess PfsClientAccess { get; set; }
        [Inject] PfsClientPlatform PfsClientPlatform { get; set; }
        [Inject] private IDialogService Dialog { get; set; }

        [CascadingParameter] MudDialogInstance MudDialog { get; set; }

        protected bool _fullscreen { get; set; } = false;

        protected string _nextButton { get; set; } = "Next";

        List<MarketMeta> _allMarkets = null;

        protected string _overviewOfSetup = string.Empty;

        // MARKET(S)
        protected string _selMarkets = string.Empty;



        // 
        List<StockMeta> _viewedStocks = null;
        string _search = "";

        object m_selStockTicker;

        protected int _tabActivePanel = 0;
        protected bool[] _tabDisabled = new bool[(int)ProgressID.MarkerOfLast];

        protected enum ProgressID : int
        {
            Markets = 0,
            Currency,
            Provider,
            Stocks,
            Overview,

            MarkerOfLast,
        }

        // Setup Selections

        protected class Setup
        {
            public string Entry { get; set; }
            public CurrencyCode Currency { get; set; }

            public ExtDataProviders ProviderID { get; set; }
        }

        protected List<Setup> _setup = null;

        protected override void OnInitialized()
        {
            // All markets available on selection list
            _allMarkets = PfsClientAccess.Fetch().GetMarketMeta(false/*all*/);

            foreach (ProgressID id in Enum.GetValues(typeof(ProgressID)))
            {
                if (id == ProgressID.MarkerOfLast)
                    continue;

                _tabDisabled[(int)id] = true;
            }
            _tabDisabled[0] = false;
        }

        #region MARKETS

        protected MarketID[] GetMarketIDs()
        {
            if (string.IsNullOrWhiteSpace(_selMarkets))
                return null;

            string[] marketIDs = _selMarkets.Split(',');

            List<MarketID> ret = new();

            foreach (string m in marketIDs)
            {
                string marketID = m.TrimStart().TrimEnd(); // leaves dang spaces there, so use separate trimmed variables...

                ret.Add((MarketID)Enum.Parse(typeof(MarketID), marketID));
            }

            return ret.ToArray();
        }

        #endregion

        #region CURRENCY

        protected bool _requireCurrencyProvider = false;
        protected ExtDataProviders _currencyProviderID = ExtDataProviders.Unknown;
        protected string _currencyProviderKey = string.Empty;

        protected CurrencyCode _homeCurrency = CurrencyCode.USD;
        protected CurrencyCode HomeCurrency
        {
            get { return _homeCurrency; }

            set
            {
                _homeCurrency = value;

                CheckIfCurrencyProviderRequired();
            }
        }

        protected void CheckIfCurrencyProviderRequired()
        {
            // If all selected markets are same currency, as selected home currency then no
            foreach (Setup s in _setup)
            {
                if (s.Currency != _homeCurrency)
                {
                    _requireCurrencyProvider = true;
                    return;
                }
            }
            _requireCurrencyProvider = false;
        }

        #endregion

        #region PROVIDER

        protected ExtDataProviders _providerID = ExtDataProviders.Unknown;
        protected ProviderCfg _providerCfg = null;
        protected string _providerKey = string.Empty;
        protected int _providerProposal = 0;

        protected static readonly ReadOnlyDictionary<ExtDataProviders, ProviderCfg> _providersDesc = new ReadOnlyDictionary<ExtDataProviders, ProviderCfg>(new Dictionary<ExtDataProviders, ProviderCfg>
        {
            [ExtDataProviders.Unibit] = new ProviderCfg()
            {
                Desc = "Unibit.ai. USA + TSX + Most markets. Free account is OK option as its super fast, and  "
                     + "has excelent coverage of markets. Its limited per monthly max credit use, but as only "
                     + "fetching end of day data their free plan should be ok for 150 stocks!. "
                     + "Sadly, there is often days that bunch of stocks End-Of-Day valuation are many hours late!",
            },
            [ExtDataProviders.Iexcloud] = new ProviderCfg()
            {
                Desc = "iexcloud.io. All markets. Free account has enough credits, those for normal local user cases "
                     + "should provide plenty of space to track different stocks and markets. As a new integration "
                     + "my testing is still going on for this, but looks one of best options to use specially if "
                     + "wanting to do international markets. Slow to get days data, often 6 hours before data is available. ",
            },
            [ExtDataProviders.AlphaVantage] = new ProviderCfg()
            {
                Desc = "Alphavantage.co. USA + TSX. Free account is OK option to try out, but please remember they "
                     + "enforce 5 tickers per minute fetch speed for free account. This gets slow if has 50+ "
                     + "stocks tracking as initial loading is going to take good while each day (10min+). "
                     + "This actually is one of trusted providers I prefer to use on my PrivSrv. ",
            },
            [ExtDataProviders.Polygon] = new ProviderCfg()
            {
                Desc = "Polygon.io. US markets only. Free account is OK option to try out, but please remember they "
                     + "enforce 5 tickers per minute fetch speed for free account. This gets slow if has 50+ "
                     + "stocks tracking as initial loading is going to take good while each day (10min+). ",
            },
            [ExtDataProviders.Marketstack] = new ProviderCfg()
            {
                Desc = "Marketstack.com. All markets. Fast. They do NOT have usable free account. Premium account is 9$ per "
                     + "month for few hundred stocks tracking. This is one I use personally as of 2021-Nov (testing on going atm).",
            },
        });

        protected void OnBtnAlternativeProvider()
        {
            _providerProposal++;
            SetProviderProposal();
        }

        protected bool SetProviderProposal()
        {
            // This is ones we have configuring here on Setup Wizard to be available for setup
            List<ExtDataProviders> suppProviders = _providersDesc.Keys.ToList();

            // And these are ones UI per current settings supports
            List<ExtDataProviders> uisProviders = PfsClientPlatform.GetClientProviderIDs(ExtDataProviderJobType.EndOfDay);

            // Create simple dictionary w providerID - bool to eliminate all those cant do whats asked by user...
            Dictionary<ExtDataProviders, bool> candidates = new();

            foreach ( ExtDataProviders providerID in suppProviders )
            {
                if (uisProviders.Contains(providerID) == false)
                    continue;

                candidates.Add(providerID, true);
            }

            // These are markets we want our proposed providers to support
            MarketID[] reqMarketIDs = GetMarketIDs();

            // Loop markets one by one, and eliminate candidates those cant do that market
            foreach ( MarketID marketID in reqMarketIDs )
            {
                // Loop actual enum, as we wanna be able to edit 'candidates' list inside here
                foreach (ExtDataProviders providerID in Enum.GetValues(typeof(ExtDataProviders)))
                {
                    if (candidates.ContainsKey(providerID) == false)
                        // Not even candidate, so skip
                        continue;

                    if (candidates[providerID] == false)
                        // Already rejected
                        continue;

                    IExtDataProvider providerObj = PfsClientPlatform.GetProviderObj(providerID);

                    if (providerObj == null)
                        // Hmm, should this even happen, anyway skip...
                        continue;

                    if (providerObj.Support(marketID) == false)
                        // Missed a market, rejected...
                        candidates[providerID] = false;
                }
            }

            List<ExtDataProviders> availableProviders = candidates.Where(x => x.Value == true).Select(x => x.Key).ToList();

            if ( availableProviders.Count() == 0)
                // Note! There always should be at least one candidate left, as otherwise would not add its market..
                return false;

            _providerID = availableProviders[_providerProposal % availableProviders.Count()];
            _providerCfg = _providersDesc[_providerID];
            return true;
        }

        protected class ProviderCfg
        {
            public string Desc { get; set; }
        }

#endregion

        #region STOCKS

        public string _pfName = "My Portfolio";
        public string _sgName = "Stocks";

        #endregion

        protected StockMeta _addStockMeta = null;

        protected async Task OnSearchChangedAsync(string search)
        {
            await UpdateStocks();
        }

        protected async Task UpdateStocks()
        {
            if (string.IsNullOrWhiteSpace(_search) == true)
                return;

            MarketID[] marketIDs = GetMarketIDs();

            _viewedStocks = new();

            foreach ( MarketID marketID in marketIDs )
            {
                List<CompanyMeta> searchCompanies = await PfsClientAccess.Fetch().SearchCompaniesAsync(marketID, _search);

                if (searchCompanies.Count() == 0)
                    continue;

                _viewedStocks.AddRange(searchCompanies.ConvertAll(s => new StockMeta()
                {
                    MarketID = marketID,
                    Ticker = s.Ticker,
                    Name = s.CompanyName,
                    STID = Guid.Empty,
                }));
            }
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

        private async Task OnBtnNext()
        {
            switch ((ProgressID)_tabActivePanel)
            {
                case ProgressID.Markets:
                    {
                        if (string.IsNullOrWhiteSpace(_selMarkets) == true)
                            return;

                        MarketID[] marketIDs = GetMarketIDs();

                        if (marketIDs == null || marketIDs.Count() > 5)
                        {
                            await Dialog.ShowMessageBox("Lets keep this simple!", "Max 5 selections, as you can later add more", yesText: "Ok");
                            return;
                        }

                        _setup = new();

                        foreach (MarketID marketID in marketIDs)
                        {
                            Setup s = new()
                            {
                                Entry = marketID.ToString(),
                                Currency = _allMarkets.Single(m => m.ID == marketID).Currency,
                                ProviderID = ExtDataProviders.Unknown,
                            };

                            _setup.Add(s);
                        }

                        _tabDisabled[(int)ProgressID.Markets] = true;
                        _tabDisabled[(int)ProgressID.Currency] = false;

                        CheckIfCurrencyProviderRequired();
                        _tabActivePanel = (int)ProgressID.Currency;
                    }
                    break; // => _setup -list is created w row per selected market

                case ProgressID.Currency:
                    {
                        if (_requireCurrencyProvider == true && _currencyProviderKey.Length != GetKeyLength(ExtDataProviders.Polygon))
                        {
                            await Dialog.ShowMessageBox("Missing key!", "For this market / currency selection we do need that key, please.", yesText: "Ok");
                            return;
                        }

                        if (SetProviderProposal() == false)
                        {
                            await Dialog.ShowMessageBox("Coding error!", "Please report case for developers, interesting failure...", yesText: "Ok");
                            return;
                        }

                        _tabDisabled[(int)ProgressID.Currency] = true;
                        _tabDisabled[(int)ProgressID.Provider] = false;

                        _tabActivePanel = (int)ProgressID.Provider;

                        _currencyProviderID = _requireCurrencyProvider == false ? ExtDataProviders.Unknown : ExtDataProviders.Polygon;

                        Setup s = new()
                        {
                            Entry = "Home Currency",
                            Currency = _homeCurrency,
                            ProviderID = _currencyProviderID,
                        };

                        _setup.Add(s);
                    }
                    break; // => if '_requireCurrencyProvider' is TRUE then '_currencyProviderKey' has key for it

                case ProgressID.Provider:
                    {
                        if (_providerKey.Length != GetKeyLength(_providerID))
                        {
                            await Dialog.ShowMessageBox("Missing key!", "Need to give that key, as this application uses it on your browser session to fetch stock valuations.", yesText: "Ok");
                            return;
                        }

                        _tabDisabled[(int)ProgressID.Provider] = true;
                        _tabDisabled[(int)ProgressID.Stocks] = false;

                        _tabActivePanel = (int)ProgressID.Stocks;
                    }
                    break;

                case ProgressID.Stocks:
                    {
                        if (string.IsNullOrWhiteSpace(_pfName) == true)
                            _pfName = "My Portfolio";

                        if (string.IsNullOrWhiteSpace(_sgName) == true)
                            _pfName = "Stocks";

                        if ( (m_selStockTicker == null || string.IsNullOrWhiteSpace(m_selStockTicker.ToString())) && _viewedStocks.Count() != 1)
                        {
                            await Dialog.ShowMessageBox("Select stock!", "Lets get one stock selected so have something to start with.", yesText: "Ok");
                            return;
                        }

                        _addStockMeta = new()
                        {
                            STID = Guid.Empty,
                            Name = string.Empty,
                        };

                        if ( m_selStockTicker != null )
                        {
                            string[] split = m_selStockTicker.ToString().Split(' ');
                            _addStockMeta.MarketID = (MarketID)Enum.Parse(typeof(MarketID), split[0]);
                            _addStockMeta.Ticker = split[1];
                        }
                        else
                        {
                            _addStockMeta.MarketID = _viewedStocks[0].MarketID;
                            _addStockMeta.Ticker = _viewedStocks[0].Ticker;
                        }

                        _tabDisabled[(int)ProgressID.Stocks] = true;
                        _tabDisabled[(int)ProgressID.Overview] = false;

                        _tabActivePanel = (int)ProgressID.Overview;

                        SetOverviewOfSetup();
                        _nextButton = "Setup";
                    }
                    break;

                case ProgressID.Overview:
                    {
                        await RunWizardSetupAsync();

                        MudDialog.Close();
                    }
                    break;
            }
        }

        protected void SetOverviewOfSetup()
        {
            _overviewOfSetup = "Ready for launch, please start countdown for Portfoliostalker setup...";
        }

        protected bool _isSetupBusy = false;

        protected async Task RunWizardSetupAsync()
        {
            _overviewOfSetup = "To the moon...";
            _isSetupBusy = true;

            //
            // 1) Get existing settings, and apply market's & key selections
            //
            SettMarketProviders settings = PfsClientAccess.Account().GetLocalMarketProviders();

            if ( _currencyProviderID != ExtDataProviders.Unknown)
                // Currency providers key
                settings.ProviderKeys[_currencyProviderID] = _currencyProviderKey;

            settings.ProviderKeys[_providerID] = _providerKey;

            MarketID[] marketIDs = GetMarketIDs();

            foreach ( MarketID marketID in marketIDs )
            {
                if (settings.MarketsProvider.ContainsKey(marketID) == true)
                    settings.MarketsProvider[marketID] = _providerID;
                else
                    settings.MarketsProvider.Add(marketID, _providerID);
            }

            PfsClientAccess.Account().SetLocalMarketProviders(settings);

            //
            // 2) Set HomeCurrency & Currency Provider
            //

            PfsClientAccess.Account().Property("HOMECURRENCY", _homeCurrency.ToString());

            if ( _requireCurrencyProvider && _currencyProviderID != ExtDataProviders.Unknown)
                PfsClientAccess.Account().Property("CURRENCYPROVIDER", _currencyProviderID.ToString());

            //
            // 3) Create PF, SG & Add selected stock
            //

            PfsClientAccess.StalkerMgmt().DoAction(string.Format("Add-Portfolio PfName=[{0}]", _pfName));
            PfsClientAccess.StalkerMgmt().DoAction(string.Format("Add-Group PfName=[{0}] SgName=[{1}]", _pfName, _sgName));

            Guid STID = await PfsClientAccess.StalkerMgmt().AddStockTrackingAsync(_addStockMeta.MarketID, _addStockMeta.Ticker, _addStockMeta.Name);

            PfsClientAccess.StalkerMgmt().DoAction(string.Format("Follow-Group SgName=[{0}] Stock=[{1}]", _sgName, STID));

            //
            // 4) Try fetch some data from selected external provider
            //

            await PfsClientAccess.Fetch().DoUpdateExpiredEodAsync();

            //
            // 5) Kick currency fetch going on
            //

            PfsClientAccess.Account().Property("CURRENCYCONVERSIONDATE", "UPDATE");
        }

        protected int GetKeyLength(ExtDataProviders providerID)
        {
            switch ( providerID )
            {
                case ExtDataProviders.Polygon: return 32;
                case ExtDataProviders.Iexcloud: return 35;
                case ExtDataProviders.Unibit: return 32;
                case ExtDataProviders.Tiingo: return 40;
                case ExtDataProviders.Marketstack: return 32;
                case ExtDataProviders.AlphaVantage: return 16;
            }
            return 0;
        }
    }
}
