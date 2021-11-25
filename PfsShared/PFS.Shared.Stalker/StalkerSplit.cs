/*
 * Copyright (c) 2021 Jami Suni
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Collections.Generic;

namespace PFS.Shared.Stalker
{
    public class StalkerSplit
    {
        // Instead of using standard string.Split... doing own as I wanna keep things together if has [] around it
        public static List<string> SplitLine(string line)
        {
            /* Rules:
             * - Supports [this is longer] and Note=[This is longer]    => 'this is longer' 'Note=This is longer'
             * - '[' is required to be after ' ' or '='
             * - ']' is required to be before space or end of line
             */
            List<string> ret = new();

            int open = 0;
            string split = string.Empty;
            char prevCh;
            char ch = '~';

            for ( int pos = 0; pos < line.Length; pos++ )
            {
                prevCh = ch;
                ch = line[pos];

                if (ch == '[' && (prevCh == ' ' || prevCh == '='))
                {
                    // increase open count
                    open++;
                    continue;
                }

                if (ch == ']' && open > 0 && (pos+1 == line.Length || line[pos+1] == ' ') )
                {
                    open--;
                    continue;
                }

                if (ch == ' ' && open == 0)
                {
                    if (string.IsNullOrWhiteSpace(split) == false)
                        ret.Add(split);

                    split = string.Empty;
                    continue;
                }

                split += ch;
            }

            if (string.IsNullOrWhiteSpace(split) == false)
                ret.Add(split);

            return ret;
        }
    }
}
