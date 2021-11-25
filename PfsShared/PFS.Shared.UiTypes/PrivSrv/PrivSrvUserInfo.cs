using System;
using System.Text;

namespace PFS.Shared.UiTypes
{
    public class PrivSrvUserInfo
    {
        public string Username { get; set; }
        public bool Admin { get; set; }
        public int TrackedStocks { get; set; }
        public DateTime Expiration { get; set; }
        public string Note { get; set; }
        public string UserProperties { get; set; } // !!!Important!!! Dont use this 'set' on code, its just for WAPI use.. use 'SetProperties'

        public bool SetProperties(string properties)
        {
            if (string.IsNullOrWhiteSpace(properties) == true)
                UserProperties = string.Empty;

            StringBuilder set = new();

            string[] splitCombos = properties.Split(';');

            foreach (string combo in splitCombos)
            {
                PrivSrvUserProperty propID;
                string[] split = combo.Split('=');

                if (split.Length != 2 || Enum.TryParse(split[0], out propID) == false || string.IsNullOrWhiteSpace(split[1]) == true )
                    return false;

                if (set.Length > 0)
                    set.Append(';');

                set.Append(propID.ToString() + '=' + split[1]);
            }

            UserProperties = set.ToString();
            return true;
        }

        public string GetProperty(PrivSrvUserProperty propID)
        {
            if (string.IsNullOrWhiteSpace(UserProperties) == true)
                return null;

            string[] splitCombos = UserProperties.Split(';');

            foreach (string combo in splitCombos)
            {
                PrivSrvUserProperty checkPropID;
                string[] split = combo.Split('=');

                if (split.Length != 2 || Enum.TryParse(split[0], out checkPropID) == false || string.IsNullOrWhiteSpace(split[1]) == true)
                    return null;

                if (checkPropID == propID)
                    return split[1];
            }
            return null;
        }
    }

    public enum PrivSrvUserProperty : int
    {
        Unknown = 0,

        MaxAllowedStocks,       // If set, allowes a special limit of stocks for user (0 == unlimited)
    }
}
