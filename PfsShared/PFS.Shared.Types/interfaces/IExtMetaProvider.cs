/*
 * Copyright (c) 2021 Jami Suni
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Collections.Generic;

namespace PFS.Shared.Types
{
    // Interface that different external providers implement for PFS. Do NOT let exceptions out!
    public interface IExtMetaProvider
    {
        void SetPrivateKey(string key);

        string GetLastError();

        List<CompanyMeta> GetAllStocksOnMarket(MarketMeta marketMeta);

        List<CompanyMeta> GetAllStocksOnCSV(MarketMeta marketMeta, string csvContent);
    }
}
