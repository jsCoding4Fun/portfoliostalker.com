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
using System.Text;
using Microsoft.AspNetCore.Components;

using PfsDevelUI.PFSLib;

using Microsoft.AspNetCore.Components.Forms;
using System.IO;

using Serilog;

using MudBlazor;

using PFS.Shared.Types;
using PFS.Shared.Stalker;
using PFS.Shared.StalkerAddons;
using PFS.Shared.ExtTransactions;

namespace PfsDevelUI.Components
{
    // Complicated piece of multi-step UI, allowing user to import external CSV etc records from Bank/Broker to PFS 
    public partial class TabImportTransactions
    {
        [Inject] IDialogService Dialog { get; set; }
        [Inject] PfsClientAccess PfsClientAccess { get; set; }

        [Parameter] public string PfName { get; set; } = string.Empty;  // Mandatory to be set by owner

        /* !!!DOCUMENT!!! Import Broker/Bank Transactions to PFS from external formats
         * 
         * As PFS system doesnt itself provide import for Broker/Bank transactions, but allows it by standard format only.
         * That conversion is done w help of 'IExtTransactions' routies those perform CSV -> EtRecord conversion, and 
         * then here lot of magic related to company matching and accepting result is done here. Finally properly formatted
         * conversion result is given to PFS in Stalker Action's format with help of 'StalkerExtTransactions'.
         */

        /* One of main problems on this conversion is matching information from Broker/Banks to actual companies, as they
         * give variation of information from stock a record is referring so some matching needs to be done automatic,
         * and even with help of user 'linking' unknown companies to recognized companies. 
         * 
         * Additionally this import is often way new user of PFS system starts setting up he's account, so is totally
         * empty and success of getting new user to system depends highly of success of this Import operation.. so 
         * goal is to make it as automatic as possible, but allow user manually solving problem situations so that can 
         * gain as full import as possible.. and fast way to import all old transactions to PFS to see benefits of using it!
         * 
         * Major missing link atm is that cant find source for ISIN's.. those would make everything so so much easier :/
         * 
         * Linking companies has following functionalities:
         * 
         * - To actually proceed with storing imported records, its stock must be under User's Tracked stocks ala Stalker 
         *   but part of process below has way of adding new stocks to Stalker per 'unknown' company solving
         *   
         * - Ticker rules, so limitation to one stock per account w specific ticker, tuff luck otherwise. Anyway match to 
         *   stalker list is done per ticker if one is given by transaction.
         *   
         *   
         *   
         * !!!TODO!!!  
         * 
         *  
         * 
         */

        protected string _mainView;
        protected Progress _progress = Progress.PreConversion;
        protected DateTime? _fromDay = new DateTime(DateTime.Now.Year, 1, 1);
        protected StalkerExtTransactions _stalkerExtTransactions = null;

        // This list is created this component, with help of DlgCompaniesLink to solve 'unknown' companies by user manually linking them correct ones

        protected enum Progress
        {
            PreConversion,          // User to select provider and import raw data from csv file
            ViewAll,                // Conversion w PFS.External.Transactions provider code, and processing information to present parsing status
                                    // plus change to link / track companies to account those are not yet followed but has now records on importing
            ViewAccepted,
            Finished,
            Failed,
        }

        /* Transaction Import Flow: (!Split small functions!)
         * 
         * PreConversion: 
         * 
         * 1) Select Import Format
         * 2) Import '_rawByteData' from file
         * 
         * ViewAll-Init:
         * 3) Create 'EtRecord's with PFS.External.Transactions -converters help
         * 4) Basic validation of 'EtRecord's, against clear range errors and duplicates
         * 5) Check all acceptable records, and track all companies wo STID available locally
         * 6) Use user's existing Stalker information to map companies with Ticker
         * 
         * ViewAll-Repeat:
         * 7) View Event status for user & wait actions
         * 
         * ViewAll-AddCompanies
         * 8) Allow launch dialog to automatically/manually assign records to companies, and add new tracked stocks for records
         * 9) Update 'EtRecord' with matched/added companies and re-ViewAll
         */

        // 1) Select Import Format
        ExtTransactionsProvider _selFormat = ExtTransactionsProvider.Unknown;   

        IBrowserFile _selectedFile = null;  // This is Microsoft provided, no nuget's required
        byte[] _rawByteData = null;         // <= stays same on whole time w original content unmodified

        // 2) Import '_rawByteData' from file
        private async Task OnInputFileChangeAsync(InputFileChangeEventArgs e) 
        {
            _selectedFile = e.File;

            MemoryStream ms = new MemoryStream();
            Stream stream = e.File.OpenReadStream();
            await stream.CopyToAsync(ms);

            _rawByteData = ms.ToArray();

            _mainView = GetProvider().Convert2RawString(_rawByteData);

            StateHasChanged();
        }

        List<ExtTransaction> _etRecords = null;   // Holder of converted transaction, and later limited to only acceptable ones...

        private async Task OnBtnDoConversionAsync() // From: PreConversion (view 'raw' data) ==> ViewAll (including transactions with errors)
        {
            if (_selFormat == ExtTransactionsProvider.Unknown || _rawByteData == null )
                return;

            // => _rawByteData

            if (ConvertRawData2EtRecords() == false )
            {
                await Dialog.ShowMessageBox("Failed!", "Conversion of content failed with selected broker/content format", yesText: "Ok");
                return;
            }

            // => _etRecords

            ValidateEtRecordsTransactionStatus();

            // => _etRecords with 'ProcessingStatus' set

            ListAllTargetCompanies();

            // => _targetCompanies with 'all companies' those have any record effecting them

            MapUsingStalkerTickerConnection();

            // => Add STID for those _targetCompanies that can be found from User's account (Stalker) by exact ticker match

            ViewAll(); // Finishes initial conversion, and shows records on minimal processed format to user

#if false
            StringBuilder bld = new();

            foreach (EtCompany company in _targetCompanies )
            {
                bld.AppendLine(string.Format("M=[{0}] T=[{1}] N=[{2}] I=[{3}] S=[{4}]", company.Market, company.Ticker, company.Name, company.ISIN, company.STID));
            }

            _mainView = bld.ToString();
#endif
        }

        // 3) Create 'EtRecord's with PFS.External.Transactions -converters help
        private bool ConvertRawData2EtRecords()
        {
            try
            {
                _etRecords = GetProvider().Convert2ExtTransaction(_rawByteData);

                return true;
            }
            catch (Exception e)
            {
                Log.Error(e, "Failed to Exception! Conversion() with selected provider failed.");
            }
            _progress = Progress.Failed;
            return false;
        }

        // 4) Basic validation of 'EtRecord's, against clear range errors and duplicates
        private void ValidateEtRecordsTransactionStatus()
        {
            // Recreate this always if needs validation, as Stalker gets updated per user actions
            _stalkerExtTransactions = new StalkerExtTransactions(PfName,
                                            (CurrencyCode)Enum.Parse(typeof(CurrencyCode), PfsClientAccess.Account().Property("HOMECURRENCY")),
                                            PfsClientAccess.StalkerMgmt().GetCopyOfStalker());

            foreach (ExtTransaction entry in _etRecords)
            {
                _stalkerExtTransactions.Validate(entry);
            }
        }

        private List<EtCompany> _targetCompanies = null;

        // 5) Check all acceptable records, and track all companies and potential STID if found match from Stalker
        private void ListAllTargetCompanies()
        {
            _targetCompanies = new();

            foreach ( EtCompany company in _etRecords.ConvertAll(r => r.Company).ToList() )
            {
                if (string.IsNullOrWhiteSpace(company.Ticker) == true && string.IsNullOrWhiteSpace(company.Name) == true)
                    continue;

                if (string.IsNullOrWhiteSpace(company.Ticker) == false && _targetCompanies.Any(c => c.Ticker == company.Ticker) == true)
                    continue;

                if (string.IsNullOrWhiteSpace(company.Name) == false && _targetCompanies.Any(c => c.Name == company.Name) == true)
                    continue;

                if (string.IsNullOrWhiteSpace(company.ISIN) == false && _targetCompanies.Any(c => c.ISIN == company.ISIN) == true )
                    continue;

                _targetCompanies.Add(new EtCompany()
                {
                    Market = company.Market,
                    Ticker = company.Ticker,
                    Name = company.Name,
                    ISIN = company.ISIN,
                    STID = Guid.Empty,
                });
            }
        }

        // 6) Use user's existing Stalker information to map companies with Ticker
        private void MapUsingStalkerTickerConnection()
        {
            StalkerContent stalkerContent = PfsClientAccess.StalkerMgmt().GetCopyOfStalker();

            foreach (EtCompany company in _targetCompanies )
            {
                if (string.IsNullOrWhiteSpace(company.Ticker) == true || company.STID != Guid.Empty )
                    continue;

                // Note! Ignores potential market information of search criteria.. ala follows one identical ticker per account
                StockMeta stockMeta = PfsClientAccess.StalkerMgmt().GetStockMeta(MarketID.Unknown, company.Ticker.ToUpper());

                if (stockMeta == null)
                    continue;

                // Updates record on '_targetCompanies' list
                company.STID = stockMeta.STID;

                // Updates records to matching Et's on '_etRecords' list
                _etRecords.Where(r => r.Company.Ticker == company.Ticker && r.Company.Name == company.Name && r.Company.ISIN == company.ISIN).ToList()
                          .ForEach(a => a.Company.STID = stockMeta.STID);
            }
        }

        // 7) View Event status for user & wait actions
        private void ViewAll()
        {
            int duplicates = 0;
            int errors = 0;
            int newCompanies = 0;

            StringBuilder bldr = new();

            // Loop each 'raw' broker converted transaction result, and do general Validation for them.. plus log entries
            foreach (ExtTransaction et in _etRecords)
            {
                if ( et.Status == ExtTransaction.ProcessingStatus.Duplicate )
                    duplicates++;

                else if (et.Status == ExtTransaction.ProcessingStatus.Acceptable)
                {
                    bldr.AppendLine(EtTransToString(et));
                }
                else 
                    errors++;
            }

            _mainView = string.Empty;

            if ( errors > 0 || duplicates > 0 )
            {
                _mainView += string.Format("Has {0} Errors, {1} Duplicate records!", errors, duplicates) + Environment.NewLine;
            }

            if (newCompanies > 0)
            {
                _mainView += string.Format("Has new company(s), those should be added now!") + Environment.NewLine;
            }

            if (string.IsNullOrEmpty(_mainView) == false)
                _mainView += Environment.NewLine;

            _mainView += bldr.ToString();

            _progress = Progress.ViewAll;
            return;
        }

        // 8) Allow launch dialog to automatically/manually assign records to companies, and add new tracked stocks for records
        protected async Task OnBtnAddNewCompanies()
        {
            var parameters = new DialogParameters();
            parameters.Add("Companies", _targetCompanies.ToArray());
            parameters.Add("PfName", PfName);

            DialogOptions maxWidth = new DialogOptions() { MaxWidth = MaxWidth.Medium, FullWidth = true };

            var dialog = Dialog.Show<DlgCompaniesAdd>("", parameters, maxWidth);

            var result = await dialog.Result;

            if (!result.Cancelled)
            {
                // 9) Update 'EtRecord' with matched/added companies and re-ViewAll
                List<EtCompany> dlgRet = (List<EtCompany>)result.Data;

                foreach (EtCompany company in dlgRet)
                {
                    _etRecords.Where(r => r.Company.Ticker == company.Ticker && r.Company.Name == company.Name && r.Company.ISIN == company.ISIN).ToList()
                              .ForEach(a => a.Company.STID = company.STID);
                }

                // What ever was linked or added on dialog should be now updated to all '_etRecords' as a 'assigned STID' w user tracking that stock
                ViewAll();
                StateHasChanged();
            }
        }

        private void OnBtnViewAccepted() // From: ViewAll  ==> ViewAccepted (view 'processable' transactions)
        {
            List<ExtTransaction> etAcceptableRecords = new();

            foreach (ExtTransaction et in _etRecords)
            {
                if (et.Status != ExtTransaction.ProcessingStatus.Acceptable)
                    continue;

                if (et.RecordDate < _fromDay)
                    continue;

                // Also this time we are going to surrender w initial broker specific ticker/name etc info, and use PFS defined
                StockMeta stockMeta = PfsClientAccess.StalkerMgmt().GetStockMeta(et.Company.STID);

                if (stockMeta == null)
                    // Should never ever happen
                    continue;

                et.Company.Market = stockMeta.MarketID.ToString();
                et.Company.Ticker = stockMeta.Ticker;
                et.Company.Name = stockMeta.Name;

                etAcceptableRecords.Add(et);
            }

            // Now only has EtRecords left those we actually can process...
            _etRecords = etAcceptableRecords.OrderBy(t => t.Company.Name).ThenBy(t => t.RecordDate).ToList();

            if (_etRecords.Count == 0)
            {
                _mainView = "Nothing todo, no new acceptable transaction records found.";
                _progress = Progress.Failed;
                return;
            }

            StringBuilder bldr = new();

            foreach ( ExtTransaction acceptable in _etRecords)
            {
                bldr.AppendLine(string.Format("[{0}] {1}", acceptable.Company.Name, EtTransToString(acceptable)));
            }

            _mainView = bldr.ToString();
            _progress = Progress.ViewAccepted;
            return;
        }

        private async Task OnBtnProcessAsync()
        {
            // Everything should be ready for actually processing transaction records, but we do have still DryRun possibility
            // so its time to create actual list of action's.. 

            StalkerContent dryContent = PfsClientAccess.StalkerMgmt().GetCopyOfStalker();
            dryContent.TrackActions();

            foreach ( ExtTransaction acceptable in _etRecords)
            {
                StalkerError err = StalkerError.OK;
                string action = _stalkerExtTransactions.Convert(acceptable);

                if (string.IsNullOrWhiteSpace(action) == true)
                    err = StalkerError.FAIL;
                else
                    err = dryContent.DoAction(action);

                if ( err != StalkerError.OK )
                {
                    Log.Error("Failed to Processing! {0}", EtTransToString(acceptable));
                    Log.Error("   with ActionCmd {0}", action);
                    _progress = Progress.Failed;
                    await Dialog.ShowMessageBox("Failed to process!", "Interrupting as failed, see logs!", yesText: "Ok");
                    return;
                }
            }

            // Getting here means last second DryRun was successfull... so lets push tuff in.. and be happy...
            List<string> actionSet = dryContent.GetActions();

            if (PfsClientAccess.StalkerMgmt().DoActionSet(actionSet) != StalkerError.OK )
            {
                Log.Error("Failed to Processing! ActionSet");
                _progress = Progress.Failed;
                return;
            }

            _mainView = string.Join(Environment.NewLine, actionSet);

            _progress = Progress.Finished;

            bool? result = await Dialog.ShowMessageBox("Success!", "All transactions acceptable transactions added now to account", yesText: "Ok");
        }

        protected string EtTransToString(ExtTransaction etTrans)
        {
            StringBuilder str = new();

            switch ( etTrans.Type )
            {
                case ExtTransaction.EtType.Buy:
                    str.Append("BUY>");
                    str.Append(LocalGetTimeAndUniqueID());
                    str.Append(LocalGetPrice());
                    str.Append(LocalGetCompany());
                    break;

                case ExtTransaction.EtType.Sell:
                    str.Append("SELL>");
                    str.Append(LocalGetTimeAndUniqueID());
                    str.Append(LocalGetPrice());
                    str.Append(LocalGetCompany());
                    break;

                case ExtTransaction.EtType.Divident:
                    str.Append("DIVIDENT>");
                    str.Append(LocalGetTimeAndUniqueID());
                    str.Append(LocalGetPrice());
                    str.Append(LocalGetCompany());
                    break;

                default:

                    return str.ToString();
            }

            return str.ToString();

            string LocalGetTimeAndUniqueID()
            {
                return etTrans.RecordDate.ToString("yyyy-MM-dd") + " ID=[" + etTrans.UniqueID + "] ";
            }

            string LocalGetPrice()
            {
                if ( etTrans.Fee > 0 )
                    return string.Format("U={0} ApU={1} F={2} ", etTrans.Units, etTrans.AmountPerUnit, etTrans.Fee);
                else
                    return string.Format("U={0} ApU={1} ", etTrans.Units, etTrans.AmountPerUnit);
            }

            string LocalGetCompany()
            {
                if (etTrans.Company.STID != Guid.Empty )
                {
                    StockMeta stockMeta = PfsClientAccess.StalkerMgmt().GetStockMeta(etTrans.Company.STID);

                    return string.Format("C={0}${1} [{2}]", stockMeta.MarketID.ToString(), stockMeta.Ticker, stockMeta.Name);
                }

                return string.Format("C[?NEW?]={0}${1} {2}", etTrans.Company.Market, etTrans.Company.Ticker, etTrans.Company.Name);
            }
        }

        protected IExtTransactions GetProvider() // !!!NOTE!!! These are provider's for Broker transaction converting available atm
        {
            switch (_selFormat)
            {
                case ExtTransactionsProvider.NordnetFI: return new EtNordnetFI();
                case ExtTransactionsProvider.NordeaFI: return new EtNordeaFI();
            }
            return null;
        }
    }
}
