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

namespace PFS.Shared.TraceAPIs
{
    public class TraceSymbols         // !!!TODO!!! Double check after testing backup, that this is even need anymore... I mean could use for STID_T etc..
    {
        protected Dictionary<string, string> m_symbols = new Dictionary<string, string>();

        public void Add(string symbol, string value)
        {
            if (m_symbols.ContainsKey(symbol) == true)
                // Already added
                return;

            m_symbols.Add(symbol, value);
        }

        public string GetValue(string symbol)
        {
            if (m_symbols.ContainsKey(symbol) == false)
                return "";

            return m_symbols[symbol];
        }

        public Guid GetValueAsGuid(string symbol)
        {
            string ret = GetValue(symbol);

            if (ret == "")
                return Guid.Empty;

            return Guid.Parse(ret);
        }

        public string GetSymbol(string value)
        {
            if (m_symbols.ContainsValue(value) == false)
                return "";

            return m_symbols.Single(p => p.Value == value).Key;
        }
    }
}
