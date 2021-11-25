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
using System.Dynamic;
using System.Globalization;
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

using BlazorDownloadFile;
using CsvHelper;

using PFS.Shared.UiTypes;
using System.Text;
using System.Text.Json;

namespace PfsDevelUI.Components
{
    // This report is specially targeted to more public/exportable holdings report generation
    public partial class ReportExportHoldings
    {
        // !!!TODO!!! View Group Total if SETT + do sorting INSIDE of group per SortBy
        [Inject] IDialogService Dialog { get; set; }
        [Inject] PfsClientAccess PfsClientAccess { get; set; }
        [Inject] PfsClientPlatform PfsClientPlatform { get; set; }
        [Inject] IBlazorDownloadFileService BlazorDownloadFileService { get; set; }

        protected const string _localStorageUiKey = "UiSettExportHoldings";

        protected List<ReportExportHoldingsData> _origData = null;   // Data from PFS.Client (single fetch)
        protected ReportExportHoldingsData _origTotal = null;
        protected List<ReportExportHoldingsData> _origGroupTotals = null;

        protected string _rawHtmlTable = string.Empty;
        protected string _rawHtmlNotes = string.Empty;

        protected bool _noContent = false; // Little helper to show nothing if has nothing

        // COLUMNS
        public enum RepColumn : int     // Presents all COLUMNS available on this report
        {
            Ticker,
            CompanyName,
            Group,
            AvrgPrice,              // Per Sett: HC / MC$
            AvrgTime,
            Invested,               // Only HC
            Valuation,              // Only HC
            WeightP,                // % of total Valuation
            DivPLast,
            DivTotal,
            DivPTotal,
            TotalGain,
            TotalPGain,
            Notes,
        }

        // SETTINGS
        public enum RepSettings : int   // Presents all ON/OFF settings supported for customizing report
        {
            AvrgPriceHC,
            InvertedSorting,
            ViewGroupTotals,
            ViewTotal,
            ShowPercentSigns,
            SeparateNotes,
        }

        // EXPORT
        public enum RepExport : int
        {
            CSV,
            HTMLTable,
            HTMLNotes,
        }

        protected override void OnParametersSet()
        {
            _selSettings = string.Empty;

            if (InitPerSavedSelections() == false )
            {
                // Loading stored selections/settings failed, so fallback to default ones

                _selColumns = String.Join(',', System.Enum.GetNames(typeof(RepColumn)));
                _selSettings = string.Empty;
                _sortBy = RepColumn.CompanyName.ToString();
            }

            // Per '_selColumns' set hash table to help setting checkboxes also..
            HashSet<string> selColumnDef = new HashSet<string>();
            foreach (RepColumn col in Enum.GetValues(typeof(RepColumn)))
                if ( _selColumns.Contains(col.ToString()))
                    selColumnDef.Add(col.ToString());
            SelColumnDef = selColumnDef;

            // Per _selSettings
            HashSet<string> selSettingsDef = new HashSet<string>();
            foreach (RepSettings sett in Enum.GetValues(typeof(RepSettings)))
                if (_selSettings.Contains(sett.ToString()))
                    selSettingsDef.Add(sett.ToString());
            SelSettingsDef = selSettingsDef;

            // Get report content from PFS.Client
            var reportData = PfsClientAccess.Report().GetExportHoldingsData(string.Empty);
            _origData = reportData.stocks;
            _origTotal = reportData.total;
            _origGroupTotals = reportData.groupTotals;

            // Lets fix viewed name for unassigned group if has one
            if (_origGroupTotals != null)
            {
                ReportExportHoldingsData unassigned = _origGroupTotals.SingleOrDefault(g => g.GroupDef == string.Empty);

                if (unassigned != null)
                    // Set 'name' something as thats needs to be set for group fields.. even for unassigned part of group totals
                    // but keep GroupDef empty for unassigned so it gets to pushed last on viewing 
                    unassigned.Name = "*unassigned to group*";
            }

            if (_origData != null)
                // First view is per stored settings combo
                RefreshReport();
            else
                _noContent = true;
        }

        // EV: Select COLUMNS

        private IEnumerable<string> SelColumnDef { get; set; } = new HashSet<string>(); // Helper to get checkboxes to match 'SelColumns'

        // Note! This little beauty here allows '_selColumns' to have list of selection, but on screen it just shows 'Select'
        protected string _selColumns = string.Empty;
        protected string SelColumns(List<string> columns)
        {
            _selColumns = string.Join(',', columns.ToArray());
            RefreshReport();
            return "Select";
        }

        // EV: Do SORTING

        protected string _sortBy = string.Empty;
        protected string SortBy { get { return _sortBy; }
            set { _sortBy = value; RefreshReport(); } }

        // EV: Do SETTINGS
        
        private IEnumerable<string> SelSettingsDef { get; set; } = new HashSet<string>(); // Helper to get checkboxes to match 'SelSettings'

        protected string _selSettings = string.Empty;
        protected string SelSettings(List<string> settings)
        {
            _selSettings = string.Join(',', settings.ToArray());
            RefreshReport();
            StateHasChanged();
            return "Select";
        }

        protected void RefreshReport()
        {
            // !!!NOTE!!! Uses special ""@((MarkupString)_rawHtmlTable)"" to output dynamically created HTML table on page
            _rawHtmlTable = CreateHTMLTable();

            if (_selSettings.Contains(RepSettings.SeparateNotes.ToString()))
                _rawHtmlNotes = CreateHTMLNotesTable();
            else
                _rawHtmlNotes = String.Empty;
        }

        protected List<dynamic> CreateDynRecords(List<ViewReportStockHoldingsData> data)
        {
            try
            {
                var records = new List<dynamic>();

                foreach (ViewReportStockHoldingsData entry in data)
                    records.Add(CreateDynActiveFields(entry));

                return records;
            }
            catch (Exception)
            {
            }
            return null;
        }

        protected dynamic CreateDynActiveFields(ViewReportStockHoldingsData data)
        {
            var activeFields = new ExpandoObject() as IDictionary<string, Object>;

            foreach (RepColumn col in Enum.GetValues(typeof(RepColumn)))
            {
                if (_selColumns.Contains(col.ToString()))
                {
                    var value = data.GetType().GetProperty(col.ToString()).GetValue(data, null);

                    if (value == null)
                        activeFields.Add(col.ToString(), string.Empty);
                    else
                        activeFields.Add(col.ToString(), value);
                }
            }
            return activeFields;
        }

        // Separate view of notes w separate html table used for formatting
        protected string CreateHTMLNotesTable()
        {
            try
            {
                if (_origData.Count == 0)
                    return String.Empty;

                StringBuilder content = new();
                content.AppendLine("<table>");
                content.AppendLine("<tfoot>");
                content.AppendLine("</tfoot>");

                foreach (ReportExportHoldingsData inData in _origData.OrderBy(s => s.Name).ToList())
                {
                    if (string.IsNullOrWhiteSpace(inData.ReportNote) == true)
                        continue;

                    content.AppendLine("<tbody>");

                    // Using footer to show name of company
                    content.AppendLine("<tfoot>");
                    content.AppendLine("<tr>");
                    content.AppendLine(string.Format("<th>[{0}:{1}] {2}</th>", inData.MarketID.ToString(), inData.Ticker, inData.Name));
                    content.AppendLine("</tr>");
                    content.AppendLine("</tfoot>");

                    // And normal row for notes itself
                    content.AppendLine("<tr>");
                    content.AppendLine(string.Format("<td>{0}</td>", inData.ReportNote));
                    content.AppendLine("</tr>");

                    content.AppendLine("</tbody>");
                }

                content.AppendLine("</table>");

                return content.ToString(); 
            }
            catch ( Exception )
            {
            }
            return string.Empty;
        }

        protected string CreateHTMLTable()
        {
            try
            {
                List<ViewReportStockHoldingsData> viewData = new();

                foreach (ReportExportHoldingsData inData in _origData)
                    viewData.Add(ConvertOrigDataToView(inData));

                if (viewData.Count == 0)
                {
                    _noContent = true;
                    return String.Empty;
                }

                dynamic firstRecord = CreateDynActiveFields(viewData[0]);

                StringBuilder content = new();
                content.AppendLine("<table>");
                content.AppendLine("<thead>");
                content.AppendLine("<tr>");

                foreach (KeyValuePair<string, object> kvp in (IDictionary<string, object>)firstRecord)
                {
                    content.AppendLine(string.Format("<th>{0}</th>", kvp.Key));
                }

                content.AppendLine("</tr>");
                content.AppendLine("</thead>");

                // Total row is per settings (we put it here first, as seams to push it around bit randomly)

                if (_selSettings.Contains(RepSettings.ViewTotal.ToString()) || _selSettings.Contains(RepSettings.ViewGroupTotals.ToString()))
                    // We actually include this also to 'ViewGroupTotals' even if 'ViewTotal' as otherwise things get messy on browser
                    Local_AddTotal(ref content, ConvertOrigDataToView(_origTotal, true));

                // Header is same for all cases, but body handling variers

                if (_selSettings.Contains(RepSettings.ViewGroupTotals.ToString()) && _origGroupTotals != null )
                    // 
                    Local_GroupTable(ref content, viewData);
                else
                    Local_StandardTable(ref content, viewData);

                content.AppendLine("</table>");

                return content.ToString();
            }
            catch (Exception)
            {
            }
            return string.Empty;


            void Local_StandardTable(ref StringBuilder content, List<ViewReportStockHoldingsData> viewData)
            {
                // Coming here we just wanna apply user setting sorting, and create one big table w all stocks entries
                List<dynamic> records = CreateDynRecords(ApplySorting(viewData));

                content.AppendLine("<tbody>");

                foreach (var rec in records)
                    Local_AddStock2Content(ref content, rec);

                content.AppendLine("</tbody>");
            }

            void Local_GroupTable(ref StringBuilder content, List<ViewReportStockHoldingsData> viewData)
            {
                // In this case groups are dictating viewing, so all stocks are under their groups

                List<ViewReportStockHoldingsData> viewGroups = new();
                foreach (ReportExportHoldingsData origGroupTotal in _origGroupTotals )
                    viewGroups.Add(ConvertOrigDataToView(origGroupTotal, true));

                List<ViewReportStockHoldingsData> sortedGroups = Local_GetSortedGroups(viewGroups);

                foreach (ViewReportStockHoldingsData group in sortedGroups)
                {
                    if (string.IsNullOrWhiteSpace(group.Group) == true)
                        //Unassigned group that goes last...
                        continue;

                    List<dynamic> records = CreateDynRecords(ApplySorting(viewData.Where(e => e.Group == group.Group).ToList()));

                    if (records.Count == 0)
                        //Not showing empty groups
                        continue;

                    Local_AddTotal(ref content, group);

                    content.AppendLine("<tbody>");

                    foreach (var rec in records)
                        Local_AddStock2Content(ref content, rec);

                    content.AppendLine("</tbody>");
                }

                // Plus there can be unassigned stocks, pretty much not yet set per group.. so those are shown end of report (wo own total)

                List<ViewReportStockHoldingsData> unassigned = viewData.Where(e => string.IsNullOrWhiteSpace(e.Group)).ToList();

                if (unassigned.Count > 0)
                {
                    ViewReportStockHoldingsData unassignedTotal = sortedGroups.Single(g => g.Group == string.Empty);
                    Local_AddTotal(ref content, unassignedTotal);

                    List<dynamic> records = CreateDynRecords(ApplySorting(unassigned).ToList());

                    content.AppendLine("<tbody>");

                    foreach (var rec in records)
                        Local_AddStock2Content(ref content, rec);

                    content.AppendLine("</tbody>");
                }
            }

            // Uses _origGroupTotals pulls things kind of sorted per generic selection if possible or defaults to user order
            List<ViewReportStockHoldingsData> Local_GetSortedGroups(List<ViewReportStockHoldingsData> unsortedGroups)
            {
                Func<ViewReportStockHoldingsData, Object> orderByFunc = null;

                switch (SortBy)
                {
                    case "Ticker": orderByFunc = field => field.Ticker; break;
                    case "CompanyName": orderByFunc = field => field.CompanyName; break;
                    case "Group": orderByFunc = field => field.Group; break;
                    case "AvrgPrice": orderByFunc = field => field.AvrgPriceD; break;
                    case "AvrgTime": orderByFunc = field => field.AvrgTimeD; break;
                    case "Invested": orderByFunc = field => field.InvestedD; break;
                    case "Valuation": orderByFunc = field => field.ValuationD; break;
                    case "WeightP": orderByFunc = field => field.WeightPD; break;
                    case "DivPLast": orderByFunc = field => field.DivPLastD; break;
                    case "DivTotal": orderByFunc = field => field.DivTotalD; break;
                    case "DivPTotal": orderByFunc = field => field.DivPTotalD; break;
                    case "TotalGain": orderByFunc = field => field.TotalGainD; break;
                    case "TotalPGain": orderByFunc = field => field.TotalPGainD; break;
                    case "Notes": orderByFunc = field => field.Notes; break;

                    default:
                        orderByFunc = field => field.CompanyName;
                        break;
                }

                if (_selSettings.Contains(RepSettings.InvertedSorting.ToString()))
                    return unsortedGroups.OrderByDescending(orderByFunc).ToList();
                else
                    return unsortedGroups.OrderBy(orderByFunc).ToList();
            }

            void Local_AddStock2Content(ref StringBuilder content, dynamic rec)
            {
                content.AppendLine("<tr>");
                foreach (KeyValuePair<string, object> kvp in (IDictionary<string, object>)rec)
                {
                    content.AppendLine(string.Format("<td>{0}</td>", kvp.Value.ToString()));
                }
                content.AppendLine("</tr>");
            }

            void Local_AddTotal(ref StringBuilder content, ViewReportStockHoldingsData viewTotal)
            {
                dynamic activeTotal = CreateDynActiveFields(viewTotal);

                content.AppendLine("<tfoot>");
                content.AppendLine("<tr>");
                foreach (KeyValuePair<string, object> kvp in (IDictionary<string, object>)activeTotal)
                {
                    if (kvp.Key == RepColumn.CompanyName.ToString() )
                    {
                        if (string.IsNullOrEmpty(kvp.Value.ToString()) == true)
                            content.AppendLine(string.Format("<th colspan='1'>Total:</th>"));
                        else
                            content.AppendLine(string.Format("<th colspan='1'>{0}:</th>", kvp.Value.ToString()));
                    }
                    else
                        content.AppendLine(string.Format("<th>{0}</th>", kvp.Value.ToString()));
                }
                content.AppendLine("</tr>");
                content.AppendLine("</tfoot>");
            }
        }

        // Eats PFS.Client given stock/total row, and outputs UI/Export ready formatted content for it...
        protected ViewReportStockHoldingsData ConvertOrigDataToView(ReportExportHoldingsData inData, bool total = false)
        {
            ViewReportStockHoldingsData outData = new()
            {
                Ticker = inData.Ticker,
                CompanyName = inData.Name,
                Group = inData.GroupDef,
                AvrgPrice = String.Empty,           // HC/MC => Depends from RepSettings.AvrgPriceHC    Carefull! Value always HC, view is HC/MC
                AvrgPriceD = 0,
                AvrgTime = String.Empty,
                AvrgTimeD = (int)(inData.AvrgTime),
                InvestedD = inData.HcInvested.HasValue ? inData.HcInvested.Value : 0,        // Invested HC only
                ValuationD = inData.HcValuation.HasValue ? inData.HcValuation.Value : 0,
                WeightPD = inData.HcWeightP.HasValue ? inData.HcWeightP.Value : 0,
                DivPLastD = inData.HcDividentLastP.HasValue && inData.HcDividentLastP.Value >= 0.05m ? inData.HcDividentLastP.Value : 0,
                DivTotalD = inData.HcDividentTotal.HasValue ? inData.HcDividentTotal.Value : 0,
                DivPTotalD = inData.HcDividentTotalP.HasValue && inData.HcDividentTotalP.Value >= 0.05m ? inData.HcDividentTotalP.Value : 0,
                TotalGainD = inData.HcTotalGain.HasValue ? inData.HcTotalGain.Value : 0,
                TotalPGainD = inData.HcTotalGainP.HasValue ? inData.HcTotalGainP.Value : 0,
                Notes = inData.ReportNote,
            };

            outData.AvrgPriceD = inData.HcAvrgPrice.HasValue ? inData.HcAvrgPrice.Value : 0;

            if (_selSettings.Contains(RepSettings.AvrgPriceHC.ToString()))
                outData.AvrgPrice = Local_FormatDecimal(outData.AvrgPriceD,2);    // HC viewed
            else
                outData.AvrgPrice = Local_FormatDecimal(inData.AvrgPrice,2) + UiF.Curr(inData.MarketCurrency); // MC$ viewed

            outData.Invested = outData.InvestedD.ToString("0");
            outData.Valuation = outData.ValuationD.ToString("0");
            outData.WeightP = Local_FormatDecimal(outData.WeightPD, 1);
            outData.DivPLast = Local_FormatDecimal(outData.DivPLastD, 1);
            outData.DivTotal = outData.DivTotalD.ToString("0");
            outData.DivPTotal = Local_FormatDecimal(outData.DivPTotalD, 1);
            outData.TotalGain = outData.TotalGainD.ToString("0");
            outData.TotalPGain = outData.TotalPGainD.ToString("0");

            if (_selSettings.Contains(RepSettings.ShowPercentSigns.ToString()))
            {
                outData.WeightP += "%";
                outData.DivPLast += "%";
                outData.DivPTotal += "%";
                outData.TotalPGain += "%";
            }

            outData.AvrgTime = Local_FormatAvrgTime((int)outData.AvrgTimeD);

            if (total)
            {
                // In case of total field, we do remove some tuff...
                outData.AvrgPrice = String.Empty;
            }

            return outData;


            string Local_FormatDecimal(decimal value, int digits)
            {
                if (value == 0)
                    return "0";

                switch ( digits )
                {
                    case 0: return value.ToString("0");
                    case 1: return value.ToString("0.0");
                    case 2: return value.ToString("0.00");
                    case 3: return value.ToString("0.000");
                }
                return value.ToString("0");
            }

            string Local_FormatAvrgTime(int months)
            {
                if (months < 12)
                    return months.ToString() + "m";

                if (months < 36)
                    return (months / 12).ToString() + "y" + (months % 12 != 0 ? (months % 12).ToString() + "m" : "");

                return (months / 12).ToString() + "y";
            }
        }

        List<ViewReportStockHoldingsData> ApplySorting(List<ViewReportStockHoldingsData> unsorted)
        {
            Func<ViewReportStockHoldingsData, Object> orderByFunc = null;

            switch (SortBy)
            {
                case "Ticker": orderByFunc = field => field.Ticker; break;
                case "CompanyName": orderByFunc = field => field.CompanyName; break;
                case "Group": orderByFunc = field => field.Group; break;
                case "AvrgPrice": orderByFunc = field => field.AvrgPriceD; break;
                case "AvrgTime": orderByFunc = field => field.AvrgTimeD; break;
                case "Invested": orderByFunc = field => field.InvestedD; break;
                case "Valuation": orderByFunc = field => field.ValuationD; break;
                case "WeightP": orderByFunc = field => field.WeightPD; break;
                case "DivPLast": orderByFunc = field => field.DivPLastD; break;
                case "DivTotal": orderByFunc = field => field.DivTotalD; break;
                case "DivPTotal": orderByFunc = field => field.DivPTotalD; break;
                case "TotalGain": orderByFunc = field => field.TotalGainD; break;
                case "TotalPGain": orderByFunc = field => field.TotalPGainD; break;
                case "Notes": orderByFunc = field => field.Notes; break;

                default:
                    orderByFunc = field => field.CompanyName;
                    break;
            }

            if (_selSettings.Contains(RepSettings.InvertedSorting.ToString()))
                return unsorted.OrderByDescending(orderByFunc).ToList();
            else
                return unsorted.OrderBy(orderByFunc).ToList();
        }

        protected class ViewReportStockHoldingsData // Note! Must have *property* field for each 'RepColumn' w same name w type *string* w view formatted
        {
            /* RULES:
             * - Keep all field names clean of HC as user doesnt need to see all that HC tuff on headers even all fields are HC's
             * - Keep everything as STRING that is getting viewed, that way formatting easily passes to all exports on identical way
             * - As sorting still needs numbers, looks like we do double fields
             */
            public string Ticker { get; set; }
            public string CompanyName { get; set; }
            public string Group { get; set; }

            public string AvrgPrice { get; set; }               public decimal AvrgPriceD { get; set; }
            public string AvrgTime { get; set; }                public decimal AvrgTimeD { get; set; }

            public string Invested { get; set; }                public decimal InvestedD { get; set; }
            public string Valuation { get; set; }               public decimal ValuationD { get; set; }
            public string WeightP { get; set; }                 public decimal WeightPD { get; set; }

            public string DivPLast { get; set; }                public decimal DivPLastD { get; set; }
            public string DivTotal { get; set; }                public decimal DivTotalD { get; set; }
            public string DivPTotal { get; set; }               public decimal DivPTotalD { get; set; }
            public string TotalGain { get; set; }               public decimal TotalGainD { get; set; }
            public string TotalPGain { get; set; }              public decimal TotalPGainD { get; set; }

            public string Notes { get; set; }
        }

#region EXPORT

        protected async Task OnExportAsync(RepExport format)
        {
            string stringContent = string.Empty;
            string filename = string.Empty;

            switch (format)
            {
                case RepExport.CSV:
                    stringContent = Local_ExportCSV();
                    filename = "PfsExportHoldings_" + DateTime.Today.ToString("yyyyMMdd") + ".csv";
                    break;

                case RepExport.HTMLTable:
                    stringContent = CreateHTMLTable();
                    filename = "PfsHoldingsHtmlTable_" + DateTime.Today.ToString("yyyyMMdd") + ".txt";
                    break;

                case RepExport.HTMLNotes:
                    stringContent = CreateHTMLNotesTable();
                    filename = "PfsHoldingsHtmlNotes_" + DateTime.Today.ToString("yyyyMMdd") + ".txt";
                    break;
            }

            if (string.IsNullOrWhiteSpace(stringContent) == false)
            {
                //await BlazorDownloadFileService.DownloadFile(filename, stringContent, "application/zip");
                await BlazorDownloadFileService.DownloadFileFromText(filename, stringContent, "text/plain");
            }

            return;

            string Local_ExportCSV()
            {
                try
                {
                    List<ViewReportStockHoldingsData> exportReport = new();

                    foreach (ReportExportHoldingsData inData in _origData)   // ExportCSV *only* outputs actual stock rows, no totals!
                        exportReport.Add(ConvertOrigDataToView(inData));

                    var dynRecords = CreateDynRecords(exportReport);

                    using (var writer = new StringWriter())
                    using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                    {
                        csv.WriteRecords(dynRecords);

                        return writer.ToString();
                    }
                }
                catch (Exception)
                {
                }
                return string.Empty;
            }
        }

#endregion

#region SAVE SETTINGS

        protected void OnBtnSaveSelections()
        {
            SettingsOnLocalStorage store = new()
            {
                SelColumns = _selColumns,
                SortBy = _sortBy,
                SelSettings = _selSettings,
            };
            string saveJSON = JsonSerializer.Serialize(store);

            PfsClientPlatform.LocalStorageStore(_localStorageUiKey, saveJSON);
        }

        protected bool InitPerSavedSelections()
        {
            try
            {
                string savedJSON = PfsClientPlatform.LocalStorageGet(_localStorageUiKey);

                if (string.IsNullOrWhiteSpace(savedJSON) == true)
                    return false;

                SettingsOnLocalStorage sett = JsonSerializer.Deserialize<SettingsOnLocalStorage>(savedJSON);

                if (sett == null || sett.SelColumns == null || sett.SortBy == null || sett.SelSettings == null)
                    // Add all fields here, as new fields may set null by json if not found.. and then problems...
                    return false;

                _selColumns = sett.SelColumns;
                _sortBy = sett.SortBy;
                _selSettings = sett.SelSettings;

                return true;
            }
            catch (Exception)
            {
                PfsClientPlatform.LocalStorageRemove(_localStorageUiKey);
            }
            return false;
        }

        protected class SettingsOnLocalStorage
        {
            public string SelColumns { get; set; }
            public string SortBy { get; set; }
            public string SelSettings { get; set; }
        }

#endregion
    }
}
