/*
 * Copyright (c) 2021 Jami Suni
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections.Generic;

namespace PFS.Shared.UiTypes
{
    // Interface for all Note's related operations, currently StockNotes
    public interface INoteMgmt
    {
        // !!!LATER!!! Allow one Rich Text -document to be attached to User's each Stock..  long term better as gets rid of line breaks confusing display wide!

        void NoteSave(Guid STID, StockNote note);

        StockNote NoteGet(Guid STID);

        string NoteGetOverview(Guid STID);


        // Stock Note GROUP & TAGS (stored as part of StockNote so hold here)

        // returns null if group is not defined. returns group name on [0]. rest of field has userdef values, those can be string.empty's
        string[] GetTagGroup(int groupID);

        bool SaveTagGroup(int groupID, string[] fields);

        List<TagGroupsUsage> GetTagGroupsUsage();

        // !!!LATER!!! I really dont like this function, batch update => hard to understand.. but hate to do it one by one also w lot of savings
        void SaveTagGroupsUsage(List<TagGroupsUsage> tags);
    }
}
