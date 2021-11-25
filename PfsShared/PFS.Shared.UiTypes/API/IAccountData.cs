/*
 * Copyright (c) 2021 Jami Suni
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Threading.Tasks;

namespace PFS.Shared.UiTypes
{
    // Access to Account's User -information and to different Export/Import functionalities related to Account
    public interface IAccountData
    {
        // returns errorMsg, so string.Empty is OK
        Task<string> UserLoginAsync(string username, string password, bool remember);

        void UserLogout();

        Task<bool> UserRegisterAsync(string username, string password, string email);

        Task<bool> UserChangePasswordAsync(string oldPassword, string newPassword);

        Task<bool> UserChangeEmailAsync(string password, string newEmail);

        // UserRecoverPassword(string email); !!!WAY LATER!!!

        string Property(string property, string value = null);

        string AccountProperty(string property);
        Task<bool> AccountPropertySetAsync(string property, string value);

        // Clears all UserData off locally, from Caches & LocalStorage.. and if absoluteAll = true, then does full LocalStorage nuke (incl StockNotes)
        void ClearLocally(bool absoluteAll = false);

        // Main local user oriented backup functionality uses zip format w multiple files
        bool ImportAccountFromZip(byte[] zip);

        byte[] ExportAccountAsZip();

        Task<bool> BackupAccountZipToPfsAsync();

        Task<(byte[] content, DateTime backupDateUTC)> FetchAccountBackupZipFromPfsAsync();

        // General text file / textual data import function for multiple use cases
        Task<bool> ImportTextContentAsync(ImportType importType, string content);

        // DryRun version of 'ImportTextContentAsync', allowing user to preview result before import, works limited type's
        Task<string> ConvertTextContentAsync(ImportType importType, string content);

        // Identical structure set/get as PrivServer has used for Local WASM related provider/market configs

        SettMarketProviders GetLocalMarketProviders();
        void SetLocalMarketProviders(SettMarketProviders config);
    }
}
