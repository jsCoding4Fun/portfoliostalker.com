using System;

namespace PFS.Shared.UiTypes
{
    public class News
    {
        public int ID { get; set; } // Forever up running ID, so highest ID is latest..

        public NewsCategory Category { get; set; }
        public NewsStatus Status { get; set; }

        public string Header { get; set; }
        public string Text { get; set; }
        public string Params { get; set; } = string.Empty;

        public DateTime Date { get; set; }

        public void AddParam(string param, string value)
        {
            Params += string.Format("{0}={1}{2}", param, value, Environment.NewLine);
        }

        public string GetParam(string param)
        {
            string[] allParams = Params.Split(Environment.NewLine);

            foreach ( string p in allParams )
            {
                if (string.IsNullOrWhiteSpace(p) == true)
                    continue;

                string pParam = p.Substring(0, p.IndexOf('='));
                string pValue = p.Substring(p.IndexOf('=') + 1);

                if (pParam == param)
                    return pValue;
            }
            return string.Empty;
        }
    }

    /* !!!DOCUMENT!!! News
     * 
     * Search !NEWS! for some hotspots on code
     *
     * Creation of news:
     * 
     * - User visible news are added thru AdminUI, that also has ability to close news to prevent sending it client.
     * - Internal news are special category used for sending 'hidden' information to clients:
     * 		- InternalStockMeta, is created by 'PfsMetaSrv' when latest Company info alters company name of tracked stock
     *                            or stock is removed totally from list or ticker has been changed. News itself is created 
     *					         and send thru 'PfsAccSrv' to all clients as hidden news if Admin 'approves this change
     * 						     issue on Admin UI' and updates new status to Tracked list. 
     *
     * News on PFS Servers:
     *
     * - 'PfsAccSrv' has 'AccSrvNews' component to handle storing, as news are atm only passed client as part of login
     *
     * News on Client: 
     *
     * - As part of 'FEAccountData' 'UserLoginAsync' it receives all newer news articles from 'PfsAccSrv' as a part of 
     *   successfull login response. Client holds 'LastNewsID' that it sends as part of login request to request any
     *   news articles newer that running ID is.
     * - Viewing of news is done on default view of application, where user can read and delete news articles.
     * - 'LocalContent' has 'NewsUnreadAmount' that allows easy access to know if there is something pending users reading
     * - Property "UNREADNEWS" passes same number to UI side, where its used example on PageHeader
     * - 'LocalNews' is place where received news information is stored clients local storage (unread/read ones)
     * - 'PageHeader' is showing special 'unread news available icon' that can be clicked to go reading news
     *
     * Handling of 'NewsCategory.InternalStockMeta'
     * - On client after successfull PFS login, all pending unread news items of this type are handled on background with 
     *   'OperInternalNews.UpdateStockMetaAsync' function assuming client is on Local Mode, meaning PrivSrv is not configured.
     * - If PrivSrv is configured then this 'OperInternalNews.UpdateStockMetaAsync' operation is done after successfull 
     *   private server login is performed 'FEPrivSrvMgmt' UserLoginAsync(string newaddr = null)
     * - 'OperInternalNews' on client side is responsible handling all internal unread events, by updating client side information
     *   and if PrivSrv is on use then also informing from changes there
     * -> On end if this change effected to specific user's stock information then news article type is changed, and its updated
     *    to be user readable notification from effects taken for specific stock
     */

    public enum NewsCategory : int // Dependency! If adding field here, see 'UpdateUnreadAmount' !!!
    {
        Unknown = 0,
        UserNormal,
        InternalStockMeta,
        FeaAdminReports,
    }

    // This status is used differently different places:
    // Server:      Unread is 'active', and Closed is 'removed' -> allowing change of mind for Admin to set 'closed' if should not sent it clients anymore
    // UI User:     Unread/Read/Closed go per users actions
    // UI Internal: Unread is 'unhandled', and Closed is 'handled'
    public enum NewsStatus : int
    {
        Unknown = 0,
        Unread,         
        Read,
        Closed,
    }
}
