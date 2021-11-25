/*
 * Copyright (c) 2021 Jami Suni
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
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

using BlazorDownloadFile; // https://github.com/arivera12/BlazorDownloadFile

using PFS.Shared.Types;
using PFS.Shared.UiTypes;
using PFS.Shared.Stalker;
using PFS.Shared.StalkerAddons;

namespace PfsDevelUI.Components
{
    // Dialog: AccountImport, allows user to import notes, or full account backup.
    public partial class DlgImport
    {
        [Inject] PfsClientAccess PfsClientAccess { get; set; }
        [Inject] private IDialogService Dialog { get; set; }

        [CascadingParameter] MudDialogInstance MudDialog { get; set; }

        [Inject] IBlazorDownloadFileService BlazorDownloadFileService { get; set; }

        protected static readonly ReadOnlyDictionary<ImportType, DlgImportCfg> _configs = new ReadOnlyDictionary<ImportType, DlgImportCfg>(new Dictionary<ImportType, DlgImportCfg>
        {
            [ImportType.AccountBackupZip] = new DlgImportCfg()
            {
                ContentTxt = false,
                SupportsDryRun = false,
                ImportNote = "PfsExport_YYYYMMDD.zip. This is restoring previous full account backup status to account." +
                             "running this is going to automatically generate backup of information to be removed",
                ImportWarning = "Warning! This is going to remove all existing data, please be carefull!",
            },
            [ImportType.StockNotesTxt] = new DlgImportCfg()
            {
                ContentTxt = true,
                SupportsDryRun = false,
                ImportNote = "StockNotes.txt. Used to import manually edited or previous backup of stock notes",
                ImportWarning = "Warning! All previous Stock Note descriptions are going to be replaced with ones from this txt file",
            },
            [ImportType.StockCustomStockListTxt] = new DlgImportCfg()
            {
                ContentTxt = true,
                SupportsDryRun = true,
                ImportNote = "Uses special formatted file to import more stocks to account. All operations are incremental " +
                             "so no previous information is lost. Format to be used is txt file w syntax where:" + Environment.NewLine + 
                             "#portfolio#stockgroup "+ Environment.NewLine +
                             "NYSE,T " + Environment.NewLine +
                             "MSFT ",               
                ImportWarning = null,
            },
            [ImportType.StalkerCmdSet] = new DlgImportCfg()
            {
                ContentTxt = true,
                SupportsDryRun = false,
                ImportNote = "Takes CmdLine formatted set of commands in as txt file.",
                ImportWarning = null,
            },
            [ImportType.WaveAlarms] = new DlgImportCfg()
            {
                ContentTxt = true,
                SupportsDryRun = true,
                ImportNote = "Eats STOCKS_2020.txt, and imports those ~$TICKER~...~ alarms out of there. " + Environment.NewLine +
                             "Note, doesnt create new stocks but only only updates alarms (and removed ALL old alarms from those stocks)",
                ImportWarning = "Warning! Pretty please, always do DryRun first, as gives bunch of extra info there to check!",
            },
            [ImportType.KirkVslCsv] = new DlgImportCfg()
            {
                ContentTxt = true,
                SupportsDryRun = true,
                ImportNote = "On VSL: File-Download-Comma Separated Value" + Environment.NewLine +
                             "Note, creates list under PF=Kirk SG=VSL. Any existing data on SG names VSL is deleted!",
                ImportWarning = "....",
            },
        });

        protected bool _fullscreen { get; set; } = false;

        protected ImportType _importType { get; set; } = ImportType.Unknown;

        protected string _dryRunOutput { get; set; }

        IBrowserFile _selectedFile = null; // This is Microsoft provided, no nuget's required

        List<ImportType> _supportedImports = null;

        protected bool _showBusySignal = false;
        protected override void OnInitialized()
        {
            _supportedImports = new();
            _supportedImports.Add(ImportType.AccountBackupZip);
            _supportedImports.Add(ImportType.StockNotesTxt);        // !!!LATER!!! Add as AdminUI activable FEA??
            _supportedImports.Add(ImportType.WaveAlarms);           // !!!LATER!!! Add as AdminUI activable FEA??

            if (PfsClientAccess.Account().AccountProperty(UserSettPropertyAdmin.FeaKirkVslImport.ToString()) == "TRUE" )
                _supportedImports.Add(ImportType.KirkVslCsv);

#if false
        StockCustomStockListTxt,        // Nope, not tested for long time.. need full recheck/rewrite
        StalkerCmdSet,                  // Could active, but keep it hidden for now...
#endif
        }

        protected void OnImportTypeChanged(ImportType type)
        {
            _importType = type;
            _selectedFile = null;
            _dryRunOutput = string.Empty;
            StateHasChanged();
        }

        private void OnInputFileChange(InputFileChangeEventArgs e)
        {
            // Note: Picky, hides if different format: Use: <InputFile OnChange="@OnInputFileChange" accept=".txt"></InputFile>

            // http://www.binaryintellect.net/articles/06473cc7-a391-409e-948d-3752ba3b4a6c.aspx
            _selectedFile = e.File;
            _dryRunOutput = string.Empty;
            this.StateHasChanged();
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

        protected async Task DlgDryRunAsync()
        {
            if (_selectedFile == null)
                return;

            _showBusySignal = true;

            MemoryStream ms = new MemoryStream();
            Stream stream = _selectedFile.OpenReadStream(2000000); // Default is just 512K, needs bit more...
            await stream.CopyToAsync(ms);

            string result = System.Text.Encoding.UTF8.GetString(ms.ToArray());

            _dryRunOutput = await PfsClientAccess.Account().ConvertTextContentAsync(_importType, result);

            _showBusySignal = false;
        }

        protected async Task DlgOkAsync()
        {
            bool ret = true;
            string failedMsg = "Something is wrong looks like? yes, something wrong...";

            _dryRunOutput = string.Empty;

            if (_selectedFile == null)
            {
                bool? result = await Dialog.ShowMessageBox("Failed!", "Dont we need file to convert? How about select one?", yesText: "Ok");
                return;
            }

            if (_importType == ImportType.AccountBackupZip)
            {
                try
                {
                    byte[] zip = PfsClientAccess.Account().ExportAccountAsZip();
                    string fileName = "PfsExport_" + DateTime.Today.ToString("yyyyMMdd") + ".zip";
                    await BlazorDownloadFileService.DownloadFile(fileName, zip, "application/zip");
                }
                catch (Exception)
                {
                    // !!!LATER!!! Should really make this work always, but some messed up cases we need import even export doesnt work...
                }
            }

            _showBusySignal = true;

            MemoryStream ms = new MemoryStream();
            Stream stream = _selectedFile.OpenReadStream(2000000); // Default is just 512K, needs bit more...
            await stream.CopyToAsync(ms);

            if (_configs[_importType].ContentTxt == false)
            {
                // Note! Atm only bin/zip format supported is out very own PfsExport.. so has dedicated function 

                // This cleans all user data locally, imports new tuff per file, and backups locally.. so all replaced!
                ret = PfsClientAccess.Account().ImportAccountFromZip(ms.ToArray());
            }
            else if (_importType == ImportType.StalkerCmdSet)
            {
                StalkerError error = StalkerError.OK;

                // Note! This is handled separately, as w new Stalker -library is pretty dang flexible and has its own interface
                string result = System.Text.Encoding.UTF8.GetString(ms.ToArray());

                string[] lines = result.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
                List<string> stalkerActionSet = new();

                StalkerContent stalkerContent = PfsClientAccess.StalkerMgmt().GetCopyOfStalker();

                StalkerCmdLine stalkerCmdLineInterpreter = new StalkerCmdLine(stalkerContent);

                foreach ( string line in lines )
                {
                    error = stalkerCmdLineInterpreter.DoCmdLine(line.Replace("\r", null), out _);

                    if (error != StalkerError.OK)
                    {
                        failedMsg = string.Format("({0}) {1}", error.ToString(), line.Replace("\r", null));
                        break;
                    }
                }

                if (error == StalkerError.OK)
                {
                    List<string> output;
                    error = stalkerCmdLineInterpreter.DoCmdLine("LIST", out output);

                    if (error == StalkerError.OK && output != null && output.Count() > 0)
                    {
                        error = PfsClientAccess.StalkerMgmt().DoActionSet(output);

                        if ( error != StalkerError.OK )
                        {
                            failedMsg = "DoActionSet() -failed, hmm...";
                        }
                    }
                }

                if (error == StalkerError.OK)
                    ret = true;
                else
                    ret = false;
            }
            else
            {
                string result = System.Text.Encoding.UTF8.GetString(ms.ToArray());

                ret = await PfsClientAccess.Account().ImportTextContentAsync(_importType, result);
            }


            _showBusySignal = false;

            // !!!TODO!!! !!!LATER!!! Get result bool, if fails partially or fully show error and list of errors on import per List<string>

            if (ret == true)
            {
                MudDialog.Close();
            }
            else
            {
                bool? result = await Dialog.ShowMessageBox("Failed!", failedMsg, yesText: "Ok");
            }
        }

        protected struct DlgImportCfg
        {
            public string ImportNote { get; set; }
            public string ImportWarning { get; set; }
            public bool ContentTxt { get; set; }
            public bool SupportsDryRun { get; set; }
        }
    }
}