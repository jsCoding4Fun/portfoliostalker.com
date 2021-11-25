/*
 * Copyright (c) 2021 Jami Suni
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace PFS.Shared.StalkerAddons
{
    internal class StalkerCmdLineTemplate
    {
        // string per expected parameter, on order, presenting Name of field and its expected content
        public static string[] Get(CmdLineOperation Operation, CmdLineElement Element)
        {
            ActionTemplate template = Templates.Where(t => t.Operation == Operation && t.Elements.Contains(Element) == true ).SingleOrDefault();

            if (template == null)
                return null;

            return template.Params.Split(' ');
        }

        protected class ActionTemplate
        {
            public CmdLineOperation Operation { get; set; }
            public List<CmdLineElement> Elements { get; set; }
            public string Params { get; set; }
        };

        protected readonly static ImmutableArray<ActionTemplate> Templates = ImmutableArray.Create(new ActionTemplate[]
        {
            new ActionTemplate()                            // List-Alarm
            {
                Operation = CmdLineOperation.List,
                Elements = new List<CmdLineElement>() { CmdLineElement.Alarm },
                Params = "Stock=STID",
            },

            new ActionTemplate()                            // Add-Alarm???? Stock Value Note      (plus multiple Watch% with same prototype)
            {
                Operation = CmdLineOperation.Add,
                Elements = new List<CmdLineElement>()
                {
                    CmdLineElement.AlarmUnder,
                    CmdLineElement.AlarmUnderWatchP,
                    CmdLineElement.AlarmUnderWatch2P,
                    CmdLineElement.AlarmUnderWatch3P,
                    CmdLineElement.AlarmUnderWatch4P,
                    CmdLineElement.AlarmUnderWatch5P,
                    CmdLineElement.AlarmUnderWatch6P,
                    CmdLineElement.AlarmUnderWatch7P,
                    CmdLineElement.AlarmUnderWatch8P,
                    CmdLineElement.AlarmUnderWatch9P,

                    CmdLineElement.AlarmOver,
                    CmdLineElement.AlarmOverWatchP,
                },
                Params = "Stock=STID Value=Decimal:0.01 Note=String:0:100",
            },

            new ActionTemplate()                            // Add-AlarmUnderWatch Stock Value Param1 Note
            {
                Operation = CmdLineOperation.Add,
                Elements = new List<CmdLineElement>() { CmdLineElement.AlarmUnderWatch },
                Params = "Stock=STID Value=Decimal:0.01 Param1=Decimal:0.01 Note=String:0:100",
            },

            new ActionTemplate()                            // Edit-AlarmUnder Stock EditedValue Value Note
            {
                Operation = CmdLineOperation.Edit,
                Elements = new List<CmdLineElement>() { CmdLineElement.AlarmUnder },
                Params = "Stock=STID EditedValue=Decimal:0.01 Value=Decimal:0.01 Note=String:0:100",
            },

            new ActionTemplate()                            // Edit-AlarmUnderWatch Stock EditedValue Value Param1 Note
            {
                Operation = CmdLineOperation.Edit,
                Elements = new List<CmdLineElement>() { CmdLineElement.AlarmUnderWatch },
                Params = "Stock=STID EditedValue=Decimal:0.01 Value=Decimal:0.01 Param1=Decimal:0.01 Note=String:0:100",
            },
        });
    }
}
