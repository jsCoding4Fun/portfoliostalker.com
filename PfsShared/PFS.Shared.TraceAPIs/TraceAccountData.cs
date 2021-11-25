/*
 * Copyright (c) 2021 Jami Suni
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Threading.Tasks;
using System.Text.Json;

using PFS.Shared.UiTypes;

namespace PFS.Shared.TraceAPIs
{
    // Allows to capture all traffic to PFS.Client's AccountData -component to log/trace it for replay/debug/etc purposes
    public class TraceAccountData : IAccountData
    {
        public event EventHandler<string> ParsingEvent;

        protected IAccountData _forward;

        public TraceAccountData(ref IAccountData forward)
        {
            _forward = forward;
        }

        public async Task<string> UserLoginAsync(string username, string password, bool remember)
        {
            string ret = await _forward.UserLoginAsync(username, password, remember);

            string line = string.Format("!A \x1F UserLoginAsync \x1F username=USERNAME \x1F password=PASSWORD \x1F remember={0}", remember.ToString());

            line += Environment.NewLine + "^ ret:" + ret.ToString();

            ParsingEvent?.Invoke(this, line);

            return ret;
        }

        public void UserLogout()
        {
            _forward.UserLogout();

            string line = string.Format("!A \x1F UserLogout");

            ParsingEvent?.Invoke(this, line);
        }

        public async Task<bool> UserRegisterAsync(string username, string password, string email)
        {
            bool ret = await _forward.UserRegisterAsync(username, password, email);

            string line = string.Format("!A \x1F UserRegisterAsync \x1F username=USERNAME \x1F password=PASSWORD \x1F email=SECRET");

            line += Environment.NewLine + "^ ret:" + ret.ToString();

            ParsingEvent?.Invoke(this, line);

            return ret;
        }

        public async Task<bool> UserChangePasswordAsync(string oldPassword, string newPassword)
        {
            bool ret = await _forward.UserChangePasswordAsync(oldPassword, newPassword);

            string line = string.Format("!A \x1F UserChangePasswordAsync \x1F oldPassword=SECRET \x1F newPassword=SECRET");

            line += Environment.NewLine + "^ ret:" + ret.ToString();

            ParsingEvent?.Invoke(this, line);

            return ret;
        }

        public async Task<bool> UserChangeEmailAsync(string password, string newEmail)
        {
            bool ret = await _forward.UserChangeEmailAsync(password, newEmail);

            string line = string.Format("!A \x1F UserChangeEmailAsync \x1F password=PASSWORD \x1F email=SECRET");

            line += Environment.NewLine + "^ ret:" + ret.ToString();

            ParsingEvent?.Invoke(this, line);

            return ret;
        }

        public string Property(string property, string value = null)
        {
            string ret = _forward.Property(property, value);

            string line = string.Format("!A \x1F Property \x1F property={0} \x1F value={1}", property, value != null ? value : string.Empty);

            line += Environment.NewLine + "^ ret:" + ret.ToString();

            ParsingEvent?.Invoke(this, line);

            return ret;
        }

        public string AccountProperty(string property)
        {
            string ret = _forward.AccountProperty(property);

            string line = string.Format("!A \x1F AccountProperty \x1F property={0}", property);

            line += Environment.NewLine + "^ ret:" + ret.ToString();

            ParsingEvent?.Invoke(this, line);

            return ret;
        }

        public async Task<bool> AccountPropertySetAsync(string property, string value)
        {
            bool ret = await _forward.AccountPropertySetAsync(property, value);

            string line = string.Format("!A \x1F AccountPropertySetAsync \x1F property={0} \x1F value={1}", property, value);

            line += Environment.NewLine + "^ ret:" + ret.ToString();

            ParsingEvent?.Invoke(this, line);

            return ret;
        }

        public SettMarketProviders GetLocalMarketProviders()
        {
            SettMarketProviders ret = _forward.GetLocalMarketProviders();

            string line = string.Format("!A \x1F GetLocalMarketProviders");

            line += "^ ret: [JSON-SettMarketProviders]" + JsonSerializer.Serialize(ret);

            ParsingEvent?.Invoke(this, line);

            return ret;
        }

        public void SetLocalMarketProviders(SettMarketProviders config)
        {
            _forward.SetLocalMarketProviders(config);

            string line = string.Format("!A \x1F SetLocalMarketProviders \x1F config={0}", JsonSerializer.Serialize(config));

            ParsingEvent?.Invoke(this, line);
        }

        public void ClearLocally(bool absoluteAll = false)
        {
            _forward.ClearLocally(absoluteAll);

            string line = string.Format("!A \x1F ClearLocally \x1F absoluteAll={0}", absoluteAll.ToString());

            ParsingEvent?.Invoke(this, line);
        }

        public async Task<bool> ImportTextContentAsync(ImportType importType, string content)
        {
            bool ret = await _forward.ImportTextContentAsync(importType, content);

            string line = string.Format("!A \x1F ImportTextContentAsync \x1F importType={0} \x1F content={1}", importType.ToString(), content);

            line += Environment.NewLine + "^ ret:" + ret.ToString();

            ParsingEvent?.Invoke(this, line);

            return ret;
        }

        public async Task<string> ConvertTextContentAsync(ImportType importType, string content)
        {
            string ret = await _forward.ConvertTextContentAsync(importType, content);

            string line = string.Format("!A \x1F ConvertTextContentAsync \x1F importType={0} \x1F content={1}", importType.ToString(), content);

            ParsingEvent?.Invoke(this, line);

            return ret;
        }

        public bool ImportAccountFromZip(byte[] zip)
        {
            bool ret = _forward.ImportAccountFromZip(zip);

            string line = string.Format("!A \x1F ImportAccountFromZip zipLength={0}", zip.Length);

            line += Environment.NewLine + "^ ret:" + ret.ToString();

            ParsingEvent?.Invoke(this, line);

            return ret;
        }

        public byte[] ExportAccountAsZip()
        {
            byte[] ret = _forward.ExportAccountAsZip();

            string line = string.Format("!A \x1F ExportAccountAsZip");

            line += Environment.NewLine + "^ TotalBytes=" + ret.Length;

            ParsingEvent?.Invoke(this, line);

            return ret;
        }

        public async Task<bool> BackupAccountZipToPfsAsync()
        {
            bool ret = await _forward.BackupAccountZipToPfsAsync();

            string line = string.Format("!A \x1F BackupAccountZipToPfsAsync");

            line += Environment.NewLine + "^ ret:" + ret.ToString();

            ParsingEvent?.Invoke(this, line);

            return ret;
        }

        public async Task<(byte[] content, DateTime backupDateUTC)> FetchAccountBackupZipFromPfsAsync()
        {
            (byte[] content, DateTime backupDateUTC) = await _forward.FetchAccountBackupZipFromPfsAsync();

            string line = string.Format("!A \x1F FetchAccountBackupZipFromPfsAsync");

            if ( backupDateUTC != DateTime.MinValue && content != null )
                line += Environment.NewLine + "^ backupDateUTC=" + backupDateUTC.ToString("yyyy-MMM-ddd hh:mm") + " TotalBytes=" + content.Length;
            else
                line += Environment.NewLine + "^ failed!";

            ParsingEvent?.Invoke(this, line);

            return (content: content, backupDateUTC: backupDateUTC);
        }
    }
}
