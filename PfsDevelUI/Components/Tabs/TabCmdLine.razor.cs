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
using Microsoft.AspNetCore.Components.Web;

using PfsDevelUI.PFSLib;

using PFS.Shared.Types;
using PFS.Shared.Stalker;
using PFS.Shared.StalkerAddons;

namespace PfsDevelUI.Components
{
    public partial class TabCmdLine
    {
        protected string _cmdLine;      // Everything to know from (MudTextField) bind : https://docs.microsoft.com/en-us/aspnet/core/blazor/components/data-binding?view=aspnetcore-5.0
        protected string _cmdLog;

        StalkerCmdLine _userCmdLine = null;

        [Inject] PfsClientAccess PfsClientAccess { get; set; }
        [Inject] PfsClientPlatform PfsClientPlatform { get; set; }

        protected override void OnInitialized()
        {
            StalkerContent stalkerContent = PfsClientAccess.StalkerMgmt().GetCopyOfStalker();
            _userCmdLine = new StalkerCmdLine(stalkerContent);
        }

        protected void OnKeyUp(KeyboardEventArgs args)
        {
            if (string.IsNullOrWhiteSpace(_cmdLine) == true)
                return;

            if ( args.Key == "Enter" )
            {
                List<string> output;

                if ( (_cmdLine == "COMMIT" || _cmdLine == "SAVE") )
                {
                    if ( DoSave() == true)
                    {
                        _cmdLog = string.Empty;
                        _cmdLine = string.Empty;

                        StalkerContent stalkerContent = PfsClientAccess.StalkerMgmt().GetCopyOfStalker();
                        _userCmdLine = new StalkerCmdLine(stalkerContent);

                        StateHasChanged();
                    }
                    return;
                }
#if false
                if ( _cmdLine.StartsWith("LOCALSTORE ") == true )       // Handly little command allowing to set/clear any LocalStorage key record!
                {
                    if (LocalStore(_cmdLine) == true)
                    {
                        _cmdLog = string.Empty;
                        _cmdLine = string.Empty;
                    }
                    return;
                }
#endif
                StalkerError error = _userCmdLine.DoCmdLine(_cmdLine, out output);

                if (error == StalkerError.OK)
                {
                    string entry = _cmdLine + Environment.NewLine;

                    if (output != null)
                        foreach (string outLine in output)
                            entry += "   " + outLine + Environment.NewLine;

                    _cmdLog = entry + _cmdLog;
                    _cmdLine = string.Empty;
                }
                else
                {
                    _cmdLog = string.Format("[{0}] {1}", error, _cmdLine) + Environment.NewLine + _cmdLog;
                }

                StateHasChanged();
            }
            return;

#if false
            bool LocalStore(string cmdLine)
            {
                // Allows to set/clear any localstorage key to specific value
                cmdLine = cmdLine.Replace("LOCALSTORE ", "").TrimStart();
                int firstSpaceIndex = cmdLine.IndexOf(' ');

                if (firstSpaceIndex <= 0)
                    return false;

                string key = cmdLine.Substring(0, firstSpaceIndex).TrimEnd();
                string value = cmdLine.Substring(firstSpaceIndex).TrimStart().TrimEnd();

                if (string.IsNullOrWhiteSpace(value) == true)
                    PfsClientPlatform.LocalStorageRemove(key);
                else
                    PfsClientPlatform.LocalStorageStore(key, value);

                return true;
            }
#endif
        }

        protected bool DoSave()
        {
            List<string> output;
            StalkerError error = _userCmdLine.DoCmdLine("LIST", out output);

            if ( error == StalkerError.OK && output != null && output.Count() > 0 )
            {
                error = PfsClientAccess.StalkerMgmt().DoActionSet(output);

                if (error == StalkerError.OK)
                    return true;
            }
            return false;
        }
    }
}
