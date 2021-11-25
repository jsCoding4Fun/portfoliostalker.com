/*
 * Copyright (c) 2021 Jami Suni
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using MudBlazor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using System.Text;
using System.Timers;

using Serilog;

using PFS.Shared.Types;
using PFS.Shared.UiTypes;
using PFS.Shared.TraceAPIs;

using PFS.Client;
using PFS.Types.WebAPI;
using PFS.Shared.ExtProviders;

namespace PfsDevelUI.PFSLib
{
    public class PfsClientAccess
    {
        static public PfsClientAccess Ref = null;

        protected PfsClient _pfsClient = null;

        // 'PfsClient' provided API's
        protected IAccountData _accountData = null;
        protected IStalkerMgmt _stalkerMgmt = null;
        protected INoteMgmt _noteMgmt = null;
        protected IFetchData _fetchData = null;
        protected IReportData _reportData = null;
        protected IPrivSrvMgmt _privSrvMgmt = null;

        protected StringBuilder _recorder = null;
        protected TraceSymbols _recordedSymbols = new TraceSymbols();

        public string PfsClientVersionNumber { get; set; } = "N/A";

        public CurrencyCode HomeCurrency { get { return (CurrencyCode)Enum.Parse(typeof(CurrencyCode), Account().Property("HOMECURRENCY")); } }
        public PfsClientAccess(PfsClientPlatform pfsClientPlatform, IPfsSrvWebAPI pfsSrvCalls = null)
        {
            Ref = this; // !!!NASTY!!! Someday hopefully can figure clean solution but thats going to take some reading...

            // Note! Version number from project settings
            PfsClientVersionNumber = Assembly.GetExecutingAssembly().
                                        GetCustomAttribute<AssemblyInformationalVersionAttribute>().
                                        InformationalVersion;

            // Fully syncronous initialization of PfsClient per LocalStorage hold information
            _pfsClient = new PfsClient();
            _pfsClient.Init(pfsClientPlatform, pfsSrvCalls, PfsClientVersionNumber);

            //
            // MarketDataProviders & CurrencyDataProviders pushed to PFS.Client here, as it doesnt have dependency to library holding them 
            //

            // We follow list for UI implemented 'GetClientProviderIDs' to add them here so that these two places stay connected
            List <ExtDataProviders> allProviders = pfsClientPlatform.GetClientProviderIDs(ExtDataProviderJobType.EndOfDay);
            allProviders.AddRange(pfsClientPlatform.GetClientProviderIDs(ExtDataProviderJobType.Currency));

            foreach (ExtDataProviders providerID in allProviders.Distinct().ToList())
            {
                IExtDataProvider extDataProvider = null;

                switch ( providerID )
                {
                    case ExtDataProviders.Unibit:
                        extDataProvider = new ExtMarketDataUNIBIT();
                        break;

                    case ExtDataProviders.Polygon:
                        extDataProvider = new ExtMarketDataPolygon();
                        break;

                    case ExtDataProviders.Marketstack:
                        extDataProvider = new ExtMarketDataMarketstack();
                        break;
#if false // Search: "TIINGO-NOT-ON-LOCAL"
                    case ExtDataProviders.Tiingo:
                        extDataProvider = new ExtMarketDataTiingo();
                        break;
#endif
                    case ExtDataProviders.AlphaVantage:
                        extDataProvider = new ExtMarketDataAlphaVantage();
                        break;

                    case ExtDataProviders.Iexcloud:
                        extDataProvider = new ExtMarketDataIexcloud();
                        break;
                }

                if (extDataProvider != null)
                {
                    // PfsClient wants objects
                    _pfsClient.InitAddExtDataProvider(providerID, extDataProvider);

                    // And we sit on them in 'PfsClientPlatform' for UI's use cases
                    pfsClientPlatform.OnInitAddProviderObj(providerID, extDataProvider);
                }
            }

            // Even Tiingo is NOT supported by WASM, we need it on 'pfsClientPlatform' as its used to PrivSrv Support(market?) queries
            pfsClientPlatform.OnInitAddProviderObj(ExtDataProviders.Tiingo, new ExtMarketDataTiingo()); // Search: "TIINGO-NOT-ON-LOCAL"

            // First do direct linking to API
            _accountData = _pfsClient.Account();

            _stalkerMgmt = _pfsClient.StalkerMgmt();
            _noteMgmt = _pfsClient.NotesMgmt();
            _fetchData = _pfsClient.Fetch();
            _reportData = _pfsClient.Report();
            _privSrvMgmt = _pfsClient.PrivSrv();

            // Add capture wrapper to each Client API interface that PfsWebAppl uses. Allows to record all API calls for replay/testing purposes
            if (_accountData.Property("RECORDING") == "TRUE ")
            {
                TraceAccountData traceAccountData = new TraceAccountData(ref _pfsClient.Account());
                traceAccountData.ParsingEvent += OnRecordingEvent;
                _accountData = traceAccountData;

                TraceStalkerMgmt traceStalkerMgmt = new TraceStalkerMgmt(ref _pfsClient.StalkerMgmt());
                traceStalkerMgmt.ParsingEvent += OnRecordingEvent;
                _stalkerMgmt = traceStalkerMgmt;

                TraceNoteMgmt traceNoteMgmt = new TraceNoteMgmt(ref _pfsClient.NotesMgmt(), ref _recordedSymbols);
                traceNoteMgmt.ParsingEvent += OnRecordingEvent;
                _noteMgmt = traceNoteMgmt;

                TraceFetchData traceFetchData = new TraceFetchData(ref _pfsClient.Fetch());
                traceFetchData.ParsingEvent += OnRecordingEvent;
                _fetchData = traceFetchData;

                TraceReportData traceReportData = new TraceReportData(ref _pfsClient.Report(), ref _recordedSymbols);
                traceReportData.ParsingEvent += OnRecordingEvent;
                _reportData = traceReportData;

                TracePrivSrvMgmt tracePrivSrvMgmt = new TracePrivSrvMgmt(ref _pfsClient.PrivSrv(), ref _recordedSymbols);
                tracePrivSrvMgmt.ParsingEvent += OnRecordingEvent;
                _privSrvMgmt = tracePrivSrvMgmt;

                RecordingOn();
            }
        }

        protected void OnRecordingEvent(object sender, string line)
        {
            if (_recorder == null)
                return;

            _recorder.AppendLine(line);
        }

        public void OnRecordingLogEvent(string line)
        {
            if (_recorder == null)
                return;

            _recorder.AppendLine('#' + line);
        }

        public void RecordingOn()
        {
            if (_recorder == null)
                // Start recording of API activity
                _recorder = new();
        }

        public void RecordingOff()
        {
            _recorder = null;
        }

        public void RecordingEmpty()
        {
            if (_recorder != null)
                _recorder = new();
        }

        public string RecordingGet()
        {
            if (_recorder == null)
                return string.Empty;

            return _recorder.ToString();
        }

        // !!!NOTE!!! As wanting to have possibility to record API traffic, we cant allow direct usage of PfsClient
        //            instead we provide those API's here w capturing attached to them

        public ref IStalkerMgmt StalkerMgmt() { return ref _stalkerMgmt; }
        public ref INoteMgmt NoteMgmt() { return ref _noteMgmt; }
        public ref IReportData Report() { return ref _reportData; }
        public ref IFetchData Fetch() { return ref _fetchData; }
        public ref IAccountData Account() { return ref _accountData; }

        public ref IPrivSrvMgmt PrivSrvMgmt() { return ref _privSrvMgmt; }

        public HashSet<NavTreeElem> GetTreeData()
        {
            HashSet<NavTreeElem> ret = new HashSet<NavTreeElem>();
            List<NavTreeElem> tempList = new List<NavTreeElem>();  // temporary list to find parents easier than multilevel HashSet mess...

            List<FetchTreeData> inputList = _pfsClient.Fetch().GetTreeData();

            foreach (FetchTreeData input in inputList)
            {
                NavTreeElem output = new NavTreeElem(input.Name, input.Type);

                if (string.IsNullOrEmpty(input.ParentName) == true)
                {
                    ret.Add(output);
                    tempList.Add(output);
                }
                else
                {
                    NavTreeElem parent = tempList.First(x => x.Name == input.ParentName && x.Type == Local_ParentType(output.Type) );

                    if (parent.TreeItems == null)
                        parent.TreeItems = new HashSet<NavTreeElem>();

                    parent.TreeItems.Add(output);
                    parent.IsExpanded = true;
                    tempList.Add(output);
                }
            }

            return ret;


            ViewTreeEntry Local_ParentType(ViewTreeEntry child)
            {
                switch ( child )
                {
                    case ViewTreeEntry.Portfolio: return ViewTreeEntry.Account;
                    case ViewTreeEntry.StockGroup: return ViewTreeEntry.Portfolio;
                }
                return ViewTreeEntry.Unknown;
            }
        }
    }

    public class NavTreeElem
    {
        public string Name { get; set; }

        public ViewTreeEntry Type { get; set; }

        public string Icon { get; set; }

        public bool IsExpanded { get; set; }

        public HashSet<NavTreeElem> TreeItems { get; set; }

        public NavTreeElem(string name, ViewTreeEntry type)
        {
            Name = name;
            Type = type;

            switch ( type )
            {
                case ViewTreeEntry.Account:
                    Icon = Icons.Filled.Label; //  AccountCircle;
                    break;

                case ViewTreeEntry.Portfolio:
                    Icon = Icons.Filled.Label;
                    break;

                case ViewTreeEntry.StockGroup:
                    Icon = Icons.Filled.QuestionAnswer;
                    break;

                default:
                    Icon = Icons.Filled.QuestionAnswer;
                    break;
            }
        }
    }
}
