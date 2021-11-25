/*
 * Copyright (c) 2021 Jami Suni
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Linq;

namespace PFS.Shared.Stalker
{
    /* On string, do nor allow:
     * {} as used by C#
     * [] as used by concat cmd Lines
     * = as used by concat cmd lines
     * " as part of C#
     * 
     * => Preferred in code delimeter is ASCII 31 (0x1F) Unit Separator (that is pretty much zero use on writing)
     */

    // 
    public class StalkerValidate
    {
        static readonly string _allowedChars = " _#-!@$%^&*()+,.<>:;?~";

        static public bool String(string content, CharSet charSet)
        {
            bool ret = false;

            if (content.All(c => Char.IsLetterOrDigit(c) || _allowedChars.Contains(c)) == true)
                // Atm all these characters are allowed in ALL cases, if changes come do exceptions below
                ret = true;

            switch ( charSet )
            {
                case CharSet.CharSetCompanyName:
                    // Had to add ' to company names as little company named ""MCD,McDonald's Corporation"" uses 
                    // And '/' as CODI,D/B/A Compass Diversified Holdings Shares of Beneficial Interest
                    if (content.All(c => Char.IsLetterOrDigit(c) || _allowedChars.Contains(c) == true || "'/".Contains(c) == true) )
                        ret = true;
                    break;

                case CharSet.CharSetPfName:
                case CharSet.CharSetSgName:

                    if (content.Contains('#') == true)
                        return false;
                    break;
            }

            return ret;
        }

        public enum CharSet : int
        {
            Unknown = 0,
            CharSetNote,
            CharSetPfName,          // As its passed by NavMenu as URL parameter, following found illegal: #
            CharSetSgName,          // As its passed by NavMenu as URL parameter, following found illegal: #
            CharSetTicker,
            CharSetCompanyName,
            CharSetHoldingID,
            CharSetHoldingNote,
            CharSetTradeID,
            CharSetDividentID,
        }
    }
}

