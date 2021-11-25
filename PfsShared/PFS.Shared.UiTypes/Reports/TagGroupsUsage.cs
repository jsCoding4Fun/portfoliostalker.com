using System;
using System.Collections.Generic;

using PFS.Shared.Types;

namespace PFS.Shared.UiTypes
{
    public class TagGroupsUsage
    {
        public const int MaxTagGroups = 3; // Note! UI trusts atm this value to be minimum (3)

        public const int MaxTagGroupValues = 15;    // 2021-Nov: Increased from 12->15 as started feel tight

        public StockMeta StockMeta { get; set; }

        // current selections for different TagGroups for this stock. (string.Empty is handled as 'unselected')
        public string[] Groups { get; set; }

        public TagGroupsUsage()
        {
            Groups = new string[MaxTagGroups];
        }
    }
}
