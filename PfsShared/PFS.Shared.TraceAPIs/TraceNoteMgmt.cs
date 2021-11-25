/*
 * Copyright (c) 2021 Jami Suni
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections.Generic;
using PFS.Shared.UiTypes;

namespace PFS.Shared.TraceAPIs
{
    public class TraceNoteMgmt : INoteMgmt
    {
        public event EventHandler<string> ParsingEvent;

        protected INoteMgmt _forward;

        protected int _saIdSymbols = 0;
        protected TraceSymbols _symbols;

        public TraceNoteMgmt(ref INoteMgmt forward, ref TraceSymbols symbols)
        {
            _symbols = symbols;
            _forward = forward;
        }

        // !!!THINK!!! Add ConvertBase -class w some helper functions, like large string broken per lines and added out 'line' w ^tag= prefixes

        public void NoteSave(Guid STID, StockNote note)
        {
            _forward.NoteSave(STID, note);

            string line = string.Format("! NoteSave {0}", GetGuidSymbol(STID));

            // !!!TODO!!! Add note content to line

            ParsingEvent?.Invoke(this, line);
        }

        public StockNote NoteGet(Guid STID)
        {
            // !S StockNoteGet @STID001

            StockNote ret = _forward.NoteGet(STID);

            string line = string.Format("! StockNoteGet {0}", GetGuidSymbol(STID));

            if (ret != null)
            {
                // !!!TODO!!! Add content w ^ 
            }

            ParsingEvent?.Invoke(this, line);

            return ret;
        }

        public string NoteGetOverview(Guid STID)
        {
            // !S StockNoteGetOverview @STID001
            string ret = "";
            StockNote note = _forward.NoteGet(STID);

            string line = string.Format("! StockNoteGetOverview {0}", GetGuidSymbol(STID));

            if (note != null)
            {
                ret = note.Overview;

                // !!!TODO!!! Add 'ret' to line
            }

            ParsingEvent?.Invoke(this, line);

            return ret;
        }

        public string[] GetTagGroup(int groupID)
        {
            string[] fields = _forward.GetTagGroup(groupID);

            string line = string.Format("!F \x1F GetTagGroup !!!TODO!!!");

            // !!!TODO!!!

            ParsingEvent?.Invoke(this, line);

            return fields;
        }
        public bool SaveTagGroup(int groupID, string[] fields)
        {
            bool ret = _forward.SaveTagGroup(groupID, fields);

            string line = string.Format("!F \x1F SaveTagGroup !!!TODO!!!");

            // !!!TODO!!!

            ParsingEvent?.Invoke(this, line);

            return ret;
        }

        public List<TagGroupsUsage> GetTagGroupsUsage()
        {
            List<TagGroupsUsage> ret = _forward.GetTagGroupsUsage();

            string line = string.Format("!F \x1F GetTagGroupsUsage !!!TODO!!!");

            // !!!TODO!!!

            ParsingEvent?.Invoke(this, line);

            return ret;
        }

        public void SaveTagGroupsUsage(List<TagGroupsUsage> tags)
        {
            _forward.SaveTagGroupsUsage(tags);

            string line = string.Format("!F \x1F SaveTagGroupsUsage !!!TODO!!!");

            // !!!TODO!!!

            ParsingEvent?.Invoke(this, line);
        }

        protected string GetGuidSymbol(Guid ID)
        {
            return _symbols.GetSymbol(ID.ToString());
        }
    }
}
