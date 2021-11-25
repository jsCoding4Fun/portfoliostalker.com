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

namespace PFS.Shared.UiTypes
{
    // Must be provided to PFS.Client by owner as a way to access platform services
    public interface IClientPlatform
    {
        DateTime GetCurrentUtcTime();

        DateTime GetCurrentLocalTime();

        void LocalStorageStore(string key, string value);

        string LocalStorageGet(string key);

        void LocalStorageRemove(string key);

        void LocalStorageClearAll();

        List<string> LocalStorageGetKeys();

        List<ExtDataProviders> GetClientProviderIDs(ExtDataProviderJobType jobType);
    }
}
