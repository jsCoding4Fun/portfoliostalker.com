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
    // Interface that all 'PFS.Shared.ExtTransaction' -providers must implement for Import Transaction functionality
    public interface IExtTransactions
    {
        string Convert2RawString(byte[] byteContent);

        List<ExtTransaction> Convert2ExtTransaction(byte[] byteContent);
    }
}
