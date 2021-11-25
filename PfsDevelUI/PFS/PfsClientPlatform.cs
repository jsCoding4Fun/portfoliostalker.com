/*
 * Copyright (c) 2021 Jami Suni
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections.Generic;

using PFS.Shared.Types;
using PFS.Shared.UiTypes;

namespace PfsDevelUI.PFSLib
{
    public class PfsClientPlatform : IClientPlatform
    {
        // [inject] only works for blazor components, so here we need to pass it as ""constructor injection""
        private readonly Blazored.LocalStorage.ISyncLocalStorageService _localStorage = null;

        public PfsClientPlatform(Blazored.LocalStorage.ISyncLocalStorageService localStorage)
        {
            _localStorage = localStorage;
        }

        public DateTime GetCurrentUtcTime()
        {
            return DateTime.UtcNow;
        }

        public DateTime GetCurrentLocalTime()
        {
            return DateTime.Now;
        }

        public void LocalStorageStore(string key, string value)
        {
            // https://github.com/Blazored/LocalStorage
            _localStorage.SetItemAsString(key, value);
        }

        public string LocalStorageGet(string key)
        {
            string ret = "";

            try
            {
                ret = _localStorage.GetItemAsString(key);
            }
            catch ( Exception e )
            {
                string hmm = e.Message;
            }
            return ret;
        }

        public void LocalStorageRemove(string key)
        {
            _localStorage.RemoveItem(key);
        }

        public void LocalStorageClearAll()
        {
            _localStorage.Clear();
        }

        public List<string> LocalStorageGetKeys()
        {
            List<string> ret = new();

            for ( int i = 0; ; i++ )
            {
                string key = _localStorage.Key(i);

                if (string.IsNullOrWhiteSpace(key) == true)
                    break;

                ret.Add(key);
            }

            return ret;
        }

        // After lot of spinning, this seams most logical place to 'hardcode' provider's available for client (== WASM)
        public List<ExtDataProviders> GetClientProviderIDs(ExtDataProviderJobType jobType)
        {
            // Note! 'LocalFetchExtProviders' has actual implementation specifics, and requires changes when adding new ones

            // Decision! This doesnt NOT care about keys! So providers can be selected on use wo keys been set

            List<ExtDataProviders> ret = new();

            // As of 2021-Jul Tiingo, doesnt wanna work on WASM

            switch (jobType)
            {
                case ExtDataProviderJobType.Intraday:

                    ret.Add(ExtDataProviders.Marketstack);
                    ret.Add(ExtDataProviders.Iexcloud);
                    break;

                case ExtDataProviderJobType.EndOfDay:

                    ret.Add(ExtDataProviders.Unibit);
                    ret.Add(ExtDataProviders.Polygon);
                    ret.Add(ExtDataProviders.Marketstack);
                    ret.Add(ExtDataProviders.AlphaVantage);
                    ret.Add(ExtDataProviders.Iexcloud);
                    // ret.Add(ExtDataProviders.Tiingo);        Search: "TIINGO-NOT-ON-LOCAL"
                    break;
            }

            return ret;
        }

        protected Dictionary<ExtDataProviders, IExtDataProvider> _providerObjects = new();

        public void OnInitAddProviderObj(ExtDataProviders providerID, IExtDataProvider providerObj)
        {
            _providerObjects.Add(providerID, providerObj);
        }

        public IExtDataProvider GetProviderObj(ExtDataProviders providerID)
        {
            return _providerObjects[providerID];
        }
    }
}
